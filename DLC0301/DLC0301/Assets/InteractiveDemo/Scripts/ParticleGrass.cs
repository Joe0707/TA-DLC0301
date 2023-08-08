using UnityEngine;

[ExecuteInEditMode]
public class ParticleGrass : MonoBehaviour
{
    new ParticleSystem particleSystem;

    public bool useRaycastFading = false;
    public LayerMask raycastMask;
    public float FadeStart = 0;
    public float FadeEnd = 1;

    Vector3 lastPosition;
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

            var direction = transform.position - lastPosition;
            var localDirection = transform.InverseTransformDirection(direction);
            var worldDirection = transform.TransformVector(localDirection);
            lastPosition = transform.position;

            var worldDirectionX = Mathf.Clamp01(worldDirection.x * 10 * 0.5f + 0.5f);
            var worldDirectionZ = Mathf.Clamp01(worldDirection.z * 10 * 0.5f + 0.5f);

            particleColor = new Color(worldDirectionX, worldDirectionZ, particleColor.b, particleColor.a);

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
