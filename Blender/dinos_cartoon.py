# Modelos cartoon de dinosaurios y fósiles para el Museo Autista.
# Ejecutar:  blender --background --python dinos_cartoon.py -- <carpeta_salida>
# Genera un FBX por modelo. Convención: Z arriba, el animal mira hacia +Y.
# Jerarquía para animar en Unity: DinoCuerpo > Cabeza > Mandibula, DinoCuerpo > Cola.
# Fósiles: Base + Huesos > (cada hueso separado, para la animación de "desarmar").

import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector, Euler

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.dirname(__file__)
os.makedirs(OUT, exist_ok=True)

MATS = {}


def mat(name, rgb):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.7
    MATS[name] = m
    return m


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.curves):
        for b in list(block):
            block.remove(b)


def skin(name, points, edges, material, origin=(0, 0, 0), parent=None, subdiv=2):
    """Malla orgánica con Skin modifier. points: [(pos, (rx, ry))]; origen en 'origin'."""
    o = Vector(origin)
    me = bpy.data.meshes.new(name)
    me.from_pydata([Vector(p) - o for p, _ in points], edges, [])
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    obj.location = o
    mod = obj.modifiers.new("Skin", "SKIN")
    mod.use_smooth_shade = True
    sv = me.skin_vertices[0].data
    for i, (_, r) in enumerate(points):
        r = r if isinstance(r, tuple) else (r, r)
        sv[i].radius = r
    sv[0].use_root = True
    sub = obj.modifiers.new("Subdiv", "SUBSURF")
    sub.levels = subdiv
    sub.render_levels = subdiv
    me.materials.append(material)
    if parent:
        set_parent(obj, parent)
    return obj


def chain(pts):
    return [(i, i + 1) for i in range(len(pts) - 1)]


def set_parent(child, parent):
    bpy.context.view_layer.update()
    mw = child.matrix_world.copy()
    child.parent = parent
    child.matrix_world = mw


def sphere(name, loc, scale, material, parent=None, rot=(0, 0, 0), segs=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=segs // 2, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    obj.data.materials.append(material)
    if parent:
        set_parent(obj, parent)
    return obj


def cone(name, base, tip, radius, material, parent=None):
    base, tip = Vector(base), Vector(tip)
    d = tip - base
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=radius, radius2=0.0, depth=d.length, location=(base + tip) / 2)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    obj.data.materials.append(material)
    if parent:
        set_parent(obj, parent)
    return obj


def empty(name, loc, parent=None):
    e = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(e)
    e.location = loc
    if parent:
        set_parent(e, parent)
    return e


def eyes(head, pos, size, spread_dir=1.0):
    white, black = mat("OjoBlanco", (0.97, 0.96, 0.93)), mat("Pupila", (0.05, 0.05, 0.07))
    for s in (-1, 1):
        p = Vector((s * pos[0], pos[1], pos[2]))
        sphere("Ojo", p, (size, size, size), white, head)
        sphere("Pupila", p + Vector((s * size * 0.35, size * 0.55, size * 0.1)), (size * 0.5,) * 3, black, head)
        sphere("Brillo", p + Vector((s * size * 0.25, size * 0.9, size * 0.4)), (size * 0.14,) * 3, white, head)


def export(name):
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=False, axis_forward="-Z", axis_up="Y", use_mesh_modifiers=True,
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False, object_types={"EMPTY", "MESH"})
    print("Exportado", path)


# ====================================================================== DINOSAURIOS

def legs(points, edges, attach_idx, hip, knee, ankle, toe, r):
    """Añade un par de patas al esqueleto skin (simétricas)."""
    for s in (-1, 1):
        base = len(points)
        pts = [((s * hip[0], hip[1], hip[2]), r[0]), ((s * knee[0], knee[1], knee[2]), r[1]),
               ((s * ankle[0], ankle[1], ankle[2]), r[2]), ((s * toe[0], toe[1], toe[2]), r[3])]
        points += pts
        edges += [(attach_idx, base), (base, base + 1), (base + 1, base + 2), (base + 2, base + 3)]


def trex():
    clear()
    skinC, belly = mat("TRex_Piel", (0.56, 0.78, 0.42)), mat("TRex_Panza", (0.96, 0.88, 0.62))
    stripes, white = mat("TRex_Rayas", (0.40, 0.62, 0.34)), mat("Diente", (0.98, 0.97, 0.92))
    pts = [((0, -0.35, 1.35), (0.55, 0.6)), ((0, 0.25, 1.55), (0.62, 0.66)), ((0, 0.7, 1.85), (0.38, 0.4)), ((0, 0.9, 2.0), (0.32, 0.32))]
    ed = chain(pts)
    legs(pts, ed, 0, (0.4, -0.3, 1.1), (0.45, -0.05, 0.62), (0.45, -0.3, 0.22), (0.45, 0.05, 0.08), [(0.36, 0.42), (0.2, 0.2), (0.12, 0.12), (0.16, 0.09)])
    for s in (-1, 1):
        b = len(pts)
        pts += [((s * 0.32, 0.65, 1.45), 0.08), ((s * 0.38, 0.85, 1.3), 0.06)]
        ed += [(1, b), (b, b + 1)]
    body = skin("DinoCuerpo", pts, ed, skinC)
    sphere("Panza", (0, 0.35, 1.3), (0.52, 0.6, 0.55), belly, body)
    for i in range(4):
        cone("Escama", (0, 0.5 - i * 0.35, 2.02 - i * 0.12), (0, 0.45 - i * 0.35, 2.22 - i * 0.12), 0.09, stripes, body)

    tail_pts = [((0, -0.5, 1.4), (0.5, 0.52)), ((0, -1.2, 1.25), (0.33, 0.34)), ((0, -1.85, 1.05), (0.18, 0.18)), ((0, -2.4, 0.9), (0.06, 0.06))]
    tail = skin("Cola", tail_pts, chain(tail_pts), skinC, origin=(0, -0.5, 1.4), parent=body)
    for i in range(3):
        cone("EscamaCola", (0, -0.8 - i * 0.5, 1.72 - i * 0.18), (0, -0.85 - i * 0.5, 1.88 - i * 0.18), 0.07, stripes, tail)

    head_o = (0, 0.95, 2.05)
    hp = [((0, 0.9, 2.1), (0.36, 0.38)), ((0, 1.35, 2.18), (0.38, 0.36)), ((0, 1.8, 2.08), (0.3, 0.24))]
    head = skin("Cabeza", hp, chain(hp), skinC, origin=head_o, parent=body)
    eyes(head, (0.24, 1.3, 2.42), 0.15)
    for s in (-1, 1):
        sphere("Nariz", (s * 0.1, 1.95, 2.2), (0.035,) * 3, mat("Pupila", (0.05, 0.05, 0.07)), head)
        for i in range(4):
            cone("Diente", (s * 0.22, 1.35 + i * 0.14, 1.9), (s * 0.22, 1.36 + i * 0.14, 1.78), 0.035, white, head)
    jp = [((0, 1.05, 1.86), (0.24, 0.18)), ((0, 1.75, 1.8), (0.26, 0.12))]
    skin("Mandibula", jp, chain(jp), belly, origin=(0, 1.05, 1.9), parent=head)
    export("TRex_Cartoon")


def triceratops():
    clear()
    skinC, frill, horn = mat("Tri_Piel", (0.96, 0.70, 0.46)), mat("Tri_Gola", (0.88, 0.50, 0.58)), mat("Cuerno", (0.98, 0.95, 0.85))
    pts = [((0, -0.75, 1.05), (0.62, 0.64)), ((0, 0.0, 1.15), (0.74, 0.72)), ((0, 0.7, 1.05), (0.6, 0.6))]
    ed = chain(pts)
    legs(pts, ed, 0, (0.46, -0.7, 0.8), (0.5, -0.65, 0.42), (0.5, -0.65, 0.12), (0.5, -0.5, 0.05), [(0.26, 0.26), (0.18, 0.18), (0.17, 0.17), (0.2, 0.1)])
    legs(pts, ed, 2, (0.46, 0.65, 0.75), (0.5, 0.68, 0.4), (0.5, 0.68, 0.12), (0.5, 0.82, 0.05), [(0.24, 0.24), (0.17, 0.17), (0.16, 0.16), (0.19, 0.1)])
    body = skin("DinoCuerpo", pts, ed, skinC)
    for i in range(5):
        sphere("Mancha", (0, 0.5 - i * 0.3, 1.82 - abs(i - 2) * 0.05), (0.12, 0.14, 0.06), frill, body)

    tp = [((0, -1.1, 1.05), (0.38, 0.38)), ((0, -1.8, 0.75), (0.2, 0.2)), ((0, -2.3, 0.55), (0.05, 0.05))]
    skin("Cola", tp, chain(tp), skinC, origin=(0, -1.1, 1.05), parent=body)

    hp = [((0, 1.0, 1.05), (0.42, 0.44)), ((0, 1.5, 0.92), (0.34, 0.36)), ((0, 1.85, 0.8), (0.15, 0.2))]
    head = skin("Cabeza", hp, chain(hp), skinC, origin=(0, 0.95, 1.05), parent=body)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.72, depth=0.08, location=(0, 0.9, 1.5), rotation=(math.radians(-60), 0, 0))
    disc = bpy.context.active_object
    disc.name = "Gola"
    bpy.ops.object.shade_smooth()
    disc.data.materials.append(frill)
    set_parent(disc, head)
    for i in range(9):
        a = math.radians(-100 + i * 25)
        p = Vector((math.sin(a) * 0.72, 0, math.cos(a) * 0.72))
        p.rotate(Euler((math.radians(-60), 0, 0)))
        sphere("Borde", Vector((0, 0.9, 1.5)) + p, (0.1,) * 3, skinC, head)
    for s in (-1, 1):
        cone("Cuerno", (s * 0.2, 1.3, 1.3), (s * 0.3, 1.9, 1.75), 0.08, horn, head)
    cone("CuernoNariz", (0, 1.75, 1.0), (0, 1.9, 1.22), 0.06, horn, head)
    eyes(head, (0.26, 1.35, 1.18), 0.12)
    jp = [((0, 1.3, 0.72), (0.2, 0.14)), ((0, 1.8, 0.66), (0.13, 0.09))]
    skin("Mandibula", jp, chain(jp), skinC, origin=(0, 1.3, 0.75), parent=head)
    export("Triceratops_Cartoon")


def stegosaurus():
    clear()
    skinC, plate, white = mat("Stego_Piel", (0.55, 0.72, 0.90)), mat("Stego_Placas", (0.97, 0.62, 0.44)), mat("Cuerno", (0.98, 0.95, 0.85))
    pts = [((0, -0.85, 1.0), (0.5, 0.52)), ((0, -0.1, 1.35), (0.66, 0.68)), ((0, 0.6, 0.98), (0.5, 0.5)), ((0, 0.95, 0.8), (0.28, 0.28))]
    ed = chain(pts)
    legs(pts, ed, 0, (0.4, -0.8, 0.85), (0.44, -0.75, 0.45), (0.44, -0.75, 0.12), (0.44, -0.6, 0.05), [(0.28, 0.28), (0.18, 0.18), (0.16, 0.16), (0.18, 0.1)])
    legs(pts, ed, 2, (0.4, 0.55, 0.7), (0.42, 0.58, 0.38), (0.42, 0.58, 0.12), (0.42, 0.7, 0.05), [(0.22, 0.22), (0.15, 0.15), (0.14, 0.14), (0.16, 0.09)])
    body = skin("DinoCuerpo", pts, ed, skinC)
    for i in range(7):
        y = 0.75 - i * 0.28
        h = 0.3 + math.sin((i + 1) / 8 * math.pi) * 0.25
        z = 1.35 + math.sin((i + 1) / 8 * math.pi) * 0.55
        sphere("Placa", ((0.07 if i % 2 else -0.07), y, z + h * 0.35), (0.05, h * 0.55, h * 0.75), plate, body, rot=(math.radians(-15), 0, 0))

    tp = [((0, -1.2, 1.0), (0.34, 0.34)), ((0, -1.9, 0.8), (0.18, 0.18)), ((0, -2.4, 0.7), (0.07, 0.07))]
    tail = skin("Cola", tp, chain(tp), skinC, origin=(0, -1.2, 1.0), parent=body)
    for s in (-1, 1):
        cone("Pua", (s * 0.08, -2.1, 0.78), (s * 0.45, -2.15, 1.05), 0.05, white, tail)
        cone("Pua", (s * 0.06, -2.3, 0.74), (s * 0.42, -2.45, 0.95), 0.045, white, tail)

    hp = [((0, 0.88, 0.82), (0.26, 0.26)), ((0, 1.4, 0.64), (0.22, 0.18))]
    head = skin("Cabeza", hp, chain(hp), skinC, origin=(0, 0.95, 0.8), parent=body)
    eyes(head, (0.15, 1.35, 0.75), 0.09)
    jp = [((0, 1.2, 0.5), (0.12, 0.08)), ((0, 1.5, 0.5), (0.1, 0.06))]
    skin("Mandibula", jp, chain(jp), skinC, origin=(0, 1.2, 0.52), parent=head)
    export("Stegosaurus_Cartoon")


def brachiosaurus():
    clear()
    skinC, spots = mat("Braquio_Piel", (0.78, 0.66, 0.90)), mat("Braquio_Manchas", (0.60, 0.48, 0.80))
    pts = [((0, -0.8, 2.0), (0.72, 0.74)), ((0, 0.0, 2.2), (0.86, 0.86)), ((0, 0.8, 2.35), (0.72, 0.72))]
    ed = chain(pts)
    legs(pts, ed, 0, (0.5, -0.8, 1.7), (0.52, -0.82, 0.9), (0.52, -0.85, 0.15), (0.52, -0.75, 0.06), [(0.34, 0.34), (0.26, 0.26), (0.25, 0.25), (0.3, 0.12)])
    legs(pts, ed, 2, (0.5, 0.8, 2.0), (0.52, 0.85, 1.0), (0.52, 0.9, 0.15), (0.52, 1.0, 0.06), [(0.32, 0.32), (0.25, 0.25), (0.24, 0.24), (0.29, 0.12)])
    body = skin("DinoCuerpo", pts, ed, skinC)
    for i in range(6):
        sphere("Mancha", ((0.3 if i % 2 else -0.35), 0.6 - i * 0.3, 2.95 - abs(i - 2.5) * 0.06), (0.2, 0.24, 0.08), spots, body)

    tp = [((0, -1.3, 2.0), (0.52, 0.52)), ((0, -2.3, 1.55), (0.3, 0.3)), ((0, -3.2, 1.05), (0.13, 0.13)), ((0, -3.9, 0.8), (0.04, 0.04))]
    skin("Cola", tp, chain(tp), skinC, origin=(0, -1.3, 2.0), parent=body)

    hp = [((0, 1.1, 2.6), (0.46, 0.46)), ((0, 1.45, 3.55), (0.3, 0.3)), ((0, 1.75, 4.45), (0.23, 0.23)), ((0, 2.0, 4.78), (0.3, 0.3)), ((0, 2.4, 4.72), (0.22, 0.19))]
    head = skin("Cabeza", hp, chain(hp), skinC, origin=(0, 1.1, 2.6), parent=body)
    sphere("Cresta", (0, 2.0, 5.05), (0.16, 0.2, 0.1), spots, head)
    eyes(head, (0.22, 2.15, 4.9), 0.11)
    jp = [((0, 2.05, 4.55), (0.16, 0.1)), ((0, 2.45, 4.55), (0.14, 0.08))]
    skin("Mandibula", jp, chain(jp), skinC, origin=(0, 2.05, 4.6), parent=head)
    export("Brachiosaurus_Cartoon")


# ====================================================================== FÓSILES

BONE = None
STONE = None


def bone(name, a, b, r, parent):
    """Hueso de caricatura: caña delgada con nudos redondeados en los extremos."""
    a, b = Vector(a), Vector(b)
    mid = (a + b) / 2
    pts = [(a, (r * 1.7, r * 1.7)), (a + (b - a) * 0.15, (r, r)), (mid, (r * 0.9, r * 0.9)), (a + (b - a) * 0.85, (r, r)), (b, (r * 1.7, r * 1.7))]
    return skin(name, pts, chain(pts), BONE, origin=mid, parent=parent, subdiv=1)


def base(length, width):
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, 0.1), scale=(width / 2, length / 2, 0.1))
    b = bpy.context.active_object
    b.name = "Base"
    bev = b.modifiers.new("Bevel", "BEVEL")
    bev.width = 0.05
    bev.segments = 3
    b.data.materials.append(STONE)
    return b


def spine_skeleton(name, spine, rib_range, rib_size, leg_specs, skull_size, biped, base_len):
    clear()
    base(base_len, 1.6)
    bones_root = empty("Huesos", (0, 0, 0))
    n = len(spine)
    for i, p in enumerate(spine):
        r = 0.07 + 0.06 * math.sin(i / (n - 1) * math.pi)
        v = skin("Vertebra%d" % i, [((p[0], p[1], p[2] - r * 0.4), (r, r)), ((p[0], p[1], p[2] + r * 0.9), (r * 0.45, r * 0.3))],
                 [(0, 1)], BONE, origin=p, parent=bones_root, subdiv=1)
    i0, i1 = rib_range
    for i in range(i0, i1 + 1):
        k = math.sin((i - i0 + 1) / (i1 - i0 + 2) * math.pi)
        rs = rib_size * (0.55 + 0.45 * k)
        p = Vector(spine[i])
        for s in (-1, 1):
            rp = [(p, (0.035, 0.035)), (p + Vector((s * rs * 0.75, 0, -rs * 0.25)), (0.03, 0.03)), (p + Vector((s * rs * 0.6, 0.05, -rs * 1.0)), (0.025, 0.025))]
            skin("Costilla%d%s" % (i, "D" if s > 0 else "I"), rp, chain(rp), BONE, origin=p, parent=bones_root, subdiv=1)
    for idx, fwd in leg_specs:
        hip = Vector(spine[idx])
        for s in (-1, 1):
            h = hip + Vector((s * 0.25, 0, 0))
            knee = h + Vector((s * 0.03, fwd, -(hip.z - 0.2) * 0.5))
            foot = Vector((h.x + s * 0.02, h.y - (0.05 if biped else 0), 0.3))
            bone("Femur", h, knee, 0.045, bones_root)
            bone("Tibia", knee, foot, 0.04, bones_root)
            bone("Pie", foot, foot + Vector((0, 0.22, -0.06)), 0.03, bones_root)
    if biped:
        p = Vector(spine[i0])
        for s in (-1, 1):
            bone("Brazo", p + Vector((s * 0.18, 0, -0.15)), p + Vector((s * 0.22, 0.25, -0.35)), 0.025, bones_root)
    head = Vector(spine[0])
    sk = [((head.x, head.y + skull_size * 0.1, head.z + 0.05), (skull_size * 0.32, skull_size * 0.3)),
          ((head.x, head.y + skull_size * 0.8, head.z), (skull_size * 0.2, skull_size * 0.16))]
    skull = skin("Craneo", sk, chain(sk), BONE, origin=head, parent=bones_root, subdiv=2)
    dark = mat("Cuenca", (0.25, 0.20, 0.18))
    for s in (-1, 1):
        sphere("Cuenca", (head.x + s * skull_size * 0.2, head.y + skull_size * 0.2, head.z + 0.1), (skull_size * 0.1,) * 3, dark, skull)
    jw = [((head.x, head.y + skull_size * 0.1, head.z - skull_size * 0.22), (skull_size * 0.13, 0.04)),
          ((head.x, head.y + skull_size * 0.75, head.z - skull_size * 0.2), (skull_size * 0.1, 0.03))]
    skin("MandibulaFosil", jw, chain(jw), BONE, origin=(head.x, head.y, head.z - skull_size * 0.2), parent=bones_root, subdiv=1)
    export(name)


def raptor_skeleton():
    spine = [(0, 0.95, 1.55), (0, 0.8, 1.45), (0, 0.66, 1.32)]
    spine += [(0, 0.52 - i * 0.16, 1.22 - i * 0.01) for i in range(6)]
    spine += [(0, -0.45 - i * 0.18, 1.16 - i * 0.035) for i in range(1, 9)]
    spine_skeleton("Raptor_Fosil", spine, (3, 8), 0.4, [(7, 0.2)], 0.5, True, 3.2)


def sauropod_skeleton():
    spine = [(0, 1.9 - i * 0.2, 3.3 - i * 0.28) for i in range(6)]
    spine += [(0, 0.7 - i * 0.25, 1.95 + math.sin(i / 6 * math.pi) * 0.12) for i in range(7)]
    spine += [(0, -0.9 - i * 0.25, 1.9 - i * 0.12) for i in range(1, 10)]
    spine_skeleton("Sauropodo_Fosil", spine, (6, 12), 0.7, [(7, 0.1), (12, -0.1)], 0.45, False, 5.2)


def trex_skull():
    clear()
    base(1.3, 1.1)
    root = empty("Huesos", (0, 0, 0))
    dark = mat("Cuenca", (0.25, 0.20, 0.18))
    white = mat("Diente", (0.98, 0.97, 0.92))
    cp = [((0, -0.2, 0.6), (0.3, 0.3)), ((0, 0.2, 0.55), (0.27, 0.27)), ((0, 0.55, 0.48), (0.2, 0.17))]
    cr = skin("Cupula", cp, chain(cp), BONE, origin=(0, 0.1, 0.55), parent=root)
    for s in (-1, 1):
        sphere("Cuenca", (s * 0.2, -0.05, 0.72), (0.1, 0.1, 0.11), dark, cr)
        sphere("Fosa", (s * 0.2, 0.35, 0.6), (0.06, 0.1, 0.06), dark, cr)
    teeth = empty("Dientes", (0, 0.3, 0.33), root)
    for i in range(6):
        for s in (-1, 1):
            cone("Diente", (s * 0.17, 0.0 + i * 0.1, 0.4), (s * 0.17, 0.0 + i * 0.1, 0.26), 0.03, white, teeth)
    jp = [((0, -0.2, 0.28), (0.22, 0.07)), ((0, 0.55, 0.3), (0.14, 0.05))]
    skin("MandibulaFosil", jp, chain(jp), BONE, origin=(0, 0.1, 0.28), parent=root)
    export("CraneoTRex_Fosil")


def ammonite():
    clear()
    shell = mat("Amonite", (0.88, 0.70, 0.50))
    rib = mat("Amonite2", (0.76, 0.58, 0.42))
    curve = bpy.data.curves.new("Espiral", "CURVE")
    curve.dimensions = "3D"
    sp = curve.splines.new("POLY")
    n = 90
    sp.points.add(n - 1)
    for i in range(n):
        th = i * 0.13
        r = 0.035 * math.exp(0.16 * th)
        sp.points[i].co = (math.cos(th) * r, 0, math.sin(th) * r + 0.55, 1)
        sp.points[i].radius = r * 0.95 + 0.02
    curve.bevel_depth = 1.0
    curve.bevel_resolution = 6
    curve.use_fill_caps = True
    obj = bpy.data.objects.new("Concha", curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(shell)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.shade_smooth()
    obj = bpy.context.active_object
    for i in range(8, n, 5):
        th = i * 0.13
        r = 0.035 * math.exp(0.16 * th)
        rr = r * 0.95 + 0.02
        bpy.ops.mesh.primitive_torus_add(major_radius=rr * 1.02, minor_radius=rr * 0.12, location=(math.cos(th) * r, 0, math.sin(th) * r + 0.55),
                                         rotation=(0, -th, 0))
        t = bpy.context.active_object
        t.name = "Costilla"
        bpy.ops.object.shade_smooth()
        t.data.materials.append(rib)
        set_parent(t, obj)
    export("Amonite_Fosil")


if __name__ == "__main__":
    BONE = mat("Hueso", (0.95, 0.90, 0.78))
    STONE = mat("Piedra", (0.62, 0.56, 0.50))
    # Los dinosaurios vivos ahora vienen del pack "PBR Animated Dinosaurs"; aquí solo los fósiles.
    raptor_skeleton()
    sauropod_skeleton()
    trex_skull()
