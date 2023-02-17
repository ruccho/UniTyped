# UniTyped

[日本語版](README.ja.md)

UniTyped is a source generator for Unity that provides strongly typed access to serialized data through SerializedObject / SerializedProperty.　It helps you write more concise and safe editor extension code.

- **Statically Typed**: You don't have to write tons of `FindProperty` or touch the confusing SerializedProperty APIs.
- **Less Heap Allocations**:  Generated code is struct-based and designed to avoid boxing; It is suitable for repeated invocations from OnInspectorGUI() and other editor codes, making your editor experience better.

## Requirements
 - Unity 2021.3 or newer

## Installation
Add git URL `https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped` from UPM.

## Usage
Put `[UniTyped.UniTyped]` attribute on your MonoBehaviour, ScriptableObject or your custom serializable class / struct then you can use `UniTyped.Generated.[YourNamespace].[YourClass]View` struct in your editor code.

```csharp
using UnityEngine;
using UniTyped;
using UniTyped.Generated;

[UniTyped]
public class Example : MonoBehaviour
{
    [SerializeField] private int someValue = 0;
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(Example))]
public class ExampleEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        //ExampleView is auto-generated struct.
        var view = new ExampleView()
        {
            Target = serializedObject
        };
        
        //equivalent to serializedObject.FindProperty("someValue").intValue++;
        view.someValue++;

        serializedObject.ApplyModifiedProperties();
        
    }
}

#endif
```

## Configure Code Generation
Use `[UniTypedField]` attribute to control the result of code generation.

 - `ignore`: ignores the field.
 - `nestedField`: By default, UniTyped flattens accessor property to allow direct manipulation of values, but this option avoids that and exposes internal view. It is useful to access `SerializedProperty` of specific field.

```csharp
using UnityEngine;
using UniTyped;
using UniTyped.Generated;


[UniTyped]
public class Example : MonoBehaviour
{
    [SerializeField, UniTypedField(ignore = true)]
    private int ignoredField = 0;
    
    [SerializeField, UniTypedField(forceNested = true)]
    private int nestedField = 0;
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(Example))]
public class ExampleEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var view = new ExampleView()
        {
            Target = serializedObject
        };

        
        view.ignoredField++; // error: Cannot resolve symbol 'ignoreField'

        Debug.Log(view.nestedField.Value); // int Value { get; set; }
        Debug.Log(view.nestedField.Property); // SerializedProperty Property { get; set; }
        

        serializedObject.ApplyModifiedProperties();
        
    }
}
#endif
```



## Limitations
 - This project is currently experimental and breaking changes may be made.
 - It may not cover all use cases. Please let me know by issues if you notice anything.

