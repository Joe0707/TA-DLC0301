using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CorgiFoliagePainter;

namespace CorgiFoliagePainter.Editor
{
    public class FoliagePreviewGenerator
    {
        [System.NonSerialized]
        private Dictionary<int, Texture2D> _previewCache = new Dictionary<int, Texture2D>();

        public Texture2D GetPreviewTexture(FoliageGroup group, FoliageMeshData data)
        {
            var key = group.GetInstanceID() | data.name.GetHashCode();
            if(_previewCache.TryGetValue(key, out Texture2D texture))
            {
                return texture;
            }

            var hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            var renderTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
                renderTexture.hideFlags = hideFlags;

            var iconContentGo = new GameObject();
                iconContentGo.hideFlags = hideFlags;

            var contentTransform = iconContentGo.transform;
                contentTransform.position = new Vector3(1000, 1000, 1000);
                contentTransform.rotation = Quaternion.identity;
                contentTransform.localScale = Vector3.one;

            var lod0 = data.LODs[0];

            for (var r = 0; r < lod0.RenderData.Length; ++r)
            {
                var renderData = lod0.RenderData[r];

                var renderDataGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var renderDataTranform = renderDataGo.transform;
                    renderDataTranform.SetParent(contentTransform);
                    renderDataTranform.localPosition = renderData.localPosition;
                    renderDataTranform.localRotation = renderData.localRotation;
                    renderDataTranform.localScale = renderData.localScale;

                var contentMeshFilter = renderDataGo.GetComponent<MeshFilter>();
                var contentMeshRenderer = renderDataGo.GetComponent<MeshRenderer>();

                contentMeshFilter.sharedMesh = renderData.mesh;
                contentMeshRenderer.sharedMaterial = renderData.material;

                // note: uses the direct instanced rendering data 
                var propertyBlocks = group.GetSplitPropertyBlocks(0);
                if (propertyBlocks != null && propertyBlocks.Length > 0)
                {
                    contentMeshRenderer.SetPropertyBlock(propertyBlocks[0]);
                }
            }

            var cameraGo = new GameObject("_previewCamera");
                cameraGo.hideFlags = hideFlags;

            var cameraTransform = cameraGo.transform;
                cameraTransform.position = new Vector3(1000, 1000, 999);
                cameraTransform.rotation = Quaternion.identity;
                cameraTransform.localScale = Vector3.one;

            var camera = cameraGo.AddComponent<Camera>();
                camera.targetTexture = renderTexture;
                camera.clearFlags = CameraClearFlags.Color | CameraClearFlags.Depth;
                camera.backgroundColor = Color.clear;

            // move camera 
            var rotateY = 15f;
            var rotateX = 25f;

            var bounds = GetBoundsOfRenderers(iconContentGo);
            var size = bounds.size.magnitude * 0.5f;

            cameraTransform.position = bounds.center - new Vector3(0f, 0f, 1f);

            cameraTransform.RotateAround(bounds.center, Vector3.up, rotateY);
            cameraTransform.RotateAround(bounds.center, Vector3.right, rotateX);

            var cameraToBounds = (bounds.center - cameraTransform.position).normalized;
            cameraTransform.rotation = Quaternion.LookRotation(cameraToBounds, Vector3.up);
            cameraTransform.position = bounds.center - cameraToBounds * (size + 0.25f);

            // render
            camera.Render();
            
            var result = CreateFromRenderTexture(camera.targetTexture, TextureFormat.ARGB32);

            _previewCache.Add(key, result);

            GameObject.DestroyImmediate(iconContentGo);
            GameObject.DestroyImmediate(cameraGo);

            renderTexture.Release();

            return result; 
        }

        private Bounds GetBoundsOfRenderers(GameObject self)
        {
            var bounds = new Bounds();

            var visualRenderers = self.GetComponentsInChildren<Renderer>();
            for (var r = 0; r < visualRenderers.Length; ++r)
            {
                var visualRenderer = visualRenderers[r];
                var visualBounds = visualRenderer.bounds;

                if (r == 0)
                {
                    bounds = visualBounds;
                }
                else
                {
                    bounds.min = Vector3.Min(bounds.min, visualBounds.min);
                    bounds.min = Vector3.Min(bounds.min, visualBounds.max);

                    bounds.max = Vector3.Max(bounds.max, visualBounds.min);
                    bounds.max = Vector3.Max(bounds.max, visualBounds.max);
                }
            }

            return bounds;
        }

        private Texture2D CreateFromRenderTexture(RenderTexture renderTexture, TextureFormat format)
        {
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(renderTexture.width, renderTexture.height, format, false);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();

            // reset 
            RenderTexture.active = null;

            return texture;
        }
    }
}
