namespace CorgiFoliagePainter
{
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;
    using CorgiFoliagePainter.Extensions;

    [ExecuteAlways]
    public class FoliageRenderingManager : MonoBehaviour
    {
        private static FoliageRenderingManager _instance;

        public static FoliageRenderingManager GetInstance(bool createIfDoesNotExist = false)
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<FoliageRenderingManager>();
            }

            if (_instance == null && createIfDoesNotExist)
            {
                var newGo = new GameObject("FoliageRenderingManager");
                _instance = newGo.AddComponent<FoliageRenderingManager>();

                Debug.LogWarning($"No FoliageRenderingManager was found, so one was created." +
                    $"\nPlease conside setting up one yourself, instead.", _instance);
            }

            return _instance;
        }

        [Tooltip("Method used for rendering. Due to a ShaderGraph bug, any ShaderGraph shaders are only compatible with direct rendering. Direct rendering has some limitations, such as 1024 instances per draw call. It also requires more CPU time.")] 
        public FoliageRenderingMode renderMode = FoliageRenderingMode.DrawMeshInstanced;

        [Tooltip("When using DrawMeshInstanced renderMode, batches within a single group will be split by this number. 256 is okay for PC, but you may need smaller batch sizes for mobile devices.")] 
        public int InstancedBatchSize = 256;

        [Tooltip("When this is enabled, the SceneView's camera will be accounted for when calculating LODs, even during Play. Disable when you need accurate LODs in Play mode, and do not care about the SceneView rendering foliage.")]
        public bool EditorConsiderSceneCameraGameView = true;

        [Tooltip("Global data block. If one does not exist, it will be created for you.")] public FoliageResources resources;

        private const int _maxFoliageDisplacers = 128;
        [System.NonSerialized] private GraphicsBuffer _foliageDisplacementBuffer;
        [System.NonSerialized] private NativeArray<FoliageDisplacementData> _foliageDisplacementData;
        [System.NonSerialized] private List<FoliageMoverComponent> _foliageMovers = new List<FoliageMoverComponent>();

        private readonly int _FoliageDisplacementBuffer = Shader.PropertyToID("_FoliageDisplacementBuffer");
        private readonly int _FoliageDisplacementCount = Shader.PropertyToID("_FoliageDisplacementCount");

        private bool _platformSupportsComputeBuffers;

        [System.Serializable]
        public enum FoliageRenderingMode
        {
            DrawMeshInstanced         = 0,
            DrawMeshInstancedIndirect = 1,
        }

        private void OnEnable()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"Only one FoliageRenderManager can be active at a time. Disabling the newest one now.", gameObject);
                this.enabled = false;
                return;
            }

            _platformSupportsComputeBuffers = SystemInfo.supportsComputeShaders;

            if (_platformSupportsComputeBuffers)
            {
                _foliageDisplacementBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _maxFoliageDisplacers, System.Runtime.InteropServices.Marshal.SizeOf<FoliageDisplacementData>());
                _foliageDisplacementData = new NativeArray<FoliageDisplacementData>(_maxFoliageDisplacers, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            }
            else
            {
                Debug.LogWarning($"This platform does not support Compute Shaders, so displacement will not work.");
            }

            _instance = this;
            EnsureSettingsResources();
        }

        private void OnDisable()
        {
            if(_foliageDisplacementBuffer != null)
            {
                _foliageDisplacementBuffer.Dispose(); 
            }

            if(_foliageDisplacementData.IsCreated)
            {
                _foliageDisplacementData.Dispose(); 
            }
        }

        private void EnsureSettingsResources()
        {
#if UNITY_EDITOR
            if (resources == null)
            {
                resources = FoliageResources.FindConfig();
            }
#endif
        }

        private void Update()
        {
            UpdateFoliageLODIndices();
            UpdateFoliageDisplacers();

            var realRenderMode = renderMode;
            if(!_platformSupportsComputeBuffers)
            {
                realRenderMode = FoliageRenderingMode.DrawMeshInstanced;
            }

            switch (realRenderMode)
            {
                case FoliageRenderingMode.DrawMeshInstanced:
                    UpdateRenderModeInstancedDirect(); 
                    break;
                case FoliageRenderingMode.DrawMeshInstancedIndirect:
                    UpdateRenderModeInstancedIndirect(); 
                    break;
            }
        }

        private void UpdateFoliageDisplacers()
        {
            if (!_platformSupportsComputeBuffers)
            {
                Shader.SetGlobalFloat(_FoliageDisplacementCount, 0); 
                return;
            }

            var moversCount = _foliageMovers.Count;

            for (var i = 0; i < moversCount; ++i)
            {
                var moverComponent = _foliageMovers[i];

                _foliageDisplacementData[i] = new FoliageDisplacementData()
                {
                    position = moverComponent.transform.position,
                    radius = moverComponent.Radius,
                };
            }

            _foliageDisplacementBuffer.SetData(_foliageDisplacementData, 0, 0, moversCount);
            Shader.SetGlobalBuffer(_FoliageDisplacementBuffer, _foliageDisplacementBuffer); 
            Shader.SetGlobalFloat(_FoliageDisplacementCount, moversCount); 
        }

        private void UpdateFoliageLODIndices()
        {
#if UNITY_EDITOR
            var sceneCameras = UnityEditor.SceneView.GetAllSceneCameras();
            var isPlaying = Application.isPlaying;
#endif

            for (var i = 0; i < _registeredGroups.Count; ++i)
            {
                var group = _registeredGroups[i];


                for (var s = 0; s < group.Subgroups.Count; ++s)
                {
                    var subGroup = group.Subgroups[s];
                    var foliageData = subGroup.FoliageData;

                    if (foliageData == null || foliageData.LODs == null || foliageData.LODs.Length == 0 || subGroup.TrsDatas.Length == 0)
                    {
                        continue;
                    }

                    var lowestLodIndex = FoliageMeshData.FoliageMeshLOD.CullIndex;

                    for (var c = 0; c < _registeredCameras.Count; ++c)
                    {
                        var camera = _registeredCameras[c];
                        var cameraLodIndex = group.CalculateLodIndex(s, camera.transform.position);
                        lowestLodIndex = Mathf.Min(lowestLodIndex, cameraLodIndex);
                    }

#if UNITY_EDITOR
                    if(EditorConsiderSceneCameraGameView || !isPlaying)
                    {
                        for (var c = 0; c < sceneCameras.Length; ++c)
                        {
                            var camera = sceneCameras[c];
                            var cameraLodIndex = group.CalculateLodIndex(s, camera.transform.position);
                            lowestLodIndex = Mathf.Min(lowestLodIndex, cameraLodIndex);
                        }
                    }
#endif
                    group.CacheLodIndex(s, lowestLodIndex); 
                }
            }
        }

        private void UpdateRenderModeInstancedIndirect()
        {
            if (!_platformSupportsComputeBuffers)
            {
                return;
            }

            for (var i = 0; i < _registeredGroups.Count; ++i)
            {
                var group = _registeredGroups[i];

                if (group.EditorIsDisplayingStaticMeshes)
                {
                    continue;
                }

                for (var s = 0; s < group.Subgroups.Count; ++s)
                {
                    var subGroup = group.Subgroups[s];
                    var foliageData = subGroup.FoliageData;

                    if (foliageData == null || foliageData.LODs == null || foliageData.LODs.Length == 0 || subGroup.TrsDatas.Length == 0)
                    {
                        continue;
                    }

                    var lodIndex = group.GetLodIndexCache(s);
                    if (lodIndex == FoliageMeshData.FoliageMeshLOD.CullIndex)
                    {
                        continue;
                    }

                    var groupLod = foliageData.LODs[lodIndex];
                    if (groupLod.RenderData == null || groupLod.RenderData.Length == 0)
                    {
                        continue;
                    }

                    group.UpdateGraphicsBuffers(_platformSupportsComputeBuffers);

                    for(var r = 0; r < groupLod.RenderData.Length; ++r)
                    {
                        var renderData = groupLod.RenderData[r];

                        var mesh = renderData.mesh;
                        var material = renderData.material;
                        var shadowsMode = group.ShadowsMode;
                        var lightProbeUsage = group.LightProbeUsage;
                        var propertyBlock = group.GetIndirectPropertyBlock(s, lodIndex, r);
                        var layer = group.gameObject.layer;
                        var bounds = group.GetSubgroupBounds(s);
                        var args = group.GetArgs(s, lodIndex, r);

                        if (material == null || !material.enableInstancing)
                        {
                            continue;
                        }

                        var unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;


                        switch (lightProbeUsage)
                        {
                            case FoliageGroup.LightProbeMode.Off:
                                unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                                break;
                            case FoliageGroup.LightProbeMode.PerInstanceLightProbe:
                            case FoliageGroup.LightProbeMode.UseSingleAnchorOverride:
                            case FoliageGroup.LightProbeMode.UseSingleAnchorGroupCenter:
                                unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
                                break;
                        }

                        if(unityLightProbeUsage == LightProbeUsage.CustomProvided)
                        {
                            unityLightProbeUsage = LightProbeUsage.BlendProbes;
                        }

                        Graphics.DrawMeshInstancedIndirect(mesh, 0, material,
                            bounds, args, 0, propertyBlock, shadowsMode, true, layer, null, unityLightProbeUsage);
                    }
                }
            }
        }

        private void UpdateRenderModeInstancedDirect()
        {
            for (var i = 0; i < _registeredGroups.Count; ++i)
            {
                var group = _registeredGroups[i];

                if(group.EditorIsDisplayingStaticMeshes)
                {
                    continue;
                }

                for (var s = 0; s < group.Subgroups.Count; ++s)
                {
                    var subGroup = group.Subgroups[s];
                    var foliageData = subGroup.FoliageData;

                    if (foliageData == null || foliageData.LODs == null || foliageData.LODs.Length == 0 || subGroup.TrsDatas.Length == 0)
                    {
                        continue;
                    }

                    var lodIndex = group.GetLodIndexCache(s);
                    if (lodIndex == FoliageMeshData.FoliageMeshLOD.CullIndex)
                    {
                        continue;
                    }

                    var groupLod = foliageData.LODs[lodIndex];
                    if (groupLod.RenderData == null || groupLod.RenderData.Length == 0)
                    {
                        continue;
                    }

                    group.UpdateGraphicsBuffers(_platformSupportsComputeBuffers);

                    for(var r = 0; r < groupLod.RenderData.Length; ++r)
                    {
                        var renderData = groupLod.RenderData[r];

                        var mesh = renderData.mesh;
                        var material = renderData.material;
                        var shadowsMode = group.ShadowsMode;
                        var lightProbeUsage = group.LightProbeUsage;
                        var layer = group.gameObject.layer;

                        var propertyBlocks = group.GetSplitPropertyBlocks(s);
                        var trsData = group.GetTrsMatrices(s);
                        var trsDataFallback = group.GetTrsMatricesFallback(s);

                        if (material == null || !material.enableInstancing)
                        {
                            continue;
                        }

                        var unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                        switch (lightProbeUsage)
                        {
                            case FoliageGroup.LightProbeMode.Off:
                                unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                                break;
                            case FoliageGroup.LightProbeMode.PerInstanceLightProbe:
                            case FoliageGroup.LightProbeMode.UseSingleAnchorOverride:
                            case FoliageGroup.LightProbeMode.UseSingleAnchorGroupCenter:
                                unityLightProbeUsage = UnityEngine.Rendering.LightProbeUsage.CustomProvided;
                                break;
                        }

                        // generates garbage? also, not supported on older unity versions (added in 2021+)
                        // for (var splitIndex = 0; splitIndex < trsData.Length; ++splitIndex)
                        // {
                        //     var splitGroup = trsData[splitIndex];
                        //     var lodGroup = splitGroup[lodIndex];
                        //     var renderGroup = lodGroup[r];
                        //     var propertyBlock = propertyBlocks[splitIndex];
                        // 
                        //     if(renderGroup.Length == 0)
                        //     {
                        //         continue;
                        //     }
                        // 
                        //     var renderParams = new RenderParams(material);
                        //         renderParams.matProps = propertyBlock;
                        //         renderParams.lightProbeUsage = unityLightProbeUsage;
                        //         renderParams.shadowCastingMode = shadowsMode;
                        //     
                        //     Graphics.RenderMeshInstanced(renderParams, mesh, 0, renderGroup, renderGroup.Length, 0);
                        // }

                        // faster, and does not generate garbage. 
                        for (var splitIndex = 0; splitIndex < trsDataFallback.Length; ++splitIndex)
                        {
                            var splitGroup = trsDataFallback[splitIndex];
                            var lodGroup = splitGroup[lodIndex];
                            var renderGroup = lodGroup[r];
                            var propertyBlock = propertyBlocks[splitIndex];
                        
                            if (renderGroup.Length == 0)
                            {
                                continue;
                            }
                        
                            Graphics.DrawMeshInstanced(mesh, 0, material, renderGroup, renderGroup.Length,
                                propertyBlock, shadowsMode, true, layer, null, unityLightProbeUsage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Which groups to render. 
        /// </summary>
        private List<FoliageGroup> _registeredGroups = new List<FoliageGroup>();

        /// <summary>
        /// Which cameras are used for LOD calculation. 
        /// </summary>
        private List<FoliageCameraHelper> _registeredCameras = new List<FoliageCameraHelper>();

        /// <summary>
        /// Registers a camera with the foliage system. Used for LOD calculation.
        /// </summary>
        /// <param name="cameraHelper"></param>
        public static void RegisterCamera(FoliageCameraHelper cameraHelper)
        {
            if(_instance != null)
            {
                _instance._registeredCameras.Add(cameraHelper);
            }
        }

        /// <summary>
        /// Unregisters a camera with the foliage system. Used for LOD calculation.
        /// </summary>
        /// <param name="cameraHelper"></param>
        public static void UnregisterCamera(FoliageCameraHelper cameraHelper)
        {
            if(_instance != null)
            {
                _instance._registeredCameras.Remove(cameraHelper);
            }
        }

        /// <summary>
        /// Adds a foliage group to the rendering system. 
        /// </summary>
        /// <param name="group"></param>
        public static void RegisterFoliageGroup(FoliageGroup group)
        {
            var instance = GetInstance(true);
                instance._registeredGroups.Add(group);
        }

        /// <summary>
        /// Removes a foliage group from the rendering system. 
        /// </summary>
        /// <param name="group"></param>
        public static void UnregisterFoliageGroup(FoliageGroup group)
        {
            if(_instance != null)
            {
                _instance._registeredGroups.Remove(group);
            }
        }

        /// <summary>
        /// Returns the current rendering manager's current rendering mode. 
        /// </summary>
        /// <returns></returns>
        public static FoliageRenderingMode GetCurrentRenderingMode()
        {
            if(_instance)
            {
                return _instance.renderMode;
            }
            else
            {
                return FoliageRenderingMode.DrawMeshInstanced;
            }
        }

        /// <summary>
        /// Returns the current batch size for direct instanced rendering.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentBatchSize()
        {
            if(_instance != null)
            {
                return _instance.InstancedBatchSize;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Registers a mover, so that it displaces foliage.
        /// </summary>
        /// <param name="mover"></param>
        public static void RegisterMover(FoliageMoverComponent mover)
        {
            if(_instance != null)
            {
                _instance._foliageMovers.Add(mover);
            }
        }

        /// <summary>
        /// Unregisters a mover, so that it no longer displaces folige. 
        /// </summary>
        /// <param name="mover"></param>
        public static void UnregisterMover(FoliageMoverComponent mover)
        {
            if(_instance != null)
            {
                _instance._foliageMovers.Remove(mover);
            }
        }

        /// <summary>
        /// Returns the internal list of registered foliage groups. Careful, this is the actual list, not a copy. 
        /// </summary>
        /// <returns></returns>
        public List<FoliageGroup> GetRegisteredFoliageGroups()
        {
            return _registeredGroups;
        }
    }
}
