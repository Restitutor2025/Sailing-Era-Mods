# Builds release/v1.0.1-base/Restitutor-Base-v1.0.1.zip from the v1.0.0 Base DLLs (hash-checked
# against release/v1.0.0-files.json) + manifest.json in each Mods subfolder + README.
# MelonLoader 0.7.2+ only scans a Mods subfolder that contains manifest.json.
import json, hashlib, zipfile, os, sys
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(root)
src = zipfile.ZipFile('release/v1.0.0/Restitutor-Base-v1.0.0.zip')
want = {d.replace('\\', '/'): h for _, d, h, *_ in json.load(open('release/v1.0.0-files.json', encoding='utf-8'))['Base']}
out = 'release/v1.0.1-base/Restitutor-Base-v1.0.1.zip'
dirs = set()
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    for i in src.infolist():
        name = i.filename.replace('\\', '/')
        data = src.read(i)
        h = hashlib.sha256(data).hexdigest().upper()
        if want.get(name) != h: sys.exit(f'hash mismatch: {name}')
        z.writestr(name, data)
        if name.startswith('Mods/'): dirs.add(os.path.dirname(name))
    if set(want) != {n.replace('\\', '/') for n in src.namelist()}: sys.exit('file list differs from v1.0.0-files.json')
    for d in sorted(dirs): z.writestr(d + '/manifest.json', '{}\n')
    z.write('release/v1.0.1-base/README_Restitutor-Base.txt', 'README_Restitutor-Base.txt')
print(out, hashlib.sha256(open(out, 'rb').read()).hexdigest().upper())
