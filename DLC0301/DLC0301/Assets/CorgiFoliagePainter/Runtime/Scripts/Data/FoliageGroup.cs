namespace CorgiFoliagePainter
{
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using CorgiFoliagePainter.Extensions;

    [ExecuteAlways]
    public class FoliageGroup : MonoBehaviour
    {
        [System.Serializable]
        public enum FoliageSpace
        {
            Self    = 0,
            World   = 1,
        }

        [System.NonSerialized] private Bounds _wholeBounds;

        [Tooltip("Contains information used internally for the foliage system.")]
        public List<FoliageSubgroup> Subgroups = new List<FoliageSubgroup>();

        [Tooltip("Contains information used internally for the foliage system.")]
        public List<int> EditorSelectedSubgroups = new List<int>(); 

        [Tooltip("If SpaceMode is set to Self, the foliage group will move with the Transform. If it is set to World, the Transform will have no influence.")] public FoliageSpace SpaceMode; 

        // editor window settings - stored in the FoliageGroup so the settings are rememebered per-group.
        [Tooltip("Controls how the mouse position in the editor scene view is transformed into a world position.")] public MouseMode EditorMouseMode = MouseMode.MeshSurface;
        [Tooltip("Controls how instances are created at the mouse position.")] public BrushMode EditorBrushMode = BrushMode.RandomInRadius;
        [Tooltip("Offsets the mouse position from the surface of whatever it's hovering.")] [Range(-10f, 10f)] public float EditorPlacePointOffsetFromSurface = 0f;
        [Tooltip("When dragging to draw instances, this value is used to determine how far you must drag for a new instance to be placed.")] [Range(0.01f, 1f)] public float EditorDragPlaceEveryDistance = 0.25f;
        [Tooltip("When dragging to draw instances, this value is used to determine how many instances to place.")] [Range(1, 16)] public int EditorBrushDensity = 4;
        [Tooltip("Rotates the plane used for placing new instances.")] public Quaternion EditorPlacePointPlaneNormalRotation = Quaternion.Euler(0f, 0f, 0f);
        [Tooltip("Shifts around the plane used for placing new instances.")] public Vector3 EditorPlacePointPlaneOffset;
        [Tooltip("LayerMask used for the physics based collision place mode.")] public LayerMask EditorPlaceLayerMask = int.MaxValue;
        [Tooltip("If enabled, the mouse position will attempt to snap to a nearby navmesh.")] public bool EditorSnapToNavMesh;
        [Tooltip("If enabled, the mouse position will attempt to snap to nearby vertices. Requires ReadWrite to be enabled on the mesh import settings.")] public bool EditorSnapToNearestVert;
        [Tooltip("Debug - draws bounding boxes around foliage instances.")] public bool EditorDrawIndividualBoundingBoxes;
        [Tooltip("Debug - draws the bounding box for the whole foliage group.")] public bool EditorDrawBoundingBox;
        [Tooltip("Radius used for placing foliage instances. Anything inside of the radius is fair game for random positioning.")] public float EditorDrawRadius = 1.0f;
        [Tooltip("Scale of newly drawn instances.")] public Vector3 EditorPlacePointScale = Vector3.one;
        [Tooltip("Rotation of newly drawn instances")] public Vector3 EditorPlacePointRotationEulor = Vector3.zero;
        [Tooltip("Color of newly drawn instances.")] public Gradient EditorPlacePointColor = new Gradient() {  
            colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) } ,
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };
        [Tooltip("If enabled, the paintbrush will affect color.")] public bool EditorPaintColor = true;
        [Tooltip("If enabled, the paintbrush will affect scale.")] public bool EditorPaintScale = true;
        [Tooltip("If enabled, the paintbrush will affect rotation.")] public bool EditorPaintRotation = true;

        [Tooltip("Color used for painted instances.")]
        public Gradient EditorPaintColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };

        [Tooltip("Scale of painted instances.")] public Vector3 EditorPaintScaleTarget = Vector3.one;
        [Tooltip("Rotation of painted instances")] public Vector3 EditorPaintRotationTarget = Vector3.zero;
        [Tooltip("Intensity of paint effect.")] [Range(0f, 1f)] public float EditorPaintOpacity = 0.10f;
        [Tooltip("From this value to the outside of the brush, fade towards 0% opacity.")] [Range(0f, 1f)] public float EditorPaintOpacityFeatherToPercent = 1.0f;

        [Tooltip("If enabled, newly drawn instances will have a random scale.")] public bool EditorRandomizeScale = false;
        [Tooltip("If enabled, newly drawn instances will have a random rotation.")] public bool EditorRandomizeRotation = false;
        [Tooltip("The value used for random scale jitter.")] public Vector3 EditorRandomScaleRange = new Vector3(0.25f, 0.25f, 0.25f);
        [Tooltip("The value used for random rotation jitter.")] public Vector3 EditorRandomRotationEulorRange = new Vector3(0f, 360f, 0f);
        [Tooltip("If enabled, show group stats in the inspector.")] public bool EditorShowGroupStats;
        [Tooltip("If enabled, show raw instancing data in the inspector.")] public bool EditorShowRawInstancingData;
        [Tooltip("If enabled, use the normal of the mouse hovered surface for rotating newly added foliage instances.")] public bool EditorUseNormalOfSurfaceForDrawing = true;
        [Tooltip("Minimum distance newly added foliage instances can be placed from existing instances.")] public float EditorMinimumDistanceBetweenInstances;

        [Tooltip("When drawing, which shadow mode should be used?")] public ShadowCastingMode ShadowsMode = ShadowCastingMode.On;
        [Tooltip("When drawing, should we use light probes? And if so, how should we use them?")] public LightProbeMode LightProbeUsage = LightProbeMode.PerInstanceLightProbe;
        [Tooltip("When using UseSingleAnchorOverride, this is the transform used for the light probe sample position. If null, the transform for this group is used instead.")] public Transform LightProbeAnchorOverride;

        [System.Serializable]
        public enum LightProbeMode
        {
            Off = 0,
            PerInstanceLightProbe = 1,
            UseSingleAnchorGroupCenter = 2,
            UseSingleAnchorOverride = 3,
        }

        [System.NonSerialized] private bool _isDirty;
        public int EditorSelectedSubgroupIndex;
        public int EditorSelectedFoliageInstanceIndex;
        public bool EditorIsDisplayingStaticMeshes;

        private static readonly int _FoliageTrsBuffer = Shader.PropertyToID("_FoliageTrsBuffer");
        private static readonly int _FoliageMetadataBuffer = Shader.PropertyToID("_FoliageMetadataBuffer");
        private static readonly int _FoliageMetadataFallbackTexture = Shader.PropertyToID("_FoliageMetadataFallbackTexture");
        private static readonly int _FoliageMetadataFallbackMetadata = Shader.PropertyToID("_FoliageMetadataFallbackMetadata");
        private static readonly int _FoliageLocalToWorld = Shader.PropertyToID("_FoliageLocalToWorld");
        private static readonly int _FoliageWorldToLocal = Shader.PropertyToID("_FoliageWorldToLocal");

        [System.Serializable]
        public enum MouseMode
        {
            Plane               = 0,
            MeshSurface         = 1,
            CollisionSurface    = 2,
        }

        [System.Serializable]
        public enum BrushMode
        {
            SinglePoint         = 0,
            RandomInRadius      = 1, 
        }

        private void OnEnable()
        {
            ValidateData(); 
            SetDirty();

            FoliageRenderingManager.RegisterFoliageGroup(this);
        }

        private void OnDisable()
        {
            FoliageRenderingManager.UnregisterFoliageGroup(this);
            DisposeGraphicsBuffers(); 
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var currentSelectedGameobject = UnityEditor.Selection.activeGameObject;
            if(currentSelectedGameobject != null)
            {
                // don't show if we're already selected 
                if (currentSelectedGameobject == gameObject)
                {
                    return;
                }

                // don't show if we've selected any other foliage group (so we can still draw over them in mesh mode..) 
                var selectedFoliageGroup = currentSelectedGameobject.GetComponent<FoliageGroup>();
                if(selectedFoliageGroup != null)
                {
                    return; 
                }
            }

            var restore = Gizmos.color;

            Gizmos.DrawIcon(transform.position, "foliage_icon.png", true);

            var bounds = GetBounds();
            Gizmos.color = new Color(1, 1, 1, 1f / 255f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = restore;
        }

        private void OnDrawGizmosSelected()
        {
            if (Subgroups == null || Subgroups.Count == 0) return;
            
            var hasData = false;

            for(var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];
                var foliageData = subgroupData.FoliageData; 
                if(foliageData == null)
                {
                    continue;
                }

                var TrsDatas = subgroupData.TrsDatas;
                var LODs = foliageData.LODs;

                if (LODs == null || LODs.Length == 0 || LODs[0].RenderData == null || LODs[0].RenderData.Length == 0 || LODs[0].RenderData[0].mesh == null)
                {
                    continue; 
                }

                if(TrsDatas.Length > 0)
                {
                    hasData = true; 
                }

                if (EditorDrawIndividualBoundingBoxes)
                {
                    var meshBounds = LODs[0].RenderData[0].mesh.bounds;

                    Gizmos.color = Color.green;

                    var localToWorld = transform.localToWorldMatrix;

                    for (var i = 0; i < TrsDatas.Length; ++i)
                    {
                        var trsMatrix = (Matrix4x4) TrsDatas[i].trs;

                        if(SpaceMode == FoliageSpace.Self)
                        {
                            trsMatrix = localToWorld * trsMatrix;
                        }

                        var boundsCenter = trsMatrix.MultiplyPoint(meshBounds.center);
                        var boundsSize = trsMatrix.MultiplyVector(meshBounds.size);

                        Gizmos.DrawWireCube(boundsCenter, boundsSize);
                    }
                }
            }

            if(EditorDrawBoundingBox && hasData)
            {
                Gizmos.color = Color.green * 0.75f;
                Gizmos.DrawWireCube(_wholeBounds.center, _wholeBounds.size); 
            }
        }

        private void OnValidate()
        {
            if(EditorMinimumDistanceBetweenInstances < 0f)
            {
                EditorMinimumDistanceBetweenInstances = 0f;
                UnityEditor.EditorUtility.SetDirty(this); 
            }
        }
#endif

        /// <summary>
        /// Ensures data is in a valid state. Returns true if anything changed.
        /// </summary>
        public bool ValidateData()
        {
            var anyChanges = false;

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];

                var TrsDatas = subgroupData.TrsDatas;
                var Metadatas = subgroupData.Metadatas;

                // sometimes undo/redo seems to give us bad data.
                // so, scan for it and remove it to prevent any future issues 
                for (var i = TrsDatas.Length - 1; i >= 0; --i)
                {
                    var data = TrsDatas[i];
                    var trsMatrix = (Matrix4x4)data.trs;
                    if (!trsMatrix.ValidTRS())
                    {
                        RemoveSingle(s, i);

                        Debug.LogWarning($"Unity's Undo/Redo broke a foliage instance, so it has been removed.");

                        anyChanges = true;
                    }
                }

                // just in case this somehow happens.. 
                if (TrsDatas.Length != Metadatas.Length)
                {
                    Debug.LogWarning($"Size mismatch between TrsDatas and Metadatas on a FoliageGroup. Fixing.", this);

                    if (TrsDatas.Length > Metadatas.Length)
                    {
                        var sizeDifference = Mathf.Abs(TrsDatas.Length - Metadatas.Length);
                        
                        System.Array.Resize(ref TrsDatas, Metadatas.Length);

                        Debug.LogWarning($"To resolve the size mismatch, {sizeDifference} entries have been removed from TrsDatas.", this);
                    }
                    else
                    {
                        var sizeDifference = Mathf.Abs(TrsDatas.Length - Metadatas.Length);
                        
                        System.Array.Resize(ref Metadatas, TrsDatas.Length);

                        Debug.LogWarning($"To resolve the size mismatch, {sizeDifference} entries have been removed from Metadatas.", this);
                    }

                    anyChanges = true;
                }
            }

            return anyChanges;
        }

        /// <summary>
        /// Completely clears graphics memory allocated for this foliage group. Automatically ran when this group is disabled. 
        /// </summary>
        public void DisposeGraphicsBuffers()
        {
            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];
                subgroupData.Dispose();
            }
        }

#if FOLIAGE_FOUND_BURST
        [BurstCompile]
#endif
        private struct RecalculateBoundsJob : IJob
        {
            public NativeArray<FoliageTransformationData> Input;
            public int InputLength;

            public Bounds MeshBounds;

            public FoliageSpace SpaceMode;
            public Matrix4x4 localToWorld;

            public NativeArray<Bounds> Output;

            public void Execute()
            {
                var bounds = new Bounds();
                var meshBounds = MeshBounds;

                for (var i = 0; i < InputLength; ++i)
                {
                    var trsData = Input[i];
                    var trsMatrix = (Matrix4x4) trsData.trs;

                    if(SpaceMode == FoliageSpace.Self)
                    {
                        trsMatrix = localToWorld * trsMatrix;
                    }

                    if (i == 0)
                    {
                        var center = trsMatrix.MultiplyPoint(meshBounds.center);
                        var boundsSize = trsMatrix.MultiplyVector(meshBounds.size);

                        bounds = new Bounds(center, boundsSize);   
                    }
                    else
                    {
                        var instanceMin = trsMatrix.MultiplyPoint(meshBounds.min);
                        var instanceMax = trsMatrix.MultiplyPoint(meshBounds.max);

                        bounds.min = Vector3.Min(bounds.min, instanceMin);
                        bounds.min = Vector3.Min(bounds.min, instanceMax);
                        bounds.max = Vector3.Max(bounds.max, instanceMin);
                        bounds.max = Vector3.Max(bounds.max, instanceMax);
                    }
                }

                Output[0] = bounds; 
            }
        }

        /// <summary>
        /// Recalculates the bounding box of the whole foliage group.
        /// </summary>
        public void RecalculateBounds()
        {
            JobHandle dependencyChain = default;

            // todo: avoid this allocation somehow 
            var nativeInputs = new NativeArray<FoliageTransformationData>[Subgroups.Count];
            var nativeOutputs = new NativeArray<Bounds>[Subgroups.Count];

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];
                var foliageData = subgroupData.FoliageData;

                if (foliageData == null || foliageData.LODs == null || foliageData.LODs.Length == 0)
                {
                    continue;
                }

                var bounds = foliageData.GetOverallBounds();
                var TrsDatas = subgroupData.TrsDatas;

                var nativeOutput = new NativeArray<Bounds>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var nativeInput = new NativeArray<FoliageTransformationData>(TrsDatas.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                for (var t = 0; t < TrsDatas.Length; ++t) nativeInput[t] = TrsDatas[t];

                nativeInputs[s] = nativeInput;
                nativeOutputs[s] = nativeOutput;

                var calcBoundsJob = new RecalculateBoundsJob()
                {
                    Input = nativeInput,
                    InputLength = nativeInput.Length,

                    SpaceMode = SpaceMode,
                    localToWorld = transform.localToWorldMatrix,

                    MeshBounds = bounds,
                    Output = nativeOutput,
                };

                var calcBoundsHandle = calcBoundsJob.Schedule();
                dependencyChain = JobHandle.CombineDependencies(calcBoundsHandle, dependencyChain);
            }

            dependencyChain.Complete(); 

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];
                var foliageData = subgroupData.FoliageData;
                if (foliageData == null || foliageData.LODs == null || foliageData.LODs.Length == 0)
                {
                    continue;
                }

                subgroupData._bounds = nativeOutputs[s][0];
                nativeInputs[s].Dispose();
                nativeOutputs[s].Dispose();

                if(s == 0)
                {
                    _wholeBounds = subgroupData._bounds;
                }
                else
                {
                    var min = _wholeBounds.min;
                    var max = _wholeBounds.max;

                    min = Vector3.Min(min, subgroupData._bounds.min);
                    max = Vector3.Max(max, subgroupData._bounds.max);
                    min = Vector3.Min(min, subgroupData._bounds.max);
                    max = Vector3.Max(max, subgroupData._bounds.min);

                    _wholeBounds.SetMinMax(min, max); 
                }
            }
        }

        /// <summary>
        /// Appends a single foliage data point to the group. Use AppendMany() if you have more than one you want to append. Use SetDirty() when finished editing data. 
        /// </summary>
        /// <param name="data"></param>
        public void AppendSingle(int subgroupIndex, FoliageTransformationData trs, FoliageMetadata metadata)
        {
            if(SpaceMode == FoliageSpace.Self)
            {
                trs.trs = (float4x4) (transform.worldToLocalMatrix * (Matrix4x4) trs.trs);
            }

            var subGroup = Subgroups[subgroupIndex];
            var newLength = subGroup.TrsDatas.Length + 1;
            System.Array.Resize(ref subGroup.TrsDatas, newLength);
            System.Array.Resize(ref subGroup.Metadatas, newLength);

            subGroup.TrsDatas[newLength - 1] = trs;
            subGroup.Metadatas[newLength - 1] = metadata;
        }

        /// <summary>
        /// Removes an index from the foliage group. Uses a RemoveAtSwapBack. Use SetDirty() when finished editing data. 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveSingle(int subgroupIndex, int index)
        {
            RemoveAtSwapBack(ref Subgroups[subgroupIndex].TrsDatas, index);
            RemoveAtSwapBack(ref Subgroups[subgroupIndex].Metadatas, index);
        }

        private void RemoveAtSwapBack<T>(ref T[] data, int index) where T : struct
        {
            var prevCount = data.Length;
            var prevIndex = prevCount - 1;
            var prevData = data[prevIndex];

            data[index] = prevData;
            System.Array.Resize(ref data, prevCount - 1);
        }
        
        /// <summary>
        /// If marked as dirty, this will rebuild the graphics buffers and the property block used for rendering.
        /// </summary>
        public void UpdateGraphicsBuffers(bool supportsComputeShaders)
        {
            // check if transform changed, if self spae 
            if (SpaceMode == FoliageSpace.Self && transform.hasChanged)
            {
                transform.hasChanged = false;
                SetDirty();
            }

            if (!_isDirty)
            {
                return;
            }

            DisposeGraphicsBuffers();
            _isDirty = false;

            var needsDirectData = false; 
            var needsIndirectData = false; 

            var foliageRenderingManager = FoliageRenderingManager.GetInstance(true);
            if(foliageRenderingManager != null)
            {
                var foliageRenderingMode = foliageRenderingManager.renderMode;

                if(foliageRenderingMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstanced)
                {
                    needsDirectData = true;
                }

                if(supportsComputeShaders && foliageRenderingMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstancedIndirect)
                {
                    needsIndirectData = true; 
                }
            }

            var localToWorldMatrix = transform.localToWorldMatrix;
            var worldToLocalMatrix = transform.worldToLocalMatrix;

            var strideTrsData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FoliageTransformationData));
            var strideInverseTrsData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float4x4));
            var strideMetadata = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FoliageMetadata));

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupData = Subgroups[s];
                var foliageData = subgroupData.FoliageData;

                if(foliageData == null)
                {
                    continue;
                }

                var TrsDatas = subgroupData.TrsDatas;
                var Metadatas = subgroupData.Metadatas;
                var LODs = foliageData.LODs;

                // no data, nothing to do.. 
                var instanceCount = TrsDatas.Length;
                if (instanceCount == 0 || LODs == null || LODs.Length == 0)
                {
                    continue;
                }

                if (needsDirectData)
                {
                    if (LightProbeUsage != LightProbeMode.Off)
                    {
                        subgroupData._lightProbeSHData = new SphericalHarmonicsL2[instanceCount];
                        subgroupData._occlusionProbeSHData = new Vector4[instanceCount];

                        if (LightProbeUsage == LightProbeMode.PerInstanceLightProbe)
                        {
                            subgroupData._lightProbePositionData = new Vector3[instanceCount];

                            for (var t = 0; t < TrsDatas.Length; ++t)
                            {
                                var trsData = TrsDatas[t];
                                var trsMatrix = (Matrix4x4)trsData.trs;

                                var position = trsMatrix.GetPositionFromMatrix();
                                subgroupData._lightProbePositionData[t] = position;
                            }

                            LightProbes.CalculateInterpolatedLightAndOcclusionProbes(subgroupData._lightProbePositionData, subgroupData._lightProbeSHData, subgroupData._occlusionProbeSHData);
                        }
                        else if (LightProbeUsage == LightProbeMode.UseSingleAnchorOverride)
                        {
                            var anchorPosition = transform.position;

                            if (LightProbeAnchorOverride != null)
                            {
                                anchorPosition = LightProbeAnchorOverride.position;
                            }

                            LightProbes.GetInterpolatedProbe(anchorPosition, null, out SphericalHarmonicsL2 probe);

                            for (var t = 0; t < TrsDatas.Length; ++t)
                            {
                                subgroupData._lightProbeSHData[t] = probe;
                            }
                        }
                        else if (LightProbeUsage == LightProbeMode.UseSingleAnchorGroupCenter)
                        {
                            LightProbes.GetInterpolatedProbe(_wholeBounds.center, null, out SphericalHarmonicsL2 probe);

                            for (var t = 0; t < TrsDatas.Length; ++t)
                            {
                                subgroupData._lightProbeSHData[t] = probe;
                            }
                        }
                    }

                    // update trs cache 
                    var maxGroupSize = FoliageRenderingManager.GetCurrentBatchSize();
                    var splitCount = (TrsDatas.Length / maxGroupSize) + 1; 

                    subgroupData._trsCacheFallback = new Matrix4x4[splitCount][][][];
                    subgroupData._trsCacheNative = new NativeArray<Matrix4x4>[splitCount][][];
                    subgroupData._propertyBlocks = new MaterialPropertyBlock[splitCount];

                    if (supportsComputeShaders)
                    {
                        subgroupData._directBuffers = new GraphicsBuffer[splitCount];
                    }
                    else
                    {
                        subgroupData._directTextureFallbacks = new Texture2D[splitCount];
                    }

                    if(LightProbeUsage != LightProbeMode.Off)
                    {
                        subgroupData._lightProbeShDataSplit = new SphericalHarmonicsL2[splitCount][]; 
                        subgroupData._occlusionProbeSHDataSplit = new Vector4[splitCount][]; 
                    }

                    for (var splitIndex = 0; splitIndex < splitCount; ++splitIndex)
                    {
                        var groupIndexStart = splitIndex * maxGroupSize;
                        var groupSize = Mathf.Min(maxGroupSize, TrsDatas.Length - groupIndexStart);

                        // Debug.Log($"[{splitIndex} / {splitCount}] {groupSize}");

                        var trsLodGroups = new NativeArray<Matrix4x4>[LODs.Length][];
                        var trsLodGroupsFallback = new Matrix4x4[LODs.Length][][];
                        subgroupData._trsCacheNative[splitIndex] = trsLodGroups;
                        subgroupData._trsCacheFallback[splitIndex] = trsLodGroupsFallback;

                        JobHandle combinedDependencies = default;

                        for (var lodIndex = 0; lodIndex < LODs.Length; ++lodIndex)
                        {
                            var lod = LODs[lodIndex];

                            var trsRenderdataGroups = new NativeArray<Matrix4x4>[lod.RenderData.Length];
                            var trsRenderdataGroupsFallback = new Matrix4x4[lod.RenderData.Length][];

                            trsLodGroups[lodIndex] = trsRenderdataGroups;
                            trsLodGroupsFallback[lodIndex] = trsRenderdataGroupsFallback;

                            for (var renderDataIndex = 0; renderDataIndex < lod.RenderData.Length; ++renderDataIndex)
                            {
                                var renderData = lod.RenderData[renderDataIndex];
                                var renderDataLocalToWorld = Matrix4x4.TRS(renderData.localPosition, renderData.localRotation.GetQuaternionSafe(), renderData.localScale);

                                if (groupSize == 0)
                                {
                                    trsRenderdataGroups[renderDataIndex] = new NativeArray<Matrix4x4>(groupSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                                    trsRenderdataGroupsFallback[renderDataIndex] = new Matrix4x4[groupSize];
                                } 
                                else
                                {
                                    var input = new NativeArray<FoliageTransformationData>(groupSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                                        input.CopyFromFast(TrsDatas, groupSize, groupIndexStart);

                                    var output = new NativeArray<Matrix4x4>(groupSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                                    trsRenderdataGroups[renderDataIndex] = output;
                                    trsRenderdataGroupsFallback[renderDataIndex] = new Matrix4x4[groupSize];

                                    var job = new UpdateTransformationDirectData()
                                    {
                                        Input = input,
                                        Output = output,

                                        groupTransformLocalToWorld = localToWorldMatrix,
                                        renderDataLocalToWorld = renderDataLocalToWorld,
                                        spaceMode = SpaceMode,

                                        length = groupSize,
                                    };

                                    var handle = job.Schedule();
                                    combinedDependencies = JobHandle.CombineDependencies(combinedDependencies, handle); 
                                }
                            }
                        }

                        // complete the sets here, to avoid race conditions 
                        combinedDependencies.Complete();

                        // configure fallback matrices 
                        // would be nice to not have to use this, but the native Graphics.RenderMeshInstanced() is unfortunately too slow,
                        // also, this will always be necessary to support older unity versions (2020 and below)
                        for (var lodIndex = 0; lodIndex < LODs.Length; ++lodIndex)
                        {
                            var trsRenderdataGroups = trsLodGroups[lodIndex];
                            var trsRenderdataGroupsFallback = trsLodGroupsFallback[lodIndex];

                            for (var renderDataIndex = 0; renderDataIndex < trsRenderdataGroups.Length; ++renderDataIndex)
                            {
                                var renderDataGroup = trsRenderdataGroups[renderDataIndex];
                                var renderDataGroupFallback = trsRenderdataGroupsFallback[renderDataIndex];

                                renderDataGroup.CopyToFast(renderDataGroupFallback, renderDataGroupFallback.Length);
                            }
                        }

                        if (LightProbeUsage != LightProbeMode.Off)
                        {
                            subgroupData._lightProbeShDataSplit[splitIndex] = new SphericalHarmonicsL2[groupSize];
                            subgroupData._occlusionProbeSHDataSplit[splitIndex] = new Vector4[groupSize];

                            for (var i = 0; i < groupSize; ++i)
                            {
                                var _lightProbeShDataSplit = subgroupData._lightProbeShDataSplit[splitIndex];
                                    _lightProbeShDataSplit[i] = subgroupData._lightProbeSHData[groupIndexStart + i];

                                var _occlusionProbeSHDataSplit = subgroupData._occlusionProbeSHDataSplit[splitIndex];
                                    _occlusionProbeSHDataSplit[i] = subgroupData._occlusionProbeSHData[groupIndexStart + i];

                                subgroupData._lightProbeShDataSplit[splitIndex] = _lightProbeShDataSplit;
                                subgroupData._occlusionProbeSHDataSplit[splitIndex] = _occlusionProbeSHDataSplit;
                            }
                        }

                        // direct rendering property block
                        var groupPropertyBlock = new MaterialPropertyBlock();
                        groupPropertyBlock.SetMatrix(_FoliageLocalToWorld, localToWorldMatrix);
                        groupPropertyBlock.SetMatrix(_FoliageWorldToLocal, worldToLocalMatrix);

                        if(LightProbeUsage != LightProbeMode.Off)
                        {
                            groupPropertyBlock.CopySHCoefficientArraysFrom(subgroupData._lightProbeShDataSplit[splitIndex]);
                            groupPropertyBlock.CopyProbeOcclusionArrayFrom(subgroupData._occlusionProbeSHDataSplit[splitIndex]);
                        }

                        // direct rendering graphics buffer
                        if (supportsComputeShaders)
                        {
                            if(groupSize > 0)
                            {
                                var directBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, groupSize, strideMetadata);
                                    directBuffer.SetData(Metadatas, groupIndexStart, 0, groupSize);
                                subgroupData._directBuffers[splitIndex] = directBuffer;

                                groupPropertyBlock.SetBuffer(_FoliageMetadataBuffer, directBuffer);
                            }
                            else
                            {
                                subgroupData._directBuffers[splitIndex] = null;
                            }
                        }

                        // if StructuredBuffers (compute) are not supported, fall back to using a texture 
                        else
                        {
                            var directFallbackTexture = new Texture2D(groupSize, 1, TextureFormat.ARGB32, false, false); 

                            for(var i = 0; i < groupSize; ++i)
                            {
                                var metaData = Metadatas[groupIndexStart + i];
                                var metaDataColor = metaData.color;
                                directFallbackTexture.SetPixel(i, 0, new Color(metaDataColor.x, metaDataColor.y, metaDataColor.z, metaDataColor.w)); 
                            }

                            directFallbackTexture.Apply();
                            subgroupData._directTextureFallbacks[splitIndex] = directFallbackTexture;
                            groupPropertyBlock.SetTexture(_FoliageMetadataFallbackTexture, directFallbackTexture);
                            groupPropertyBlock.SetVector(_FoliageMetadataFallbackMetadata, new float4(groupSize, 0, 0, 0));
                        }

                        subgroupData._propertyBlocks[splitIndex] = groupPropertyBlock;
                    }
                }
                
                if(needsIndirectData)
                {
                    // manage buffers
                    subgroupData._indirectArgsBuffers = new GraphicsBuffer[foliageData.LODs.Length][];
                    subgroupData._indirectTrsBuffer = new GraphicsBuffer[foliageData.LODs.Length][];

                    subgroupData._indirectMetadataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Metadatas.Length, strideMetadata);
                    subgroupData._indirectMetadataBuffer.SetData(Metadatas);
                    
                    // manage property block (indirect rendering) 
                    if (subgroupData._indirectPropertyBlock == null)
                    {
                        subgroupData._indirectPropertyBlock = new MaterialPropertyBlock[foliageData.LODs.Length][];
                    }

                    for (var l = 0; l < foliageData.LODs.Length; ++l)
                    {
                        var foliageLod = foliageData.LODs[l];
                        var indirectArgsForLod = new GraphicsBuffer[foliageLod.RenderData.Length];
                        var indirectTrsForLod = new GraphicsBuffer[foliageLod.RenderData.Length];

                        subgroupData._indirectArgsBuffers[l] = indirectArgsForLod;
                        subgroupData._indirectTrsBuffer[l] = indirectTrsForLod;

                        var indirectPropertyBlockForLod = subgroupData._indirectPropertyBlock[l];

                        if(indirectPropertyBlockForLod == null)
                        {
                            indirectPropertyBlockForLod = new MaterialPropertyBlock[foliageLod.RenderData.Length];
                            subgroupData._indirectPropertyBlock[l] = indirectPropertyBlockForLod;
                        }

                        for (var r = 0; r < foliageLod.RenderData.Length; ++r)
                        {
                            var rendererData = foliageLod.RenderData[r];
                            var renderDataLocalToWorld = Matrix4x4.TRS(rendererData.localPosition, rendererData.localRotation.GetQuaternionSafe(), rendererData.localScale);

                            indirectArgsForLod[r] = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, sizeof(float));
                            indirectArgsForLod[r].SetData(new uint[]
                                {
                                    rendererData.mesh.GetIndexCount(0),
                                    (uint) TrsDatas.Length,
                                    rendererData.mesh.GetIndexStart(0),
                                    rendererData.mesh.GetBaseVertex(0),
                                    0,
                                }
                            );

                            // handle self space mode.. 
                            var trsCount = TrsDatas.Length;

                            var input = new NativeArray<FoliageTransformationData>(trsCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory); 
                            var output = new NativeArray<FoliageTransformationData>(trsCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                            input.CopyFromFast(TrsDatas, trsCount);

                            var transformJob = new UpdateTransformationData()
                            {
                                Input = input,
                                Output = output,
                                renderDataLocalToWorld = renderDataLocalToWorld,
                                groupTransformLocalToWorld = localToWorldMatrix,
                                spaceMode = SpaceMode,
                            };

                            var transformHandle = transformJob.Schedule(trsCount, 256);
                                transformHandle.Complete();

                            indirectTrsForLod[r] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, trsCount, strideTrsData);
                            indirectTrsForLod[r].SetData(output);

                            input.Dispose(); 
                            output.Dispose(); 

                            // configure material property block 
                            var indirectPropertyBlockForRenderData = indirectPropertyBlockForLod[r];
                            if(indirectPropertyBlockForRenderData == null)
                            {
                                indirectPropertyBlockForRenderData = new MaterialPropertyBlock();
                                indirectPropertyBlockForLod[r] = indirectPropertyBlockForRenderData;
                            }

                            indirectPropertyBlockForRenderData.Clear();
                            indirectPropertyBlockForRenderData.SetBuffer(_FoliageTrsBuffer, indirectTrsForLod[r]);
                            indirectPropertyBlockForRenderData.SetBuffer(_FoliageMetadataBuffer, subgroupData._indirectMetadataBuffer);
                            indirectPropertyBlockForRenderData.SetMatrix(_FoliageLocalToWorld, localToWorldMatrix);
                            indirectPropertyBlockForRenderData.SetMatrix(_FoliageWorldToLocal, worldToLocalMatrix);

                            // note: because of the 1023 limit on light probes, this method is incompatible with indirect rendering mode until the shader is modified to support it 
                            // if (LightProbeUsage != LightProbeMode.Off)
                            // {
                            //     indirectPropertyBlockForRenderData.CopySHCoefficientArraysFrom(subgroupData._lightProbeSHData);
                            //     indirectPropertyBlockForRenderData.CopyProbeOcclusionArrayFrom(subgroupData._occlusionProbeSHData);
                            // }
                        }
                    }
                }
            }

            // update bounds 
            RecalculateBounds();
        }


#if FOLIAGE_FOUND_BURST
        [BurstCompile]
#endif
        private struct UpdateTransformationData : IJobParallelFor
        {
            [ReadOnly] public NativeArray<FoliageTransformationData> Input;
            [WriteOnly] public NativeArray<FoliageTransformationData> Output;

            public Matrix4x4 renderDataLocalToWorld;
            public Matrix4x4 groupTransformLocalToWorld;
            public FoliageSpace spaceMode;

            public void Execute(int index)
            {
                var trs = (Matrix4x4)Input[index].trs;
                    trs = trs * renderDataLocalToWorld;

                if (spaceMode == FoliageSpace.Self)
                {
                    trs = groupTransformLocalToWorld * trs;
                }

                Output[index] = new FoliageTransformationData()
                {
                    trs = trs,
                    inverseTrs = trs.inverse,
                };
            }
        }

#if FOLIAGE_FOUND_BURST
        [BurstCompile]
#endif
        private struct UpdateTransformationDirectData : IJob
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<FoliageTransformationData> Input;
            [WriteOnly] public NativeArray<Matrix4x4> Output;

            public Matrix4x4 renderDataLocalToWorld;
            public Matrix4x4 groupTransformLocalToWorld;
            public FoliageSpace spaceMode;

            public int length;

            public void Execute()
            {
                for(var i = 0; i < length; ++i)
                {
                    var data = Input[i];

                    var trs = (Matrix4x4) data.trs;
                        trs = trs * renderDataLocalToWorld;

                    if (spaceMode == FoliageSpace.Self)
                    {
                        trs = groupTransformLocalToWorld * trs;
                    }

                    Output[i] = trs;
                }
            }
        }

        /// <summary>
        /// Returns the MaterialPropertyBlock to use for indirect rendering. 
        /// </summary>
        /// <returns></returns>
        public MaterialPropertyBlock GetIndirectPropertyBlock(int subgroupIndex, int lodIndex, int renderDataIndex)
        {
            return Subgroups[subgroupIndex]._indirectPropertyBlock[lodIndex][renderDataIndex];
        }

        /// <summary>
        /// Returns a GraphicsBuffer to use for DrawProceduralIndirect(). 
        /// </summary>
        /// <returns></returns>
        public GraphicsBuffer GetArgs(int subgroupIndex, int lodIndex, int renderDataIndex)
        {
            return Subgroups[subgroupIndex]._indirectArgsBuffers[lodIndex][renderDataIndex];
        }

        /// <summary>
        /// Marks the group as dirty, so the next update will rebuild buffers. 
        /// </summary>
        public void SetDirty()
        {
            _isDirty = true; 
        }

        /// <summary>
        /// Returns an array of split Matrix4x4 matrices intended for use for DrawMeshInstanced(). 
        /// Because DrawMeshInstanced() can only render 1024 at a time, these are split into groups of 1024.
        /// This native variant is for use with Graphics.RenderMeshInstanced(). 
        /// </summary>
        /// <returns></returns>
        public NativeArray<Matrix4x4>[][][] GetTrsMatrices(int subgroupIndex)
        {
            return Subgroups[subgroupIndex]._trsCacheNative;
        }

        /// <summary>
        /// Returns an array of split Matrix4x4 matrices intended for use for DrawMeshInstanced(). 
        /// Because DrawMeshInstanced() can only render 1024 at a time, these are split into groups of 1024.
        /// This falback variant is for using with Graphics.DrawMeshInstanced().
        /// </summary>
        /// <returns></returns>
        public Matrix4x4[][][][] GetTrsMatricesFallback(int subgroupIndex)
        {
            return Subgroups[subgroupIndex]._trsCacheFallback;
        }

        /// <summary>
        /// Returns the split MaterialPropertyBlocks(). Used in direct instanced rendering. 
        /// </summary>
        /// <returns></returns>
        public MaterialPropertyBlock[] GetSplitPropertyBlocks(int subgroupIndex)
        {
            return Subgroups[subgroupIndex]._propertyBlocks;
        }

        /// <summary>
        /// Returns the bounds of the foliage group. Useful for indirect rendering. 
        /// </summary>
        /// <returns></returns>
        public Bounds GetBounds()
        {
            return _wholeBounds;
        }

        /// <summary>
        /// Returns the bounds of a subgroup within this foliage group. 
        /// </summary>
        /// <param name="subgroupIndex"></param>
        /// <returns></returns>
        public Bounds GetSubgroupBounds(int subgroupIndex)
        {
            return Subgroups[subgroupIndex]._bounds;
        }

        /// <summary>
        /// Removes all serialized data from this group. 
        /// </summary>
        public void ClearData()
        {
            for(var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroup = Subgroups[s];
                subgroup.TrsDatas = new FoliageTransformationData[0];
                subgroup.Metadatas = new FoliageMetadata[0];
            }
        }

        /// <summary>
        /// Returns a LOD based on the input position. 
        /// LODs are calculated based on the distance between the input position and it's position projected on the AABB of the FoliageGroup.
        /// </summary>
        /// <param name="cameraPosition"></param>
        /// <returns></returns>
        public int CalculateLodIndex(int subgroupIndex, Vector3 cameraPosition)
        {

            var subgroupData = Subgroups[subgroupIndex];
            var foliageData = subgroupData.FoliageData;
            var LODs = foliageData.LODs;

            var boundsPosition = subgroupData._bounds.ClosestPoint(cameraPosition);
            var boundsDistance = Vector3.Distance(cameraPosition, boundsPosition);
            if (subgroupData._bounds.Contains(cameraPosition)) boundsDistance = 0f;

            for (var li = 0; li < LODs.Length; ++li)
            {
                var lod = LODs[li];

                if(boundsDistance <= lod.distance)
                {
                    return li;
                }
            }

            return FoliageMeshData.FoliageMeshLOD.CullIndex; 
        }

        /// <summary>
        /// Sets the space mode for this group. 
        /// If updateTrsData is true, the internal trs matrix data will be updated such that the world space position of individual foliage instances remains the same as before the space conversion.
        /// </summary>
        /// <param name="space"></param>
        /// <param name="updateTrsData"></param>
        public void SetSpaceMode(FoliageSpace space, bool updateTrsData)
        {
            SpaceMode = space; 

            if(updateTrsData)
            {
                var matrix = space == FoliageSpace.Self ? transform.worldToLocalMatrix : transform.localToWorldMatrix;

                for (var s = 0; s < Subgroups.Count; ++s)
                {
                    var subgroup = Subgroups[s];
                    var TrsDatas = subgroup.TrsDatas;

                    for (var t = 0; t < TrsDatas.Length; ++t)
                    {
                        var trsData = TrsDatas[t];

                        trsData.trs = matrix * (Matrix4x4) trsData.trs;

                        TrsDatas[t] = trsData;
                    }
                }
            }
        }

        /// <summary>
        /// Places the transform.position to the bounding box center. 
        /// Maintains the world space position of all of foliage instances.
        /// </summary>
        public void CenterPivot()
        {
            var prevSpace = SpaceMode;

            if(prevSpace == FoliageSpace.Self)
            {
                SetSpaceMode(FoliageSpace.World, true); 
            }

            var prefabPlacerGroup = transform.Find("PrefabPlacerGroup");
            
            if(prefabPlacerGroup != null)
            {
                prefabPlacerGroup.SetParent(null, true);
            }

            transform.position = _wholeBounds.center;

            if (prefabPlacerGroup != null)
            {
                prefabPlacerGroup.SetParent(transform, true);
            }

            if (prevSpace == FoliageSpace.Self)
            {
                SetSpaceMode(FoliageSpace.Self, true);
            }
        }

        /// <summary>
        /// Creates a copy of the foliage instancing data and returns the copy. 
        /// Useful for if you need to serialize the data and store for later. 
        /// Note: does not serialize FoliageMeshData data. 
        /// </summary>
        /// <param name="trsData"></param>
        /// <param name="metadata"></param>
        public SerializableFoliageGroup GetAllFoliageData()
        {
            var serializedData = new SerializableFoliageGroup();
                serializedData.data = new List<SerializableFoliageSubgroup>(Subgroups.Count);

            for(var s = 0; s < Subgroups.Count; ++s)
            {
                var serializedSubgroup = new SerializableFoliageSubgroup();
                var subgroupOriginal = Subgroups[s];

                serializedSubgroup.FoliageMeshDataName = subgroupOriginal.FoliageData.name;

                serializedSubgroup.TrsDatas = new List<FoliageTransformationData>(subgroupOriginal.TrsDatas.Length);
                for(var i = 0; i < subgroupOriginal.TrsDatas.Length; ++i)
                {
                    serializedSubgroup.TrsDatas.Add(subgroupOriginal.TrsDatas[i]);
                }

                serializedSubgroup.Metadatas = new List<FoliageMetadata>(subgroupOriginal.Metadatas.Length);
                for (var i = 0; i < subgroupOriginal.Metadatas.Length; ++i)
                {
                    serializedSubgroup.Metadatas.Add(subgroupOriginal.Metadatas[i]);
                }

                serializedData.data.Add(serializedSubgroup);
            }

            return serializedData; 
        }

        /// <summary>
        /// Copies in input data into the foliage group. 
        /// Useful for loading from some serialized state. 
        /// Note: does not load FoliageMeshData, so you'll need to first ensure your subgroups are setup with the correct number and FoliageMeshData before loading data.
        /// </summary>
        /// <param name="trsData"></param>
        /// <param name="metadata"></param>
        public void SetAllFoliageData(SerializableFoliageGroup serializedData)
        {
            // todo, support this somehow 
            if(Subgroups.Count != serializedData.data.Count)
            {
                Debug.LogError($"Trying to load a differently sized subgroup. This is not currently supported.");
                return;
            }

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroupOriginal = serializedData.data[s];
                var subgroupLocal = Subgroups[s];

                subgroupLocal.TrsDatas = new FoliageTransformationData[subgroupOriginal.TrsDatas.Count];
                for (var i = 0; i < subgroupOriginal.TrsDatas.Count; ++i)
                {
                    subgroupLocal.TrsDatas[i] = subgroupOriginal.TrsDatas[i];
                }

                subgroupLocal.Metadatas = new FoliageMetadata[subgroupOriginal.Metadatas.Count];
                for (var i = 0; i < subgroupOriginal.Metadatas.Count; ++i)
                {
                    subgroupLocal.Metadatas[i] = subgroupOriginal.Metadatas[i];
                }
            }
        }

        public void CacheLodIndex(int subgroupIndex, int lodIndex)
        {
            var subGroup = Subgroups[subgroupIndex];
            subGroup._lodIndexCache = lodIndex;
        }

        public int GetLodIndexCache(int subgroupIndex)
        {
            var subGroup = Subgroups[subgroupIndex];
            return subGroup._lodIndexCache;
        }

        public bool GetHasBakedMeshes()
        {
            foreach (var subGroup in Subgroups)
            {
                if(subGroup.BakedMeshLods != null && subGroup.BakedMeshLods.Count > 0)
                {
                    return true;
                }
            }

            return false; 
        }

#if UNITY_EDITOR
        public void EditorSerializedBakedMeshes()
        {
            var foliageResources = FoliageResources.FindConfig();

            var proceduralFolderName = "ProceduralFoliageMeshes";
            var proceduralPath = $"{foliageResources.ProceduralMeshAssetPath}/{proceduralFolderName}";
            if (!UnityEditor.AssetDatabase.IsValidFolder(proceduralPath))
            {
                UnityEditor.AssetDatabase.CreateFolder(foliageResources.ProceduralMeshAssetPath, proceduralFolderName);
            }

            foreach (var subGroup in Subgroups)
            {
                if (subGroup.BakedMeshLods != null && subGroup.BakedMeshLods.Count > 0)
                {
                    for (var lodIndex = 0; lodIndex < subGroup.BakedMeshLods.Count; ++lodIndex)
                    {
                        var bakedMeshLod = subGroup.BakedMeshLods[lodIndex];
                        var bakedRenderDatas = bakedMeshLod.BakedRenderDatas;

                        for(var r = 0; r < bakedRenderDatas.Count; ++r)
                        {
                            var renderDataGroup = bakedRenderDatas[r];

                            // serialize material 
                            var materialAssetPath = $"{proceduralPath}/{System.Guid.NewGuid()}.asset";
                            UnityEditor.AssetDatabase.CreateAsset(renderDataGroup.material, materialAssetPath);
                            renderDataGroup.material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);

                            for (var m = 0; m < renderDataGroup.meshes.Count; ++m)
                            {
                                var bakedMesh = renderDataGroup.meshes[m];

                                // serialize mesh 
                                var assetPath = $"{proceduralPath}/{System.Guid.NewGuid()}.asset";
                                UnityEditor.AssetDatabase.CreateAsset(bakedMesh, assetPath);
                                renderDataGroup.meshes[m] = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                            }
                        }
                    }
                }
            }
        }

        public bool EditorGetHasSerializedMeshes()
        {
            foreach (var subGroup in Subgroups)
            {
                if (subGroup.BakedMeshLods != null && subGroup.BakedMeshLods.Count > 0)
                {
                    for (var lodIndex = 0; lodIndex < subGroup.BakedMeshLods.Count; ++lodIndex)
                    {
                        var bakedMeshLod = subGroup.BakedMeshLods[lodIndex];
                        var bakedRenderDatas = bakedMeshLod.BakedRenderDatas;

                        for (var r = 0; r < bakedRenderDatas.Count; ++r)
                        {
                            var renderDataGroup = bakedRenderDatas[r];

                            for (var m = 0; m < renderDataGroup.meshes.Count; ++m)
                            {
                                var bakedMesh = renderDataGroup.meshes[m];
                                var result = UnityEditor.AssetDatabase.GetAssetPath(bakedMesh);

                                if(!string.IsNullOrEmpty(result))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false; 
        }
#endif

        public void DisposeBakedMeshes()
        {
            foreach (var subGroup in Subgroups)
            {
                if(subGroup.BakedMeshLods != null && subGroup.BakedMeshLods.Count > 0)
                {
                    for(var l = 0; l < subGroup.BakedMeshLods.Count; ++l)
                    {
                        var bakedMeshLod = subGroup.BakedMeshLods[l];
                        var bakedRenderDatas = bakedMeshLod.BakedRenderDatas;

                        for (var r = 0; r < bakedRenderDatas.Count; ++r)
                        {
                            var renderDataGroup = bakedRenderDatas[r];

                            if (renderDataGroup.material != null)
                            {
#if UNITY_EDITOR
                                if (Application.isPlaying)
                                {
                                    Material.Destroy(renderDataGroup.material);
                                }
                                else
                                {
                                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(renderDataGroup.material);
                                    if (!string.IsNullOrEmpty(assetPath))
                                    {
                                        Debug.Log($"Deleted {assetPath}");
                                        UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                                    }
                                    else
                                    {
                                        Debug.Log($"Could not find asset for {renderDataGroup.material.name}", renderDataGroup.material);
                                        Material.DestroyImmediate(renderDataGroup.material, true);
                                    }
                                }

#else
                                Material.Destroy(renderDataGroup.material);
#endif
                            }

                            for (var m = 0; m < renderDataGroup.meshes.Count; ++m)
                            {
                                var bakedMesh = renderDataGroup.meshes[m];
                                if (bakedMesh != null)
                                {
#if UNITY_EDITOR
                                    if (Application.isPlaying)
                                    {
                                        Mesh.Destroy(bakedMesh);
                                    }
                                    else
                                    {
                                        var assetPath = UnityEditor.AssetDatabase.GetAssetPath(bakedMesh);
                                        if (!string.IsNullOrEmpty(assetPath))
                                        {
                                            Debug.Log($"Deleted {assetPath}");
                                            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                                        }
                                        else
                                        {
                                            Debug.Log($"Could not find asset for {bakedMesh.name}", bakedMesh);
                                            Mesh.DestroyImmediate(bakedMesh, true);
                                        }
                                    }
#else
                                    Mesh.Destroy(bakedMesh);
#endif
                                }

                            }
                        }
                    }

                    subGroup.BakedMeshLods.Clear(); 
                }
            }
        }

        private void AddRange<T>(List<T> source, List<T> destination) where T : struct
        {
            for(var i = 0; i < source.Count; ++i)
            {
                destination.Add(source[i]); 
            }
        }

        public void BakeInstancesIntoStaticMeshes()
        {
            for(var s = 0; s < Subgroups.Count; ++s)
            {
                var subGroup = Subgroups[s];

                var foliageData = subGroup.FoliageData;
                if(foliageData == null)
                {
                    continue;
                }
                
                if(foliageData.LODs == null || foliageData.LODs.Length == 0)
                {
                    continue;
                }

                subGroup.BakedMeshLods = new List<FoliageSubgroup.BakedMeshLod>(foliageData.LODs.Length);

                var cache_triangles = new List<int>();
                var cache_vertices = new List<Vector3>();
                var cache_normals = new List<Vector3>();
                var cache_tangents = new List<Vector4>();
                var cache_color = new List<Color>();
                var cache_uv0 = new List<Vector4>();
                // var cache_uv1 = new List<Vector4>(); // reserved by foliage system 
                var cache_uv2 = new List<Vector4>();
                var cache_uv3 = new List<Vector4>();

                var running_triangles = new List<int>();
                var running_vertices = new List<Vector3>();
                var running_normals = new List<Vector3>();
                var running_tangents = new List<Vector4>();
                var running_color = new List<Color>();
                var running_uv0 = new List<Vector4>();
                var running_uv1 = new List<Vector4>();
                var running_uv2 = new List<Vector4>();
                var running_uv3 = new List<Vector4>();

                for (var l = 0; l < foliageData.LODs.Length; ++l)
                {
                    var lod = foliageData.LODs[l];

                    // lod data 
                    var bakedMeshLodData = new FoliageSubgroup.BakedMeshLod();
                        bakedMeshLodData.BakedRenderDatas = new List<FoliageSubgroup.BakedMeshRenderData>();

                    subGroup.BakedMeshLods.Add(bakedMeshLodData);

                    for (var r = 0; r < lod.RenderData.Length; ++r)
                    {
                        var renderData = lod.RenderData[r];

                        var renderDataLocalToWorld = Matrix4x4.TRS(renderData.localPosition, renderData.localRotation.GetQuaternionSafe(), renderData.localScale);
                        var lodMesh = renderData.mesh;

                        cache_triangles.Clear();
                        cache_vertices.Clear();
                        cache_normals.Clear();
                        cache_tangents.Clear();
                        cache_color.Clear();
                        cache_uv0.Clear();
                        // cache_uv1.Clear();
                        cache_uv2.Clear();
                        cache_uv3.Clear();

                        var has_normal = lodMesh.HasVertexAttribute(VertexAttribute.Normal);
                        var has_tangent = lodMesh.HasVertexAttribute(VertexAttribute.Tangent);
                        var has_color = lodMesh.HasVertexAttribute(VertexAttribute.Color);
                        var has_uv0 = lodMesh.HasVertexAttribute(VertexAttribute.TexCoord0);
                        // var has_uv1 = lodMesh.HasVertexAttribute(VertexAttribute.TexCoord1);
                        var has_uv2 = lodMesh.HasVertexAttribute(VertexAttribute.TexCoord2);
                        var has_uv3 = lodMesh.HasVertexAttribute(VertexAttribute.TexCoord3);

                        lodMesh.GetTriangles(cache_triangles, 0);
                        lodMesh.GetVertices(cache_vertices);
                        if (has_normal) lodMesh.GetNormals(cache_normals);
                        if (has_tangent) lodMesh.GetTangents(cache_tangents);
                        if (has_color) lodMesh.GetColors(cache_color);
                        if (has_uv0) lodMesh.GetUVs(0, cache_uv0);
                        // if(has_uv1) lodMesh.GetUVs(1, cache_uv1);
                        if (has_uv2) lodMesh.GetUVs(2, cache_uv2);
                        if (has_uv3) lodMesh.GetUVs(3, cache_uv3);

                        var runningVertexCount = 0;
                        Mesh runningMesh = null;

                        var bakedMeshRenderData = new FoliageSubgroup.BakedMeshRenderData();
                            bakedMeshRenderData.meshes = new List<Mesh>();
                            bakedMeshRenderData.material = Material.Instantiate(renderData.material);
                            bakedMeshRenderData.material.EnableKeyword("_USEBAKEDDATA");
                            bakedMeshRenderData.material.SetFloat("_USEBAKEDDATA", 1f);

                        bakedMeshLodData.BakedRenderDatas.Add(bakedMeshRenderData);

                        for (var t = 0; t < subGroup.TrsDatas.Length; ++t)
                        {
                            var metadata = subGroup.Metadatas[t];
                            var metadataColor = metadata.color;

                            var trsData = subGroup.TrsDatas[t];
                            var trs = (Matrix4x4) trsData.trs;
                                trs = trs * renderDataLocalToWorld;

                            if (runningVertexCount + lodMesh.vertexCount >= ushort.MaxValue || runningMesh == null)
                            {
                                if (runningMesh != null)
                                {
                                    runningMesh.SetVertices(running_vertices);
                                    runningMesh.SetTriangles(running_triangles, 0);

                                    if (has_normal) runningMesh.SetNormals(running_normals);
                                    if (has_tangent) runningMesh.SetTangents(running_tangents);
                                    if (has_color) runningMesh.SetColors(running_color);
                                    if (has_uv0) runningMesh.SetUVs(0, running_uv0);
                                    runningMesh.SetUVs(1, running_uv1); // reserved by foliage system 
                                    if (has_uv2) runningMesh.SetUVs(2, running_uv2);
                                    if (has_uv3) runningMesh.SetUVs(3, running_uv3);
                                }

                                // clear running data 
                                running_triangles.Clear();
                                running_vertices.Clear();
                                running_normals.Clear();
                                running_tangents.Clear();
                                running_color.Clear();
                                running_uv0.Clear();
                                running_uv1.Clear();
                                running_uv2.Clear();
                                running_uv3.Clear();

                                // start next mesh 
                                runningVertexCount = 0;
                                runningMesh = new Mesh();
                                runningMesh.name = $"{gameObject.name}_subgroup{s}_lod{l}_m{bakedMeshRenderData.meshes.Count}";
                                bakedMeshRenderData.meshes.Add(runningMesh);
                            }

                            runningVertexCount += lodMesh.vertexCount;

                            var vertex_offset = running_vertices.Count;

                            for (var vertexIndex = 0; vertexIndex < cache_vertices.Count; ++vertexIndex)
                            {
                                var vertex = cache_vertices[vertexIndex];
                                vertex = (math.mul(trs, new float4(vertex.x, vertex.y, vertex.z, 1f))).xyz;

                                running_vertices.Add(vertex);

                                if (has_normal)
                                {
                                    var normal = cache_normals[vertexIndex];
                                    normal = math.normalize((math.mul(trs, new float4(normal.x, normal.y, normal.z, 0f))).xyz);

                                    running_normals.Add(normal);
                                }

                                if (has_tangent)
                                {
                                    var tangent = cache_tangents[vertexIndex];
                                    tangent = math.mul(trs, tangent);

                                    running_tangents.Add(tangent);
                                }

                                // custom foliage system data 
                                // [foliage color (xyz), foliage index (t)]
                                running_uv1.Add(new float4(metadataColor.x, metadataColor.y, metadataColor.z, t));
                            }

                            // AddRange(cache_vertices, running_vertices);
                            // if (has_normal) AddRange(cache_normals, running_normals);
                            // if (has_tangent) AddRange(cache_tangents, running_tangents);

                            if (has_color) AddRange(cache_color, running_color);
                            if (has_uv0) AddRange(cache_uv0, running_uv0);
                            // if (has_uv1) AddRange(cache_uv1, running_uv1);
                            if (has_uv2) AddRange(cache_uv2, running_uv2);
                            if (has_uv3) AddRange(cache_uv3, running_uv3);

                            for (var triangleIndex = 0; triangleIndex < cache_triangles.Count; ++triangleIndex)
                            {
                                running_triangles.Add(vertex_offset + cache_triangles[triangleIndex]);
                            }
                        }

                        if (running_vertices.Count > 0 && runningMesh != null)
                        {
                            runningMesh.SetVertices(running_vertices);
                            runningMesh.SetTriangles(running_triangles, 0);

                            if (has_normal) runningMesh.SetNormals(running_normals);
                            if (has_tangent) runningMesh.SetTangents(running_tangents);
                            if (has_color) runningMesh.SetColors(running_color);
                            if (has_uv0) runningMesh.SetUVs(0, running_uv0);
                            runningMesh.SetUVs(1, running_uv1); // reserved by foliage system 
                            if (has_uv2) runningMesh.SetUVs(2, running_uv2);
                            if (has_uv3) runningMesh.SetUVs(3, running_uv3);
                        }
                    }
                }
            }
        }

        public void ShowStaticMeshes()
        {
            if(EditorIsDisplayingStaticMeshes)
            {
                return;
            }

            EditorIsDisplayingStaticMeshes = true;

            var childGo = new GameObject("BakedMeshes");
                childGo.transform.SetParent(transform);
                childGo.transform.localPosition = Vector3.zero;
                childGo.transform.localRotation = Quaternion.identity;
                childGo.transform.localScale = Vector3.one;

            var parentLodGroup = childGo.AddComponent<LODGroup>();
            var parentLods = new List<LOD>();

            int maxLodCount = 0;
            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subGroup = Subgroups[s];
                var subGroupLodCount = subGroup.BakedMeshLods.Count;
                maxLodCount = math.max(maxLodCount, subGroupLodCount); 
            }

            var lodsRenderers = new List<List<Renderer>>();
            for(var l = 0; l < maxLodCount; ++l)
            {
                lodsRenderers.Add(new List<Renderer>());

                parentLods.Add(new LOD()
                {
                    screenRelativeTransitionHeight = 1.0f - (float)(l + 1) / (maxLodCount + 1),
                    fadeTransitionWidth = 0f,
                });
            }

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subGroup = Subgroups[s];

                for (var l = 0; l < subGroup.BakedMeshLods.Count; ++l)
                {
                    var lodGroup = subGroup.BakedMeshLods[l];
                    var lodRenderers = lodsRenderers[l];

                    var lodRenderDatas = lodGroup.BakedRenderDatas;

                    for(var r = 0; r < lodRenderDatas.Count; ++r)
                    {
                        var renderDataGroup = lodRenderDatas[r];
                        var lodMaterial = renderDataGroup.material;

                        for(var m = 0; m < renderDataGroup.meshes.Count; ++m)
                        {
                            var lodMesh = renderDataGroup.meshes[m];

                            var lodMeshGo = new GameObject($"{gameObject.name}_subgroup{s}_lod{l}_m{m}");
                                lodMeshGo.transform.SetParent(childGo.transform);
                                lodMeshGo.transform.localPosition = Vector3.zero;
                                lodMeshGo.transform.localRotation = Quaternion.identity;
                                lodMeshGo.transform.localScale = Vector3.one;

                            var lodMeshFilter = lodMeshGo.AddComponent<MeshFilter>();
                                lodMeshFilter.sharedMesh = lodMesh;

                            var lodMeshRenderer = lodMeshGo.AddComponent<MeshRenderer>();
                                lodMeshRenderer.sharedMaterial = lodMaterial;

                            lodRenderers.Add(lodMeshRenderer);
                        }
                    }

                }
            }

            for(var l = 0; l < parentLods.Count; ++l)
            {
                var parentLod = parentLods[l];
                    parentLod.renderers = lodsRenderers[l].ToArray();

                parentLods[l] = parentLod;
            }

            parentLodGroup.SetLODs(parentLods.ToArray()); 
        }

        public void HideStaticMeshes()
        {
            if(!EditorIsDisplayingStaticMeshes)
            {
                return;
            }

            EditorIsDisplayingStaticMeshes = false;

            var bakedMeshesChildTransform = transform.Find("BakedMeshes");
            if(bakedMeshesChildTransform != null)
            {
                if(Application.isPlaying)
                {
                    GameObject.Destroy(bakedMeshesChildTransform.gameObject);
                }
                else
                {
                    GameObject.DestroyImmediate(bakedMeshesChildTransform.gameObject);
                }
            }
        }

        public void CollectStats(out FoliageRenderingManager.FoliageRenderingMode renderMode, out int drawCalls, out int instanceCount, out ulong vertexCount, out ulong triangleCount)
        {
            renderMode = FoliageRenderingManager.GetCurrentRenderingMode();

            drawCalls = 0;
            instanceCount = 0;
            vertexCount = 0;
            triangleCount = 0;

            for (var s = 0; s < Subgroups.Count; ++s)
            {
                var subgroup = Subgroups[s];
                var foliageData = subgroup.FoliageData;

                if (foliageData.LODs == null || foliageData.LODs.Length == 0)
                {
                    continue;
                }

                var lod0 = foliageData.LODs[0];
                if (lod0.RenderData == null || lod0.RenderData.Length == 0)
                {
                    continue;
                }

                var renderDataCount = lod0.RenderData.Length;

                if (renderMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstanced)
                {
                    var batchSize = FoliageRenderingManager.GetCurrentBatchSize();
                    var splitCount = (subgroup.TrsDatas.Length / batchSize) + 1;
                    drawCalls += splitCount * renderDataCount;
                }
                else if (renderMode == FoliageRenderingManager.FoliageRenderingMode.DrawMeshInstancedIndirect)
                {
                    drawCalls += renderDataCount;
                }

                var subgroupTrsCount = subgroup.TrsDatas.Length;
                instanceCount += subgroupTrsCount * renderDataCount;

                for (var r = 0; r < renderDataCount; ++r)
                {
                    var renderData = lod0.RenderData[r];
                    var renderMesh = renderData.mesh;
                    var meshVertexCount = renderMesh.vertexCount;
                    var meshTriangleCount = renderMesh.GetIndexCount(0);

                    var groupVertexCount = meshVertexCount * subgroupTrsCount;
                    var groupTriangleCount = meshTriangleCount * subgroupTrsCount;

                    vertexCount += (ulong)(uint)groupVertexCount;
                    triangleCount += (ulong)groupTriangleCount;
                }
            }
        }
    }
}