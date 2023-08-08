namespace CorgiFoliagePainter
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class FoliageCameraHelper : MonoBehaviour
    {
        [System.NonSerialized] private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            FoliageRenderingManager.RegisterCamera(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            FoliageRenderingManager.UnregisterCamera(this);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(DoDeferredReRegisterCamera()); 
        }

        private IEnumerator DoDeferredReRegisterCamera()
        {
            // when a new scene is loaded, we're probably catching it as a result of the camera persisting between scenes
            // if that is the case, wait a frame for the next scene's FoliageManager to register it's static Instance
            // and then, register ourselves to it
            yield return null;

            // just in case, unregister (harmless) before registering
            FoliageRenderingManager.UnregisterCamera(this);
            FoliageRenderingManager.RegisterCamera(this);
        }

        public Camera GetCamera()
        {
            return _camera;
        }
    }
}
