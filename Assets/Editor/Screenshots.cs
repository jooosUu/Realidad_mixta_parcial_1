using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseoAutista.EditorTools
{
    /// <summary>Renderiza vistas de cada escena a PNG (carpeta Screenshots/) para revisar el diseño.</summary>
    public static class Screenshots
    {
        [MenuItem("Museo Autista/4. Capturas de verificación")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory("Screenshots");
            Shoot("Inicio", "inicio_menu", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 1.5f, 3f));
            Shoot("Inicio", "inicio_general", new Vector3(0f, 3.5f, -6f), new Vector3(0f, 3f, 12f));
            Shoot("Tutorial", "tutorial_inicio", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 1.4f, 5f));
            Shoot("Tutorial", "tutorial_sala", new Vector3(0f, 1.6f, 5f), new Vector3(0f, 1.3f, 11f));
            Shoot("Recorrido", "recorrido_vestibulo", new Vector3(0f, 1.6f, -4.2f), new Vector3(0f, 1.4f, 3f));
            Shoot("Recorrido", "recorrido_aviones", new Vector3(0f, 1.6f, 10.5f), new Vector3(0f, 1.8f, 20f));
            Shoot("Recorrido", "recorrido_aviones_2", new Vector3(0f, 1.6f, 10.5f), new Vector3(-4f, 1.4f, 11f));
            Shoot("Recorrido", "recorrido_dinos", new Vector3(10.5f, 1.6f, 0f), new Vector3(20f, 1.8f, 0f));
            Shoot("Recorrido", "recorrido_dinos_trex", new Vector3(10.5f, 1.6f, 0f), new Vector3(13.5f, 1.4f, 4.3f));
            Shoot("Recorrido", "recorrido_dinos_raptor", new Vector3(11f, 1.8f, 1.5f), new Vector3(13.5f, 0.9f, 4.3f));
            Shoot("Recorrido", "recorrido_dinos_stego", new Vector3(11f, 1.8f, -1.5f), new Vector3(13.5f, 1.0f, -4.3f));
            Shoot("Recorrido", "recorrido_fosiles", new Vector3(0f, 1.6f, -10.5f), new Vector3(0f, 1.5f, -20f));
            Shoot("Recorrido", "recorrido_fosiles_raptor", new Vector3(0f, 1.6f, -10.5f), new Vector3(4f, 1.2f, -13.5f));
            Shoot("Recorrido", "recorrido_carros", new Vector3(-10.5f, 1.6f, 0f), new Vector3(-20f, 1.3f, 0f));
            Shoot("Recorrido", "recorrido_carros_2", new Vector3(-10.5f, 1.6f, 0f), new Vector3(-13.5f, 1.0f, -4.3f));
            Shoot("Recorrido", "recorrido_cenital", new Vector3(0f, 45f, 0f), new Vector3(0f, 0f, 0.01f), 70f);
            Shoot("Felicitaciones", "felicitaciones", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 1.7f, 3f));
            Debug.Log("[MuseoAutista] Capturas guardadas en Screenshots/");
        }

        public static void Debug1()
        {
            Directory.CreateDirectory("Screenshots");
            Shoot("Recorrido", "debug_raptor", new Vector3(2.5f, 2.2f, -11.5f), new Vector3(4.3f, 0.8f, -13.5f));
            var go = GameObject.Find("Modelo_raptor_fosil");
            if (go != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    Debug.Log("[DBG] " + r.name + " " + r.enabled + " " + r.gameObject.activeInHierarchy + " " + r.bounds.center + " " + r.bounds.size);
        }

        public static void PreviewPrefabs()
        {
            Directory.CreateDirectory("Screenshots/modelos");
            var paths = new System.Collections.Generic.List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Low Poly Mushrooms Pack/Prefabs" })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Add("Assets/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab");
            paths.Add("Assets/FerociousIndustries/PBRDinosaurs/PBRStegasaurus/Prefabs/LODG/Stegasaurus_Green.prefab");
            paths.Add("Assets/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Prefabs/PBR/Pachycephalasaurus_Green.prefab");
            foreach (var path in paths)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var cam = Camera.main;
                float d = b.extents.magnitude * 2.2f;
                cam.transform.position = b.center + new Vector3(1f, 0.4f, 0.35f).normalized * d;
                cam.transform.LookAt(b.center);
                cam.farClipPlane = d * 4f;
                var rt = new RenderTexture(512, 384, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(512, 384, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 512, 384), 0, 0);
                string name = Path.GetFileNameWithoutExtension(path) + "_" + b.size.ToString("F1").Replace(" ", "");
                File.WriteAllBytes("Screenshots/modelos/" + name + ".png", tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
        }

        static void Shoot(string scene, string file, Vector3 pos, Vector3 look, float fov = 80f)
        {
            string path = "Assets/Scenes/" + scene + ".unity";
            if (EditorSceneManager.GetActiveScene().path != path) EditorSceneManager.OpenScene(path);

            var go = new GameObject("CamCaptura");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.transform.position = pos;
            go.transform.LookAt(look);

            // Oculta el fundido negro y el retículo del jugador durante la captura.
            var fader = GameObject.Find("Fundido");
            if (fader) fader.SetActive(false);

            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes("Screenshots/" + file + ".png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            if (fader) fader.SetActive(true);
        }
    }
}
