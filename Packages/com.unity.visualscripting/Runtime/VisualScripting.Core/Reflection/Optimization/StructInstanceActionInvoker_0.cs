using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StructInstanceActionInvoker<TTarget> : OptimizedInvokerBase where TTarget : struct
    {
        public StructInstanceActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }
        private ActionRef invoke;
        private delegate void ActionRef(ref TTarget target);

        public override object Invoke(object target, params object[] args)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget);
            return null;
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            throw new InvalidOperationException("Instance member on struct requires ref target.");
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return InvokeRef(ref target);
        }

        public override object Invoke(object target)
        {
            ref TTarget tempTarget = ref Unsafe.Unbox<TTarget>(target);
            invoke(ref tempTarget);
            return null;
        }

        public override ParameterValue InvokeRef(ref ParameterValue target)
        {
            // This should handle: Variables, Literals and Port DefaultValues
            if (target.IsBoxed)
            {
                if (target.ObjectValue is TTarget)
                {
                    invoke(ref target.Unbox<TTarget>());
                    return ParameterValue.None;
                }
                else if (target.IsBoxedNumeric)
                {
                    TTarget converted = target.AsNumeric<TTarget>();
                    invoke(ref converted);
                    return ParameterValue.None;
                }
            }

            // Allow conversion this will remove the reference but works the same as Normal Visual Scripting.

            TTarget tempTarget = target.Cast<TTarget>();
            invoke(ref tempTarget);
            target = ParameterValue.Create(tempTarget);

            return ParameterValue.None;
        }

        protected override void CreateDelegate() => invoke = (ActionRef)methodInfo.CreateDelegate(typeof(ActionRef));
        protected override Type[] GetParameterTypes() => Type.EmptyTypes;

        protected sealed override void CompileExpression()
        {
            CreateDelegate();
        }

        protected override void VerifyTarget(object target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
        protected override void VerifyTarget(ParameterValue target) => OptimizedReflection.VerifyInstanceTarget<TTarget>(target);
    }
}