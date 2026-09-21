"""Offline native arithmetic tests in this Python process; never starts/attaches to the game."""
from pathlib import Path
import hashlib, json, sys, ctypes, random
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'analysis/python_libs'))
import capstone, pefile
raw=Path('E:/Program/steam/steamapps/common/Sailing Era/GameAssembly.dll').read_bytes()
assert hashlib.sha256(raw).hexdigest()=='50d53d17829e3e77b9786ea42d998d1ad258f0653846524069f22e5e442effca'
pe=pefile.PE(data=raw,fast_load=True)
methods=json.loads((root/'analysis/native-map.json').read_text(encoding='utf-8-sig'))
md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_64)
MAX=2147483647
cases=[(b,g) for b in [-2**63,-1,0,999998,999999,1000000,MAX-1,MAX,MAX+1,2**32,2**63-1] for g in [-2**31,-1,0,1,999999,MAX]]
rng=random.Random(20260919)
cases += [(rng.randrange(0,2**63),rng.randrange(-2**31,2**31)) for _ in range(500)]
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.VirtualAlloc.argtypes=[ctypes.c_void_p,ctypes.c_size_t,ctypes.c_ulong,ctypes.c_ulong];k.VirtualAlloc.restype=ctypes.c_void_p
k.VirtualProtect.argtypes=[ctypes.c_void_p,ctypes.c_size_t,ctypes.c_ulong,ctypes.POINTER(ctypes.c_ulong)]
k.VirtualFree.argtypes=[ctypes.c_void_p,ctypes.c_size_t,ctypes.c_ulong]
k.GetCurrentProcess.restype=ctypes.c_void_p
k.FlushInstructionCache.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_size_t]
results=[]
for name,rva,size,offset,length,pre,post in [
 ('AccountRefresh',0x11AE200,0x3322,0x19A0,0x49,'55 48 83 EC 40 48 89 E5 89 55 10','8B 45 14 48 83 C4 40 5D C3'),
 ('AccountRefreshCallback',0x4438A0,0x6DA,0x2D1,0x3E,'56 48 83 EC 40 48 89 E6 48 89 4E 18 48 89 F0','8B 46 10 48 83 C4 40 5E C3')]:
 b=(root/f'TradeExp/evidence/{name}.bin').read_bytes()
 patched=(root/f'TradeExp/evidence/{name}.patched.bin').read_bytes()
 assert b==pe.get_data(rva,size) and len(patched)==size
 assert len([m for m in methods if int(m['RVA'],16)==rva])==1
 assert b[:offset]==patched[:offset] and b[offset+length:]==patched[offset+length:]
 original=list(md.disasm(b,rva)); block=list(md.disasm(patched[offset:offset+length],rva+offset))
 assert sum(i.size for i in block)==length
 assert all(i.mnemonic not in ('call','ret') for i in block)
 for i in original:
  if not rva+offset<=i.address<rva+offset+length and i.mnemonic.startswith('j') and i.op_str.startswith('0x'):
   assert not rva+offset<int(i.op_str,16)<rva+offset+length
 code=bytes.fromhex(pre)+patched[offset:offset+length]+bytes.fromhex(post)
 addr=k.VirtualAlloc(None,len(code),0x3000,0x04)
 assert addr
 try:
  ctypes.memmove(addr,code,len(code));old=ctypes.c_ulong()
  assert k.VirtualProtect(addr,len(code),0x20,ctypes.byref(old))
  assert k.FlushInstructionCache(k.GetCurrentProcess(),addr,len(code))
  fn=ctypes.CFUNCTYPE(ctypes.c_int32,ctypes.c_int64,ctypes.c_int32)(addr)
  for balance,gain in cases:
   expected=max(0,min(MAX,max(0,balance)+gain))
   actual=fn(balance,gain)
   assert actual==expected,(name,balance,gain,actual,expected)
 finally:
  assert k.VirtualFree(addr,0,0x8000)
 (root/f'TradeExp/evidence/{name}.patched.asm').write_text('\n'.join(f'{i.address:08X} {i.mnemonic} {i.op_str}' for i in block),encoding='utf-8')
 results.append(dict(method=name,patch_rva=hex(rva+offset),patch_length=length,cases=len(cases),patched_sha256=hashlib.sha256(patched).hexdigest()))
(root/'TradeExp/evidence/verification.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(f'PASS: original hashes, patch boundaries, unique RVAs, no incoming interior branches; {len(cases)*2} native arithmetic cases in isolated Python process. Game not run.')
