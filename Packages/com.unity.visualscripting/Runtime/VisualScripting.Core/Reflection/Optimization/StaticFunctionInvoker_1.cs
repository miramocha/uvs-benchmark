using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    public sealed class StaticFunctionInvoker<TParam0, TResult> : StaticFunctionInvokerBase<TResult>
    {
        public unsafe StaticFunctionInvoker(MethodInfo methodInfo) : base(methodInfo)
        {
            invoke = (delegate*<TParam0, TResult>)methodInfo.MethodHandle.GetFunctionPointer();
        }

        private readonly unsafe delegate*<TParam0, TResult> invoke;

        public override object Invoke(object target, params object[] args)
        {
            if (args.Length != 1)
                throw new TargetParameterCountException();

            return Invoke(target, args[0]);
        }

        public override object Invoke(object target, object arg0)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);

                try
                {
                    return InvokeUnsafe(target, arg0);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(target, arg0);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0]);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args[0]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object InvokeUnsafe(object target, object arg0)
        {
            return invoke((TParam0)arg0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ParameterValue InvokeUnsafe(ParameterValue arg0)
        {
            return ParameterValue.Create(invoke(arg0.Cast<TParam0>()));
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0)
        {
            if (OptimizedReflection.safeMode)
            {
                VerifyTarget(target);
                VerifyArgument<TParam0>(methodInfo, 0, arg0);

                try
                {
                    return InvokeUnsafe(arg0);
                }
                catch (TargetInvocationException) { throw; }
                catch (Exception ex) { throw new TargetInvocationException(ex); }
            }
            else
            {
                return InvokeUnsafe(arg0);
            }
        }

        protected override Type[] GetParameterTypes()
        {
            return new[] { typeof(TParam0) };
        }

        protected override void CompileExpression(MethodCallExpression callExpression, ParameterExpression[] parameterExpressions)
        {
        }

        protected override void CreateDelegate()
        {
        }
    }
}