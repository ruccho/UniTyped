# UniTyped

[English](README.md)

UniTypedは、SerializedObjectやマテリアルパラメータ、タグやレイヤーなどのデータに対し、型付けされたアクセスを可能にするソースジェネレータです。より簡潔で安全なコードを書くのに役立ちます。

- **静的型付き**: 文字列ベースの不安定なデータアクセスを排除し、存在しないデータへのアクセスをコンパイルの段階で発見できるようになります。大量の `FindProperty` を書いたり、SerializedPropertyのややこしいAPIを触る必要がなくなります。
- **低ヒープメモリ確保**:  生成コードは構造体ベースで、boxingを避けるように設計されています。

# Table of Contents

 - [要件](#要件)
 - [インストール](#インストール)
 - [制限事項](#制限事項)
 - [機能](#機能)
    - [型付きビューの自動生成](#型付きビューの自動生成)
        - [シリアライズされたオブジェクト](#シリアライズされたオブジェクト)
        - [マテリアル](#マテリアル)
        - [Animatorパラメータ](#Animatorパラメータ)
    - [タグとレイヤーのEnumの自動生成](#タグとレイヤーのEnumの自動生成)
 - [Manual Generator](#manual-generator)

# 要件
 - Unity 2021.2 以上で完全にサポートされます。
 - Unity 2021.1 以下では[Manual Generator](#manual-generator)を使用することでUniTypedの機能を利用できます。

# インストール
Package Manager から次の git URL を追加してください：`https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped`

# 制限事項
 - このプロジェクトは現在experimentalで、破壊的変更が加えられる可能性があります。
 - すべてのユースケースがカバーできていないかもしれません。お気づきの点があればissueでお知らせください。

# 機能

## 型付きビューの自動生成

### シリアライズされたオブジェクト
`[UniTyped.UniTyped]`属性をMonoBehaviourやScriptableObject、またはその他のSerializableなカスタムクラスに適用してください。すると、`UniTyped.Generated.[YourNamespace].[YourClass]View` という構造体が使用できるようになります。

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
        //ExampleViewは自動生成されたstruct
        var view = new ExampleView()
        {
            Target = serializedObject
        };
        
        //serializedObject.FindProperty("someValue").intValue++;と同等
        view.someValue++;

        serializedObject.ApplyModifiedProperties();
        
    }
}

#endif
```

#### 配列・リストの操作

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


#### コード生成を調整する
`[UniTypedField]`属性のオプションを使用して、コード生成の内容を調整できます。

 - `ignore`: そのフィールドをコード生成の対象から外します。
 - `nestedField`: デフォルトでUniTypedは値の直接操作を可能にするために、アクセサプロパティをフラット化します。このオプションはそれを無効化し、内部のViewを公開します。特定の `SerializedProperty` にアクセスしたい場合などに便利です。

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

### マテリアル

`UniTyped.UniTypedMaterialView` 属性つきの `partial` なstructを作成します。

```csharp
using UnityEngine;
using UniTyped;

//このスクリプトからの相対パスを指定
[UniTypedMaterialView("NewUnlitShader.shader")]
public partial struct NewUnlitShaderView
{
    
}

//ShaderGraphにも対応しています。
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

### Animatorパラメータ

`UniTyped.UniTypedAnimatorView` 属性つきの `partial` なstructを作成します。
最新のAnimator Controllerアセットが`Save Project`でディスクに保存されていることを確認してください。

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

## タグとレイヤーのEnumの自動生成

`UniTyped.Reflection` 名前空間に `Tags`, `Layers`, `SortingLayers` の enum が自動生成されます。

これらのメンバーは`UniTyped`アセンブリ内に生成されるため、Project Settingsからタグやレイヤーに変更を加えた後、`UniTyped`アセンブリをリコンパイルする必要があります。これはメニューバーの `Assets` > `UniTyped` > `Apply Tags and Layers Reflection` オプションで行えます。

```csharp
using System.Collections.ObjectModel;
using UnityEngine;
using UniTyped.Reflection;

public class TagsAndLayersExample : MonoBehaviour
{
    private void Start()
    {
        // ---Tags---
        
        Debug.Log(Tags.New_tag);
        
        // タグ名は UniTyped.Reflection.TagUtility を使用して操作できます。
        Debug.Log(TagUtility.GetTagName(Tags.New_tag)); // "New tag"
        TagUtility.TryGetTagValue("New tag", out Tags result); // result: Tags.New_tag
        ReadOnlyCollection<string> tagNames = TagUtility.TagNames; // タグ名を列挙
        
        
        // ---Layers---
        
        Debug.Log(Layers.Default);
        Debug.Log(Layers.UI);
        Debug.Log(Layers.Water);
        Debug.Log(Layers.Ignore_Raycast);
        Debug.Log(Layers.TransparentFX);
        
        // Layers列挙体の値はそのままレイヤーのインデックスに変換できます。
        Debug.Log(LayerMask.LayerToName((int)Layers.Default));
        
        
        // ---Sorting Layers---
        
        Debug.Log(SortingLayers.Default);
        
        // SortingLayers列挙体の値はそのままSorting LayerのIDに変換できます。
        SortingLayer.GetLayerValueFromID((int)SortingLayers.Default);
    }
}
```
 
# Manual Generator
デフォルトでは、UniTypeはUnity 2021.2以降で使用できる Roslyn source generator の機能を使用します。 Unity 2021.1以下では、個別のパッケージとして提供される Manual Generator を使用して代替することができます。

## 要件

 - .NET ランタイム (`netcoreapp3.1` ターゲットが使用可能なもの)
    - `dotnet` CLIツールが使用できることを確認してください
 - MSBuild (Visual Studio や .NET SDK に含まれています)

## インストール

Package Manager から次の git URL を追加してください： `https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped.manualgenerator`

## 使い方

1. Projectビューから `Create` > `UniTyped` > `Manual Generator Profile` でGenerator Profileを作成します。
2. Generator Itemを追加します。
3. 対象のスクリプトを含む `csproj` のパスを指定します。
4. 出力先のC# スクリプトのパスを指定します（上書きされます！）
5. `Generate` ボタンを押します。

![image](https://user-images.githubusercontent.com/16096562/220120237-1fb1afa2-cd56-4b4f-80c6-aa1b3269a24e.png)

## ヒント
 - Manual generator の実行可能ファイル `UniTyped.Generator.Standalone.exe` が `Packages/com.ruccho.unityped.manualgenerator/Editor/Executable~/netcoreapp3.1` で見つかります。 このツールは次のオプションでコマンドラインから使用できます： `--project=<CSPROJ PATH> --output=<OUTPUT SCRIPT PATH>`.