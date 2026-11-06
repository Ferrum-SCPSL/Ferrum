"""Build deterministic runtime assets and inspect their contents without executing Ferrum."""
import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
import re
import shutil
import struct
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
META = json.loads((ROOT / 'release.json').read_text(encoding='utf-8'))
assert re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?', META['version'])
assert META['branch'] in ('alpha', 'beta', 'stable') and META['abi'] == 2

def digest(data):
    return hashlib.sha256(data).hexdigest()

def inspect_pe(data):
    assert data[:2] == b'MZ', 'Missing PE header'
    pe = struct.unpack_from('<I', data, 0x3C)[0]
    assert data[pe:pe+4] == b'PE\0\0', 'Invalid PE signature'
    assert struct.unpack_from('<H', data, pe+4)[0] == 0x8664, 'Expected AMD64'

def inspect_native(data, platform):
    if platform == 'win-x64':
        inspect_pe(data)
    else:
        assert data[:6] == b'\x7fELF\x02\x01', 'Expected ELF64 little endian'
        assert struct.unpack_from('<H', data, 18)[0] == 62, 'Expected x86-64 ELF'

def catalogs():
    result = {}
    english = None
    for language in ('en', 'ru', 'uk', 'zh-CN'):
        path = ROOT / 'Ferrum.Localization/Catalogs' / (language + '.csv')
        data = path.read_bytes()
        rows = list(csv.reader(io.StringIO(data.decode('utf-8'), newline='')))
        assert rows[0] == ['key', 'value']
        assert all(len(row) == 2 for row in rows[1:])
        values = dict(rows[1:]); assert len(values) == len(rows)-1
        if english is None:
            english = values
        assert values.keys() == english.keys(), f'Catalog keys differ: {language}'
        for key, value in values.items():
            assert sorted(re.findall(r'\{[0-9]+\}', value)) == sorted(re.findall(r'\{[0-9]+\}', english[key])), f'Placeholder mismatch: {language}/{key}'
        result['localizations/' + path.name] = data
    return result

def package(configuration, platform):
    files = catalogs()
    for name in ('Ferrum.Loader', 'Ferrum.Contracts', 'Ferrum.Runtime', 'Ferrum.Interop', 'Ferrum.Localization', 'Ferrum.Updater'):
        data = (ROOT / 'artifacts/managed' / name / configuration / (name + '.dll')).read_bytes()
        inspect_pe(data)
        category = 'bootstrap' if name in ('Ferrum.Loader', 'Ferrum.Contracts') else 'runtime'
        files[category + '/' + name + '.dll'] = data
    native = 'ferrum_native.dll' if platform == 'win-x64' else 'libferrum_native.so'
    target = 'x86_64-pc-windows-msvc' if platform == 'win-x64' else 'x86_64-unknown-linux-gnu'
    data = (ROOT / 'artifacts/cargo/production' / target / configuration.lower() / native).read_bytes()
    inspect_native(data, platform)
    files['runtime/' + native] = data
    manifest = {key: META[key] for key in ('version', 'branch', 'bootstrap', 'contracts', 'abi')}
    manifest['platform'] = platform
    manifest['files'] = [{'path': path, 'sha256': digest(data)} for path, data in sorted(files.items())]
    files['manifest.json'] = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    output = ROOT / 'artifacts/package' / platform / configuration
    output.mkdir(parents=True, exist_ok=True)
    name = 'Ferrum-win-x64.zip' if platform == 'win-x64' else 'Ferrum-linux-x64.tar.gz'
    destination = output / name
    if platform == 'win-x64':
        with zipfile.ZipFile(destination, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
            for path, data in sorted(files.items()):
                entry = zipfile.ZipInfo(path, (2026, 1, 1, 0, 0, 0)); entry.external_attr = 0o100644 << 16
                archive.writestr(entry, data, compress_type=zipfile.ZIP_DEFLATED)
        with zipfile.ZipFile(destination) as archive:
            assert set(archive.namelist()) == set(files)
            for path, data in files.items(): assert archive.read(path) == data
    else:
        with tarfile.open(destination, 'w:gz', format=tarfile.USTAR_FORMAT) as archive:
            for path, data in sorted(files.items()):
                entry = tarfile.TarInfo(path); entry.size = len(data); entry.mode = 0o644; entry.mtime = 0
                archive.addfile(entry, io.BytesIO(data))
        with tarfile.open(destination, 'r:gz') as archive:
            assert set(archive.getnames()) == set(files)
            for path, data in files.items(): assert archive.extractfile(path).read() == data
    (output / 'manifest.json').write_bytes(files['manifest.json'])
    (output / 'SHA256SUMS.txt').write_text(digest(destination.read_bytes()) + '  ' + name + '\n', encoding='utf-8')
    print(f'Packaged {name}: {len(files)} files, {destination.stat().st_size} bytes; static integrity/CSV checks passed.')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--configuration', choices=['Debug', 'Release'], default='Release')
    parser.add_argument('--platform', choices=['win-x64', 'linux-x64'], required=True)
    args = parser.parse_args()
    package(args.configuration, args.platform)
