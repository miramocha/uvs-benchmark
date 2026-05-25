using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public abstract class OptimizedInvokerBase
    {
        protected OptimizedInvokerBase(MethodInfo methodInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                if (methodInfo == null)
                {
                    throw new ArgumentNullException(nameof(methodInfo));
                }
            }

            this.methodInfo = methodInfo;
            targetType = methodInfo.DeclaringType;
        }

        protected readonly Type targetType;
        protected readonly MethodInfo methodInfo;

        protected void VerifyArgument<TParam>(MethodInfo methodInfo, int argIndex, object arg)
        {
            if (!typeof(TParam).IsAssignableFrom(arg))
            {
                throw new ArgumentException($"The provided argument value for '{targetType}.{methodInfo.Name}' does not match the parameter type.\nProvided: {arg?.GetType().ToString() ?? "null"}\nExpected: {typeof(TParam)}", methodInfo.GetParameters()[argIndex].Name);
            }
        }

        protected void VerifyArgument<TParam>(MethodInfo methodInfo, int argIndex, ParameterValue arg)
        {
            if (!arg.IsAssignableFrom<TParam>())
            {
                throw new ArgumentException($"The provided argument value for '{targetType}.{methodInfo.Name}' does not match the parameter type.\nProvided: {arg.GetValueType()?.ToString() ?? "null"}\nExpected: {typeof(TParam)}", methodInfo.GetParameters()[argIndex].Name);
            }
        }

        public virtual void Compile()
        {
            if (OptimizedReflection.useJit)
            {
                CompileExpression();
            }
            else
            {
                CreateDelegate();
            }
        }

        protected ParameterExpression[] GetParameterExpressions()
        {
            var methodParameters = methodInfo.GetParameters();
            var parameterTypes = GetParameterTypes();

            if (methodParameters.Length != parameterTypes.Length)
            {
                throw new ArgumentException("Parameter count of method info doesn't match generic argument count.", nameof(methodInfo));
            }

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] != methodParameters[i].ParameterType)
                {
                    throw new ArgumentException("Parameter type of method info doesn't match generic argument.", nameof(methodInfo));
                }
            }

            var parameterExpressions = new ParameterExpression[parameterTypes.Length];

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                parameterExpressions[i] = Expression.Parameter(parameterTypes[i], "parameter" + i);
            }

            return parameterExpressions;
        }

        protected abstract Type[] GetParameterTypes();

        public abstract object Invoke(object target, params object[] args);
        public abstract ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args);
        public abstract ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args);

        public virtual object Invoke(object target)
        {
            throw new TargetParameterCountException();
        }

        public virtual object Invoke(object target, object arg0)
        {
            throw new TargetParameterCountException();
        }

        public virtual object Invoke(object target, object arg0, object arg1)
        {
            throw new TargetParameterCountException();
        }

        public virtual object Invoke(object target, object arg0, object arg1, object arg2)
        {
            throw new TargetParameterCountException();
        }

        public virtual object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            throw new TargetParameterCountException();
        }

        public virtual object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            throw new TargetParameterCountException();
        }

        public virtual ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            throw new TargetParameterCountException();
        }

        protected abstract void CompileExpression();

        protected abstract void CreateDelegate();

        protected abstract void VerifyTarget(object target);
        protected abstract void VerifyTarget(ParameterValue target);
    }
}
