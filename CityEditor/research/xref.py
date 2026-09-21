import json,struct,bisect,sys
GA='/mnt/user-data/uploads/Sailing Era/GameAssembly.dll'
b=open(GA,'rb').read()
pe=struct.unpack_from('<I',b,0x3c)[0];ns=struct.unpack_from('<H',b,pe+6)[0];opt=struct.unpack_from('<H',b,pe+20)[0]
secs=[]
for i in range(ns):
    o=pe+24+opt+i*40;name=b[o:o+8].rstrip(b'\0');vs,va,rs,ra=struct.unpack_from('<4I',b,o+8);secs.append((name,va,vs,ra,rs))
m=json.load(open('/mnt/user-data/uploads/Sailing_era_Restitutor/analysis/native-map.json'))
funcs=sorted((int(r['RVA'],16),int(r['Length'],16),r['signature']) for r in m)
starts=[f[0] for f in funcs]
def owner(rva):
    i=bisect.bisect_right(starts,rva)-1
    return funcs[i] if i>=0 and rva<funcs[i][0]+funcs[i][1] else None
def callers(target):
    out=[]
    for name,va,vs,ra,rs in secs:
        if name!=b'.text' and not name.startswith(b'il2cpp'): continue
        data=b[ra:ra+rs]
        i=data.find(b'\xe8')
        while i!=-1:
            if i+5<=len(data):
                rel=struct.unpack_from('<i',data,i+1)[0]
                if va+i+5+rel==target: out.append(va+i)
            i=data.find(b'\xe8',i+1)
    return out
if __name__=='__main__':
    q=sys.argv[1]
    for r in m:
        if q in r['signature']:
            t=int(r['RVA'],16);print('TARGET',r['signature'],hex(t))
            for c in callers(t):
                o=owner(c);print('  ',hex(c),o[2] if o else '?')
