using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceFunctionInvoker<TTarget, TReturn> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceFunctionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private FuncRef invoke;
        private delegate TReturn FuncRef(ref TTarget target);

        public override object Invoke(object target, params object[] args)
        {
            if (target is TTarget)
            {
                ref TTarget t = ref Unsafe.Unbox<TTarget>(target);
                return invoke(ref t);
            }

            throw new InvalidCastException();
        }

        public override object Invoke(object target)
        {
            if (target is TTarget)
            {
                ref TTarget t = ref Unsafe.Unbox<TTarget>(target);
                return invoke(ref t);
            }

            throw new InvalidCastException();
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            throw new InvalidOperationException("Instance member on struct requires ref target.");
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return InvokeRef(ref target);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target)
        {
            // This should handle: Variables and Literals.
            // since this is visual scripting's normal functionality
            if (target.IsBoxed)
            {
                ref TTarget t = ref target.TryUnbox<TTarget>(out var canUnbox);
                if (canUnbox)
                {
                    return ParameterValue.Create(invoke(ref t));
                }

                if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    return ParameterValue.Create(invoke(ref converted));
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            TReturn result = invoke(ref tempTarget);
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.Create(result);
        }

        protected override void CreateDelegate() => invoke = (FuncRef)methodInfo.CreateDelegate(typeof(FuncRef));
        protected override Type[] GetParameterTypes() => Type.EmptyTypes;

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }
        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}