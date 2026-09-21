"""Offline verification only: no game process or original saves accessed."""
from pathlib import Path
import json, sys, hashlib
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'analysis/python_libs'))
import capstone, pefile

baseline = ROOT/'FleetInfo/evidence/GetNpcsBySeaAreaId.bin'
b = baseline.read_bytes()
game = Path('E:/Program/steam/steamapps/common/Sailing Era/GameAssembly.dll').read_bytes()
assert hashlib.sha256(game).hexdigest().upper() == '50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA'
pe = pefile.PE(data=game, fast_load=True)
assert pe.get_data(0xB08DC0, 0x962) == b
patched = bytearray(b); patched[0x3B3] = 0
assert [i for i in range(len(b)) if b[i] != patched[i]] == [0x3B3]
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
before = list(md.disasm(b, 0xB08DC0)); after = list(md.disasm(patched, 0xB08DC0))
assert len(before) == len(after)
diff = [(a.address, a.mnemonic, a.op_str, z.op_str) for a,z in zip(before,after) if a.bytes != z.bytes]
assert diff == [(0xB09171, 'mov', 'r9b, 2', 'r9b, 0')]
methods = json.loads((ROOT/'analysis/native-map.json').read_text(encoding='utf-8-sig'))
aliases = [m for m in methods if int(m['RVA'],16) == 0xB08DC0]
assert len(aliases) == 1 and aliases[0]['method'] == 'GetNpcsBySeaAreaId'
cases = []
for p in sorted((ROOT/'analysis/bar-fleet-investigation/save-snapshot').glob('*.teams.json')):
    rows = json.loads(p.read_text()); lookup = {}; skipped = 0
    for row in rows:
        if row['owner'] in lookup: skipped += 1
        else: lookup[row['owner']] = row
    for owner, row in lookup.items():
        assert row == next(r for r in rows if r['owner'] == owner)
    if not skipped: assert list(lookup.values()) == rows
    if p.name == '3.teams.json':
        assert len(rows) == 74 and len(lookup) == 71 and skipped == 3
        assert lookup[1110201]['guid'] == 1550029328656367616
    cases.append({'file':p.name, 'teams_unchanged':len(rows), 'lookup_owners':len(lookup), 'duplicate_insertions_skipped':skipped})
assert len(cases) == 19
result = {'instruction_diff':diff, 'method_alias_count':len(aliases), 'save_cases':cases,
          'scope':'Static disassembly and offline insertion-policy model; not native runtime or gameplay validation.'}
(ROOT/'FleetInfo/evidence/verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('PASS: baseline, single instruction/byte change, unique method RVA, 19 saved fleet lists. Gameplay not run.')
