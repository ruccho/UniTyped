using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniTyped.Editor
{
    static class EditorUtils
    {
        [MenuItem("Assets/UniTyped/Apply Tags and Layers Reflection")]
        private static void ApplyTagManagerReflection()
        {
            AssetDatabase.SaveAssets();
            RecompileUniTypedAssembly();
        }

        private static void RecompileUniTypedAssembly()
        {
            AssetDatabase.ImportAsset("Packages/com.ruccho.unityped/Runtime/Scripts/ProjectAnchor.cs", ImportAssetOptions.ForceUpdate);
        }
    }
}
