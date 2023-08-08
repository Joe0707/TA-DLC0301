using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CorgiFoliagePainter
{

    [CreateAssetMenu(fileName = "NewFoliageMeshData", menuName = "Corgi Foliage/New Foliage Mesh Data")]
    public class FoliageMeshData : ScriptableObject
    {
        [System.Serializable]
        public struct FoliageMeshLOD
        {
            public const int CullIndex = 999;

            public float distance;
            public FoliageMeshLODRenderData[] RenderData;

            public Bounds GetLodBounds()
            {
                var bounds = new Bounds(Vector3.zero, Vector3.one);
                var firstBoundsSet = false;

                for (var r = 0; r < RenderData.Length; ++r)
                {
                    var renderData = RenderData[r];
                    var renderMesh = renderData.mesh;

                    if (renderMesh == null)
                    {
                        continue;
                    }

                    if (firstBoundsSet)
                    {
                        var min = renderMesh.bounds.min;
                        var max = renderMesh.bounds.max;

                        bounds.min = Vector3.Min(min, bounds.min);
                        bounds.max = Vector3.Max(max, bounds.max);
                        bounds.min = Vector3.Min(min, bounds.max);
                        bounds.max = Vector3.Max(max, bounds.min);
                    }
                    else
                    {
                        firstBoundsSet = true;
                        bounds = renderMesh.bounds;
                    }
                }

                return bounds;
            }
        }

        [System.Serializable]
        public struct FoliageMeshLODRenderData
        {
            // render data
            [Tooltip("The mesh used for drawing.")] public Mesh mesh;
            [Tooltip("The material used for drawing the instanced meshes. Note: Instancing must be enabled on the Material.")] public Material material;

            // local offsets 
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        [Header("Meshes")]
        [Tooltip("The mesh LODs used for drawing.")] public FoliageMeshLOD[] LODs = new FoliageMeshLOD[0];

        [Header("Default Editor Settings")]
        public Gradient DefaultRandomColors;
        public Vector3 DefaultBaseScale = new Vector3(1f, 1f, 1f);
        public Vector3 DefaultBaseRotationEulor;
        public Vector3 DefaultRandomScale = new Vector3(0.25f, 0.25f, 0.25f);
        public Vector3 DefaultRandomRotationEulor = new Vector3(0f, 360f, 0f);

        public Bounds GetOverallBounds()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            var firstBoundsSet = false;

            for (var l = 0; l < LODs.Length && l < 1; ++l)
            {
                var LOD = LODs[l];

                for (var r = 0; r < LOD.RenderData.Length; ++r)
                {
                    var renderData = LOD.RenderData[r];
                    var renderMesh = renderData.mesh;

                    if (renderMesh == null)
                    {
                        continue;
                    }

                    if (firstBoundsSet)
                    {
                        var min = renderMesh.bounds.min;
                        var max = renderMesh.bounds.max;

                        bounds.min = Vector3.Min(min, bounds.min);
                        bounds.max = Vector3.Max(max, bounds.max);
                        bounds.min = Vector3.Min(min, bounds.max);
                        bounds.max = Vector3.Max(max, bounds.min);
                    }
                    else
                    {
                        firstBoundsSet = true;
                        bounds = renderMesh.bounds;
                    }
                }
            }

            return bounds;
        }

        [System.Serializable]
        public class PrefabPlacerSettings
        {
            public GameObject prefab;
            public Vector3 localPosition = Vector3.zero;
            public Quaternion localRotation = Quaternion.identity;
            public Vector3 localScale = Vector3.one;
        }

        // extra tools settings 
        public PrefabPlacerSettings ExtraToolsPrefabPlacerSettings;
    }
}
