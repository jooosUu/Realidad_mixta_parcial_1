using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using static MuseoAutista.EditorTools.Kit;

namespace MuseoAutista.EditorTools
{
    /// <summary>
    /// Genera las 4 escenas del Museo Autista (Inicio, Tutorial, Recorrido, Felicitaciones),
    /// configura Android + Google Cardboard y compila el APK.
    /// </summary>
    public static class SceneBuilder
    {
        const string SceneDir = "Assets/Scenes";
        const float WallH = 7f;
        const float DoorH = 5.5f;
        const float DoorHalf = 3f;
        const float CorridorHalf = 4f;
        const float HallLen = 18f;
        const float HalfW = 7f;

        static ModelLibrary library;

        // ================================================================== MENÚ

        [MenuItem("Museo Autista/1. Configurar proyecto (Android + Cardboard)")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "JuanJ";
            PlayerSettings.productName = "Museo Autista VR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.juanj.museoautista");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            // Cardboard no soporta GameActivity (predeterminado en Unity 6): usar Activity.
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 2;
                QualitySettings.pixelLightCount = 1;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.vSyncCount = 0;
            }

            ConfigureXR();
            AssetDatabase.SaveAssets();
            Debug.Log("[MuseoAutista] Proyecto configurado para Android + Cardboard.");
        }

        static void ConfigureXR()
        {
            if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = "Android Settings";
                perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, settings);
                AssetDatabase.AddObjectToAsset(settings, perTarget);
            }
            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = "Android Providers";
                AssetDatabase.AddObjectToAsset(manager, perTarget);
                settings.Manager = manager;
            }
            settings.InitManagerOnStart = true;
#pragma warning disable CS0618
            settings.Manager.automaticLoading = true;
            settings.Manager.automaticRunning = true;
#pragma warning restore CS0618
            bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, "Google.XR.Cardboard.XRLoader", BuildTargetGroup.Android);
            Debug.Log("[MuseoAutista] Cardboard XR loader asignado: " + ok);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(perTarget);
        }

        [MenuItem("Museo Autista/2. Construir escenas")]
        public static void BuildScenes()
        {
            Init();
            ModelLibrary.AutoAssign();
            library = ModelLibrary.LoadOrCreate();
            if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets", "Scenes");
            foreach (var f in Directory.GetFiles(Gen + "/Meshes", "*.asset")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            var lighting = LightingAsset();
            CreateSoundBank();
            CreateContemplationAssets();
            var songs = ImportSongs(out var covers);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var (name, build) in new (string, Action)[]
                     {
                         (SceneNames.Inicio, BuildInicio),
                         (SceneNames.Tutorial, BuildTutorial),
                         (SceneNames.Recorrido, BuildRecorrido),
                         (SceneNames.Felicitaciones, BuildFelicitaciones),
                     })
            {
                ScenePrefix = name;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Lightmapping.lightingSettings = lighting;
                build();
                var mp = new GameObject("Musica").AddComponent<MusicPlayer>();
                mp.bundled = songs;
                mp.bundledCovers = covers;
                string path = SceneDir + "/" + name + ".unity";
                EditorSceneManager.SaveScene(scene, path);
                Lightmapping.Bake();
                EditorSceneManager.SaveScene(scene, path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[MuseoAutista] Escenas generadas.");
        }

        [MenuItem("Museo Autista/3. Compilar APK")]
        public static void BuildApk()
        {
            ConfigureProject();
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = "Builds/MuseoAutistaVR.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[MuseoAutista] Resultado APK: " + report.summary.result + " tamaño " + report.summary.totalSize);
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Línea de comandos: configura, construye escenas y captura imágenes de verificación.</summary>
        public static void BatchAll()
        {
            BatchScenes();
            BuildApk();
        }

        public static void BatchApk()
        {
            ConfigureProject();
            BuildApk();
        }

        public static void BatchScenes()
        {
            ConfigureProject();
            BuildScenes();
            Screenshots.CaptureAll();
        }

        static void CreateContemplationAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            const string path = "Assets/Resources/ContemplacionDomo.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("MuseoAutista/Dim")); AssetDatabase.CreateAsset(m, path); }
            m.color = new Color(0.02f, 0.02f, 0.04f, 0f);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Sonidos de la interfaz tomados del pack "Car and transportation sounds collection".</summary>
        static void CreateSoundBank()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            const string path = "Assets/Resources/SoundBank.asset";
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
            if (bank == null) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, path); }
            SoundBank.Entry E(string id, string file, float vol, float pitch = 1f) => new SoundBank.Entry
            {
                id = id, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(CarSounds + file), volume = vol, pitch = pitch
            };
            bank.entries = new[]
            {
                E("click", "ElevatorButton.wav", 0.7f),
                E("grab", "2CV6KeysOut.wav", 0.8f),
                E("place", "2CV6HandbrakeOn.wav", 0.9f),
                E("error", "2CV6MotorNoStart.wav", 0.5f),
                E("teleport", "ElevatorStartToGo.wav", 0.45f),
                E("success", "ElevatorStop.wav", 0.7f),
            };
            EditorUtility.SetDirty(bank);
        }

        /// <summary>
        /// Copia las canciones sueltas (.mp3/.ogg/.wav) de la carpeta "Musica" (junto al proyecto)
        /// a Assets/Audio/Musica y las configura comprimidas y en streaming.
        /// </summary>
        static List<AudioClip> ImportSongs(out List<Texture2D> covers)
        {
            covers = new List<Texture2D>();
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Musica"));
            const string dst = "Assets/Audio/Musica";
            if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
            if (!AssetDatabase.IsValidFolder(dst)) AssetDatabase.CreateFolder("Assets/Audio", "Musica");
            if (Directory.Exists(src))
                foreach (var f in Directory.GetFiles(src))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".mp3" && ext != ".ogg" && ext != ".wav") continue;
                    string target = Path.Combine(dst, Path.GetFileName(f));
                    if (!File.Exists(target)) File.Copy(f, target);
                }
            AssetDatabase.Refresh();
            var clips = new List<AudioClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { dst }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (AudioImporter)AssetImporter.GetAtPath(p);
                var set = imp.defaultSampleSettings;
                if (set.loadType != AudioClipLoadType.Streaming || set.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    set.loadType = AudioClipLoadType.Streaming;
                    set.compressionFormat = AudioCompressionFormat.Vorbis;
                    set.quality = 0.4f;
                    set.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                    imp.defaultSampleSettings = set;
                    imp.SaveAndReimport();
                }
                clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(p));
            }
            clips.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));

            // Portadas incrustadas en los MP3 (etiqueta ID3) -> texturas.
            const string coverDir = "Assets/Audio/Portadas";
            if (!AssetDatabase.IsValidFolder(coverDir)) AssetDatabase.CreateFolder("Assets/Audio", "Portadas");
            foreach (var c in clips)
            {
                Texture2D tex = null;
                byte[] img = Id3Cover.FromFile(Path.GetFullPath(AssetDatabase.GetAssetPath(c)));
                if (img != null)
                {
                    bool png = img.Length > 4 && img[0] == 0x89 && img[1] == 0x50;
                    string cp = coverDir + "/" + c.name + (png ? ".png" : ".jpg");
                    if (!File.Exists(cp))
                    {
                        File.WriteAllBytes(cp, img);
                        AssetDatabase.ImportAsset(cp);
                        var ti = (TextureImporter)AssetImporter.GetAtPath(cp);
                        ti.maxTextureSize = 256;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.SaveAndReimport();
                    }
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(cp);
                }
                covers.Add(tex);
            }
            Debug.Log("[MuseoAutista] Canciones incluidas: " + clips.Count);
            return clips;
        }

        static LightingSettings LightingAsset()
        {
            string path = Gen + "/Iluminacion.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null)
            {
                ls = new LightingSettings();
                AssetDatabase.CreateAsset(ls, path);
            }
            ls.bakedGI = false;
            ls.realtimeGI = false;
            EditorUtility.SetDirty(ls);
            return ls;
        }

        // ================================================================== MATERIALES COMUNES

        static Material Floor(string name, Color c, Vector2 tiling) => Mat(name, c, 0.1f, false, null, FloorTex, tiling);
        static Material WallMat(string name, Color c) => Mat(name, c, 0.08f);
        static Material Trim => Mat("Zocalo", new Color(0.32f, 0.30f, 0.36f));
        static Material Wood => M("Madera", new Color(0.72f, 0.52f, 0.36f));
        static Material PedestalMat => M("Pedestal", new Color(0.93f, 0.91f, 0.86f), 0.2f);

        static Color Pastel(Color c, float t) => Color.Lerp(c, Color.white, t);

        // ================================================================== ESCENA: INICIO

        static void BuildInicio()
        {
            Lighting(new Color(0.72f, 0.80f, 0.92f), new Color(0.72f, 0.72f, 0.70f), new Color(0.45f, 0.50f, 0.40f),
                Skybox("CieloDia", new Color(0.45f, 0.62f, 0.85f), new Color(0.55f, 0.62f, 0.50f), 1.15f));
            var world = new GameObject("Mundo").transform;

            // Suelo: césped + plaza
            Box(world, new Vector3(0f, -0.05f, 10f), new Vector3(90f, 0.1f, 90f), Mat("Cesped", new Color(0.56f, 0.76f, 0.46f), 0.05f, false, null, GrassTex, new Vector2(20f, 20f)));
            Box(world, new Vector3(0f, 0.005f, 7f), new Vector3(12f, 0.02f, 18f), Floor("Plaza", new Color(0.90f, 0.84f, 0.74f), new Vector2(6f, 9f)));

            // Fachada del museo
            var facade = Node("Museo", world, new Vector3(0f, 0f, 20f));
            var wallC = WallMat("FachadaMuseo", new Color(0.97f, 0.90f, 0.78f));
            Box(facade, new Vector3(0f, 4f, 4f), new Vector3(28f, 8f, 8f), wallC);
            Box(facade, new Vector3(0f, 8.4f, 3.6f), new Vector3(29f, 0.8f, 9f), M("Cornisa", new Color(0.55f, 0.72f, 0.82f)));
            Box(facade, new Vector3(0f, 0.25f, -1.2f), new Vector3(18f, 0.5f, 2.6f), PedestalMat);
            for (int i = 0; i < 6; i++)
            {
                float x = -7.5f + i * 3f;
                if (Mathf.Abs(x) < 2f) continue;
                var col = Seg(PrimitiveType.Cylinder, facade, new Vector3(x, 0.5f, -1.4f), new Vector3(x, 8f, -1.4f), 0.35f, M("Columna", new Color(0.98f, 0.96f, 0.92f)));
                MakeStatic(col);
            }
            Box(facade, new Vector3(0f, 2.3f, -0.02f), new Vector3(3.4f, 3.6f, 0.1f), M("Puerta", new Color(0.42f, 0.32f, 0.30f)));
            var sign = WorldCanvas("Letrero", facade, new Vector3(0f, 6.3f, -0.1f), Quaternion.identity, new Vector2(16f, 2.4f), 100f);
            Img(sign.transform, Vector2.zero, new Vector2(1600f, 240f), new Color(0.30f, 0.45f, 0.60f), RoundSprite, true);
            Label(sign.transform, "MUSEO AUTISTA", new Vector2(0f, 20f), new Vector2(1560f, 170f), 140, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(sign.transform, "aviones · dinosaurios · hongos · carros", new Vector2(0f, -85f), new Vector2(1560f, 60f), 46, new Color(1f, 0.93f, 0.75f));

            // Decoración: estatua de T-Rex, avión en poste, carro, árboles
            Place(world, Exhibit("stegosaurus", () => ModelFactory.Stegosaurus("Estatua_Dino", new Color(0.55f, 0.72f, 0.88f), new Color(0.95f, 0.62f, 0.45f)), false),
                new Vector3(7.5f, 0.3f, 10f), -140f, 1f);
            Box(world, new Vector3(7.5f, 0.15f, 10f), new Vector3(4f, 0.3f, 4f), M("PedestalVerde", new Color(0.62f, 0.72f, 0.55f)));
            var jet = Exhibit("f16", () => ModelFactory.Jet("Avion_Poste", ModelFactory.JetStyle.Falcon, new Color(0.62f, 0.74f, 0.88f), new Color(0.92f, 0.52f, 0.45f), 3.2f), false);
            Place(world, jet, new Vector3(-7.5f, 0f, 10f), 150f, 1f);
            jet.transform.Find("Cuerpo").localRotation = Quaternion.Euler(-12f, 0f, 15f);
            Place(world, Exhibit("clasico", () => ModelFactory.Beetle("Carro_Parqueado", new Color(0.50f, 0.80f, 0.78f)), false),
                new Vector3(-5.5f, 0f, 3.5f), 60f, 1f);
            var amPed = Node("PedestalAmonite", world, new Vector3(5.2f, 0f, 3.2f));
            MossPlatform(amPed, Vector3.zero, new Vector2(1.8f, 1.8f), 9);
            Place(amPed, Exhibit("amanita", () => ModelFactory.Ammonite("Amanita_Plaza", 2.5f), false), new Vector3(0f, 0.2f, 0f), -60f, 0.9f);

            int v = 0;
            for (float a = 0; a < 360f; a += 24f)
            {
                float r = 20f + (v % 3) * 3f;
                Vector3 p = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * r, 0f, 8f + Mathf.Cos(a * Mathf.Deg2Rad) * r);
                if (p.z > 14f && Mathf.Abs(p.x) < 16f) continue;
                ModelFactory.Tree(world, p, 1f + (v % 3) * 0.2f, v++);
            }
            for (int i = -1; i <= 1; i += 2)
            {
                ModelFactory.Bush(world, new Vector3(i * 6.5f, 0f, 15.5f), 1.2f);
                ModelFactory.Bush(world, new Vector3(i * 3.2f, 0f, -2.5f), 1f);
            }

            // Menú principal
            Vector3 eye = new Vector3(0f, 1.6f, 0f);
            Vector3 mp = new Vector3(0f, 1.6f, 2.8f);
            var menu = Panel("MenuPrincipal", world, mp, Facing(mp, eye), new Vector2(1.7f, 1.9f));
            AccentBar(menu, new Color(0.95f, 0.72f, 0.45f));
            Label(menu.transform, "MUSEO AUTISTA", new Vector2(0f, 730f), new Vector2(1600f, 170f), 150, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(menu.transform, "Una colección de intereses especiales", new Vector2(0f, 585f), new Vector2(1600f, 100f), 72, PanelSub);
            var buttonsParent = menu.transform;
            ButtonOnPanel("Tutorial", buttonsParent, 380f, ButtonAction.LoadScene, SceneNames.Tutorial, false);
            ButtonOnPanel("Recorrido del museo", buttonsParent, 90f, ButtonAction.LoadScene, SceneNames.Recorrido, false);
            ButtonOnPanel("Salir", buttonsParent, -200f, ButtonAction.Quit, null, true);
            Label(menu.transform, "Mira un botón hasta que se llene\nel anillo o toca el visor.", new Vector2(0f, -610f), new Vector2(1600f, 240f), 70, PanelSub);

            Vector3 lp = new Vector3(-2.7f, 1.6f, 1.3f);
            var how = Panel("ComoUsar", world, lp, Facing(lp, eye), new Vector2(1.5f, 1.6f));
            AccentBar(how, new Color(0.55f, 0.80f, 0.85f));
            Label(how.transform, "Cómo se usa", new Vector2(0f, 630f), new Vector2(1400f, 120f), 90, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(how.transform,
                "• El <b>punto blanco</b> es tu puntero.\n\n" +
                "• Mira un objeto: el <b>anillo</b> se llena y se activa.\n\n" +
                "• Los <b>círculos azules</b> del suelo te teletransportan.\n\n" +
                "• Mantén pulsado el visor para <b>recentrar</b>.\n\n" +
                "• Abajo a la izquierda está el <b>reproductor de música</b>: míralo para abrirlo.",
                new Vector2(0f, -80f), new Vector2(1350f, 1200f), 60, PanelText, TextAnchor.UpperLeft);

            Vector3 rp = new Vector3(2.7f, 1.6f, 1.3f);
            var what = Panel("QueVeras", world, rp, Facing(rp, eye), new Vector2(1.5f, 1.6f));
            AccentBar(what, new Color(0.62f, 0.80f, 0.55f));
            Label(what.transform, "Qué verás", new Vector2(0f, 630f), new Vector2(1400f, 120f), 90, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
            Label(what.transform,
                $"<color=#{Hex(AvionesColor)}>■</color> <b>Aviones de combate</b>\n   F-15, Rafale y F-22\n\n" +
                $"<color=#{Hex(DinosColor)}>■</color> <b>Dinosaurios</b>\n   T-Rex, Triceratops y más\n\n" +
                $"<color=#{Hex(HongosColor)}>■</color> <b>Hongos</b>\n   Amanita, Boletus y un bosque\n\n" +
                $"<color=#{Hex(CarrosColor)}>■</color> <b>Carros</b>\n   Deportivo, sedán, pickup y rally",
                new Vector2(0f, -80f), new Vector2(1350f, 1200f), 56, PanelText, TextAnchor.UpperLeft);

            Rig(Vector3.zero, 0f, null);
        }

        static GazeButton ButtonOnPanel(string label, Transform panel, float yMm, ButtonAction action, string scene, bool danger, float widthM = 1.25f, int fontSize = 0, float heightM = 0.24f)
        {
            // El panel usa escala 1/1000; el botón se coloca en coordenadas del padre del canvas.
            var parent = panel.parent;
            Vector3 world = panel.TransformPoint(new Vector3(0f, yMm, -40f));
            var b = Button(label, parent, parent.InverseTransformPoint(world), panel.localRotation, new Vector2(widthM, heightM), action, scene, danger, fontSize);
            return b;
        }

        // ================================================================== ESCENA: TUTORIAL

        static void BuildTutorial()
        {
            Lighting(new Color(0.78f, 0.80f, 0.88f), new Color(0.72f, 0.70f, 0.68f), new Color(0.50f, 0.48f, 0.46f),
                Skybox("CieloDia", new Color(0.45f, 0.62f, 0.85f), new Color(0.55f, 0.62f, 0.50f), 1.15f));
            var room = new GameObject("SalaTutorial").transform;
            float x0 = -5f, x1 = 5f, z0 = -2f, z1 = 14f, h = 4.5f;
            var wall = WallMat("ParedTutorial", new Color(0.93f, 0.90f, 0.84f));
            Box(room, new Vector3(0f, -0.05f, 6f), new Vector3(10f, 0.1f, 16f), Floor("PisoTutorial", new Color(0.85f, 0.78f, 0.68f), new Vector2(5f, 8f)));
            Box(room, new Vector3(0f, h + 0.05f, 6f), new Vector3(10.3f, 0.1f, 16.3f), Mat("Techo", new Color(0.95f, 0.94f, 0.92f), 0.1f, false, new Color(0.30f, 0.30f, 0.29f)));
            Box(room, new Vector3(x0, h / 2f, 6f), new Vector3(0.3f, h, 16.3f), wall);
            Box(room, new Vector3(x1, h / 2f, 6f), new Vector3(0.3f, h, 16.3f), wall);
            Box(room, new Vector3(0f, h / 2f, z0), new Vector3(10.3f, h, 0.3f), wall);
            Box(room, new Vector3(0f, h / 2f, z1), new Vector3(10.3f, h, 0.3f), wall);
            foreach (float x in new[] { x0 + 0.16f, x1 - 0.16f })
                Box(room, new Vector3(x, 0.6f, 6f), new Vector3(0.02f, 1.2f, 16f), M("ZocaloTutorial", new Color(0.55f, 0.78f, 0.82f)), false);
            for (int i = 0; i < 3; i++)
                Box(room, new Vector3(0f, h - 0.03f, 1f + i * 5f), new Vector3(2.4f, 0.04f, 1.2f), Glow("Lampara", new Color(1f, 0.97f, 0.9f), 1f), false);

            var poster = Panel("Consejos", room, new Vector3(x0 + 0.17f, 2.2f, 4f), Quaternion.Euler(0f, -90f, 0f), new Vector2(2.2f, 1.2f));
            AccentBar(poster, new Color(0.95f, 0.72f, 0.45f));
            Label(poster.transform, "Consejos de confort", new Vector2(0f, 460f), new Vector2(2000f, 100f), 72, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(poster.transform, "• Si te mareas, cierra los ojos y descansa.\n• Mantén pulsado el visor para recentrar.\n• La X arriba a la izquierda vuelve al inicio.",
                new Vector2(0f, -60f), new Vector2(2000f, 800f), 54, PanelText, TextAnchor.MiddleLeft);

            Vector3 start = Vector3.zero;
            var startTp = Teleport(room, start, "tutorial", "Salto_Inicio");
            var target = Teleport(room, new Vector3(0f, 0f, 5f), "tutorial", "Salto_Practica");

            // Paso 3/4: agarrar y colocar
            var grabPed = Node("PedestalAgarre", room, new Vector3(-1.4f, 0f, 7.6f));
            Pedestal(grabPed, 0.9f, new Color(0.55f, 0.78f, 0.82f));
            var egg = MakeGrabbable(ModelFactory.DinoEgg(), room, grabPed.position + Vector3.up * 0.95f, "practica", "Huevo de práctica", "mesa de práctica");
            var sockPed = Node("PedestalColocar", room, new Vector3(1.4f, 0f, 7.6f));
            var socket = MakeSocket(sockPed, "practica", new Color(0.95f, 0.72f, 0.45f));
            var sockSign = Panel("CartelSocket", sockPed, new Vector3(0f, 1.75f, 0f), Facing(sockPed.position, new Vector3(0f, 1.6f, 5f)), new Vector2(1.1f, 0.4f));
            socket.statusText = Label(sockSign.transform, "", Vector2.zero, new Vector2(1050f, 360f), 52, PanelText);
            socket.waitingMessage = "Coloca aquí el huevo";

            // Paso 5: animar un modelo
            var jetPed = Node("PedestalAvion", room, new Vector3(0f, 0f, 11f));
            Pedestal(jetPed, 0.45f, AvionesColor, 0.7f, true);
            var jet = ModelFactory.Jet("Avion_Practica", ModelFactory.JetStyle.Falcon, new Color(0.62f, 0.74f, 0.88f), new Color(0.92f, 0.52f, 0.45f));
            Place(jetPed, jet, new Vector3(0f, 0.45f, 0f), 200f, 1f);

            // Panel guía
            var panelRoot = Node("PanelGuia", room, Vector3.zero);
            var guide = Panel("Guia", panelRoot, Vector3.zero, Quaternion.identity, new Vector2(1.9f, 1.12f));
            AccentBar(guide, new Color(0.55f, 0.80f, 0.85f));
            var counter = Label(guide.transform, "Paso 1", new Vector2(0f, 460f), new Vector2(1700f, 60f), 44, PanelSub);
            var title = Label(guide.transform, "", new Vector2(0f, 350f), new Vector2(1700f, 110f), 86, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            var body = Label(guide.transform, "", new Vector2(0f, 40f), new Vector2(1750f, 500f), 60, PanelText, TextAnchor.MiddleCenter);
            var next = ButtonOnPanel("Siguiente", guide.transform, -380f, ButtonAction.None, null, false, 0.8f);
            var finals = Node("BotonesFinales", panelRoot, Vector3.zero);
            var bRec = ButtonOnPanel("Ir al recorrido", guide.transform, -380f, ButtonAction.LoadScene, SceneNames.Recorrido, false, 0.8f);
            var bIni = ButtonOnPanel("Inicio", guide.transform, -380f, ButtonAction.LoadScene, SceneNames.Inicio, true, 0.5f);
            bRec.transform.SetParent(finals, true);
            bIni.transform.SetParent(finals, true);
            bRec.transform.position += guide.transform.right * -0.35f;
            bIni.transform.position += guide.transform.right * 0.45f;
            Vector3 anchor0 = new Vector3(0f, 1.7f, 2.6f);

            var tc = new GameObject("Tutorial").AddComponent<TutorialController>();
            tc.panel = panelRoot;
            tc.titleText = title;
            tc.bodyText = body;
            tc.stepCounter = counter;
            tc.nextButton = next;
            tc.finalButtons = finals.gameObject;
            tc.teleportTarget = target;

            Transform Anchor(string n, Vector3 p, Vector3 viewer)
            {
                var a = Node(n, room, p);
                a.rotation = Facing(p, viewer);
                return a;
            }
            Vector3 eye0 = new Vector3(0f, 1.6f, 0f), eye1 = new Vector3(0f, 1.6f, 5f);
            var a0 = Anchor("Ancla_Paso1", anchor0, eye0);
            var a2 = Anchor("Ancla_Paso3", new Vector3(0f, 2.15f, 8.6f), eye1);
            var a4 = Anchor("Ancla_Paso5", new Vector3(-2.4f, 1.8f, 8.4f), eye1);
            var a5 = Anchor("Ancla_Paso6", new Vector3(0f, 1.9f, 7.9f), eye1);

            tc.steps = new[]
            {
                new TutorialController.Step { title = "1. El puntero Gaze", panelAnchor = a0, enable = new GameObject[0],
                    body = "El punto blanco en el centro de tu vista es el puntero.\nMira el botón <b>Siguiente</b> y mantén la mirada hasta que el anillo se llene (o toca el visor)." },
                new TutorialController.Step { title = "2. Teletransporte", panelAnchor = a0, enable = new[] { target.gameObject },
                    body = "Mira hacia abajo, al <b>círculo azul</b> del suelo delante de ti, y mantén la mirada para saltar hasta él." },
                new TutorialController.Step { title = "3. Agarrar objetos", panelAnchor = a2, enable = new[] { egg.gameObject },
                    body = "Mira el <b>huevo</b> del pedestal izquierdo para agarrarlo.\nFlotará delante de ti mientras lo llevas." },
                new TutorialController.Step { title = "4. Colocar objetos", panelAnchor = a2, enable = new[] { socket.gameObject },
                    body = "Ahora mira el pedestal derecho con el <b>aro amarillo</b> para colocar el huevo en su lugar." },
                new TutorialController.Step { title = "5. Animar exhibiciones", panelAnchor = a4, enable = new[] { jet.gameObject },
                    body = "Mira el <b>avión</b> del fondo hasta que se llene el anillo:\n¡encenderá sus motores!" },
                new TutorialController.Step { title = "¡Tutorial completado!", panelAnchor = a5, enable = new GameObject[0],
                    body = "Ya sabes mirar, moverte, agarrar, colocar y animar.\n¡Vamos al museo!" },
            };

            Rig(start, 0f, startTp);
        }

        // ================================================================== ESCENA: RECORRIDO

        struct HallDef
        {
            public string id, name, title, subtitle, direction;
            public Color color;
            public float yaw;
            public Vector3 origin;
        }

        static void BuildRecorrido()
        {
            Lighting(new Color(0.80f, 0.82f, 0.90f), new Color(0.74f, 0.72f, 0.70f), new Color(0.52f, 0.50f, 0.48f),
                Skybox("CieloDia", new Color(0.45f, 0.62f, 0.85f), new Color(0.55f, 0.62f, 0.50f), 1.15f), 1.0f);
            var museum = new GameObject("Museo").transform;
            var lobby = Node("Pasillo", museum, Vector3.zero);

            // Pasillo largo con las 4 salas a los lados (entradas enormes).
            const float z0 = -9f, z1 = 35f, zA = 5f, zB = 21f;
            float len = z1 - z0, zc = (z0 + z1) / 2f;
            var corridorWall = WallMat("ParedPasillo", new Color(0.95f, 0.92f, 0.86f));
            Box(lobby, new Vector3(0f, -0.05f, zc), new Vector3(CorridorHalf * 2f, 0.1f, len), Floor("PisoPasillo", new Color(0.93f, 0.86f, 0.74f), new Vector2(2f, 11f)));
            Box(lobby, new Vector3(0f, 0.005f, zc), new Vector3(2.4f, 0.01f, len - 1f), Mat("Alfombra", new Color(0.62f, 0.36f, 0.34f)), false);
            Box(lobby, new Vector3(0f, WallH + 0.05f, zc), new Vector3(CorridorHalf * 2f + 0.3f, 0.1f, len + 0.3f), Mat("Techo", new Color(0.95f, 0.94f, 0.92f), 0.1f, false, new Color(0.30f, 0.30f, 0.29f)));
            for (float z = z0 + 3f; z < z1; z += 5f)
                Box(lobby, new Vector3(0f, WallH - 0.03f, z), new Vector3(2.4f, 0.04f, 1.4f), Glow("Lampara", new Color(1f, 0.97f, 0.9f), 1f), false);
            // tramos de pared del pasillo que no cubren las salas
            foreach (float x in new[] { -CorridorHalf, CorridorHalf })
                foreach (var seg in new[] { new Vector2(z0, zA - HalfW), new Vector2(zA + HalfW, zB - HalfW), new Vector2(zB + HalfW, z1) })
                {
                    float l = seg.y - seg.x;
                    Box(lobby, new Vector3(x, WallH / 2f, (seg.x + seg.y) / 2f), new Vector3(0.3f, WallH, l + 0.3f), corridorWall);
                    Box(lobby, new Vector3(x - Mathf.Sign(x) * 0.16f, 0.6f, (seg.x + seg.y) / 2f), new Vector3(0.02f, 1.2f, l), Trim, false);
                }
            Box(lobby, new Vector3(0f, WallH / 2f, z0), new Vector3(CorridorHalf * 2f + 0.3f, WallH, 0.3f), corridorWall);
            Box(lobby, new Vector3(0f, WallH / 2f, z1), new Vector3(CorridorHalf * 2f + 0.3f, WallH, 0.3f), corridorWall);
            var endSign = WorldCanvas("MuralPasillo", lobby, new Vector3(0f, 4.2f, z1 - 0.17f), Quaternion.identity, new Vector2(7f, 2f), 100f);
            Img(endSign.transform, Vector2.zero, new Vector2(700f, 200f), new Color(0.30f, 0.45f, 0.60f), RoundSprite, true);
            Label(endSign.transform, "MUSEO AUTISTA", new Vector2(0f, 25f), new Vector2(680f, 110f), 80, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(endSign.transform, "aviones · dinosaurios · hongos · carros", new Vector2(0f, -55f), new Vector2(680f, 40f), 30, new Color(1f, 0.93f, 0.75f));

            var halls = new[]
            {
                new HallDef { id = "aviones", name = "Sala de Aviones", title = "AVIONES DE COMBATE", subtitle = "Máquinas que rompen la barrera del sonido", direction = "1ª puerta a la izquierda", color = AvionesColor, yaw = 270f, origin = new Vector3(-CorridorHalf, 0f, zA) },
                new HallDef { id = "dinosaurios", name = "Sala de Dinosaurios", title = "DINOSAURIOS", subtitle = "Gigantes del Mesozoico (muy seguros)", direction = "1ª puerta a la derecha", color = DinosColor, yaw = 90f, origin = new Vector3(CorridorHalf, 0f, zA) },
                new HallDef { id = "hongos", name = "Sala de Hongos", title = "HONGOS", subtitle = "Ni plantas ni animales: un reino aparte", direction = "2ª puerta a la izquierda", color = HongosColor, yaw = 270f, origin = new Vector3(-CorridorHalf, 0f, zB) },
                new HallDef { id = "carros", name = "Sala de Carros", title = "CARROS", subtitle = "Ruedas, motores y velocidad", direction = "2ª puerta a la derecha", color = CarrosColor, yaw = 90f, origin = new Vector3(CorridorHalf, 0f, zB) },
            };

            var sockets = new List<PlacementSocket>();
            var progressTexts = new List<Text>();
            var finishButtons = new List<GameObject>();
            foreach (var h in halls)
                BuildHall(museum, h, sockets, progressTexts, finishButtons);

            // Mesa de restauración con las 4 piezas (a un lado para no tapar el pasillo)
            Vector3 eye = new Vector3(0f, 1.6f, z0 + 2f);
            var table = Node("MesaRestauracion", lobby, new Vector3(-2.2f, 0f, z0 + 6.5f));
            Prim(PrimitiveType.Cylinder, table, new Vector3(0f, 0.43f, 0f), new Vector3(1.6f, 0.43f, 1.6f), Wood, default, true);
            Prim(PrimitiveType.Cylinder, table, new Vector3(0f, 0.88f, 0f), new Vector3(2.1f, 0.03f, 2.1f), M("MesaTapa", new Color(0.85f, 0.70f, 0.55f)), default, true);
            MakeStatic(table.gameObject);
            var items = new (GameObject model, string id, string name, string hall)[]
            {
                (ModelFactory.MiniJet(), "minijet", "Mini avión", "Sala de Aviones"),
                (ModelFactory.DinoEgg(), "huevo", "Huevo de dinosaurio", "Sala de Dinosaurios"),
                (MiniMushroom(), "hongo", "Hongo pequeño", "Sala de Hongos"),
                (ModelFactory.MiniCar(), "minicarro", "Carrito deportivo", "Sala de Carros"),
            };
            for (int i = 0; i < items.Length; i++)
            {
                // En fila, perpendicular a la mirada desde la entrada, para que ninguna tape a otra.
                Vector3 view = (table.localPosition - new Vector3(eye.x, 0f, eye.z)).normalized;
                Vector3 across = new Vector3(view.z, 0f, -view.x);
                Vector3 p = table.localPosition + across * (-0.75f + i * 0.5f) + Vector3.up * 0.91f;
                var g = MakeGrabbable(items[i].model, lobby, p, items[i].id, items[i].name, items[i].hall);
                g.transform.localScale = Vector3.one * 1.25f;
                g.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            }

            // Carteles de la entrada
            Vector3 ip = new Vector3(-2.7f, 2.05f, z0 + 4.2f);
            var info = Panel("Instrucciones", lobby, ip, Facing(ip, eye), new Vector2(2.3f, 1.3f));
            AccentBar(info, new Color(0.55f, 0.80f, 0.85f));
            Label(info.transform, "¡Bienvenido al Museo Autista!", new Vector2(0f, 530f), new Vector2(2200f, 110f), 86, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(info.transform,
                "• Mira los <b>círculos azules</b> del suelo para avanzar por el pasillo.\n" +
                "• Mira un <b>modelo</b> hasta llenar el anillo para animarlo.\n" +
                "• Usa <b>Contemplar</b> en las placas para admirar una pieza.\n" +
                "• La <b>X</b> del visor te lleva al menú principal.",
                new Vector2(0f, -40f), new Vector2(2150f, 950f), 60, PanelText, TextAnchor.MiddleLeft);
            ButtonOnPanel("Menú principal", info.transform, -790f, ButtonAction.LoadScene, SceneNames.Inicio, true, 0.9f);

            Vector3 mp = new Vector3(2.7f, 2.05f, z0 + 4.2f);
            var mission = Panel("Mision", lobby, mp, Facing(mp, eye), new Vector2(2.3f, 1.3f));
            AccentBar(mission, new Color(0.95f, 0.72f, 0.45f));
            Label(mission.transform, "MISIÓN: piezas perdidas", new Vector2(0f, 530f), new Vector2(2200f, 110f), 86, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            string list = "";
            for (int i = 0; i < items.Length; i++)
                list += "• <b>" + items[i].name + "</b>: " + halls[i].direction + "\n";
            Label(mission.transform, "Lleva cada pieza de la mesa al aro\namarillo de su sala:\n" + list,
                new Vector2(0f, 20f), new Vector2(2100f, 800f), 64, PanelText, TextAnchor.MiddleLeft);
            progressTexts.Add(Label(mission.transform, "", new Vector2(0f, -520f), new Vector2(2150f, 130f), 58, new Color(0.25f, 0.45f, 0.30f), TextAnchor.MiddleCenter));
            finishButtons.Add(ButtonOnPanel("Finalizar recorrido", mission.transform, -790f, ButtonAction.LoadScene, SceneNames.Felicitaciones, false, 1.0f).gameObject);

            // Decoración: macetas junto a las puertas y al fondo
            foreach (var c in new[] { new Vector3(-3.1f, 0f, z0 + 1f), new Vector3(3.1f, 0f, z0 + 1f), new Vector3(-3.1f, 0f, 13f), new Vector3(3.1f, 0f, 13f), new Vector3(-3.1f, 0f, z1 - 1f), new Vector3(3.1f, 0f, z1 - 1f) })
            {
                Prim(PrimitiveType.Cylinder, lobby, c + Vector3.up * 0.3f, new Vector3(1.1f, 0.3f, 1.1f), M("Maceta", new Color(0.85f, 0.55f, 0.45f)));
                ModelFactory.Bush(lobby, c + Vector3.up * 0.55f, 1f);
            }

            // Puntos de salto del pasillo
            var start = Teleport(lobby, new Vector3(0f, 0f, z0 + 2f), "pasillo", "Salto_Entrada");
            Teleport(lobby, new Vector3(0f, 0f, zA), "pasillo", "Salto_Puertas1");
            Teleport(lobby, new Vector3(0f, 0f, 13f), "pasillo", "Salto_Centro");
            Teleport(lobby, new Vector3(0f, 0f, zB), "pasillo", "Salto_Puertas2");
            Teleport(lobby, new Vector3(0f, 0f, z1 - 4f), "pasillo", "Salto_Fondo");

            var progress = new GameObject("Progreso").AddComponent<MuseumProgress>();
            progress.sockets = sockets.ToArray();
            progress.progressTexts = progressTexts.ToArray();
            progress.finishButtons = finishButtons.ToArray();

            Rig(start.transform.position - Vector3.up * 0.02f, 0f, start);
        }

        static void BuildHall(Transform museum, HallDef h, List<PlacementSocket> sockets, List<Text> progressTexts, List<GameObject> finishButtons)
        {
            var hall = Node("Sala_" + h.id, museum, h.origin, new Vector3(0f, h.yaw, 0f));
            var wall = WallMat("Pared_" + h.id, Pastel(h.color, 0.72f));
            var wains = Mat("Zocalo_" + h.id, Pastel(h.color, 0.15f), 0.1f);

            Box(hall, new Vector3(0f, -0.05f, HallLen / 2f), new Vector3(14f, 0.1f, HallLen), Floor("Piso_" + h.id, Pastel(h.color, 0.55f), new Vector2(3.5f, 4.5f)));
            Box(hall, new Vector3(0f, WallH + 0.05f, HallLen / 2f), new Vector3(14.3f, 0.1f, HallLen + 0.3f), Mat("Techo", new Color(0.95f, 0.94f, 0.92f), 0.1f, false, new Color(0.30f, 0.30f, 0.29f)));
            // pared hacia el pasillo con una entrada enorme
            float side = HalfW - DoorHalf;
            Box(hall, new Vector3(-(DoorHalf + side / 2f), WallH / 2f, 0f), new Vector3(side + 0.3f, WallH, 0.3f), wall);
            Box(hall, new Vector3(DoorHalf + side / 2f, WallH / 2f, 0f), new Vector3(side + 0.3f, WallH, 0.3f), wall);
            Box(hall, new Vector3(0f, (DoorH + WallH) / 2f, 0f), new Vector3(DoorHalf * 2f + 0.1f, WallH - DoorH, 0.3f), wall);
            // laterales y fondo
            Box(hall, new Vector3(-HalfW, WallH / 2f, HallLen / 2f), new Vector3(0.3f, WallH, HallLen + 0.3f), wall);
            Box(hall, new Vector3(HalfW, WallH / 2f, HallLen / 2f), new Vector3(0.3f, WallH, HallLen + 0.3f), wall);
            Box(hall, new Vector3(0f, WallH / 2f, HallLen), new Vector3(14.3f, WallH, 0.3f), wall);
            // zócalos de color de la sala
            Box(hall, new Vector3(-HalfW + 0.16f, 0.6f, HallLen / 2f), new Vector3(0.02f, 1.2f, HallLen - 0.3f), wains, false);
            Box(hall, new Vector3(HalfW - 0.16f, 0.6f, HallLen / 2f), new Vector3(0.02f, 1.2f, HallLen - 0.3f), wains, false);
            Box(hall, new Vector3(0f, 0.6f, HallLen - 0.16f), new Vector3(13.7f, 1.2f, 0.02f), wains, false);
            // marco de la puerta
            foreach (float x in new[] { -DoorHalf - 0.15f, DoorHalf + 0.15f })
                Box(hall, new Vector3(x, DoorH / 2f, 0f), new Vector3(0.4f, DoorH, 0.7f), wains);
            Box(hall, new Vector3(0f, DoorH + 0.15f, 0f), new Vector3(DoorHalf * 2f + 0.7f, 0.3f, 0.7f), wains);
            for (int i = 0; i < 3; i++)
                Box(hall, new Vector3(0f, WallH - 0.03f, 4f + i * 5f), new Vector3(2.6f, 0.04f, 1.2f), Glow("Lampara", new Color(1f, 0.97f, 0.9f), 1f), false);

            // letrero sobre la puerta (lado vestíbulo) y mural del fondo
            var door = WorldCanvas("LetreroPuerta", hall, new Vector3(0f, (DoorH + 0.3f + WallH) / 2f, -0.37f), Quaternion.identity, new Vector2(6.2f, 1.1f), 100f);
            Img(door.transform, Vector2.zero, new Vector2(620f, 110f), h.color, RoundSprite, true);
            Label(door.transform, h.title, new Vector2(0f, 14f), new Vector2(600f, 70f), 50, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(door.transform, h.name + "  ·  " + h.direction, new Vector2(0f, -35f), new Vector2(600f, 30f), 22, new Color(1f, 0.95f, 0.85f));

            var mural = WorldCanvas("Mural", hall, new Vector3(0f, 4.3f, HallLen - 0.17f), Quaternion.identity, new Vector2(7f, 1.6f), 100f);
            Img(mural.transform, Vector2.zero, new Vector2(700f, 160f), h.color, RoundSprite, true);
            Label(mural.transform, h.title, new Vector2(0f, 25f), new Vector2(680f, 90f), 66, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(mural.transform, h.subtitle, new Vector2(0f, -45f), new Vector2(680f, 40f), 30, new Color(1f, 0.95f, 0.85f));

            // puntos de salto
            Teleport(hall, new Vector3(0f, 0f, 3.5f), h.id, "Salto_" + h.id + "_1");
            Teleport(hall, new Vector3(0f, 0f, 10.5f), h.id, "Salto_" + h.id + "_2");
            Vector3 eye1 = new Vector3(0f, 1.6f, 3.5f), eye2 = new Vector3(0f, 1.6f, 10.5f);

            // exhibiciones
            switch (h.id)
            {
                case "aviones": FillAviones(hall, eye1, eye2); break;
                case "dinosaurios": FillDinos(hall, eye1, eye2); break;
                case "hongos": FillHongos(hall, eye1, eye2); break;
                case "carros": FillCarros(hall, eye1, eye2); break;
            }

            AddContemplateButtons(hall);

            // pedestal de la pieza faltante
            string accepts = h.id == "aviones" ? "minijet" : h.id == "dinosaurios" ? "huevo" : h.id == "hongos" ? "hongo" : "minicarro";
            var sp = Node("PedestalPiezaFaltante", hall, new Vector3(4.3f, 0f, 13f));
            var socket = MakeSocket(sp, accepts, new Color(0.95f, 0.78f, 0.40f));
            sockets.Add(socket);
            Vector3 signPos = new Vector3(2.9f, 1.95f, 12.2f);
            var sign = Panel("CartelPiezaFaltante", hall, signPos, Facing(signPos, eye2), new Vector2(1.3f, 0.75f));
            AccentBar(sign, new Color(0.95f, 0.72f, 0.45f));
            Label(sign.transform, "PIEZA FALTANTE", new Vector2(0f, 280f), new Vector2(1250f, 80f), 58, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            socket.statusText = Label(sign.transform, "", new Vector2(0f, 60f), new Vector2(1250f, 260f), 48, PanelText);
            socket.waitingMessage = "Trae la pieza desde la mesa del pasillo\ny mira el aro amarillo para colocarla.";
            progressTexts.Add(Label(sign.transform, "", new Vector2(0f, -250f), new Vector2(1250f, 150f), 46, new Color(0.25f, 0.45f, 0.30f), TextAnchor.MiddleCenter));
            finishButtons.Add(ButtonOnPanel("Finalizar recorrido", sign.transform, -520f, ButtonAction.LoadScene, SceneNames.Felicitaciones, false, 0.95f).gameObject);
        }

        /// <summary>Agrega a cada placa de la sala un botón "Contemplar" ligado a la pieza más cercana.</summary>
        static void AddContemplateButtons(Transform hall)
        {
            var exhibits = hall.GetComponentsInChildren<ExhibitAnimator>(true);
            foreach (Transform t in hall)
            {
                if (!t.name.StartsWith("Placa_")) continue;
                var canvas = t.Find("Placa");
                if (canvas == null || exhibits.Length == 0) continue;
                ExhibitAnimator best = null;
                float bestD = float.MaxValue;
                foreach (var e in exhibits)
                {
                    Vector3 d = e.transform.position - t.position;
                    d.y *= 0.3f;
                    if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = e; }
                }
                var b = ButtonOnPanel("Contemplar", canvas, -440f, ButtonAction.None, null, false, 0.95f, 52, 0.16f);
                b.normalColor = new Color(0.42f, 0.34f, 0.55f);
                b.hoverColor = new Color(0.56f, 0.46f, 0.72f);
                b.gameObject.AddComponent<ContemplateButton>().target = best;
            }
        }

        // ---------------------------------------------------------------- contenido de cada sala

        static readonly Vector3 L6 = new Vector3(-4.3f, 0f, 6.5f), R6 = new Vector3(4.3f, 0f, 6.5f), L13 = new Vector3(-4.3f, 0f, 13f), C16 = new Vector3(0f, 0f, 15.6f);
        static readonly Vector3 PL6 = new Vector3(-2.3f, 0f, 4.6f), PR6 = new Vector3(2.3f, 0f, 4.6f), PL13 = new Vector3(-2.3f, 0f, 11.4f), PC16 = new Vector3(1.9f, 0f, 13.5f);

        static void FillAviones(Transform hall, Vector3 e1, Vector3 e2)
        {
            Color acc = AvionesColor;
            JetOnPedestal(hall, "f16", L6, 135f, ModelFactory.JetStyle.Falcon, new Color(0.62f, 0.74f, 0.88f), new Color(0.92f, 0.52f, 0.45f));
            Plaque(hall, PL6, e1, "F-15 Eagle", "Caza de superioridad aérea de EE. UU. (1976), con dos motores y dos derivas. Supera Mach 2,5 (más de 2.600 km/h).", acc);
            JetOnPedestal(hall, "mig29", R6, -135f, ModelFactory.JetStyle.TwinTail, new Color(0.60f, 0.82f, 0.72f), new Color(0.40f, 0.55f, 0.70f));
            Plaque(hall, PR6, e1, "Dassault Rafale", "Caza francés con ala en delta y pequeñas alas delanteras (canards) que lo hacen muy ágil. Opera desde tierra y portaaviones.", acc);
            JetOnPedestal(hall, "f22", L13, 135f, ModelFactory.JetStyle.Stealth, new Color(0.70f, 0.68f, 0.82f), new Color(0.45f, 0.45f, 0.60f));
            Plaque(hall, PL13, e2, "Camuflaje desierto", "El mismo tipo de caza pintado con tonos arena: el camuflaje lo esconde cuando vuela bajo sobre el desierto.", acc);
            JetOnPedestal(hall, "f15", C16, 180f, ModelFactory.JetStyle.Stealth, new Color(0.70f, 0.68f, 0.82f), new Color(0.45f, 0.45f, 0.60f), 1.2f);
            Plaque(hall, PC16, e2, "F-22 Raptor (maqueta)", "Caza furtivo (2005): su forma angulada lo hace casi invisible al radar. Vuela supersónico sin postcombustión.", acc);

            // avión acrobático colgado del techo
            var hang = Exhibit("acrobatico", () => ModelFactory.Jet("Avion_Acrobatico", ModelFactory.JetStyle.Acrobat, new Color(0.95f, 0.80f, 0.40f), new Color(0.35f, 0.55f, 0.85f), 0f, false), true);
            var holder = Node("Colgado", hall, new Vector3(0f, 4.7f, 7.5f), new Vector3(0f, 25f, 0f));
            Place(holder, hang, Vector3.zero, 0f, 1f);
            hang.transform.localRotation = Quaternion.Euler(-6f, 0f, 12f);
            var bob = holder.gameObject.AddComponent<SimpleMotion>();
            bob.bobAmplitude = 0.06f;
            bob.bobFrequency = 0.25f;
            foreach (float z in new[] { -0.8f, 0.8f })
            {
                Vector3 p = holder.localPosition + holder.localRotation * new Vector3(0f, 0.2f, z);
                Seg(PrimitiveType.Cylinder, hall, p, new Vector3(p.x, WallH, p.z), 0.008f, Trim);
            }
        }

        static void FillDinos(Transform hall, Vector3 e1, Vector3 e2)
        {
            Color acc = DinosColor;
            DinoPlatform(hall, L6, new Vector2(3.4f, 3.4f));
            Place(hall, Exhibit("raptor", () => ModelFactory.TRex("Raptor", new Color(0.62f, 0.80f, 0.50f), new Color(0.95f, 0.88f, 0.65f)), true), L6 + Vector3.up * 0.25f, 140f, 1f);
            Plaque(hall, PL6, e1, "Velociraptor", "Cazador rápido del Cretácico, del tamaño de un pavo grande. Tenía plumas y una garra curva en cada pie.", acc);
            DinoPlatform(hall, R6, new Vector2(3.4f, 3.4f));
            Place(hall, Exhibit("stegosaurus", () => ModelFactory.Stegosaurus("Estegosaurio", new Color(0.55f, 0.72f, 0.88f), new Color(0.95f, 0.62f, 0.45f)), true), R6 + Vector3.up * 0.25f, -135f, 1f);
            Plaque(hall, PR6, e1, "Stegosaurus", "Jurásico. Placas en la espalda y púas en la cola para defenderse... con un cerebro del tamaño de una nuez.", acc);
            DinoPlatform(hall, L13, new Vector2(3.4f, 3.4f));
            Place(hall, Exhibit("pachy", () => ModelFactory.Triceratops("Paquicefalosaurio", new Color(0.95f, 0.72f, 0.50f), new Color(0.85f, 0.52f, 0.58f)), true), L13 + Vector3.up * 0.25f, 135f, 1f);
            Plaque(hall, PL13, e2, "Pachycephalosaurus", "Su cráneo tenía un domo de hueso de 25 cm de grosor: lo usaba como casco para dar cabezazos.", acc);

            // Fósiles
            Place(hall, Exhibit("sauropodo_fosil", () => ModelFactory.SauropodSkeleton("Esqueleto_Sauropodo"), true), C16, 90f, 1f);
            Plaque(hall, PC16, e2, "Esqueleto de saurópodo", "Dinosaurio de cuello largo. Los huesos de su cuello tenían huecos de aire para pesar menos. Míralo para desarmarlo.", acc);
            var ped = Node("PedestalCraneo", hall, new Vector3(-5.5f, 0f, 9.75f));
            Pedestal(ped, 0.8f, acc, 0.65f);
            Place(ped, Exhibit("craneo_trex", () => ModelFactory.TRexSkull("Craneo_TRex"), true), new Vector3(0f, 0.8f, 0f), 90f, 1f);
            Plaque(hall, new Vector3(-3.4f, 0f, 9.3f), e2, "Cráneo de T-Rex", "Medía 1,5 m. Sus dientes, del tamaño de un plátano, se renovaban durante toda su vida.", acc);
            Place(hall, Exhibit("raptor_fosil", () => ModelFactory.RaptorSkeleton("Esqueleto_Raptor"), true), new Vector3(5.4f, 0f, 9.75f), 0f, 0.85f);
            Plaque(hall, new Vector3(3.4f, 0f, 9.3f), e2, "Fósil de Velociraptor", "Así se ve el raptor de al lado sin piel ni plumas: más de 200 huesos convertidos en piedra.", acc);

            var warn = WorldCanvas("CartelHumor", hall, new Vector3(-6.82f, 3.0f, 6.5f), Quaternion.Euler(0f, -90f, 0f), new Vector2(2.4f, 0.8f));
            Img(warn.transform, Vector2.zero, new Vector2(2400f, 800f), new Color(0.98f, 0.85f, 0.45f), RoundSprite, true);
            Label(warn.transform, "¡POR FAVOR NO ALIMENTE\nA LOS DINOSAURIOS!", Vector2.zero, new Vector2(2300f, 700f), 130, new Color(0.30f, 0.22f, 0.15f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        const string HongosPack = "Assets/Low Poly Mushrooms Pack/Prefabs/";

        /// <summary>Plataforma de musgo con rocas y hongos pequeños alrededor (sin cerca).</summary>
        static void MossPlatform(Transform hall, Vector3 center, Vector2 size, int seed)
        {
            var root = Node("Musgo", hall, center);
            MakeStatic(Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.1f, 0f), new Vector3(size.x, 0.1f, size.y), M("Musgo", new Color(0.45f, 0.62f, 0.38f), 0.1f), default, true));
            MakeStatic(Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.05f, 0f), new Vector3(size.x + 0.15f, 0.05f, size.y + 0.15f), M("TierraMusgo", new Color(0.50f, 0.40f, 0.32f))));
            var rnd = new System.Random(seed);
            string[] small = { "Mushrooms/Champignon_little", "Mushrooms/Lactarius_little", "Mushrooms/ImleriaBadia_little", "Mushrooms/Cantharellaceae_little", "Mushrooms/Leccinum_little" };
            for (int i = 0; i < 5; i++)
            {
                float ang = (float)(i * 72 + rnd.Next(30)) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(ang) * size.x * 0.4f, 0.2f, Mathf.Sin(ang) * size.y * 0.4f);
                if (i % 2 == 0) ModelAt(HongosPack + "Other/Rock" + (1 + rnd.Next(6)) + ".prefab", root, p, 0.45f, rnd.Next(360));
                else ModelAt(HongosPack + small[rnd.Next(small.Length)] + ".prefab", root, p, 0.35f, rnd.Next(360));
            }
        }

        static void FillHongos(Transform hall, Vector3 e1, Vector3 e2)
        {
            Color acc = HongosColor;
            MossPlatform(hall, L6, new Vector2(3.2f, 3.2f), 1);
            Place(hall, Exhibit("amanita", () => ModelFactory.Ammonite("Amanita", 3f), true), L6 + Vector3.up * 0.2f, 0f, 1f);
            Plaque(hall, PL6, e1, "Amanita muscaria", "El hongo de los cuentos: sombrero rojo con puntos blancos. Es venenoso, ¡solo se mira! Míralo para que suelte esporas.", acc);
            MossPlatform(hall, R6, new Vector2(3.2f, 3.2f), 2);
            Place(hall, Exhibit("boletus", () => ModelFactory.Ammonite("Boletus", 3f), true), R6 + Vector3.up * 0.2f, 0f, 1f);
            Plaque(hall, PR6, e1, "Boletus", "Sombrero grueso y pie ancho. Debajo no tiene láminas sino poros, como una esponja.", acc);
            MossPlatform(hall, L13, new Vector2(3.2f, 3.2f), 3);
            Place(hall, Exhibit("macrolepiota", () => ModelFactory.Ammonite("Parasol", 3f), true), L13 + Vector3.up * 0.2f, 0f, 1f);
            Plaque(hall, PL13, e2, "Hongo parasol", "Macrolepiota procera: puede medir 40 cm de alto y su sombrero se abre como una sombrilla.", acc);

            // Bosque de hongos del fondo: varios hongos, troncos y rocas (una sola exhibición).
            MossPlatform(hall, C16, new Vector2(6.5f, 3.0f), 4);
            var bosque = new GameObject("Modelo_bosque");
            var body = Node("Cuerpo", bosque.transform, Vector3.zero);
            ModelAt(HongosPack + "Mushrooms/Agaricales_big.prefab", body, new Vector3(0f, 0f, 0.2f), 2.8f, 20f);
            ModelAt(HongosPack + "Mushrooms/Cantharellaceae_big.prefab", body, new Vector3(-1.6f, 0f, -0.2f), 1.4f, 60f);
            ModelAt(HongosPack + "Mushrooms/Lactarius_big.prefab", body, new Vector3(1.5f, 0f, -0.3f), 1.5f, 10f);
            ModelAt(HongosPack + "Mushrooms/ImleriaBadia_big.prefab", body, new Vector3(-2.5f, 0f, 0.5f), 1.2f, 200f);
            ModelAt(HongosPack + "Mushrooms/Champignon_big.prefab", body, new Vector3(2.5f, 0f, 0.4f), 1.1f, 120f);
            ModelAt(HongosPack + "Other/log.prefab", body, new Vector3(0.9f, 0f, 0.9f), 2.0f, 75f);
            ModelAt(HongosPack + "Mushrooms/Fomes.prefab", body, new Vector3(0.9f, 0.45f, 0.9f), 0.6f, 75f);
            FitCollider(bosque);
            var ba = bosque.AddComponent<ExhibitAnimator>();
            ba.kind = ExhibitKind.Mushroom;
            ba.body = body;
            ba.dwellTime = 1.6f;
            ba.glowParts = new GameObject[0];
            ba.wheels = new Transform[0];
            ba.spores = Spores(bosque.transform, new Vector3(0f, 2.2f, 0f), 2f);
            AssignSound(ba, "bosque");
            Place(hall, ba, C16 + Vector3.up * 0.2f, 180f, 1f);
            Plaque(hall, PC16, e2, "Bosque de hongos", "Los hongos no son plantas: son un reino aparte, más cercano a los animales. Bajo tierra forman redes que miden kilómetros.", acc);
        }

        static void FillCarros(Transform hall, Vector3 e1, Vector3 e2)
        {
            Color acc = CarrosColor;
            Place(hall, Exhibit("deportivo", () => ModelFactory.SportsCar("Deportivo", new Color(0.92f, 0.48f, 0.42f)), true), L6, 135f, 1f);
            Plaque(hall, PL6, e1, "Deportivo", "Carro bajo y aerodinámico con alerón trasero: el aire lo empuja contra el suelo para tomar curvas rápido.", acc);
            Place(hall, Exhibit("clasico", () => ModelFactory.Beetle("Clasico", new Color(0.50f, 0.80f, 0.78f)), true), R6, -135f, 1f);
            Plaque(hall, PR6, e1, "Sedán", "El carro familiar más común: cuatro puertas, maletero separado y espacio para cinco personas.", acc);
            Place(hall, Exhibit("jeep", () => ModelFactory.Jeep("Jeep", new Color(0.95f, 0.80f, 0.40f)), true), L13, 135f, 1f);
            Plaque(hall, PL13, e2, "Camioneta pickup", "Cabina para pasajeros y un platón abierto atrás para cargar: la favorita del campo y las obras.", acc);
            Place(hall, Exhibit("f1", () => ModelFactory.F1("Formula1", new Color(0.35f, 0.45f, 0.75f), new Color(0.95f, 0.60f, 0.45f)), true), C16, 90f, 1f);
            Plaque(hall, PC16, e2, "Carro de rally", "Corre por tierra, nieve y asfalto. Su alerón y suspensión reforzada lo mantienen pegado al camino en saltos y curvas.", acc);
        }

        // ---------------------------------------------------------------- piezas de escenario

        static void JetOnPedestal(Transform hall, string id, Vector3 pos, float yaw, ModelFactory.JetStyle style, Color main, Color acc, float scale = 1f)
        {
            var ped = Node("Pedestal_" + id, hall, pos);
            Pedestal(ped, 0.45f, AvionesColor, 0.7f, true);
            var jet = Exhibit(id, () => ModelFactory.Jet("Avion_" + id, style, main, acc), true);
            Place(ped, jet, new Vector3(0f, 0.45f, 0f), yaw, scale);
        }

        static void Pedestal(Transform parent, float height, Color accent, float radius = 0.5f, bool round = false)
        {
            GameObject g;
            if (round)
                g = Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, height / 2f, 0f), new Vector3(radius * 2f, height / 2f, radius * 2f), PedestalMat, default, true);
            else
                g = Prim(PrimitiveType.Cube, parent, new Vector3(0f, height / 2f, 0f), new Vector3(radius * 2f, height, radius * 2f), PedestalMat, default, true);
            MakeStatic(g);
            var trim = round
                ? Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.06f, 0f), new Vector3(radius * 2f + 0.06f, 0.06f, radius * 2f + 0.06f), M("Trim_" + ColorUtility.ToHtmlStringRGB(accent), accent))
                : Prim(PrimitiveType.Cube, parent, new Vector3(0f, 0.06f, 0f), new Vector3(radius * 2f + 0.06f, 0.12f, radius * 2f + 0.06f), M("Trim_" + ColorUtility.ToHtmlStringRGB(accent), accent));
            MakeStatic(trim);
        }

        static void DinoPlatform(Transform hall, Vector3 center, Vector2 size)
        {
            var root = Node("Recinto", hall, center);
            var grass = M("PastoRecinto", new Color(0.60f, 0.80f, 0.50f), 0.1f);
            MakeStatic(Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.125f, 0f), new Vector3(size.x, 0.25f, size.y), grass, default, true));
            MakeStatic(Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.06f, 0f), new Vector3(size.x + 0.12f, 0.12f, size.y + 0.12f), M("BordeRecinto", new Color(0.72f, 0.62f, 0.50f))));
            // cerca de madera caricaturesca (sin collider)
            var fence = Node("Cerca", root, Vector3.zero);
            float hx = size.x / 2f + 0.25f, hz = size.y / 2f + 0.25f;
            var corners = new[] { new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                int posts = Mathf.CeilToInt(Vector3.Distance(a, b) / 0.9f);
                for (int p = 0; p < posts; p++)
                    Prim(PrimitiveType.Cube, fence, Vector3.Lerp(a, b, p / (float)posts) + Vector3.up * 0.4f, new Vector3(0.1f, 0.8f, 0.1f), Wood);
                Seg(PrimitiveType.Cube, fence, a + Vector3.up * 0.62f, b + Vector3.up * 0.62f, 0.035f, Wood);
                Seg(PrimitiveType.Cube, fence, a + Vector3.up * 0.32f, b + Vector3.up * 0.32f, 0.035f, Wood);
            }
            Combine(fence, new Transform[0]);
            MakeStatic(fence.gameObject);
            var rock = M("Roca", new Color(0.70f, 0.66f, 0.62f));
            MakeStatic(Prim(PrimitiveType.Sphere, root, new Vector3(size.x * 0.36f, 0.3f, size.y * 0.36f), new Vector3(0.5f, 0.35f, 0.45f), rock));
            ModelFactory.Bush(root, new Vector3(-size.x * 0.36f, 0.25f, size.y * 0.38f), 0.7f);
        }

        static void Plaque(Transform hall, Vector3 pos, Vector3 viewer, string title, string body, Color accent)
        {
            var root = Node("Placa_" + title.Replace(" ", "_"), hall, pos);
            root.localRotation = Facing(pos, viewer);
            MakeStatic(Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.5f, 0.05f), new Vector3(0.12f, 0.5f, 0.12f), Trim));
            MakeStatic(Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.5f, 0.02f, 0.5f), Trim));
            var c = Panel("Placa", root, new Vector3(0f, 1.2f, 0f), Quaternion.Euler(25f, 0f, 0f), new Vector2(1.1f, 0.7f));
            Img(c.transform, new Vector2(0f, 300f), new Vector2(1040f, 60f), accent, RoundSprite, true);
            Label(c.transform, title, new Vector2(0f, 190f), new Vector2(1040f, 110f), 74, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(c.transform, body, new Vector2(0f, -35f), new Vector2(1020f, 340f), 48, PanelText, TextAnchor.MiddleCenter);
            Label(c.transform, "Mira el modelo para animarlo", new Vector2(0f, -275f), new Vector2(1000f, 60f), 36, new Color(0.25f, 0.45f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Italic);
        }

        static GameObject MiniMushroom()
        {
            var root = new GameObject("MiniHongo");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HongosPack + "Mushrooms/Amanita_little.prefab");
            if (prefab != null) Model(prefab, root.transform, 0.32f, 0f, false);
            else Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.15f, 0f), Vector3.one * 0.25f, M("HongoMini", new Color(0.85f, 0.3f, 0.3f)));
            return root;
        }

        static Grabbable MakeGrabbable(GameObject model, Transform parent, Vector3 localPos, string id, string name, string hall)
        {
            var root = new GameObject("Pieza_" + id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.2f;
            col.center = new Vector3(0f, 0.15f, 0f);
            var g = root.AddComponent<Grabbable>();
            g.itemId = id;
            g.displayName = name;
            g.hallName = hall;
            g.dwellTime = 1.2f;
            return g;
        }

        static PlacementSocket MakeSocket(Transform parent, string accepts, Color glow)
        {
            Pedestal(parent, 1.0f, glow, 0.42f);
            var root = Node("Receptor", parent, new Vector3(0f, 1.0f, 0f));
            var ring = Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.01f, 0f), new Vector3(0.7f, 0.012f, 0.7f), Glow("AroReceptor", glow, 0.5f));
            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.012f, 0.5f), PedestalMat);
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.2f, 0f);
            col.size = new Vector3(0.9f, 0.5f, 0.9f);
            var anchor = Node("Ancla", root, new Vector3(0f, 0.03f, 0f));
            var s = root.gameObject.AddComponent<PlacementSocket>();
            s.acceptsItemId = accepts;
            s.anchor = anchor;
            s.glow = ring.GetComponent<Renderer>();
            s.dwellTime = 1.0f;
            return s;
        }

        static readonly string[] CarIds = { "deportivo", "clasico", "jeep", "f1" };
        static readonly string[] JetIds = { "f16", "mig29", "f22", "f15", "acrobatico" };
        static readonly string[] MushroomIds = { "amanita", "boletus", "macrolepiota", "bosque" };
        const string CarSounds = "Assets/Car and transportation sounds collection/";
        const string DinoAudio = "Assets/FerociousIndustries/PBRDinosaurs/";

        static ExhibitKind KindOf(string id)
        {
            if (Array.IndexOf(CarIds, id) >= 0) return ExhibitKind.Car;
            if (Array.IndexOf(JetIds, id) >= 0) return ExhibitKind.Jet;
            if (Array.IndexOf(MushroomIds, id) >= 0) return ExhibitKind.Mushroom;
            if (id.Contains("fosil") || id.Contains("craneo")) return ExhibitKind.Fossil;
            return ExhibitKind.Dinosaur;
        }

        /// <summary>Sonido propio de cada exhibición (assets descargados).</summary>
        static void AssignSound(ExhibitAnimator a, string id)
        {
            string path = null;
            float pitch = 1f, vol = 0.8f;
            switch (a.kind)
            {
                case ExhibitKind.Car: path = CarSounds + "2CV6Ignition.wav"; vol = 0.7f; break;
                case ExhibitKind.Jet: path = CarSounds + "TramAcceleratingFaster.wav"; pitch = 1.6f; vol = 0.6f; break;
                case ExhibitKind.Mushroom: path = CarSounds + "ElevatorButton.wav"; pitch = 0.55f; vol = 0.9f; break;
                case ExhibitKind.Dinosaur:
                    path = id == "raptor" ? DinoAudio + "PBRVelociraptor/Audio Clips/RaptorRoar1.wav"
                         : id == "stegosaurus" ? DinoAudio + "PBRStegasaurus/Audio Clips/Roar1.wav"
                         : id == "pachy" ? DinoAudio + "PBRPachycephalasaurus/Audio Clips/Pachycephalosaurus_Roar.wav" : null;
                    break;
            }
            if (path == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) return;
            a.actionClip = clip;
            a.actionPitch = pitch;
            a.actionVolume = vol;
        }

        /// <summary>Instancia un prefab, lo escala a 'size' (dimensión mayor) y lo apoya en y=0 centrado.</summary>
        static GameObject Model(GameObject prefab, Transform parent, float size, float giro, bool keepMaterials)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(parent, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.Euler(0f, giro, 0f) * inst.transform.localRotation;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            // Solo el nivel de detalle principal (algunos LOD lejanos se ven negros con el shader toon).
            foreach (var lg in inst.GetComponentsInChildren<LODGroup>())
            {
                var lods = lg.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                    foreach (var r in lods[i].renderers)
                        if (r != null) UnityEngine.Object.DestroyImmediate(r.gameObject);
                UnityEngine.Object.DestroyImmediate(lg);
            }
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return inst;
            Bounds b = WorldBounds(rs);
            float k = size / Mathf.Max(0.01f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            inst.transform.localScale *= k;
            b = WorldBounds(rs);
            Vector3 center = parent.InverseTransformPoint(b.center);
            float minY = parent.InverseTransformPoint(b.min).y;
            inst.transform.localPosition -= new Vector3(center.x, minY, center.z);
            foreach (var r in rs)
            {
                if (!keepMaterials) r.sharedMaterials = Array.ConvertAll(r.sharedMaterials, ToonFrom);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return inst;
        }

        static GameObject ModelAt(string path, Transform parent, Vector3 pos, float size, float yaw, bool keep = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            var holder = Node(Path.GetFileNameWithoutExtension(path), parent, pos, new Vector3(0f, yaw, 0f));
            Model(prefab, holder, size, 0f, keep);
            return holder.gameObject;
        }

        /// <summary>Controlador de animación: reposo en bucle + rugido con el trigger "Rugir".</summary>
        static RuntimeAnimatorController DinoController(string id, Animator animator)
        {
            if (animator == null || animator.avatar == null) return null;
            string fbx = AssetDatabase.GetAssetPath(animator.avatar);
            var clips = new List<AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) clips.Add(c);
            AnimationClip Pick(params string[] keys)
            {
                foreach (var k in keys)
                    foreach (var c in clips)
                        if (c.name.Contains(k) && !c.name.Contains("_RM")) return c;
                return null;
            }
            var idle = Pick("Idle1", "IdleA", "Idle");
            var roar = Pick("Roar1", "_Roar", "Roar");
            if (idle == null) return null;

            if (!AssetDatabase.IsValidFolder(Gen + "/Anim")) AssetDatabase.CreateFolder(Gen, "Anim");
            string path = Gen + "/Anim/" + id + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Rugir", AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Reposo");
            sIdle.motion = idle;
            sm.defaultState = sIdle;
            // Repite el reposo aunque el clip no venga marcado como bucle.
            var loop = sIdle.AddTransition(sIdle);
            loop.hasExitTime = true;
            loop.exitTime = 0.97f;
            loop.duration = 0.15f;
            if (roar != null)
            {
                var sRoar = sm.AddState("Rugido");
                sRoar.motion = roar;
                var toRoar = sm.AddAnyStateTransition(sRoar);
                toRoar.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Rugir");
                toRoar.duration = 0.2f;
                toRoar.canTransitionToSelf = false;
                var back = sRoar.AddTransition(sIdle);
                back.hasExitTime = true;
                back.exitTime = 0.92f;
                back.duration = 0.3f;
            }
            return ctrl;
        }

        static ParticleSystem Spores(Transform parent, Vector3 pos, float radius)
        {
            var go = new GameObject("Esporas");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.gravityModifier = -0.02f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(0.75f, 1f, 0.7f));
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            return ps;
        }

        /// <summary>Usa el prefab de ModelLibrary si existe; si no, el modelo procedural.</summary>
        static ExhibitAnimator Exhibit(string id, Func<ExhibitAnimator> procedural, bool interactive)
        {
            var entry = library != null ? library.Get(id) : null;
            ExhibitAnimator a;
            if (entry != null)
            {
                var kind = KindOf(id);
                var root = new GameObject("Modelo_" + id);
                Transform turntable = null;
                if (kind == ExhibitKind.Car)
                {
                    Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(3.4f, 0.05f, 3.4f), M("Oscuro", new Color(0.20f, 0.21f, 0.25f)));
                    turntable = Node("Plataforma", root.transform, new Vector3(0f, 0.1f, 0f));
                    Prim(PrimitiveType.Cylinder, turntable, new Vector3(0f, 0.04f, 0f), new Vector3(3.2f, 0.04f, 3.2f), M("Plataforma", new Color(0.82f, 0.82f, 0.86f)));
                }
                bool onStand = kind == ExhibitKind.Jet && id != "acrobatico";
                if (onStand)
                {
                    Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.04f, 0.8f), M("Oscuro", new Color(0.20f, 0.21f, 0.25f)));
                    Seg(PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0f, 0.7f, 0f), 0.06f, M("Oscuro", new Color(0.20f, 0.21f, 0.25f)));
                }
                var body = Node("Cuerpo", turntable != null ? turntable : root.transform,
                    turntable != null ? new Vector3(0f, 0.08f, 0f) : onStand ? new Vector3(0f, 0.62f, 0f) : Vector3.zero);
                var inst = Model(entry.prefab, body, entry.tamano, entry.giro, entry.mantenerMateriales);
                FitCollider(root);
                a = root.AddComponent<ExhibitAnimator>();
                a.body = body;
                a.kind = kind;
                a.dwellTime = 1.6f;
                a.head = FindDeep(inst.transform, "Cabeza");
                a.jaw = FindDeep(inst.transform, "Mandibula");
                a.tail = FindDeep(inst.transform, "Cola");
                var huesos = FindDeep(inst.transform, "Huesos");
                if (huesos != null)
                {
                    var list = new List<Transform>();
                    foreach (Transform t in huesos) list.Add(t);
                    a.bones = list.ToArray();
                }
                if (kind == ExhibitKind.Dinosaur)
                {
                    var anim = inst.GetComponentInChildren<Animator>();
                    var ctrl = DinoController(id, anim);
                    if (ctrl != null)
                    {
                        anim.runtimeAnimatorController = ctrl;
                        anim.applyRootMotion = false;
                        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                        a.animator = anim;
                    }
                }
                if (kind == ExhibitKind.Mushroom)
                {
                    var rs = inst.GetComponentsInChildren<Renderer>();
                    Bounds bb = WorldBounds(rs);
                    a.spores = Spores(root.transform, root.transform.InverseTransformPoint(bb.center + Vector3.up * bb.extents.y * 0.6f), bb.extents.x * 0.8f);
                }
                a.turntable = turntable;
                a.wheels = new Transform[0];
                a.glowParts = new GameObject[0];
                AssignSound(a, id);
            }
            else a = procedural();

            if (!interactive)
            {
                foreach (var c in a.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                a.actionClip = null;
                // Los dinosaurios decorativos mantienen su animación de reposo.
                if (a.kind != ExhibitKind.Dinosaur) a.enabled = false;
            }
            return a;
        }

        static Bounds WorldBounds(Renderer[] rs)
        {
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var f = FindDeep(c, name);
                if (f != null) return f;
            }
            return null;
        }

        static readonly Dictionary<Material, Material> toonCache = new Dictionary<Material, Material>();

        /// <summary>Convierte un material importado al estilo toon con contorno (mantiene color y textura).</summary>
        static Material ToonFrom(Material src)
        {
            if (src == null) return M("Importado", Color.gray);
            if (toonCache.TryGetValue(src, out var m)) return m;
            Color c = src.HasProperty("_Color") ? src.color : Color.white;
            c.a = 1f;
            Texture tex = src.HasProperty("_MainTex") ? src.mainTexture : null;
            m = Mat("Imp_" + src.name.Replace(" ", "_") + "_" + Math.Abs(AssetDatabase.GetAssetPath(src).GetHashCode() % 10000), c, 0.3f, true, null, tex);
            toonCache[src] = m;
            return m;
        }

        static void Place(Transform parent, ExhibitAnimator ex, Vector3 localPos, float yaw, float scale)
        {
            var t = ex.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            t.localScale = Vector3.one * scale;
        }

        // ================================================================== ESCENA: FELICITACIONES

        static void BuildFelicitaciones()
        {
            Lighting(new Color(0.85f, 0.78f, 0.88f), new Color(0.80f, 0.72f, 0.70f), new Color(0.50f, 0.48f, 0.50f),
                Skybox("CieloAtardecer", new Color(0.75f, 0.55f, 0.65f), new Color(0.50f, 0.52f, 0.48f), 1.1f), 1.0f, new Vector3(20f, -60f, 0f));
            var world = new GameObject("Celebracion").transform;
            Box(world, new Vector3(0f, -0.05f, 0f), new Vector3(80f, 0.1f, 80f), Mat("Cesped", new Color(0.56f, 0.76f, 0.46f), 0.05f, false, null, GrassTex, new Vector2(20f, 20f)));
            MakeStatic(Prim(PrimitiveType.Cylinder, world, new Vector3(0f, 0.08f, 3f), new Vector3(12f, 0.08f, 12f), Floor("Escenario", new Color(0.95f, 0.88f, 0.76f), new Vector2(4f, 4f)), default, true));

            // arco de globos caricaturesco
            Color[] balloon = { new Color(0.95f, 0.62f, 0.55f), new Color(0.55f, 0.78f, 0.90f), new Color(0.98f, 0.85f, 0.50f), new Color(0.65f, 0.85f, 0.62f), new Color(0.80f, 0.68f, 0.90f) };
            var arch = Node("ArcoGlobos", world, new Vector3(0f, 0f, 6.5f));
            for (int i = 0; i <= 24; i++)
            {
                float a = Mathf.PI * i / 24f;
                Prim(PrimitiveType.Sphere, arch, new Vector3(Mathf.Cos(a) * 4.8f, 0.3f + Mathf.Sin(a) * 4.6f, 0f), Vector3.one * 0.62f, M("Globo" + i % 5, balloon[i % 5]));
            }
            Combine(arch, new Transform[0]);
            MakeStatic(arch.gameObject);

            Vector3 eye = new Vector3(0f, 1.6f, 0f);
            Vector3 pp = new Vector3(0f, 1.85f, 3.4f);
            var panel = Panel("Felicitaciones", world, pp, Facing(pp, eye), new Vector2(2.3f, 1.75f));
            AccentBar(panel, new Color(0.95f, 0.72f, 0.45f));
            Label(panel.transform, "¡FELICITACIONES!", new Vector2(0f, 690f), new Vector2(2200f, 140f), 120, PanelText, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(panel.transform, "Completaste el recorrido del Museo Autista\ny devolviste todas las piezas perdidas.", new Vector2(0f, 480f), new Vector2(2250f, 180f), 64, PanelSub);
            var stats = Label(panel.transform, "", new Vector2(0f, 180f), new Vector2(2200f, 320f), 62, PanelText);
            ButtonOnPanel("Volver al inicio", panel.transform, -220f, ButtonAction.LoadScene, SceneNames.Inicio, false, 1.3f);
            ButtonOnPanel("Repetir recorrido", panel.transform, -500f, ButtonAction.LoadScene, SceneNames.Recorrido, false, 1.3f);
            ButtonOnPanel("Salir", panel.transform, -760f, ButtonAction.Quit, null, true, 0.8f);
            new GameObject("Resumen").AddComponent<CongratsScreen>().statsText = stats;

            // trofeo y personajes
            var tp = Node("PedestalTrofeo", world, new Vector3(-2.3f, 0.16f, 3.1f));
            Pedestal(tp, 0.9f, new Color(0.95f, 0.72f, 0.45f), 0.35f, true);
            var trophy = ModelFactory.Trophy(tp, new Vector3(0f, 0.9f, 0f));
            trophy.transform.localScale = Vector3.one * 1.3f;
            trophy.AddComponent<SimpleMotion>().spinSpeed = 30f;
            Place(world, Exhibit("raptor", () => ModelFactory.TRex("Raptor_Fiesta", new Color(0.62f, 0.80f, 0.50f), new Color(0.95f, 0.88f, 0.65f)), true), new Vector3(3.3f, 0.16f, 4.2f), -120f, 1f);
            Place(world, Exhibit("deportivo", () => ModelFactory.SportsCar("Carro_Fiesta", new Color(0.92f, 0.48f, 0.42f), false), false), new Vector3(-4.2f, 0.16f, 1.6f), 70f, 1f);
            var jet = Exhibit("acrobatico", () => ModelFactory.Jet("Avion_Fiesta", ModelFactory.JetStyle.Acrobat, new Color(0.95f, 0.80f, 0.40f), new Color(0.35f, 0.55f, 0.85f), 0f, false), false);
            var orbit = Node("Orbita", world, new Vector3(0f, 6f, 6f));
            orbit.gameObject.AddComponent<SimpleMotion>().spinSpeed = 25f;
            Place(orbit, jet, new Vector3(7f, 0f, 0f), 180f, 1f);
            jet.transform.localRotation = Quaternion.Euler(0f, 180f, -25f);

            int v = 0;
            for (float a = 0; a < 360f; a += 30f)
                ModelFactory.Tree(world, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 16f, 0f, 3f + Mathf.Cos(a * Mathf.Deg2Rad) * 16f), 1.1f, v++);

            Confetti(world, new Vector3(0f, 5.5f, 4f));
            Rig(Vector3.zero, 0f, null);
        }

        static void Confetti(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Confeti");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.gravityModifier = 0.08f;
            main.maxParticles = 400;
            main.startRotation3D = false;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.62f, 0.55f), new Color(0.55f, 0.78f, 0.90f)) { mode = ParticleSystemGradientMode.TwoColors };
            var em = ps.emission;
            em.rateOverTime = 40f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 0.5f, 5f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
}
