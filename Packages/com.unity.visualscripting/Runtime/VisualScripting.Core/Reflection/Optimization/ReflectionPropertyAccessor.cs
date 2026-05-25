using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class ReflectionPropertyAccessor : OptimizedAccessorBase
    {
        public ReflectionPropertyAccessor(PropertyInfo propertyInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(propertyInfo)).IsNotNull(propertyInfo);
            }

            this.propertyInfo = propertyInfo;
        }

        private readonly PropertyInfo propertyInfo;

        public override void Compile() { }

        public override  object GetValue(object target)
        {
            return propertyInfo.GetValue(target, null);
        }

        public override void SetValue(object target, object value)
        {
            propertyInfo.SetValue(target, value, null);
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        {
            propertyInfo.SetValue(ConvertTarget(target), ConvertArg(value));
        }

        public override ParameterValue GetValue(ParameterValue target)
        {
            return ParameterValue.Create(propertyInfo.GetValue(ConvertTarget(target)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertArg(ParameterValue value)
        {
            object obj = value.ToObject();
            Type targetType = propertyInfo.PropertyType;

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
            Type targetType = propertyInfo.DeclaringType;

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