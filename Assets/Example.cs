using System;
using UnityEngine;
using UniTyped;
using UniTyped.Generated;

[UniTyped] //コード生成用の属性
public class Example : MonoBehaviour
{
    [SerializeField] private int[] someArray;
    [SerializeField, UniTypedField(forceNested = true)] private Texture2D texture;

    [SerializeReference] private SerializeReferenceTest serializeReference;
}

public abstract class SerializeReferenceTest
{
    [SerializeField] private int baseValue;
}

//[Serializable]
public class SerializeReferenceTestDerived : SerializeReferenceTest
{
    [SerializeField] private int derivedValue;
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

        //配列
        for (int i = 0; i < view.someArray.Length; i++)
        {
            Debug.Log(view.someArray[i].Value);
        }
        
        //IEnumerator経由でアクセス
        foreach (var element in view.someArray)
        {
            Debug.Log(element.Value);
        }

        view.serializeReference ??= new SerializeReferenceTestDerived(); 

        serializedObject.ApplyModifiedProperties();
        
    }
}

#endif



/*


[UniTyped]
public class UniTypedTest : MonoBehaviour
{
    [SerializeField] private Byte ByteValue = default;
    [SerializeField] private SByte SByteValue = default;
    [SerializeField] private Int16 Int16Value = default;
    [SerializeField] private UInt16 UInt16Value = default;
    [SerializeField] private Int32 Int32Value = default;
    [SerializeField] private UInt32 UInt32Value = default;
    [SerializeField] private Int64 Int64Value = default;
    [SerializeField] private UInt64 UInt64Value = default;
    [SerializeField] private Single SingleValue = default;
    [SerializeField] private Double DoubleValue = default;
    [SerializeField] private Boolean BooleanValue = default;
    [SerializeField] private String StringValue = default;
    [SerializeField] private Char CharValue = default;

    
    [SerializeField] private AnimationCurve AnimationCurveValue = default;
    [SerializeField] private BoundsInt BoundsIntValue = default;
    [SerializeField] private Bounds BoundsValue = default;
    [SerializeField] private Color ColorValue = default;
    [SerializeField] private Hash128 Hash128Value = default;
    [SerializeField] private Quaternion QuaternionValue = default;
    [SerializeField] private RectInt RectIntValue = default;
    [SerializeField] private Rect RectValue = default;
    [SerializeField] private Vector2Int Vector2IntValue = default;
    [SerializeField] private Vector2 Vector2Value = default;
    [SerializeField] private Vector3Int Vector3IntValue = default;
    [SerializeField] private Vector3 Vector3Value = default;
    [SerializeField] private Vector4 Vector4Value = default;

    [SerializeField] private SomeEnum someEnum = default;
    [SerializeField] private SomeEnumSmall someEnumSmall = default;

    [SerializeField] private UnityEngine.Object someObject = default;
    [SerializeField] private Texture2D someTexture = default;

    [SerializeField] private GenericsTest<int, float> genericsValue = default;

    [SerializeField] private FixedBufferContainer fixedBufferContainer = default;

}

[Serializable]
public class GenericsTest<T1, T2>
{
    [SerializeField] private T1 t1Value;
    [SerializeField] private T1[] t1Array;
    [SerializeField] private List<T1> t1List;
}

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

[Serializable]
public unsafe struct FixedBufferContainer
{
    [SerializeField] private fixed char fixedBuffer[30];
}

*/