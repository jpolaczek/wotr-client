"""Generate the figures-v3 artwork with the OpenAI Images API (ART-01, ART-02).

Usage: OPENAI_API_KEY=... python3 generate.py [key ...]
Writes raw/<key>.png (downscaled to 512 px with LANCZOS) and appends each generation to generations.jsonl
with the prompt, model, usage and the SHA-256 of the original and the stored image. The key never leaves the
environment. Without keys it generates every item that has no raw image yet.
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
STORED = 512


def prompt(spec, key):
    style, subject = spec['items'][key]
    return spec['styles'][style] + subject


def background(spec, key):
    """Figures are cut out; a style may ask for its own background (portraits are painted on one)."""
    return spec.get('backgrounds', {}).get(spec['items'][key][0], spec['background'])


def request(spec, text, key):
    body = {'model': spec['model'], 'prompt': text, 'size': spec['size'], 'quality': spec['quality'], 'n': 1,
            'background': background(spec, key), 'output_format': 'png'}
    req = urllib.request.Request('https://api.openai.com/v1/images/generations', data=json.dumps(body).encode(),
                                 headers={'Authorization': 'Bearer ' + os.environ['OPENAI_API_KEY'],
                                          'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=600) as response:
        return json.load(response)


def store(key, original):
    image = Image.open(io.BytesIO(original)).convert('RGBA').resize((STORED, STORED), Image.LANCZOS)
    path = RAW / (key + '.png')
    image.save(path, optimize=True)
    return path.read_bytes()


def generate(spec, key):
    text = prompt(spec, key)
    started = time.time()
    try:
        result = request(spec, text, key)
    except urllib.error.HTTPError as error:
        print('FAIL', key, error.code, error.read().decode()[:400])
        return False
    original = base64.b64decode(result['data'][0]['b64_json'])
    stored = store(key, original)
    entry = {'key': key, 'model': spec['model'], 'size': spec['size'], 'quality': spec['quality'],
             'background': background(spec, key), 'prompt': text, 'usage': result.get('usage', {}),
             'originalSha256': hashlib.sha256(original).hexdigest(), 'storedSha256': hashlib.sha256(stored).hexdigest(),
             'storedSize': STORED, 'at': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}
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
