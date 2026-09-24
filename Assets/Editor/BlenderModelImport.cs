using UnityEditor;

namespace MuseoAutista.EditorTools
{
    /// <summary>Convierte los ejes de Blender (Z arriba) a Unity (Y arriba) al importar los modelos cartoon.</summary>
    public class BlenderModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Models/Blender")) return;
            var mi = (ModelImporter)assetImporter;
            mi.bakeAxisConversion = true;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
        }
    }
}
