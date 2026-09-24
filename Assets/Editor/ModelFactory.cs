using System.Collections.Generic;
using UnityEngine;
using static MuseoAutista.EditorTools.Kit;

namespace MuseoAutista.EditorTools
{
    /// <summary>
    /// Modelos cartoon hechos con primitivas (estilo caricaturesco con contorno).
    /// Todos se construyen con la base en y=0 y mirando hacia +Z.
    /// Se pueden reemplazar por prefabs de la Asset Store con ModelLibrary.
    /// </summary>
    public static class ModelFactory
    {
        const PrimitiveType Sph = PrimitiveType.Sphere;
        const PrimitiveType Cub = PrimitiveType.Cube;
        const PrimitiveType Cyl = PrimitiveType.Cylinder;
        const PrimitiveType Cap = PrimitiveType.Capsule;

        static Material Dark => M("Oscuro", new Color(0.20f, 0.21f, 0.25f));
        static Material White => M("Blanco", new Color(0.97f, 0.96f, 0.93f));
        static Material Pupil => M("Pupila", new Color(0.08f, 0.08f, 0.10f), 0f);
        static Material Glass => M("Vidrio", new Color(0.45f, 0.70f, 0.85f), 0.6f);
        static Material Metal => M("Metal", new Color(0.62f, 0.64f, 0.68f), 0.5f);
        static Material Tire => M("Llanta", new Color(0.18f, 0.18f, 0.20f), 0.1f);
        static Material Bone => M("Hueso", new Color(0.93f, 0.88f, 0.76f), 0.35f);
        static Material Stone => M("Piedra", new Color(0.62f, 0.56f, 0.50f), 0.1f);

        static GameObject Root(string name) => new GameObject(name);

        /// <summary>Ojo cartoon (esclerótica + pupila + brillo) mirando hacia 'dir'.</summary>
        static void Eye(Transform p, Vector3 pos, float size, Vector3 dir)
        {
            var e = Node("Ojo", p, pos);
            e.localRotation = Quaternion.LookRotation(dir);
            Prim(Sph, e, Vector3.zero, Vector3.one * size, White);
            Prim(Sph, e, new Vector3(0f, 0f, size * 0.32f), Vector3.one * size * 0.5f, Pupil);
            Prim(Sph, e, new Vector3(size * 0.1f, size * 0.12f, size * 0.5f), Vector3.one * size * 0.14f, White);
        }

        static ExhibitAnimator Finish(GameObject root, ExhibitKind kind, Transform body, params Transform[] pivots)
        {
            FitCollider(root);
            var list = new List<Transform>(pivots) { body };
            Combine(root.transform, list);
            var a = root.AddComponent<ExhibitAnimator>();
            a.kind = kind;
            a.body = body;
            a.dwellTime = 1.6f;
            return a;
        }

        // ================================================================== AVIONES

        public enum JetStyle { Falcon, TwinTail, Stealth, Acrobat }

        public static ExhibitAnimator Jet(string name, JetStyle style, Color main, Color accent, float stand = 0.75f, bool withStand = true)
        {
            var root = Root(name);
            var mMain = M("Avion_" + style, main);
            var mAcc = M("AvionAcento_" + style, accent);
            if (withStand)
            {
                Prim(Cyl, root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.04f, 0.8f), Dark);
                Seg(Cyl, root.transform, new Vector3(0f, 0f, 0f), new Vector3(0f, stand - 0.15f, -0.1f), 0.06f, Dark);
            }
            var body = Node("Cuerpo", root.transform, new Vector3(0f, stand, 0f));

            bool stealth = style == JetStyle.Stealth;
            float w = stealth ? 0.55f : 0.44f, h = stealth ? 0.30f : 0.42f;
            Prim(Cap, body, Vector3.zero, new Vector3(w, 1.35f, h), mMain, new Vector3(90f, 0f, 0f));
            Prim(Sph, body, new Vector3(0f, -0.01f, 1.28f), new Vector3(w * 0.55f, h * 0.55f, 0.35f), mAcc);
            Prim(Sph, body, new Vector3(0f, h * 0.42f, 0.55f), new Vector3(w * 0.62f, 0.3f, 0.7f), Glass);
            Prim(Cub, body, new Vector3(0f, -h * 0.45f, 0.2f), new Vector3(w * 0.6f, 0.14f, 0.55f), mAcc);

            float span = stealth ? 0.95f : 1.0f, chord = stealth ? 0.95f : 0.62f, sweep = stealth ? 32f : 22f;
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(Cub, body, new Vector3(s * 0.62f, -0.02f, -0.22f), new Vector3(span, 0.06f, chord), mMain, new Vector3(0f, s * sweep, 0f));
                Prim(Cub, body, new Vector3(s * 1.1f, -0.02f, -0.45f), new Vector3(0.12f, 0.065f, 0.3f), mAcc, new Vector3(0f, s * sweep, 0f));
                Prim(Cub, body, new Vector3(s * 0.42f, 0f, -1.12f), new Vector3(0.55f, 0.05f, 0.34f), mMain, new Vector3(0f, s * 22f, 0f));
                if (style == JetStyle.Falcon || style == JetStyle.Acrobat)
                    Seg(Cap, body, new Vector3(s * 1.18f, 0f, 0f), new Vector3(s * 1.18f, 0f, -0.55f), 0.045f, White);
            }

            if (style == JetStyle.TwinTail || style == JetStyle.Stealth)
            {
                float cant = stealth ? 26f : 10f;
                for (int s = -1; s <= 1; s += 2)
                    Prim(Cub, body, new Vector3(s * 0.24f, 0.36f, -0.98f), new Vector3(0.05f, 0.55f, 0.48f), mAcc, new Vector3(-25f, 0f, -s * cant));
            }
            else
                Prim(Cub, body, new Vector3(0f, 0.42f, -0.98f), new Vector3(0.05f, 0.65f, 0.52f), mAcc, new Vector3(-25f, 0f, 0f));

            var glows = new List<GameObject>();
            float[] engines = style == JetStyle.TwinTail || style == JetStyle.Stealth ? new[] { -0.13f, 0.13f } : new[] { 0f };
            var burner = Glow("Postcombustion", new Color(1f, 0.62f, 0.30f), 1.3f);
            foreach (float x in engines)
            {
                Seg(Cyl, body, new Vector3(x, 0f, -1.2f), new Vector3(x, 0f, -1.42f), engines.Length > 1 ? 0.11f : 0.15f, Dark);
                var g = Node("Fuego", body, new Vector3(x, 0f, -1.42f));
                Seg(Cap, g, Vector3.zero, new Vector3(0f, 0f, -0.35f), 0.08f, burner);
                glows.Add(g.gameObject);
            }
            if (style == JetStyle.Acrobat)
                Prim(Cub, body, new Vector3(0f, 0.2f, -0.2f), new Vector3(0.05f, 0.05f, 1.6f), mAcc);

            var a = Finish(root, ExhibitKind.Jet, body, glows.ConvertAll(g => g.transform).ToArray());
            a.glowParts = glows.ToArray();
            return a;
        }

        // ================================================================== DINOSAURIOS

        public static ExhibitAnimator TRex(string name, Color skin, Color belly)
        {
            var root = Root(name);
            var mS = M("TRex_Piel", skin);
            var mB = M("TRex_Panza", belly);
            var body = Node("Cuerpo", root.transform, Vector3.zero);

            Prim(Sph, body, new Vector3(0f, 1.45f, 0.05f), new Vector3(1.05f, 1.1f, 1.7f), mS, new Vector3(-12f, 0f, 0f));
            Prim(Sph, body, new Vector3(0f, 1.32f, 0.32f), new Vector3(0.8f, 0.8f, 1.1f), mB, new Vector3(-12f, 0f, 0f));
            Seg(Cap, body, new Vector3(0f, 1.75f, 0.55f), new Vector3(0f, 2.05f, 0.85f), 0.36f, mS);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(Sph, body, new Vector3(s * 0.4f, 1.2f, -0.12f), new Vector3(0.45f, 0.8f, 0.7f), mS);
                Seg(Cap, body, new Vector3(s * 0.45f, 0.95f, -0.2f), new Vector3(s * 0.45f, 0.3f, -0.3f), 0.15f, mS);
                Prim(Sph, body, new Vector3(s * 0.45f, 0.12f, 0f), new Vector3(0.32f, 0.22f, 0.55f), mS);
                for (int c = -1; c <= 1; c++)
                    Prim(Sph, body, new Vector3(s * 0.45f + c * 0.09f, 0.07f, 0.27f), new Vector3(0.08f, 0.08f, 0.1f), White);
                Seg(Cap, body, new Vector3(s * 0.32f, 1.45f, 0.72f), new Vector3(s * 0.36f, 1.3f, 0.95f), 0.07f, mS);
            }
            for (int i = 0; i < 4; i++)
                Prim(Sph, body, new Vector3(0f, 1.95f - i * 0.02f, 0.45f - i * 0.38f), new Vector3(0.14f, 0.18f, 0.22f), mB);

            var head = Node("Cabeza", body, new Vector3(0f, 2.1f, 0.9f));
            Prim(Sph, head, new Vector3(0f, 0.12f, 0.3f), new Vector3(0.72f, 0.62f, 0.85f), mS);
            Prim(Sph, head, new Vector3(0f, 0.04f, 0.68f), new Vector3(0.55f, 0.4f, 0.55f), mS);
            Eye(head, new Vector3(0.24f, 0.28f, 0.45f), 0.2f, new Vector3(0.6f, 0.1f, 1f));
            Eye(head, new Vector3(-0.24f, 0.28f, 0.45f), 0.2f, new Vector3(-0.6f, 0.1f, 1f));
            Prim(Sph, head, new Vector3(0.1f, 0.14f, 0.93f), Vector3.one * 0.05f, Pupil);
            Prim(Sph, head, new Vector3(-0.1f, 0.14f, 0.93f), Vector3.one * 0.05f, Pupil);
            for (int i = 0; i < 4; i++)
                for (int s = -1; s <= 1; s += 2)
                    Prim(Cub, head, new Vector3(s * 0.2f, -0.13f, 0.5f + i * 0.1f), new Vector3(0.05f, 0.07f, 0.05f), White, new Vector3(0f, 45f, 0f));

            var jaw = Node("Mandibula", head, new Vector3(0f, -0.1f, 0.2f));
            Prim(Sph, jaw, new Vector3(0f, -0.08f, 0.35f), new Vector3(0.5f, 0.22f, 0.8f), mB);

            var tail = Node("Cola", body, new Vector3(0f, 1.55f, -0.65f));
            Seg(Cap, tail, Vector3.zero, new Vector3(0f, -0.1f, -0.8f), 0.38f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.1f, -0.8f), new Vector3(0f, -0.3f, -1.5f), 0.25f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.3f, -1.5f), new Vector3(0f, -0.5f, -2.05f), 0.13f, mS);

            var a = Finish(root, ExhibitKind.Dinosaur, body, head, jaw, tail);
            a.head = head; a.jaw = jaw; a.tail = tail;
            return a;
        }

        public static ExhibitAnimator Triceratops(string name, Color skin, Color frill)
        {
            var root = Root(name);
            var mS = M("Tri_Piel", skin);
            var mF = M("Tri_Gola", frill);
            var body = Node("Cuerpo", root.transform, Vector3.zero);
            Prim(Sph, body, new Vector3(0f, 1.0f, 0f), new Vector3(1.35f, 1.15f, 2.0f), mS);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Seg(Cyl, body, new Vector3(sx * 0.45f, 0.75f, sz * 0.6f), new Vector3(sx * 0.48f, 0.12f, sz * 0.62f), 0.17f, mS);
                    Prim(Cyl, body, new Vector3(sx * 0.48f, 0.08f, sz * 0.64f), new Vector3(0.42f, 0.08f, 0.42f), mS);
                }
            for (int i = 0; i < 5; i++)
                Prim(Sph, body, new Vector3(0f, 1.55f, 0.6f - i * 0.3f), new Vector3(0.18f, 0.12f, 0.18f), mF);

            var head = Node("Cabeza", body, new Vector3(0f, 1.1f, 0.95f));
            Prim(Sph, head, new Vector3(0f, 0f, 0.35f), new Vector3(0.7f, 0.62f, 0.8f), mS);
            Prim(Cyl, head, new Vector3(0f, 0.35f, 0.05f), new Vector3(1.3f, 0.05f, 1.15f), mF, new Vector3(-65f, 0f, 0f));
            for (int i = 0; i < 7; i++)
            {
                float ang = Mathf.Lerp(-80f, 80f, i / 6f) * Mathf.Deg2Rad;
                Prim(Sph, head, new Vector3(Mathf.Sin(ang) * 0.62f, 0.35f + Mathf.Cos(ang) * 0.56f, -0.2f + Mathf.Cos(ang) * 0.02f), Vector3.one * 0.14f, mS);
            }
            for (int s = -1; s <= 1; s += 2)
                Seg(Cap, head, new Vector3(s * 0.18f, 0.3f, 0.45f), new Vector3(s * 0.26f, 0.62f, 0.95f), 0.055f, White);
            Seg(Cap, head, new Vector3(0f, 0.08f, 0.72f), new Vector3(0f, 0.28f, 0.88f), 0.05f, White);
            Prim(Sph, head, new Vector3(0f, -0.08f, 0.72f), new Vector3(0.28f, 0.26f, 0.3f), Dark);
            Eye(head, new Vector3(0.24f, 0.2f, 0.52f), 0.17f, new Vector3(0.7f, 0.1f, 1f));
            Eye(head, new Vector3(-0.24f, 0.2f, 0.52f), 0.17f, new Vector3(-0.7f, 0.1f, 1f));
            var jaw = Node("Mandibula", head, new Vector3(0f, -0.15f, 0.4f));
            Prim(Sph, jaw, new Vector3(0f, -0.05f, 0.18f), new Vector3(0.4f, 0.18f, 0.45f), mS);

            var tail = Node("Cola", body, new Vector3(0f, 1.0f, -0.9f));
            Seg(Cap, tail, Vector3.zero, new Vector3(0f, -0.25f, -0.8f), 0.28f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.25f, -0.8f), new Vector3(0f, -0.5f, -1.4f), 0.13f, mS);

            var a = Finish(root, ExhibitKind.Dinosaur, body, head, jaw, tail);
            a.head = head; a.jaw = jaw; a.tail = tail;
            return a;
        }

        public static ExhibitAnimator Stegosaurus(string name, Color skin, Color plates)
        {
            var root = Root(name);
            var mS = M("Stego_Piel", skin);
            var mP = M("Stego_Placas", plates);
            var body = Node("Cuerpo", root.transform, Vector3.zero);
            Prim(Sph, body, new Vector3(0f, 1.05f, 0f), new Vector3(1.15f, 1.15f, 2.1f), mS, new Vector3(8f, 0f, 0f));
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    float top = sz > 0 ? 0.7f : 0.85f;
                    Seg(Cyl, body, new Vector3(sx * 0.4f, top, sz * 0.62f), new Vector3(sx * 0.42f, 0.1f, sz * 0.64f), 0.15f, mS);
                    Prim(Cyl, body, new Vector3(sx * 0.42f, 0.07f, sz * 0.66f), new Vector3(0.36f, 0.07f, 0.36f), mS);
                }
            for (int i = 0; i < 7; i++)
            {
                float z = 0.75f - i * 0.28f;
                float y = 1.55f + Mathf.Sin((i + 1) / 8f * Mathf.PI) * 0.25f;
                float size = 0.28f + Mathf.Sin((i + 1) / 8f * Mathf.PI) * 0.22f;
                Prim(Cub, body, new Vector3((i % 2 == 0 ? 0.08f : -0.08f), y, z), new Vector3(0.06f, size, size), mP, new Vector3(45f, 0f, 0f));
            }
            var head = Node("Cabeza", body, new Vector3(0f, 0.85f, 1.0f));
            Seg(Cap, head, Vector3.zero, new Vector3(0f, -0.1f, 0.45f), 0.2f, mS);
            Prim(Sph, head, new Vector3(0f, -0.12f, 0.58f), new Vector3(0.34f, 0.3f, 0.45f), mS);
            Eye(head, new Vector3(0.14f, -0.02f, 0.62f), 0.12f, new Vector3(0.8f, 0.1f, 1f));
            Eye(head, new Vector3(-0.14f, -0.02f, 0.62f), 0.12f, new Vector3(-0.8f, 0.1f, 1f));
            var jaw = Node("Mandibula", head, new Vector3(0f, -0.2f, 0.5f));
            Prim(Sph, jaw, new Vector3(0f, -0.02f, 0.12f), new Vector3(0.22f, 0.1f, 0.3f), mS);

            var tail = Node("Cola", body, new Vector3(0f, 1.2f, -0.95f));
            Seg(Cap, tail, Vector3.zero, new Vector3(0f, -0.2f, -0.8f), 0.28f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.2f, -0.8f), new Vector3(0f, -0.35f, -1.5f), 0.14f, mS);
            for (int s = -1; s <= 1; s += 2)
            {
                Seg(Cap, tail, new Vector3(s * 0.05f, -0.32f, -1.35f), new Vector3(s * 0.35f, -0.05f, -1.45f), 0.04f, White);
                Seg(Cap, tail, new Vector3(s * 0.05f, -0.34f, -1.5f), new Vector3(s * 0.35f, -0.1f, -1.7f), 0.04f, White);
            }
            var a = Finish(root, ExhibitKind.Dinosaur, body, head, jaw, tail);
            a.head = head; a.jaw = jaw; a.tail = tail;
            return a;
        }

        public static ExhibitAnimator Brachiosaurus(string name, Color skin, Color spots)
        {
            var root = Root(name);
            var mS = M("Braquio_Piel", skin);
            var mP = M("Braquio_Manchas", spots);
            var body = Node("Cuerpo", root.transform, Vector3.zero);
            Prim(Sph, body, new Vector3(0f, 2.0f, 0f), new Vector3(1.45f, 1.4f, 2.4f), mS, new Vector3(-8f, 0f, 0f));
            for (int i = 0; i < 5; i++)
                Prim(Sph, body, new Vector3((i % 2 == 0 ? 0.35f : -0.4f), 2.45f + (i % 3) * 0.08f, 0.7f - i * 0.4f), new Vector3(0.35f, 0.2f, 0.4f), mP);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Seg(Cyl, body, new Vector3(sx * 0.5f, 2.0f, 0.75f), new Vector3(sx * 0.52f, 0.1f, 0.85f), 0.22f, mS);
                Seg(Cyl, body, new Vector3(sx * 0.5f, 1.7f, -0.7f), new Vector3(sx * 0.52f, 0.1f, -0.78f), 0.22f, mS);
                Prim(Cyl, body, new Vector3(sx * 0.52f, 0.08f, 0.87f), new Vector3(0.52f, 0.08f, 0.52f), mS);
                Prim(Cyl, body, new Vector3(sx * 0.52f, 0.08f, -0.8f), new Vector3(0.52f, 0.08f, 0.52f), mS);
            }
            var head = Node("Cuello", body, new Vector3(0f, 2.45f, 0.95f));
            Seg(Cap, head, Vector3.zero, new Vector3(0f, 0.9f, 0.45f), 0.32f, mS);
            Seg(Cap, head, new Vector3(0f, 0.9f, 0.45f), new Vector3(0f, 1.75f, 0.7f), 0.24f, mS);
            Prim(Sph, head, new Vector3(0f, 1.95f, 0.85f), new Vector3(0.45f, 0.4f, 0.62f), mS);
            Prim(Sph, head, new Vector3(0f, 2.12f, 0.75f), new Vector3(0.3f, 0.2f, 0.3f), mP);
            Eye(head, new Vector3(0.17f, 2.02f, 1.0f), 0.14f, new Vector3(0.7f, 0f, 1f));
            Eye(head, new Vector3(-0.17f, 2.02f, 1.0f), 0.14f, new Vector3(-0.7f, 0f, 1f));
            var jaw = Node("Mandibula", head, new Vector3(0f, 1.82f, 0.85f));
            Prim(Sph, jaw, new Vector3(0f, -0.02f, 0.2f), new Vector3(0.3f, 0.12f, 0.35f), mS);

            var tail = Node("Cola", body, new Vector3(0f, 2.0f, -1.1f));
            Seg(Cap, tail, Vector3.zero, new Vector3(0f, -0.4f, -1.0f), 0.36f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.4f, -1.0f), new Vector3(0f, -0.9f, -1.9f), 0.2f, mS);
            Seg(Cap, tail, new Vector3(0f, -0.9f, -1.9f), new Vector3(0f, -1.3f, -2.6f), 0.1f, mS);

            var a = Finish(root, ExhibitKind.Dinosaur, body, head, jaw, tail);
            a.head = head; a.jaw = jaw; a.tail = tail;
            return a;
        }

        // ================================================================== FÓSILES

        static Transform BonePart(Transform parent, string name, Vector3 pos, List<Transform> bones)
        {
            var n = Node(name, parent, pos);
            bones.Add(n);
            return n;
        }

        /// <summary>Esqueleto a partir de una columna (cuello→cola). Cada hueso se puede separar al animar.</summary>
        static ExhibitAnimator Skeleton(string name, Vector3[] spine, int ribStart, int ribEnd, float ribSize,
            (int attach, float length, float fwd)[] legs, float skullSize, bool biped, float slabLength)
        {
            var root = Root(name);
            var bones = new List<Transform>();
            Prim(Cub, root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.6f, 0.2f, slabLength), Stone);
            var body = Node("Esqueleto", root.transform, Vector3.zero);

            for (int i = 0; i < spine.Length; i++)
            {
                float r = Mathf.Lerp(0.16f, 0.07f, Mathf.Abs(i - ribStart) / (float)spine.Length) * (i > ribEnd ? 0.8f : 1f);
                var b = BonePart(body, "Vertebra" + i, spine[i], bones);
                Prim(Sph, b, Vector3.zero, new Vector3(r, r * 1.2f, r), Bone);
                Prim(Cub, b, new Vector3(0f, r * 0.8f, 0f), new Vector3(r * 0.3f, r * 0.9f, r * 0.5f), Bone);
            }
            for (int i = ribStart; i <= ribEnd; i++)
            {
                float k = Mathf.Sin((i - ribStart + 1f) / (ribEnd - ribStart + 2f) * Mathf.PI);
                for (int s = -1; s <= 1; s += 2)
                {
                    var b = BonePart(body, "Costilla" + i + (s > 0 ? "D" : "I"), spine[i], bones);
                    float rs = ribSize * (0.55f + 0.45f * k);
                    Seg(Cap, b, Vector3.zero, new Vector3(s * rs * 0.7f, -rs * 0.3f, 0f), 0.03f, Bone);
                    Seg(Cap, b, new Vector3(s * rs * 0.7f, -rs * 0.3f, 0f), new Vector3(s * rs * 0.55f, -rs * 1.0f, 0.05f), 0.028f, Bone);
                }
            }
            foreach (var leg in legs)
            {
                Vector3 hip = spine[leg.attach];
                for (int s = -1; s <= 1; s += 2)
                {
                    var b = BonePart(body, "Pata" + leg.attach + (s > 0 ? "D" : "I"), hip + new Vector3(s * 0.28f, 0f, 0f), bones);
                    Vector3 knee = new Vector3(s * 0.04f, -hip.y * 0.5f + 0.1f, leg.fwd);
                    Vector3 foot = new Vector3(s * 0.05f, -hip.y + 0.26f, biped ? -0.05f : 0f);
                    Prim(Sph, b, Vector3.zero, Vector3.one * 0.18f, Bone);
                    Seg(Cap, b, Vector3.zero, knee, 0.06f, Bone);
                    Seg(Cap, b, knee, foot, 0.05f, Bone);
                    Prim(Cub, b, foot + new Vector3(0f, -0.03f, 0.1f), new Vector3(0.14f, 0.05f, 0.28f), Bone);
                }
            }
            if (biped)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    var arm = BonePart(body, "Brazo" + s, spine[ribStart] + new Vector3(s * 0.2f, -0.2f, 0.05f), bones);
                    Seg(Cap, arm, Vector3.zero, new Vector3(s * 0.05f, -0.25f, 0.18f), 0.035f, Bone);
                    Seg(Cap, arm, new Vector3(s * 0.05f, -0.25f, 0.18f), new Vector3(s * 0.05f, -0.2f, 0.35f), 0.03f, Bone);
                }
            }
            var skull = BonePart(body, "Craneo", spine[0] + new Vector3(0f, 0.05f, skullSize * 0.5f), bones);
            Prim(Sph, skull, Vector3.zero, new Vector3(skullSize * 0.6f, skullSize * 0.55f, skullSize), Bone);
            for (int s = -1; s <= 1; s += 2)
                Prim(Sph, skull, new Vector3(s * skullSize * 0.2f, skullSize * 0.1f, skullSize * 0.05f), Vector3.one * skullSize * 0.2f, Dark);
            var jaw = BonePart(body, "Mandibula", spine[0] + new Vector3(0f, -skullSize * 0.25f, skullSize * 0.55f), bones);
            Prim(Cub, jaw, Vector3.zero, new Vector3(skullSize * 0.4f, skullSize * 0.12f, skullSize * 0.85f), Bone);

            FitCollider(root);
            var pivots = new List<Transform>(bones) { body };
            Combine(root.transform, pivots);
            var a = root.AddComponent<ExhibitAnimator>();
            a.kind = ExhibitKind.Fossil;
            a.body = body;
            a.bones = bones.ToArray();
            a.dwellTime = 1.6f;
            return a;
        }

        public static ExhibitAnimator RaptorSkeleton(string name)
        {
            var spine = new List<Vector3>();
            // cuello (sube hacia delante), lomo, cola
            spine.Add(new Vector3(0f, 1.55f, 1.0f));
            spine.Add(new Vector3(0f, 1.45f, 0.85f));
            spine.Add(new Vector3(0f, 1.32f, 0.7f));
            for (int i = 0; i < 6; i++) spine.Add(new Vector3(0f, 1.22f - i * 0.01f, 0.55f - i * 0.16f));
            for (int i = 1; i <= 8; i++) spine.Add(new Vector3(0f, 1.16f - i * 0.03f, -0.4f - i * 0.18f));
            return Skeleton(name, spine.ToArray(), 3, 8, 0.42f, new[] { (7, 1.1f, 0.2f) }, 0.45f, true, 3.2f);
        }

        public static ExhibitAnimator SauropodSkeleton(string name)
        {
            var spine = new List<Vector3>();
            for (int i = 0; i < 6; i++) spine.Add(new Vector3(0f, 3.3f - i * 0.28f, 1.9f - i * 0.2f));
            for (int i = 0; i < 7; i++) spine.Add(new Vector3(0f, 1.95f + Mathf.Sin(i / 6f * Mathf.PI) * 0.12f, 0.7f - i * 0.25f));
            for (int i = 1; i <= 9; i++) spine.Add(new Vector3(0f, 1.9f - i * 0.12f, -0.9f - i * 0.25f));
            return Skeleton(name, spine.ToArray(), 6, 12, 0.75f, new[] { (7, 1.9f, 0.1f), (12, 1.9f, -0.1f) }, 0.4f, false, 5.2f);
        }

        public static ExhibitAnimator TRexSkull(string name)
        {
            var root = Root(name);
            var bones = new List<Transform>();
            Prim(Cub, root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1.1f, 0.12f, 1.3f), Stone);
            var body = Node("Craneo", root.transform, new Vector3(0f, 0.12f, 0f));
            var cr = BonePart(body, "Cupula", new Vector3(0f, 0.45f, -0.15f), bones);
            Prim(Sph, cr, Vector3.zero, new Vector3(0.62f, 0.55f, 0.75f), Bone);
            for (int s = -1; s <= 1; s += 2)
                Prim(Sph, cr, new Vector3(s * 0.22f, 0.06f, 0.15f), Vector3.one * 0.18f, Dark);
            var sn = BonePart(body, "Hocico", new Vector3(0f, 0.36f, 0.35f), bones);
            Prim(Sph, sn, Vector3.zero, new Vector3(0.45f, 0.38f, 0.75f), Bone);
            Prim(Sph, sn, new Vector3(0f, 0.08f, 0.28f), new Vector3(0.12f, 0.08f, 0.1f), Dark);
            var teeth = BonePart(body, "Dientes", new Vector3(0f, 0.18f, 0.3f), bones);
            for (int i = 0; i < 6; i++)
                for (int s = -1; s <= 1; s += 2)
                    Seg(Cap, teeth, new Vector3(s * 0.17f, 0f, -0.2f + i * 0.1f), new Vector3(s * 0.17f, -0.1f, -0.2f + i * 0.1f), 0.025f, White);
            var jaw = BonePart(body, "Mandibula", new Vector3(0f, 0.08f, 0.15f), bones);
            Prim(Cub, jaw, Vector3.zero, new Vector3(0.46f, 0.1f, 0.95f), Bone);
            FitCollider(root);
            var pivots = new List<Transform>(bones) { body };
            Combine(root.transform, pivots);
            var a = root.AddComponent<ExhibitAnimator>();
            a.kind = ExhibitKind.Fossil; a.body = body; a.bones = bones.ToArray(); a.dwellTime = 1.6f;
            return a;
        }

        static GameObject lastAmmonite;

        static GameObject AmmoniteRoot(string name, float scale)
        {
            Ammonite(name, scale, false);
            return lastAmmonite;
        }

        /// <summary>Amonite: espiral logarítmica de "cámaras".</summary>
        public static ExhibitAnimator Ammonite(string name, float scale = 1f, bool animated = true)
        {
            var root = Root(name);
            lastAmmonite = root;
            var bones = new List<Transform>();
            var body = Node("Concha", root.transform, new Vector3(0f, 0.2f * scale + 0.1f, 0f));
            var shell = M("Amonite", new Color(0.86f, 0.70f, 0.52f));
            var shell2 = M("Amonite2", new Color(0.78f, 0.60f, 0.45f));
            int n = 30;
            for (int i = 0; i < n; i++)
            {
                float th = i * 0.42f;
                float r = 0.03f * Mathf.Exp(0.13f * th);
                float s = r * 0.85f + 0.02f;
                var b = animated ? BonePart(body, "Camara" + i, new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, 0f) * scale, bones)
                                 : Node("Camara" + i, body, new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, 0f) * scale);
                Prim(Sph, b, Vector3.zero, new Vector3(s, s, s * 0.9f) * scale, i % 2 == 0 ? shell : shell2);
            }
            if (!animated) return null;
            FitCollider(root);
            var pivots = new List<Transform>(bones) { body };
            Combine(root.transform, pivots);
            var a = root.AddComponent<ExhibitAnimator>();
            a.kind = ExhibitKind.Fossil; a.body = body; a.bones = bones.ToArray(); a.dwellTime = 1.6f;
            return a;
        }

        // ================================================================== CARROS

        static Transform Wheel(Transform body, Vector3 pos, float r, float width, List<Transform> wheels)
        {
            var w = Node("Rueda", body, pos);
            Prim(Cyl, w, Vector3.zero, new Vector3(r * 2f, width * 0.5f, r * 2f), Tire, new Vector3(0f, 0f, 90f));
            Prim(Cyl, w, new Vector3(Mathf.Sign(pos.x) * width * 0.08f, 0f, 0f), new Vector3(r * 1.1f, width * 0.5f, r * 1.1f), Metal, new Vector3(0f, 0f, 90f));
            Prim(Cub, w, new Vector3(Mathf.Sign(pos.x) * width * 0.55f, 0f, 0f), new Vector3(0.02f, r * 1.0f, 0.06f), Dark);
            wheels.Add(w);
            return w;
        }

        static GameObject Light(Transform body, Vector3 pos, Vector3 size, bool front)
        {
            Prim(Sph, body, pos, size, front ? M("FaroApagado", new Color(0.95f, 0.93f, 0.80f)) : M("CalaveraApagada", new Color(0.70f, 0.25f, 0.22f)));
            var g = Prim(Sph, body, pos + new Vector3(0f, 0f, front ? 0.01f : -0.01f), size * 1.08f,
                front ? Glow("FaroEncendido", new Color(1f, 0.95f, 0.75f), 1.2f) : Glow("CalaveraEncendida", new Color(1f, 0.3f, 0.25f), 1f));
            g.name = "Luz";
            return g;
        }

        static ExhibitAnimator FinishCar(GameObject root, Transform body, Transform turntable, List<Transform> wheels, List<GameObject> lights)
        {
            FitCollider(root);
            var pivots = new List<Transform>(wheels) { body, turntable };
            foreach (var l in lights) pivots.Add(l.transform);
            Combine(root.transform, pivots);
            var a = root.AddComponent<ExhibitAnimator>();
            a.kind = ExhibitKind.Car;
            a.body = body;
            a.turntable = turntable;
            a.wheels = wheels.ToArray();
            a.glowParts = lights.ToArray();
            a.dwellTime = 1.6f;
            return a;
        }

        static Transform Turntable(Transform root, float radius)
        {
            Prim(Cyl, root, new Vector3(0f, 0.05f, 0f), new Vector3(radius * 2f + 0.2f, 0.05f, radius * 2f + 0.2f), Dark);
            var t = Node("Plataforma", root, new Vector3(0f, 0.1f, 0f));
            Prim(Cyl, t, new Vector3(0f, 0.04f, 0f), new Vector3(radius * 2f, 0.04f, radius * 2f), M("Plataforma", new Color(0.82f, 0.82f, 0.86f)));
            Prim(Cub, t, new Vector3(0f, 0.085f, radius * 0.85f), new Vector3(0.3f, 0.01f, 0.12f), M("FlechaPlataforma", new Color(0.95f, 0.78f, 0.35f)));
            return t;
        }

        public static ExhibitAnimator SportsCar(string name, Color paint, bool withTurntable = true)
        {
            var root = Root(name);
            var mP = M("Carro_Deportivo", paint);
            var tt = withTurntable ? Turntable(root.transform, 1.6f) : root.transform;
            var body = Node("Carroceria", tt, new Vector3(0f, withTurntable ? 0.08f : 0f, 0f));
            var wheels = new List<Transform>();
            var lights = new List<GameObject>();
            Prim(Cub, body, new Vector3(0f, 0.36f, 0f), new Vector3(1.05f, 0.3f, 2.3f), mP);
            Prim(Sph, body, new Vector3(0f, 0.4f, 0.95f), new Vector3(1.04f, 0.28f, 0.7f), mP);
            Prim(Sph, body, new Vector3(0f, 0.6f, -0.2f), new Vector3(0.88f, 0.42f, 1.25f), Glass);
            Prim(Sph, body, new Vector3(0f, 0.66f, -0.25f), new Vector3(0.8f, 0.36f, 1.05f), mP);
            Prim(Cub, body, new Vector3(0f, 0.72f, -1.1f), new Vector3(1.0f, 0.05f, 0.25f), Dark);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(Cub, body, new Vector3(s * 0.35f, 0.6f, -1.08f), new Vector3(0.05f, 0.22f, 0.08f), Dark);
                Wheel(body, new Vector3(s * 0.5f, 0.22f, 0.72f), 0.22f, 0.2f, wheels);
                Wheel(body, new Vector3(s * 0.5f, 0.22f, -0.75f), 0.23f, 0.22f, wheels);
                lights.Add(Light(body, new Vector3(s * 0.33f, 0.42f, 1.22f), new Vector3(0.22f, 0.1f, 0.08f), true));
                lights.Add(Light(body, new Vector3(s * 0.36f, 0.42f, -1.16f), new Vector3(0.2f, 0.08f, 0.06f), false));
            }
            Prim(Cub, body, new Vector3(0f, 0.3f, 1.2f), new Vector3(0.5f, 0.08f, 0.04f), Dark);
            return FinishCar(root, body, withTurntable ? tt : null, wheels, lights);
        }

        public static ExhibitAnimator Beetle(string name, Color paint)
        {
            var root = Root(name);
            var mP = M("Carro_Clasico", paint);
            var tt = Turntable(root.transform, 1.5f);
            var body = Node("Carroceria", tt, new Vector3(0f, 0.08f, 0f));
            var wheels = new List<Transform>();
            var lights = new List<GameObject>();
            Prim(Sph, body, new Vector3(0f, 0.55f, 0f), new Vector3(1.05f, 0.75f, 2.1f), mP);
            Prim(Sph, body, new Vector3(0f, 0.8f, -0.12f), new Vector3(0.88f, 0.5f, 1.1f), Glass);
            Prim(Sph, body, new Vector3(0f, 0.86f, -0.14f), new Vector3(0.8f, 0.42f, 0.9f), mP);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(Sph, body, new Vector3(s * 0.45f, 0.38f, 0.68f), new Vector3(0.35f, 0.4f, 0.65f), mP);
                Prim(Sph, body, new Vector3(s * 0.45f, 0.38f, -0.68f), new Vector3(0.35f, 0.42f, 0.7f), mP);
                Wheel(body, new Vector3(s * 0.5f, 0.2f, 0.68f), 0.2f, 0.16f, wheels);
                Wheel(body, new Vector3(s * 0.5f, 0.2f, -0.68f), 0.2f, 0.16f, wheels);
                lights.Add(Light(body, new Vector3(s * 0.45f, 0.55f, 0.95f), new Vector3(0.18f, 0.18f, 0.1f), true));
                lights.Add(Light(body, new Vector3(s * 0.42f, 0.5f, -1.0f), new Vector3(0.1f, 0.14f, 0.06f), false));
                Prim(Cub, body, new Vector3(s * 0.55f, 0.2f, 0f), new Vector3(0.12f, 0.04f, 0.7f), Dark);
            }
            Prim(Cub, body, new Vector3(0f, 0.25f, 1.08f), new Vector3(1.0f, 0.06f, 0.06f), Metal);
            Prim(Cub, body, new Vector3(0f, 0.25f, -1.08f), new Vector3(1.0f, 0.06f, 0.06f), Metal);
            return FinishCar(root, body, tt, wheels, lights);
        }

        public static ExhibitAnimator Jeep(string name, Color paint)
        {
            var root = Root(name);
            var mP = M("Carro_Jeep", paint);
            var tt = Turntable(root.transform, 1.6f);
            var body = Node("Carroceria", tt, new Vector3(0f, 0.08f, 0f));
            var wheels = new List<Transform>();
            var lights = new List<GameObject>();
            Prim(Cub, body, new Vector3(0f, 0.62f, 0f), new Vector3(1.15f, 0.45f, 2.0f), mP);
            Prim(Cub, body, new Vector3(0f, 0.9f, 0.55f), new Vector3(1.05f, 0.12f, 0.8f), mP);
            Prim(Cub, body, new Vector3(0f, 1.1f, 0.18f), new Vector3(1.05f, 0.35f, 0.05f), Glass, new Vector3(-15f, 0f, 0f));
            Prim(Cub, body, new Vector3(0f, 0.68f, 1.02f), new Vector3(0.8f, 0.3f, 0.04f), Dark);
            for (int s = -1; s <= 1; s += 2)
            {
                Seg(Cap, body, new Vector3(s * 0.5f, 0.85f, -0.2f), new Vector3(s * 0.5f, 1.35f, -0.3f), 0.04f, Dark);
                Wheel(body, new Vector3(s * 0.58f, 0.3f, 0.65f), 0.3f, 0.24f, wheels);
                Wheel(body, new Vector3(s * 0.58f, 0.3f, -0.65f), 0.3f, 0.24f, wheels);
                lights.Add(Light(body, new Vector3(s * 0.3f, 0.72f, 1.05f), new Vector3(0.16f, 0.16f, 0.06f), true));
                lights.Add(Light(body, new Vector3(s * 0.45f, 0.65f, -1.01f), new Vector3(0.1f, 0.14f, 0.05f), false));
            }
            Seg(Cap, body, new Vector3(-0.5f, 1.35f, -0.3f), new Vector3(0.5f, 1.35f, -0.3f), 0.04f, Dark);
            Prim(Cyl, body, new Vector3(0f, 0.7f, -1.12f), new Vector3(0.5f, 0.1f, 0.5f), Tire, new Vector3(90f, 0f, 0f));
            return FinishCar(root, body, tt, wheels, lights);
        }

        public static ExhibitAnimator F1(string name, Color paint, Color accent)
        {
            var root = Root(name);
            var mP = M("Carro_F1", paint);
            var mA = M("Carro_F1_Acento", accent);
            var tt = Turntable(root.transform, 1.9f);
            var body = Node("Carroceria", tt, new Vector3(0f, 0.08f, 0f));
            var wheels = new List<Transform>();
            var lights = new List<GameObject>();
            Prim(Cap, body, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.5f, 1.4f, 0.3f), mP, new Vector3(90f, 0f, 0f));
            Prim(Cub, body, new Vector3(0f, 0.3f, -0.4f), new Vector3(1.0f, 0.25f, 1.0f), mP);
            Prim(Sph, body, new Vector3(0f, 0.48f, -0.1f), new Vector3(0.32f, 0.25f, 0.5f), Dark);
            Prim(Sph, body, new Vector3(0f, 0.6f, -0.12f), new Vector3(0.2f, 0.2f, 0.2f), mA);
            Prim(Cub, body, new Vector3(0f, 0.12f, 1.45f), new Vector3(1.4f, 0.04f, 0.3f), mA);
            Prim(Cub, body, new Vector3(0f, 0.75f, -1.05f), new Vector3(1.1f, 0.05f, 0.35f), mA);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(Cub, body, new Vector3(s * 0.5f, 0.6f, -1.05f), new Vector3(0.04f, 0.35f, 0.4f), mP);
                Wheel(body, new Vector3(s * 0.72f, 0.26f, 0.95f), 0.26f, 0.3f, wheels);
                Wheel(body, new Vector3(s * 0.72f, 0.28f, -0.85f), 0.28f, 0.36f, wheels);
            }
            lights.Add(Light(body, new Vector3(0f, 0.38f, -1.02f), new Vector3(0.1f, 0.1f, 0.04f), false));
            return FinishCar(root, body, tt, wheels, lights);
        }

        // ================================================================== PIEZAS PEQUEÑAS (se agarran)

        /// <summary>Modelo pequeño sin animador, combinado en una sola malla por material.</summary>
        public static GameObject MiniJet()
        {
            var a = Jet("MiniAvion", JetStyle.Acrobat, new Color(0.95f, 0.80f, 0.40f), new Color(0.35f, 0.55f, 0.85f), 0f, false);
            a.body.localPosition = new Vector3(0f, 0.3f, 0f);
            return Strip(a, 0.16f);
        }

        public static GameObject MiniCar()
        {
            var a = SportsCar("MiniCarro", new Color(0.92f, 0.45f, 0.40f), false);
            return Strip(a, 0.16f);
        }

        public static GameObject DinoEgg()
        {
            var root = Root("HuevoDino");
            Prim(Sph, root.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.24f, 0.32f, 0.24f), M("Huevo", new Color(0.96f, 0.92f, 0.80f)));
            var spot = M("HuevoManchas", new Color(0.55f, 0.72f, 0.50f));
            for (int i = 0; i < 9; i++)
            {
                float th = i * 2.4f, y = 0.08f + (i % 4) * 0.055f;
                float rr = 0.12f * Mathf.Sin(Mathf.PI * (y + 0.02f) / 0.34f) * 0.98f;
                Prim(Sph, root.transform, new Vector3(Mathf.Cos(th) * rr, y, Mathf.Sin(th) * rr), Vector3.one * 0.05f, spot);
            }
            Combine(root.transform, new Transform[0]);
            return root;
        }

        public static GameObject MiniAmmonite()
        {
            var go = AmmoniteRoot("MiniAmonite", 0.9f);
            go.transform.GetChild(0).localPosition = new Vector3(0f, 0.18f, 0f);
            Combine(go.transform, new Transform[0]);
            return go;
        }

        static GameObject Strip(ExhibitAnimator a, float scale)
        {
            var go = a.gameObject;
            if (a.glowParts != null)
                foreach (var g in a.glowParts) if (g) Object.DestroyImmediate(g);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        // ================================================================== DECORACIÓN

        public static GameObject Tree(Transform parent, Vector3 pos, float size, int variant)
        {
            var root = Node("Arbol", parent, pos);
            Seg(Cyl, root, Vector3.zero, new Vector3(0f, 1.6f * size, 0f), 0.16f * size, M("Tronco", new Color(0.55f, 0.40f, 0.30f)));
            Color[] leaves = { new Color(0.50f, 0.74f, 0.45f), new Color(0.42f, 0.66f, 0.48f), new Color(0.62f, 0.78f, 0.42f) };
            var mL = M("Hojas" + variant % 3, leaves[variant % 3]);
            Prim(Sph, root, new Vector3(0f, 2.2f * size, 0f), Vector3.one * 1.9f * size, mL);
            Prim(Sph, root, new Vector3(0.55f * size, 1.9f * size, 0.2f * size), Vector3.one * 1.2f * size, mL);
            Prim(Sph, root, new Vector3(-0.45f * size, 2.0f * size, -0.3f * size), Vector3.one * 1.3f * size, mL);
            Prim(Sph, root, new Vector3(0.1f * size, 2.9f * size, -0.1f * size), Vector3.one * 1.1f * size, mL);
            Combine(root, new Transform[0]);
            MakeStatic(root.gameObject);
            return root.gameObject;
        }

        public static GameObject Bush(Transform parent, Vector3 pos, float size)
        {
            var root = Node("Arbusto", parent, pos);
            var m = M("Arbusto", new Color(0.46f, 0.70f, 0.46f));
            Prim(Sph, root, new Vector3(0f, 0.35f * size, 0f), new Vector3(1.1f, 0.8f, 1f) * size, m);
            Prim(Sph, root, new Vector3(0.45f * size, 0.28f * size, 0.1f), new Vector3(0.8f, 0.6f, 0.8f) * size, m);
            Prim(Sph, root, new Vector3(-0.4f * size, 0.25f * size, -0.1f), new Vector3(0.7f, 0.55f, 0.7f) * size, m);
            Combine(root, new Transform[0]);
            MakeStatic(root.gameObject);
            return root.gameObject;
        }

        public static GameObject Trophy(Transform parent, Vector3 pos)
        {
            var root = Node("Trofeo", parent, pos);
            var gold = Mat("Oro", new Color(0.95f, 0.78f, 0.35f), 0.6f, true, new Color(0.10f, 0.07f, 0.02f));
            Prim(Cub, root, new Vector3(0f, 0.06f, 0f), new Vector3(0.35f, 0.12f, 0.35f), Dark);
            Seg(Cyl, root, new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.32f, 0f), 0.04f, gold);
            Prim(Sph, root, new Vector3(0f, 0.45f, 0f), new Vector3(0.32f, 0.3f, 0.32f), gold);
            Prim(Cyl, root, new Vector3(0f, 0.55f, 0f), new Vector3(0.34f, 0.03f, 0.34f), gold);
            for (int s = -1; s <= 1; s += 2)
                Prim(Cyl, root, new Vector3(s * 0.18f, 0.47f, 0f), new Vector3(0.12f, 0.015f, 0.12f), gold, new Vector3(0f, 0f, 90f));
            Prim(Sph, root, new Vector3(0f, 0.68f, 0f), Vector3.one * 0.12f, M("Estrella", new Color(0.95f, 0.55f, 0.50f)));
            Combine(root, new Transform[0]);
            return root.gameObject;
        }
    }
}
