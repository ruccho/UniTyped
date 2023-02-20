using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniTyped.Generator.Manual
{
    [CreateAssetMenu(menuName = "UniTyped/Manual Generator Profile", fileName = "UniTypedGeneratorProfile")]
    public class UniTypedManualGeneratorProfile : ScriptableObject
    {
        public List<GenerationItem> items = new List<GenerationItem>();
        
        
    }

    [Serializable]
    public class GenerationItem
    {
        [SerializeField] public string projectPath = default;
        [SerializeField] public string outputFile = default;
    }
}