import struct
G='/mnt/user-data/uploads/Sailing Era/'
def key():
    b=open(G+'GameAssembly.dll','rb').read()
    pe=struct.unpack_from('<I',b,0x3c)[0];ns=struct.unpack_from('<H',b,pe+6)[0];opt=struct.unpack_from('<H',b,pe+20)[0]
    rva=0x3079160
    for i in range(ns):
        o=pe+24+opt+i*40;vs,va,rs,ra=struct.unpack_from('<4I',b,o+8)
        if va<=rva<va+max(vs,rs): off=rva-va+ra
    v=struct.unpack_from('<Q',b,off)[0]
    md=open(G+'SailingEra_Data/il2cpp_data/Metadata/global-metadata.dat','rb').read()
    o,n,d,l=struct.unpack_from('<4I',md,8)
    length,di=struct.unpack_from('<2I',md,o+((v&0x1fffffff)>>1)*8)
    return md[d+di:d+di+length]
def decrypt(path):
    buf=bytearray(open(path,'rb').read());k=key()
    for i in range(min(1024,len(buf))): buf[i]^=k[i%len(k)]
    return bytes(buf)
if __name__=='__main__':
    k=key();print(len(k),k[:40])
