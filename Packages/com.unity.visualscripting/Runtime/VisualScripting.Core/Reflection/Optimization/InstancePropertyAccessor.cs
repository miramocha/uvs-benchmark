using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public sealed class InstancePropertyAccessor<TTarget, TProperty> : OptimizedAccessorBase
    {
        public InstancePropertyAccessor(PropertyInfo propertyInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(propertyInfo)).IsNotNull(propertyInfo);

                if (propertyInfo.DeclaringType != typeof(TTarget))
                {
                    throw new ArgumentException("The declaring type of the property info doesn't match the generic type.", nameof(propertyInfo));
                }

                if (propertyInfo.PropertyType != typeof(TProperty))
                {
                    throw new ArgumentException("The property type of the property info doesn't match the generic type.", nameof(propertyInfo));
                }

                if (propertyInfo.IsStatic())
                {
                    throw new ArgumentException("The property is static.", nameof(propertyInfo));
                }
            }

            this.propertyInfo = propertyInfo;
        }

        private readonly PropertyInfo propertyInfo;
        private Func<TTarget, TProperty> getter;
        private Action<TTarget, TProperty> setter;

        public override void Compile()
        {
            var getterInfo = propertyInfo.GetGetMethod(true);
            var setterInfo = propertyInfo.GetSetMethod(true);

            if (getterInfo != null)
            {
                if (OptimizedReflection.useJit)
                {
                    var targetExpression = Expression.Parameter(typeof(TTarget), "target");
                    var propertyExpression = Expression.Property(targetExpression, propertyInfo);
                    getter = Expression.Lambda<Func<TTarget, TProperty>>(propertyExpression, targetExpression).Compile();
                }
                else
                {
                    getter = (Func<TTarget, TProperty>)getterInfo.CreateDelegate(typeof(Func<TTarget, TProperty>));
                }
            }

            if (setterInfo != null)
            {
                if (OptimizedReflection.useJit)
                {
                    var targetExpression = Expression.Parameter(typeof(TTarget), "target");
                    var valueExpression = Expression.Parameter(typeof(TProperty), "value");
                    var propertyExpression = Expression.Property(targetExpression, propertyInfo);
                    var assignExpression = Expression.Assign(propertyExpression, valueExpression);
                    setter = Expression.Lambda<Action<TTarget, TProperty>>(assignExpression, targetExpression, valueExpression).Compile();
                }
                else
                {
                    setter = (Action<TTarget, TProperty>)setterInfo.CreateDelegate(typeof(Action<TTarget, TProperty>));
                }
            }
        }

        public override object GetValue(object target)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyInstanceTarget<TTarget>(target);

                if (getter == null)
                {
                    throw new TargetException($"The property '{typeof(TTarget)}.{propertyInfo.Name}' has no get accessor.");
                }

                try
                {
                    return getter((TTarget)target);
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
                return getter((TTarget)target);
            }
        }

        public override void SetValue(object target, object value)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyInstanceTarget<TTarget>(target);

                if (setter == null)
                {
                    throw new TargetException($"The property '{typeof(TTarget)}.{propertyInfo.Name}' has no set accessor.");
                }

                if (!typeof(TProperty).IsAssignableFrom(value))
                {
                    throw new ArgumentException($"The provided value for '{typeof(TTarget)}.{propertyInfo.Name}' does not match the property type.\nProvided: {value?.GetType()?.ToString() ?? "null"}\nExpected: {typeof(TProperty)}");
                }

                try
                {
                    setter((TTarget)target, (TProperty)value);
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
                setter((TTarget)target, (TProperty)value);
            }
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyInstanceTarget<TTarget>(target);

                if (setter == null)
                {
                    throw new TargetException($"The property '{typeof(TTarget)}.{propertyInfo.Name}' has no set accessor.");
                }

                if (!value.IsAssignableFrom<TProperty>())
                {
                    throw new ArgumentException($"The provided value for '{typeof(TTarget)}.{propertyInfo.Name}' does not match the property type.\nProvided: {value.GetValueType()?.ToString() ?? "null"}\nExpected: {typeof(TProperty)}");
                }

                try
                {
                    setter(target.Cast<TTarget>(), value.Cast<TProperty>());
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
                setter(target.Cast<TTarget>(), value.Cast<TProperty>());
            }
        }

        public override ParameterValue GetValue(ParameterValue target)
        {
            if (OptimizedReflection.safeMode)
            {
                OptimizedReflection.VerifyInstanceTarget<TTarget>(target);

                if (getter == null)
                {
                    throw new TargetException($"The property '{typeof(TTarget)}.{propertyInfo.Name}' has no get accessor.");
                }

                try
                {
                    TTarget rawTarget = target.Cast<TTarget>();
                    TProperty result = getter(rawTarget);
                    return ParameterValue.Create(result);
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
                TTarget rawTarget = target.Cast<TTarget>();
                TProperty result = getter(rawTarget);
                return ParameterValue.Create(result);
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
