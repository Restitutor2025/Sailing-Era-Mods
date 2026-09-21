import json,collections,re,struct
M='/mnt/user-data/uploads/Sailing_era_Restitutor/analysis/native-map.json'
T='/mnt/user-data/uploads/Sailing_era_Restitutor/analysis/bundle-evidence/r11-table'
_by=None
def by():
    global _by
    if _by is None:
        _by=collections.defaultdict(list)
        for r in json.load(open(M)): _by[r['type']].append(r)
    return _by
def schema(name):
    fields=[]
    for r in by()['Gyyx.FbsTemplate.'+name]:
        s=r['signature'];meth=r['method']
        if meth in('get_ByteBuffer','__init','__assign') or meth.startswith('GetRootAs') or 'Bytes' in meth or meth.startswith('Create') or meth.startswith('Add') or meth.startswith('Start') or meth.startswith('End') or meth.startswith('Finish') or meth.startswith('Mutate') or meth.startswith('Sort') or meth.startswith('__lookup') or meth.startswith('LookupByKey') or meth.startswith('Get') and meth!='get_':
            continue
        ret=s.split(' ')[0]
        if meth.startswith('get_') and meth.endswith('Length'): continue
        if meth.startswith('get_'):
            fields.append((meth[4:],ret,False))
        elif '(System.Int32)' in s:
            fields.append((meth,ret,True))
    return fields
def chunks():
    b=open(T,'rb').read();p=0;ch=[]
    while p<len(b):
        n=struct.unpack_from('<I',b,p)[0];ch.append(b[p+4:p+4+n]);p+=4+n
    return ch
class FB:
    def __init__(s,b):s.b=b
    def u(s,p):return struct.unpack_from('<I',s.b,p)[0]
    def i(s,p):return struct.unpack_from('<i',s.b,p)[0]
    def h(s,p):return struct.unpack_from('<H',s.b,p)[0]
    def f(s,p,i):
        v=p-s.i(p);o=4+i*2
        return p+s.h(v+o) if o<s.h(v) and s.h(v+o) else 0
    def str_at(s,q):
        q+=s.u(q);return s.b[q+4:q+4+s.u(q)].decode('utf-8','replace')
    def scalar(s,q,t):
        fmt={'System.Int32':'<i','System.Int64':'<q','System.Single':'<f','System.Boolean':'<?','System.Int16':'<h','System.Byte':'<B','System.Double':'<d','System.UInt32':'<I'}.get(t,'<i')
        return struct.unpack_from(fmt,s.b,q)[0]
    def read(s,p,sch):
        d={}
        for i,(n,t,vec) in enumerate(sch):
            q=s.f(p,i)
            if vec:
                if not q: d[n]=[];continue
                q+=s.u(q);cnt=s.u(q);sz={'System.Int64':8,'System.Double':8}.get(t,4)
                if t=='System.String': d[n]=[s.str_at(q+4+j*4) for j in range(cnt)]
                else: d[n]=[s.scalar(q+4+j*sz,t) for j in range(cnt)]
            else:
                if not q: d[n]=None if t=='System.String' else 0;continue
                d[n]=s.str_at(q) if t=='System.String' else s.scalar(q,t)
        return d
    def rows(s):
        p=s.u(0);q=s.f(p,0);q+=s.u(q)
        return [q+4+j*4+s.u(q+4+j*4) for j in range(s.u(q))]
TABS=None
def table(name):
    global TABS
    if TABS is None:
        TABS=sorted(t[len('Gyyx.FbsTemplate.Table'):] for t in by() if t.startswith('Gyyx.FbsTemplate.Table'))
    ch=chunks()[TABS.index(name)];fb=FB(ch);sch=schema(name)
    return [fb.read(r,sch) for r in fb.rows()]
