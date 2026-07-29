using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

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
            if (OptimizedReflection.useJit)
            {
                var targetParam = Expression.Parameter(typeof(TTarget).MakeByRefType(), "target");
                var fieldAccess = Expression.Field(targetParam, fieldInfo);

                getter = Expression.Lambda<GetterDelegate>(fieldAccess, targetParam).Compile();

                if (fieldInfo.CanWrite())
                {
                    var valueParam = Expression.Parameter(typeof(TField), "value");
                    var assign = Expression.Assign(fieldAccess, valueParam);

                    setter = Expression.Lambda<SetterDelegate>(assign, targetParam, valueParam).Compile();
                }
            }
            else
            {
#if MODULE_COLLECTIONS_EXISTS
                fieldOffset = UnsafeUtility.GetFieldOffset(fieldInfo);
                useDirectUnsafeMapping = !RuntimeHelpers.IsReferenceOrContainsReferences<TField>();
#endif
                getter = ReflectionGetter;

                if (fieldInfo.CanWrite())
                {
                    setter = ReflectionSetter;
                }
            }
        }

#if MODULE_COLLECTIONS_EXISTS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TField ReflectionGetter(ref TTarget target)
        {
            ref byte baseRef = ref Unsafe.As<TTarget, byte>(ref target);
            ref byte fieldRef = ref Unsafe.Add(ref baseRef, fieldOffset);

            if (useDirectUnsafeMapping)
            {
                return Unsafe.ReadUnaligned<TField>(ref fieldRef);
            }

            return Unsafe.As<byte, TField>(ref fieldRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReflectionSetter(ref TTarget target, TField value)
        {
            ref byte baseRef = ref Unsafe.As<TTarget, byte>(ref target);
            ref byte fieldRef = ref Unsafe.Add(ref baseRef, fieldOffset);

            if (useDirectUnsafeMapping)
            {
                Unsafe.WriteUnaligned(ref fieldRef, value);
                return;
            }

            ref TField field = ref Unsafe.As<byte, TField>(ref fieldRef);
            field = value;
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TField ReflectionGetter(ref TTarget target)
        {
            return (TField)fieldInfo.GetValue(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReflectionSetter(ref TTarget target, TField value)
        {
            fieldInfo.SetValue(target, value);
        }
#endif
        #region Value Execution

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
            return ParameterValue.Create(getter(ref tempTarget));
        }

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
            return ParameterValue.Create(getter(ref tempTarget));
        }

        public override void SetValueRef(ref ParameterValue target, ParameterValue value)
        {
            var fieldValue = value.Cast<TField>();

            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    setter(ref t, fieldValue);
                    return;
                }
            }

            TTarget tempTarget = target.Cast<TTarget>();
            setter(ref tempTarget, fieldValue);
            target = ParameterValue.Create(tempTarget);
        }

        public override void SetValue(ParameterValue target, ParameterValue value)
            => throw new NotSupportedException("StructInstanceFieldAccessor requires a ref target to persist changes. Use the ref ParameterValue overload.");

        #endregion

        #region Interface Implementation (Boxed/Ref)

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
                setter(ref Unsafe.Unbox<TTarget>(target), (TField)value);
                return;
            }

            throw new InvalidCastException();
        }
        #endregion
    }
}