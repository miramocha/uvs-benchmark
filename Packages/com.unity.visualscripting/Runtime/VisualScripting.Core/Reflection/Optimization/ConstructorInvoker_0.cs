using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class ConstructorInvoker<TResult> : ConstructorInvokerBase<TResult>
    {
        private Func<TResult> invoke;

        public ConstructorInvoker(ConstructorInfo constructorInfo) : base(constructorInfo) { }

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 1)
                throw new TargetParameterCountException();

            return Invoke(target, args[0]);
        }

        public override object Invoke(object target)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);

                try
                {
                    return InvokeUnsafe();
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe();
            }
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            if (args.Length > 0) throw new IndexOutOfRangeException();

            return InvokeUnsafeParameterValue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object InvokeUnsafe()
        {
            return invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeUnsafeParameterValue()
        {
            return ParameterValue.Create(invoke());
        }

        public override ParameterValue Invoke(ParameterValue target)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);

                try
                {
                    return InvokeUnsafeParameterValue();
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafeParameterValue();
            }
        }

        protected override Type[] GetParameterTypes()
        {
            return Type.EmptyTypes;
        }

        protected override void CompileExpression(NewExpression newExpression, ParameterExpression[] parameterExpressions)
        {
            invoke = Expression.Lambda<Func<TResult>>(newExpression, parameterExpressions).Compile();
        }

        private static readonly object[] emptyArgs = Array.Empty<object>();

        protected override void CreateDelegate()
        {
            invoke = () =>
            {
                return (TResult)constructorInfo.Invoke(emptyArgs);
            };
        }
    }
}