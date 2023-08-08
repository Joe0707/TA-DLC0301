#if UNITY_EDITOR
namespace CorgiFoliagePainter.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(FoliageMeshData))]
    public class FoliageMeshDataEditor : Editor
    {
        private SerializedProperty LODs;
        private SerializedProperty DefaultRandomColors;
        private SerializedProperty DefaultBaseScale;
        private SerializedProperty DefaultBaseRotationEulor;
        private SerializedProperty DefaultRandomScale;
        private SerializedProperty DefaultRandomRotationEulor;
        private SerializedProperty ExtraToolsPrefabPlacerSettings;

        private void OnEnable()
        {
            LODs = serializedObject.FindProperty("LODs");
            DefaultRandomColors = serializedObject.FindProperty("DefaultRandomColors");
            DefaultBaseScale = serializedObject.FindProperty("DefaultBaseScale");
            DefaultBaseRotationEulor = serializedObject.FindProperty("DefaultBaseRotationEulor");
            DefaultRandomScale = serializedObject.FindProperty("DefaultRandomScale");
            DefaultRandomRotationEulor = serializedObject.FindProperty("DefaultRandomRotationEulor");
            ExtraToolsPrefabPlacerSettings = serializedObject.FindProperty("ExtraToolsPrefabPlacerSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var instance = (FoliageMeshData) target;

            EditorGUILayout.PropertyField(LODs);
            EditorGUILayout.PropertyField(DefaultRandomColors);
            EditorGUILayout.PropertyField(DefaultBaseScale);
            EditorGUILayout.PropertyField(DefaultBaseRotationEulor);
            EditorGUILayout.PropertyField(DefaultRandomScale);
            EditorGUILayout.PropertyField(DefaultRandomRotationEulor);

            DrawExtraToolsSettings(instance);

            DrawWarnings(instance);
            DrawToolsFoldout(instance);

            var changed = serializedObject.ApplyModifiedProperties();
            if(changed)
            {
                EditorUtility.SetDirty(instance);

                var renderManager = FoliageRenderingManager.GetInstance(false);
                if (renderManager != null)
                {
                    var foliageGroups = renderManager.GetRegisteredFoliageGroups();
                    foreach(var group in foliageGroups)
                    {
                        group.SetDirty(); 
                    }
                }
            }
        }

        private void DrawWarnings(FoliageMeshData instance)
        {
            GUILayout.BeginVertical();

            var prevLodDistance = 0f;
            var prevVertexCount = int.MaxValue;

            for(var l = 0; l < instance.LODs.Length; ++l)
            {
                var lod = instance.LODs[l];
                if(lod.distance <= 0f)
                {
                    EditorGUILayout.HelpBox($"LOD{l} has an invalid distance", MessageType.Error); 
                }

                if(lod.distance < prevLodDistance)
                {
                    EditorGUILayout.HelpBox($"LOD{l} has a lower LOD distance than it's previous LOD ({lod.distance} < {prevLodDistance})", MessageType.Error); 
                }

                if(lod.RenderData == null)
                {
                    EditorGUILayout.HelpBox($"LOD{l} has a null RenderData array?! You may need to go into Unity's Debug Inspector to fix this.", MessageType.Error);
                    continue;
                }

                var lodVertexCount = 0;

                for(var r = 0; r < lod.RenderData.Length; ++r)
                {
                    var renderData = lod.RenderData[r];

                    if (renderData.mesh == null)
                    {
                        EditorGUILayout.HelpBox($"LOD{l} does not have a mesh assigned.", MessageType.Error);
                    }
                    else
                    {
                        if (!renderData.mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position))
                        {
                            EditorGUILayout.HelpBox($"LOD{l}'s mesh {renderData.mesh.name} does not have position/vertex data.", MessageType.Error);
                        }

                        if (!renderData.mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
                        {
                            EditorGUILayout.HelpBox($"LOD{l}'s mesh {renderData.mesh.name} does not have UV0 data.", MessageType.Info);
                        }


                        lodVertexCount += renderData.mesh.vertexCount;
                    }

                    if (renderData.material == null)
                    {
                        EditorGUILayout.HelpBox($"FoliageMaterial is null.", MessageType.Error);
                    }
                    else
                    {
                        if (!renderData.material.enableInstancing)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox($"The shader used by LOD{l}'s RenderData{r}'s material {renderData.material.name} does not have GPU Instancing enabled.", MessageType.Error);
                            if (GUILayout.Button("fix it"))
                            {
                                Undo.RecordObject(renderData.material, "enabled instancing on material");
                                renderData.material.enableInstancing = true;
                                EditorUtility.SetDirty(instance);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    if(renderData.localScale.magnitude < 0.001f)
                    {
                        EditorGUILayout.HelpBox($"LOD{l}'s RenderData{r} has a localScale of or near zero. Is this a mistake?", MessageType.Warning);
                    }
                }

                if (lodVertexCount > prevVertexCount)
                {
                    EditorGUILayout.HelpBox($"LOD{l}'s meshes have more vertices than the previous LOD's meshes. ({lodVertexCount} > {prevVertexCount})", MessageType.Warning);
                }

                prevVertexCount = lodVertexCount;
                prevLodDistance = lod.distance;
            }

            GUILayout.EndVertical();
        }

        [System.NonSerialized] private bool _toolsFoldout;
        [System.NonSerialized] private bool _prefabImportWizardFoldout;
        [System.NonSerialized] private PrefabImportWizardSettings _prefabImportWizardSettings = new PrefabImportWizardSettings();

        private class PrefabImportWizardSettings
        {
            public GameObject prefab;
            public float MinimumDistance = 8f;
            public float MaximumDistance = 64f;
        }

        [System.NonSerialized] private bool _extraToolsFoldout;

        private void DrawExtraToolsSettings(FoliageMeshData instance)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("GroupBox");
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(8f, false);
                _extraToolsFoldout = EditorGUILayout.Foldout(_extraToolsFoldout, "Extra Tools Settings", true);
                EditorGUILayout.EndHorizontal();

                if (_extraToolsFoldout)
                {
                    EditorGUILayout.PropertyField(ExtraToolsPrefabPlacerSettings); 

                    if(instance.ExtraToolsPrefabPlacerSettings.prefab != null)
                    {
                        EditorGUILayout.HelpBox("This prefab is only used if you use the Extra Tools -> Prefab Placer tool on a FoliageGroup. " +
                            "However, please note that this prefab will still be pulled into memory even if you do not bake with it. " +
                            "So, please remove this reference if you do not need it for prefab placement in FoliageGroups.", MessageType.Info, true); 
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToolsFoldout(FoliageMeshData instance)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("GroupBox");
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(8f, false);
                _toolsFoldout = EditorGUILayout.Foldout(_toolsFoldout, "Tools", true);
                EditorGUILayout.EndHorizontal();

                if(_toolsFoldout)
                {
                    if(DrawPrefabImportWizard(instance))
                    {
                        return;
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Returns true if a dialog was displayed. 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private bool DrawPrefabImportWizard(FoliageMeshData instance)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(8f, false);
                _prefabImportWizardFoldout = EditorGUILayout.Foldout(_prefabImportWizardFoldout, "Prefab Import Wizard", true);
                EditorGUILayout.EndHorizontal();

                if (_prefabImportWizardFoldout)
                {
                    if (_prefabImportWizardSettings.prefab == null)
                    {
                        EditorGUILayout.HelpBox("This wizard will help you speed up FoliageMeshData creation by allowing the import of prefabs. " +
                            "To begin, create a prefab with the root object containing a LODGroup, which has simple MeshRenderers assigned to it." +
                            "Once you have a usable prefab, drag it into the object field below.", MessageType.Info, true);
                    }

                    _prefabImportWizardSettings.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefabImportWizardSettings.prefab, typeof(GameObject), false);

                    if (_prefabImportWizardSettings.prefab != null)
                    {
                        var prefabIsValid = true;

                        var prefabLodGroup = _prefabImportWizardSettings.prefab.GetComponent<LODGroup>();
                        if (prefabLodGroup == null)
                        {
                            EditorGUILayout.HelpBox("This prefab does not have a LODGroup.", MessageType.Error, true);
                            prefabIsValid = false;
                        }
                        else
                        {
                            if (prefabLodGroup.lodCount == 0)
                            {
                                EditorGUILayout.HelpBox("This prefab's LODGroup does not have any LODs assigned.", MessageType.Error, true);
                                prefabIsValid = false;
                            }
                        }

                        if (prefabIsValid)
                        {
                            EditorGUILayout.HelpBox("So far, everything seems okay!" +
                                " Hit 'Configure' to reconfigure this FoliageMeshData based on your target prefab. " +
                                "Please note: this will erase the LODs data in this FoliageMeshGroup!", MessageType.Info, true);

                            _prefabImportWizardSettings.MinimumDistance = EditorGUILayout.FloatField("Minimum LOD Distance", _prefabImportWizardSettings.MinimumDistance);
                            _prefabImportWizardSettings.MaximumDistance = EditorGUILayout.FloatField("MaximumDistance LOD Distance", _prefabImportWizardSettings.MaximumDistance);

                            if(_prefabImportWizardSettings.MinimumDistance < 1.0f)
                            {
                                _prefabImportWizardSettings.MinimumDistance = 1.0f;
                            }

                            if(_prefabImportWizardSettings.MinimumDistance >= _prefabImportWizardSettings.MaximumDistance)
                            {
                                _prefabImportWizardSettings.MinimumDistance = _prefabImportWizardSettings.MaximumDistance - 0.01f;
                            }

                            if (GUILayout.Button("Configure!"))
                            {
                                if (EditorUtility.DisplayDialog("Configure this FoliageMeshData?", $"Are you sure? This will overwrite the LODs of this FoliageMeshData ({instance.name})", "yes", "no"))
                                {
                                    RunImportPrefabWizard(instance, _prefabImportWizardSettings);
                                }

                                GUIUtility.ExitGUI();
                                return true; 
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
            return false; 
        }

        private static void RunImportPrefabWizard(FoliageMeshData instance, PrefabImportWizardSettings settings)
        {
            Undo.RecordObject(instance, "RunImportPrefabWizard");

            var prefab = settings.prefab;
            var prefabLodGroup = prefab.GetComponent<LODGroup>();
            if(prefabLodGroup == null)
            {
                Debug.LogError($"[RunImportPrefabWizard] {prefab.name} is missing a LODGroup!", prefab);
                return;
            }

            var prefabLods = prefabLodGroup.GetLODs();
            if(prefabLods == null || prefabLods.Length == 0)
            {
                Debug.LogError($"[RunImportPrefabWizard] {prefab.name}'s LODGroup is missing LODs?", prefab);
                return;
            }

            instance.LODs = new FoliageMeshData.FoliageMeshLOD[prefabLods.Length];

            for(var lodIndex = 0; lodIndex < prefabLods.Length; ++lodIndex)
            {
                var lod = prefabLods[lodIndex];
                var lodRenderers = lod.renderers;

                var renderDatas = new List<FoliageMeshData.FoliageMeshLODRenderData>();

                for (var rendererIndex = 0; rendererIndex < lodRenderers.Length; ++rendererIndex)
                {
                    var lodRenderer = lodRenderers[rendererIndex];
                    if(lodRenderer == null)
                    {
                        Debug.LogWarning($"[RunImportPrefabWizard] Skipped null Renderer reference on lod {lodIndex}.", prefab);
                        continue;
                    }

                    var lodMeshRenderer = lodRenderer as MeshRenderer;
                    if(lodMeshRenderer == null)
                    {
                        Debug.LogWarning($"[RunImportPrefabWizard] Skipped unsupported Renderer on lod {lodIndex} ({lodRenderer.name})", prefab);
                        continue;
                    }

                    var lodMeshFilter = lodMeshRenderer.GetComponent<MeshFilter>();
                    if(lodMeshFilter == null)
                    {
                        Debug.LogWarning($"[RunImportPrefabWizard] Skipped Renderer on lod {lodIndex} ({lodRenderer.name}) because no MeshFilter was found.", prefab);
                        continue;
                    }

                    var lodRendererTransform = lodMeshRenderer.transform;

                    var renderData = new FoliageMeshData.FoliageMeshLODRenderData();
                        renderData.mesh = lodMeshFilter.sharedMesh;
                        renderData.material = lodMeshRenderer.sharedMaterial;
                        renderData.localPosition = lodRendererTransform.localPosition;
                        renderData.localRotation = lodRendererTransform.localRotation;
                        renderData.localScale = lodRendererTransform.localScale;

                    renderDatas.Add(renderData); 
                }

                var distanceRange = Mathf.Max(settings.MaximumDistance - settings.MinimumDistance, 1.0f);

                var foliageLodData = new FoliageMeshData.FoliageMeshLOD();
                    foliageLodData.distance = (1.0f - lod.screenRelativeTransitionHeight) * distanceRange;
                    foliageLodData.RenderData = renderDatas.ToArray();

                instance.LODs[lodIndex] = foliageLodData;
            }

            EditorUtility.SetDirty(instance); 
        }

        [MenuItem("Assets/CorgiFoliage/Create FoliageMeshData from Prefab")]
        private static void EditorCreateFoliageMeshDataFromPrefab()
        {
            var foliageResources = FoliageResources.FindConfig();
            if (foliageResources == null)
            {
                Debug.LogError($"Could not find FoliageResources?");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                var folageMeshDatasFolderName = "FoliageMeshDatas";
                var foliageMeshDatasRootFolderPath = $"{foliageResources.EditorFoliageMeshDatasPath}/{folageMeshDatasFolderName}";
                if (!AssetDatabase.IsValidFolder(foliageMeshDatasRootFolderPath))
                {
                    AssetDatabase.CreateFolder(foliageResources.ProceduralMeshAssetPath, folageMeshDatasFolderName);
                }

                var selectedGameobjects = Selection.gameObjects;

                for(var i = 0; i < selectedGameobjects.Length; i++)
                {
                    var gameObject = selectedGameobjects[i];
                    var assetFilename = $"{foliageMeshDatasRootFolderPath}/{gameObject.name}.asset";

                    var existingAsset = AssetDatabase.LoadAssetAtPath<FoliageMeshData>(assetFilename);
                    if(existingAsset != null)
                    {
                        Debug.LogWarning($"Skipped creating FoliageMeshData for {gameObject.name}, because one already exists.", existingAsset);
                        continue;
                    }

                    var newFoliageMeshData = FoliageMeshData.CreateInstance<FoliageMeshData>();
                        newFoliageMeshData.name = gameObject.name;

                    var prefabImportWizard = new PrefabImportWizardSettings();
                        prefabImportWizard.prefab = gameObject;

                    RunImportPrefabWizard(newFoliageMeshData, prefabImportWizard);
                    AssetDatabase.CreateAsset(newFoliageMeshData, assetFilename);

                    Debug.Log($"Created FoliageMeshData for {gameObject.name}.", newFoliageMeshData);
                }

                AssetDatabase.SaveAssets();
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
            }

            AssetDatabase.StopAssetEditing();
        }
    }
}
#endif