using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceFieldAccessor<TTarget, TField> : OptimizedAccessorBase where TTarget : struct
    {
        private int fieldOffset;
        private bool useDirectUnsafeMapping;

        public StructInstanceFieldAccessor(FieldInfo fieldInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(fieldInfo)).IsNotNull(fieldInfo);
                if (fieldInfo.DeclaringType != typeof(TTarget))
                    throw new ArgumentException("Declaring type mismatch.", nameof(fieldInfo));
                if (fieldInfo.FieldType != typeof(TField))
                    throw new ArgumentException("Field type mismatch.", nameof(fieldInfo));
                if (fieldInfo.IsStatic)
                    throw new ArgumentException("The field is static.", nameof(fieldInfo));
            }

            this.fieldInfo = fieldInfo;
        }

        private readonly FieldInfo fieldInfo;
        private GetterDelegate getter;
        private SetterDelegate setter;

        private delegate TField GetterDelegate(ref TTarget target);
        private delegate void SetterDelegate(ref TTarget target, TField value);

        public override void Compile()
        {
            fieldOffset = UnsafeUtility.GetFieldOffset(fieldInfo);

            useDirectUnsafeMapping = UnsafeUtility.IsBlittable(typeof(TTarget)) && UnsafeUtility.IsBlittable(typeof(TField));

            getter = ReflectionGetter;

            if (fieldInfo.CanWrite())
            {
                setter = ReflectionSetter;
            }
        }

        private TField ReflectionGetter(ref TTarget target)
        {
            if (useDirectUnsafeMapping)
            {
                return Unsafe.ReadUnaligned<TField>(ref Unsafe.Add(ref Unsafe.As<TTarget, byte>(ref target), fieldOffset));
            }

            object boxed = target;
            return (TField)fieldInfo.GetValue(boxed);
        }

        private void ReflectionSetter(ref TTarget target, TField value)
        {
            if (useDirectUnsafeMapping)
            {
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref Unsafe.As<TTarget, byte>(ref target), fieldOffset), value);
            }
            else
            {
                object boxed = target;
                fieldInfo.SetValue(boxed, value);
                target = (TTarget)boxed;
            }
        }
        #region Value Execution

        public override ParameterValue GetValue(ParameterValue target)
        {
            TTarget tempTarget = target.Cast<TTarget>();
            TField result = getter(ref tempTarget);
            return ParameterValue.Create(result);
        }

        public override ParameterValue GetValueRef(ref ParameterValue target)
        {
            TTarget tempTarget = target.Cast<TTarget>();
            TField result = getter(ref tempTarget);
            return ParameterValue.Create(result);
        }

        public override void SetValueRef(ref ParameterValue target, ParameterValue value)
        {
            if (fieldInfo.IsInitOnly)
            {
                throw new FieldAccessException($"The field '{typeof(TTarget)}.{fieldInfo.Name}' is readonly.");
            }

            if (OptimizedReflection.safeMode)
            {
                if (target.GetValueType() != typeof(TTarget))
                    throw new ArgumentException("Target type mismatch. When setting a value on a struct the target must match the struct type.", nameof(target));
            }

            if (target.IsBoxed)
            {
                setter(ref target.Unbox<TTarget>(), value.Cast<TField>());
            }
            else
            {
                TTarget tempTarget = target.Cast<TTarget>();
                setter(ref tempTarget, value.Cast<TField>());
                target = ParameterValue.Create(tempTarget);
            }
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
        => throw new NotSupportedException("StructInstanceFieldAccessor requires a ref target to persist changes. Use the ref ParameterValue overload.");

        #endregion

        #region Interface Implementation (Boxed/Ref)

        public override object GetValue(object target)
        {
            return getter(ref Unsafe.Unbox<TTarget>(target));
        }

        public override void SetValue(object target, object value)
        {
            setter(ref Unsafe.Unbox<TTarget>(target), (TField)value);
        }

        #endregion
    }
}