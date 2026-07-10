using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StaticFunctionInvoker<TResult> : StaticFunctionInvokerBase<TResult>
    {
        public unsafe StaticFunctionInvoker(MethodInfo methodInfo) : base(methodInfo)
        {
            invoke = (delegate*<TResult>)methodInfo.MethodHandle.GetFunctionPointer();
        }

        private readonly unsafe delegate*<TResult> invoke;


        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 0)
            {
                throw new TargetParameterCountException();
            }

            return Invoke(target);
        }

        public override object Invoke(object target)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);

                try
                {
                    return InvokeUnsafe(target);
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

            return InvokeUnsafe(target);
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target);
        }

        public override ParameterValue Invoke(ParameterValue target)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);

                try
                {
                    return InvokeUnsafe();
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

            return InvokeUnsafe();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object InvokeUnsafe(object target)
        {
            return invoke();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ParameterValue InvokeUnsafe()
        {
            return ParameterValue.Create(invoke());
        }

        protected override Type[] GetParameterTypes()
        {
            return Type.EmptyTypes;
        }

        protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions)
        {
        }

        protected override void CreateDelegate()
        {
        }
    }
}