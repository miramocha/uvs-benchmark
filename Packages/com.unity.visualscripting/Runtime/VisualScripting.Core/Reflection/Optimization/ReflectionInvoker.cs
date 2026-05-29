using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Unity.VisualScripting
{
    public class ReflectionInvoker : OptimizedInvokerBase
    {
        private readonly Type[] parameterTypes;
        private readonly DelegateCompatiblity delegateCompatible;

        private readonly ThreadLocal<object[]> threadArgs;
        private static readonly object[] EmptyObjects = new object[0];

        public ReflectionInvoker(MethodInfo methodInfo, DelegateCompatiblity delegateCompatible) : base(methodInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(methodInfo)).IsNotNull(methodInfo);
            }

            this.delegateCompatible = delegateCompatible;
            parameterTypes = GetParameterTypes();
            threadArgs = new ThreadLocal<object[]>(() => new object[parameterTypes.Length]);
        }

        private Func<object, object[], object> invoker;

        public override void Compile()
        {
            if (OptimizedReflection.useJit && delegateCompatible == DelegateCompatiblity.Compatible)
            {
                ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
                ParameterExpression argsExp = Expression.Parameter(typeof(object[]), "args");

                ParameterInfo[] paramsInfo = methodInfo.GetParameters();
                Expression[] convertedArgs = new Expression[paramsInfo.Length];

                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    BinaryExpression indexExp = Expression.ArrayIndex(argsExp, Expression.Constant(i));
                    convertedArgs[i] = Expression.Convert(indexExp, paramsInfo[i].ParameterType);
                }

                Expression castTarget = methodInfo.IsStatic ? null : Expression.Convert(targetExp, methodInfo.DeclaringType);
                MethodCallExpression callExp = Expression.Call(castTarget, methodInfo, convertedArgs);

                Expression body;

                if (methodInfo.ReturnType == typeof(void))
                {
                    body = Expression.Block(
                        callExp,
                        Expression.Constant(null, typeof(object))
                    );
                }
                else
                {
                    body = Expression.Convert(callExp, typeof(object));
                }

                invoker = Expression.Lambda<Func<object, object[], object>>(body, targetExp, argsExp).Compile();
            }
        }

        public override object Invoke(object target)
        {
            if (invoker != null) return invoker(ConvertTarget(target), EmptyObjects);
            return methodInfo.Invoke(ConvertTarget(target), EmptyObjects);
        }

        public override object Invoke(object target, object arg0)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                object convertedTarget = ConvertTarget(target);

                if (invoker != null) return invoker(convertedTarget, localArgs);
                return methodInfo.Invoke(convertedTarget, localArgs);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                object convertedTarget = ConvertTarget(target);

                if (invoker != null) return invoker(convertedTarget, localArgs);
                return methodInfo.Invoke(convertedTarget, localArgs);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);
                object convertedTarget = ConvertTarget(target);

                if (invoker != null) return invoker(convertedTarget, localArgs);
                return methodInfo.Invoke(convertedTarget, localArgs);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);
                localArgs[3] = ConvertArg(arg3, 3);
                object convertedTarget = ConvertTarget(target);

                if (invoker != null) return invoker(convertedTarget, localArgs);
                return methodInfo.Invoke(convertedTarget, localArgs);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);
                localArgs[3] = ConvertArg(arg3, 3);
                localArgs[4] = ConvertArg(arg4, 4);
                object convertedTarget = ConvertTarget(target);

                if (invoker != null) return invoker(convertedTarget, localArgs);
                return methodInfo.Invoke(convertedTarget, localArgs);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override object Invoke(object target, params object[] args)
        {
            int count = args.Length;

            for (int i = 0; i < count; i++)
                args[i] = ConvertArg(args[i], i);

            if (invoker != null) return invoker(ConvertTarget(target), args);
            return methodInfo.Invoke(ConvertTarget(target), args);
        }

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                int count = args.Length;
                for (int i = 0; i < count; i++)
                    localArgs[i] = ConvertArg(args[i], i);

                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> args)
        {
            return Invoke(target, args);
        }

        public override ParameterValue Invoke(ParameterValue target)
        {
            object boxedTarget = ConvertTarget(target);

            object result = invoker != null
                ? invoker(boxedTarget, EmptyObjects)
                : methodInfo.Invoke(boxedTarget, EmptyObjects);

            return new ParameterValue(result);
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);

                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);
                localArgs[3] = ConvertArg(arg3, 3);

                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            object[] localArgs = threadArgs.Value;
            try
            {
                localArgs[0] = ConvertArg(arg0, 0);
                localArgs[1] = ConvertArg(arg1, 1);
                localArgs[2] = ConvertArg(arg2, 2);
                localArgs[3] = ConvertArg(arg3, 3);
                localArgs[4] = ConvertArg(arg4, 4);

                object boxedTarget = ConvertTarget(target);

                object result = invoker != null
                    ? invoker(boxedTarget, localArgs)
                    : methodInfo.Invoke(boxedTarget, localArgs);

                return new ParameterValue(result);
            }
            finally
            {
                Array.Clear(localArgs, 0, localArgs.Length);
            }
        }

        public override ParameterValue InvokeRef(ref ParameterValue target)
        {
            return Invoke(target);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0)
        {
            return Invoke(target, arg0);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            return Invoke(target, arg0, arg1);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            return Invoke(target, arg0, arg1, arg2);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            return Invoke(target, arg0, arg1, arg2, arg3);
        }

        public override ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            return Invoke(target, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertArg(ParameterValue value, int index)
        {
            object obj = value.ToObject();
            Type targetType = parameterTypes[index];

            return ConversionUtility.Convert(obj, targetType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertArg(object value, int index)
        {
            Type targetType = parameterTypes[index];

            return ConversionUtility.Convert(value, targetType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertTarget(ParameterValue target)
        {
            if (methodInfo.IsStatic)
            {
                return null;
            }

            Type targetType = methodInfo.DeclaringType;

            return ConversionUtility.Convert(target.ToObject(), targetType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertTarget(object target)
        {
            if (methodInfo.IsStatic)
            {
                return null;
            }

            Type targetType = methodInfo.DeclaringType;

            return ConversionUtility.Convert(target, targetType);
        }

        protected override Type[] GetParameterTypes()
        {
            return methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
        }

        protected override void CompileExpression() { }
        protected override void CreateDelegate() { }
        protected override void VerifyTarget(object target) { }
        protected override void VerifyTarget(ParameterValue target) { }
    }
}
