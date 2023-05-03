# UniTyped

[日本語版](README.ja.md)

UniTyped is a source generator for Unity that provides strongly typed access to serialized data through SerializedObject / SerializedProperty / Material.　It helps you write more concise and safe editor extension code.

- **Statically Typed**: You don't have to write tons of `FindProperty` or touch the confusing SerializedProperty APIs.
- **Less Heap Allocations**:  Generated code is struct-based and designed to avoid boxing; It is suitable for repeated invocations from OnInspectorGUI() and other editor codes, making your editor experience better.


## Requirements
 - Fully supported in Unity 2021.2 and later
 - In Unity 2021.1 and below, UniTyped features can be used with [Manual Generator](#manual-generator)

## Installation
Add git URL `https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped` from UPM.

## Typed Views for Serialized Objects
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

### Configure Code Generation
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

## Typed Views for Material

Create `partial` struct with `UniTyped.UniTypedMaterialView` attribute.

```csharp
using UnityEngine;
using UniTyped;

//specify relative path from this source file
[UniTypedMaterialView("NewUnlitShader.shader")]
public partial struct NewUnlitShaderView
{
}

//ShaderGraph is also supported.
/*
[UniTypedMaterialView("New Shader Graph.shadergraph")]
public partial struct NewShaderGraphView
{
}
*/

public class MaterialViewExample : MonoBehaviour
{
    [SerializeField] private Material mat = default;

    void Update()
    {
        var view = new NewUnlitShaderView()
        {
            Target = mat
        };

        view._Color = Color.HSVToRGB(Time.time % 1f, 1f, 1f);

    }
}

```

```shaderlab
Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    //...
}
```

## Typed Views for Animator Parameters

Create `partial` struct with `UniTyped.UniTypedAnimatorView` attribute.
Ensure that the latest aniamtor controller assets are saved to disk (with `Save Project`) .

![image](https://user-images.githubusercontent.com/16096562/235991706-f8db8e2f-36c9-4ff7-9e67-73c03645f206.png)

```csharp
using UnityEngine;
using UniTyped;

[UniTypedAnimatorView("New Animator Controller.controller")]
public partial struct NewAnimatorControllerView
{
    
}

public class AnimatorViewExample : MonoBehaviour
{

    private Animator animator;
    
    void Update()
    {
        if (!animator && !TryGetComponent(out animator)) return;
        
        var view = new NewAnimatorControllerView()
        {
            Target = animator
        };

        // Float
        view.FloatParameter = Time.time;
        view.SetFloatParameter(Time.time, dampTime, deltaTime);
        
        // Int
        view.IntParameter = Time.frameCount;
        
        // Bool
        view.BoolParameter = true;
        
        // Trigger
        view.TriggerA();
        view.ResetTriggerA();
    }
}
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
 - Executable of manual generator `UniTyped.Generator.Standalone.exe` can be found in `Packages/com.ruccho.unityped.manualgenerator/Editor/Executable~/netcoreapp3.1`. You can use it from commandline with options `--project=<CSPROJ PATH> --output=<OUTPUT SCRIPT PATH>`.