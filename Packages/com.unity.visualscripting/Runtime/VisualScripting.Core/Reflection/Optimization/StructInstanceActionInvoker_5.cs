using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceActionInvoker<TTarget, TParam0, TParam1, TParam2, TParam3, TParam4> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private ActionRef invoke;
        private delegate void ActionRef(ref TTarget target, TParam0 arg0, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4);

        public override object Invoke(object target, params object[] args)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget, (TParam0)args[0], (TParam1)args[1], (TParam2)args[2], (TParam3)args[3], (TParam4)args[4]);
            return null;
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget, (TParam0)arg0, (TParam1)arg1, (TParam2)arg2, (TParam3)arg3, (TParam4)arg4);
            return null;
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            throw new InvalidOperationException("Instance member on struct requires ref target.");
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return InvokeRef(ref target, args[0], args[1], args[2], args[3], args[4]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            // This should handle: Variables, Literals and Port DefaultValues
            if (target.IsBoxed)
            {
                if (target.ObjectValue is TTarget)
                {
                    invoke(ref target.Unbox<TTarget>(), arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>(), arg3.Cast<TParam3>(), arg4.Cast<TParam4>());
                    return ParameterValue.None;
                }
                else if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    invoke(ref converted, arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>(), arg3.Cast<TParam3>(), arg4.Cast<TParam4>());
                    return ParameterValue.None;
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            invoke(ref tempTarget, arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>(), arg3.Cast<TParam3>(), arg4.Cast<TParam4>());
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.None;
        }

        protected override void CreateDelegate() => invoke = (ActionRef)methodInfo.CreateDelegate(typeof(ActionRef));
        protected override Type[] GetParameterTypes() => new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4) };

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }
        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}