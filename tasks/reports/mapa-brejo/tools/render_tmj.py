"""Render a .tmj to PNG (tile layers in file order, skipping collision). usage: render.py map.tmj out.png [x0 y0 x1 y1] [--coll]"""
import json, sys, re
from pathlib import Path
from PIL import Image
FLIP_H, FLIP_V, FLIP_D = 0x80000000, 0x40000000, 0x20000000
def load(mp):
    mp=Path(mp); m=json.load(open(mp,encoding='utf-8'))
    sets=[]
    for ts in m['tilesets']:
        tsx=(mp.parent/ts['source']).resolve()
        t=tsx.read_text(encoding='utf-8')
        src=re.search(r'<image source="([^"]+)"',t).group(1)
        cols=int(re.search(r'columns="(\d+)"',t).group(1))
        cnt=int(re.search(r'tilecount="(\d+)"',t).group(1))
        try: img=Image.open((tsx.parent/src).resolve()).convert('RGBA')
        except Exception as e: img=None
        sets.append((ts['firstgid'],cols,cnt,img,tsx.name))
    sets.sort(key=lambda s:s[0])
    return m,sets
cache={}
def tile(sets,gid):
    if gid in cache: return cache[gid]
    raw=gid & ~(FLIP_H|FLIP_V|FLIP_D)
    for fg,cols,cnt,img,name in reversed(sets):
        if raw>=fg: break
    lid=raw-fg
    if img is None or name=='collision.tsx': cache[gid]=None; return None
    t=img.crop(((lid%cols)*32,(lid//cols)*32,(lid%cols)*32+32,(lid//cols)*32+32))
    if gid&FLIP_H: t=t.transpose(Image.FLIP_LEFT_RIGHT)
    if gid&FLIP_V: t=t.transpose(Image.FLIP_TOP_BOTTOM)
    cache[gid]=t; return t
def render(mp,out,box=None,coll=False,scale=1):
    m,sets=load(mp); W,H=m['width'],m['height']
    x0,y0,x1,y1=box or (0,0,W,H)
    im=Image.new('RGBA',((x1-x0)*32,(y1-y0)*32),(0,0,0,255))
    def layers(ls):
        for l in ls:
            if l['type']=='group': yield from layers(l['layers'])
            else: yield l
    for l in layers(m['layers']):
        if l['type']!='tilelayer' or not l.get('visible',True) and l['name']!='collision': continue
        if l['name']=='collision' and not coll: continue
        ox,oy=l.get('offsetx',0),l.get('offsety',0); d=l['data']
        for y in range(max(0,y0-1),min(H,y1+1)):
            for x in range(max(0,x0-1),min(W,x1+1)):
                g=d[y*W+x]
                if not g: continue
                if l['name']=='collision':
                    t=Image.new('RGBA',(32,32),(255,0,0,90))
                else:
                    t=tile(sets,g)
                if t is None: continue
                im.alpha_composite(t,(int((x-x0)*32+ox),int((y-y0)*32+oy))) if 0<=(x-x0)*32+ox<im.width and 0<=(y-y0)*32+oy<im.height else im.paste(t,(int((x-x0)*32+ox),int((y-y0)*32+oy)),t)
    if scale!=1: im=im.resize((int(im.width*scale),int(im.height*scale)),Image.LANCZOS)
    im.convert('RGB').save(out)
if __name__=='__main__':
    a=[x for x in sys.argv[1:] if not x.startswith('--')]
    sc=1
    for x in sys.argv:
        if x.startswith('--scale='): sc=float(x[8:])
    render(a[0],a[1],tuple(map(int,a[2:6])) if len(a)>=6 else None,'--coll' in sys.argv,sc)
