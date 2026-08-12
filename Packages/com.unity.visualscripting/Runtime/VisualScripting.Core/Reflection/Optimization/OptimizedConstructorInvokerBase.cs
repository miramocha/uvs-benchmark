using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Unity.VisualScripting
{
    public abstract class OptimizedConstructorInvokerBase
    {
        protected OptimizedConstructorInvokerBase(ConstructorInfo constructorInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                if (constructorInfo == null)
                {
                    throw new ArgumentNullException(nameof(constructorInfo));
                }
            }

            this.constructorInfo = constructorInfo;
            targetType = constructorInfo.DeclaringType;
        }

        protected readonly Type targetType;
        protected readonly ConstructorInfo constructorInfo;

        protected void VerifyArgument<TParam>(ConstructorInfo constructorInfo, int argIndex, object arg)
        {
            if (!typeof(TParam).IsAssignableFrom(arg))
            {
                throw new ArgumentException($"The provided argument value for '{targetType}.{constructorInfo.Name}' does not match the parameter type.\nProvided: {arg?.GetType().ToString() ?? "null"}\nExpected: {typeof(TParam)}", constructorInfo.GetParameters()[argIndex].Name);
            }
        }

        protected void VerifyArgument<TParam>(ConstructorInfo constructorInfo, int argIndex, ParameterValue arg)
        {
            if (!arg.IsAssignableFrom<TParam>())
            {
                throw new ArgumentException($"The provided argument value for '{targetType}.{constructorInfo.Name}' does not match the parameter type.\nProvided: {arg.GetValueType()?.ToString() ?? "null"}\nExpected: {typeof(TParam)}", constructorInfo.GetParameters()[argIndex].Name);
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
            var constructorParameters = constructorInfo.GetParameters();
            var parameterTypes = GetParameterTypes();

            if (constructorParameters.Length != parameterTypes.Length)
            {
                throw new ArgumentException("Parameter count of constructor info doesn't match generic argument count.", nameof(constructorInfo));
            }

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] != constructorParameters[i].ParameterType)
                {
                    throw new ArgumentException("Parameter type of constructor info doesn't match generic argument.", nameof(constructorInfo));
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

        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public abstract ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args);

        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target, object arg0) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target, object arg0, object arg1) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target, object arg0, object arg1, object arg2) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target, object arg0, object arg1, object arg2, object arg3) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result. target is ignored for constructors and can be passed as null.
        /// </summary>
        public virtual object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4) => throw new TargetParameterCountException();

        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3) => throw new TargetParameterCountException();
        /// <summary>
        /// Invokes the constructor with the provided arguments and returns the result as a ParameterValue. target is ignored for constructors and can be passed as <see cref="ParameterValue.Null"/>.
        /// </summary>
        public virtual ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4) => throw new TargetParameterCountException();

        protected abstract void CompileExpression();
        protected abstract void CreateDelegate();
        protected abstract void VerifyTarget(object target);
        protected abstract void VerifyTarget(ParameterValue target);
    }
}