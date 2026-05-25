using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public class StaticPropertyAccessor<TProperty> : OptimizedAccessorBase
    {
        public StaticPropertyAccessor(PropertyInfo propertyInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                if (propertyInfo == null)
                {
                    throw new ArgumentNullException(nameof(propertyInfo));
                }

                if (propertyInfo.PropertyType != typeof(TProperty))
                {
                    throw new ArgumentException("The property type of the property info doesn't match the generic type.", nameof(propertyInfo));
                }

                if (!propertyInfo.IsStatic())
                {
                    throw new ArgumentException("The property isn't static.", nameof(propertyInfo));
                }
            }

            this.propertyInfo = propertyInfo;
            targetType = propertyInfo.DeclaringType;
        }

        private readonly PropertyInfo propertyInfo;
        private Func<TProperty> getter;
        private Action<TProperty> setter;
        private Type targetType;

        public override void Compile()
        {
            var getterInfo = propertyInfo.GetGetMethod(true);
            var setterInfo = propertyInfo.GetSetMethod(true);

            if (getterInfo != null)
            {
                getter = (Func<TProperty>)getterInfo.CreateDelegate(typeof(Func<TProperty>));
            }

            if (setterInfo != null)
            {
                setter = (Action<TProperty>)setterInfo.CreateDelegate(typeof(Action<TProperty>));
            }
        }

        public override object GetValue(object target)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyStaticTarget(targetType, target);

                if (getter == null)
                {
                    throw new TargetException($"The property '{targetType}.{propertyInfo.Name}' has no get accessor.");
                }

                try
                {
                    return getter();
                }
                catch (TargetInvocationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new TargetInvocationException(ex);
                }
            }
            else
            {
                return getter();
            }
        }

        public override void SetValue(object target, object value)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyStaticTarget(targetType, target);

                if (setter == null)
                {
                    throw new TargetException($"The property '{targetType}.{propertyInfo.Name}' has no set accessor.");
                }

                if (!typeof(TProperty).IsAssignableFrom(value))
                {
                    throw new ArgumentException($"The provided value for '{targetType}.{propertyInfo.Name}' does not match the property type.\nProvided: {value?.GetType()?.ToString() ?? "null"}\nExpected: {typeof(TProperty)}");
                }

                try
                {
                    setter((TProperty)value);
                }
                catch (TargetInvocationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new TargetInvocationException(ex);
                }
            }
            else
            {
                setter((TProperty)value);
            }
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyStaticTarget(targetType, target);

                if (setter == null)
                {
                    throw new TargetException($"The property '{targetType}.{propertyInfo.Name}' has no set accessor.");
                }

                if (!value.IsAssignableFrom<TProperty>())
                {
                    throw new ArgumentException($"The provided value for '{targetType}.{propertyInfo.Name}' does not match the property type.\nProvided: {value.ToObject()?.GetType()?.ToString() ?? "null"}\nExpected: {typeof(TProperty)}");
                }

                try
                {
                    setter(value.Cast<TProperty>());
                }
                catch (TargetInvocationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new TargetInvocationException(ex);
                }
            }
            else
            {
                setter(value.Cast<TProperty>());
            }
        }

        public override ParameterValue GetValue(ParameterValue target)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyStaticTarget(targetType, target);

                if (getter == null)
                {
                    throw new TargetException($"The property '{targetType}.{propertyInfo.Name}' has no get accessor.");
                }

                try
                {
                    return ParameterValue.Create(getter());
                }
                catch (TargetInvocationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new TargetInvocationException(ex);
                }
            }
            else
            {
                return ParameterValue.Create(getter());
            }
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
