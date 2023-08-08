using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiFoliagePainter
{
    // foliage group data  
    [System.Serializable]
    public class SerializableFoliageSubgroup
    {
        public string FoliageMeshDataName;
        public List<FoliageTransformationData> TrsDatas = new List<FoliageTransformationData>();
        public List<FoliageMetadata> Metadatas = new List<FoliageMetadata>();
    }
}
