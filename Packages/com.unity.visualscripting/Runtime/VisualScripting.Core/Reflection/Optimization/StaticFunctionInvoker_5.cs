using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StaticFunctionInvoker<TParam0, TParam1, TParam2, TParam3, TParam4, TResult> : StaticFunctionInvokerBase<TResult>
    {
        public unsafe StaticFunctionInvoker(MethodInfo methodInfo) : base(methodInfo)
        {
            invoke = (delegate*<TParam0, TParam1, TParam2, TParam3, TParam4, TResult>)methodInfo.MethodHandle.GetFunctionPointer();
        }

        private readonly unsafe delegate*<TParam0, TParam1, TParam2, TParam3, TParam4, TResult> invoke;

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 5)
                throw new TargetParameterCountException();

            return Invoke(target, args[0], args[1], args[2], args[3], args[4]);
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);
                VerifyArgument<TParam3>(methodInfo, 3, arg3);
                VerifyArgument<TParam4>(methodInfo, 4, arg4);

                try
                {
                    return InvokeUnsafe(arg0, arg1, arg2, arg3, arg4);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(arg0, arg1, arg2, arg3, arg4);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0], args[1], args[2], args[3], args[4]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0], args[1], args[2], args[3], args[4]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object InvokeUnsafe(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            return invoke((TParam0)arg0, (TParam1)arg1, (TParam2)arg2, (TParam3)arg3, (TParam4)arg4);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ParameterValue InvokeUnsafe(ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            return ParameterValue.Create(invoke(arg0.Cast<TParam0>(), arg1.Cast<TParam1>(), arg2.Cast<TParam2>(), arg3.Cast<TParam3>(), arg4.Cast<TParam4>()));
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);
                VerifyArgument<TParam1>(methodInfo, 1, arg1);
                VerifyArgument<TParam2>(methodInfo, 2, arg2);
                VerifyArgument<TParam3>(methodInfo, 3, arg3);
                VerifyArgument<TParam4>(methodInfo, 4, arg4);

                try
                {
                    return InvokeUnsafe(arg0, arg1, arg2, arg3, arg4);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(arg0, arg1, arg2, arg3, arg4);
            }
        }

        protected override Type[] GetParameterTypes()
        {
            return new[] { typeof(TParam0), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4) };
        }

        protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions)
        {
        }

        protected override void CreateDelegate()
        {
        }
    }
}