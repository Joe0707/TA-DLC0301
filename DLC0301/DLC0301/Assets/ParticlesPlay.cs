using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticlesPlay : MonoBehaviour
{
    public float delay = 3.0f;
    float mtime = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        mtime += Time.deltaTime;
        if (mtime > delay)
        {
            mtime = 0.0f;
            var partcielsystem = this.gameObject.GetComponent<ParticleSystem>();
            if (partcielsystem != null)
            {
                partcielsystem.Play(true);
            }
        }
    }
}
