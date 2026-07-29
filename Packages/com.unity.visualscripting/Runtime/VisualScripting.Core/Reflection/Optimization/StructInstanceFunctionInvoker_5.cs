using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceFunctionInvoker<TTarget, TParam0, TParam1, TParam2, TParam3, TParam4, TReturn> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceFunctionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private FuncRef invoke;
        private delegate TReturn FuncRef(ref TTarget target, TParam0 arg0, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4);

        public override object Invoke(object target, params object[] args)
        {
            if (target is TTarget)
            {
                ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
                return invoke(ref tempTarget, (TParam0)args[0], (TParam1)args[1], (TParam2)args[2], (TParam3)args[3], (TParam4)args[4]);
            }

            throw new InvalidCastException();
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            if (target is TTarget)
            {
                ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
                return invoke(ref tempTarget, (TParam0)arg0, (TParam1)arg1, (TParam2)arg2, (TParam3)arg3, (TParam4)arg4);
            }

            throw new InvalidCastException();
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
            var a0 = arg0.Cast<TParam0>();
            var a1 = arg1.Cast<TParam1>();
            var a2 = arg2.Cast<TParam2>();
            var a3 = arg3.Cast<TParam3>();
            var a4 = arg4.Cast<TParam4>();

            // This should handle: Variables and Literals.
            // since this is visual scripting's normal functionality
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    return ParameterValue.Create(invoke(ref t, a0, a1, a2, a3, a4));
                }

                if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    return ParameterValue.Create(invoke(ref converted, a0, a1, a2, a3, a4));
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            TReturn result = invoke(ref tempTarget, a0, a1, a2, a3, a4);
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.Create(result);
        }

        protected override void CreateDelegate() => invoke = (FuncRef)methodInfo.CreateDelegate(typeof(FuncRef));
        protected override Type[] GetParameterTypes() => new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4) };

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }
        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}