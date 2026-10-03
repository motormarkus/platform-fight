import bpy, sys, math
from mathutils import Vector
OUT = sys.argv[sys.argv.index("--")+1] if "--" in sys.argv else "/tmp/r.png"
HIDE = sys.argv[sys.argv.index("--")+2].split(",") if "--" in sys.argv and len(sys.argv) > sys.argv.index("--")+2 else []
ONLY = sys.argv[sys.argv.index("--")+3].split(",") if "--" in sys.argv and len(sys.argv) > sys.argv.index("--")+3 else []
bpy.ops.wm.open_mainfile(filepath="/home/user/platform-fight/Lahdemallit/Chopper/OldWest_Chopper.blend")
sc = bpy.context.scene
import os
TEX = "/home/user/platform-fight/Lahdemallit/Chopper/Textures"
files = {f.lower(): f for f in os.listdir(TEX)}
for img in bpy.data.images:
    if img.source != "FILE": continue
    base = os.path.basename(img.filepath.replace("\\", "/")).lower()
    if base in files:
        img.filepath = os.path.join(TEX, files[base]); img.reload()
    else: print("puuttuu", img.filepath)
# sissybar pois (takalokasuojan objektissa omina kappaleinaan: tanko, kiinnikkeet ja tuet akselille)
import bmesh
o = bpy.data.objects["BackWheel_Fender_SissyBar"]
bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
mw = o.matrix_world; seen = set(); kill = []
for v in bm.verts:
    if v.index in seen: continue
    stack = [v]; isl = []; seen.add(v.index)
    while stack:
        x = stack.pop(); isl.append(x)
        for e in x.link_edges:
            y = e.other_vert(x)
            if y.index not in seen: seen.add(y.index); stack.append(y)
    pts = [mw @ x.co for x in isl]
    xmin = min(p[0] for p in pts); xmax = max(p[0] for p in pts); zmax = max(p[2] for p in pts)
    if xmin >= -1.61 and xmax <= -1.40 and zmax >= -0.19: kill += isl
bmesh.ops.delete(bm, geom=list(set(kill)), context="VERTS")
bm.to_mesh(o.data); bm.free()
# vanhat kamerat/valot pois
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
for o in bpy.data.objects:
    if o.type != "MESH": continue
    hide = bool(o.name in HIDE) or bool([x for x in ONLY if x] and o.name not in ONLY)
    o.hide_render = hide
# kamera: ortografinen, oikealta puolelta (-y), +x oikealle
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = 5.0
import os as _os
if _os.environ.get("SIDE") == "left":
    cam.location = (0.31, 10, -0.10); cam.rotation_euler = (math.radians(90), 0, math.radians(180))
else:
    cam.location = (0.31, -10, -0.10); cam.rotation_euler = (math.radians(90), 0, 0)
# valot: avain ylhäältä edestä, täyte, takavalo
def light(name, kind, loc, rot, energy, size=1.0):
    l = bpy.data.objects.new(name, bpy.data.lights.new(name, kind)); sc.collection.objects.link(l)
    l.location = loc; l.rotation_euler = [math.radians(a) for a in rot]; l.data.energy = energy
    if kind == "AREA": l.data.size = size
    return l
sy = 1 if _os.environ.get("SIDE") == "left" else -1
def aim(l, target=(0.3, 0, -0.3)):
    d = Vector(target) - l.location; l.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
aim(light("Key", "AREA", (1.5, 4 * sy, 4), (0, 0, 0), 900, 4))
aim(light("Fill", "AREA", (-3, 4 * sy, 1), (0, 0, 0), 300, 4))
aim(light("Rim", "AREA", (0, -4 * sy, 3), (0, 0, 0), 500, 4))
w = sc.world or bpy.data.worlds.new("W"); sc.world = w
w.use_nodes = True
bg = w.node_tree.nodes.get("Background")
if bg: bg.inputs[0].default_value = (0.35, 0.35, 0.38, 1); bg.inputs[1].default_value = 0.6
r = sc.render
r.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 48
try: sc.cycles.use_denoising = True
except Exception: pass
r.film_transparent = True
r.resolution_x, r.resolution_y = int(_os.environ.get("RX", 1600)), int(_os.environ.get("RX", 1600)) // 2; r.resolution_percentage = 100
r.image_settings.file_format = "PNG"; r.image_settings.color_mode = "RGBA"
r.filepath = OUT
bpy.ops.render.render(write_still=True)
print("valmis", OUT)
