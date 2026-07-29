using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceActionInvoker<TTarget, TParam0, TParam1, TParam2, TParam3> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private ActionRef invoke;
        private delegate void ActionRef(ref TTarget target, TParam0 arg0, TParam1 arg1, TParam2 arg2, TParam3 arg3);

        public override object Invoke(object target, params object[] args)
        {
            if (target is TTarget)
            {
                ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
                invoke(ref tempTarget, (TParam0)args[0], (TParam1)args[1], (TParam2)args[2], (TParam3)args[3]);
                return null;
            }

            throw new InvalidCastException();
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            if (target is TTarget)
            {
                ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
                invoke(ref tempTarget, (TParam0)arg0, (TParam1)arg1, (TParam2)arg2, (TParam3)arg3);
                return null;
            }

            throw new InvalidCastException();
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            throw new InvalidOperationException("Instance member on struct requires ref target.");
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return InvokeRef(ref target, args[0], args[1], args[2], args[3]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            var a0 = arg0.Cast<TParam0>();
            var a1 = arg1.Cast<TParam1>();
            var a2 = arg2.Cast<TParam2>();
            var a3 = arg3.Cast<TParam3>();

            // This should handle: Variables and Literals.
            // since this is visual scripting's normal functionality
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    invoke(ref t, a0, a1, a2, a3);
                    return ParameterValue.None;
                }

                if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    invoke(ref converted, a0, a1, a2, a3);
                    return ParameterValue.None;
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            invoke(ref tempTarget, a0, a1, a2, a3);
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.None;
        }

        protected override void CreateDelegate() => invoke = (ActionRef)methodInfo.CreateDelegate(typeof(ActionRef));
        protected override Type[] GetParameterTypes() => new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2), typeof(TParam3) };

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }
        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}