using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;


[ExecuteAlways][RequireComponent(typeof(Camera))]
public class OrthoRTCameraHelper : MonoBehaviour
{
    [DisplayOnly]
    public Camera RTCamera;
    public RenderTexture RT;
    [SerializeField]
    LayerMask layerMask;
    public bool isFollowCam = false;
    public Camera MainCamera;
    [SerializeField]
    public float followDrawDistance = 100.0f;
    [Header("Global Materials Params")]
    [SerializeField]
    public string CamSizeParams = "_FXCamSize";
    [SerializeField]
    public string CamPosParams = "_FXCamPos";
    [SerializeField]
    public string RTNameParams = "_MotionRT";

    [HideInInspector]
    [SerializeField]
    Vector3 centerPos = Vector3.zero;
    [HideInInspector]
    [SerializeField]
    float orthographicSize = 0.0f;

#if UNITY_EDITOR
    [Header("Debug")]
    //[SerializeField]
    private bool _ShowRegion = false;
#endif
    void OnEnable()
    {
        RTCamera = GetComponent<Camera>();
        if (RTCamera.targetTexture != null)
            RT = RTCamera.targetTexture;
    }
    private void OnDisable()
    {

    }
    void Update()
    {
        InitOrthoCamera();
        FollowMainCamera();
        SetMaterialsParams();
    }
    void InitOrthoCamera()
    {
        if (RTCamera == null)
        {
            RTCamera = GetComponentInChildren<Camera>();
        }
        if (RT != null)
        {
            RTCamera.targetTexture = RT;
        }
        RTCamera.transform.eulerAngles = Vector3.right * 90.0f;
        RTCamera.orthographic = true;
        RTCamera.depth = -100;
        RTCamera.aspect = 1.0f;
        RTCamera.cullingMask = layerMask;
        RTCamera.clearFlags = CameraClearFlags.SolidColor;
        //RTCamera.backgroundColor = Color.black;
        var cameraData = RTCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.renderShadows = false;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
    } 
    void FollowMainCamera()
    {
        if (MainCamera != null && isFollowCam)
        {
            Vector3[] frustumCorners = new Vector3[5];
            MainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), followDrawDistance, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            frustumCorners[0] = MainCamera.transform.position + MainCamera.transform.TransformVector(frustumCorners[0]);
            frustumCorners[1] = MainCamera.transform.position + MainCamera.transform.TransformVector(frustumCorners[1]);
            frustumCorners[2] = MainCamera.transform.position + MainCamera.transform.TransformVector(frustumCorners[2]);
            frustumCorners[3] = MainCamera.transform.position + MainCamera.transform.TransformVector(frustumCorners[3]);
            frustumCorners[4] = MainCamera.transform.position;

#if UNITY_EDITOR
            if (_ShowRegion)
            {
                for (int i = 0; i < 4; i++)
                {
                    Debug.DrawRay(frustumCorners[4], frustumCorners[i] - frustumCorners[4], Color.blue);
                }
            }
#endif

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
            //Debug.DrawRay(frustumCorners[4], centerPos - frustumCorners[4], Color.red);

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
            orthographicSize = maxDistance / 2.0f;
        }
        if (isFollowCam)
        {
            RTCamera.orthographicSize = orthographicSize;
            //RTCamera.transform.parent = null;
            var dir = Vector3.Normalize(RTCamera.transform.position - centerPos);
            RTCamera.transform.position = centerPos;
        }
    }

    void SetMaterialsParams()
    {
        Shader.SetGlobalFloat(CamSizeParams, RTCamera.orthographicSize);
        Shader.SetGlobalVector(CamPosParams, RTCamera.transform.position);
        if(RT !=null )
        Shader.SetGlobalTexture(RTNameParams, RT);
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR

        if (_ShowRegion == false)
        {
            return;
        }
        var pos = RTCamera.transform.position;
        pos.y -= RTCamera.farClipPlane * 0.5f;

        var size = new Vector3(orthographicSize * 2.0f, RTCamera.farClipPlane, orthographicSize * 2.0f);
        Gizmos.color = new Color(0, 0.5f, 1, 0.5f);
        Gizmos.DrawCube(pos, size);
        Gizmos.color = new Color(0, 0.5f, 1, 0.9f);
        Gizmos.DrawWireCube(pos, size);
#endif
    }
}
#if UNITY_EDITOR
public class DisplayOnly : PropertyAttribute
{

}
[CustomPropertyDrawer(typeof(DisplayOnly))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif