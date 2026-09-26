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
            Shoot("Inicio", "inicio_reproductor", new Vector3(0f, 1.6f, 0f), new Vector3(-0.5f, 1.0f, 0.9f), 60f);
            Shoot("Inicio", "inicio_general", new Vector3(0f, 3.5f, -6f), new Vector3(0f, 3f, 12f));
            Shoot("Tutorial", "tutorial_inicio", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 1.4f, 5f));
            Shoot("Tutorial", "tutorial_sala", new Vector3(0f, 1.6f, 5f), new Vector3(0f, 1.3f, 11f));
            Shoot("Recorrido", "recorrido_pasillo", new Vector3(0f, 1.6f, -7f), new Vector3(0f, 1.8f, 10f));
            Shoot("Recorrido", "recorrido_pasillo_mesa", new Vector3(0f, 1.6f, -7f), new Vector3(-2.2f, 0.9f, -2.5f));
            Shoot("Recorrido", "recorrido_pasillo_puertas", new Vector3(0f, 1.6f, 5f), new Vector3(-6f, 3f, 5f));
            HallShot("aviones", "recorrido_aviones", new Vector3(0f, 1.8f, 16f));
            HallShot("dinosaurios", "recorrido_dinos", new Vector3(0f, 1.8f, 16f));
            HallShot("dinosaurios", "recorrido_dinos_raptor", new Vector3(-4.3f, 1.0f, 6.5f));
            HallShot("hongos", "recorrido_hongos", new Vector3(0f, 1.6f, 16f));
            HallShot("carros", "recorrido_carros", new Vector3(0f, 1.3f, 16f));
            Shoot("Recorrido", "recorrido_cenital", new Vector3(0f, 60f, 13f), new Vector3(0f, 0f, 13.01f), 75f);
            Shoot("Felicitaciones", "felicitaciones", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 1.7f, 3f));
            Debug.Log("[MuseoAutista] Capturas guardadas en Screenshots/");
        }

        /// <summary>Simula el rayo Gaze hacia cada pieza de la mesa y cada pedestal para comprobar que se pueden apuntar.</summary>
        public static void GazeCheck()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Recorrido.unity");
            Physics.SyncTransforms();
            Vector3 eye = new Vector3(0f, 1.6f, -7f);
            foreach (var g in Object.FindObjectsByType<MuseoAutista.Grabbable>(FindObjectsSortMode.None))
            {
                Vector3 target = g.transform.position + Vector3.up * 0.2f;
                bool ok = Physics.Raycast(eye, (target - eye).normalized, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Collide)
                          && hit.collider.GetComponentInParent<MuseoAutista.Grabbable>() == g;
                Debug.Log("[GAZE] pieza " + g.itemId + ": " + (ok ? "OK" : "BLOQUEADA por " + (hit.collider ? hit.collider.name : "nada")));
            }
            foreach (var s in Object.FindObjectsByType<MuseoAutista.PlacementSocket>(FindObjectsSortMode.None))
            {
                var hall = s.transform.root.GetComponentsInChildren<Transform>();
                Transform tp = null;
                foreach (var t in hall) if (t.name.EndsWith("_2") && t.name.StartsWith("Salto_") && s.transform.IsChildOf(t.parent)) tp = t;
                Vector3 from = (tp != null ? tp.position : s.transform.position + Vector3.back * 3f) + Vector3.up * 1.6f;
                Vector3 target = s.transform.position + Vector3.up * 0.1f;
                bool ok = Physics.Raycast(from, (target - from).normalized, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Collide)
                          && hit.collider.GetComponentInParent<MuseoAutista.PlacementSocket>() == s;
                Debug.Log("[GAZE] pedestal " + s.acceptsItemId + ": " + (ok ? "OK" : "BLOQUEADO por " + (hit.collider ? hit.collider.name : "nada")));
            }
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

        static void HallShot(string hallId, string file, Vector3 lookLocal)
        {
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Recorrido.unity") EditorSceneManager.OpenScene("Assets/Scenes/Recorrido.unity");
            var hall = GameObject.Find("Sala_" + hallId);
            if (hall == null) return;
            Vector3 from = hall.transform.TransformPoint(new Vector3(0f, 1.6f, 3.5f));
            Shoot("Recorrido", file, from, hall.transform.TransformPoint(lookLocal));
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
