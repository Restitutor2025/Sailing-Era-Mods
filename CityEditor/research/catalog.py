import json,base64,struct
C='/mnt/user-data/uploads/Sailing Era/SailingEra_Data/StreamingAssets/aa/catalog.json'
def load():
    c=json.load(open(C))
    kd=base64.b64decode(c['m_KeyDataString']);bd=base64.b64decode(c['m_BucketDataString']);ed=base64.b64decode(c['m_EntryDataString'])
    def rk(off):
        t=kd[off];off+=1
        if t in(0,1,4):
            n=struct.unpack_from('<i',kd,off)[0];s=kd[off+4:off+4+n]
            return s.decode('utf-16le' if t==1 else 'ascii','replace')
        if t==2: return struct.unpack_from('<I',kd,off)[0]
        if t==3: return struct.unpack_from('<i',kd,off)[0]
        return None
    n=struct.unpack_from('<i',bd,0)[0];p=4;buckets=[]
    for _ in range(n):
        off,cnt=struct.unpack_from('<ii',bd,p);p+=8
        ents=struct.unpack_from('<%di'%cnt,bd,p);p+=4*cnt
        buckets.append((rk(off),ents))
    en=struct.unpack_from('<i',ed,0)[0];entries=[struct.unpack_from('<7i',ed,4+i*28) for i in range(en)]
    return c,buckets,entries
if __name__=='__main__':
    c,b,e=load();ids=c['m_InternalIds']
    keys=[k for k,_ in b if isinstance(k,str)]
    print(len(keys),keys[:5])
    json.dump([(k,[ids[e[x][0]] for x in ents],[e[x][2] for x in ents]) for k,ents in b if isinstance(k,str)],open('catalog_keys.json','w'))
