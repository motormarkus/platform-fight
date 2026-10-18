"""Extract the supplied PokerSet geometry; never generate or repaint card artwork.
Run from project root. Source stays untouched. Requires the already available numpy.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil
import uuid
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--source', default='C:/Users/marku/Downloads/PokerSet/PokerSet')
args = parser.parse_args()
source = Path(args.source)
output = Path('Assets/ArtSources/Poker/ImportedSet')
models = output / 'Models'
textures = output / 'Textures'
for folder in [models, textures / 'Faces']:
    folder.mkdir(parents=True, exist_ok=True)

denominations = [1, 2, 5, 10, 20, 25, 50]
body_names = {str(n) + 'Chip' for n in denominations}
blocks = []
current = None
global_counts = [0, 0, 0]
with (source / 'PokerSet.obj').open(encoding='utf-8') as stream:
    for line in stream:
        tag = line.split(' ', 1)[0].strip()
        if tag == 'o':
            name = line[2:].strip()
            keep = name.split('_')[0] in body_names or name.startswith('Text') or name.startswith('2Clubs_')
            current = {'name': name, 'base': global_counts.copy(), 'v': [], 'vt': [], 'vn': [], 'faces': [], 'mat': ''} if keep else None
            if current is not None:
                blocks.append(current)
        elif tag in ['v', 'vt', 'vn']:
            index = ['v', 'vt', 'vn'].index(tag)
            global_counts[index] += 1
            if current is not None:
                current[tag].append(tuple(map(float, line.split()[1:])))
        elif current is not None and tag == 'usemtl':
            current['mat'] = line[7:].strip()
        elif current is not None and tag == 'f':
            indices = []
            for term in line.split()[1:]:
                parts = term.split('/')
                indices.append(tuple(int(parts[i]) - current['base'][i] - 1 if i < len(parts) and parts[i] else -1 for i in range(3)))
            current['faces'].append((current['mat'], indices))

material_colors = {}
mat = ''
for line in (source / 'PokerSet.mtl').read_text().splitlines():
    if line.startswith('newmtl '):
        mat = line[7:].strip()
    elif line.startswith('Kd '):
        material_colors[mat] = list(map(float, line.split()[1:4]))

manifest = {'source': 'PokerSet.obj', 'sourceBytes': (source / 'PokerSet.obj').stat().st_size,
            'sourceSha256': hashlib.sha256((source / 'PokerSet.obj').read_bytes()).hexdigest(),
            'cardOrder': 'clubs, diamonds, hearts, spades; 2..10,J,Q,K,A', 'models': {}, 'materials': material_colors}

def export_mesh(path, parts, center, scale, card=False, only_material=None):
    verts, uvs, normals, groups = [], [], [], {}
    cache = {}
    for part in parts:
        v = np.array(part['v'], dtype=np.float64)
        n = np.array(part['vn'], dtype=np.float64)
        uv = np.array(part['vt'], dtype=np.float64)
        faces = [(m, f) for m, f in part['faces'] if only_material is None or m == only_material]
        if not faces:
            continue
        uv_min, uv_extent = np.zeros(2), np.ones(2)
        if card:
            used_uv = np.array([uv[t[1]][:2] for _, face in faces for t in face])
            uv_min = used_uv.min(axis=0)
            uv_extent = np.maximum(used_uv.max(axis=0) - uv_min, 1e-8)
        # Merge dense chip bevel vertices into cells 2% of a chip diameter, per material.
        # Text and cards retain their source geometry. Material boundaries are preserved.
        reduce = 'Chip_' in part['name']
        for material, face in faces:
            triangle_ids = []
            for vi, ti, ni in face:
                position = (v[vi] - center) * scale
                normal = n[ni] if ni >= 0 else np.array([0., 1., 0.])
                tex = uv[ti][:2] if ti >= 0 else np.zeros(2)
                if card:
                    # Observed CardFront normal is -Y; rotate -90deg around X.
                    position = np.array([position[0], -position[2], position[1]])
                    normal = np.array([normal[0], -normal[2], normal[1]])
                    tex = (tex - uv_min) / uv_extent
                else:
                    position = np.array([position[0], position[2], -position[1]])
                    normal = np.array([normal[0], normal[2], -normal[1]])
                if reduce:
                    key = (material, *np.rint(position / .020).astype(int))
                else:
                    key = (part['name'], material, vi, ti, ni)
                if key not in cache:
                    cache[key] = len(verts)
                    verts.append(position)
                    uvs.append(tex)
                    normals.append(normal)
                triangle_ids.append(cache[key])
            for k in range(1, len(triangle_ids) - 1):
                tri = (triangle_ids[0], triangle_ids[k], triangle_ids[k + 1])
                if len(set(tri)) != 3:
                    continue
                a, b, c = (verts[i] for i in tri)
                if np.linalg.norm(np.cross(b - a, c - a)) < 1e-10:
                    continue
                groups.setdefault(material, set()).add(tri)
    with path.open('w', encoding='utf-8', newline='\n') as f:
        f.write('# Derived from user supplied PokerSet.obj; source untouched.\nmtllib PokerSet.mtl\no ' + path.stem + '\n')
        for v in verts:
            f.write('v ' + ' '.join(f'{x:.7f}' for x in v) + '\n')
        for uv in uvs:
            f.write('vt ' + ' '.join(f'{x:.7f}' for x in uv) + '\n')
        for n in normals:
            f.write('vn ' + ' '.join(f'{x:.7f}' for x in n) + '\n')
        for mat, tris in groups.items():
            f.write('usemtl ' + mat + '\ns 1\n')
            for tri in sorted(tris):
                f.write('f ' + ' '.join(f'{i+1}/{i+1}/{i+1}' for i in tri) + '\n')
    manifest['models'][path.name] = {'vertices': len(verts), 'triangles': sum(map(len, groups.values())), 'materials': list(groups)}

card = next(b for b in blocks if b['name'].startswith('2Clubs_'))
v = np.array(card['v'])
center = (v.min(axis=0) + v.max(axis=0)) * .5
scale = 1 / (v[:, 0].max() - v[:, 0].min())
for material, filename in [('CardFront', 'CardFace.obj'), ('CardBackBlue', 'CardBack.obj'), ('CardSides', 'CardEdges.obj')]:
    export_mesh(models / filename, [card], center, scale, card=True, only_material=material)

for value in denominations:
    body = next(b for b in blocks if b['name'].startswith(str(value) + 'Chip_'))
    v = np.array(body['v'])
    center = (v.min(axis=0) + v.max(axis=0)) * .5
    scale = 1 / (v[:, 0].max() - v[:, 0].min())
    labels = [b for b in blocks if b['name'].startswith('Text') and abs(np.array(b['v'])[:, 0].mean() - center[0]) < .09
              and abs(np.array(b['v'])[:, 2].mean() - center[2]) < .09 and abs(np.array(b['v'])[:, 1].mean() - center[1]) < .04]
    export_mesh(models / f'Chip-{value}.obj', [body] + labels, center, scale)

with (models / 'PokerSet.mtl').open('w', encoding='utf-8') as f:
    for name, color in material_colors.items():
        f.write('newmtl ' + name + '\nKd ' + ' '.join(map(str, color)) + '\n\n')

for suit in ['clubs', 'diamonds', 'hearts', 'spades']:
    for rank in list(map(str, range(2, 11))) + ['jack', 'queen', 'king', 'ace']:
        name = f'{rank}_of_{suit}.png'
        shutil.copy2(source / 'Textures/PNG-cards-1.3' / name, textures / 'Faces' / name)
for name in ['Back1.png', 'Back2.png', 'Cards.png']:
    shutil.copy2(source / 'Textures' / name, textures / name)
source_archive = Path('Lahdemallit/PokerSet')
source_archive.mkdir(parents=True, exist_ok=True)
shutil.copy2(source / 'PokerSet.blend', source_archive / 'PokerSet.blend')
(output / 'import-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
for p in [output] + list(output.rglob('*')):
    if p.name.endswith('.meta'):
        continue
    meta = Path(str(p) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' + ('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n' if p.is_dir() else ''), encoding='utf-8')
print(json.dumps(manifest['models'], indent=2))
print('Copied 52 existing card faces, backs, source atlas and archived original blend.')
