using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MuseoAutista.EditorTools
{
    /// <summary>Utilidades para construir escenas por código: materiales, primitivas, UI 3D, jugador.</summary>
    public static class Kit
    {
        public const string Gen = "Assets/Generated";

        // Paleta desaturada para evitar fatiga visual.
        public static readonly Color PanelBg = new Color(0.94f, 0.92f, 0.88f, 0.97f);
        public static readonly Color PanelText = new Color(0.13f, 0.15f, 0.18f);
        public static readonly Color PanelSub = new Color(0.30f, 0.33f, 0.37f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.42f, 0.47f);
        public static readonly Color ButtonHover = new Color(0.30f, 0.58f, 0.63f);
        public static readonly Color DangerColor = new Color(0.55f, 0.30f, 0.28f);
        public static readonly Color DangerHover = new Color(0.70f, 0.40f, 0.36f);

        public static readonly Color AvionesColor = new Color(0.36f, 0.45f, 0.56f);
        public static readonly Color DinosColor = new Color(0.45f, 0.55f, 0.42f);
        public static readonly Color FosilesColor = new Color(0.70f, 0.60f, 0.45f);
        public static readonly Color HongosColor = new Color(0.62f, 0.50f, 0.72f);
        public static readonly Color CarrosColor = new Color(0.66f, 0.42f, 0.36f);

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static Sprite DotSprite, RingSprite, FillSprite, RoundSprite;
        public static Sprite NoteIcon, PlayIcon, PauseIcon, PrevIcon, NextIcon, PhoneIcon;
        public static Material OverlayMat, FaderMat;
        public static Texture2D FloorTex, GrassTex;
        static int meshCounter;
        public static string ScenePrefix = "S";

        /// <summary>Fuente de textos (placas, instrucciones): Black Ops One (OFL).</summary>
        public static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BlackOpsOne-Regular.ttf")
                                   ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Fuente de títulos estilo DOOM: AmazDooM (solo letras básicas).</summary>
        public static Font TitleFont => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/AmazDooMLeft.ttf") ?? Font;

        public static void Init()
        {
            mats.Clear();
            foreach (var d in new[] { Gen, Gen + "/Materials", Gen + "/Textures", Gen + "/Meshes" })
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(d));
                }

            DotSprite = MakeSprite("reticle_dot", 128, (x, y, s) => Disc(x, y, s, 0.42f, 0.30f));
            RingSprite = MakeSprite("reticle_ring", 256, (x, y, s) => Ring(x, y, s, 0.46f, 0.36f, true));
            FillSprite = MakeSprite("reticle_fill", 256, (x, y, s) => Ring(x, y, s, 0.44f, 0.38f, false));
            RoundSprite = MakeSprite("round_rect", 64, RoundRect, 24);
            NoteIcon = MakeSprite("icon_note", 128, (x, y, n) => Icon(x, y, n, IconNote));
            PlayIcon = MakeSprite("icon_play", 128, (x, y, n) => Icon(x, y, n, (u, v) => Tri(u, v, 0.3f, 0.75f)));
            PauseIcon = MakeSprite("icon_pause", 128, (x, y, n) => Icon(x, y, n, (u, v) => Bar(u, v, 0.3f, 0.43f) || Bar(u, v, 0.57f, 0.7f)));
            PrevIcon = MakeSprite("icon_prev", 128, (x, y, n) => Icon(x, y, n, (u, v) => Bar(u, v, 0.22f, 0.32f) || Tri(1f - u, v, 0.28f, 0.72f)));
            NextIcon = MakeSprite("icon_next", 128, (x, y, n) => Icon(x, y, n, (u, v) => Bar(u, v, 0.68f, 0.78f) || Tri(u, v, 0.28f, 0.72f)));
            PhoneIcon = MakeSprite("icon_phone", 128, (x, y, n) => Icon(x, y, n, IconPhone));
            FloorTex = MakeTexture("floor_tiles", 512, FloorTile);
            GrassTex = MakeTexture("grass", 256, Grass);

            OverlayMat = OverlayMaterial("GazeOverlay", Color.white, 4000);
            FaderMat = OverlayMaterial("Fader", Color.black, 4010);
        }

        // ------------------------------------------------------------------ materiales

        /// <summary>Material toon. outline=true añade contorno negro (usar en modelos, no en paredes).</summary>
        public static Material Mat(string name, Color c, float rim = 0.25f, bool outline = false,
            Color? emission = null, Texture tex = null, Vector2? tiling = null, float outlineWidth = 0.012f)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = Gen + "/Materials/" + name + ".mat";
            var shader = Shader.Find(outline ? "MuseoAutista/ToonOutline" : "MuseoAutista/Toon");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.color = c;
            m.SetColor("_EmissionColor", emission ?? Color.black);
            m.SetFloat("_RimStrength", rim);
            m.SetColor("_ShadowTint", new Color(0.80f, 0.79f, 0.86f));
            if (outline) m.SetFloat("_Outline", outlineWidth);
            m.mainTexture = tex;
            m.mainTextureScale = tiling ?? Vector2.one;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        /// <summary>Material de modelo (toon con contorno).</summary>
        public static Material M(string name, Color c, float rim = 0.3f) => Mat(name, c, rim, true);

        public static Material Glow(string name, Color c, float intensity = 1f) => Mat(name, c, 0.2f, false, c * intensity);

        static Material OverlayMaterial(string name, Color c, int queue)
        {
            string path = Gen + "/Materials/" + name + ".mat";
            var shader = Shader.Find("MuseoAutista/GazeOverlay");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.color = c;
            m.renderQueue = queue;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Skybox(string name, Color tint, Color ground, float exposure)
        {
            string path = Gen + "/Materials/" + name + ".mat";
            var shader = Shader.Find("Skybox/Procedural");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetFloat("_SunSize", 0.03f);
            m.SetFloat("_AtmosphereThickness", 0.75f);
            m.SetColor("_SkyTint", tint);
            m.SetColor("_GroundColor", ground);
            m.SetFloat("_Exposure", exposure);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ texturas

        delegate Color Painter(int x, int y, int size);

        static Texture2D Paint(int size, Painter p)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = p(x, y, size);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static string WritePng(string name, Texture2D tex)
        {
            string path = Gen + "/Textures/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        static Sprite MakeSprite(string name, int size, Painter p, int border = 0)
        {
            string path = WritePng(name, Paint(size, p));
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D MakeTexture(string name, int size, Painter p)
        {
            string path = WritePng(name, Paint(size, p));
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Aa(float d, float px) => Mathf.Clamp01(0.5f - d / px);

        static Color Disc(int x, int y, int s, float rOuter, float rInner)
        {
            float cx = (x + 0.5f) / s - 0.5f, cy = (y + 0.5f) / s - 0.5f;
            float r = Mathf.Sqrt(cx * cx + cy * cy);
            float px = 1f / s;
            float outline = Aa(r - rOuter, px);
            float fill = Aa(r - rInner, px);
            Color c = Color.Lerp(new Color(0.05f, 0.05f, 0.08f), Color.white, fill);
            c.a = outline;
            return c;
        }

        static Color Ring(int x, int y, int s, float rOuter, float rInner, bool outline)
        {
            float cx = (x + 0.5f) / s - 0.5f, cy = (y + 0.5f) / s - 0.5f;
            float r = Mathf.Sqrt(cx * cx + cy * cy);
            float px = 1f / s;
            float o = outline ? 0.018f : 0f;
            float a = Aa(r - rOuter, px) * Aa(rInner - r, px);
            float aOut = Aa(r - (rOuter + o), px) * Aa((rInner - o) - r, px);
            if (!outline) return new Color(1f, 1f, 1f, a);
            Color c = Color.Lerp(new Color(0.05f, 0.05f, 0.08f), Color.white, a);
            c.a = aOut;
            return c;
        }

        static Color RoundRect(int x, int y, int s)
        {
            float r = 20f;
            float dx = Mathf.Max(0f, Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (s - r)));
            float dy = Mathf.Max(0f, Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (s - r)));
            float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
        }

        static Color Icon(int x, int y, int s, System.Func<float, float, bool> inside)
        {
            // 4x supermuestreo para bordes suaves.
            int hits = 0;
            for (int i = 0; i < 4; i++)
            {
                float u = (x + 0.25f + (i % 2) * 0.5f) / s, v = (y + 0.25f + (i / 2) * 0.5f) / s;
                if (inside(u, v)) hits++;
            }
            return new Color(1f, 1f, 1f, hits / 4f);
        }

        static bool Tri(float u, float v, float x0, float x1)
        {
            if (u < x0 || u > x1) return false;
            float h = 0.25f * (1f - (u - x0) / (x1 - x0));
            return Mathf.Abs(v - 0.5f) <= h;
        }

        static bool Bar(float u, float v, float x0, float x1) => u >= x0 && u <= x1 && v >= 0.25f && v <= 0.75f;

        static bool IconNote(float u, float v)
        {
            bool head1 = (u - 0.33f) * (u - 0.33f) / 0.012f + (v - 0.28f) * (v - 0.28f) / 0.008f <= 1f;
            bool head2 = (u - 0.68f) * (u - 0.68f) / 0.012f + (v - 0.36f) * (v - 0.36f) / 0.008f <= 1f;
            bool stem1 = u >= 0.41f && u <= 0.46f && v >= 0.28f && v <= 0.8f;
            bool stem2 = u >= 0.76f && u <= 0.81f && v >= 0.36f && v <= 0.88f;
            float top = 0.8f + (u - 0.41f) * (0.08f / 0.4f);
            bool beam = u >= 0.41f && u <= 0.81f && v >= top - 0.07f && v <= top;
            return head1 || head2 || stem1 || stem2 || beam;
        }

        static bool IconPhone(float u, float v)
        {
            bool outer = u >= 0.3f && u <= 0.7f && v >= 0.15f && v <= 0.85f;
            bool inner = u >= 0.36f && u <= 0.64f && v >= 0.27f && v <= 0.78f;
            bool button = (u - 0.5f) * (u - 0.5f) + (v - 0.21f) * (v - 0.21f) <= 0.0008f;
            return (outer && !inner && !button) || IconNoteSmall(u, v);
        }

        static bool IconNoteSmall(float u, float v)
        {
            bool head = (u - 0.47f) * (u - 0.47f) / 0.002f + (v - 0.42f) * (v - 0.42f) / 0.0014f <= 1f;
            bool stem = u >= 0.5f && u <= 0.525f && v >= 0.42f && v <= 0.66f;
            return head || stem;
        }

        static Color FloorTile(int x, int y, int s)
        {
            int tiles = 2, tile = s / tiles;
            int tx = x / tile, ty = y / tile;
            float n = Mathf.PerlinNoise(x * 0.03f + tx * 7f, y * 0.03f + ty * 3f) * 0.06f;
            float v = 0.90f + ((tx + ty) % 2 == 0 ? 0.02f : -0.02f) + n;
            int lx = x % tile, ly = y % tile;
            if (lx < 2 || ly < 2 || lx > tile - 3 || ly > tile - 3) v = 0.62f;
            return new Color(v, v, v, 1f);
        }

        static Color Grass(int x, int y, int s)
        {
            float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f + Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.25f;
            float v = 0.85f + n * 0.3f;
            return new Color(v, v, v, 1f);
        }

        // ------------------------------------------------------------------ geometría

        public static Transform Node(string name, Transform parent, Vector3 pos, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go.transform;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material m,
            Vector3 euler = default, bool collider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // El CapsuleCollider de un cilindro ancho y bajo se vuelve una esfera enorme
                // que tapa lo que está encima (p. ej. las piezas de la mesa). Usar la malla real.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>Cilindro o cápsula entre dos puntos locales.</summary>
        public static GameObject Seg(PrimitiveType type, Transform parent, Vector3 a, Vector3 b, float radius, Material m)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            float half = type == PrimitiveType.Capsule ? Mathf.Max(len * 0.5f + radius, radius * 1.01f)
                : type == PrimitiveType.Cube ? len : len * 0.5f;
            var go = Prim(type, parent, (a + b) * 0.5f, new Vector3(radius * 2f, half, radius * 2f), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 center, Vector3 size, Material m, bool collider = true, bool isStatic = true)
        {
            var go = Prim(PrimitiveType.Cube, parent, center, size, m, default, collider);
            if (isStatic) MakeStatic(go);
            return go;
        }

        public static void MakeStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>Añade un BoxCollider que envuelve todos los renderers del objeto (para el Gaze).</summary>
        public static BoxCollider FitCollider(GameObject root, float padding = 0.05f)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var col = root.AddComponent<BoxCollider>();
            Matrix4x4 w2l = root.transform.worldToLocalMatrix;
            Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 l = w2l.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, l);
                max = Vector3.Max(max, l);
            }
            Vector3 s = root.transform.lossyScale;
            Vector3 pad = new Vector3(padding / Mathf.Max(0.001f, s.x), padding / Mathf.Max(0.001f, s.y), padding / Mathf.Max(0.001f, s.z));
            col.center = (min + max) * 0.5f;
            col.size = max - min + pad * 2f;
            return col;
        }

        /// <summary>
        /// Combina las mallas de cada grupo (raíz y pivotes animados) por material,
        /// reduciendo draw calls en móviles.
        /// </summary>
        public static void Combine(Transform root, IEnumerable<Transform> pivots)
        {
            var groups = new HashSet<Transform>(pivots) { root };
            groups.RemoveWhere(g => g == null);
            var buckets = new Dictionary<Transform, Dictionary<Material, List<MeshFilter>>>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (groups.Contains(mf.transform) || mf.transform.childCount > 0) continue;
                Transform g = mf.transform.parent;
                while (g != null && !groups.Contains(g)) g = g.parent;
                if (g == null) continue;
                var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                if (!buckets.TryGetValue(g, out var byMat)) buckets[g] = byMat = new Dictionary<Material, List<MeshFilter>>();
                if (!byMat.TryGetValue(mat, out var list)) byMat[mat] = list = new List<MeshFilter>();
                list.Add(mf);
            }

            foreach (var kv in buckets)
            {
                Transform g = kv.Key;
                foreach (var mk in kv.Value)
                {
                    if (mk.Value.Count < 2) continue;
                    var cis = new List<CombineInstance>();
                    int verts = 0;
                    foreach (var mf in mk.Value)
                    {
                        cis.Add(new CombineInstance
                        {
                            mesh = mf.sharedMesh,
                            transform = g.worldToLocalMatrix * mf.transform.localToWorldMatrix
                        });
                        verts += mf.sharedMesh.vertexCount;
                    }
                    var mesh = new Mesh { name = ScenePrefix + "_" + g.name + "_" + mk.Key.name };
                    if (verts > 65000) mesh.indexFormat = IndexFormat.UInt32;
                    mesh.CombineMeshes(cis.ToArray(), true, true);
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, Gen + "/Meshes/" + ScenePrefix + "_" + (meshCounter++) + ".asset");

                    var go = new GameObject("Combined_" + mk.Key.name);
                    go.transform.SetParent(g, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = mk.Key;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    foreach (var mf in mk.Value) Object.DestroyImmediate(mf.gameObject);
                }
            }
        }

        // ------------------------------------------------------------------ UI 3D

        /// <summary>Rotación para que un panel/canvas quede de frente a un punto de vista.</summary>
        public static Quaternion Facing(Vector3 panelPos, Vector3 viewer)
        {
            Vector3 d = panelPos - viewer;
            d.y = 0f;
            return Quaternion.LookRotation(d.sqrMagnitude < 0.0001f ? Vector3.forward : d.normalized);
        }

        public static Canvas WorldCanvas(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 sizeMeters, float unitsPerMeter = 1000f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = unitsPerMeter >= 1000f ? 3f : 2f;
            var rt = (RectTransform)go.transform;
            rt.localPosition = pos;
            rt.localRotation = rot;
            rt.sizeDelta = sizeMeters * unitsPerMeter;
            rt.localScale = Vector3.one / unitsPerMeter;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = Rect("Image", parent, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Text", parent, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            // Negrita = título: se dibuja con la fuente DOOM (en mayúsculas y sin tildes).
            bool title = style == FontStyle.Bold;
            t.font = title ? TitleFont : Font;
            t.text = title ? TextUtil.Doom(text) : text;
            t.fontSize = title ? Mathf.RoundToInt(fontSize * 1.25f) : Mathf.RoundToInt(fontSize * 0.9f);
            t.color = color;
            t.alignment = align;
            t.fontStyle = title ? FontStyle.Normal : style;
            t.lineSpacing = 1.05f;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        /// <summary>Panel de información (fondo redondeado claro). Tamaño en metros; contenido en mm.</summary>
        public static Canvas Panel(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 sizeM, Color? bg = null, bool collider = true)
        {
            var canvas = WorldCanvas(name, parent, pos, rot, sizeM);
            Img(canvas.transform, Vector2.zero, sizeM * 1000f, bg ?? PanelBg, RoundSprite, true);
            if (collider)
            {
                var col = canvas.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(sizeM.x * 1000f, sizeM.y * 1000f, 20f);
            }
            return canvas;
        }

        public static Image AccentBar(Canvas panel, Color color, float heightMm = 18f)
        {
            var rt = (RectTransform)panel.transform;
            return Img(panel.transform, new Vector2(0f, rt.sizeDelta.y * 0.5f - heightMm * 0.5f - 12f),
                new Vector2(rt.sizeDelta.x - 60f, heightMm), color, RoundSprite, true);
        }

        public static GazeButton Button(string label, Transform parent, Vector3 pos, Quaternion rot, Vector2 sizeM,
            ButtonAction action, string scene = null, bool danger = false, int fontSize = 0)
        {
            var root = new GameObject("Boton_" + label.Replace(" ", "_"));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            var bg = Prim(PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(sizeM.x, sizeM.y, 0.05f),
                danger ? Mat("BotonPeligro", DangerColor, 0.2f) : Mat("Boton", ButtonColor, 0.2f), default, true, "Fondo");

            var canvas = WorldCanvas("Etiqueta", root.transform, new Vector3(0f, 0f, -0.03f), Quaternion.identity, sizeM);
            Label(canvas.transform, label, Vector2.zero, sizeM * 1000f, fontSize > 0 ? fontSize : Mathf.RoundToInt(sizeM.y * 1000f * 0.42f),
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            var gb = root.AddComponent<GazeButton>();
            gb.action = action;
            gb.sceneName = scene;
            gb.background = bg.GetComponent<Renderer>();
            gb.normalColor = danger ? DangerColor : ButtonColor;
            gb.hoverColor = danger ? DangerHover : ButtonHover;
            gb.dwellTime = 1.5f;
            return gb;
        }

        public static GazeButton IconButton(string name, Sprite icon, Transform parent, Vector3 pos, float size, float dwell = 1.0f)
        {
            var b = Button("", parent, pos, Quaternion.identity, new Vector2(size, size), ButtonAction.None);
            b.name = "Boton_" + name;
            var canvas = b.transform.Find("Etiqueta");
            var img = Img(canvas, Vector2.zero, new Vector2(size * 1000f * 0.8f, size * 1000f * 0.8f), Color.white, icon);
            img.name = "Icono";
            b.dwellTime = dwell;
            var bg = b.transform.Find("Fondo");
            bg.localScale = new Vector3(bg.localScale.x, bg.localScale.y, 0.02f);
            canvas.localPosition = new Vector3(0f, 0f, -0.015f);
            return b;
        }

        /// <summary>Reproductor de música en la esquina inferior izquierda (ver MusicPlayerUI).</summary>
        static void MusicUI(Transform rig, Transform head)
        {
            var root = new GameObject("Reproductor").transform;
            root.SetParent(rig, false);
            var anchor = Node("Ancla", root, new Vector3(-0.5f, 1.0f, 0.9f));
            var ui = root.gameObject.AddComponent<MusicPlayerUI>();
            ui.head = head;
            ui.anchor = anchor;
            ui.icon = IconButton("Musica", NoteIcon, anchor, Vector3.zero, 0.15f, 0.8f);

            var panel = Panel("Panel", anchor, Vector3.zero, Quaternion.identity, new Vector2(0.56f, 0.3f), new Color(0.16f, 0.17f, 0.21f, 0.96f));
            ui.panel = panel.gameObject;
            // Pantallita con la portada de la canción, encima del panel.
            Img(panel.transform, new Vector2(0f, 300f), new Vector2(270f, 270f), new Color(0.10f, 0.10f, 0.13f, 1f), RoundSprite, true);
            var coverRt = Rect("Portada", panel.transform, new Vector2(0f, 300f), new Vector2(240f, 240f));
            ui.cover = coverRt.gameObject.AddComponent<RawImage>();
            ui.cover.raycastTarget = false;
            ui.defaultCover = NoteIcon.texture;
            var col = panel.GetComponent<BoxCollider>();
            col.center = new Vector3(0f, 140f, 0f);
            col.size = new Vector3(560f, 600f, 20f);
            ui.title = Label(panel.transform, "", new Vector2(0f, 95f), new Vector2(520f, 60f), 38, Color.white);
            ui.title.resizeTextForBestFit = true;
            ui.title.resizeTextMinSize = 18;
            ui.title.resizeTextMaxSize = 34;
            ui.title.verticalOverflow = VerticalWrapMode.Truncate;
            ui.status = Label(panel.transform, "", new Vector2(0f, 55f), new Vector2(520f, 30f), 24, new Color(0.7f, 0.85f, 0.9f));
            float y = -45f;
            ui.prev = IconButton("Anterior", PrevIcon, panel.transform, Vector3.zero, 0.1f);
            ui.playPause = IconButton("PlayPausa", PauseIcon, panel.transform, Vector3.zero, 0.1f);
            ui.next = IconButton("Siguiente", NextIcon, panel.transform, Vector3.zero, 0.1f);
            ui.phone = IconButton("Celular", PhoneIcon, panel.transform, Vector3.zero, 0.1f);
            var list = new[] { ui.prev, ui.playPause, ui.next, ui.phone };
            for (int i = 0; i < list.Length; i++)
            {
                var t = list[i].transform;
                t.SetParent(panel.transform.parent, false);
                t.localPosition = new Vector3(-0.195f + i * 0.13f, y / 1000f, -0.03f);
                t.SetParent(panel.transform, true);
            }
            ui.phone.normalColor = new Color(0.35f, 0.35f, 0.45f);
            ui.playPauseImage = ui.playPause.transform.Find("Etiqueta/Icono").GetComponent<Image>();
            ui.playSprite = PlayIcon;
            ui.pauseSprite = PauseIcon;
        }

        // ------------------------------------------------------------------ jugador y escena

        public static PlayerRig Rig(Vector3 pos, float yaw, TeleportPoint start)
        {
            var rig = new GameObject("PlayerRig");
            rig.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();

            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            tpd.ignoreTrackingState = true;
            tpd.rotationInput = new InputActionProperty(new InputAction("Rotacion", InputActionType.Value,
                "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            tpd.positionInput = new InputActionProperty(new InputAction("Posicion", InputActionType.Value,
                "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));

            camGo.AddComponent<EditorMouseLook>();

            // Retículo Gaze
            var reticle = WorldCanvas("Reticulo", camGo.transform, new Vector3(0f, 0f, 3f), Quaternion.identity, new Vector2(0.1f, 0.1f));
            ((RectTransform)reticle.transform).sizeDelta = new Vector2(100f, 100f);
            reticle.sortingOrder = 500;
            var fill = Img(reticle.transform, Vector2.zero, new Vector2(100f, 100f), new Color(0.35f, 0.85f, 0.95f), FillSprite);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.material = OverlayMat;
            fill.name = "Relleno";
            var ring = Img(reticle.transform, Vector2.zero, new Vector2(100f, 100f), new Color(1f, 1f, 1f, 0f), RingSprite);
            ring.material = OverlayMat;
            ring.name = "Anillo";
            var dot = Img(reticle.transform, Vector2.zero, new Vector2(26f, 26f), Color.white, DotSprite);
            dot.material = OverlayMat;
            dot.name = "Punto";

            var gaze = camGo.AddComponent<GazePointer>();
            gaze.reticle = reticle.transform;
            gaze.dot = dot;
            gaze.ring = ring;
            gaze.fill = fill;

            var hold = Node("PuntoDeAgarre", camGo.transform, new Vector3(0f, -0.28f, 0.85f));

            var faderQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faderQuad.name = "Fundido";
            Object.DestroyImmediate(faderQuad.GetComponent<Collider>());
            faderQuad.transform.SetParent(camGo.transform, false);
            faderQuad.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            faderQuad.transform.localScale = new Vector3(2f, 2f, 1f);
            var fr = faderQuad.GetComponent<MeshRenderer>();
            fr.sharedMaterial = FaderMat;
            fr.shadowCastingMode = ShadowCastingMode.Off;
            var fader = faderQuad.AddComponent<ScreenFader>();
            fader.quad = fr;

            var pr = rig.AddComponent<PlayerRig>();
            pr.head = camGo.transform;
            pr.holdPoint = hold;
            pr.fader = fader;
            pr.startPoint = start;

            MusicUI(rig.transform, camGo.transform);

            new GameObject("Cardboard").AddComponent<CardboardManager>();
            return pr;
        }

        public static TeleportPoint Teleport(Transform parent, Vector3 pos, string hallId, string name = "PuntoDeSalto")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos + Vector3.up * 0.02f;

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.7f;
            col.height = 0.3f;
            col.center = new Vector3(0f, 0.1f, 0f);
            col.isTrigger = true;

            var visual = Node("Visual", root.transform, Vector3.zero);
            var ringMat = Glow("TeleportRing", new Color(0.35f, 0.75f, 0.85f), 0.6f);
            Prim(PrimitiveType.Cylinder, visual, Vector3.zero, new Vector3(1.2f, 0.01f, 1.2f), Mat("TeleportBase", new Color(0.18f, 0.22f, 0.26f)));
            var ring = Prim(PrimitiveType.Cylinder, visual, new Vector3(0f, 0.012f, 0f), new Vector3(1.05f, 0.01f, 1.05f), ringMat);
            Prim(PrimitiveType.Cylinder, visual, new Vector3(0f, 0.02f, 0f), new Vector3(0.85f, 0.01f, 0.85f), Mat("TeleportInner", new Color(0.12f, 0.14f, 0.17f)));
            Prim(PrimitiveType.Cylinder, visual, new Vector3(0f, 0.024f, 0f), new Vector3(0.28f, 0.01f, 0.28f), ringMat);

            var tp = root.AddComponent<TeleportPoint>();
            tp.hallId = hallId;
            tp.visual = visual;
            tp.ring = ring.GetComponent<Renderer>();
            tp.dwellTime = 1.1f;
            return tp;
        }

        public static void Lighting(Color sky, Color equator, Color ground, Material skybox, float sunIntensity = 1.05f, Vector3? sunEuler = null)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;
            RenderSettings.fog = false;

            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.90f);
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(sunEuler ?? new Vector3(50f, -35f, 0f));
            RenderSettings.sun = sun;

            var fill = new GameObject("Relleno").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.90f, 1f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForceVertex;
            fill.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
        }
    }
}
