#!/usr/bin/env python3
"""Check the complete expansion decision inventory against executed Unity tests."""
import argparse
import json
from pathlib import Path
import re
import tarfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
PLAY = ROOT / "unity/Wotr.Client/Assets/Wotr/Tests/PlayMode"
# Protocol boundary accepted in T23, before expansion decision windows were added.
BASE = set("""FellowshipPhase AllocateHunt Action DiscardCards Reaction CombatCard
Losses HuntDamage RemoveExcess EffectChoice FellowshipRoute Guide CompanionSeparation
HuntReroll BattleReroll BattleContinue BattleRetreat BattleAdvance StrongholdDefense
SiegeContinuation ArmyEvent CharacterEvent""".split())
WINDOWS = {
    "CouncilVersions": "ExpansionDecisionNetworkTests.CouncilVersionsHomelandsAndShadowTokenSurviveReconnect",
    "CouncilHomelands": "ExpansionDecisionNetworkTests.CouncilVersionsHomelandsAndShadowTokenSurviveReconnect",
    "CouncilToken": "ExpansionDecisionNetworkTests.CouncilVersionsHomelandsAndShadowTokenSurviveReconnect",
    "SpecialDieChoice": "ExpansionDecisionNetworkTests.KeeperAndLesserChoicesPrecedeBothRulerDiscards",
    "KeepActionDie": "ExpansionDecisionNetworkTests.VilyaKeepsTheResolvingDieAvailable",
    "RulerDiscard": "ExpansionDecisionNetworkTests.KeeperAndLesserChoicesPrecedeBothRulerDiscards",
    "Dismay": "ExpansionDecisionNetworkTests.DismayConsumesOneTokenAndRerollsTheSelectedRuler",
    "CorruptionTile": "ExpansionCorruptionNetworkTests.HiddenCorruptionStaysFaceDownForFreePeoples",
}
CONTINUATIONS = {"ActionSequence": "ExpansionAbilityNetworkTests.AllCompanionsAndSovereignsCanPlanSeparateGroups"}
# The shared Action/Reaction/SiegeContinuation windows and every variant also
# require their network cases, not just the eight new enum values above.
CLASSES = (
    "ExpansionDecisionNetworkTests", "ExpansionCorruptionNetworkTests",
    "ExpansionTokenNetworkTests", "ExpansionAbilityNetworkTests",
    "ExpansionWindowNetworkTests", "ExpansionLobbyNetworkTests", "ExpansionCostNetworkTests",
    "ConfigurationLayoutTests", "ExpansionMatrixLayoutTests",
)


def pinned_commands():
    """GameCommands.cs from the Contracts archive pinned in wotr-deps.lock.json (run bootstrap_deps.py first)."""
    lock = json.loads((ROOT / "wotr-deps.lock.json").read_text())
    contracts = next(p for p in lock["packages"] if p["name"] == "com.wotr.contracts")
    with tarfile.open(ROOT / ".deps" / contracts["archive"], "r:gz") as archive:
        return archive.extractfile("package/Runtime/GameCommands.cs").read().decode()


def requirements():
    protocol = pinned_commands()
    body = re.search(r"enum DecisionKind\s*\{([^}]+)\}", protocol).group(1)
    kinds = set(re.findall(r"\b[A-Za-z]+\b", body))
    assert not BASE.intersection(WINDOWS), "An expansion decision was classified as base."
    assert kinds == BASE.union(WINDOWS, CONTINUATIONS), f"Unreviewed protocol windows: {kinds ^ BASE.union(WINDOWS, CONTINUATIONS)}"
    required = set()
    for name in CLASSES:
        source = (PLAY / (name + ".cs")).read_text()
        methods = re.findall(r"\[UnityTest\]\s*public IEnumerator (\w+)\(", source)
        assert methods, f"No Unity scenarios found in {name}"
        required.update(name + "." + method for method in methods)
    assert set(CONTINUATIONS.values()).issubset(required), "A continuation refers to a removed scenario."
    assert set(WINDOWS.values()).issubset(required), "A window refers to a removed scenario."
    return required


def verify_results(paths, required):
    outcomes = {}
    for path in paths:
        for case in ET.parse(path).getroot().iter("test-case"):
            name = case.get("fullname", "")
            short = ".".join(name.split(".")[-2:])
            if short in required:
                outcomes[short] = case.get("result")
    missing = sorted(required - outcomes.keys())
    failed = sorted(name for name, result in outcomes.items() if result != "Passed")
    assert not missing, f"Unexecuted UI acceptance scenarios: {missing}"
    assert not failed, f"Failed or skipped UI acceptance scenarios: {failed}"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--results", type=Path, nargs="+", help="NUnit XML reports, oldest to newest; later outcomes supersede earlier ones")
    args = parser.parse_args()
    required = requirements()
    if args.results:
        verify_results(args.results, required)
    suffix = "executed successfully" if args.results else "linked (execution not checked)"
    print(f"PASS: {len(WINDOWS)} expansion windows, {len(BASE)} inherited windows, {len(CONTINUATIONS)} saved continuation, {len(required)} Unity scenarios {suffix}")


if __name__ == "__main__":
    main()
