using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class ReflectionFieldAccessor : OptimizedAccessorBase
    {
        public ReflectionFieldAccessor(FieldInfo fieldInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(fieldInfo)).IsNotNull(fieldInfo);
            }

            this.fieldInfo = fieldInfo;
        }

        private readonly FieldInfo fieldInfo;

        public override void Compile() { }

        public override object GetValue(object target)
        {
            return fieldInfo.GetValue(target);
        }

        public override void SetValue(object target, object value)
        {
            fieldInfo.SetValue(target, value);
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        {
            fieldInfo.SetValue(ConvertTarget(target), ConvertArg(value));
        }

        public override ParameterValue GetValue(ParameterValue target)
        {
            return ParameterValue.Create(fieldInfo.GetValue(ConvertTarget(target)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertArg(ParameterValue value)
        {
            object obj = value.ToObject();
            Type targetType = fieldInfo.FieldType;

            if (obj == null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                    return null;

                throw new InvalidCastException($"Cannot assign null to {targetType.FullName}");
            }

            Type valueType = obj.GetType();

            if (targetType.IsAssignableFrom(valueType))
                return obj;

            return ConversionUtility.Convert(obj, targetType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertTarget(ParameterValue target)
        {
            object obj = target.ToObject();
            Type targetType = fieldInfo.DeclaringType;

            if (obj == null)
                throw new InvalidCastException($"Cannot assign null to {targetType.FullName}");

            Type valueType = obj.GetType();
            if (targetType.IsAssignableFrom(valueType))
                return obj;

            return ConversionUtility.Convert(obj, targetType);
        }

        public override void SetValueRef(ref ParameterValue target, ParameterValue value)
        {
            SetValue(target, value);
        }

        public override ParameterValue GetValueRef(ref ParameterValue target)
        {
            return GetValue(target);
        }
    }
}
