using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StaticFunctionInvoker<TParam0, TParam1, TParam2, TResult> : StaticFunctionInvokerBase<TResult>
    {
        public unsafe StaticFunctionInvoker(MethodInfo methodInfo) : base(methodInfo)
        {
            invoke = (delegate*<TParam0, TParam1, TParam2, TResult>)methodInfo.MethodHandle.GetFunctionPointer();
        }

        private readonly unsafe delegate*<TParam0, TParam1, TParam2, TResult> invoke;

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 3)
                throw new TargetParameterCountException();

            return Invoke(target, args[0], args[1], args[2]);
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);

                try
                {
                    return InvokeUnsafe(target, arg0, arg1, arg2);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(target, arg0, arg1, arg2);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            if (args.Length < 3) throw new IndexOutOfRangeException();

            return InvokeUnsafe(args[0], args[1], args[2]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            if (args.Length < 3) throw new IndexOutOfRangeException();

            return InvokeUnsafe(args[0], args[1], args[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object InvokeUnsafe(object target, object arg0, object arg1, object arg2)
        {
            return invoke((TParam0)arg0, (TParam1)arg1, (TParam2)arg2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ParameterValue InvokeUnsafe(ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            return ParameterValue.Create(invoke(arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>()));
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);

                try
                {
                    return InvokeUnsafe(arg0, arg1, arg2);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(arg0, arg1, arg2);
            }
        }

        protected override Type[] GetParameterTypes()
        {
            return new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2) };
        }

        protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions)
        {
        }

        protected override void CreateDelegate()
        {
        }
    }
}