"""Collect both platform builds into one complete release; does not run binaries."""
from pathlib import Path
import argparse, hashlib, json, shutil

parser=argparse.ArgumentParser(); parser.add_argument('--input',type=Path,required=True); parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args(); args.output.mkdir(parents=True,exist_ok=True)
names=['Ferrum-win-x64.zip','Ferrum-linux-x64.tar.gz','Ferrum.Installer-Win.exe','Ferrum.Installer-Linux']
for name in names:
    matches=list(args.input.rglob(name)); assert len(matches)==1, (name, matches)
    shutil.copy2(matches[0],args.output/name)
manifest=json.loads((Path(__file__).resolve().parents[1]/'release.json').read_text())
manifest['assets']=[{'name':name,'sha256':hashlib.sha256((args.output/name).read_bytes()).hexdigest()} for name in names]
(args.output/'release-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
with (args.output/'SHA256SUMS.txt').open('w',encoding='utf-8',newline='\n') as sums:
    for name in names+['release-manifest.json']:
        sums.write(hashlib.sha256((args.output/name).read_bytes()).hexdigest()+'  '+name+'\n')
assert len(list(args.output.iterdir()))==6
print('Complete release set assembled with SHA-256 checksums.')
