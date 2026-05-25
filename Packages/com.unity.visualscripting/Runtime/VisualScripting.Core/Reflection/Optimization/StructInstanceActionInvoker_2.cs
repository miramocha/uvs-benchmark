using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceActionInvoker<TTarget, TParam0, TParam1> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private ActionRef invoke;
        private delegate void ActionRef(ref TTarget target, TParam0 arg0, TParam1 arg1);

        public override object Invoke(object target, params object[] args)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget, (TParam0)args[0], (TParam1)args[1]);
            return null;
        }

        public override object Invoke(object target, object arg0, object arg1)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget, (TParam0)arg0, (TParam1)arg1);
            return null;
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            throw new InvalidOperationException("Instance member on struct requires ref target.");
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return InvokeRef(ref target, args[0], args[1]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            // This should handle: Variables, Literals and Port DefaultValues
            if (target.IsBoxed)
            {
                if (target.ObjectValue is TTarget)
                {
                    invoke(ref target.Unbox<TTarget>(), arg0.Cast<TParam0>(), arg1.Cast<TParam1>());
                    return ParameterValue.None;
                }
                else if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    invoke(ref converted, arg0.Cast<TParam0>(), arg1.Cast<TParam1>());
                    return ParameterValue.None;
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            invoke(ref tempTarget, arg0.Cast<TParam0>(), arg1.Cast<TParam1>());
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.None;
        }

        protected override void CreateDelegate() => invoke = (ActionRef)methodInfo.CreateDelegate(typeof(ActionRef));
        protected override Type[] GetParameterTypes() => new[] { typeof(TParam0), typeof(TParam1) };

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }
        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}