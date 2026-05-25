using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ParameterValue : IEquatable<ParameterValue>, IFormattable
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
        [FieldOffset(8)] public readonly int objectID;
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


        private static object[] ManagedObjects = new object[2048];
        private static int totalAllocatedCount = 0;

        private static int[] FreeIndices = new int[2048];
        private static int freeIndicesCount = 0;

        public readonly object ObjectValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (UsesObjectID)
                {
                    uint idx = (uint)objectID;
                    if (idx < (uint)ManagedObjects.Length)
                    {
                        return ManagedObjects[idx];
                    }
                }
                return null;
            }
        }

        public readonly bool IsString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return type == ValueType.String || (type == ValueType.Object && ObjectValue is string);
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

        public readonly bool IsBoxedNumeric
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ObjectValue switch
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

        public readonly bool UsesObjectID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)type >= (byte)ValueType.String;
        }

        /// <summary>
        /// Free the index of the object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeObject(int index)
        {
            if (index < 0) return;

            var array = ManagedObjects;
            if ((uint)index < (uint)array.Length)
            {
                ref var item = ref array[index];

                if (item != null)
                {
                    item = null;

                    if (freeIndicesCount == FreeIndices.Length)
                    {
                        Array.Resize(ref FreeIndices, FreeIndices.Length * 2);
                    }
                    FreeIndices[freeIndicesCount++] = index;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateObject(int index, object newValue)
        {
            if ((uint)index < (uint)ManagedObjects.Length)
            {
                ManagedObjects[index] = newValue;
            }
        }

        /// <summary>
        /// Unboxes the objectValue to a ref of <typeparamref name="T"/>.
        /// Only use when <see cref="IsBoxed"/> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Unbox<T>() where T : struct => ref Unsafe.Unbox<T>(ObjectValue);

        #region AsMethods
        /// <summary>
        /// Converts the <see cref="ObjectValue"/> to <typeparamref name="T"/>.
        /// Only use when <see cref="IsBoxedNumeric"/> is true.
        /// </summary>
        public readonly T AsNumeric<T>()
        {
            var objectValue = ObjectValue;

            if (objectValue is T exact) return exact;

            if (typeof(T) == typeof(int))
            {
                int val = objectValue switch
                {
                    int i => i,
                    long l => (int)l,
                    double d => (int)d,
                    float f => (int)f,
                    uint ui => (int)ui,
                    ulong ul => (int)ul,
                    short s => (int)s,
                    ushort us => (int)us,
                    byte b => (int)b,
                    sbyte sb => (int)sb,
                    bool bo => bo ? 1 : 0,
                    decimal de => (int)de,
                    _ => objectValue.ConvertTo<int>()
                };
                return Unsafe.As<int, T>(ref val);
            }

            if (typeof(T) == typeof(long))
            {
                long val = objectValue switch
                {
                    int i => (long)i,
                    long l => l,
                    double d => (long)d,
                    float f => (long)f,
                    uint ui => (long)ui,
                    ulong ul => (long)ul,
                    short s => (long)s,
                    ushort us => (long)us,
                    byte b => (long)b,
                    sbyte sb => (long)sb,
                    bool bo => bo ? 1L : 0L,
                    decimal de => (long)de,
                    _ => objectValue.ConvertTo<long>()
                };
                return Unsafe.As<long, T>(ref val);
            }

            if (typeof(T) == typeof(double))
            {
                double val = objectValue switch
                {
                    int i => (double)i,
                    long l => (double)l,
                    double d => d,
                    float f => (double)f,
                    uint ui => (double)ui,
                    ulong ul => (double)ul,
                    short s => (double)s,
                    ushort us => (double)us,
                    byte b => (double)b,
                    sbyte sb => (double)sb,
                    bool bo => bo ? 1.0 : 0.0,
                    decimal de => (double)de,
                    _ => objectValue.ConvertTo<double>()
                };
                return Unsafe.As<double, T>(ref val);
            }

            if (typeof(T) == typeof(float))
            {
                float val = objectValue switch
                {
                    int i => (float)i,
                    long l => (float)l,
                    double d => (float)d,
                    float f => f,
                    uint ui => (float)ui,
                    ulong ul => (float)ul,
                    short s => (float)s,
                    ushort us => (float)us,
                    byte b => (float)b,
                    sbyte sb => (float)sb,
                    bool bo => bo ? 1.0f : 0.0f,
                    decimal de => (float)de,
                    _ => objectValue.ConvertTo<float>()
                };
                return Unsafe.As<float, T>(ref val);
            }

            if (typeof(T) == typeof(uint))
            {
                uint val = objectValue switch
                {
                    int i => (uint)i,
                    long l => (uint)l,
                    double d => (uint)d,
                    float f => (uint)f,
                    uint ui => ui,
                    ulong ul => (uint)ul,
                    short s => (uint)s,
                    ushort us => (uint)us,
                    byte b => (uint)b,
                    sbyte sb => (uint)sb,
                    bool bo => bo ? 1u : 0u,
                    decimal de => (uint)de,
                    _ => objectValue.ConvertTo<uint>()
                };
                return Unsafe.As<uint, T>(ref val);
            }

            if (typeof(T) == typeof(ulong))
            {
                ulong val = objectValue switch
                {
                    int i => (ulong)i,
                    long l => (ulong)l,
                    double d => (ulong)d,
                    float f => (ulong)f,
                    uint ui => (ulong)ui,
                    ulong ul => ul,
                    short s => (ulong)s,
                    ushort us => (ulong)us,
                    byte b => (ulong)b,
                    sbyte sb => (ulong)sb,
                    bool bo => bo ? 1ul : 0ul,
                    decimal de => (ulong)de,
                    _ => objectValue.ConvertTo<ulong>()
                };
                return Unsafe.As<ulong, T>(ref val);
            }

            if (typeof(T) == typeof(short))
            {
                short val = objectValue switch
                {
                    int i => (short)i,
                    long l => (short)l,
                    double d => (short)d,
                    float f => (short)f,
                    uint ui => (short)ui,
                    ulong ul => (short)ul,
                    short s => s,
                    ushort us => (short)us,
                    byte b => (short)b,
                    sbyte sb => (short)sb,
                    bool bo => (short)(bo ? 1 : 0),
                    decimal de => (short)de,
                    _ => objectValue.ConvertTo<short>()
                };
                return Unsafe.As<short, T>(ref val);
            }

            if (typeof(T) == typeof(ushort))
            {
                ushort val = objectValue switch
                {
                    int i => (ushort)i,
                    long l => (ushort)l,
                    double d => (ushort)d,
                    float f => (ushort)f,
                    uint ui => (ushort)ui,
                    ulong ul => (ushort)ul,
                    short s => (ushort)s,
                    ushort us => us,
                    byte b => (ushort)b,
                    sbyte sb => (ushort)sb,
                    bool bo => (ushort)(bo ? 1 : 0),
                    decimal de => (ushort)de,
                    _ => objectValue.ConvertTo<ushort>()
                };
                return Unsafe.As<ushort, T>(ref val);
            }

            if (typeof(T) == typeof(byte))
            {
                byte val = objectValue switch
                {
                    int i => (byte)i,
                    long l => (byte)l,
                    double d => (byte)d,
                    float f => (byte)f,
                    uint ui => (byte)ui,
                    ulong ul => (byte)ul,
                    short s => (byte)s,
                    ushort us => (byte)us,
                    byte b => b,
                    sbyte sb => (byte)sb,
                    bool bo => (byte)(bo ? 1 : 0),
                    decimal de => (byte)de,
                    _ => objectValue.ConvertTo<byte>()
                };
                return Unsafe.As<byte, T>(ref val);
            }

            if (typeof(T) == typeof(sbyte))
            {
                sbyte val = objectValue switch
                {
                    int i => (sbyte)i,
                    long l => (sbyte)l,
                    double d => (sbyte)d,
                    float f => (sbyte)f,
                    uint ui => (sbyte)ui,
                    ulong ul => (sbyte)ul,
                    short s => (sbyte)s,
                    ushort us => (sbyte)us,
                    byte b => (sbyte)b,
                    sbyte sb => sb,
                    bool bo => (sbyte)(bo ? 1 : 0),
                    decimal de => (sbyte)de,
                    _ => objectValue.ConvertTo<sbyte>()
                };
                return Unsafe.As<sbyte, T>(ref val);
            }

            if (typeof(T) == typeof(decimal))
            {
                decimal val = objectValue switch
                {
                    int i => (decimal)i,
                    long l => l,
                    double d => (decimal)d,
                    float f => (decimal)f,
                    uint ui => (decimal)ui,
                    ulong ul => (decimal)ul,
                    short s => (decimal)s,
                    ushort us => (decimal)us,
                    byte b => (decimal)b,
                    sbyte sb => (decimal)sb,
                    bool bo => bo ? 1m : 0m,
                    decimal de => de,
                    _ => objectValue.ConvertTo<decimal>()
                };
                return Unsafe.As<decimal, T>(ref val);
            }

            // Ultimate fallback for custom struct numeric wrappers
            return (T)System.Convert.ChangeType(objectValue, typeof(T));
        }

        /// <summary>
        /// Converts the underlying math struct to the requested vector <typeparamref name="T"/>.
        /// </summary>
        public readonly T AsVector<T>() where T : unmanaged
        {
            if (type == ValueType.Object)
            {
                if (ObjectValue is T exact) return exact;
            }

            Vector4 intermediate;
            switch (type)
            {
                case ValueType.Vector4:
                case ValueType.Vector3:
                case ValueType.Vector2:
                case ValueType.Color:
                case ValueType.Quaternion:
                    intermediate = vector4Value;
                    break;
                case ValueType.Object:
                    var obj = ObjectValue;
                    if (obj is Vector4 v4) intermediate = v4;
                    else if (obj is Vector3 v3) intermediate = new Vector4(v3.x, v3.y, v3.z, 0);
                    else if (obj is Vector2 v2) intermediate = new Vector4(v2.x, v2.y, 0, 0);
                    else if (obj is Color c) intermediate = (Vector4)c;
                    else if (obj is Quaternion q) intermediate = new Vector4(q.x, q.y, q.z, q.w);
                    else return obj.ConvertTo<T>();
                    break;
                default: return ThrowInvalidCast<T>();
            }

            if (typeof(T) == typeof(Vector4)) return Unsafe.As<Vector4, T>(ref intermediate);
            if (typeof(T) == typeof(Vector3)) return Unsafe.As<Vector4, T>(ref intermediate);
            if (typeof(T) == typeof(Vector2)) return Unsafe.As<Vector4, T>(ref intermediate);
            if (typeof(T) == typeof(Color)) return Unsafe.As<Vector4, T>(ref intermediate);
            if (typeof(T) == typeof(Quaternion)) return Unsafe.As<Vector4, T>(ref intermediate);

            return ThrowInvalidCast<T>();
        }

        #endregion

        #region Comparison

        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly bool IsAssignableFrom(Type type)
        {
            if (type == typeof(object))
            {
                return true;
            }

            switch (this.type)
            {
                case ValueType.Int:
                    return ConversionUtility.HasNumericConversion(typeof(int), type) || ConversionUtility.CanConvert(typeof(int), type, true);
                case ValueType.Float:
                    return ConversionUtility.HasNumericConversion(typeof(float), type) || ConversionUtility.CanConvert(typeof(float), type, true);
                case ValueType.Double:
                    return ConversionUtility.HasNumericConversion(typeof(double), type) || ConversionUtility.CanConvert(typeof(double), type, true);
                case ValueType.Byte:
                    return ConversionUtility.HasNumericConversion(typeof(byte), type) || ConversionUtility.CanConvert(typeof(byte), type, true);
                case ValueType.SByte:
                    return ConversionUtility.HasNumericConversion(typeof(sbyte), type) || ConversionUtility.CanConvert(typeof(sbyte), type, true);
                case ValueType.Short:
                    return ConversionUtility.HasNumericConversion(typeof(short), type) || ConversionUtility.CanConvert(typeof(short), type, true);
                case ValueType.UShort:
                    return ConversionUtility.HasNumericConversion(typeof(ushort), type) || ConversionUtility.CanConvert(typeof(ushort), type, true);
                case ValueType.UInt:
                    return ConversionUtility.HasNumericConversion(typeof(uint), type) || ConversionUtility.CanConvert(typeof(uint), type, true);
                case ValueType.Long:
                    return ConversionUtility.HasNumericConversion(typeof(long), type) || ConversionUtility.CanConvert(typeof(long), type, true);
                case ValueType.ULong:
                    return ConversionUtility.HasNumericConversion(typeof(ulong), type) || ConversionUtility.CanConvert(typeof(ulong), type, true);

                case ValueType.Bool:
                    return type == typeof(bool) || ConversionUtility.CanConvert(typeof(bool), type, true);

                case ValueType.Vector2:
                    return IsVectorAssignable(type, typeof(Vector2));
                case ValueType.Vector3:
                    return IsVectorAssignable(type, typeof(Vector3));
                case ValueType.Vector4:
                    return IsVectorAssignable(type, typeof(Vector4));

                case ValueType.Color:
                    return type == typeof(Color) || type == typeof(Vector4) || ConversionUtility.CanConvert(typeof(Color), type, true);

                case ValueType.Quaternion:
                    return type == typeof(Quaternion) || ConversionUtility.CanConvert(typeof(Quaternion), type, true);

                case ValueType.String:
                    return type == typeof(string) || ConversionUtility.CanConvert(typeof(string), type, true);

                case ValueType.Object:
                    var objectValue = ObjectValue;

                    if (objectValue == null)
                    {
                        return IsNullable(type);
                    }

                    return objectValue.IsConvertibleTo(type, true);

                case ValueType.None:
                    return IsNullable(type);

                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNullable(Type t)
        {
            return !t.IsValueType || (Nullable.GetUnderlyingType(t) != null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsAssignableFrom<T>()
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(float) ||
                typeof(T) == typeof(long) || typeof(T) == typeof(double) ||
                typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return ((byte)type >= 1 & (byte)type <= 10) || (type == ValueType.Object && (ObjectValue?.IsConvertibleTo<T>(true) ?? false));
            }

            if (typeof(T) == typeof(Vector3) || typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector4))
            {
                return ((byte)type >= 11 & (byte)type <= 13) || ConversionUtility.CanConvert(GetValueType(), typeof(T), true); // Slower but flexible conversion
            }

            if (typeof(T) == typeof(bool)) return type == ValueType.Bool || ConversionUtility.CanConvert(GetValueType(), typeof(T), true);
            if (typeof(T) == typeof(Color)) return type == ValueType.Color || ConversionUtility.CanConvert(GetValueType(), typeof(T), true);
            if (typeof(T) == typeof(Quaternion)) return type == ValueType.Quaternion || ConversionUtility.CanConvert(GetValueType(), typeof(T), true);
            if (typeof(T) == typeof(string)) return type == ValueType.String || ConversionUtility.CanConvert(GetValueType(), typeof(T), true);

            if (typeof(T) == typeof(object)) return true;

            return IsAssignableFrom(typeof(T));
        }

        private static bool IsVectorAssignable(Type targetType, Type sourceType)
        {
            if (targetType == typeof(Vector3)) return true;
            if (targetType == typeof(Vector2)) return true;
            if (targetType == typeof(Vector4)) return true;

            if (targetType == typeof(object)) return true;

            return ConversionUtility.CanConvert(sourceType, targetType, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsNull() => (type == (byte)ValueType.None) | ((byte)type >= (byte)ValueType.String & objectID == -1);

        #endregion

        #region Cast Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T Cast<T>()
        {
            if (typeof(T) == typeof(int))
            {
                if (type == ValueType.Int) return Unsafe.As<int, T>(ref Unsafe.AsRef(in intValue));
                int v = ToInt32(); return Unsafe.As<int, T>(ref v);
            }

            if (typeof(T) == typeof(float))
            {
                if (type == ValueType.Float) return Unsafe.As<float, T>(ref Unsafe.AsRef(in floatValue));
                float v = ToSingle(); return Unsafe.As<float, T>(ref v);
            }

            if (typeof(T) == typeof(bool))
            {
                if (type == ValueType.Bool) return Unsafe.As<bool, T>(ref Unsafe.AsRef(in boolValue));
                bool v = ToBool(); return Unsafe.As<bool, T>(ref v);
            }

            if (typeof(T) == typeof(Vector2))
            {
                if (type == ValueType.Vector2) return Unsafe.As<Vector2, T>(ref Unsafe.AsRef(in vector2Value));
                Vector2 v = AsVector<Vector2>(); return Unsafe.As<Vector2, T>(ref v);
            }

            if (typeof(T) == typeof(Vector3))
            {
                if (type == ValueType.Vector3) return Unsafe.As<Vector3, T>(ref Unsafe.AsRef(in vector3Value));
                Vector3 v = AsVector<Vector3>(); return Unsafe.As<Vector3, T>(ref v);
            }

            if (typeof(T) == typeof(string))
            {
                if (type == ValueType.String)
                {
                    object val = ObjectValue;
                    return Unsafe.As<object, T>(ref val);
                }

                return (T)(object)ToString();
            }

            if (typeof(T) == typeof(Vector4))
            {
                if (type == ValueType.Vector4) return Unsafe.As<Vector4, T>(ref Unsafe.AsRef(in vector4Value));
                Vector4 v = AsVector<Vector4>(); return Unsafe.As<Vector4, T>(ref v);
            }

            if (typeof(T) == typeof(Color))
            {
                if (type == ValueType.Color) return Unsafe.As<Color, T>(ref Unsafe.AsRef(in colorValue));
                Color v = AsVector<Color>(); return Unsafe.As<Color, T>(ref v);
            }

            if (typeof(T) == typeof(Quaternion))
            {
                if (type == ValueType.Quaternion) return Unsafe.As<Quaternion, T>(ref Unsafe.AsRef(in quaternionValue));
                Quaternion v = AsVector<Quaternion>(); return Unsafe.As<Quaternion, T>(ref v);
            }

            if (typeof(T) == typeof(long))
            {
                if (type == ValueType.Long) return Unsafe.As<long, T>(ref Unsafe.AsRef(in longValue));
                long v = ToInt64(); return Unsafe.As<long, T>(ref v);
            }

            if (typeof(T) == typeof(double))
            {
                if (type == ValueType.Double) return Unsafe.As<double, T>(ref Unsafe.AsRef(in doubleValue));
                double v = ToDouble(); return Unsafe.As<double, T>(ref v);
            }

            if (typeof(T) == typeof(byte))
            {
                if (type == ValueType.Byte) return Unsafe.As<byte, T>(ref Unsafe.AsRef(in byteValue));
                byte v = ToByte(); return Unsafe.As<byte, T>(ref v);
            }

            if (typeof(T) == typeof(uint))
            {
                if (type == ValueType.UInt) return Unsafe.As<uint, T>(ref Unsafe.AsRef(in uintValue));
                uint v = ToUInt32(); return Unsafe.As<uint, T>(ref v);
            }

            if (typeof(T) == typeof(ulong))
            {
                if (type == ValueType.ULong) return Unsafe.As<ulong, T>(ref Unsafe.AsRef(in ulongValue));
                ulong v = ToUInt64(); return Unsafe.As<ulong, T>(ref v);
            }

            if (typeof(T) == typeof(short))
            {
                if (type == ValueType.Short) return Unsafe.As<short, T>(ref Unsafe.AsRef(in shortValue));
                short v = ToInt16(); return Unsafe.As<short, T>(ref v);
            }

            if (typeof(T) == typeof(ushort))
            {
                if (type == ValueType.UShort) return Unsafe.As<ushort, T>(ref Unsafe.AsRef(in ushortValue));
                ushort v = ToUInt16(); return Unsafe.As<ushort, T>(ref v);
            }

            if (typeof(T) == typeof(sbyte))
            {
                if (type == ValueType.SByte) return Unsafe.As<sbyte, T>(ref Unsafe.AsRef(in sbyteValue));
                sbyte v = ToSByte(); return Unsafe.As<sbyte, T>(ref v);
            }

            if (typeof(T) == typeof(object))
            {
                return (T)ToObject();
            }

            return CastObject<T>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly T ThrowInvalidCast<T>() => throw new InvalidCastException($"Cannot convert {type} to {typeof(T).FullName}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly T CastObject<T>()
        {
            var obj = ObjectValue;

            if (obj is T value) return value;
            if (obj is null) return default;

            if (IsNumeric<T>())
            {
                if (obj is IConvertible)
                {
                    return AsNumeric<T>();
                }
            }
            return Convert<T>(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric<T>()
        {
            return typeof(T) == typeof(int) ||
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(long) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(decimal);
        }

        private static T Convert<T>(object obj) => ConversionUtility.Convert<T>(obj);

        private readonly byte ToByte() => type switch
        {
            ValueType.Byte => byteValue,
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

        private readonly sbyte ToSByte() => type switch
        {
            ValueType.Byte => (sbyte)byteValue,
            ValueType.SByte => sbyteValue,
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

        private readonly short ToInt16() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
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

        private readonly ushort ToUInt16() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (ushort)sbyteValue,
            ValueType.Short => (ushort)shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => (ushort)intValue,
            ValueType.UInt => (ushort)uintValue,
            ValueType.Long => (ushort)longValue,
            ValueType.ULong => (ushort)ulongValue,
            ValueType.Float => (ushort)floatValue,
            ValueType.Double => (ushort)doubleValue,
            ValueType.Object => AsNumeric<ushort>(),
            _ => ThrowInvalidCast<ushort>()
        };

        private readonly int ToInt32() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => (int)uintValue,
            ValueType.Long => (int)longValue,
            ValueType.ULong => (int)ulongValue,
            ValueType.Float => (int)floatValue,
            ValueType.Double => (int)doubleValue,
            ValueType.Object => AsNumeric<int>(),
            _ => ThrowInvalidCast<int>()
        };

        private readonly uint ToUInt32() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (uint)sbyteValue,
            ValueType.Short => (uint)shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => (uint)intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => (uint)longValue,
            ValueType.ULong => (uint)ulongValue,
            ValueType.Float => (uint)floatValue,
            ValueType.Double => (uint)doubleValue,
            ValueType.Object => AsNumeric<uint>(),
            _ => ThrowInvalidCast<uint>()
        };

        private readonly long ToInt64() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => sbyteValue,
            ValueType.Short => shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => longValue,
            ValueType.ULong => (long)ulongValue,
            ValueType.Float => (long)floatValue,
            ValueType.Double => (long)doubleValue,
            ValueType.Object => AsNumeric<long>(),
            _ => ThrowInvalidCast<long>()
        };

        private readonly ulong ToUInt64() => type switch
        {
            ValueType.Byte => byteValue,
            ValueType.SByte => (ulong)sbyteValue,
            ValueType.Short => (ulong)shortValue,
            ValueType.UShort => ushortValue,
            ValueType.Int => (ulong)intValue,
            ValueType.UInt => uintValue,
            ValueType.Long => (ulong)longValue,
            ValueType.ULong => ulongValue,
            ValueType.Float => (ulong)floatValue,
            ValueType.Double => (ulong)doubleValue,
            ValueType.Object => AsNumeric<ulong>(),
            _ => ThrowInvalidCast<ulong>()
        };

        private readonly float ToSingle() => type switch
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
            ValueType.Double => (float)doubleValue,
            ValueType.Object => AsNumeric<float>(),
            _ => ThrowInvalidCast<float>()
        };

        private readonly double ToDouble() => type switch
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
            ValueType.Object => AsNumeric<double>(),
            _ => ThrowInvalidCast<double>()
        };

        private readonly bool ToBool() => type switch
        {
            ValueType.Bool => boolValue,
            ValueType.Byte => byteValue != 0,
            ValueType.SByte => sbyteValue != 0,
            ValueType.Short => shortValue != 0,
            ValueType.UShort => ushortValue != 0,
            ValueType.Int => intValue != 0,
            ValueType.UInt => uintValue != 0,
            ValueType.Long => longValue != 0,
            ValueType.ULong => ulongValue != 0,
            ValueType.Float => floatValue != 0.0f,
            ValueType.Double => doubleValue != 0.0,
            ValueType.Object => Convert<bool>(ObjectValue),
            _ => ThrowInvalidCast<bool>()
        };
        #endregion

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

        public readonly Type GetValueType() => type switch
        {
            ValueType.None => null,
            ValueType.Byte => typeof(byte),
            ValueType.SByte => typeof(sbyte),
            ValueType.Short => typeof(short),
            ValueType.UShort => typeof(ushort),
            ValueType.Int => typeof(int),
            ValueType.UInt => typeof(uint),
            ValueType.Long => typeof(long),
            ValueType.ULong => typeof(ulong),
            ValueType.Float => typeof(float),
            ValueType.Double => typeof(double),
            ValueType.Bool => typeof(bool),
            ValueType.Vector2 => typeof(Vector2),
            ValueType.Vector3 => typeof(Vector3),
            ValueType.Vector4 => typeof(Vector4),
            ValueType.Quaternion => typeof(Quaternion),
            ValueType.Color => typeof(Color),
            ValueType.String => typeof(string),
            ValueType.Object => ObjectValue?.GetType(),
            _ => null
        };

        #region Create

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(byte value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Byte;
            byteValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(sbyte value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.SByte;
            sbyteValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(short value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Short;
            shortValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(ushort value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.UShort;
            ushortValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(int value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Int;
            intValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(uint value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.UInt;
            uintValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(long value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Long;
            longValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(ulong value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.ULong;
            ulongValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(float value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Float;
            floatValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(double value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Double;
            doubleValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(bool value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Bool;
            boolValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector2 value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Vector2;
            vector2Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector3 value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Vector3;
            vector3Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Vector4 value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Vector4;
            vector4Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Quaternion value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Quaternion;
            quaternionValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(Color value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Color;
            colorValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(string value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.String;
            objectID = AllocateObject(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(string value, out int handle)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.String;
            handle = AllocateObject(value);
            objectID = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(object value)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            objectID = AllocateObject(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue(object value, out int handle)
        {
            Unsafe.SkipInit(out this);
            type = ValueType.Object;
            handle = AllocateObject(value);
            objectID = handle;
        }

        private static int AllocateObject(object value)
        {
            if (value == null) return -1;

            if (freeIndicesCount > 0)
            {
                int recycledIndex = FreeIndices[--freeIndicesCount];
                ManagedObjects[recycledIndex] = value;
                return recycledIndex;
            }

            var length = ManagedObjects.Length;

            if (totalAllocatedCount >= length)
            {
                Array.Resize(ref ManagedObjects, length * 2);
            }

            int newIndex = totalAllocatedCount++;
            ManagedObjects[newIndex] = value;
            return newIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParameterValue Create<T>(T value)
        {
            if (typeof(T) == typeof(int))
            {
                return new ParameterValue(Unsafe.As<T, int>(ref value));
            }
            if (typeof(T) == typeof(uint))
            {
                return new ParameterValue(Unsafe.As<T, uint>(ref value));
            }
            if (typeof(T) == typeof(long))
            {
                return new ParameterValue(Unsafe.As<T, long>(ref value));
            }
            if (typeof(T) == typeof(ulong))
            {
                return new ParameterValue(Unsafe.As<T, ulong>(ref value));
            }

            if (typeof(T) == typeof(float))
            {
                return new ParameterValue(Unsafe.As<T, float>(ref value));
            }
            if (typeof(T) == typeof(double))
            {
                return new ParameterValue(Unsafe.As<T, double>(ref value));
            }

            if (typeof(T) == typeof(bool))
            {
                return new ParameterValue(Unsafe.As<T, bool>(ref value));
            }
            if (typeof(T) == typeof(byte))
            {
                return new ParameterValue(Unsafe.As<T, byte>(ref value));
            }
            if (typeof(T) == typeof(sbyte))
            {
                return new ParameterValue(Unsafe.As<T, sbyte>(ref value));
            }
            if (typeof(T) == typeof(short))
            {
                return new ParameterValue(Unsafe.As<T, short>(ref value));
            }
            if (typeof(T) == typeof(ushort))
            {
                return new ParameterValue(Unsafe.As<T, ushort>(ref value));
            }

            if (typeof(T) == typeof(Vector2))
            {
                return new ParameterValue(Unsafe.As<T, Vector2>(ref value));
            }
            if (typeof(T) == typeof(Vector3))
            {
                return new ParameterValue(Unsafe.As<T, Vector3>(ref value));
            }
            if (typeof(T) == typeof(Vector4))
            {
                return new ParameterValue(Unsafe.As<T, Vector4>(ref value));
            }
            if (typeof(T) == typeof(Quaternion))
            {
                return new ParameterValue(Unsafe.As<T, Quaternion>(ref value));
            }
            if (typeof(T) == typeof(Color))
            {
                return new ParameterValue(Unsafe.As<T, Color>(ref value));
            }

            if (typeof(T) == typeof(string))
            {
                return new ParameterValue(Unsafe.As<T, string>(ref value));
            }

            return new ParameterValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(byte value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(sbyte value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(short value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(ushort value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(int value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(uint value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(long value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(ulong value) => new ParameterValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(float value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(double value) => new ParameterValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(bool value) => new ParameterValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(Vector2 value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(Vector3 value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(Vector4 value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(Quaternion value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ParameterValue(Color value) => new ParameterValue(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ParameterValue(string value) => new ParameterValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(ParameterValue value) => value.Cast<byte>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator sbyte(ParameterValue value) => value.Cast<sbyte>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(ParameterValue value) => value.Cast<short>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(ParameterValue value) => value.Cast<ushort>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(ParameterValue value) => value.Cast<int>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(ParameterValue value) => value.Cast<uint>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(ParameterValue value) => value.Cast<long>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(ParameterValue value) => value.Cast<ulong>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(ParameterValue value) => value.Cast<float>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(ParameterValue value) => value.Cast<double>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator decimal(ParameterValue value) => value.Cast<decimal>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(ParameterValue value) => value.Cast<bool>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(ParameterValue value) => value.Cast<Vector2>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(ParameterValue value) => value.Cast<Vector3>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4(ParameterValue value) => value.Cast<Vector4>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quaternion(ParameterValue value) => value.Cast<Quaternion>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color(ParameterValue value) => value.Cast<Color>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(ParameterValue value) => value.Cast<string>();
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
                _ => type == ValueType.Object && ObjectValue.Equals(obj)
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
    }
}
