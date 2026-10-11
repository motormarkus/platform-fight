"""Render the original FBX as a chassis and two accurately aligned wheel sprites."""
import bpy, bmesh, json, math, os, re
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/AjoPariBike'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT / 'Lahdemallit/Chopper/OldWest_Chopper.fbx'))
scene = bpy.context.scene
textures = ROOT / 'Lahdemallit/Chopper/Textures'
files = {p.name.lower(): p for p in textures.iterdir() if p.is_file()}
for image in bpy.data.images:
    if image.source != 'FILE': continue
    key = Path(image.filepath.replace('\\', '/')).name.lower()
    if key in files:
        image.filepath = str(files[key]); image.reload()
normalize = lambda s: re.sub('[^a-z0-9]', '', s.lower())
texture_files = {normalize(p.name): p for p in files.values()}
for material in bpy.data.materials:
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get('Principled BSDF')
    if shader is None: continue
    for suffix, socket in [('BaseColor','Base Color'),('Metallic','Metallic'),('Roughness','Roughness'),('Normal','Normal')]:
        path = texture_files.get(normalize(material.name+'_'+suffix+'.png'))
        if path is None:
            if suffix == 'BaseColor': raise RuntimeError('Missing material texture: '+material.name)
            continue
        node = nodes.new('ShaderNodeTexImage'); node.image = bpy.data.images.load(str(path),check_existing=True)
        if suffix != 'BaseColor': node.image.colorspace_settings.name = 'Non-Color'
        if suffix == 'Normal':
            normal = nodes.new('ShaderNodeNormalMap'); links.new(node.outputs['Color'],normal.inputs['Color'])
            links.new(normal.outputs['Normal'],shader.inputs['Normal'])
        else: links.new(node.outputs['Color'],shader.inputs[socket])

# Match the hero's existing render: remove the rear sissybar, not the original frame.
obj = bpy.data.objects['BackWheel_Fender_SissyBar']
mesh = bmesh.new(); mesh.from_mesh(obj.data)
seen, remove = set(), []
for vertex in mesh.verts:
    if vertex in seen: continue
    stack, island = [vertex], []; seen.add(vertex)
    while stack:
        v = stack.pop(); island.append(v)
        for edge in v.link_edges:
            other = edge.other_vert(v)
            if other not in seen: seen.add(other); stack.append(other)
    points = [obj.matrix_world @ v.co for v in island]
    if min(p.x for p in points) >= -1.61 and max(p.x for p in points) <= -1.40 and max(p.z for p in points) >= -0.19:
        remove.extend(island)
bmesh.ops.delete(mesh, geom=remove, context='VERTS'); mesh.to_mesh(obj.data); mesh.free()

# The FBX stores both tyres in one object and the front wheel's metalwork separately.
original_wheels = bpy.data.objects['Tires_WheelSpokes']
wheel_parts = []
for label, is_front in [('RearWheel', False), ('FrontWheel', True)]:
    wheel = original_wheels.copy(); wheel.data = original_wheels.data.copy()
    wheel.name = label; scene.collection.objects.link(wheel)
    bm = bmesh.new(); bm.from_mesh(wheel.data)
    discard = [v for v in bm.verts if ((wheel.matrix_world @ v.co).x < 0.3) == is_front]
    bmesh.ops.delete(bm, geom=discard, context='VERTS'); bm.to_mesh(wheel.data); bm.free()
    wheel_parts.append(wheel)
original_wheels.hide_render = True
front_metal = bpy.data.objects['Front Wheel_Axis_Pieces']
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and o != original_wheels]
coords = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
xmin, xmax = min(p.x for p in coords), max(p.x for p in coords)
zmin, zmax = min(p.z for p in coords), max(p.z for p in coords)
cam = bpy.data.objects.new('PairRenderCamera', bpy.data.cameras.new('PairRenderCamera'))
scene.collection.objects.link(cam); scene.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = max(xmax-xmin, (zmax-zmin)*820/388)*1.02
cam.location = ((xmin+xmax)/2, -10, (zmin+zmax)/2)
cam.rotation_euler = (math.radians(90), 0, 0)
def light(name, position, energy):
    data = bpy.data.lights.new(name, 'AREA'); data.energy = energy; data.size = 4
    ob = bpy.data.objects.new(name, data); scene.collection.objects.link(ob); ob.location = position
    ob.rotation_euler = (Vector((0.3, 0, -0.3))-ob.location).to_track_quat('-Z', 'Y').to_euler()
light('Key', (1.5,-4,4),900); light('Fill',(-3,-4,1),300); light('Rim',(0,4,3),500)
world = bpy.data.worlds.new('PairRenderWorld'); world.use_nodes = True; scene.world = world
world.node_tree.nodes['Background'].inputs[0].default_value = (.35,.35,.38,1)
world.node_tree.nodes['Background'].inputs[1].default_value = .6
scene.render.engine = 'CYCLES'; scene.cycles.samples = 24; scene.cycles.use_denoising = True
scene.render.film_transparent = True; scene.render.resolution_x = 820; scene.render.resolution_y = 388
scene.render.resolution_percentage = 100; scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
def render(label, visible):
    for ob in meshes: ob.hide_render = ob not in visible
    scene.render.filepath = str(OUT / (label + '.png'))
    bpy.ops.render.render(write_still=True)
def crop_wheel(label):
    path = OUT / (label+'.png'); image = bpy.data.images.load(str(path), check_existing=False)
    w,h = image.size; data = np.empty(w*h*4, dtype=np.float32); image.pixels.foreach_get(data)
    data = data.reshape((h,w,4)); yy,xx = np.where(data[:,:,3] > .01)
    x0,x1 = max(0,int(xx.min())-1), min(w,int(xx.max())+2)
    y0,y1 = max(0,int(yy.min())-1), min(h,int(yy.max())+2)
    cropped = bpy.data.images.new(label+'Crop',width=x1-x0,height=y1-y0,alpha=True)
    cropped.pixels.foreach_set(data[y0:y1,x0:x1,:].copy().ravel())
    old_view = scene.view_settings.view_transform
    scene.view_settings.view_transform = 'Standard'
    cropped.save_render(str(path), scene=scene)
    scene.view_settings.view_transform = old_view
    return {'position':[((x0+x1)/2-w/2)/100,((y0+y1)/2)/100], 'radius':(x1-x0)/200}
render('chassis', [o for o in meshes if o not in wheel_parts and o != front_metal])
render('rear_wheel',[wheel_parts[0]]); rear = crop_wheel('rear_wheel')
render('front_wheel',[wheel_parts[1],front_metal]); front = crop_wheel('front_wheel')
metadata = {'ppu':100,'rear':rear,'front':front,'canvas':[820,388]}
(OUT/'alignment.json').write_text(json.dumps(metadata,indent=2),encoding='utf8')
print('PAIR_BIKE_RENDERED',json.dumps(metadata))
