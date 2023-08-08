using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainMapRenderer : MonoBehaviour
{

    public Camera camToDrawWith;
    public Camera followCam;
    public bool isFollowCam = true;
    [SerializeField]
    LayerMask layer;
    // objects to render
    [SerializeField]
    public float drawDistance = 100.0f;
    public int resolution = 512;
    RenderTexture tempTex;

    [HideInInspector][SerializeField]
    Vector3 centerPos = Vector3.zero;
    [HideInInspector][SerializeField]
    float size = 0.0f;

    [SerializeField]
    private bool _ShowDebug = false;
    void OnEnable()
    {
        tempTex = new RenderTexture(resolution, resolution, 24);
    }
    private void OnDisable()
    {
        tempTex.Release();
    }
    void Update()
    {
        SetUpCam();
        DrawDiffuseMap();
    }

    public void DrawDiffuseMap()
    {
        DrawToMap("_TerrainDiffuse");
    }

    void DrawToMap(string target)
    {
        camToDrawWith.enabled = true;
        camToDrawWith.targetTexture = tempTex;
        camToDrawWith.depthTextureMode = DepthTextureMode.Depth;
        Shader.SetGlobalFloat("_OrthographicCamSize", camToDrawWith.orthographicSize);
        Shader.SetGlobalVector("_OrthographicCamPos", camToDrawWith.transform.position);
        camToDrawWith.Render();
        Shader.SetGlobalTexture(target, tempTex);
        camToDrawWith.enabled = false;
    }

    void SetUpCam()
    {
        if (followCam != null && isFollowCam)
        {
            Vector3[] frustumCorners = new Vector3[5];
            followCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), drawDistance, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            frustumCorners[0] = followCam.transform.position + followCam.transform.TransformVector(frustumCorners[0]);
            frustumCorners[1] = followCam.transform.position + followCam.transform.TransformVector(frustumCorners[1]);
            frustumCorners[2] = followCam.transform.position + followCam.transform.TransformVector(frustumCorners[2]);
            frustumCorners[3] = followCam.transform.position + followCam.transform.TransformVector(frustumCorners[3]);
            frustumCorners[4] = followCam.transform.position;

            if (_ShowDebug)
            {
                for (int i = 0; i < 4; i++)
                {
                    Debug.DrawRay(frustumCorners[4], frustumCorners[i] - frustumCorners[4], Color.blue);
                }
            }

            var totalX = 0f;
            var totalZ = 0f;
            var MaxY = 0.0f;
            foreach (var target in frustumCorners)
            {
                totalX += target.x;
                totalZ += target.z;
                if (target.y > MaxY)
                    MaxY = target.y;
            }
            var centerX = totalX / (frustumCorners.Length);
            var centerZ = totalZ / (frustumCorners.Length);

            centerPos = new Vector3(centerX, MaxY, centerZ);
            if(_ShowDebug)
            Debug.DrawRay(frustumCorners[4], centerPos - frustumCorners[4], Color.red);

            float maxDistance = 0.0f;
            foreach (var targetX in frustumCorners)
            {
                foreach (var targetY in frustumCorners)
                {
                    float distance = Vector2.Distance(new Vector2(targetX.x, targetX.z), new Vector2(targetY.x, targetY.z));
                    if (distance > maxDistance)
                        maxDistance = distance;
                }
            }
            size = maxDistance / 2.0f;
        }
        if (camToDrawWith == null)
        {
            camToDrawWith = GetComponentInChildren<Camera>();
        }
        if (isFollowCam)
        {
            camToDrawWith.cullingMask = layer;
            camToDrawWith.orthographicSize = size;
            camToDrawWith.transform.parent = null;
            var dir = Vector3.Normalize(camToDrawWith.transform.position - centerPos);
            camToDrawWith.transform.position = centerPos;
            camToDrawWith.transform.parent = gameObject.transform;
        }
    }

}