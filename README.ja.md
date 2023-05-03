# UniTyped

[English](README.md)

UniTypedは、SerializedObject や SerializedPropertyに対し、型付けされたアクセスを提供するソースジェネレータです。より簡潔で安全なエディタ拡張コードを書くのに役立ちます。

- **静的型付き**: 大量の `FindProperty` を書いたり、SerializedPropertyのややこしいAPIを触らずに、シリアライズされたデータに安全にアクセスできます。
- **低ヒープメモリ確保**:  生成コードは構造体ベースで、boxingを避けるように設計されています。OnInspectorGUI()やその他のエディタコードから繰り返し呼び出されてもメモリ確保量が少なく、エディタのパフォーマンスに優しいです。

## 要件
 - Unity 2021.2 以上で完全にサポートされます。
 - Unity 2021.1 以下では[Manual Generator](#manual-generator)を使用することでUniTypedの機能を利用できます。

## インストール
Package Manager から次の git URL を追加してください：`https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped`

## シリアライズされたデータへの型付きビュー
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

### 配列・リストの操作

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


### コード生成を調整する
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

## Materialの型付きビュー

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

## Animatorパラメータへの型付きビュー

`UniTyped.UniTypedAnimatorView` 属性つきの `partial` なstructを作成します。
最新のAnimator Controllerアセットが`Save Project`でディスクに保存されていることを確認してください。

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


## 制限事項
 - このプロジェクトは現在experimentalで、破壊的変更が加えられる可能性があります。
 - すべてのユースケースがカバーできていないかもしれません。お気づきの点があればissueでお知らせください。
 
## Manual Generator
デフォルトでは、UniTypeはUnity 2021.2以降で使用できる Roslyn source generator の機能を使用します。 Unity 2021.1以下では、個別のパッケージとして提供される Manual Generator を使用して代替することができます。

### 要件

 - .NET ランタイム (`netcoreapp3.1` ターゲットが使用可能なもの)
    - `dotnet` CLIツールが使用できることを確認してください
 - MSBuild (Visual Studio や .NET SDK に含まれています)

### インストール

Package Manager から次の git URL を追加してください： `https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped.manualgenerator`

### 使い方

1. Projectビューから `Create` > `UniTyped` > `Manual Generator Profile` でGenerator Profileを作成します。
2. Generator Itemを追加します。
3. 対象のスクリプトを含む `csproj` のパスを指定します。
4. 出力先のC# スクリプトのパスを指定します（上書きされます！）
5. `Generate` ボタンを押します。

![image](https://user-images.githubusercontent.com/16096562/220120237-1fb1afa2-cd56-4b4f-80c6-aa1b3269a24e.png)

### ヒント
 - Manual generator の実行可能ファイル `UniTyped.Generator.Standalone.exe` が `Packages/com.ruccho.unityped.manualgenerator/Editor/Executable~/netcoreapp3.1` で見つかります。 このツールは次のオプションでコマンドラインから使用できます： `--project=<CSPROJ PATH> --output=<OUTPUT SCRIPT PATH>`.