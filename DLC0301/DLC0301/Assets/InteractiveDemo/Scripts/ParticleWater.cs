using UnityEngine;

[ExecuteInEditMode]
public class ParticleWater : MonoBehaviour
{
    new ParticleSystem particleSystem;

    public bool useRaycastFading = true;
    public LayerMask raycastMask;
    public float FadeStart = 0;
    public float FadeEnd = 1;

    void OnEnable()
    {
        particleSystem = gameObject.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (particleSystem != null)
        {
            var particleModule = particleSystem.main;
            var particleColor = particleModule.startColor.color;
            if (useRaycastFading)
            {
                var fade = GetRacastFading();
                particleColor = new Color(particleColor.r, particleColor.g, particleColor.b, fade);
            }
            else
            {
                particleColor = new Color(particleColor.r, particleColor.g, particleColor.b, particleColor.a);
            }

            particleModule.startColor = particleColor;
        }
    }

    float GetRacastFading()
    {

        RaycastHit hit;
        bool raycastHit = Physics.Raycast(transform.position, -Vector3.up, out hit, Mathf.Infinity, raycastMask);
        if (hit.distance > 0)
        {
            float fadeAlpha = Mathf.Clamp01((FadeEnd - hit.distance) / (FadeEnd - FadeStart));
            return fadeAlpha;
        }
        else
        {
            return 1;
        }
    }
}
