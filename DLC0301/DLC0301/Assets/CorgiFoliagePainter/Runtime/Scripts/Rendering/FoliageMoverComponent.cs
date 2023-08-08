using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorgiFoliagePainter
{
    [ExecuteAlways]
    public class FoliageMoverComponent : MonoBehaviour
    {
        public float Radius = 1.0f;

        private void OnEnable()
        {
            FoliageRenderingManager.RegisterMover(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            FoliageRenderingManager.UnregisterMover(this);
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
            FoliageRenderingManager.UnregisterMover(this);
            FoliageRenderingManager.RegisterMover(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Radius = Mathf.Max(Radius, 0.0001f);
        }
#endif
    }
}
