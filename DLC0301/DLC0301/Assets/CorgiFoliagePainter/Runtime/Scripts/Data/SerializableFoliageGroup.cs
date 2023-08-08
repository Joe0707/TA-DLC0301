using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiFoliagePainter
{
    [System.Serializable]
    public class SerializableFoliageGroup
    {
        public List<SerializableFoliageSubgroup> data = new List<SerializableFoliageSubgroup>();
    }
}
