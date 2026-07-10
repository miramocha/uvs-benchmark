#if !ENABLE_IL2CPP
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    public readonly partial struct ParameterValue
    {
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

            return (T)System.Convert.ChangeType(objectValue, typeof(T));
        }

        /// <summary>
        /// Converts the underlying math struct or boxed vector to the requested vector <typeparamref name="T"/>.
        /// </summary>
        public readonly T AsVector<T>() where T : unmanaged
        {
            if (type == ValueType.Object)
            {
                if (ObjectValue is T exact) return exact;
            }

            if (typeof(T) == typeof(Vector4))
            {
                Vector4 val = ToVector4();
                return Unsafe.As<Vector4, T>(ref val);
            }

            if (typeof(T) == typeof(Vector3))
            {
                Vector3 val = ToVector3();
                return Unsafe.As<Vector3, T>(ref val);
            }

            if (typeof(T) == typeof(Vector2))
            {
                Vector2 val = ToVector2();
                return Unsafe.As<Vector2, T>(ref val);
            }

            if (typeof(T) == typeof(Color))
            {
                Color val = ToColor();
                return Unsafe.As<Color, T>(ref val);
            }

            if (typeof(T) == typeof(Quaternion))
            {
                Quaternion val = ToQuaternion();
                return Unsafe.As<Quaternion, T>(ref val);
            }

            if (type == ValueType.Object)
            {
                return ObjectValue.ConvertTo<T>();
            }

            return ThrowInvalidCast<T>();
        }

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

        private static bool IsNullable(Type t)
        {
            return !t.IsValueType || (Nullable.GetUnderlyingType(t) != null);
        }

        public readonly bool IsAssignableFrom<T>()
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(float) ||
                typeof(T) == typeof(long) || typeof(T) == typeof(double) ||
                typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
            {
                return (type >= ValueType.Byte && type <= ValueType.Double) || (type == ValueType.Object && (ObjectValue?.IsConvertibleTo<T>(true) ?? false));
            }

            if (typeof(T) == typeof(Vector3) || typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector4))
            {
                return (type >= ValueType.Vector2 && type <= ValueType.Vector4) || ConversionUtility.CanConvert(GetValueType(), typeof(T), true); // Slower but flexible conversion
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
        public readonly T Cast<T>()
        {
            if (typeof(T) == typeof(object))
            {
                return (T)ToObject();
            }

            if (typeof(T) == typeof(int))
            {
                if (type == ValueType.Int) return Unsafe.As<int, T>(ref Unsafe.AsRef(in intValue));
                int v = CoerceToInt32(); return Unsafe.As<int, T>(ref v);
            }

            if (typeof(T) == typeof(float))
            {
                if (type == ValueType.Float) return Unsafe.As<float, T>(ref Unsafe.AsRef(in floatValue));
                float v = CoerceToSingle(); return Unsafe.As<float, T>(ref v);
            }

            if (typeof(T) == typeof(bool))
            {
                if (type == ValueType.Bool) return Unsafe.As<bool, T>(ref Unsafe.AsRef(in boolValue));
                bool v = CoerceToBool(); return Unsafe.As<bool, T>(ref v);
            }

            if (typeof(T) == typeof(Vector2))
            {
                if (type == ValueType.Vector2) return Unsafe.As<Vector2, T>(ref Unsafe.AsRef(in vector2Value));
                Vector2 v = CoerceToVector2(); return Unsafe.As<Vector2, T>(ref v);
            }

            if (typeof(T) == typeof(Vector3))
            {
                if (type == ValueType.Vector3) return Unsafe.As<Vector3, T>(ref Unsafe.AsRef(in vector3Value));
                Vector3 v = CoerceToVector3(); return Unsafe.As<Vector3, T>(ref v);
            }

            if (typeof(T) == typeof(string))
            {
                if (type == ValueType.String)
                {
                    object val = ObjectValue;
                    return Unsafe.As<object, T>(ref val);
                }

                string v = ToString();
                return Unsafe.As<string, T>(ref v);
            }

            if (typeof(T) == typeof(Vector4))
            {
                if (type == ValueType.Vector4) return Unsafe.As<Vector4, T>(ref Unsafe.AsRef(in vector4Value));
                Vector4 v = CoerceToVector4(); return Unsafe.As<Vector4, T>(ref v);
            }

            if (typeof(T) == typeof(Color))
            {
                if (type == ValueType.Color) return Unsafe.As<Color, T>(ref Unsafe.AsRef(in colorValue));
                Color v = CoerceToColor(); return Unsafe.As<Color, T>(ref v);
            }

            if (typeof(T) == typeof(Quaternion))
            {
                if (type == ValueType.Quaternion) return Unsafe.As<Quaternion, T>(ref Unsafe.AsRef(in quaternionValue));
                Quaternion v = CoerceToQuaternion(); return Unsafe.As<Quaternion, T>(ref v);
            }

            if (typeof(T) == typeof(long))
            {
                if (type == ValueType.Long) return Unsafe.As<long, T>(ref Unsafe.AsRef(in longValue));
                long v = CoerceToInt64(); return Unsafe.As<long, T>(ref v);
            }

            if (typeof(T) == typeof(double))
            {
                if (type == ValueType.Double) return Unsafe.As<double, T>(ref Unsafe.AsRef(in doubleValue));
                double v = CoerceToDouble(); return Unsafe.As<double, T>(ref v);
            }

            if (typeof(T) == typeof(byte))
            {
                if (type == ValueType.Byte) return Unsafe.As<byte, T>(ref Unsafe.AsRef(in byteValue));
                byte v = CoerceToByte(); return Unsafe.As<byte, T>(ref v);
            }

            if (typeof(T) == typeof(uint))
            {
                if (type == ValueType.UInt) return Unsafe.As<uint, T>(ref Unsafe.AsRef(in uintValue));
                uint v = CoerceToUInt32(); return Unsafe.As<uint, T>(ref v);
            }

            if (typeof(T) == typeof(ulong))
            {
                if (type == ValueType.ULong) return Unsafe.As<ulong, T>(ref Unsafe.AsRef(in ulongValue));
                ulong v = CoerceToUInt64(); return Unsafe.As<ulong, T>(ref v);
            }

            if (typeof(T) == typeof(short))
            {
                if (type == ValueType.Short) return Unsafe.As<short, T>(ref Unsafe.AsRef(in shortValue));
                short v = CoerceToInt16(); return Unsafe.As<short, T>(ref v);
            }

            if (typeof(T) == typeof(ushort))
            {
                if (type == ValueType.UShort) return Unsafe.As<ushort, T>(ref Unsafe.AsRef(in ushortValue));
                ushort v = CoerceToUInt16(); return Unsafe.As<ushort, T>(ref v);
            }

            if (typeof(T) == typeof(sbyte))
            {
                if (type == ValueType.SByte) return Unsafe.As<sbyte, T>(ref Unsafe.AsRef(in sbyteValue));
                sbyte v = CoerceToSByte(); return Unsafe.As<sbyte, T>(ref v);
            }

            return CastObject<T>();
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
    }
}
#endif
