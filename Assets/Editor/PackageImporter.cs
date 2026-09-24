using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MuseoAutista.EditorTools
{
    /// <summary>Importa los paquetes gratuitos de la Asset Store ya descargados (aviones y carros).</summary>
    public static class PackageImporter
    {
        static readonly string[] Packages =
        {
            "Raptor3D/3D ModelsVehiclesAir/Fighter Jet Low Poly.unitypackage",
            "Polygrunt Studios/3D ModelsVehiclesAir/POLYGRUNT - Low Poly Rafale Jet.unitypackage",
            "Mighty Handful/3D ModelsVehicles/Cartoon Vehicles - Low Poly Cars FREE.unitypackage",
            "Quint/3D ModelsVehicles/FREE CAR PACK - Low Poly.unitypackage",
        };

        [MenuItem("Museo Autista/Importar paquetes descargados")]
        public static void ImportAll()
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x");
            foreach (var rel in Packages)
            {
                string path = Path.Combine(root, rel);
                if (!File.Exists(path)) { Debug.LogWarning("[MuseoAutista] No encontrado: " + path); continue; }
                AssetDatabase.ImportPackage(path, false);
                Debug.Log("[MuseoAutista] Importado: " + rel);
            }
            AssetDatabase.Refresh();
        }
    }
}
