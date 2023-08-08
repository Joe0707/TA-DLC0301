namespace CorgiFoliagePainter.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using UnityEditorInternal;
    using Unity.Mathematics;
    using UnityEngine.Rendering;
    using CorgiFoliagePainter.Extensions;

    [CustomEditor(typeof(FoliageGroup))]
    public class FoliageGroupEditor : Editor
    {
        [System.NonSerialized] 
        private FoliagePreviewGenerator previewGenerator = new FoliagePreviewGenerator();

        private SerializedProperty EditorMouseMode;
        private SerializedProperty EditorBrushMode;
        private SerializedProperty EditorPlacePointOffsetFromSurface;
        private SerializedProperty EditorDragPlaceEveryDistance;
        private SerializedProperty EditorBrushDensity;
        private SerializedProperty EditorPlacePointPlaneNormalRotation;
        private SerializedProperty EditorPlacePointPlaneOffset;
        private SerializedProperty EditorPlaceLayerMask;
        private SerializedProperty EditorSnapToNavMesh;
        private SerializedProperty EditorSnapToNearestVert;
        private SerializedProperty EditorDrawIndividualBoundingBoxes;
        private SerializedProperty EditorDrawBoundingBox;
        private SerializedProperty EditorDrawRadius;
        private SerializedProperty EditorPlacePointScale;
        private SerializedProperty EditorPlacePointRotationEulor;
        private SerializedProperty EditorPlacePointColor;
        private SerializedProperty EditorRandomizeScale;
        private SerializedProperty EditorRandomizeRotation;
        private SerializedProperty EditorRandomScaleRange;
        private SerializedProperty EditorRandomRotationEulorRange;
        private SerializedProperty Subgroups;
        private SerializedProperty SpaceMode;
        private SerializedProperty ShadowsMode;
        private SerializedProperty LightProbeUsage;
        private SerializedProperty LightProbeAnchorOverride;

        private SerializedProperty EditorPaintColor;
        private SerializedProperty EditorPaintScale;
        private SerializedProperty EditorPaintRotation;
        private SerializedProperty EditorPaintColorGradient;
        private SerializedProperty EditorPaintScaleTarget;
        private SerializedProperty EditorPaintRotationTarget;
        private SerializedProperty EditorPaintOpacity;
        private SerializedProperty EditorPaintOpacityFeatherToPercent;
        private SerializedProperty EditorUseNormalOfSurfaceForDrawing;
        private SerializedProperty EditorMinimumDistanceBetweenInstances;

        // some cached values for the editor 
        private Vector3 _prevDragAtPosition;
        private EditorTab _editorTab = EditorTab.Data;

        // some cached values for TryGetPointFromMouse
        private Vector3 _previousMeshPoint0 = Vector3.zero;
        private Vector3 _previousMeshPoint1 = Vector3.zero;
        private Vector3 _previousMeshSurfacePoint = Vector3.zero;
        private Vector3 _previousMeshSurfaceNormal = Vector3.up;
        private bool _hasPreviousMeshSurfacePoint = false;

        private const string DocumentationURL = "https://docs.google.com/document/d/12hBXmdcXM_KQel0zX1tgaynjnNwe84evJVm4ADMCXb8/";

        private Vector3 _prevMousePosition = Vector3.zero;
        private Quaternion _prevMouseRotation = Quaternion.identity;
        private MaterialPropertyBlock _drawPropertyBlock;

        private Vector2 _subgroupScroll;
        private int _newFoliageMeshPickerWindow;
        private readonly Color _selectedColor = new Color(3f / 255f, 136f / 255f, 252f / 255f, 1.0f);

        private void OpenDocumentation()
        {
            Application.OpenURL(DocumentationURL);
        }

        private enum EditorTab
        {
            Data,
            Drawing,
            Painting,
            Erasing,
            Selection,
            BakeStatic,
            ExtraTools,
        }

        private void OnEnable()
        {
            Subgroups = serializedObject.FindProperty("Subgroups");
            SpaceMode = serializedObject.FindProperty("SpaceMode");

            EditorMouseMode = serializedObject.FindProperty("EditorMouseMode");
            EditorBrushMode = serializedObject.FindProperty("EditorBrushMode");
            EditorPlacePointOffsetFromSurface = serializedObject.FindProperty("EditorPlacePointOffsetFromSurface");
            EditorDragPlaceEveryDistance = serializedObject.FindProperty("EditorDragPlaceEveryDistance");
            EditorBrushDensity = serializedObject.FindProperty("EditorBrushDensity");
            EditorPlacePointPlaneNormalRotation = serializedObject.FindProperty("EditorPlacePointPlaneNormalRotation");
            EditorPlacePointPlaneOffset = serializedObject.FindProperty("EditorPlacePointPlaneOffset");
            EditorPlaceLayerMask = serializedObject.FindProperty("EditorPlaceLayerMask");
            EditorSnapToNavMesh = serializedObject.FindProperty("EditorSnapToNavMesh");
            EditorSnapToNearestVert = serializedObject.FindProperty("EditorSnapToNearestVert");
            EditorDrawIndividualBoundingBoxes = serializedObject.FindProperty("EditorDrawIndividualBoundingBoxes");
            EditorDrawBoundingBox = serializedObject.FindProperty("EditorDrawBoundingBox");
            EditorDrawRadius = serializedObject.FindProperty("EditorDrawRadius");
            EditorPlacePointScale = serializedObject.FindProperty("EditorPlacePointScale");
            EditorPlacePointRotationEulor = serializedObject.FindProperty("EditorPlacePointRotationEulor");
            EditorPlacePointColor = serializedObject.FindProperty("EditorPlacePointColor");
            EditorRandomizeScale = serializedObject.FindProperty("EditorRandomizeScale");
            EditorRandomizeRotation = serializedObject.FindProperty("EditorRandomizeRotation");
            EditorRandomScaleRange = serializedObject.FindProperty("EditorRandomScaleRange");
            EditorRandomRotationEulorRange = serializedObject.FindProperty("EditorRandomRotationEulorRange");
            EditorPaintColor = serializedObject.FindProperty("EditorPaintColor");
            EditorPaintScale = serializedObject.FindProperty("EditorPaintScale");
            EditorPaintRotation = serializedObject.FindProperty("EditorPaintRotation");
            EditorPaintColorGradient = serializedObject.FindProperty("EditorPaintColorGradient");
            EditorPaintScaleTarget = serializedObject.FindProperty("EditorPaintScaleTarget");
            EditorPaintRotationTarget = serializedObject.FindProperty("EditorPaintRotationTarget");
            EditorPaintOpacity = serializedObject.FindProperty("EditorPaintOpacity");
            EditorPaintOpacityFeatherToPercent = serializedObject.FindProperty("EditorPaintOpacityFeatherToPercent");
            EditorUseNormalOfSurfaceForDrawing = serializedObject.FindProperty("EditorUseNormalOfSurfaceForDrawing");
            EditorMinimumDistanceBetweenInstances = serializedObject.FindProperty("EditorMinimumDistanceBetweenInstances");
            ShadowsMode = serializedObject.FindProperty("ShadowsMode");
            LightProbeUsage = serializedObject.FindProperty("LightProbeUsage");
            LightProbeAnchorOverride = serializedObject.FindProperty("LightProbeAnchorOverride");

            Undo.undoRedoPerformed += EditorOnUndoRedo;

#if FOLIAGE_FOUND_URP || FOLIAGE_FOUND_HDRP
            RenderPipelineManager.beginCameraRendering += OnRenderPipelineBeginCameraRendering;
#else
            Camera.onPreCull += CustomUpdateOnRenderCamera;
#endif
        }

#if FOLIAGE_FOUND_URP || FOLIAGE_FOUND_HDRP
        private void OnRenderPipelineBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            CustomUpdateOnRenderCamera(camera); 
        }
#endif

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= EditorOnUndoRedo;
            Tools.hidden = false;

#if FOLIAGE_FOUND_URP || FOLIAGE_FOUND_HDRP
            RenderPipelineManager.beginCameraRendering -= OnRenderPipelineBeginCameraRendering;
#else
            Camera.onPreCull -= CustomUpdateOnRenderCamera;
#endif
        }

        private void CustomUpdateOnRenderCamera(Camera camera)
        {
            var instance = (FoliageGroup)target;
            var resources = FoliageRenderingManager.GetInstance(true).resources;

            if (_drawPropertyBlock == null)
            {
                _drawPropertyBlock = new MaterialPropertyBlock();
            }

            _drawPropertyBlock.Clear();

            var diameter = instance.EditorDrawRadius * 2f;

            switch (_editorTab)
            {
                case EditorTab.Data:
                case EditorTab.BakeStatic:
                    return;
                case EditorTab.Drawing:
                    _drawPropertyBlock.SetColor("_BaseColor", new Color(0.1379494f, 0.9433962f, 0.790248f, 1.0f));
                    break;
                case EditorTab.Painting:
                    _drawPropertyBlock.SetColor("_BaseColor", new Color(0.8349807f, 0.8679245f, 0.4462442f, 1.0f));
                    break;
                case EditorTab.Erasing:
                    _drawPropertyBlock.SetColor("_BaseColor", new Color(0.8490566f, 0.3824565f, 0.3484336f, 1.0f));
                    break;
                case EditorTab.Selection:
                    _drawPropertyBlock.SetColor("_BaseColor", new Color(0.5471698f, 0.5471698f, 0.5471698f, 1.0f));
                    diameter = 0.25f;
                    break;
            }

            var planeNormal = _prevMouseRotation * Vector3.up;
            var planeTangent = _prevMouseRotation * Vector3.right;
            var planeBitangent = _prevMouseRotation * Vector3.forward;

            var featherPercent = instance.EditorPaintOpacityFeatherToPercent;
            var nothingSelected = instance.EditorSelectedSubgroups.Count == 0;

            if (nothingSelected)
            {
                _drawPropertyBlock.SetColor("_BaseColor", new Color(0.8490566f, 0.3824565f, 0.3484336f, 1.0f));

                Graphics.DrawMesh(resources.SphereGizmoMesh, Matrix4x4.TRS(_prevMousePosition, _prevMouseRotation, Vector3.one * diameter), resources.SphereGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
                Graphics.DrawMesh(resources.CubeGizmoMesh, Matrix4x4.TRS(_prevMousePosition, _prevMouseRotation * Quaternion.Euler(0f, 45f, 0f), new Vector3(0.1f, 0.1f, 1f) * diameter), resources.InvalidGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
                Graphics.DrawMesh(resources.CubeGizmoMesh, Matrix4x4.TRS(_prevMousePosition, _prevMouseRotation * Quaternion.Euler(0f, 45f + 90f, 0f), new Vector3(0.1f, 0.1f, 1f) * diameter), resources.InvalidGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
            }
            else
            {
                if(instance.EditorBrushMode == FoliageGroup.BrushMode.SinglePoint)
                {
                    diameter = 0.25f;
                }

                var trsSphereDiameter = Matrix4x4.TRS(_prevMousePosition, _prevMouseRotation, Vector3.one * diameter);
                Graphics.DrawMesh(resources.SphereGizmoMesh, trsSphereDiameter, resources.SphereGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);

                if (instance.EditorPaintOpacityFeatherToPercent < 1f && _editorTab != EditorTab.Selection)
                {
                    var trsSphereFeather = Matrix4x4.TRS(_prevMousePosition, _prevMouseRotation, Vector3.one * diameter * instance.EditorPaintOpacityFeatherToPercent);
                    Graphics.DrawMesh(resources.SphereGizmoMesh, trsSphereFeather, resources.SphereGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
                }

                if (instance.EditorBrushMode == FoliageGroup.BrushMode.RandomInRadius && _editorTab != EditorTab.Selection)
                {
                    var randomPointCount = Mathf.Max(1, instance.EditorBrushDensity);
                    for (var i = 0; i < randomPointCount; ++i)
                    {
                        var t = (float)i / randomPointCount;
                        var circlePoint = UnitCircleOnPlane(planeTangent, planeBitangent, t * 360f);
                        var randomTrs = Matrix4x4.TRS(_prevMousePosition + circlePoint * instance.EditorDrawRadius * 0.5f * featherPercent, Quaternion.identity, Vector3.one * 0.10f);
                        Graphics.DrawMesh(resources.SphereGizmoMesh, randomTrs, resources.SphereGizmoMaterial, 0, camera, 0, _drawPropertyBlock, ShadowCastingMode.Off, false, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
                    }
                }
            }
        }

        private void EditorOnUndoRedo()
        {
            var instance = (FoliageGroup)target;
            if(instance != null)
            {
                var anyChanges = instance.ValidateData();
                if(anyChanges)
                {
                    EditorUtility.SetDirty(instance);
                }

                instance.SetDirty(); 
            }
        }

        // inspector 
        public override void OnInspectorGUI()
        {
            var instance = (FoliageGroup)target;

#if !FOLIAGE_FOUND_SHADERGRAPH
            var foliageResources = FoliageResources.FindConfig();
            if (!foliageResources.IgnoreNoShadergraphWarning)
            {
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.HelpBox("ShaderGraph is not installed! The built in materials will not function without it. " +
                        "However, you can ignore this if you are using your own non-ShaderGraph materials for your own custom foliage.", MessageType.Warning);

                    if(GUILayout.Button("ignore"))
                    {
                        foliageResources.IgnoreNoShadergraphWarning = true;
                        EditorUtility.SetDirty(foliageResources);
                    }
                }
                GUILayout.EndHorizontal();
            }
#endif

#if !FOLIAGE_FOUND_BURST
            EditorGUILayout.HelpBox("Burst is not installed! The plugin will still function, but things will be slower.", MessageType.Warning);
#endif

            var hasBakedData = instance.GetHasBakedMeshes();
            if(hasBakedData)
            {
                _editorTab = EditorTab.BakeStatic;
            }

            if (!instance.enabled)
            {
                EditorGUILayout.HelpBox("This Component is disabled, so most editor functions will not be available.", MessageType.Warning);
            }

            if (!instance.gameObject.activeInHierarchy)
            {
                EditorGUILayout.HelpBox("This GameObject is disabled, so most editor functions will not be available.", MessageType.Warning);
            }

            if(!GetSceneViewGizmosEnabled())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Gizmos are disabled! Gizmos must be enabled for the foliage tools to work in the SceneView!", MessageType.Warning);
                if(GUILayout.Button("enable"))
                {
                    SetSceneViewGizmos(true); 
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical("GroupBox");
            {
                // subgroup selection area 
                GUILayout.BeginVertical("GroupBox");
                {
                    EditorGUILayout.LabelField("Foliage Mesh Data", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    GUILayout.BeginHorizontal();
                    {
                        _subgroupScroll = GUILayout.BeginScrollView(_subgroupScroll, false, false, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.Height(64f + 12f + 32f + 8f));
                        {
                            GUILayout.BeginHorizontal();

                            for (var s = 0; s < instance.Subgroups.Count; ++s)
                            {
                                var subgroup = instance.Subgroups[s];
                                var foliageData = subgroup.FoliageData;

                                GUILayout.BeginVertical(GUILayout.Width(64f), GUILayout.Height(64f));
                                {
                                    // allow dropping in a new subgroup 
                                    if (foliageData == null)
                                    {
                                        var newFieldData = (FoliageMeshData)EditorGUILayout.ObjectField(null, typeof(FoliageMeshData), false, GUILayout.Width(128f));
                                        if (newFieldData != null)
                                        {
                                            Undo.RecordObject(instance, "updated subgroup data");
                                            subgroup.FoliageData = newFieldData;
                                            EditorUtility.SetDirty(this);
                                        }

                                        if (GUILayout.Button("-", GUILayout.Width(64f)))
                                        {
                                            Undo.RecordObject(instance, "updated subgroup data");
                                            subgroup.Dispose();
                                            instance.Subgroups.RemoveAt(s);
                                            EditorUtility.SetDirty(instance);
                                        }
                                    }

                                    // draw preview and allow selection of subgroup 
                                    else
                                    {
                                        var lods = foliageData.LODs;

                                        var previewTexture = EditorGUIUtility.whiteTexture;

                                        if (lods.Length > 0)
                                        {
                                            previewTexture = previewGenerator.GetPreviewTexture(instance, foliageData);
                                        }

                                        GUILayout.Label($"{foliageData.name}", GUILayout.Width(64f), GUILayout.Height(12f));

                                        var oldColor = GUI.backgroundColor;
                                        GUI.backgroundColor = instance.EditorSelectedSubgroups.Contains(s) ? _selectedColor : oldColor;

                                        if (GUILayout.Button(previewTexture, GUILayout.Width(64f), GUILayout.Height(64f)))
                                        {
                                            if (instance.EditorSelectedSubgroups.Contains(s))
                                            {
                                                instance.EditorSelectedSubgroups.Remove(s);
                                            }
                                            else
                                            {
                                                instance.EditorSelectedSubgroups.Add(s);
                                            }
                                        }
                                        GUI.backgroundColor = oldColor;

                                        var captureIndex = s;

                                        if (GUILayout.Button("...", GUILayout.Width(64f)))
                                        {
                                            var popup = new GenericMenu();
                                            popup.AddItem(new GUIContent("go to"), false, () =>
                                            {
                                                Selection.SetActiveObjectWithContext(foliageData, foliageData);
                                            });
                                            popup.AddItem(new GUIContent("change"), false, () =>
                                            {
                                                Undo.RecordObject(instance, "changed FoliageMeshData");
                                                subgroup.FoliageData = null;
                                                instance.SetDirty();
                                                EditorUtility.SetDirty(instance);
                                            });
                                            popup.AddItem(new GUIContent("remove"), false, () =>
                                            {
                                                Undo.RecordObject(instance, "removed subgroup");

                                                var subgroup = instance.Subgroups[captureIndex];
                                                subgroup.Dispose();

                                                instance.Subgroups.RemoveAt(captureIndex);
                                                instance.SetDirty();
                                                EditorUtility.SetDirty(instance);
                                            });
                                            popup.AddItem(new GUIContent("use default editor values"), false, () =>
                                            {
                                                Undo.RecordObject(instance, "updated editor settings");

                                                var subgroup = instance.Subgroups[captureIndex];
                                                var foliageData = subgroup.FoliageData;

                                                instance.EditorPlacePointColor = foliageData.DefaultRandomColors;
                                                instance.EditorPlacePointScale = foliageData.DefaultBaseScale;
                                                instance.EditorPlacePointRotationEulor = foliageData.DefaultBaseRotationEulor;
                                                instance.EditorRandomScaleRange = foliageData.DefaultRandomScale;
                                                instance.EditorRandomRotationEulorRange = foliageData.DefaultRandomRotationEulor;

                                                EditorUtility.SetDirty(instance);
                                            });
                                            popup.ShowAsContext();
                                        }
                                    }
                                }
                                GUILayout.EndVertical();
                            }

                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();

                        if (GUILayout.Button("+", GUILayout.Width(64f), GUILayout.Height(64f)))
                        {
                            // Undo.RecordObject(instance, "new subgroup");
                            // instance.Subgroups.Add(new FoliageGroup.Subgroup());
                            // EditorUtility.SetDirty(this);

                            _newFoliageMeshPickerWindow = EditorGUIUtility.GetControlID(FocusType.Passive) + 1;
                            EditorGUIUtility.ShowObjectPicker<FoliageMeshData>(null, false, string.Empty, _newFoliageMeshPickerWindow);
                        }

                        if(Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _newFoliageMeshPickerWindow)
                        {
                            var selectedObject = (FoliageMeshData) EditorGUIUtility.GetObjectPickerObject();
                            _newFoliageMeshPickerWindow = -1;

                            if(selectedObject != null)
                            {
                                Undo.RecordObject(instance, "new subgroup");
                                instance.Subgroups.Add(new FoliageSubgroup() {  FoliageData = selectedObject });
                                EditorUtility.SetDirty(this);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.LabelField($"There are {instance.EditorSelectedSubgroups.Count} FoliageMeshData(s) selected. Drawing, painting, and erasing affect these subgroups.");
                }
                GUILayout.EndVertical();

                // validate selection box 
                for(var s = instance.EditorSelectedSubgroups.Count - 1; s >= 0; --s)
                {
                    var selectedInex = instance.EditorSelectedSubgroups[s];
                    if(selectedInex < 0 || selectedInex >= instance.Subgroups.Count)
                    {
                        instance.EditorSelectedSubgroups.RemoveAt(s);
                        EditorUtility.SetDirty(instance); 
                    }
                }

                var anyValidSubgroups = false;

                for(var s = 0; s < instance.Subgroups.Count && !anyValidSubgroups; ++s)
                {
                    var subgroup = instance.Subgroups[s];
                    if(subgroup.FoliageData != null)
                    {
                        for(var l = 0; l < subgroup.FoliageData.LODs.Length; ++l)
                        {
                            var lod = subgroup.FoliageData.LODs[l];
                            for(var r = 0; r < lod.RenderData.Length; ++r)
                            {
                                var renderData = lod.RenderData[r];
                                if(renderData.mesh != null && renderData.material != null)
                                {
                                    anyValidSubgroups = true;
                                    break; 
                                }
                            }
                        }
                    }
                }

                if (instance.Subgroups == null || instance.Subgroups.Count == 0)
                {
                    EditorGUILayout.HelpBox("There are no FoliageMeshDatas assigned to this FoliageGroup. Use the + button above to assign at least one!", MessageType.Info);
                }

                else if (!anyValidSubgroups)
                {
                    EditorGUILayout.HelpBox("There are no valid FoliageMeshDatas assigned to this FoliageGroup. Please ensure that your FoliageMeshDatas have a mesh assigned to their LODs!", MessageType.Warning);
                }

                else
                {
                    var anyInvalidMaterials = false;

                    for (var s = 0; s < instance.Subgroups.Count && !anyInvalidMaterials; ++s)
                    {
                        var subgroup = instance.Subgroups[s];
                        var foliageData = subgroup.FoliageData;

                        for(var l = 0; l < foliageData.LODs.Length; ++l)
                        {
                            var lod = foliageData.LODs[l];

                            for(var r = 0; r < lod.RenderData.Length; ++r)
                            {
                                var renderData = lod.RenderData[r];

                                if (renderData.material != null && !renderData.material.enableInstancing)
                                {
                                    anyInvalidMaterials = true;
                                }
                            }
                        }
                    }

                    if (anyInvalidMaterials)
                    {
                        EditorGUILayout.HelpBox("One of the materials in one of your subgroups does not have instancing enabled. Fix it!", MessageType.Error);
                    }

                    // toolbar tabs 
                    EditorGUILayout.LabelField("Tools/Tabs", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    {
                        DrawLayoutEditorTab("data", EditorTab.Data, hasBakedData);
                        DrawLayoutEditorTab("draw", EditorTab.Drawing, hasBakedData);
                        DrawLayoutEditorTab("paint", EditorTab.Painting, hasBakedData);
                        DrawLayoutEditorTab("erase", EditorTab.Erasing, hasBakedData);
                        DrawLayoutEditorTab("select", EditorTab.Selection, hasBakedData); 
                        DrawLayoutEditorTab("bake", EditorTab.BakeStatic, false); 
                        DrawLayoutEditorTab("extras", EditorTab.ExtraTools, hasBakedData); 
                    }
                    GUILayout.EndHorizontal();

                    if (_editorTab == EditorTab.Data)
                    {
                        DrawDataTabInspector(instance);
                    }
                    else if (_editorTab == EditorTab.Drawing)
                    {
                        DrawDrawingTabInspector(instance);
                    }
                    else if (_editorTab == EditorTab.Erasing)
                    {
                        DrawEraseTabInspector(instance);
                    }
                    else if (_editorTab == EditorTab.Painting)
                    {
                        DrawPaintTabInspector(instance);
                    }
                    else if (_editorTab == EditorTab.Selection)
                    {
                        DrawSelectionTabInspector(instance); 
                    }
                    else if(_editorTab == EditorTab.BakeStatic)
                    {
                        if(DrawBakeTabInspector(instance))
                        {
                            return;
                        }
                    }
                    else if(_editorTab == EditorTab.ExtraTools)
                    {
                        DrawExtraToolsTabInspector(instance); 
                    }

                    // stats 
                    DrawInspectorStats(instance);
                }
            }
            GUILayout.EndVertical();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(instance);
                instance.SetDirty(); 
            }
        }

        private void DrawSelectionTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);

            GUILayout.BeginVertical("GroupBox");
            {
                var validSelection = false;

                if(instance.EditorSelectedSubgroupIndex >= 0 && instance.EditorSelectedSubgroupIndex < instance.Subgroups.Count && instance.Subgroups.Count > 0)
                {
                    var subgroup = instance.Subgroups[instance.EditorSelectedSubgroupIndex];
                    if(subgroup.TrsDatas.Length > 0 && instance.EditorSelectedFoliageInstanceIndex >= 0 && instance.EditorSelectedFoliageInstanceIndex < subgroup.TrsDatas.Length)
                    {
                        validSelection = true;
                    }
                }

                if(!validSelection)
                {
                    GUILayout.Label("No instance selected yet. Click one in the scene view to select one!"); 
                }
                else
                {
                    GUILayout.Label($"Subgroup {instance.EditorSelectedSubgroupIndex}'s instance {instance.EditorSelectedFoliageInstanceIndex} has been selected.");

                    var subgroup = instance.Subgroups[instance.EditorSelectedSubgroupIndex];
                    var instanceTrsData = subgroup.TrsDatas[instance.EditorSelectedFoliageInstanceIndex];
                    var instanceMetadata = subgroup.Metadatas[instance.EditorSelectedFoliageInstanceIndex];

                    var color = new Color(instanceMetadata.color.x, instanceMetadata.color.y, instanceMetadata.color.z, instanceMetadata.color.w);

                    var trs = (Matrix4x4)instanceTrsData.trs;
                    var position = trs.GetPositionFromMatrix();
                    var rotationEulor = trs.rotation.eulerAngles;
                    var scale = trs.lossyScale;

                    var newPosition = EditorGUILayout.Vector3Field("position", position);
                    var newRotationEulor = EditorGUILayout.Vector3Field("rotation", rotationEulor);
                    var newScale = EditorGUILayout.Vector3Field("scale", scale);

                    if (newPosition != position || newRotationEulor != rotationEulor || newScale != scale)
                    {
                        Undo.RecordObject(instance, "updated trs");

                        instanceTrsData.trs = Matrix4x4.TRS(newPosition, Quaternion.Euler(newRotationEulor), newScale);
                        subgroup.TrsDatas[instance.EditorSelectedFoliageInstanceIndex] = instanceTrsData;
                        instance.SetDirty();

                        EditorUtility.SetDirty(instance);
                    }

                    var newColor = EditorGUILayout.ColorField("color", color);
                    if (newColor != color)
                    {
                        Undo.RecordObject(instance, "Changed color of foliage instance");

                        instanceMetadata.color = new float4(newColor.r, newColor.g, newColor.b, newColor.a);
                        subgroup.Metadatas[instance.EditorSelectedFoliageInstanceIndex] = instanceMetadata;

                        EditorUtility.SetDirty(instance);
                        instance.SetDirty();
                    }

                    EditorGUILayout.Space();
                    if(GUILayout.Button("delete"))
                    {
                        Undo.RecordObject(instance, "deleted single"); 
                        instance.RemoveSingle(instance.EditorSelectedSubgroupIndex, instance.EditorSelectedFoliageInstanceIndex);
                        instance.SetDirty();
                        instance.EditorSelectedSubgroupIndex = -1;
                        instance.EditorSelectedFoliageInstanceIndex = -1; 
                        EditorUtility.SetDirty(instance);
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawDataTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);

            GUILayout.BeginVertical("GroupBox");
            {
                GUILayout.BeginVertical();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                {
                    var newSpaceMode = (FoliageGroup.FoliageSpace)EditorGUILayout.EnumPopup("Space Mode", instance.SpaceMode);
                    if (newSpaceMode != instance.SpaceMode)
                    {
                        Undo.RecordObject(instance, "Set Space Mode");
                        instance.SetSpaceMode(newSpaceMode, true);
                        instance.SetDirty();
                        EditorUtility.SetDirty(instance);
                    }

                    if (GUILayout.Button("center pivot", GUILayout.Width(128f)))
                    {
                        Undo.RecordObject(instance, "center pivot");
                        instance.CenterPivot();
                        instance.SetDirty();
                        EditorUtility.SetDirty(instance);
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(ShadowsMode);
                EditorGUILayout.PropertyField(LightProbeUsage);
                if(instance.LightProbeUsage == FoliageGroup.LightProbeMode.UseSingleAnchorOverride)
                {
                    EditorGUILayout.PropertyField(LightProbeAnchorOverride);
                }

                var renderingManager = FoliageRenderingManager.GetInstance(false);
                if (renderingManager != null && renderingManager.renderMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstancedIndirect)
                {
                    var lightProbeModeUnsupported = instance.LightProbeUsage == FoliageGroup.LightProbeMode.PerInstanceLightProbe 
                        || instance.LightProbeUsage == FoliageGroup.LightProbeMode.UseSingleAnchorOverride 
                        || instance.LightProbeUsage == FoliageGroup.LightProbeMode.UseSingleAnchorGroupCenter;

                    if (lightProbeModeUnsupported)
                    {
                        EditorGUILayout.HelpBox("Because your foliageRenderingManager is set to DrawMeshInstancedIndirect, the current LightProbeUsage setting is unsupported.", MessageType.Info, true); 
                    }
                }

                GUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Data Browser", EditorStyles.boldLabel);
                DrawInspectorBrowseRawData(instance);

                // visualization settings 
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Visualization Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(EditorDrawBoundingBox);
                EditorGUILayout.PropertyField(EditorDrawIndividualBoundingBoxes);

                
            }
            GUILayout.EndVertical();
        }

        private void DrawDrawingTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);
            GUILayout.BeginVertical("GroupBox");
            {
                if (!DrawWarningIfNoSubgroupSelected(instance))
                {
                    DrawInspectorBrushSettings(instance, false);

                    // instance settings  
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Instance Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(EditorUseNormalOfSurfaceForDrawing);
                    EditorGUILayout.PropertyField(EditorMinimumDistanceBetweenInstances);
                    EditorGUILayout.PropertyField(EditorPlacePointScale);
                    EditorGUILayout.PropertyField(EditorPlacePointRotationEulor);
                    EditorGUILayout.PropertyField(EditorPlacePointColor);

                    EditorGUILayout.PropertyField(EditorRandomizeScale);
                    if (instance.EditorRandomizeScale) EditorGUILayout.PropertyField(EditorRandomScaleRange);
                    EditorGUILayout.PropertyField(EditorRandomizeRotation);
                    if (instance.EditorRandomizeRotation) EditorGUILayout.PropertyField(EditorRandomRotationEulorRange);
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawEraseTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);
            GUILayout.BeginVertical("GroupBox");
            {
                if (!DrawWarningIfNoSubgroupSelected(instance))
                {
                    DrawInspectorBrushSettings(instance, false);

                    GUILayout.BeginHorizontal("GroupBox");
                    {
                        if (GUILayout.Button("Erase Group Data"))
                        {
                            Undo.RecordObject(instance, "Erased Group Data");

                            instance.ClearData();
                            instance.SetDirty();

                            EditorUtility.SetDirty(instance);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawPaintTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);
            GUILayout.BeginVertical("GroupBox");
            {
                if (!DrawWarningIfNoSubgroupSelected(instance))
                {
                    DrawInspectorBrushSettings(instance, true);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(EditorPaintOpacity);
                    EditorGUILayout.PropertyField(EditorPaintColor);
                    if (instance.EditorPaintColor) EditorGUILayout.PropertyField(EditorPaintColorGradient);
                    EditorGUILayout.PropertyField(EditorPaintScale);
                    if (instance.EditorPaintScale) EditorGUILayout.PropertyField(EditorPaintScaleTarget);
                    EditorGUILayout.PropertyField(EditorPaintRotation);
                    if (instance.EditorPaintRotation) EditorGUILayout.PropertyField(EditorPaintRotationTarget);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Edit all instances", EditorStyles.boldLabel);

                    // color 
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PropertyField(EditorPlacePointColor);
                        if (GUILayout.Button("Randomize Colors", GUILayout.Width(128f)))
                        {
                            Undo.RecordObject(instance, "Randomized Colors");

                            for (var si = 0; si < instance.EditorSelectedSubgroups.Count; ++si)
                            {
                                var subgroupIndex = instance.EditorSelectedSubgroups[si];
                                var subgroup = instance.Subgroups[subgroupIndex];
                                for (var i = 0; i < subgroup.Metadatas.Length; ++i)
                                {
                                    var metadata = subgroup.Metadatas[i];
                                    metadata.color = (float4)(Vector4)instance.EditorPlacePointColor.Evaluate(UnityEngine.Random.Range(0f, 1f));

                                    subgroup.Metadatas[i] = metadata;
                                }
                            }

                            instance.SetDirty();
                            EditorUtility.SetDirty(instance);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // scale 
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        EditorGUILayout.PropertyField(EditorPlacePointScale);
                        EditorGUILayout.PropertyField(EditorRandomScaleRange);
                        GUILayout.EndVertical();
                        if (GUILayout.Button("Randomize Scale", GUILayout.Width(128f)))
                        {
                            Undo.RecordObject(instance, "Randomized Scale");

                            for (var si = 0; si < instance.EditorSelectedSubgroups.Count; ++si)
                            {
                                var subgroupIndex = instance.EditorSelectedSubgroups[si];
                                var subgroup = instance.Subgroups[subgroupIndex];

                                for (var i = 0; i < subgroup.Metadatas.Length; ++i)
                                {
                                    var trsData = subgroup.TrsDatas[i];
                                    var matrix = (Matrix4x4)trsData.trs;

                                    var position = matrix.GetPositionFromMatrix();
                                    var rotation = matrix.rotation;
                                    var scale = instance.EditorPlacePointScale + Vector3.Scale(UnityEngine.Random.insideUnitSphere, instance.EditorRandomScaleRange);
                                    matrix.SetTRS(position, rotation, scale);

                                    trsData.trs = matrix;
                                    subgroup.TrsDatas[i] = trsData;
                                }
                            }

                            instance.SetDirty();
                            EditorUtility.SetDirty(instance);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // rotation 
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        EditorGUILayout.PropertyField(EditorPlacePointRotationEulor);
                        EditorGUILayout.PropertyField(EditorRandomRotationEulorRange);
                        GUILayout.EndVertical();
                        if (GUILayout.Button("Randomize Rotation", GUILayout.Width(128f)))
                        {
                            Undo.RecordObject(instance, "Randomized Rotation");

                            for (var si = 0; si < instance.EditorSelectedSubgroups.Count; ++si)
                            {
                                var subgroupIndex = instance.EditorSelectedSubgroups[si];
                                var subgroup = instance.Subgroups[subgroupIndex];

                                for (var i = 0; i < subgroup.Metadatas.Length; ++i)
                                {
                                    var trsData = subgroup.TrsDatas[i];
                                    var matrix = (Matrix4x4)trsData.trs;

                                    var position = matrix.GetPositionFromMatrix();
                                    var rotation = Quaternion.Euler(instance.EditorPlacePointRotationEulor)
                                        * Quaternion.Euler(Vector3.Scale(UnityEngine.Random.insideUnitSphere, instance.EditorRandomRotationEulorRange));
                                    var scale = matrix.lossyScale;
                                    matrix.SetTRS(position, rotation, scale);

                                    trsData.trs = matrix;
                                    subgroup.TrsDatas[i] = trsData;
                                }
                            }

                            instance.SetDirty();
                            EditorUtility.SetDirty(instance);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        private bool DrawWarningIfNoSubgroupSelected(FoliageGroup instance)
        {
            if(instance.EditorSelectedSubgroups.Count == 0)
            {
                EditorGUILayout.HelpBox("Please select at least one FoliageMeshData above!", MessageType.Info);
                return true;
            }

            return false; 
        }

        private void DrawInspectorStats(FoliageGroup instance)
        {
            GUILayout.BeginVertical("GroupBox");

            GUILayout.BeginHorizontal();
            GUILayout.Space(16f); 
            instance.EditorShowGroupStats = EditorGUILayout.Foldout(instance.EditorShowGroupStats, "Group Stats", true); 
            GUILayout.EndHorizontal();

            if(instance.EditorShowGroupStats)
            {
                instance.CollectStats(out var renderMode, out var drawCalls, out var instanceCount, out var vertexCount, out var triangleCount); 

                var infoMessage = $"There are {instance.Subgroups.Count:N0} subgroups, which contain {instanceCount:N0} instances, which will require {drawCalls:N0} draw calls via {renderMode}. " +
                    $"\n\nCombined, there are {triangleCount:N0} triangles with {vertexCount:N0} vertices on LOD0.";

                var shadowCastingEnabled = instance.ShadowsMode == ShadowCastingMode.On;
                if(shadowCastingEnabled)
                {
                    infoMessage += $"\n\nShadowCasting is On, so you may see 2x to 4x these values being drawn, depending on your Shadow Cascade settings.";
                }

                EditorGUILayout.HelpBox(infoMessage, MessageType.Info, true);

                if (renderMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstanced)
                {
                    GUILayout.Space(12f);

                    EditorGUILayout.LabelField("Draw call trackers. Each bar filled is another draw call.");

                    for (var s = 0; s < instance.Subgroups.Count; ++s)
                    {
                        var subgroup = instance.Subgroups[s];
                        var batchSize = FoliageRenderingManager.GetCurrentBatchSize();
                        var splitCount = (subgroup.TrsDatas.Length / batchSize) + 1;
                        DrawProgressBar($"[{subgroup.FoliageData.name}] x{splitCount}", (float)(subgroup.TrsDatas.Length - (splitCount - 1) * batchSize) / batchSize);
                    }

                    GUILayout.Space(12f);
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.HelpBox("For shader wizards, if you would like to reduce the number of draw calls, please read over the documentation about DrawMeshInstancedIndirect.", MessageType.Info);
                        if (GUILayout.Button("documentation"))
                        {
                            OpenDocumentation(); 
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawProgressBar(string label, float progress)
        {
            // sometimes spits out "ArgumentOutOfRangeException: Length cannot be less than zero." from some internal unity issue?
            var r = EditorGUILayout.BeginVertical();
            if (r.height > 0f && r.width > 0f)
            {
                EditorGUI.ProgressBar(r, progress, label);
            }

            GUILayout.Space(18);
            EditorGUILayout.EndVertical();
        }

        private void DrawLayoutEditorTab(string tabName, EditorTab tabIndex, bool disableIfTrue)
        {
            var selected = _editorTab == tabIndex;
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = selected ? _selectedColor * 1.25f : oldColor;

            GUILayout.BeginVertical(GUILayout.Height(24f));
            GUILayout.FlexibleSpace(); 
            EditorGUI.BeginDisabledGroup(tabIndex == _editorTab || disableIfTrue);
            {
                if (GUILayout.Button(tabName, GUILayout.Height(selected ? 24f : 20f)))
                {
                    _editorTab = tabIndex;
                }
            }
            EditorGUI.EndDisabledGroup(); 
            GUILayout.EndVertical();

            GUI.backgroundColor = oldColor;
        }

        private void DrawInspectorBrushSettings(FoliageGroup instance, bool showFeatherSetting)
        {
            // brush settings
            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(EditorMouseMode);

            if (instance.EditorMouseMode == FoliageGroup.MouseMode.Plane)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField("* Plane Brush Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(EditorPlacePointPlaneOffset);
                EditorGUILayout.PropertyField(EditorPlacePointPlaneNormalRotation);

                EditorGUILayout.BeginHorizontal();
                {
                    if(GUILayout.Button("center on bounding box"))
                    {
                        instance.RecalculateBounds();
                        var bounds = instance.GetBounds(); 

                        Undo.RecordObject(instance, "center");
                        instance.EditorPlacePointPlaneOffset = bounds.center;
                        EditorUtility.SetDirty(instance); 
                    }

                    if(GUILayout.Button("center on camera"))
                    {
                        var worldRay = HandleUtility.GUIPointToWorldRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
                        var offset = worldRay.origin + worldRay.direction * 10f;

                        Undo.RecordObject(instance, "center");
                        instance.EditorPlacePointPlaneOffset = offset;
                        EditorUtility.SetDirty(instance);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            if (instance.EditorMouseMode == FoliageGroup.MouseMode.CollisionSurface)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField("* CollisionSurface Brush Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(EditorPlaceLayerMask);
                EditorGUILayout.PropertyField(EditorSnapToNavMesh);
                EditorGUILayout.PropertyField(EditorSnapToNearestVert);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.PropertyField(EditorBrushMode);
            if (instance.EditorBrushMode == FoliageGroup.BrushMode.RandomInRadius)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField("*Brush Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(EditorBrushDensity);
                EditorGUILayout.PropertyField(EditorDrawRadius);
                if(showFeatherSetting) EditorGUILayout.PropertyField(EditorPaintOpacityFeatherToPercent);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.PropertyField(EditorPlacePointOffsetFromSurface);
            EditorGUILayout.PropertyField(EditorDragPlaceEveryDistance);
        }

        private void DrawInspectorColoredBox(float height, Color color)
        {
            var boxRect = EditorGUILayout.BeginVertical();
            GUILayout.Space(height);
            EditorGUI.DrawRect(boxRect, color);
            EditorGUILayout.EndVertical();
        }

        private int _currentDataPageIndex;
        private int _currentDataBrowserSubgroupIndex;

        private void DrawInspectorBrowseRawData(FoliageGroup instance)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            instance.EditorShowRawInstancingData = EditorGUILayout.Foldout(instance.EditorShowRawInstancingData, "Raw Instancing Data", true);
            GUILayout.EndHorizontal(); 

            if (instance.EditorShowRawInstancingData)
            {

                if(instance.Subgroups.Count > 0)
                {
                    GUILayout.BeginVertical("GroupBox");
                    {
                        var optionStrings = new string[instance.Subgroups.Count];
                        for (var s = 0; s < instance.Subgroups.Count; s++) optionStrings[s] = $"subgroup {s}";
                        _currentDataBrowserSubgroupIndex = EditorGUILayout.Popup(_currentDataBrowserSubgroupIndex, optionStrings);

                        var subgroup = instance.Subgroups[_currentDataBrowserSubgroupIndex];

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("shuffle TRS", GUILayout.Width(128f)))
                            {
                                Undo.RecordObject(instance, "shuffle trs");

                                subgroup.TrsDatas.Shuffle();
                                instance.SetDirty();

                                EditorUtility.SetDirty(instance);
                            }

                            if (GUILayout.Button("shuffle metadata", GUILayout.Width(128f)))
                            {
                                Undo.RecordObject(instance, "shuffle metadata");

                                subgroup.Metadatas.Shuffle();
                                instance.SetDirty();

                                EditorUtility.SetDirty(instance);
                            }
                        }
                        GUILayout.EndHorizontal();


                        var trsDatas = subgroup.TrsDatas;
                        var metadatas = subgroup.Metadatas;

                        var maxCountPerPage = 32;
                        var currentPageCount = trsDatas.Length / maxCountPerPage;

                        // pagination buttons 
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("<<"))
                            {
                                _currentDataPageIndex = 0;
                            }

                            if (GUILayout.Button("<"))
                            {
                                _currentDataPageIndex -= 1;
                            }

                            GUILayout.Label($"{_currentDataPageIndex} / {currentPageCount}");

                            if (GUILayout.Button(">"))
                            {
                                _currentDataPageIndex += 1;
                            }

                            if (GUILayout.Button(">>"))
                            {
                                _currentDataPageIndex = currentPageCount;
                            }

                            _currentDataPageIndex = Mathf.Clamp(_currentDataPageIndex, 0, currentPageCount);
                        }
                        GUILayout.EndHorizontal();

                        // data 
                        for (var i = 0; i < trsDatas.Length; ++i)
                        {
                            var pageIndex = (i / maxCountPerPage);
                            if (pageIndex != _currentDataPageIndex)
                            {
                                continue;
                            }

                            var trsData = trsDatas[i];
                            var metadata = metadatas[i];

                            var trs = (Matrix4x4)trsData.trs;
                            var color = new Color(metadata.color.x, metadata.color.y, metadata.color.z, metadata.color.w);
                            var position = trs.GetPositionFromMatrix();
                            var rotationEulor = trs.rotation.eulerAngles;
                            var scale = trs.lossyScale;

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField($"{i}:", GUILayout.Width(32f));

                                EditorGUILayout.LabelField($"pos", GUILayout.Width(24f));
                                var newPosition = new Vector3(
                                    EditorGUILayout.FloatField(position.x, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(position.y, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(position.z, GUILayout.Width(32f))
                                    );

                                EditorGUILayout.LabelField($"rot", GUILayout.Width(24f));
                                var newRotationEulor = new Vector3(
                                    EditorGUILayout.FloatField(rotationEulor.x, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(rotationEulor.y, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(rotationEulor.z, GUILayout.Width(32f))
                                    );

                                EditorGUILayout.LabelField($"scale", GUILayout.Width(32f));
                                var newScale = new Vector3(
                                    EditorGUILayout.FloatField(scale.x, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(scale.y, GUILayout.Width(32f)),
                                    EditorGUILayout.FloatField(scale.z, GUILayout.Width(32f))
                                    );

                                if (newPosition != position || newRotationEulor != rotationEulor || newScale != scale)
                                {
                                    Undo.RecordObject(instance, "updated trs");

                                    trsData.trs = Matrix4x4.TRS(newPosition, Quaternion.Euler(newRotationEulor), newScale);
                                    subgroup.TrsDatas[i] = trsData;
                                    instance.SetDirty();

                                    EditorUtility.SetDirty(instance);
                                }

                                // EditorGUILayout.LabelField($"{position}", GUILayout.Width(128f));
                                var newColor = EditorGUILayout.ColorField(color);
                                if (newColor != color)
                                {
                                    Undo.RecordObject(instance, "Changed color of foliage instance");

                                    metadata.color = new float4(newColor.r, newColor.g, newColor.b, newColor.a);
                                    subgroup.Metadatas[i] = metadata;

                                    EditorUtility.SetDirty(instance);
                                    instance.SetDirty();
                                }

                                // todo: bring back 
                                // if (GUILayout.Button("x", GUILayout.Width(32f)))
                                // {
                                //     SerializedFoliageData.DeleteArrayElementAtIndex(i);
                                //     serializedObject.ApplyModifiedProperties();
                                //     instance.SetDirty();
                                // }
                            }
                            GUILayout.EndHorizontal();

                            // raw 
                            // var propertyElement = SerializedFoliageData.GetArrayElementAtIndex(i);
                            // EditorGUILayout.PropertyField(propertyElement); 
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginHorizontal("GroupBox");
                    {
                        if (GUILayout.Button("save json (clipboard)"))
                        {
                            var foliageData = instance.GetAllFoliageData();
                            var foliageJson = JsonUtility.ToJson(foliageData);
                            Debug.Log(foliageJson);

                            GUIUtility.systemCopyBuffer = foliageJson;
                        }

                        if (GUILayout.Button("load json (clipboard)"))
                        {
                            var foliageJson = GUIUtility.systemCopyBuffer;
                            var foliageData = JsonUtility.FromJson<SerializableFoliageGroup>(foliageJson);
                            instance.SetAllFoliageData(foliageData);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private bool _debugBakeFoldout;

        /// <summary>
        /// Returns true if a dialog was displayed. 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private bool DrawBakeTabInspector(FoliageGroup instance)
        {

            EditorGUILayout.Space(-14);
            GUILayout.BeginVertical("GroupBox");
            {
                // bake settings 
                var hasBakedData = instance.GetHasBakedMeshes();

                if(hasBakedData)
                {
                    EditorGUILayout.Space(16);

                    EditorGUILayout.HelpBox("This FoliageGroup is currently baked. " +
                        "Because it is baked, this editor has been locked to the baked tab. " +
                        "To edit this foliage group, delete the baked data.", MessageType.Info, true);

                    EditorGUILayout.Space(16);
                }
                else
                {
                    EditorGUILayout.Space(16);

                    EditorGUILayout.HelpBox("This tab allows you to take your instanced foliage data and combine it into as few Mesh instances as possible, complete with LODGroups. " +
                        "Once baked, this editor becomes locked until the baked data is deleted. " +
                        "It is not recommended to bake all foliage groups, only use when you are sure it is the right choice. " +
                        "Baked groups may be faster to render in some scenarios, but they always consume more disk space and GPU memory. " +
                        "For more information, please read over the documentation. ", MessageType.Info, true);

                    EditorGUILayout.Space(16);
                }

                if (hasBakedData)
                {
                    if (GUILayout.Button("Delete Baked/Serialized Data"))
                    {
                        if(EditorUtility.DisplayDialog("Delete Baked Data", "Are you sure?", "yes", "uhh, no?"))
                        {
                            Undo.RecordObject(instance, "Deleted Serialized Data");
                            instance.HideStaticMeshes();
                            instance.DisposeBakedMeshes();
                        }

                        GUIUtility.ExitGUI();
                        return true;
                    }

                    EditorGUILayout.BeginVertical("GroupBox");

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16f);
                    _debugBakeFoldout = EditorGUILayout.Foldout(_debugBakeFoldout, "Debug Tools", true);
                    EditorGUILayout.EndHorizontal();

                    if(_debugBakeFoldout)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Show Static"))
                            {
                                instance.ShowStaticMeshes();
                            }

                            if (GUILayout.Button("Hide Static"))
                            {
                                instance.HideStaticMeshes();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    if (GUILayout.Button("Bake"))
                    {
                        Undo.RecordObject(instance, "Baked Static Meshes");
                        instance.BakeInstancesIntoStaticMeshes();
                        instance.EditorSerializedBakedMeshes();
                        instance.ShowStaticMeshes();
                    }
                }
            }
            GUILayout.EndVertical();

            return false; 
        }

        [System.NonSerialized] private bool _extraToolsPrefabPlacerFoldout;

        private void DrawExtraToolsTabInspector(FoliageGroup instance)
        {
            EditorGUILayout.Space(-14);
            GUILayout.BeginVertical("GroupBox");
            {
                GUILayout.BeginVertical("GroupBox");
                GUILayout.BeginHorizontal();
                EditorGUILayout.Space(8f, false); 
                _extraToolsPrefabPlacerFoldout = EditorGUILayout.Foldout(_extraToolsPrefabPlacerFoldout, "Prefab Paster", true);
                GUILayout.EndHorizontal();
                if(_extraToolsPrefabPlacerFoldout)
                {
                    const string prefabPlacerChildName = "PrefabPlacerGroup";
                    var prefabPlacerGroup = instance.transform.Find(prefabPlacerChildName);
                    var prefabsHaveBeenPlaced = prefabPlacerGroup != null;

                    if (prefabsHaveBeenPlaced)
                    {
                        EditorGUILayout.HelpBox("Prefabs have already been pasted for this group. If you wish to refresh them, " +
                            "please press 'Remove Placed Prefabs' and then re-paste them using this window.", 
                            MessageType.Info, true);

                        if(GUILayout.Button("Remove Placed Prefabs"))
                        {
                            Undo.DestroyObjectImmediate(prefabPlacerGroup.gameObject); 
                        }
                    }
                    else
                    {

                        var anySubgroupHasPrefabToPlace = false;
                        var subGroups = instance.Subgroups;
                        foreach(var subgroup in subGroups)
                        {
                            if (subgroup.FoliageData != null && subgroup.FoliageData.ExtraToolsPrefabPlacerSettings != null && subgroup.FoliageData.ExtraToolsPrefabPlacerSettings.prefab != null)
                            {
                                anySubgroupHasPrefabToPlace = true;
                                break; 
                            }
                        }



                        if (!anySubgroupHasPrefabToPlace)
                        {
                            EditorGUILayout.HelpBox("None of this FoliageGroup's FoliageMeshDatas contain references to prefabs to place. " +
                                "Please navigate to their FoliageMeshData objects and add prefabs to the ExtraToolsPrefabPlacerSettings, to use this tool.", MessageType.Info, true); 
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("If you press 'Place', your prefabs will be instantiated over and over for every instance in this FolageGroup. Be careful!", 
                                MessageType.Info, true);

                            if (GUILayout.Button("Place"))
                            {

                                var prefabPlacerGroupGo = new GameObject(prefabPlacerChildName);
                                prefabPlacerGroup = prefabPlacerGroupGo.transform;
                                prefabPlacerGroup.SetParent(instance.transform);
                                prefabPlacerGroup.localScale = Vector3.one;
                                prefabPlacerGroup.localPosition = Vector3.zero;
                                prefabPlacerGroup.localRotation = Quaternion.identity;

                                var groupLocalToWorld = instance.transform.localToWorldMatrix;

                                foreach(var subgroup in subGroups)
                                {
                                    var foliageData = subgroup.FoliageData;
                                    var prefabPlacerSettings = foliageData.ExtraToolsPrefabPlacerSettings;

                                    if(prefabPlacerSettings.prefab == null)
                                    {
                                        continue;
                                    }

                                    var placeTrs = Matrix4x4.TRS(prefabPlacerSettings.localPosition, prefabPlacerSettings.localRotation.GetQuaternionSafe(), prefabPlacerSettings.localScale);

                                    for(var t = 0; t < subgroup.TrsDatas.Length; ++t)
                                    {
                                        var trsData = subgroup.TrsDatas[t];
                                        var trs = (Matrix4x4) trsData.trs;
                                            trs = trs * placeTrs;

                                        if (instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                                        {
                                            trs = groupLocalToWorld * trs; 
                                        }

                                        var prefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefabPlacerSettings.prefab);
                                            prefabInstance.transform.position = trs.GetPositionFromMatrix();
                                            prefabInstance.transform.rotation = trs.GetRotationFromMatrix();
                                            prefabInstance.transform.localScale = trs.GetScaleFromMatrix(); 
                                            prefabInstance.transform.SetParent(prefabPlacerGroup, true); 
                                    }
                                }

                                Undo.RegisterCreatedObjectUndo(prefabPlacerGroupGo, "Placed Prefabs");
                            }
                        }
                    }
                }
                GUILayout.EndVertical(); 
            }
            GUILayout.EndVertical();
        }

        // scene 
        private void OnSceneGUI()
        {
            var instance = (FoliageGroup) target;
            if(instance == null)
            {
                return;
            }

            if(_editorTab == EditorTab.Data || _editorTab == EditorTab.BakeStatic)
            {
                Tools.hidden = false; 
                return; 
            }

            Tools.hidden = true;

            if (_editorTab == EditorTab.Selection)
            {
                if (TryGetPointFromMouse(instance, out Vector3 mousePoint, out Quaternion mouseRotation))
                {
                    _prevMousePosition = mousePoint;
                    _prevMouseRotation = mouseRotation;
                }

                DrawSelectableInstances(instance);
            }
            else
            {
                if (instance.EditorMouseMode == FoliageGroup.MouseMode.Plane)
                {
                    DrawPlacePlane(instance);
                }

                if (TryGetPointFromMouse(instance, out Vector3 mousePoint, out Quaternion mouseRotation))
                {
                    _prevMousePosition = mousePoint;
                    _prevMouseRotation = mouseRotation;

                    UpdateInputState(out bool mouseLeftDown, out bool mouseLeftUp, out bool mouseLeftDrag);

                    if (_editorTab == EditorTab.Drawing && instance.EditorSelectedSubgroups.Count > 0)
                    {
                        if (instance.EditorBrushMode == FoliageGroup.BrushMode.SinglePoint)
                        {
                            if (mouseLeftDown || mouseLeftDrag)
                            {
                                if (mouseLeftDown || Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                                {
                                    _prevDragAtPosition = mousePoint;

                                    Undo.RecordObject(instance, "Add Points");

                                    var position = mousePoint;
                                    var rotation = mouseRotation;
                                    var scale = instance.EditorPlacePointScale;

                                    RandomizePointData(instance, out int subgroupIndex, ref position, ref rotation, ref scale);

                                    if(!IsPointTooClose(instance, position))
                                    {
                                        var trsData = new FoliageTransformationData()
                                        {
                                            trs = Matrix4x4.TRS(position, rotation, scale)
                                        };

                                        var metadata = new FoliageMetadata()
                                        {
                                            color = (float4)(Vector4)instance.EditorPlacePointColor.Evaluate(UnityEngine.Random.Range(0f, 1f)),
                                        };

                                        instance.AppendSingle(subgroupIndex, trsData, metadata);
                                        instance.SetDirty();
                                        EditorUtility.SetDirty(instance);
                                    }
                                }
                            }
                        }
                        else if (instance.EditorBrushMode == FoliageGroup.BrushMode.RandomInRadius)
                        {
                            if (mouseLeftDown || mouseLeftDrag)
                            {
                                if (mouseLeftDown || Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                                {
                                    Undo.RecordObject(instance, "Add Points");

                                    _prevDragAtPosition = mousePoint;
                                    AddPointsInRange(instance, mousePoint, mouseRotation);

                                    instance.SetDirty();
                                    EditorUtility.SetDirty(instance);
                                }
                            }
                        }
                    }
                    else if (_editorTab == EditorTab.Erasing && instance.EditorSelectedSubgroups.Count > 0)
                    {
                        if (instance.EditorBrushMode == FoliageGroup.BrushMode.SinglePoint)
                        {
                            if (mouseLeftDown)
                            {
                                _prevDragAtPosition = mousePoint;
                                RemovePointsInRange(instance, mousePoint, 0f);
                            }
                            else if (mouseLeftDrag && Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                            {
                                _prevDragAtPosition = mousePoint;
                                RemovePointsInRange(instance, mousePoint, 0f);
                            }
                        }
                        else if (instance.EditorBrushMode == FoliageGroup.BrushMode.RandomInRadius)
                        {
                            if (mouseLeftDown)
                            {
                                _prevDragAtPosition = mousePoint;
                                RemovePointsInRange(instance, mousePoint, instance.EditorDrawRadius);
                                Repaint();
                            }
                            else if (mouseLeftDrag && Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                            {
                                _prevDragAtPosition = mousePoint;
                                RemovePointsInRange(instance, mousePoint, instance.EditorDrawRadius);
                                Repaint();
                            }
                        }
                    }
                    else if (_editorTab == EditorTab.Painting && instance.EditorSelectedSubgroups.Count > 0)
                    {
                        if (instance.EditorBrushMode == FoliageGroup.BrushMode.SinglePoint)
                        {
                            if (mouseLeftDown)
                            {
                                _prevDragAtPosition = mousePoint;
                                PaintPointsInRange(instance, mousePoint, 0f);
                            }
                            else if (mouseLeftDrag && Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                            {
                                _prevDragAtPosition = mousePoint;
                                PaintPointsInRange(instance, mousePoint, 0f);
                            }
                        }
                        else if (instance.EditorBrushMode == FoliageGroup.BrushMode.RandomInRadius)
                        {
                            if (mouseLeftDown)
                            {
                                _prevDragAtPosition = mousePoint;
                                PaintPointsInRange(instance, mousePoint, instance.EditorDrawRadius);
                                Repaint();
                            }
                            else if (mouseLeftDrag && Vector3.Distance(_prevDragAtPosition, mousePoint) > instance.EditorDragPlaceEveryDistance)
                            {
                                _prevDragAtPosition = mousePoint;
                                PaintPointsInRange(instance, mousePoint, instance.EditorDrawRadius);
                                Repaint();
                            }
                        }
                    }
                }
            }

            // always force a repaint so we can see the updated visuals 
            InternalEditorUtility.RepaintAllViews();
        }

        private void DrawSelectableInstances(FoliageGroup instance)
        {
            var mousePosition = Event.current.mousePosition;
            var worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);

            var validSelection = false;
            if(instance.EditorSelectedSubgroupIndex >= 0 && instance.Subgroups.Count > 0 && instance.EditorSelectedSubgroupIndex < instance.Subgroups.Count)
            {
                validSelection = true;
            }

            if(validSelection)
            {
                var subgroup = instance.Subgroups[instance.EditorSelectedSubgroupIndex];
                if(subgroup.TrsDatas.Length <= 0 || instance.EditorSelectedFoliageInstanceIndex < 0 || instance.EditorSelectedFoliageInstanceIndex >= subgroup.TrsDatas.Length)
                {
                    validSelection = false; 
                }
            }

            if(validSelection)
            {
                // handle delete key 
                if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
                {
                    Undo.RecordObject(instance, "deleted single");
                    instance.RemoveSingle(instance.EditorSelectedSubgroupIndex, instance.EditorSelectedFoliageInstanceIndex);
                    instance.SetDirty();
                    instance.EditorSelectedSubgroupIndex = -1;
                    instance.EditorSelectedFoliageInstanceIndex = -1;
                    EditorUtility.SetDirty(instance);
                    Event.current.Use(); 
                    return;
                }

                var subgroup = instance.Subgroups[instance.EditorSelectedSubgroupIndex];
                var trsData = subgroup.TrsDatas[instance.EditorSelectedFoliageInstanceIndex];
                var trsMatrix = (Matrix4x4) trsData.trs;

                if(instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                {
                    trsMatrix = instance.transform.localToWorldMatrix * trsMatrix;
                }

                var position = trsMatrix.GetPositionFromMatrix();
                var rotation = trsMatrix.rotation;
                var scale = trsMatrix.lossyScale;

                var changed = false;
                var handleRotationUsed = Tools.pivotRotation == PivotRotation.Local;

                switch (Tools.current)
                {
                    case Tool.Move:
                        if (DrawHandlePosition(Vector3.zero, handleRotationUsed ? rotation : Quaternion.identity, ref position, out Vector3 positionDelta))
                            changed = true;
                        break;

                    case Tool.Rotate:
                        if (DrawHandleRotation(position, ref rotation))
                            changed = true;
                        break;

                    case Tool.Scale:
                        if(DrawHandleScale(position, handleRotationUsed ? rotation : Quaternion.identity, ref scale))
                            changed = true;
                        break;
                }

                if(changed)
                {
                    Undo.RecordObject(instance, "changed trs");

                    if (instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                    {
                        var worldToLocal = instance.transform.worldToLocalMatrix;
                        position = worldToLocal.MultiplyPoint(position);
                        rotation = worldToLocal.rotation * rotation;
                        scale = worldToLocal.MultiplyVector(scale);
                    }

                    trsMatrix.SetTRS(position, rotation, scale);
                    trsData.trs = trsMatrix;
                    subgroup.TrsDatas[instance.EditorSelectedFoliageInstanceIndex] = trsData;
                    instance.SetDirty();

                    EditorUtility.SetDirty(instance);
                }
            }

            UpdateInputState(out bool mouseLeftDown, out bool mouseLeftUp, out bool mouseLeftDrag);

            if(mouseLeftDown)
            {
                var localToWorld = instance.transform.localToWorldMatrix;

                for (var s = 0; s < instance.Subgroups.Count; ++s)
                {
                    var subgroup = instance.Subgroups[s];
                    var meshBounds = subgroup.FoliageData.LODs[0].GetLodBounds();

                    for (var t = 0; t < subgroup.TrsDatas.Length; ++t)
                    {
                        var trsData = subgroup.TrsDatas[t];
                        var trsMatrix = (Matrix4x4) trsData.trs;

                        if (instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                        {
                            trsMatrix = localToWorld * trsMatrix;
                        }

                        var boundsCenter = trsMatrix.MultiplyPoint(meshBounds.center);
                        var boundsSize = Vector3.Scale(meshBounds.size, trsMatrix.lossyScale);

                        var bounds = new Bounds(boundsCenter, boundsSize);
                        if(bounds.IntersectRay(worldRay))
                        {
                            OnClickedInstance(instance, s, t);
                            return;
                        }
                    }
                }

                Undo.RecordObject(instance, "selected new instance");
                instance.EditorSelectedSubgroupIndex = -1;
                instance.EditorSelectedFoliageInstanceIndex = -1;
            }
        }

        private void OnClickedInstance(FoliageGroup instance, int subgroupIndex, int instanceIndex)
        {
            Undo.RecordObject(instance, "selected new instance");
            instance.EditorSelectedSubgroupIndex = subgroupIndex;
            instance.EditorSelectedFoliageInstanceIndex = instanceIndex;
        }

        private void DrawPlacePlane(FoliageGroup instance)
        {
            var center = instance.EditorPlacePointPlaneOffset;
            var rotation = instance.EditorPlacePointPlaneNormalRotation;

            if(DrawHandlePosition(Vector3.up * 2f, Tools.pivotRotation == PivotRotation.Local ? rotation : Quaternion.identity, ref center, out Vector3 positionDelta))
            {
                instance.EditorPlacePointPlaneOffset = center; 
                EditorUtility.SetDirty(instance); 
            }

            if(DrawHandleRotation(center, ref rotation))
            {
                instance.EditorPlacePointPlaneNormalRotation = rotation;
                EditorUtility.SetDirty(instance);
            }

            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;
            var right = rotation * Vector3.right;

            var corner00 = center - right - forward;
            var corner01 = center - right + forward;
            var corner10 = center + right - forward;
            var corner11 = center + right + forward;

            Handles.color = Color.green;

            // [ ] 
            Handles.DrawLine(corner00, corner01);
            Handles.DrawLine(corner01, corner11);
            Handles.DrawLine(corner11, corner10);
            Handles.DrawLine(corner10, corner00);
        }
        
        private void PaintPointsInRange(FoliageGroup instance, Vector3 mousePoint, float mouseRange)
        {
            Undo.RecordObject(instance, "RemovePointsInRange");

            for (var si = 0; si < instance.EditorSelectedSubgroups.Count; ++si)
            {
                var subgroupIndex = instance.EditorSelectedSubgroups[si];
                var subgroup = instance.Subgroups[subgroupIndex];
                var foliageData = subgroup.FoliageData;

                var meshBounds = foliageData.LODs[0].GetLodBounds();
                var mouseBoundsSize = new Vector3(mouseRange * 2.0f, mouseRange * 2.0f, mouseRange * 2.0f);

                // traverse backwards, because we use a RemoveAtSwapBack method 
                for (var i = subgroup.TrsDatas.Length - 1; i >= 0; --i)
                {
                    var trs = (Matrix4x4) subgroup.TrsDatas[i].trs;

                    if (instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                    {
                        trs = instance.transform.localToWorldMatrix * trs;
                    }

                    var boundsCenter = trs.MultiplyPoint(meshBounds.center);
                    var boundsSize = Vector3.Scale(meshBounds.size, trs.lossyScale);
                    var bounds = new Bounds(boundsCenter, boundsSize + mouseBoundsSize);
                    var distanceFromBounds = Vector3.Distance(mousePoint, bounds.ClosestPoint(mousePoint));

                    if (bounds.Contains(mousePoint) || distanceFromBounds < 0.01f)
                    {
                        var distanceFromMouse = Vector3.Distance(mousePoint, trs.GetPositionFromMatrix());
                        var featherIntensity = Mathf.InverseLerp(instance.EditorDrawRadius, instance.EditorDrawRadius * instance.EditorPaintOpacityFeatherToPercent, distanceFromMouse);
                        if (instance.EditorPaintOpacityFeatherToPercent >= 1.0f) featherIntensity = 1.0f;

                        var metadata = subgroup.Metadatas[i];
                        var trsData = subgroup.TrsDatas[i];

                        var matrix = (Matrix4x4)trsData.trs;
                        var position = matrix.GetPositionFromMatrix();
                        var rotation = matrix.rotation;
                        var scale = matrix.lossyScale;

                        if (instance.EditorPaintColor)
                        {
                            var newColor = (Vector4)instance.EditorPaintColorGradient.Evaluate(UnityEngine.Random.Range(0f, 1f));
                            metadata.color = (float4)Vector4.Lerp(metadata.color, newColor, instance.EditorPaintOpacity * featherIntensity);
                        }

                        if (instance.EditorPaintScale)
                        {
                            scale = Vector3.Lerp(scale, instance.EditorPaintScaleTarget, instance.EditorPaintOpacity * featherIntensity);
                            matrix.SetTRS(position, rotation, scale);
                        }

                        if (instance.EditorPaintRotation)
                        {
                            rotation = Quaternion.Slerp(rotation, Quaternion.Euler(instance.EditorPaintRotationTarget), instance.EditorPaintOpacity * featherIntensity);
                            matrix.SetTRS(position, rotation, scale);
                        }

                        trsData.trs = matrix;

                        subgroup.TrsDatas[i] = trsData;
                        subgroup.Metadatas[i] = metadata;
                    }
                }
            }

            instance.SetDirty();
            EditorUtility.SetDirty(instance);
        }

        private void RemovePointsInRange(FoliageGroup instance, Vector3 mousePoint, float mouseRange)
        {
            Undo.RecordObject(instance, "RemovePointsInRange");

            for (var si = 0; si < instance.EditorSelectedSubgroups.Count; ++si)
            {
                var subgroupIndex = instance.EditorSelectedSubgroups[si];
                var subgroup = instance.Subgroups[subgroupIndex];
                var foliageData = subgroup.FoliageData;

                var meshBounds = foliageData.LODs[0].GetLodBounds();
                var mouseBoundsSize = new Vector3(mouseRange * 2.0f, mouseRange * 2.0f, mouseRange * 2.0f);

                var removedCount = 0;

                // traverse backwards, because we use a RemoveAtSwapBack method 
                for (var i = subgroup.TrsDatas.Length - 1; i >= 0; --i)
                {
                    var trs = (Matrix4x4) subgroup.TrsDatas[i].trs;

                    if (instance.SpaceMode == FoliageGroup.FoliageSpace.Self)
                    {
                        trs = instance.transform.localToWorldMatrix * trs;
                    }

                    var boundsCenter = trs.MultiplyPoint(meshBounds.center);
                    var boundsSize = Vector3.Scale(meshBounds.size, trs.lossyScale);
                    var bounds = new Bounds(boundsCenter, boundsSize + mouseBoundsSize);

                    if (bounds.Contains(mousePoint) || Vector3.Distance(mousePoint, bounds.ClosestPoint(mousePoint)) < 0.01f)
                    {
                        instance.RemoveSingle(subgroupIndex, i);
                        removedCount++;

                        if (removedCount > instance.EditorBrushDensity)
                        {
                            break;
                        }
                    }
                }
            }

            instance.SetDirty(); 
            EditorUtility.SetDirty(instance);
        }

        private void AddPointsInRange(FoliageGroup instance, Vector3 mousePoint, Quaternion mouseRotation)
        {
            Undo.RecordObject(instance, "AddPointsInRange");

            var radius = instance.EditorDrawRadius;
            var normal = mouseRotation * Vector3.up;

            

            for (var i = 0; i < instance.EditorBrushDensity; ++i)
            {
                var randomJitterSphere = UnityEngine.Random.insideUnitSphere * radius;
                var randomJitterFlat = Vector3.ProjectOnPlane(randomJitterSphere, normal);

                var targetPosition = mousePoint + randomJitterFlat;
                var targetRotation = mouseRotation;

                if (instance.EditorMouseMode == FoliageGroup.MouseMode.MeshSurface)
                {
                    var valid = false;

                    // HandleUtility.PickGameObject only works in certain event types
                    if (_previouslyCapturedEventType == EventType.MouseMove || _previouslyCapturedEventType == EventType.MouseDown || _previouslyCapturedEventType == EventType.MouseDrag)
                    {
                        var go = HandleUtility.PickGameObject(HandleUtility.WorldToGUIPoint(targetPosition), false);
                        if (go != null)
                        {
                            var hit = RXLookingGlass.IntersectRayGameObject(new Ray(targetPosition + normal * 0.01f, -normal), go, out RaycastHit info);
                            if (hit && Vector3.Distance(targetPosition, info.point) < radius)
                            {
                                targetPosition = info.point + info.normal.normalized * instance.EditorPlacePointOffsetFromSurface;
                                targetRotation = GetRotationFromNormal(info.normal);
                                valid = true;
                            }
                        }
                    }

                    if(!valid)
                    {
                        continue; 
                    }
                }
                else if (instance.EditorMouseMode == FoliageGroup.MouseMode.CollisionSurface)
                {
                    var collisionHit = Physics.Raycast(targetPosition + normal * 0.01f, -normal, out RaycastHit collisionInfo, radius, instance.EditorPlaceLayerMask, QueryTriggerInteraction.Ignore);
                    if (collisionHit)
                    {
                        if (instance.EditorSnapToNavMesh && UnityEngine.AI.NavMesh.SamplePosition(collisionInfo.point, out UnityEngine.AI.NavMeshHit navMeshInfo, 16f, int.MaxValue))
                        {
                            var navMeshUp = navMeshInfo.normal;

                            var navMeshForward = Vector3.ProjectOnPlane(Vector3.forward, navMeshUp);
                            if (navMeshForward.sqrMagnitude > 0.001f)
                            {
                                navMeshForward = navMeshForward.normalized;
                            }
                            else
                            {
                                navMeshForward = Vector3.forward;
                            }

                            var navMeshPointRotation = Quaternion.LookRotation(navMeshForward, navMeshUp);
                            targetPosition = navMeshInfo.position;
                            targetRotation = navMeshPointRotation;
                        }
                        else if (instance.EditorSnapToNearestVert && collisionInfo.triangleIndex >= 0)
                        {
                            var meshFilter = collisionInfo.collider.GetComponent<MeshFilter>();
                            var mesh = meshFilter.sharedMesh;

                            var localToWorld = meshFilter.transform.localToWorldMatrix;

                            var vertices = mesh.vertices;
                            var normals = mesh.normals;

                            var triangles = mesh.triangles;
                            var triIndex = collisionInfo.triangleIndex;

                            var vertIndex0 = triangles[triIndex * 3 + 0];
                            var vertIndex1 = triangles[triIndex * 3 + 1];
                            var vertIndex2 = triangles[triIndex * 3 + 2];

                            var vertex0 = localToWorld.MultiplyPoint(vertices[vertIndex0]);
                            var vertex1 = localToWorld.MultiplyPoint(vertices[vertIndex1]);
                            var vertex2 = localToWorld.MultiplyPoint(vertices[vertIndex2]);

                            var normal0 = localToWorld.MultiplyVector(normals[vertIndex0]);
                            var normal1 = localToWorld.MultiplyVector(normals[vertIndex1]);
                            var normal2 = localToWorld.MultiplyVector(normals[vertIndex2]);

                            var distance0 = Vector3.Distance(vertex0, collisionInfo.point);
                            var distance1 = Vector3.Distance(vertex1, collisionInfo.point);
                            var distance2 = Vector3.Distance(vertex2, collisionInfo.point);

                            var rotation0 = Quaternion.LookRotation(normal0, Vector3.up);
                            var rotation1 = Quaternion.LookRotation(normal1, Vector3.up);
                            var rotation2 = Quaternion.LookRotation(normal2, Vector3.up);

                            if (distance0 < distance1 && distance0 < distance2)
                            {
                                targetPosition = vertex0 + normal0 * instance.EditorPlacePointOffsetFromSurface;
                                targetRotation = rotation0;
                            }
                            else if (distance1 < distance0 && distance1 < distance2)
                            {
                                targetPosition = vertex1 + normal1 * instance.EditorPlacePointOffsetFromSurface;
                                targetRotation = rotation1;
                            }
                            else
                            {
                                targetPosition = vertex2 + normal2 * instance.EditorPlacePointOffsetFromSurface;
                                targetRotation = rotation2;
                            }
                        }
                        else
                        {
                            targetPosition = collisionInfo.point + collisionInfo.normal * instance.EditorPlacePointOffsetFromSurface;
                            targetRotation = GetRotationFromNormal(collisionInfo.normal);
                        }
                    }
                    else
                    {
                        continue; 
                    }
                }

                if (!instance.EditorUseNormalOfSurfaceForDrawing)
                {
                    targetRotation = Quaternion.Euler(instance.EditorPlacePointRotationEulor);
                }

                var position = targetPosition;
                var rotation = targetRotation * Quaternion.Euler(instance.EditorPlacePointRotationEulor);
                var scale = instance.EditorPlacePointScale;

                RandomizePointData(instance, out int subgroupIndex, ref position, ref rotation, ref scale);

                if(IsPointTooClose(instance, position))
                {
                    continue;
                }

                var trsData = new FoliageTransformationData()
                {
                    trs = Matrix4x4.TRS(position, rotation, scale)
                };

                var metadata = new FoliageMetadata()
                {
                    color = (float4)(Vector4)instance.EditorPlacePointColor.Evaluate(UnityEngine.Random.Range(0f, 1f)),
                };

                instance.AppendSingle(subgroupIndex, trsData, metadata);
            }

            instance.SetDirty();
            EditorUtility.SetDirty(instance);
        }

        private void RandomizePointData(FoliageGroup instance, out int subgroupIndex, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            if (!instance.EditorUseNormalOfSurfaceForDrawing)
            {
                rotation = Quaternion.Euler(instance.EditorPlacePointRotationEulor);
            }

            var randomSubgroupSelectionIndex = UnityEngine.Random.Range(0, instance.EditorSelectedSubgroups.Count);
            subgroupIndex = instance.EditorSelectedSubgroups[randomSubgroupSelectionIndex];

            if (instance.EditorRandomizeRotation)
            {
                var randomEulor = instance.EditorRandomRotationEulorRange;
                rotation = rotation * Quaternion.Euler(
                    UnityEngine.Random.Range(-randomEulor.x, +randomEulor.x),
                    UnityEngine.Random.Range(-randomEulor.y, +randomEulor.y),
                    UnityEngine.Random.Range(-randomEulor.z, +randomEulor.z));
            }

            if (instance.EditorRandomizeScale)
            {
                var randomScale = instance.EditorRandomScaleRange;
                scale += new Vector3(
                    UnityEngine.Random.Range(-randomScale.x, +randomScale.x),
                    UnityEngine.Random.Range(-randomScale.y, +randomScale.y),
                    UnityEngine.Random.Range(-randomScale.z, +randomScale.z));
            }
        }

        private bool IsPointTooClose(FoliageGroup instance, Vector3 position)
        {
            if (instance.EditorMinimumDistanceBetweenInstances > 0f)
            {
                for (var s = 0; s < instance.Subgroups.Count; ++s)
                {
                    var subgroup = instance.Subgroups[s];

                    for (var t = 0; t < subgroup.TrsDatas.Length; ++t)
                    {
                        var otherTrs = (Matrix4x4)subgroup.TrsDatas[t].trs;
                        var otherPosition = otherTrs.GetPositionFromMatrix();

                        if (Vector3.Distance(otherPosition, position) < instance.EditorMinimumDistanceBetweenInstances)
                        {
                            return true; 
                        }
                    }
                }
            }

            return false; 
        }

        public static Vector3 UnitCircleOnPlane(Vector3 right, Vector3 up, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            var s = Mathf.Sin(rad);
            var c = Mathf.Cos(rad);

            var result = right * c + up * s;
            return result.normalized; 
        }
        
        private Quaternion GetRotationFromNormal(Vector3 normal)
        {
            var forward = Vector3.forward;

            if(Mathf.Abs(Vector3.Dot(normal, forward)) >= 0.999f)
            {
                forward = Vector3.right;
            }
            else
            {
                forward = Vector3.ProjectOnPlane(forward, normal).normalized;
            }

            return Quaternion.LookRotation(forward, normal); 
        }

        private bool TryGetPointFromMouse(FoliageGroup group, out Vector3 point, out Quaternion rotation)
        {
            var mousePosition = Event.current.mousePosition;
            var worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);

            point = Vector3.zero;
            rotation = Quaternion.identity;

            switch (group.EditorMouseMode)
            {
                case FoliageGroup.MouseMode.Plane:
                    var planePosition = group.EditorPlacePointPlaneOffset;
                    var planeNormal = group.EditorPlacePointPlaneNormalRotation * Vector3.up;
                    var rayOrigin = worldRay.origin;
                    var rayDirection = worldRay.direction;
                    var denominator = Mathf.Abs(Vector3.Dot(rayDirection, planeNormal));

                    if (denominator < 0.0001f)
                    {
                        return false;
                    }

                    var t = Mathf.Abs(Vector3.Dot(planePosition - rayOrigin, planeNormal)) / denominator;
                    point = rayOrigin + rayDirection * t;
                    rotation = group.EditorPlacePointPlaneNormalRotation;

                    return true;
                case FoliageGroup.MouseMode.MeshSurface:

                    // HandleUtility.PickGameObject only works in certain event types, so we need to cache the result to use between event types 
                    if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        var go = HandleUtility.PickGameObject(mousePosition, false);
                        if (go != null)
                        {
                            var hit = RXLookingGlass.IntersectRayGameObject(worldRay, go, out RaycastHit info);
                            if (hit)
                            {
                                _previousMeshPoint0 = info.point;
                                _previousMeshPoint1 = info.point + info.normal.normalized * group.EditorPlacePointOffsetFromSurface;

                                _hasPreviousMeshSurfacePoint = true;
                                _previousMeshSurfacePoint = info.point + info.normal.normalized * group.EditorPlacePointOffsetFromSurface;
                                _previousMeshSurfaceNormal = info.normal;

                                point = _previousMeshSurfacePoint;
                                rotation = GetRotationFromNormal(_previousMeshSurfaceNormal);

                                return true;
                            }
                        }
                        else
                        {
                            _previousMeshSurfacePoint = Vector3.zero;
                            _previousMeshSurfaceNormal = Vector3.up;
                            _hasPreviousMeshSurfacePoint = false;
                        }
                    }

                    if(group.EditorPlacePointOffsetFromSurface > 0f)
                    {
                        Handles.color = Color.white * 0.75f;
                        Handles.DrawSolidDisc(_previousMeshPoint0, _previousMeshSurfaceNormal, 0.25f); 
                        Handles.DrawLine(_previousMeshPoint0, _previousMeshPoint1);
                    }

                    point = _previousMeshSurfacePoint;
                    rotation = GetRotationFromNormal(_previousMeshSurfaceNormal);

                    return _hasPreviousMeshSurfacePoint;
                case FoliageGroup.MouseMode.CollisionSurface:
                    RaycastHit collisionInfo;
                    var collisionHit = Physics.Raycast(worldRay, out collisionInfo, 256f, group.EditorPlaceLayerMask, QueryTriggerInteraction.Ignore);
                    if (collisionHit)
                    {
                        if (group.EditorSnapToNavMesh && UnityEngine.AI.NavMesh.SamplePosition(collisionInfo.point, out UnityEngine.AI.NavMeshHit navMeshInfo, 16f, int.MaxValue))
                        {
                            var navMeshUp = navMeshInfo.normal;

                            var navMeshForward = Vector3.ProjectOnPlane(Vector3.forward, navMeshUp);
                            if (navMeshForward.sqrMagnitude > 0.001f)
                            {
                                navMeshForward = navMeshForward.normalized;
                            }
                            else
                            {
                                navMeshForward = Vector3.forward;
                            }

                            var navMeshPointRotation = Quaternion.LookRotation(navMeshForward, navMeshUp);
                            point = navMeshInfo.position;
                            rotation = navMeshPointRotation;

                            return true;
                        }

                        if (group.EditorSnapToNearestVert && collisionInfo.triangleIndex >= 0)
                        {
                            var meshFilter = collisionInfo.collider.GetComponent<MeshFilter>();
                            var mesh = meshFilter.sharedMesh;

                            var localToWorld = meshFilter.transform.localToWorldMatrix;

                            var vertices = mesh.vertices;
                            var normals = mesh.normals;

                            var triangles = mesh.triangles;
                            var triIndex = collisionInfo.triangleIndex;

                            var vertIndex0 = triangles[triIndex * 3 + 0];
                            var vertIndex1 = triangles[triIndex * 3 + 1];
                            var vertIndex2 = triangles[triIndex * 3 + 2];

                            var vertex0 = localToWorld.MultiplyPoint(vertices[vertIndex0]);
                            var vertex1 = localToWorld.MultiplyPoint(vertices[vertIndex1]);
                            var vertex2 = localToWorld.MultiplyPoint(vertices[vertIndex2]);

                            var normal0 = localToWorld.MultiplyVector(normals[vertIndex0]);
                            var normal1 = localToWorld.MultiplyVector(normals[vertIndex1]);
                            var normal2 = localToWorld.MultiplyVector(normals[vertIndex2]);

                            var distance0 = Vector3.Distance(vertex0, collisionInfo.point);
                            var distance1 = Vector3.Distance(vertex1, collisionInfo.point);
                            var distance2 = Vector3.Distance(vertex2, collisionInfo.point);

                            var rotation0 = Quaternion.LookRotation(normal0, Vector3.up);
                            var rotation1 = Quaternion.LookRotation(normal1, Vector3.up);
                            var rotation2 = Quaternion.LookRotation(normal2, Vector3.up);

                            if (distance0 < distance1 && distance0 < distance2)
                            {
                                if (group.EditorPlacePointOffsetFromSurface > 0f)
                                {
                                    Handles.color = Color.white * 0.75f;
                                    Handles.DrawSolidDisc(vertex0, normal0, 0.25f);
                                    Handles.DrawLine(vertex0, vertex0 + normal0 * group.EditorPlacePointOffsetFromSurface);
                                }

                                point = vertex0 + normal0 * group.EditorPlacePointOffsetFromSurface;
                                rotation = rotation0;
                            }
                            else if (distance1 < distance0 && distance1 < distance2)
                            {
                                if (group.EditorPlacePointOffsetFromSurface > 0f)
                                {
                                    Handles.color = Color.white * 0.75f;
                                    Handles.DrawSolidDisc(vertex1, normal1, 0.25f);
                                    Handles.DrawLine(vertex1, vertex1 + normal1 * group.EditorPlacePointOffsetFromSurface);
                                }

                                point = vertex1 + normal1 * group.EditorPlacePointOffsetFromSurface;
                                rotation = rotation1;
                            }
                            else
                            {
                                if (group.EditorPlacePointOffsetFromSurface > 0f)
                                {
                                    Handles.color = Color.white * 0.75f;
                                    Handles.DrawSolidDisc(vertex2, normal2, 0.25f);
                                    Handles.DrawLine(vertex2, vertex2 + normal2 * group.EditorPlacePointOffsetFromSurface);
                                }

                                point = vertex2 + normal2 * group.EditorPlacePointOffsetFromSurface;
                                rotation = rotation2;
                            }

                            return true;
                        }

                        if (group.EditorPlacePointOffsetFromSurface > 0f)
                        {
                            Handles.color = Color.white * 0.75f;
                            Handles.DrawSolidDisc(collisionInfo.point, collisionInfo.normal, 0.25f);
                            Handles.DrawLine(collisionInfo.point, collisionInfo.point + collisionInfo.normal * group.EditorPlacePointOffsetFromSurface);
                        }

                        point = collisionInfo.point + collisionInfo.normal * group.EditorPlacePointOffsetFromSurface;
                        rotation = GetRotationFromNormal(collisionInfo.normal);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }

            return false;
        }

        private EventType _previouslyCapturedEventType;

        private void UpdateInputState(out bool mouseLeftDown, out bool mouseLeftUp, out bool mouseLeftDrag)
        {
            mouseLeftDown = false;
            mouseLeftUp = false;
            mouseLeftDrag = false;

            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            _previouslyCapturedEventType = e.type;

            // only touch button 0 
            if (e.button != 0)
            {
                return; 
            }

            switch (e.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    mouseLeftDown = true;

                    GUIUtility.hotControl = controlID;
                    e.Use();
                    break;
                case EventType.MouseUp:
                    mouseLeftUp = true;

                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
                case EventType.MouseDrag:
                    mouseLeftDrag = true;

                    GUIUtility.hotControl = controlID;
                    e.Use();
                    break;
            }
        }

        private bool DrawHandlePosition(Vector3 offset, Quaternion handleRotation, ref Vector3 positionRef, out Vector3 delta)
        {
            var startPosition = positionRef;
            positionRef = Handles.PositionHandle(startPosition + offset, handleRotation) - offset;

            delta = positionRef - startPosition;
            var changed = delta.sqrMagnitude > 0;
            return changed;
        }

        private bool DrawHandleRotation(Vector3 position, ref Quaternion rotation)
        {
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;
            var forward = rotation * Vector3.forward;

            Handles.color = Color.green;
            Handles.DrawLine(position, position + up);

            Handles.color = Color.red;
            Handles.DrawLine(position, position + right);

            Handles.color = Color.blue;
            Handles.DrawLine(position, position + forward);

            rotation = Handles.RotationHandle(rotation, position);

            var new_up = rotation * Vector3.up;
            var new_right = rotation * Vector3.right;
            var new_forward = rotation * Vector3.forward;

            var changed =
                   (new_forward - forward).sqrMagnitude > 0
                || (new_right - right).sqrMagnitude > 0
                || (new_up - up).sqrMagnitude > 0;

            return changed;
        }

        private bool DrawHandleScale(Vector3 position, Quaternion rotation, ref Vector3 scale)
        {
            var startScale = scale;
            scale = Handles.ScaleHandle(scale, position, rotation, HandleUtility.GetHandleSize(position));

            var delta_scale = startScale - scale;
            var changed = delta_scale.sqrMagnitude > 0;
            return changed;
        }

        // create 
        [MenuItem("GameObject/CorgiFoliage/New Foliage Group", priority = 10)]
        public static GameObject MenuItemCreateFoliageGroup()
        {
            var newGameobject = new GameObject("NewFoliageGroup", typeof(FoliageGroup));

            if (Selection.activeTransform != null)
            {
                newGameobject.transform.SetParent(Selection.activeTransform);
            }

            Selection.SetActiveObjectWithContext(newGameobject, newGameobject);

            return newGameobject;
        }

        [MenuItem("GameObject/CorgiFoliage/New Foliage Rendering Manager", priority = 10)]
        public static GameObject MenuItemCreateFoligeManager()
        {
            var newGameobject = new GameObject("FoliageManager", typeof(FoliageRenderingManager));

            if (Selection.activeTransform != null)
            {
                newGameobject.transform.SetParent(Selection.activeTransform);
            }

            Selection.SetActiveObjectWithContext(newGameobject, newGameobject);

            return newGameobject;
        }

        public static void SetSceneViewGizmos(bool enable)
        {
            var sceneView = EditorWindow.GetWindow<SceneView>(null, false);
            if(sceneView != null)
            {
                sceneView.drawGizmos = enable;
            }
        }

        public static bool GetSceneViewGizmosEnabled()
        {
            var sceneView = EditorWindow.GetWindow<SceneView>(null, false);
            if(sceneView != null)
            {
                return sceneView.drawGizmos;
            }
            else
            {
                return true;
            }
        }
    }
}
