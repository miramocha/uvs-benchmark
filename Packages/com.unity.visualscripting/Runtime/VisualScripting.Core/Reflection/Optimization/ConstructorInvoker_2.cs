using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class ConstructorInvoker<TResult, TParam0, TParam1> : ConstructorInvokerBase<TResult>
    {
        private Func<TParam0, TParam1, TResult> invoke;

        public ConstructorInvoker(ConstructorInfo constructorInfo) : base(constructorInfo) { }

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 2)
                throw new TargetParameterCountException();

            return Invoke(target, args[0], args[1]);
        }

        public override object Invoke(object target, object arg0, object arg1)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(constructorInfo, 0, arg0);
                VerifyArgument<TParam1>(constructorInfo, 1, arg1);

                try
                {
                    return InvokeUnsafe(target, arg0, arg1);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(target, arg0, arg1);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            if (args.Length < 2) throw new IndexOutOfRangeException();

            return InvokeUnsafe(args[0], args[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object InvokeUnsafe(object target, object arg0, object arg1)
        {
            return invoke((TParam0)arg0, (TParam1)arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeUnsafe(ParameterValue arg0, ParameterValue arg1)
        {
            return ParameterValue.Create(invoke(arg0.Cast<TParam0>(), arg1.Cast<TParam1>()));
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(constructorInfo, 0, arg0);
                VerifyArgument<TParam1>(constructorInfo, 1, arg1);

                try
                {
                    return InvokeUnsafe(arg0, arg1);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(arg0, arg1);
            }
        }

        protected override Type[] GetParameterTypes()
        {
            return new[] { typeof(TParam0), typeof(TParam1) };
        }

        protected override void CompileExpression(NewExpression newExpression, ParameterExpression[] parameterExpressions)
        {
            invoke = Expression.Lambda<Func<TParam0, TParam1, TResult>>(newExpression, parameterExpressions).Compile();
        }

        protected override void CreateDelegate()
        {
            invoke = (arg0, arg1) =>
            {
                return (TResult)constructorInfo.Invoke(new object[] { arg0, arg1 });
            };
        }
    }
}