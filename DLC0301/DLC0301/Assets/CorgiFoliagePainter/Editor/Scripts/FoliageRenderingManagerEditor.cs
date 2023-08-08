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

    [CustomEditor(typeof(FoliageRenderingManager))]
    public class FoliageRenderingManagerEditor : Editor
    {
        private readonly Color _selectedColor = new Color(3f / 255f, 136f / 255f, 252f / 255f, 1.0f);

        private SerializedProperty renderMode;
        private SerializedProperty InstancedBatchSize;
        private SerializedProperty resources;
        private SerializedProperty EditorConsiderSceneCameraGameView;

        private EditorTab _editorTab;

        private enum EditorTab
        {
            Settings,
            Bake,
        }

        private void OnEnable()
        {
            renderMode = serializedObject.FindProperty("renderMode");
            InstancedBatchSize = serializedObject.FindProperty("InstancedBatchSize");
            resources = serializedObject.FindProperty("resources");
            EditorConsiderSceneCameraGameView = serializedObject.FindProperty("EditorConsiderSceneCameraGameView");   
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var instance = (FoliageRenderingManager) target;

            // toolbar tabs 
            EditorGUILayout.LabelField("Tools/Tabs", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            {
                DrawLayoutEditorTab("settings", EditorTab.Settings, false);
                DrawLayoutEditorTab("bake", EditorTab.Bake, false);
            }
            GUILayout.EndHorizontal();

            // settings 
            switch(_editorTab)
            {
                case EditorTab.Settings:
                    DrawTabSettings(instance);
                    break;
                case EditorTab.Bake:
                    if(DrawTabBake(instance))
                    {
                        return; 
                    }
                    break; 
            }

            // stats 
            EditorGUILayout.BeginVertical("GroupBox");
            {
                ulong total_drawCalls = 0;
                ulong total_instanceCount = 0;
                ulong total_vertexCount = 0;
                ulong total_triangleCount = 0;

                bool any_shadows = false;

                var foliages = instance.GetRegisteredFoliageGroups();
                foreach(var foliage in foliages)
                {
                    foliage.CollectStats(out _, out int drawCalls, out int instanceCount, out ulong vertexCount, out ulong triangleCount);

                    total_drawCalls += (ulong) drawCalls;
                    total_instanceCount += (ulong)instanceCount;
                    total_vertexCount += vertexCount;
                    total_triangleCount += triangleCount;

                    any_shadows = any_shadows || foliage.ShadowsMode == ShadowCastingMode.On;
                }

                var foliageRenderMode = FoliageRenderingManager.GetCurrentRenderingMode();
                var infoMessage = $"There are {foliages.Count:N0} FoliageGroup(s), which contain {total_instanceCount:N0} instances, which will require {total_drawCalls:N0} draw calls via {foliageRenderMode}. " +
                    $"\n\nCombined, there are {total_triangleCount:N0} triangles with {total_vertexCount:N0} vertices on LOD0.";

                if (any_shadows)
                {
                    infoMessage += $"\n\nShadowCasting is On for at least 1 FoliageGroup, so you may see 2x to 4x these values being drawn, depending on your Shadow Cascade settings.";
                }

                EditorGUILayout.HelpBox(infoMessage, MessageType.Info, true);
            }
            EditorGUILayout.EndVertical();

            var changed = serializedObject.ApplyModifiedProperties();
            if(changed)
            {
                var foliages = instance.GetRegisteredFoliageGroups();
                foreach(var foliageGroup in foliages)
                {
                    foliageGroup.SetDirty(); 
                }
            }
        }

        private void DrawTabSettings(FoliageRenderingManager instance)
        {
            EditorGUILayout.Space(-14);
            EditorGUILayout.BeginVertical("GroupBox");
            {
                // resources 
                EditorGUILayout.PropertyField(resources);
                EditorGUILayout.Space();

                // settings 
                EditorGUILayout.PropertyField(renderMode);
                if (instance.renderMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstanced)
                {
                    EditorGUILayout.PropertyField(InstancedBatchSize);
                }
                EditorGUILayout.PropertyField(EditorConsiderSceneCameraGameView);
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Returns true if a dialog was displayed. 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private bool DrawTabBake(FoliageRenderingManager instance)
        {
            EditorGUILayout.Space(-14);
            EditorGUILayout.BeginVertical("GroupBox");
            {
                var hasBakedData = false;

                var foliageGroups = instance.GetRegisteredFoliageGroups();
                foreach(var group in foliageGroups)
                {
                    hasBakedData = hasBakedData || group.GetHasBakedMeshes();
                }

                if (hasBakedData)
                {
                    EditorGUILayout.Space(16);
                    EditorGUILayout.HelpBox("There are FoliageGroups that are currently baked.", MessageType.Info, true);
                    EditorGUILayout.Space(16);

                    if (GUILayout.Button("Delete Baked/Serialized Data"))
                    {
                        if (EditorUtility.DisplayDialog("Delete Baked Data", "Are you sure?", "yes", "uhh, no?"))
                        {
                            Undo.RecordObjects(foliageGroups.ToArray(), "Deleted Serialized Data");

                            foreach(var foliageGroup in foliageGroups)
                            {
                                foliageGroup.HideStaticMeshes();
                                foliageGroup.DisposeBakedMeshes();
                            }
                        }

                        GUIUtility.ExitGUI();
                        return true;
                    }
                }
                else
                {
                    EditorGUILayout.Space(16);

                    EditorGUILayout.HelpBox("This tab allows you to take your instanced foliage data and combine it into as few Mesh instances as possible, complete with LODGroups. " +
                        "Once baked, the editor for FoliageGroups become locked until the baked data is deleted. " +
                        "It is not recommended to bake all foliage groups, only use when you are sure it is the right choice. " +
                        "Baked groups may be faster to render in some scenarios, but they always consume more disk space and GPU memory. " +
                        "For more information, please read over the documentation. ", MessageType.Info, true);

                    EditorGUILayout.Space(16);

                    if (GUILayout.Button("Bake"))
                    {
                        Undo.RecordObjects(foliageGroups.ToArray(), "Baked Static Meshes");

                        foreach(var foliageGroup in foliageGroups)
                        {
                            foliageGroup.BakeInstancesIntoStaticMeshes();
                            foliageGroup.EditorSerializedBakedMeshes();
                            foliageGroup.ShowStaticMeshes();
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();

            return true; 
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
    }
}
