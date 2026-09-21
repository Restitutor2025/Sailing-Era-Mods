import struct,io
def lz4_block(src,usize):
    dst=bytearray();i=0;n=len(src)
    while i<n:
        tok=src[i];i+=1;ll=tok>>4
        if ll==15:
            while True:
                b=src[i];i+=1;ll+=b
                if b!=255:break
        dst+=src[i:i+ll];i+=ll
        if i>=n:break
        off=src[i]|(src[i+1]<<8);i+=2;ml=tok&15
        if ml==15:
            while True:
                b=src[i];i+=1;ml+=b
                if b!=255:break
        ml+=4;s=len(dst)-off
        if off>=ml: dst+=dst[s:s+ml]
        else:
            for k in range(ml): dst.append(dst[s+k])
    return bytes(dst)
def decomp(data,flag,usize):
    c=flag&0x3f
    if c==0:return data
    if c in(2,3):return lz4_block(data,usize)
    if c==1:
        import lzma
        props=data[:5];dec=lzma.LZMADecompressor(lzma.FORMAT_RAW,filters=[{'id':lzma.FILTER_LZMA1,'dict_size':struct.unpack('<I',props[1:5])[0],'lc':props[0]%9,'lp':(props[0]//9)%5,'pb':props[0]//45}])
        return dec.decompress(data[5:],usize)
    raise Exception('comp %d'%c)
def cstr(b,p):
    e=b.index(b'\0',p);return b[p:e].decode(),e+1
def read_bundle(b):
    p=0;sig,p=cstr(b,p);ver=struct.unpack_from('>I',b,p)[0];p+=4
    _,p=cstr(b,p);_,p=cstr(b,p)
    size,csz,usz,flags=struct.unpack_from('>qIII',b,p);p+=20
    if ver>=7:p=(p+15)//16*16
    if flags&0x80: bi=b[len(b)-csz:]
    else: bi=b[p:p+csz];p+=csz
    bi=decomp(bi,flags,usz)
    q=16;bc=struct.unpack_from('>i',bi,q)[0];q+=4;blocks=[]
    for _ in range(bc):
        u,c,f=struct.unpack_from('>IIH',bi,q);q+=10;blocks.append((u,c,f))
    nc=struct.unpack_from('>i',bi,q)[0];q+=4;nodes=[]
    for _ in range(nc):
        o,s,f=struct.unpack_from('>qqI',bi,q);q+=20;path,q=cstr(bi,q);nodes.append((o,s,f,path))
    if flags&0x200: p=(p+15)//16*16
    data=bytearray()
    for u,c,f in blocks:
        data+=decomp(b[p:p+c],f,u);p+=c
    return {path:bytes(data[o:o+s]) for o,s,f,path in nodes}
class R:
    def __init__(s,b,p=0,end='<'):s.b=b;s.p=p;s.e=end
    def u(s,f):
        v=struct.unpack_from(s.e+f,s.b,s.p);s.p+=struct.calcsize(s.e+f);return v[0] if len(v)==1 else v
    def cstr(s):
        e=s.b.index(b'\0',s.p);v=s.b[s.p:e].decode('utf-8','replace');s.p=e+1;return v
    def align(s,n=4):s.p=(s.p+n-1)//n*n
COMMON=None
def read_serialized(b):
    r=R(b,0,'>');msz,fsz,ver,doff=r.u('IIII')
    if ver>=9: end=r.u('B');r.p+=3
    if ver>=22:
        msz,fsz,doff=r.u('Iqq');r.p+=8
    e='<' if end==0 else '>'
    r.e=e
    uver=r.cstr();plat=r.u('i');tt=r.u('?')
    tc=r.u('i');types=[]
    for _ in range(tc):
        cid=r.u('i');strip=r.u('?');sidx=r.u('h')
        if cid==114: r.p+=16
        r.p+=16
        nodes=[]
        if tt:
            nn,sl=r.u('ii');nb=r.b[r.p:r.p+nn*32];r.p+=nn*32;sb=r.b[r.p:r.p+sl];r.p+=sl
            def gs(o):
                if o&0x80000000: return commonstr(o&0x7fffffff)
                e2=sb.index(b'\0',o);return sb[o:e2].decode()
            for k in range(nn):
                v,lvl,arr,to,no,sz,idx,fl=struct.unpack_from(e+'hBBIIiIi',nb,k*32)
                nodes.append(dict(level=lvl,type=gs(to),name=gs(no),size=sz,flags=fl,array=arr))
            if ver>=21:
                dc=r.u('i');r.p+=4*dc
        types.append((cid,nodes))
    if 7<=ver<14: r.u('i')
    oc=r.u('i');objs=[]
    for _ in range(oc):
        r.align(4);pid=r.u('q');bs=r.u('q') if ver>=22 else r.u('I');bsz=r.u('I');ti=r.u('i')
        objs.append(dict(path_id=pid,off=doff+bs,size=bsz,cid=types[ti][0],tree=types[ti][1]))
    return objs,e
_CS=None
def commonstr(o):
    global _CS
    if _CS is None:
        import os
        _CS=open(os.path.join(os.path.dirname(__file__),'commonstrings.bin'),'rb').read()
    e=_CS.index(b'\0',o);return _CS[o:e].decode()
def parse_tree(b,nodes,p,e):
    # returns (value,newp); nodes[0] is root
    def build(i):
        n=nodes[i];ch=[];j=i+1
        while j<len(nodes) and nodes[j]['level']>n['level']:
            if nodes[j]['level']==n['level']+1: ch.append(j)
            j+=1
        return ch
    kids={i:build(i) for i in range(len(nodes))}
    prim={'SInt8':'b','UInt8':'B','char':'B','SInt16':'h','short':'h','UInt16':'H','unsigned short':'H','SInt32':'i','int':'i','UInt32':'I','unsigned int':'I','Type*':'i','SInt64':'q','long long':'q','UInt64':'Q','unsigned long long':'Q','FileSize':'Q','float':'f','double':'d','bool':'?'}
    pos=[p]
    def rd(i):
        n=nodes[i];t=n['type']
        if t=='string':
            l=struct.unpack_from(e+'i',b,pos[0])[0];pos[0]+=4;v=b[pos[0]:pos[0]+l];pos[0]+=l
            pos[0]=(pos[0]+3)//4*4;return v.decode('utf-8','replace')
        if t in prim and not kids[i]:
            f=prim[t];v=struct.unpack_from(e+f,b,pos[0])[0];pos[0]+=struct.calcsize(f)
            if n['flags']&0x4000:pos[0]=(pos[0]+3)//4*4
            return v
        if t=='TypelessData':
            l=struct.unpack_from(e+'i',b,pos[0])[0];pos[0]+=4;v=b[pos[0]:pos[0]+l];pos[0]+=l;pos[0]=(pos[0]+3)//4*4;return v
        ks=kids[i]
        if len(ks)==1 and nodes[ks[0]]['type']=='Array':
            arr=kids[ks[0]];cnt=struct.unpack_from(e+'i',b,pos[0])[0];pos[0]+=4
            et=nodes[arr[1]]
            if et['type'] in('UInt8','char') and not kids[arr[1]]:
                v=b[pos[0]:pos[0]+cnt];pos[0]+=cnt
            else: v=[rd(arr[1]) for _ in range(cnt)]
            if n['flags']&0x4000 or nodes[ks[0]]['flags']&0x4000:pos[0]=(pos[0]+3)//4*4
            return v
        d={}
        for k in ks: d[nodes[k]['name']]=rd(k)
        if n['flags']&0x4000:pos[0]=(pos[0]+3)//4*4
        return d
    return rd(0)
