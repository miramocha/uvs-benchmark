using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    [SerializationVersion("A")]
    public sealed class VariableDeclaration
    {
        [Obsolete(Serialization.ConstructorWarning)]
        public VariableDeclaration() { }

        public VariableDeclaration(string name, object value)
        {
            this.name = name;
            _value = value;
        }

        public VariableDeclaration(string name, ParameterValue value)
        {
            this.name = name;
            // We need to use object for the inital creation
            _value = BoxMutator.CloneBoxSafely(value);
        }

        [Serialize]
        public string name { get; private set; }

        [DoNotSerialize]
        private object _value;

        [SerializeAs(nameof(value)), Value]
        public object value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public void SetValue(ParameterValue parameter)
        {
            _value = BoxMutator.UpdateBoxedValue(_value, parameter);
        }

        [Serialize]
        public SerializableType typeHandle { get; set; }

#if UNITY_EDITOR
        [Serialize]
        public bool isOpen { get; set; }
#endif
    }

    public static class BoxMutator
    {
        public static object CloneBoxSafely(ParameterValue obj)
        {
            switch (obj.type)
            {
                case ParameterValue.ValueType.Bool: return obj.boolValue;
                case ParameterValue.ValueType.Byte: return obj.byteValue;
                case ParameterValue.ValueType.SByte: return obj.sbyteValue;
                case ParameterValue.ValueType.Short: return obj.shortValue;
                case ParameterValue.ValueType.UShort: return obj.ushortValue;
                case ParameterValue.ValueType.Int: return obj.intValue;
                case ParameterValue.ValueType.UInt: return obj.uintValue;
                case ParameterValue.ValueType.Long: return obj.longValue;
                case ParameterValue.ValueType.ULong: return obj.ulongValue;
                case ParameterValue.ValueType.Float: return obj.floatValue;
                case ParameterValue.ValueType.Double: return obj.doubleValue;
                case ParameterValue.ValueType.Vector2: return obj.vector2Value;
                case ParameterValue.ValueType.Vector3: return obj.vector3Value;
                case ParameterValue.ValueType.Vector4: return obj.vector4Value;
                case ParameterValue.ValueType.Quaternion: return obj.quaternionValue;
                case ParameterValue.ValueType.Color: return obj.colorValue;

                case ParameterValue.ValueType.String:
                    return obj.ObjectValue;

                case ParameterValue.ValueType.Object:
                    object rawObject = obj.ObjectValue;
                    if (rawObject == null) return null;

                    if (rawObject.GetType().IsValueType)
                    {
                        return rawObject switch
                        {
                            bool b => b,
                            byte by => by,
                            sbyte sb => sb,
                            short s => s,
                            ushort us => us,
                            int i => i,
                            uint ui => ui,
                            long l => l,
                            ulong ul => ul,
                            float f => f,
                            double d => d,
                            Vector2 v2 => v2,
                            Vector3 v3 => v3,
                            Vector4 v4 => v4,
                            Quaternion q => q,
                            Color c => c,
                            _ => RuntimeHelpers.GetObjectValue(rawObject)
                        };
                    }

                    return rawObject;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Updates an existing boxed value in-place to avoid GC allocations.
        /// Falls back to creating a new box if types do not match.
        /// </summary>
        public static object UpdateBoxedValue(object existingBox, ParameterValue newValue)
        {
            if (existingBox == null || newValue.UsesObjectID)
            {
                return newValue.ObjectValue;
            }

            switch (newValue.type)
            {
                case ParameterValue.ValueType.Bool:
                    if (existingBox is bool) { Unsafe.Unbox<bool>(existingBox) = newValue.boolValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Byte:
                    if (existingBox is byte) { Unsafe.Unbox<byte>(existingBox) = newValue.byteValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.SByte:
                    if (existingBox is sbyte) { Unsafe.Unbox<sbyte>(existingBox) = newValue.sbyteValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Short:
                    if (existingBox is short) { Unsafe.Unbox<short>(existingBox) = newValue.shortValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.UShort:
                    if (existingBox is ushort) { Unsafe.Unbox<ushort>(existingBox) = newValue.ushortValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Int:
                    if (existingBox is int) { Unsafe.Unbox<int>(existingBox) = newValue.intValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.UInt:
                    if (existingBox is uint) { Unsafe.Unbox<uint>(existingBox) = newValue.uintValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Long:
                    if (existingBox is long) { Unsafe.Unbox<long>(existingBox) = newValue.longValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.ULong:
                    if (existingBox is ulong) { Unsafe.Unbox<ulong>(existingBox) = newValue.ulongValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Float:
                    if (existingBox is float) { Unsafe.Unbox<float>(existingBox) = newValue.floatValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Double:
                    if (existingBox is double) { Unsafe.Unbox<double>(existingBox) = newValue.doubleValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Vector2:
                    if (existingBox is Vector2) { Unsafe.Unbox<Vector2>(existingBox) = newValue.vector2Value; return existingBox; }
                    break;
                case ParameterValue.ValueType.Vector3:
                    if (existingBox is Vector3) { Unsafe.Unbox<Vector3>(existingBox) = newValue.vector3Value; return existingBox; }
                    break;
                case ParameterValue.ValueType.Vector4:
                    if (existingBox is Vector4) { Unsafe.Unbox<Vector4>(existingBox) = newValue.vector4Value; return existingBox; }
                    break;
                case ParameterValue.ValueType.Quaternion:
                    if (existingBox is Quaternion) { Unsafe.Unbox<Quaternion>(existingBox) = newValue.quaternionValue; return existingBox; }
                    break;
                case ParameterValue.ValueType.Color:
                    if (existingBox is Color) { Unsafe.Unbox<Color>(existingBox) = newValue.colorValue; return existingBox; }
                    break;
            }

            return newValue.ToObject();
        }
    }
}
