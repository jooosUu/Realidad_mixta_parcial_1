using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MuseoAutista.EditorTools
{
    /// <summary>
    /// Permite reemplazar los modelos procedurales por prefabs (por ejemplo, assets
    /// gratuitos de la Asset Store). Asigna un prefab a un id y vuelve a ejecutar
    /// "Museo Autista > 2. Construir escenas". El prefab se escala al tamaño indicado.
    /// </summary>
    [CreateAssetMenu(menuName = "Museo Autista/Model Library")]
    public class ModelLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string descripcion;
            public GameObject prefab;
            [Tooltip("Tamaño máximo (m) al que se escala el prefab.")]
            public float tamano = 2.5f;
            [Tooltip("Giro (grados) para que el modelo mire hacia +Z.")]
            public float giro;
            [Tooltip("Conservar los materiales originales del asset (no pasarlos a estilo toon).")]
            public bool mantenerMateriales;
        }

        public List<Entry> entries = new List<Entry>();

        public const string Path = "Assets/Config/ModelLibrary.asset";

        static readonly (string id, string desc, float size)[] Defaults =
        {
            ("f16", "Avión F-15 (sala aviones)", 2.8f),
            ("mig29", "Avión Rafale (sala aviones)", 2.8f),
            ("f22", "Avión camuflaje desierto (sala aviones)", 2.8f),
            ("f15", "Avión del fondo (sala aviones)", 3.2f),
            ("acrobatico", "Avión colgado del techo", 2.8f),
            ("raptor", "Velociraptor animado", 2.8f),
            ("stegosaurus", "Estegosaurio animado", 4.2f),
            ("pachy", "Paquicefalosaurio animado", 3.0f),
            ("raptor_fosil", "Esqueleto de Velociraptor", 2.6f),
            ("craneo_trex", "Cráneo fósil de T-Rex", 1.3f),
            ("sauropodo_fosil", "Esqueleto de saurópodo", 5.2f),
            ("amanita", "Hongo Amanita (rojo con puntos)", 2.6f),
            ("boletus", "Hongo Boletus", 2.4f),
            ("macrolepiota", "Hongo parasol", 3.2f),
            ("deportivo", "Carro deportivo", 2.4f),
            ("clasico", "Sedán", 2.2f),
            ("jeep", "Camioneta pickup", 2.3f),
            ("f1", "Carro de rally", 2.6f),
        };

        public static ModelLibrary LoadOrCreate()
        {
            var lib = AssetDatabase.LoadAssetAtPath<ModelLibrary>(Path);
            if (lib == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Config")) AssetDatabase.CreateFolder("Assets", "Config");
                lib = CreateInstance<ModelLibrary>();
                AssetDatabase.CreateAsset(lib, Path);
            }
            lib.entries.RemoveAll(e => Array.FindIndex(Defaults, d => d.id == e.id) < 0);
            foreach (var d in Defaults)
            {
                var e = lib.entries.Find(x => x.id == d.id);
                if (e == null) lib.entries.Add(new Entry { id = d.id, descripcion = d.desc, tamano = d.size });
                else { e.descripcion = d.desc; e.tamano = d.size; }
            }
            EditorUtility.SetDirty(lib);
            return lib;
        }

        public Entry Get(string id) => entries.Find(e => e.id == id && e.prefab != null);

        /// <summary>Modelos incluidos en el proyecto: Asset Store (aviones y carros) + Blender (dinos y fósiles).</summary>
        const string Dinos = "Assets/FerociousIndustries/PBRDinosaurs/";
        const string Hongos = "Assets/Low Poly Mushrooms Pack/Prefabs/Mushrooms/";

        /// <summary>Modelos del proyecto: Asset Store (aviones, carros, dinos, hongos) + Blender (fósiles).</summary>
        static readonly (string id, string path, float giro, bool keep)[] Known =
        {
            ("f16", "Assets/Raptor3D/FA_N26/0_Prefabs/FA_N26_Color_1_Prefab.prefab", 0f, false),
            ("mig29", "Assets/POLYGRUNT - Lowpoly Rafale Jet/Prefabs/rafale.prefab", 0f, false),
            ("f22", "Assets/Raptor3D/FA_N26/0_Prefabs/FA_N26_Color_2_Prefab.prefab", 0f, false),
            ("deportivo", "Assets/Free car pack/Prefabs/Plane.011.prefab", 180f, false),
            ("clasico", "Assets/Free car pack/Prefabs/Plane.015.prefab", 180f, false),
            ("jeep", "Assets/Free car pack/Prefabs/Plane.035.prefab", 180f, false),
            ("f1", "Assets/Free car pack/Prefabs/Plane.030.prefab", 180f, false),
            ("raptor", Dinos + "PBRVelociraptor/Prefabs/Mobile/5K/Raptor_Animated_FBX_5K_Green.prefab", 180f, true),
            ("stegosaurus", Dinos + "PBRStegasaurus/Prefabs/LODG/Stegasaurus_Green.prefab", 0f, true),
            ("pachy", Dinos + "PBRPachycephalasaurus/Prefabs/PBR/Pachycephalasaurus_Green.prefab", 0f, true),
            ("raptor_fosil", "Assets/Models/Blender/Raptor_Fosil.fbx", 0f, false),
            ("sauropodo_fosil", "Assets/Models/Blender/Sauropodo_Fosil.fbx", 0f, false),
            ("craneo_trex", "Assets/Models/Blender/CraneoTRex_Fosil.fbx", 0f, false),
            ("amanita", Hongos + "Amanita_big.prefab", 0f, false),
            ("boletus", Hongos + "Boletus_big.prefab", 0f, false),
            ("macrolepiota", Hongos + "Macrolepiota_big.prefab", 0f, false),
        };

        /// <summary>Asigna los modelos conocidos a los huecos vacíos de la librería.</summary>
        [MenuItem("Museo Autista/0. Asignar modelos importados")]
        public static void AutoAssign()
        {
            var lib = LoadOrCreate();
            int n = 0;
            foreach (var e in lib.entries)
                if (e.prefab != null && System.Array.FindIndex(Known, k => k.id == e.id) < 0) e.prefab = null;
            foreach (var k in Known)
            {
                var e = lib.entries.Find(x => x.id == k.id);
                if (e == null) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(k.path);
                if (go == null) { Debug.LogWarning("[MuseoAutista] No existe " + k.path); continue; }
                e.prefab = go;
                e.giro = k.giro;
                e.mantenerMateriales = k.keep;
                n++;
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[MuseoAutista] Modelos asignados: " + n);
        }
    }
}
