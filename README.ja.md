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
 - `[SerializeReference]` には（まだ）対応していません。
 - このプロジェクトは現在experimentalで、破壊的変更が加えられる可能性があります。
 - すべてのユースケースがカバーできていないかもしれません。お気づきの点があればissueでお知らせください。

