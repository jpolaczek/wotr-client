"""Generate the v2 application icon candidates with the OpenAI Images API (UI-260).

Usage: OPENAI_API_KEY=... python3 generate.py [key ...]
Writes raw/<key>.png (the full 1024x1024 drawing on a transparent background) and appends each generation to
generations.jsonl with the prompt, model, usage and the SHA-256 of the original and the stored image. The key never
leaves the environment. Without keys it generates every item that has no raw image yet.
"""
import base64
import hashlib
import io
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
RAW = HERE / 'raw'
LOG = HERE / 'generations.jsonl'
QUALITY = 90


def request(spec, text):
    body = {'model': spec['model'], 'prompt': text, 'size': spec['size'], 'quality': spec['quality'], 'n': 1,
            'background': spec['background'], 'output_format': 'png'}
    req = urllib.request.Request('https://api.openai.com/v1/images/generations', data=json.dumps(body).encode(),
                                 headers={'Authorization': 'Bearer ' + os.environ['OPENAI_API_KEY'],
                                          'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=600) as response:
        return json.load(response)


def store(key, original):
    image = Image.open(io.BytesIO(original)).convert('RGBA')
    path = RAW / (key + '.png')
    image.save(path, optimize=True)
    return path.read_bytes()


def generate(spec, key):
    text = spec['style'] + spec['items'][key]
    started = time.time()
    try:
        result = request(spec, text)
    except urllib.error.HTTPError as error:
        print('FAIL', key, error.code, error.read().decode()[:400])
        return False
    original = base64.b64decode(result['data'][0]['b64_json'])
    stored = store(key, original)
    entry = {'key': key, 'model': spec['model'], 'size': spec['size'], 'quality': spec['quality'],
             'background': spec['background'], 'prompt': text, 'usage': result.get('usage', {}),
             'originalSha256': hashlib.sha256(original).hexdigest(), 'storedSha256': hashlib.sha256(stored).hexdigest(),
             'storedFormat': 'png', 'at': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}
    with LOG.open('a') as log:
        log.write(json.dumps(entry, ensure_ascii=False) + '\n')
    print('OK', key, round(time.time() - started, 1), 's')
    return True


def main():
    spec = json.loads((HERE / 'prompts.json').read_text())
    RAW.mkdir(exist_ok=True)
    keys = sys.argv[1:] or [key for key in spec['items'] if not (RAW / (key + '.png')).exists()]
    failed = [key for key in keys if not generate(spec, key)]
    if failed:
        sys.exit('Failed: ' + ', '.join(failed))


if __name__ == '__main__':
    main()
