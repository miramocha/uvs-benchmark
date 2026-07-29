using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StructInstancePropertyAccessor<TTarget, TProperty> : OptimizedAccessorBase where TTarget : struct
    {
        public StructInstancePropertyAccessor(PropertyInfo propertyInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(propertyInfo)).IsNotNull(propertyInfo);
                if (propertyInfo.DeclaringType != typeof(TTarget))
                    throw new ArgumentException("Declaring type mismatch.", nameof(propertyInfo));
                if (propertyInfo.PropertyType != typeof(TProperty))
                    throw new ArgumentException("Property type mismatch.", nameof(propertyInfo));
                if (propertyInfo.IsStatic())
                    throw new ArgumentException("The property is static.", nameof(propertyInfo));
            }

            this.propertyInfo = propertyInfo;
        }

        private readonly PropertyInfo propertyInfo;
        private GetterDelegate getter;
        private SetterDelegate setter;

        private delegate TProperty GetterDelegate(ref TTarget target);
        private delegate void SetterDelegate(ref TTarget target, TProperty value);

        public override void Compile()
        {
            var getterInfo = propertyInfo.GetGetMethod(true);
            var setterInfo = propertyInfo.GetSetMethod(true);

            if (getterInfo != null)
            {
                getter = (GetterDelegate)getterInfo.CreateDelegate(typeof(GetterDelegate));
            }

            if (setterInfo != null)
            {
                setter = (SetterDelegate)setterInfo.CreateDelegate(typeof(SetterDelegate));
            }
        }

        #region Interface Implementation (ParameterValue)

        public override ParameterValue GetValueRef(ref ParameterValue target)
        {
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    return ParameterValue.Create(getter(ref t));
                }
            }

            TTarget tempTarget = target.Cast<TTarget>();
            TProperty result = getter(ref tempTarget);
            target = ParameterValue.Create(tempTarget);
            return ParameterValue.Create(result);
        }

        public override void SetValueRef(ref ParameterValue target, ParameterValue value)
        {
            var propertyValue = value.Cast<TProperty>();
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    setter(ref t, propertyValue);
                    return;
                }
            }

            TTarget tempTarget = target.Cast<TTarget>();
            setter(ref tempTarget, value.Cast<TProperty>());
            target = ParameterValue.Create(tempTarget);
        }

        public override ParameterValue GetValue(ParameterValue target)
        {
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    return ParameterValue.Create(getter(ref t));
                }
            }

            TTarget tempTarget = target.Cast<TTarget>();
            TProperty result = getter(ref tempTarget);
            return ParameterValue.Create(result);
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        {
            throw new NotSupportedException("StructInstancePropertyAccessor requires a ref target to persist changes. Use the ref ParameterValue overload.");
        }

        #endregion

        #region Interface Implementation (Boxed)

        public override object GetValue(object target)
        {
            if (target is TTarget)
            {
                return getter(ref Unsafe.Unbox<TTarget>(target));
            }

            throw new InvalidCastException();
        }

        public override void SetValue(object target, object value)
        {
            if (target is TTarget)
            {
                setter(ref Unsafe.Unbox<TTarget>(target), (TProperty)value);
                return;
            }

            throw new InvalidCastException();
        }
        #endregion
    }
}