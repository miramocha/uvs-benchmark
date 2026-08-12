using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public sealed class StaticActionInvoker<TParam0, TParam1, TParam2, TParam3> : StaticActionInvokerBase
    {
        public StaticActionInvoker(MethodInfo methodInfo) : base(methodInfo) { }

        private Action<TParam0, TParam1, TParam2, TParam3> invoke;

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 4)
            {
                throw new TargetParameterCountException();
            }

            return Invoke(target, args[0], args[1], args[2], args[3]);
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);
                VerifyArgument<TParam3>(methodInfo, 3, arg3);

                try
                {
                    return InvokeUnsafe(target, arg0, arg1, arg2, arg3);
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

            return InvokeUnsafe(target, arg0, arg1, arg2, arg3);
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0], args[1], args[2], args[3]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0], args[1], args[2], args[3]);
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);
                VerifyArgument<TParam3>(methodInfo, 3, arg3);

                try
                {
                    return InvokeUnsafe(target, arg0, arg1, arg2, arg3);
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

            return InvokeUnsafe(target, arg0, arg1, arg2, arg3);
        }

        private object InvokeUnsafe(object target, object arg0, object arg1, object arg2, object arg3)
        {
            invoke.Invoke((TParam0)arg0, (TParam1)arg1, (TParam2)arg2, (TParam3)arg3);
            return null;
        }

        private ParameterValue InvokeUnsafe(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            invoke.Invoke(arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>(), arg3.Cast<TParam3>());
            return ParameterValue.Null;
        }

        protected override Type[] GetParameterTypes()
        {
            return new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2), typeof(TParam3) };
        }

        protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions)
        {
            invoke = Expression.Lambda<Action<TParam0, TParam1, TParam2, TParam3>>(callExpression, parameterExpressions).Compile();
        }

        protected override void CreateDelegate()
        {
            invoke = (Action<TParam0, TParam1, TParam2, TParam3>)methodInfo.CreateDelegate(typeof(Action<TParam0, TParam1, TParam2, TParam3>));
        }
    }
}