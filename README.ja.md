# UniTyped

[English](README.md)

UniTypedは、SerializedObject や SerializedPropertyに対し、型付けされたアクセスを提供するソースジェネレータです。より簡潔で安全なエディタ拡張コードを書くのに役立ちます。

- **静的型付き**: 大量の `FindProperty` を書いたり、SerializedPropertyのややこしいAPIを触らずに、シリアライズされたデータに安全にアクセスできます。
- **低ヒープメモリ確保**:  生成コードは構造体ベースで、boxingを避けるように設計されています。OnInspectorGUI()やその他のエディタコードから繰り返し呼び出されてもメモリ確保量が少なく、エディタのパフォーマンスに優しいです。

## 要件
 - Unity 2021.3 or newer

## インストール
Package Manager から次の git URL を追加してください：`https://github.com/ruccho/UniTyped.git?path=/Packages/com.ruccho.unityped`

## 使い方
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

## サポートされている型

参考: https://docs.unity3d.com/Manual/script-Serialization.html

 - C#のプリミティブ型 (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `string`, `char`)
 - Unityのビルトイン型 (`AnimationCurve`, `BoundsInt`, `Bounds`, `Color`, `Hash128`, `Quaternion`, `RectInt`, `Rect`, `Vector2Int`, `Vector2`, `Vector3Int`, `Vector3`, `Vector4`)
 - Enum (32ビット以下)
 - `UnityEngine.Object` 派生型への参照
 - `[Serializable]`つきのカスタムクラス・構造体
 - 要素の型がシリアライズ可能な Array / List<T>
 - 固定サイズバッファ
 - `[SerializeReference]` つきのフィールド

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


## コード生成を調整する
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



## 制限事項
 - このプロジェクトは現在experimentalで、破壊的変更が加えられる可能性があります。
 - すべてのユースケースがカバーできていないかもしれません。お気づきの点があればissueでお知らせください。

