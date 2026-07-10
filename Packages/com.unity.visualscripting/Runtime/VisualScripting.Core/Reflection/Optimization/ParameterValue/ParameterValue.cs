using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly partial struct ParameterValue : IEquatable<ParameterValue>, IFormattable
    {
        public static readonly ParameterValue None = default;

        public enum ValueType : byte
        {
            None = 0,
            Byte, SByte, Short, UShort,
            Int, UInt, Long, ULong,
            Float, Double,
            Vector2, Vector3, Vector4, Quaternion, Color,
            Bool,
            String,
            Object
        }

        [FieldOffset(0)] public readonly ValueType type;
        [FieldOffset(4)] public readonly int objectID;
        [FieldOffset(8)] public readonly byte byteValue;
        [FieldOffset(8)] public readonly sbyte sbyteValue;
        [FieldOffset(8)] public readonly short shortValue;
        [FieldOffset(8)] public readonly ushort ushortValue;
        [FieldOffset(8)] public readonly int intValue;
        [FieldOffset(8)] public readonly uint uintValue;
        [FieldOffset(8)] public readonly long longValue;
        [FieldOffset(8)] public readonly ulong ulongValue;
        [FieldOffset(8)] public readonly float floatValue;
        [FieldOffset(8)] public readonly double doubleValue;
        [FieldOffset(8)] public readonly bool boolValue;
        [FieldOffset(8)] public readonly Vector2 vector2Value;
        [FieldOffset(8)] public readonly Vector3 vector3Value;
        [FieldOffset(8)] public readonly Vector4 vector4Value;
        [FieldOffset(8)] public readonly Color colorValue;
        [FieldOffset(8)] public readonly Quaternion quaternionValue;

        public readonly bool UsesObjectID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => type >= ValueType.String;
        }

        public readonly object ObjectValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (type < ValueType.String)
                    return ToObject();

                return ParameterValueObjectRegistry.Get(objectID);
            }
        }

        public readonly bool IsString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (type == ValueType.String) return true;
                if (type != ValueType.Object) return false;
                return ObjectValue is string;
            }
        }

        public readonly bool IsBoxedNumeric
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (type != ValueType.Object) return false;

                return ObjectValue switch
                {
                    sbyte => true,
                    byte => true,
                    short => true,
                    ushort => true,
                    int => true,
                    uint => true,
                    long => true,
                    ulong => true,
                    float => true,
                    double => true,
                    decimal => true,
                    _ => false
                };
            }
        }

        public readonly bool IsBoxed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return type == ValueType.Object;
            }
        }

        /// <summary>
        /// Free the index of the object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeObject(int index)
        {
            ParameterValueObjectRegistry.Free(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void UpdateObject(object newValue)
        {
            ParameterValueObjectRegistry.Update(objectID, newValue);
        }

        /// <summary>
        /// Unboxes the objectValue to a ref of <typeparamref name="T"/>.
        /// Only use when <see cref="IsBoxed"/> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Unbox<T>() where T : struct => ref Unsafe.Unbox<T>(ObjectValue);

        #region Comparison

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsNull()
        {
            return type == ValueType.None || (UsesObjectID && (objectID < 0 || ObjectValue == null));
        }

        #endregion

        #region Cast Helpers

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly T ThrowInvalidCast<T>() => throw new InvalidCastException($"Cannot convert {type} to {typeof(T).FullName}");

        private readonly T CastObject<T>()
        {
            var obj = ObjectValue;

            if (obj is T value) return value;

            if (obj is null) return default;

            if (TypeTraits<T>.IsNumeric)
            {
                return AsNumeric<T>();
            }
            return ConversionUtility.Convert<T>(obj);
        }

        public readonly byte ToByte() => type == ValueType.Byte ? byteValue : CoerceToByte();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly byte CoerceToByte() => type switch
        {
            ValueType.SByte => (byte)sbyteValue,
            ValueType.Short => (byte)shortValue,
            ValueType.UShort => (byte)ushortValue,
            ValueType.Int => (byte)intValue,
            ValueType.UInt => (byte)uintValue,
            ValueType.Long => (byte)longValue,
            ValueType.ULong => (byte)ulongValue,
            ValueType.Float => (byte)floatValue,
            ValueType.Double => (byte)doubleValue,
            ValueType.Object => AsNumeric<byte>(),
            _ => ThrowInvalidCast<byte>()
        };

        public readonly sbyte ToSByte() => type == ValueType.SByte ? sbyteValue : CoerceToSByte();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly sbyte CoerceToSByte() => type switch
        {
            ValueType.Byte => (sbyte)byteValue,
            ValueType.Short => (sbyte)shortValue,
            ValueType.UShort => (sbyte)ushortValue,
            ValueType.Int => (sbyte)intValue,
            ValueType.UInt => (sbyte)uintValue,
            ValueType.Long => (sbyte)longValue,
            ValueType.ULong => (sbyte)ulongValue,
            ValueType.Float => (sbyte)floatValue,
            ValueType.Double => (sbyte)doubleValue,
            ValueType.Object => AsNumeric<sbyte>(),
            _ => ThrowInvalidCast<sbyte>()
        };

        public readonly short ToInt16() => type == ValueType.Short ? shortValue : CoerceToInt16();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly short CoerceToInt16() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.UShort => (short)ushortValue,
            ValueType.Int => (short)intValue,
            ValueType.UInt => (short)uintValue,
            ValueType.Long => (short)longValue,
            ValueType.ULong => (short)ulongValue,
            ValueType.Float => (short)floatValue,
            ValueType.Double => (short)doubleValue,
            ValueType.Object => AsNumeric<short>(),
            _ => ThrowInvalidCast<short>()
        };

        public readonly ushort ToUInt16() => type == ValueType.UShort ? ushortValue : CoerceToUInt16();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly ushort CoerceToUInt16() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (ushort)sbyteValue,
            ValueType.Short => (ushort)shortValue,
            ValueType.Int => (ushort)intValue,
            ValueType.UInt => (ushort)uintValue,
            ValueType.Long => (ushort)longValue,
            ValueType.ULong => (ushort)ulongValue,
            ValueType.Float => (ushort)floatValue,
            ValueType.Double => (ushort)doubleValue,
            ValueType.Object => AsNumeric<ushort>(),
            _ => ThrowInvalidCast<ushort>()
        };

        public readonly int ToInt32() => type == ValueType.Int ? intValue : CoerceToInt32();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly int CoerceToInt32() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.UInt => (int)uintValue,
            ValueType.Long => (int)longValue,
            ValueType.ULong => (int)ulongValue,
            ValueType.Float => (int)floatValue,
            ValueType.Double => (int)doubleValue,
            ValueType.Object => AsNumeric<int>(),
            _ => ThrowInvalidCast<int>()
        };

        public readonly uint ToUInt32() => type == ValueType.UInt ? uintValue : CoerceToUInt32();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly uint CoerceToUInt32() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (uint)sbyteValue,
            ValueType.Short => (uint)shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => (uint)intValue,
            ValueType.Long => (uint)longValue,
            ValueType.ULong => (uint)ulongValue,
            ValueType.Float => (uint)floatValue,
            ValueType.Double => (uint)doubleValue,
            ValueType.Object => AsNumeric<uint>(),
            _ => ThrowInvalidCast<uint>()
        };

        public readonly long ToInt64() => type == ValueType.Long ? longValue : CoerceToInt64();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly long CoerceToInt64() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => uintValue,
            ValueType.ULong => (long)ulongValue,
            ValueType.Float => (long)floatValue,
            ValueType.Double => (long)doubleValue,
            ValueType.Object => AsNumeric<long>(),
            _ => ThrowInvalidCast<long>()
        };

        public readonly ulong ToUInt64() => type == ValueType.ULong ? ulongValue : CoerceToUInt64();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly ulong CoerceToUInt64() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (ulong)sbyteValue,
            ValueType.Short => (ulong)shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => (ulong)intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => (ulong)longValue,
            ValueType.Float => (ulong)floatValue,
            ValueType.Double => (ulong)doubleValue,
            ValueType.Object => AsNumeric<ulong>(),
            _ => ThrowInvalidCast<ulong>()
        };

        public readonly float ToSingle() => type == ValueType.Float ? floatValue : CoerceToSingle();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly float CoerceToSingle() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => longValue,
            ValueType.ULong => ulongValue,
            ValueType.Double => (float)doubleValue,
            ValueType.Object => AsNumeric<float>(),
            _ => ThrowInvalidCast<float>()
        };

        public readonly double ToDouble() => type == ValueType.Double ? doubleValue : CoerceToDouble();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly double CoerceToDouble() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => longValue,
            ValueType.ULong => ulongValue,
            ValueType.Float => floatValue,
            ValueType.Object => AsNumeric<double>(),
            _ => ThrowInvalidCast<double>()
        };

        public readonly bool ToBool() => type == ValueType.Bool ? boolValue : CoerceToBool();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool CoerceToBool() => type switch
        {
            ValueType.Byte or ValueType.SByte or ValueType.Short or ValueType.UShort or
            ValueType.Int or ValueType.UInt or ValueType.Long or ValueType.ULong => ulongValue != 0,

            ValueType.Float => floatValue != 0.0f,
            ValueType.Double => doubleValue != 0.0,
            ValueType.Object => ConversionUtility.Convert<bool>(ObjectValue),
            _ => ThrowInvalidCast<bool>()
        };

        public readonly Vector2 ToVector2() => type == ValueType.Vector2 ? vector2Value : CoerceToVector2();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly Vector2 CoerceToVector2() => type switch
        {
            ValueType.Vector3 or ValueType.Vector4 or ValueType.Color or ValueType.Quaternion => vector2Value,

            ValueType.Object => ConversionUtility.Convert<Vector2>(ObjectValue),
            _ => ThrowInvalidCast<Vector2>()
        };

        public readonly Vector3 ToVector3() => type == ValueType.Vector3 ? vector3Value : CoerceToVector3();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly Vector3 CoerceToVector3() => type switch
        {
            ValueType.Vector4 or ValueType.Color => vector3Value,
            ValueType.Vector2 => vector2Value,
            ValueType.Quaternion => quaternionValue.eulerAngles,

            ValueType.Object => ConversionUtility.Convert<Vector3>(ObjectValue),
            _ => ThrowInvalidCast<Vector3>()
        };

        public readonly Vector4 ToVector4() => type == ValueType.Vector4 ? vector4Value : CoerceToVector4();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly Vector4 CoerceToVector4() => type switch
        {
            ValueType.Color or ValueType.Quaternion => vector4Value,
            ValueType.Vector3 => vector3Value,
            ValueType.Vector2 => vector2Value,

            ValueType.Object => ConversionUtility.Convert<Vector4>(ObjectValue),
            _ => ThrowInvalidCast<Vector4>()
        };

        public readonly Color ToColor() => type == ValueType.Color ? colorValue : CoerceToColor();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly Color CoerceToColor() => type switch
        {
            ValueType.Vector4 or ValueType.Quaternion => colorValue,
            ValueType.Vector3 => new Color(vector3Value.x, vector3Value.y, vector3Value.z),
            ValueType.Vector2 => new Color(vector2Value.x, vector2Value.y, 0f),

            ValueType.Object => ConversionUtility.Convert<Color>(ObjectValue),
            _ => ThrowInvalidCast<Color>()
        };

        public readonly Quaternion ToQuaternion() => type == ValueType.Quaternion ? quaternionValue : CoerceToQuaternion();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly Quaternion CoerceToQuaternion() => type switch
        {
            ValueType.Vector4 or ValueType.Color => quaternionValue,
            ValueType.Vector3 => Quaternion.Euler(vector3Value),
            ValueType.Vector2 => Quaternion.Euler(vector2Value.x, vector2Value.y, 0f),

            ValueType.Object => ConversionUtility.Convert<Quaternion>(ObjectValue),
            _ => ThrowInvalidCast<Quaternion>()
        };

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly object ToObject() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => longValue,
            ValueType.ULong => ulongValue,
            ValueType.Float => floatValue,
            ValueType.Double => doubleValue,
            ValueType.Bool => boolValue,
            ValueType.Vector2 => vector2Value,
            ValueType.Vector3 => vector3Value,
            ValueType.Vector4 => vector4Value,
            ValueType.Quaternion => quaternionValue,
            ValueType.Color => colorValue,
            ValueType.String => (string)ObjectValue,
            ValueType.Object => ObjectValue,
            _ => null
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void SetTypeUnsafe(ValueType newType)
        {
            Unsafe.AsRef(in type) = newType;
        }

        #region Create

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(byte value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Byte;
            byteValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(sbyte value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.SByte;
            sbyteValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(short value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Short;
            shortValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(ushort value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.UShort;
            ushortValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(int value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Int;
            intValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(uint value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.UInt;
            uintValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(long value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Long;
            longValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(ulong value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.ULong;
            ulongValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(float value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Float;
            floatValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(double value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Double;
            doubleValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(bool value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Bool;
            boolValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector2 value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Vector2;
            vector2Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector3 value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Vector3;
            vector3Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector4 value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Vector4;
            vector4Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Quaternion value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Quaternion;
            quaternionValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Color value)
        {
            Unsafe.SkipInit(out this);
            objectID = -1;
            type = ValueType.Color;
            colorValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(string value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.String;
            objectID = ParameterValueObjectRegistry.Allocate(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(string value, out int handle)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.String;
            handle = ParameterValueObjectRegistry.Allocate(value);
            objectID = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(UnityEngine.Object value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            objectID = ParameterValueObjectRegistry.Allocate(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(UnityEngine.Object value, out int handle)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            handle = ParameterValueObjectRegistry.Allocate(value);
            objectID = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(object value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            objectID = ParameterValueObjectRegistry.Allocate(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(object value, out int handle)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            handle = ParameterValueObjectRegistry.Allocate(value);
            objectID = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(in ParameterValue other)
        {
            this = other;

            if (other.UsesObjectID)
            {
                var managedObject = ParameterValueObjectRegistry.Get(other.objectID);
                objectID = ParameterValueObjectRegistry.Allocate(managedObject);
            }
        }

        public static implicit operator ParameterValue(byte value) => new ParameterValue(value);
        public static implicit operator ParameterValue(sbyte value) => new ParameterValue(value);
        public static implicit operator ParameterValue(short value) => new ParameterValue(value);
        public static implicit operator ParameterValue(ushort value) => new ParameterValue(value);
        public static implicit operator ParameterValue(int value) => new ParameterValue(value);
        public static implicit operator ParameterValue(uint value) => new ParameterValue(value);
        public static implicit operator ParameterValue(long value) => new ParameterValue(value);
        public static implicit operator ParameterValue(ulong value) => new ParameterValue(value);

        public static implicit operator ParameterValue(float value) => new ParameterValue(value);
        public static implicit operator ParameterValue(double value) => new ParameterValue(value);

        public static implicit operator ParameterValue(bool value) => new ParameterValue(value);

        public static implicit operator ParameterValue(Vector2 value) => new ParameterValue(value);
        public static implicit operator ParameterValue(Vector3 value) => new ParameterValue(value);
        public static implicit operator ParameterValue(Vector4 value) => new ParameterValue(value);
        public static implicit operator ParameterValue(Quaternion value) => new ParameterValue(value);
        public static implicit operator ParameterValue(Color value) => new ParameterValue(value);
        public static explicit operator ParameterValue(string value) => new ParameterValue(value);

        public static implicit operator byte(ParameterValue value) => value.ToByte();
        public static implicit operator sbyte(ParameterValue value) => value.ToSByte();
        public static implicit operator short(ParameterValue value) => value.ToInt16();
        public static implicit operator ushort(ParameterValue value) => value.ToUInt16();
        public static implicit operator int(ParameterValue value) => value.ToInt32();
        public static implicit operator uint(ParameterValue value) => value.ToUInt32();
        public static implicit operator long(ParameterValue value) => value.ToInt64();
        public static implicit operator ulong(ParameterValue value) => value.ToUInt64();

        public static implicit operator float(ParameterValue value) => value.ToSingle();
        public static implicit operator double(ParameterValue value) => value.ToDouble();
        public static implicit operator decimal(ParameterValue value) => value.Cast<decimal>();

        public static implicit operator bool(ParameterValue value) => value.ToBool();

        public static implicit operator Vector2(ParameterValue value) => value.ToVector2();
        public static implicit operator Vector3(ParameterValue value) => value.ToVector3();
        public static implicit operator Vector4(ParameterValue value) => value.ToVector4();
        public static implicit operator Quaternion(ParameterValue value) => value.ToQuaternion();
        public static implicit operator Color(ParameterValue value) => value.ToColor();
        public static implicit operator string(ParameterValue value) => value.ToString();
        #endregion

        private static readonly Dictionary<Type, ValueType> TypeToEnum = new Dictionary<Type, ValueType>
        {
            { typeof(byte), ValueType.Byte },
            { typeof(sbyte), ValueType.SByte },
            { typeof(short), ValueType.Short },
            { typeof(ushort), ValueType.UShort },
            { typeof(int), ValueType.Int },
            { typeof(uint), ValueType.UInt },
            { typeof(long), ValueType.Long },
            { typeof(ulong), ValueType.ULong },
            { typeof(float), ValueType.Float },
            { typeof(double), ValueType.Double },
            { typeof(bool), ValueType.Bool },
            { typeof(Vector2), ValueType.Vector2 },
            { typeof(Vector3), ValueType.Vector3 },
            { typeof(Vector4), ValueType.Vector4 },
            { typeof(Quaternion), ValueType.Quaternion },
            { typeof(Color), ValueType.Color },
            { typeof(string), ValueType.String },
        };

        public static ValueType GetParameterValueType(Type type)
        {
            if (type == null) return ValueType.None;

            if (TypeToEnum.TryGetValue(type, out var valueType))
            {
                return valueType;
            }

            return ValueType.Object;
        }

        public override bool Equals(object obj)
        {
            var isNull = IsNull();
            if (obj == null || isNull) return obj == null && isNull;

            if (obj is ParameterValue other) return Equals(other);

            return obj switch
            {
                int i when type == ValueType.Int => intValue == i,
                float f when type == ValueType.Float => floatValue == f,
                string s when type == ValueType.String => (string)ObjectValue == s,
                bool b when type == ValueType.Bool => boolValue == b,
                Vector2 v2 when type == ValueType.Vector2 => vector2Value == v2,
                Vector3 v3 when type == ValueType.Vector3 => vector3Value == v3,
                Quaternion q when type == ValueType.Quaternion => quaternionValue == q,
                double d when type == ValueType.Double => doubleValue == d,
                long l when type == ValueType.Long => longValue == l,
                uint ui when type == ValueType.UInt => uintValue == ui,
                ulong ul when type == ValueType.ULong => ulongValue == ul,
                byte by when type == ValueType.Byte => byteValue == by,
                sbyte sb when type == ValueType.SByte => sbyteValue == sb,
                short s when type == ValueType.Short => shortValue == s,
                ushort us when type == ValueType.UShort => ushortValue == us,
                Vector4 v4 when type == ValueType.Vector4 => vector4Value == v4,
                Color c when type == ValueType.Color => colorValue == c,
                _ => type == ValueType.Object && (ObjectValue?.Equals(obj) ?? false)
            };
        }

        public readonly bool Equals(ParameterValue other)
        {
            if (type != other.type) return false;

            var isNull = IsNull();
            var otherIsNull = other.IsNull();
            if (otherIsNull || isNull) return otherIsNull && isNull;

            return type switch
            {
                ValueType.Int => intValue == other.intValue,
                ValueType.Float => floatValue == other.floatValue,
                ValueType.Vector3 => vector3Value == other.vector3Value,
                ValueType.String => (string)ObjectValue == (string)other.ObjectValue,
                ValueType.Object => ObjectValue.Equals(other.ObjectValue),
                ValueType.Bool => boolValue == other.boolValue,
                ValueType.Double => doubleValue == other.doubleValue,
                ValueType.Vector2 => vector2Value == other.vector2Value,
                ValueType.Quaternion => quaternionValue == other.quaternionValue,
                ValueType.Long => longValue == other.longValue,
                ValueType.UInt => uintValue == other.uintValue,
                ValueType.ULong => ulongValue == other.ulongValue,
                ValueType.Byte => byteValue == other.byteValue,
                ValueType.SByte => sbyteValue == other.sbyteValue,
                ValueType.Short => shortValue == other.shortValue,
                ValueType.UShort => ushortValue == other.ushortValue,
                ValueType.Vector4 => vector4Value == other.vector4Value,
                ValueType.Color => colorValue == other.colorValue,
                _ => false
            };
        }

        public static bool operator ==(ParameterValue left, object right) => left.Equals(right);
        public static bool operator !=(ParameterValue left, object right) => !left.Equals(right);

        public static bool operator ==(object left, ParameterValue right) => right.Equals(left);
        public static bool operator !=(object left, ParameterValue right) => !right.Equals(left);

        public static bool operator ==(ParameterValue left, ParameterValue right) => left.Equals(right);
        public static bool operator !=(ParameterValue left, ParameterValue right) => !left.Equals(right);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(type);

            switch (type)
            {
                case ValueType.None:
                    break;
                case ValueType.Bool:
                    hash.Add(boolValue);
                    break;
                case ValueType.Byte:
                    hash.Add(byteValue);
                    break;
                case ValueType.SByte:
                    hash.Add(sbyteValue);
                    break;
                case ValueType.Short:
                    hash.Add(shortValue);
                    break;
                case ValueType.UShort:
                    hash.Add(ushortValue);
                    break;
                case ValueType.Int:
                    hash.Add(intValue);
                    break;
                case ValueType.UInt:
                    hash.Add(uintValue);
                    break;
                case ValueType.Long:
                    hash.Add(longValue);
                    break;
                case ValueType.ULong:
                    hash.Add(ulongValue);
                    break;
                case ValueType.Float:
                    hash.Add(floatValue);
                    break;
                case ValueType.Double:
                    hash.Add(doubleValue);
                    break;
                case ValueType.Vector2:
                    hash.Add(vector2Value);
                    break;
                case ValueType.Vector3:
                    hash.Add(vector3Value);
                    break;
                case ValueType.Vector4:
                    hash.Add(vector4Value);
                    break;
                case ValueType.Quaternion:
                    hash.Add(quaternionValue);
                    break;
                case ValueType.Color:
                    hash.Add(colorValue);
                    break;
                default:
                    hash.Add(ObjectValue);
                    break;
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return type switch
            {
                ValueType.None => "null",
                ValueType.Bool => boolValue.ToString(),
                ValueType.Byte => byteValue.ToString(),
                ValueType.SByte => sbyteValue.ToString(),
                ValueType.Short => shortValue.ToString(),
                ValueType.UShort => ushortValue.ToString(),
                ValueType.Int => intValue.ToString(),
                ValueType.UInt => uintValue.ToString(),
                ValueType.Long => longValue.ToString(),
                ValueType.ULong => ulongValue.ToString(),
                ValueType.Float => floatValue.ToString(),
                ValueType.Double => doubleValue.ToString(),
                ValueType.Vector2 => vector2Value.ToString(),
                ValueType.Vector3 => vector3Value.ToString(),
                ValueType.Vector4 => vector4Value.ToString(),
                ValueType.Quaternion => quaternionValue.ToString(),
                ValueType.Color => colorValue.ToString(),
                _ => ObjectValue?.ToString() ?? "null",
            };
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (type)
            {
                case ValueType.None:
                    return "null";
                case ValueType.Bool:
                    return boolValue.ToString();
                case ValueType.Byte:
                    return byteValue.ToString(format, formatProvider);
                case ValueType.SByte:
                    return sbyteValue.ToString(format, formatProvider);
                case ValueType.Short:
                    return shortValue.ToString(format, formatProvider);
                case ValueType.UShort:
                    return ushortValue.ToString(format, formatProvider);
                case ValueType.Int:
                    return intValue.ToString(format, formatProvider);
                case ValueType.UInt:
                    return uintValue.ToString(format, formatProvider);
                case ValueType.Long:
                    return longValue.ToString(format, formatProvider);
                case ValueType.ULong:
                    return ulongValue.ToString(format, formatProvider);
                case ValueType.Float:
                    return floatValue.ToString(format, formatProvider);
                case ValueType.Double:
                    return doubleValue.ToString(format, formatProvider);
                case ValueType.Vector2:
                    return vector2Value.ToString(format, formatProvider);
                case ValueType.Vector3:
                    return vector3Value.ToString(format, formatProvider);
                case ValueType.Vector4:
                    return vector4Value.ToString(format, formatProvider);
                case ValueType.Quaternion:
                    return quaternionValue.ToString(format, formatProvider);
                case ValueType.Color:
                    return colorValue.ToString(format, formatProvider);
                default:
                    {
                        var val = ObjectValue;
                        if (val is IFormattable formattable) return formattable.ToString(format, formatProvider);
                        return val?.ToString() ?? "null";
                    }
            }
        }

        public static class TypeTraits<T>
        {
            public static readonly bool IsInt = typeof(T) == typeof(int);
            public static readonly bool IsFloat = typeof(T) == typeof(float);
            public static readonly bool IsBool = typeof(T) == typeof(bool);
            public static readonly bool IsVector2 = typeof(T) == typeof(Vector2);
            public static readonly bool IsVector3 = typeof(T) == typeof(Vector3);
            public static readonly bool IsVector4 = typeof(T) == typeof(Vector4);
            public static readonly bool IsColor = typeof(T) == typeof(Color);
            public static readonly bool IsQuaternion = typeof(T) == typeof(Quaternion);
            public static readonly bool IsString = typeof(T) == typeof(string);
            public static readonly bool IsLong = typeof(T) == typeof(long);
            public static readonly bool IsDouble = typeof(T) == typeof(double);
            public static readonly bool IsByte = typeof(T) == typeof(byte);
            public static readonly bool IsUInt = typeof(T) == typeof(uint);
            public static readonly bool IsULong = typeof(T) == typeof(ulong);
            public static readonly bool IsShort = typeof(T) == typeof(short);
            public static readonly bool IsUShort = typeof(T) == typeof(ushort);
            public static readonly bool IsSByte = typeof(T) == typeof(sbyte);
            public static readonly bool IsDecimal = typeof(T) == typeof(decimal);
            public static readonly bool IsObject = typeof(T) == typeof(object);

            public static readonly ValueType ValueType = GetParameterValueType(typeof(T));

            public static readonly Type Type = typeof(T);
            public static readonly bool IsNumeric = IsInt || IsFloat || IsDouble || IsLong || IsByte || IsSByte || IsShort || IsUShort || IsUInt || IsULong || IsDecimal;
            public static readonly bool IsPrimitiveNumericNoDecimal =
            IsInt || IsFloat || IsLong || IsDouble || IsByte ||
            IsSByte || IsShort || IsUShort || IsUInt || IsULong;

            public static readonly bool IsNullable = !typeof(T).IsValueType || (Nullable.GetUnderlyingType(typeof(T)) != null);
        }
    }
}
