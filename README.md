# UniTyped

[日本語版](README.ja.md)

UniTyped is a source generator for Unity that provides strongly typed access to serialized data through SerializedObject / SerializedProperty.　It helps you write more concise and safe editor extension code.

- **Statically Typed**: You don't have to write tons of `FindProperty` or touch the confusing SerializedProperty APIs.
- **Less Heap Allocations**:  Generated code is struct-based and designed to avoid boxing; It is suitable for repeated invocations from OnInspectorGUI() and other editor codes, making your editor experience better.

## Requirements
 - Fully supported in Unity 2021.2 and later
 - In Unity 2021.1 and below, UniTyped features can be used with [Manual Generator](#manual-generator)

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

## Supported Types

See also: https://docs.unity3d.com/Manual/script-Serialization.html

 - Primitive C# types (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `string`, `char`)
 - Built-in Unity Types (`AnimationCurve`, `BoundsInt`, `Bounds`, `Color`, `Hash128`, `Quaternion`, `RectInt`, `Rect`, `Vector2Int`, `Vector2`, `Vector3Int`, `Vector3`, `Vector4`)
 - Enum types (32 bites or smaller)
 - References to objects that derive from `UnityEngine.Object`
 - Custom classes / structs with `[Serializable]`
 - Array / List<T> with serializable element type
 - Fixed-size buffers
 - `[SerializeReference]` fields

```csharp

using System;
using System.Collections.Generic;
using UnityEngine;
using UniTyped;

[UniTyped]
public class Example : MonoBehaviour
{
    // primitive C# types

    [SerializeField] private byte someByte = default;
    [SerializeField] private sbyte someSbyte = default;
    [SerializeField] private short someShort = default;
    [SerializeField] private ushort someUshort = default;
    [SerializeField] private int someInt = default;
    [SerializeField] private uint someUint = default;
    [SerializeField] private long someLong = default;
    [SerializeField] private ulong someUlong = default;
    [SerializeField] private float someFloat = default;
    [SerializeField] private double someDouble = default;
    [SerializeField] private bool someBool = default;
    [SerializeField] private string someString = default;
    [SerializeField] private char someChar = default;

    // built-in unity types

    [SerializeField] private AnimationCurve someAnimationCurve = default;
    [SerializeField] private BoundsInt someBoundsInt = default;
    [SerializeField] private Bounds someBounds = default;
    [SerializeField] private Color someColor = default;
    [SerializeField] private Hash128 someHash128 = default;
    [SerializeField] private Quaternion someQuaternion = default;
    [SerializeField] private RectInt someRectInt = default;
    [SerializeField] private Rect someRect = default;
    [SerializeField] private Vector2Int someVector2Int = default;
    [SerializeField] private Vector2 someVector2 = default;
    [SerializeField] private Vector3Int someVector3Int = default;
    [SerializeField] private Vector3 someVector3 = default;
    [SerializeField] private Vector4 someVector4 = default;

    // enum

    [SerializeField] private SomeEnum someEnum = default;
    [SerializeField] private SomeEnumSmall someEnumSmall = default;

    public enum SomeEnum
    {
        Option0,
        Option1
    }

    public enum SomeEnumSmall : byte
    {
        Option0,
        Option1
    }

    // UnityEngine.Object

    [SerializeField] private UnityEngine.Object someObject = default;
    [SerializeField] private Texture2D someTexture = default;

    // custom serializable

    [SerializeField] private SerializableTest someSerializable = default;

    [Serializable]
    public class SerializableTest
    {
        [SerializeField] private int value;
    }

    // SerializeReference

    [SerializeReference] private object someManagedReference = default;

    // array / list

    [SerializeField] private int[] someArray = default;
    [SerializeField] private List<int> someList = default;

    // fixed buffer

    [SerializeField] private FixedBufferContainer fixedBufferContainer = default;

    [Serializable]
    public unsafe struct FixedBufferContainer
    {
        [SerializeField] private fixed char fixedBuffer[30];
    }
}

```

### Array / List operation

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UniTyped;

[UniTyped]
public class Example : MonoBehaviour
{
    [SerializeField] private int[] someArray = default;
    [SerializeField] private List<int> someList = default;
}


#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(Example))]
public class ExampleEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var view = new UniTyped.Generated.ExampleView()
        {
            Target = serializedObject
        };

        // array access
        for (int i = 0; i < view.someArray.Length; i++)
        {
            Debug.Log(view.someArray[i].Value);
        }

        //also accessible with IEnumerator<T>
        foreach (var element in view.someArray)
        {
            Debug.Log(element.Value);
        }

        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
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


## Manual Generator
By default, UniType uses the functionality of the Roslyn source generator available in Unity 2021.2 and later. In Unity 2021.1 and below, you can use UniType with manual generator provided as individual package.

### Requirements

 - .NET runtime (supports `netcoreapp3.1` target)
    - ensure `dotnet` cli tool is available
 - MSBuild (included in Visual Studio or .NET SDK)

### Installation

Add git URL `https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped.manualgenerator` from Package Manager.

### Usage

1. Create generator profile asset from `Create` > `UniTyped` > `Manual Generator Profile` in project view.
2. Add generator item.
3. Register `csproj` path of the project contains target scripts.
4. Register output C# path. (will be overwritten!)
5. Click `Generate` button.

![image](https://user-images.githubusercontent.com/16096562/220120237-1fb1afa2-cd56-4b4f-80c6-aa1b3269a24e.png)

### Hint
 - Executable of manual generator `UniTyped.Generator.Standalone.exe` can be found in `Packages/com.ruccho.unityped.manualgenerator/Editor/~Executable/netcoreapp3.1`. You can use it from commandline with options `--ptojectPath=<CSPROJ PATH> --output=<OUTPUT SCRIPT PATH>`.