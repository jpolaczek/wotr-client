import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parents[2] / 'scripts'
sys.path.insert(0, str(SCRIPTS))
import test_inventory as inventory
import test_fixture as fixture
spec = importlib.util.spec_from_file_location('batch_runner', SCRIPTS / 'test-client-batches.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class TestInventoryTests(unittest.TestCase):
    def test_missing_extra_and_skipped_cases_cannot_pass(self):
        with self.assertRaisesRegex(ValueError, 'missing'):
            inventory.validate_selection(['a', 'b'], {'a': {'result': 'Passed'}})
        with self.assertRaisesRegex(ValueError, 'extra'):
            inventory.validate_selection(['a'], {'a': {'result': 'Passed'}, 'b': {'result': 'Passed'}})
        self.assertEqual(['a'], inventory.validate_selection(['a'], {'a': {'result': 'Skipped'}}))

    def test_duplicate_xml_case_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'tests.xml'
            path.write_text('<test-run><test-case fullname="a" classname="X"/><test-case fullname="a" classname="X"/></test-run>')
            with self.assertRaisesRegex(ValueError, 'Duplicate'):
                inventory.cases(path)

    def test_discovery_is_bound_to_sources_and_xml(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(inventory, 'source_manifest', return_value={'test.cs': 'before'}):
            path = Path(temp) / 'discovery.xml'
            path.write_text('<test-discovery><test-case fullname="a" classname="X"/></test-discovery>')
            meta = dict(kind='unity-discovery', mode='PlayMode', xml_sha256=inventory.digest(path), source_files={'test.cs': 'before'})
            Path(str(path) + '.json').write_text(json.dumps(meta))
            self.assertEqual({'a': 'X'}, inventory.inventory([path]))
            with patch.object(inventory, 'source_manifest', return_value={'test.cs': 'after'}):
                with self.assertRaisesRegex(ValueError, 'changed'):
                    inventory.inventory([path])
            path.write_text('<test-discovery><test-case fullname="subset" classname="X"/></test-discovery>')
            with self.assertRaisesRegex(ValueError, 'changed'):
                inventory.inventory([path])

    def test_historical_result_is_not_current_discovery(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(inventory, 'source_manifest', return_value={}):
            path = Path(temp) / 'old.xml'
            Path(str(path) + '.json').write_text(json.dumps({'kind': 'test-result'}))
            with self.assertRaisesRegex(ValueError, 'discovery'):
                inventory.inventory([path])

    def test_different_orders_keep_exact_inventory_once(self):
        required = {f'Class{i//5}.Test{i}': f'Class{i//5}' for i in range(25)}
        first = runner.make_batches(required, 4, 'mixed', 1)
        second = runner.make_batches(required, 4, 'mixed', 2)
        self.assertNotEqual(first, second)
        for batches in [first, second]:
            flat = [case for batch in batches for case in batch]
            self.assertEqual(sorted(required), sorted(flat))
            self.assertTrue(all(len(batch) <= 4 for batch in batches))

    def test_connection_credentials_roundtrip_and_never_target_shared_database(self):
        value = 'Host=127.0.0.1;Port=55432;Database=shared_test;Username=wotr;Password="a;b""c"'
        fields = fixture.connection_fields(value)
        self.assertEqual('a;b"c', fields['password'])
        fields['database'] = 'wotr_batch_unique_test'
        self.assertEqual(fields, fixture.connection_fields(fixture.connection_string(fields)))
        self.assertEqual('postgres', fixture.postgres_env(fields)['PGDATABASE'])
        self.assertNotIn('shared_test', fixture.connection_string(fields))


class RunnerEvidenceTests(unittest.TestCase):
    def run_fake(self, temp, status='Failed', mutate=False):
        output = Path(temp) / 'result'
        def execute(command, **kwargs):
            xml = Path(kwargs['env']['WOTR_CLIENT_TEST_RESULTS'])
            xml.write_text(f'<test-run><test-case fullname="Class.Case" classname="Class" result="{status}"><failure><message>first failure</message></failure></test-case></test-run>')
            (xml.parent / 'isolation.json').write_text(json.dumps({'database': 'unique_test', 'cleanup': 'dropped'}))
            from subprocess import CompletedProcess
            return CompletedProcess(command, 0)
        before = {'source': 'before'}
        with patch.object(runner, 'inventory', return_value={'Class.Case': 'Class'}), \
             patch.object(runner, 'provenance', return_value={'source_files': before}), \
             patch.object(runner, 'digest', return_value='hash'), \
             patch.object(runner, 'source_manifest', side_effect=[before, {'source': 'after'}] if mutate else [before, before]), \
             patch.object(runner.subprocess, 'run', side_effect=execute) as run, \
             patch.object(sys, 'argv', ['runner', '--inventory', 'discovery.xml', '--output', str(output)]):
            if mutate:
                with self.assertRaisesRegex(RuntimeError, 'sources changed'):
                    runner.main()
                result = None
            else:
                result = runner.main()
            self.assertEqual(1, run.call_count)
        return result, json.loads((output / 'results.json').read_text())

    def test_first_failure_is_retained_and_never_retried_into_green(self):
        with tempfile.TemporaryDirectory() as temp:
            code, evidence = self.run_fake(temp)
            self.assertEqual(1, code)
            self.assertEqual('failed', evidence['status'])
            self.assertEqual('first failure', evidence['batches'][0]['outcomes']['Class.Case']['message'])

    def test_sources_changing_after_last_batch_invalidate_green_results(self):
        with tempfile.TemporaryDirectory() as temp:
            _, evidence = self.run_fake(temp, status='Passed', mutate=True)
            self.assertEqual('incomplete', evidence['status'])
            self.assertIn('sources changed', evidence['error'])

    def test_owned_database_is_dropped_on_test_failure(self):
        from subprocess import CompletedProcess
        with tempfile.TemporaryDirectory() as temp, \
             patch.dict(os.environ, {'WOTR_TEST_DB': 'Host=localhost;Database=shared_test;Username=wotr',
                                     'WOTR_CLIENT_TEST_RESULTS': str(Path(temp) / 'tests.xml')}, clear=True), \
             patch.object(fixture.subprocess, 'run', side_effect=lambda cmd, **kw: CompletedProcess(cmd, 7 if cmd[0] == 'bash' else 0)) as run, \
             patch.object(sys, 'argv', ['fixture']):
            self.assertEqual(7, fixture.main())
            created = run.call_args_list[0].args[0][-1]
            dropped = run.call_args_list[-1].args[0][-1]
            self.assertEqual(created, dropped)
            self.assertTrue(created.startswith('wotr_batch_') and created.endswith('_test'))
            self.assertNotEqual('shared_test', dropped)
            self.assertEqual('dropped', json.loads((Path(temp) / 'isolation.json').read_text())['cleanup'])

if __name__ == '__main__':
    unittest.main()
