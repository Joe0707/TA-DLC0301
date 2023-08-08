using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CorgiFoliagePainter
{
    [System.Serializable]
    public struct FoliageTransformationData
    {
        public float4x4 trs; 
        public float4x4 inverseTrs; 
    }
}
