#if ENABLE_IL2CPP
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T AsNumeric<T>()
        {
            var objectValue = ObjectValue;

            if (objectValue is T exact) return exact;

            if (TypeTraits<T>.IsInt)
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

            if (TypeTraits<T>.IsLong)
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

            if (TypeTraits<T>.IsDouble)
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

            if (TypeTraits<T>.IsFloat)
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

            if (TypeTraits<T>.IsUInt)
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

            if (TypeTraits<T>.IsULong)
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

            if (TypeTraits<T>.IsShort)
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

            if (TypeTraits<T>.IsUShort)
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

            if (TypeTraits<T>.IsByte)
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

            if (TypeTraits<T>.IsSByte)
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

            if (TypeTraits<T>.IsDecimal)
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

            return (T)System.Convert.ChangeType(objectValue, TypeTraits<T>.Type);
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

            if (TypeTraits<T>.IsVector4)
            {
                Vector4 val = ToVector4();
                return Unsafe.As<Vector4, T>(ref val);
            }

            if (TypeTraits<T>.IsVector3)
            {
                Vector3 val = ToVector3();
                return Unsafe.As<Vector3, T>(ref val);
            }

            if (TypeTraits<T>.IsVector2)
            {
                Vector2 val = ToVector2();
                return Unsafe.As<Vector2, T>(ref val);
            }

            if (TypeTraits<T>.IsColor)
            {
                Color val = ToColor();
                return Unsafe.As<Color, T>(ref val);
            }

            if (TypeTraits<T>.IsQuaternion)
            {
                Quaternion val = ToQuaternion();
                return Unsafe.As<Quaternion, T>(ref val);
            }

            if (TypeTraits<T>.IsVector2Int)
            {
                Vector2Int val = ToVector2Int();
                return Unsafe.As<Vector2Int, T>(ref val);
            }

            if (TypeTraits<T>.IsVector3Int)
            {
                Vector3Int val = ToVector3Int();
                return Unsafe.As<Vector3Int, T>(ref val);
            }

            if (TypeTraits<T>.IsRect)
            {
                Rect val = ToRect();
                return Unsafe.As<Rect, T>(ref val);
            }

            if (TypeTraits<T>.IsRay2D)
            {
                Ray2D val = ToRay2D();
                return Unsafe.As<Ray2D, T>(ref val);
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
            if (type == Types.Object)
            {
                return true;
            }

            switch (this.type)
            {
                case ValueType.Int:
                    return ConversionUtility.HasNumericConversion(Types.Int, type) || ConversionUtility.CanConvert(Types.Int, type, true);
                case ValueType.Float:
                    return ConversionUtility.HasNumericConversion(Types.Float, type) || ConversionUtility.CanConvert(Types.Float, type, true);
                case ValueType.Double:
                    return ConversionUtility.HasNumericConversion(Types.Double, type) || ConversionUtility.CanConvert(Types.Double, type, true);
                case ValueType.Byte:
                    return ConversionUtility.HasNumericConversion(Types.Byte, type) || ConversionUtility.CanConvert(Types.Byte, type, true);
                case ValueType.SByte:
                    return ConversionUtility.HasNumericConversion(Types.SByte, type) || ConversionUtility.CanConvert(Types.SByte, type, true);
                case ValueType.Short:
                    return ConversionUtility.HasNumericConversion(Types.Short, type) || ConversionUtility.CanConvert(Types.Short, type, true);
                case ValueType.UShort:
                    return ConversionUtility.HasNumericConversion(Types.UShort, type) || ConversionUtility.CanConvert(Types.UShort, type, true);
                case ValueType.UInt:
                    return ConversionUtility.HasNumericConversion(Types.UInt, type) || ConversionUtility.CanConvert(Types.UInt, type, true);
                case ValueType.Long:
                    return ConversionUtility.HasNumericConversion(Types.Long, type) || ConversionUtility.CanConvert(Types.Long, type, true);
                case ValueType.ULong:
                    return ConversionUtility.HasNumericConversion(Types.ULong, type) || ConversionUtility.CanConvert(Types.ULong, type, true);

                case ValueType.Bool:
                    return type == Types.Bool || ConversionUtility.CanConvert(Types.Bool, type, true);

                case ValueType.Vector2: return IsVectorAssignable(type, Types.Vector2);
                case ValueType.Vector3: return IsVectorAssignable(type, Types.Vector3);
                case ValueType.Vector4: return IsVectorAssignable(type, Types.Vector4);
                case ValueType.Vector2Int: return IsVectorAssignable(type, Types.Vector2Int);
                case ValueType.Vector3Int: return IsVectorAssignable(type, Types.Vector3Int);
                case ValueType.Rect: return IsVectorAssignable(type, Types.Rect);
                case ValueType.Ray2D: return IsVectorAssignable(type, Types.Ray2D);

                case ValueType.Color:
                    return type == Types.Color || type == Types.Vector4 || ConversionUtility.CanConvert(Types.Color, type, true);

                case ValueType.Quaternion:
                    return type == Types.Quaternion || ConversionUtility.CanConvert(Types.Quaternion, type, true);

                case ValueType.String:
                    return type == Types.String || ConversionUtility.CanConvert(Types.String, type, true);

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
            if (TypeTraits<T>.IsNumeric)
            {
                return (type >= ValueType.Byte && type <= ValueType.Decimal) || (type == ValueType.Object && (ObjectValue?.IsConvertibleTo<T>(true) ?? false));
            }

            // Updated Vector/Math block
            if (TypeTraits<T>.IsVector3 || TypeTraits<T>.IsVector2 || TypeTraits<T>.IsVector4 ||
                TypeTraits<T>.IsVector2Int || TypeTraits<T>.IsVector3Int || TypeTraits<T>.IsRect || TypeTraits<T>.IsRay2D)
            {
                return (type >= ValueType.Vector2 && type <= ValueType.Ray2D) || ConversionUtility.CanConvert(GetValueType(), TypeTraits<T>.Type, true);
            }

            if (TypeTraits<T>.IsBool) return type == ValueType.Bool || ConversionUtility.CanConvert(GetValueType(), TypeTraits<T>.Type, true);
            if (TypeTraits<T>.IsColor) return type == ValueType.Color || ConversionUtility.CanConvert(GetValueType(), TypeTraits<T>.Type, true);
            if (TypeTraits<T>.IsQuaternion) return type == ValueType.Quaternion || ConversionUtility.CanConvert(GetValueType(), TypeTraits<T>.Type, true);
            if (TypeTraits<T>.IsString) return type == ValueType.String || ConversionUtility.CanConvert(GetValueType(), TypeTraits<T>.Type, true);

            if (TypeTraits<T>.IsObject) return true;

            return IsAssignableFrom(TypeTraits<T>.Type);
        }

        private static bool IsVectorAssignable(Type targetType, Type sourceType)
        {
            if (targetType == Types.Vector3) return true;
            if (targetType == Types.Vector2) return true;
            if (targetType == Types.Vector4) return true;
            if (targetType == Types.Object) return true;

            return ConversionUtility.CanConvert(sourceType, targetType, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T Cast<T>()
        {
            if (TypeTraits<T>.IsObject)
            {
                return (T)ToObject();
            }

            if (TypeTraits<T>.IsInt)
            {
                int v = ToInt32();
                return Unsafe.As<int, T>(ref v);
            }

            if (TypeTraits<T>.IsFloat)
            {
                float v = ToSingle();
                return Unsafe.As<float, T>(ref v);
            }

            if (TypeTraits<T>.IsBool)
            {
                bool v = ToBool();
                return Unsafe.As<bool, T>(ref v);
            }

            if (TypeTraits<T>.IsVector2)
            {
                Vector2 v = ToVector2();
                return Unsafe.As<Vector2, T>(ref v);
            }

            if (TypeTraits<T>.IsVector3)
            {
                Vector3 v = ToVector3();
                return Unsafe.As<Vector3, T>(ref v);
            }

            if (TypeTraits<T>.IsString)
            {
                if (type == ValueType.String)
                {
                    object val = ObjectValue;
                    return Unsafe.As<object, T>(ref val);
                }

                string v = ToString();
                return Unsafe.As<string, T>(ref v);
            }

            if (TypeTraits<T>.IsVector4)
            {
                Vector4 v = ToVector4();
                return Unsafe.As<Vector4, T>(ref v);
            }

            if (TypeTraits<T>.IsColor)
            {
                Color v = ToColor();
                return Unsafe.As<Color, T>(ref v);
            }

            if (TypeTraits<T>.IsQuaternion)
            {
                Quaternion v = ToQuaternion();
                return Unsafe.As<Quaternion, T>(ref v);
            }

            if (TypeTraits<T>.IsLong)
            {
                long v = ToInt64();
                return Unsafe.As<long, T>(ref v);
            }

            if (TypeTraits<T>.IsDouble)
            {
                double v = ToDouble();
                return Unsafe.As<double, T>(ref v);
            }

            if (TypeTraits<T>.IsDecimal)
            {
                decimal v = ToDecimal();
                return Unsafe.As<decimal, T>(ref v);
            }

            if (TypeTraits<T>.IsByte)
            {
                byte v = ToByte();
                return Unsafe.As<byte, T>(ref v);
            }

            if (TypeTraits<T>.IsUInt)
            {
                uint v = ToUInt32();
                return Unsafe.As<uint, T>(ref v);
            }

            if (TypeTraits<T>.IsULong)
            {
                ulong v = ToUInt64();
                return Unsafe.As<ulong, T>(ref v);
            }

            if (TypeTraits<T>.IsShort)
            {
                short v = ToInt16();
                return Unsafe.As<short, T>(ref v);
            }

            if (TypeTraits<T>.IsUShort)
            {
                ushort v = ToUInt16();
                return Unsafe.As<ushort, T>(ref v);
            }

            if (TypeTraits<T>.IsSByte)
            {
                sbyte v = ToSByte();
                return Unsafe.As<sbyte, T>(ref v);
            }

            if (TypeTraits<T>.IsVector2Int)
            {
                Vector2Int v = ToVector2Int();
                return Unsafe.As<Vector2Int, T>(ref v);
            }

            if (TypeTraits<T>.IsVector3Int)
            {
                Vector3Int v = ToVector3Int();
                return Unsafe.As<Vector3Int, T>(ref v);
            }

            if (TypeTraits<T>.IsRect)
            {
                Rect v = ToRect();
                return Unsafe.As<Rect, T>(ref v);
            }

            if (TypeTraits<T>.IsRay2D)
            {
                Ray2D v = ToRay2D();
                return Unsafe.As<Ray2D, T>(ref v);
            }

            return CastObject<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParameterValue Create<T>(T value)
        {
            if (TypeTraits<T>.IsInt)
            {
                return new ParameterValue(Unsafe.As<T, int>(ref value));
            }
            if (TypeTraits<T>.IsUInt)
            {
                return new ParameterValue(Unsafe.As<T, uint>(ref value));
            }
            if (TypeTraits<T>.IsLong)
            {
                return new ParameterValue(Unsafe.As<T, long>(ref value));
            }
            if (TypeTraits<T>.IsULong)
            {
                return new ParameterValue(Unsafe.As<T, ulong>(ref value));
            }
            if (TypeTraits<T>.IsFloat)
            {
                return new ParameterValue(Unsafe.As<T, float>(ref value));
            }
            if (TypeTraits<T>.IsDouble)
            {
                return new ParameterValue(Unsafe.As<T, double>(ref value));
            }
            if (TypeTraits<T>.IsDecimal)
            {
                return new ParameterValue(Unsafe.As<T, decimal>(ref value));
            }
            if (TypeTraits<T>.IsBool)
            {
                return new ParameterValue(Unsafe.As<T, bool>(ref value));
            }
            if (TypeTraits<T>.IsByte)
            {
                return new ParameterValue(Unsafe.As<T, byte>(ref value));
            }
            if (TypeTraits<T>.IsSByte)
            {
                return new ParameterValue(Unsafe.As<T, sbyte>(ref value));
            }
            if (TypeTraits<T>.IsShort)
            {
                return new ParameterValue(Unsafe.As<T, short>(ref value));
            }
            if (TypeTraits<T>.IsUShort)
            {
                return new ParameterValue(Unsafe.As<T, ushort>(ref value));
            }
            if (TypeTraits<T>.IsVector2)
            {
                return new ParameterValue(Unsafe.As<T, Vector2>(ref value));
            }
            if (TypeTraits<T>.IsVector3)
            {
                return new ParameterValue(Unsafe.As<T, Vector3>(ref value));
            }
            if (TypeTraits<T>.IsVector4)
            {
                return new ParameterValue(Unsafe.As<T, Vector4>(ref value));
            }
            if (TypeTraits<T>.IsVector2Int)
            {
                return new ParameterValue(Unsafe.As<T, Vector2Int>(ref value));
            }
            if (TypeTraits<T>.IsVector3Int)
            {
                return new ParameterValue(Unsafe.As<T, Vector3Int>(ref value));
            }
            if (TypeTraits<T>.IsRect)
            {
                return new ParameterValue(Unsafe.As<T, Rect>(ref value));
            }
            if (TypeTraits<T>.IsRay2D)
            {
                return new ParameterValue(Unsafe.As<T, Ray2D>(ref value));
            }
            if (TypeTraits<T>.IsQuaternion)
            {
                return new ParameterValue(Unsafe.As<T, Quaternion>(ref value));
            }
            if (TypeTraits<T>.IsColor)
            {
                return new ParameterValue(Unsafe.As<T, Color>(ref value));
            }
            if (TypeTraits<T>.IsString)
            {
                return new ParameterValue(Unsafe.As<T, string>(ref value));
            }

            return new ParameterValue(value);
        }

        public readonly Type GetValueType() => type switch
        {
            ValueType.None => null,
            ValueType.Byte => Types.Byte,
            ValueType.SByte => Types.SByte,
            ValueType.Short => Types.Short,
            ValueType.UShort => Types.UShort,
            ValueType.Int => Types.Int,
            ValueType.UInt => Types.UInt,
            ValueType.Long => Types.Long,
            ValueType.ULong => Types.ULong,
            ValueType.Float => Types.Float,
            ValueType.Double => Types.Double,
            ValueType.Decimal => Types.Decimal,
            ValueType.Bool => Types.Bool,
            ValueType.Vector2 => Types.Vector2,
            ValueType.Vector3 => Types.Vector3,
            ValueType.Vector4 => Types.Vector4,
            ValueType.Vector2Int => Types.Vector2Int,
            ValueType.Vector3Int => Types.Vector3Int,
            ValueType.Rect => Types.Rect,
            ValueType.Ray2D => Types.Ray2D,
            ValueType.Quaternion => Types.Quaternion,
            ValueType.Color => Types.Color,
            ValueType.String => Types.String,
            ValueType.Object => ObjectValue?.GetType(),
            _ => null
        };

        /// <summary>
        /// Holds pre-allocated static references to common types.
        /// </summary>
        private static class Types
        {
            public static readonly Type Object = typeof(object);
            public static readonly Type Int = typeof(int);
            public static readonly Type Float = typeof(float);
            public static readonly Type Double = typeof(double);
            public static readonly Type Decimal = typeof(decimal);
            public static readonly Type Byte = typeof(byte);
            public static readonly Type SByte = typeof(sbyte);
            public static readonly Type Short = typeof(short);
            public static readonly Type UShort = typeof(ushort);
            public static readonly Type UInt = typeof(uint);
            public static readonly Type Long = typeof(long);
            public static readonly Type ULong = typeof(ulong);
            public static readonly Type Bool = typeof(bool);
            public static readonly Type Vector2 = typeof(Vector2);
            public static readonly Type Vector3 = typeof(Vector3);
            public static readonly Type Vector4 = typeof(Vector4);
            public static readonly Type Vector2Int = typeof(Vector2Int);
            public static readonly Type Vector3Int = typeof(Vector3Int);
            public static readonly Type Rect = typeof(Rect);
            public static readonly Type Ray2D = typeof(Ray2D);
            public static readonly Type Color = typeof(Color);
            public static readonly Type Quaternion = typeof(Quaternion);
            public static readonly Type String = typeof(string);
        }
    }
}
#endif
