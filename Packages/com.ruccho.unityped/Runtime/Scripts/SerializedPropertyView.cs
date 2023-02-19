#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


namespace UniTyped.Editor
{
    public interface ISerializedPropertyView
    {
        public SerializedProperty Property { get; set; }
    }

    public struct SerializedPropertyViewUnsupported : ISerializedPropertyView
    {
        public SerializedProperty Property { get; set; }
    }

    public interface ISerializedPropertyView<T> : ISerializedPropertyView
    {
        public T Value { get; set; }
    }

    public struct SerializedPropertyViewArray<TElementView> : IEnumerable<TElementView>
        where TElementView : struct, ISerializedPropertyView
    {
        public SerializedProperty Property { get; set; }

        public int Length
        {
            get => Property.arraySize;
            set => Property.arraySize = value;
        }

        public TElementView this[int index]
        {
            get
            {
                var view = new TElementView()
                {
                    Property = Property.GetArrayElementAtIndex(index)
                };

                return view;
            }
        }

        public void InsertElementAt(int index)
        {
            Property.InsertArrayElementAtIndex(index);
        }

        public void DeleteElementAt(int index)
        {
            Property.DeleteArrayElementAtIndex(index);
        }

        public void Clear()
        {
            Property.ClearArray();
        }

        public bool MoveElement(int srcIndex, int destIndex)
        {
            return Property.MoveArrayElement(srcIndex, destIndex);
        }

        public IEnumerator<TElementView> GetEnumerator()
        {
            return Enumerator.Get(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class Enumerator : IEnumerator<TElementView>
        {
            private static readonly Stack<Enumerator> pool = new Stack<Enumerator>();

            public static Enumerator Get(SerializedPropertyViewArray<TElementView> target)
            {
                if (pool.TryPeek(out var pooled))
                {
                    var popped = pool.Pop();
                    popped.target = target;
                    popped.Reset();
                    return popped;
                }

                return new Enumerator(target);
            }

            private SerializedPropertyViewArray<TElementView> target;
            private int cursor = -1;

            public Enumerator(SerializedPropertyViewArray<TElementView> target)
            {
                this.target = target;
            }

            public bool MoveNext()
            {
                cursor++;
                if (cursor < target.Length)
                {
                    Current = target[cursor];
                    return true;
                }
                else return false;
            }

            public void Reset()
            {
                cursor = -1;
            }

            public TElementView Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                pool.Push(this);
            }
        }
    }

    public struct SerializedPropertyViewFixedBuffer<TElementView> where TElementView : struct, ISerializedPropertyView
    {
        public SerializedProperty Property { get; set; }

        public int Length
        {
            get => Property.fixedBufferSize;
        }

        public TElementView this[int index]
        {
            get
            {
                var view = new TElementView()
                {
                    Property = Property.GetFixedBufferElementAtIndex(index)
                };

                return view;
            }
        }
    }


    public struct SerializedPropertyViewObjectReference<T> : ISerializedPropertyView<T> where T : UnityEngine.Object
    {
        public SerializedProperty Property { get; set; }

        public T Value
        {
            get => Property.objectReferenceValue as T;
            set => Property.objectReferenceValue = value;
        }
    }

    public struct SerializedPropertyViewManagedReference<T> : ISerializedPropertyView<T>
    {
        public SerializedProperty Property { get; set; }

        public T Value
        {
            get => (T)Property.managedReferenceValue;
            set => Property.managedReferenceValue = value;
        }
    }

    public struct SerializedPropertyViewByte : ISerializedPropertyView<byte>
    {
        public SerializedProperty Property { get; set; }

        public byte Value
        {
            get => (byte)Property.intValue;
            set => Property.intValue = value;
        }
    }

    public struct SerializedPropertyViewSByte : ISerializedPropertyView<sbyte>
    {
        public SerializedProperty Property { get; set; }

        public sbyte Value
        {
            get => (sbyte)Property.intValue;
            set => Property.intValue = value;
        }
    }

    public struct SerializedPropertyViewShort : ISerializedPropertyView<short>
    {
        public SerializedProperty Property { get; set; }

        public short Value
        {
            get => (short)Property.intValue;
            set => Property.intValue = value;
        }
    }

    public struct SerializedPropertyViewUShort : ISerializedPropertyView<ushort>
    {
        public SerializedProperty Property { get; set; }

        public ushort Value
        {
            get => (ushort)Property.intValue;
            set => Property.intValue = value;
        }
    }

    public struct SerializedPropertyViewInt : ISerializedPropertyView<int>
    {
        public SerializedProperty Property { get; set; }

        public int Value
        {
            get => Property.intValue;
            set => Property.intValue = value;
        }
    }

    public struct SerializedPropertyViewUInt : ISerializedPropertyView<uint>
    {
        public SerializedProperty Property { get; set; }

        public uint Value
        {
            get => (uint)Property.longValue;
            set => Property.longValue = value;
        }
    }

    public struct SerializedPropertyViewLong : ISerializedPropertyView<long>
    {
        public SerializedProperty Property { get; set; }

        public long Value
        {
            get => Property.longValue;
            set => Property.longValue = value;
        }
    }

    public struct SerializedPropertyViewULong : ISerializedPropertyView<ulong>
    {
        public SerializedProperty Property { get; set; }

        public ulong Value
        {
#if UNITY_2022_2_OR_NEWER
            get => Property.ulongValue;
            set => Property.ulongValue = value;
#else
            get
            {
                return unchecked((ulong)Property.longValue);
            }
            set
            {
                Property.longValue = unchecked((long)value);
            }
#endif
        }
    }

    public struct SerializedPropertyViewFloat : ISerializedPropertyView<float>
    {
        public SerializedProperty Property { get; set; }

        public float Value
        {
            get => Property.floatValue;
            set => Property.floatValue = value;
        }
    }

    public struct SerializedPropertyViewDouble : ISerializedPropertyView<double>
    {
        public SerializedProperty Property { get; set; }

        public double Value
        {
            get => Property.doubleValue;
            set => Property.doubleValue = value;
        }
    }

    public struct SerializedPropertyViewBool : ISerializedPropertyView<bool>
    {
        public SerializedProperty Property { get; set; }

        public bool Value
        {
            get => Property.boolValue;
            set => Property.boolValue = value;
        }
    }

    public struct SerializedPropertyViewString : ISerializedPropertyView<string>
    {
        public SerializedProperty Property { get; set; }

        public string Value
        {
            get => Property.stringValue;
            set => Property.stringValue = value;
        }
    }

    public struct SerializedPropertyViewChar : ISerializedPropertyView<char>
    {
        public SerializedProperty Property { get; set; }

        public char Value
        {
            get
            {
                var val = Property.stringValue;
                return val.Length >= 0 ? val[0] : '\0';
            }
            set => Property.stringValue = new string(value, 1);
        }
    }

    public struct SerializedPropertyViewAnimationCurve : ISerializedPropertyView<AnimationCurve>
    {
        public SerializedProperty Property { get; set; }

        public AnimationCurve Value
        {
            get => Property.animationCurveValue;
            set => Property.animationCurveValue = value;
        }
    }

    public struct SerializedPropertyViewBoundsInt : ISerializedPropertyView<BoundsInt>
    {
        public SerializedProperty Property { get; set; }

        public BoundsInt Value
        {
            get => Property.boundsIntValue;
            set => Property.boundsIntValue = value;
        }
    }

    public struct SerializedPropertyViewBounds : ISerializedPropertyView<Bounds>
    {
        public SerializedProperty Property { get; set; }

        public Bounds Value
        {
            get => Property.boundsValue;
            set => Property.boundsValue = value;
        }
    }

    public struct SerializedPropertyViewColor : ISerializedPropertyView<Color>
    {
        public SerializedProperty Property { get; set; }

        public Color Value
        {
            get => Property.colorValue;
            set => Property.colorValue = value;
        }
    }

    public struct SerializedPropertyViewHash128 : ISerializedPropertyView<Hash128>
    {
        public SerializedProperty Property { get; set; }

        public Hash128 Value
        {
            get => Property.hash128Value;
            set => Property.hash128Value = value;
        }
    }

    public struct SerializedPropertyViewQuaternion : ISerializedPropertyView<Quaternion>
    {
        public SerializedProperty Property { get; set; }

        public Quaternion Value
        {
            get => Property.quaternionValue;
            set => Property.quaternionValue = value;
        }
    }

    public struct SerializedPropertyViewRectInt : ISerializedPropertyView<RectInt>
    {
        public SerializedProperty Property { get; set; }

        public RectInt Value
        {
            get => Property.rectIntValue;
            set => Property.rectIntValue = value;
        }
    }

    public struct SerializedPropertyViewRect : ISerializedPropertyView<Rect>
    {
        public SerializedProperty Property { get; set; }

        public Rect Value
        {
            get => Property.rectValue;
            set => Property.rectValue = value;
        }
    }

    public struct SerializedPropertyViewVector2Int : ISerializedPropertyView<Vector2Int>
    {
        public SerializedProperty Property { get; set; }

        public Vector2Int Value
        {
            get => Property.vector2IntValue;
            set => Property.vector2IntValue = value;
        }
    }

    public struct SerializedPropertyViewVector2 : ISerializedPropertyView<Vector2>
    {
        public SerializedProperty Property { get; set; }

        public Vector2 Value
        {
            get => Property.vector2Value;
            set => Property.vector2Value = value;
        }
    }

    public struct SerializedPropertyViewVector3Int : ISerializedPropertyView<Vector3Int>
    {
        public SerializedProperty Property { get; set; }

        public Vector3Int Value
        {
            get => Property.vector3IntValue;
            set => Property.vector3IntValue = value;
        }
    }

    public struct SerializedPropertyViewVector3 : ISerializedPropertyView<Vector3>
    {
        public SerializedProperty Property { get; set; }

        public Vector3 Value
        {
            get => Property.vector3Value;
            set => Property.vector3Value = value;
        }
    }

    public struct SerializedPropertyViewVector4 : ISerializedPropertyView<Vector4>
    {
        public SerializedProperty Property { get; set; }

        public Vector4 Value
        {
            get => Property.vector4Value;
            set => Property.vector4Value = value;
        }
    }
}
#endif