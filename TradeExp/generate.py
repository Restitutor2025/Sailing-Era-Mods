"""Build the two replacement display-arithmetic blocks from audited x64 instructions."""
from pathlib import Path
root=Path(__file__).resolve().parent/'evidence'
blocks=[
 ('AccountRefresh',0x19A0,0x49,'50 31 C0 48 85 C9 48 0F 48 C8 48 63 45 10 48 01 C1 71 05 B9 FF FF FF 7F B8 FF FF FF 7F 48 39 C1 48 0F 4F C8 31 C0 48 85 C9 48 0F 48 C8 89 4D 14 58'),
 ('AccountRefreshCallback',0x2D1,0x3E,'48 8B 40 18 51 31 C9 48 85 C0 48 0F 48 C1 48 63 D2 48 01 C2 71 05 BA FF FF FF 7F B9 FF FF FF 7F 48 39 CA 48 0F 4F D1 31 C9 48 85 D2 48 0F 48 D1 89 56 10 59')]
for name,offset,size,hexcode in blocks:
 b=bytearray((root/(name+'.bin')).read_bytes());code=bytes.fromhex(hexcode)
 assert len(code)<=size
 b[offset:offset+size]=code+b'\x90'*(size-len(code))
 (root/(name+'.patched.bin')).write_bytes(b)
