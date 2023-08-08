
namespace CorgiFoliagePainter
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class FoliageResources : ScriptableObject
    {
        public Material SphereGizmoMaterial;
        public Material InvalidGizmoMaterial;
        public Mesh SphereGizmoMesh;
        public Mesh CubeGizmoMesh;
        public bool IgnoreNoShadergraphWarning;
        public Sprite GizmoFoliageIcon;

        public string ProceduralMeshAssetPath = "Assets";
        public string EditorFoliageMeshDatasPath = "Assets";

#if UNITY_EDITOR
        public static FoliageResources FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:FoliageResources");
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var result = AssetDatabase.LoadAssetAtPath<FoliageResources>(assetPath);
                if (result == null) continue;

                return result;
            }

            var newEditorConfig = FoliageResources.CreateInstance<FoliageResources>();

            var newAssetPath = "Assets/FoliageResources.asset";
            AssetDatabase.CreateAsset(newEditorConfig, newAssetPath);
            AssetDatabase.SaveAssets();
            var newAsset = AssetDatabase.LoadAssetAtPath<FoliageResources>(newAssetPath);

            Debug.Log("[CorgiFoliagePainter] FoliageResources was not found, so one has been created.", newAsset);

            return newAsset;
        }
#endif
    }
}
