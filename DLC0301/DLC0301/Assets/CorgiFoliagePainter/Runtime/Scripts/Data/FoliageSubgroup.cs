using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorgiFoliagePainter
{
    // foliage group data  
    [System.Serializable]
    public class FoliageSubgroup
    {
        [Tooltip("The foliage datas used for drawing.")] public FoliageMeshData FoliageData;
        [Tooltip("Contains information used internally for the foliage system.")] public FoliageTransformationData[] TrsDatas = new FoliageTransformationData[0];
        [Tooltip("Contains information used internally for the foliage system.")] public FoliageMetadata[] Metadatas = new FoliageMetadata[0];
        [Tooltip("Contains information used internally for the foliage system.")] public List<BakedMeshLod> BakedMeshLods = new List<BakedMeshLod>();

        [System.Serializable]
        public class BakedMeshLod
        {
            public List<BakedMeshRenderData> BakedRenderDatas; // [render data index, split mesh index]
        }

        [System.Serializable]
        public class BakedMeshRenderData
        {
            public List<Mesh> meshes;
            public Material material; 
        }

        [System.NonSerialized] public MaterialPropertyBlock[] _propertyBlocks;
        [System.NonSerialized] public GraphicsBuffer[] _directBuffers;
        [System.NonSerialized] public Texture2D[] _directTextureFallbacks;
        [System.NonSerialized] public MaterialPropertyBlock[][] _indirectPropertyBlock; // [lod index, renderdata index]
        [System.NonSerialized] public GraphicsBuffer[][] _indirectTrsBuffer; // [lod index, renderdata index]
        [System.NonSerialized] public GraphicsBuffer _indirectMetadataBuffer;
        [System.NonSerialized] public GraphicsBuffer[][] _indirectArgsBuffers; // [lod_index, renderData_index]
        [System.NonSerialized] public NativeArray<Matrix4x4>[][][] _trsCacheNative; // [split index, lod index, renderData index, foliage index]
        [System.NonSerialized] public Matrix4x4[][][][] _trsCacheFallback; // [split index, lod index, renderData index, foliage index]
        [System.NonSerialized] public int _lodIndexCache;
        [System.NonSerialized] public Bounds _bounds;

        // light/occlusion probe data cache 
        [System.NonSerialized] public Vector3[] _lightProbePositionData;
        [System.NonSerialized] public SphericalHarmonicsL2[] _lightProbeSHData;
        [System.NonSerialized] public SphericalHarmonicsL2[][] _lightProbeShDataSplit;
        [System.NonSerialized] public Vector4[] _occlusionProbeSHData;
        [System.NonSerialized] public Vector4[][] _occlusionProbeSHDataSplit;

        /// <summary>
        /// If you remove a subgroup, be sure to Dispose() it first.
        /// </summary>
        public void Dispose()
        {
            if (_indirectTrsBuffer != null)
            {
                for(var l = 0; l < _indirectTrsBuffer.Length; ++l)
                {
                    var lodBuffers = _indirectTrsBuffer[l];
                    if(lodBuffers != null)
                    {
                        for (var r = 0; r < lodBuffers.Length; ++r)
                        {
                            var renderDataBuffer = lodBuffers[r];

                            if(renderDataBuffer != null)
                            {
                                renderDataBuffer.Dispose();
                            }

                            lodBuffers[r] = null; 
                        }
                    }

                    _indirectTrsBuffer[l] = null;
                }
            }

            if (_indirectMetadataBuffer != null)
            {
                _indirectMetadataBuffer.Dispose();
                _indirectMetadataBuffer = null;
            }

            if (_indirectArgsBuffers != null)
            {
                for(var l = 0; l < _indirectArgsBuffers.Length; ++l)
                {
                    var indirectLodBuffers = _indirectArgsBuffers[l];
                    if(indirectLodBuffers != null)
                    {
                        for(var r = 0; r < indirectLodBuffers.Length; ++r)
                        {
                            var indirectRenderBuffer = indirectLodBuffers[r];
                            if(indirectRenderBuffer != null)
                            {
                                indirectRenderBuffer.Dispose(); 
                            }
                        }
                    }

                    _indirectArgsBuffers[l] = null;
                }

                _indirectArgsBuffers = null;
            }

            if (_directBuffers != null)
            {
                for (var i = 0; i < _directBuffers.Length; ++i)
                {
                    var buffer = _directBuffers[i];

                    if (buffer != null)
                    {
                        buffer.Dispose();
                    }

                    _directBuffers[i] = null;
                }
            }

            if(_directTextureFallbacks != null)
            {
                for (var i = 0; i < _directTextureFallbacks.Length; ++i)
                {
                    var fallbackTexture = _directTextureFallbacks[i];

                    if (fallbackTexture != null)
                    {
#if UNITY_EDITOR
                        if(Application.isPlaying)
                        {
                            Texture2D.Destroy(fallbackTexture);
                        }
                        else
                        {
                            Texture2D.DestroyImmediate(fallbackTexture);
                        }
#else
                        Texture2D.Destroy(fallbackTexture);
#endif
                    }

                    _directTextureFallbacks[i] = null;
                }
            }

            if(_indirectPropertyBlock != null)
            {
                for(var i = 0; i < _indirectPropertyBlock.Length; ++i)
                {
                    var propertyBlocks = _indirectPropertyBlock[i];

                    for (var j = 0; j < propertyBlocks.Length; ++j)
                    {
                        propertyBlocks[j] = null;
                    }

                    _indirectPropertyBlock[i] = null;
                }

                _indirectPropertyBlock = null;
            }

            if (_trsCacheNative != null)
            {
                for (var ti = 0; ti < _trsCacheNative.Length; ++ti)
                {
                    var splitGroups = _trsCacheNative[ti];
                    for (var si = 0; si < splitGroups.Length; ++si)
                    {
                        var splitLodGroups = splitGroups[si];
                        for (var li = 0; li < splitLodGroups.Length; ++li)
                        {
                            var splitLodGroup = splitLodGroups[li];
                            if (splitLodGroup.IsCreated)
                            {
                                splitLodGroup.Dispose();
                            }
                        }
                    }
                }

                _trsCacheNative = null; 
            }
        }
    }
}
