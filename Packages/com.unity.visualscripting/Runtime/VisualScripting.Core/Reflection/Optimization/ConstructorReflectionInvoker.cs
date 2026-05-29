using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Unity.VisualScripting
{
    public class ReflectionConstructorInvoker : OptimizedConstructorInvokerBase
    {
        private readonly Type[] parameterTypes;
        private readonly DelegateCompatiblity delegateCompatible;

        private readonly ThreadLocal<object[]> threadArgs;

        private static readonly object[] EmptyObjects = Array.Empty<object>();

        public ReflectionConstructorInvoker(ConstructorInfo constructorInfo, DelegateCompatiblity delegateCompatible) : base(constructorInfo)
        {
            if (OptimizedReflection.safeMode)
            {
                Ensure.That(nameof(constructorInfo)).IsNotNull(constructorInfo);
            }

            this.delegateCompatible = delegateCompatible;
            parameterTypes = GetParameterTypes();
            threadArgs = new ThreadLocal<object[]>(() => new object[parameterTypes.Length]);
        }

        private Func<object[], object> invoker;

        public override void Compile()
        {
            if (OptimizedReflection.useJit && delegateCompatible == DelegateCompatiblity.Compatible)
            {
                ParameterExpression argsExp = Expression.Parameter(typeof(object[]), "args");

                ParameterInfo[] paramsInfo = constructorInfo.GetParameters();
                Expression[] convertedArgs = new Expression[paramsInfo.Length];

                for (int i = 0; i < paramsInfo.Length; i++)
                {
                    Expression indexExp = Expression.Constant(i);
                    Expression accessorExp = Expression.ArrayIndex(argsExp, indexExp);
                    convertedArgs[i] = Expression.Convert(accessorExp, paramsInfo[i].ParameterType);
                }

                Expression newExpression = Expression.New(constructorInfo, convertedArgs);
                Expression castNewExpression = Expression.Convert(newExpression, typeof(object));

                invoker = Expression.Lambda<Func<object[], object>>(castNewExpression, argsExp).Compile();
            }
        }

        public override object Invoke(object target, params object[] args)
        {
            if (invoker != null)
            {
                return invoker(args);
            }

            object[] Args = threadArgs.Value;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    Args[i] = ConvertArg(args[i], i);
                }

                return constructorInfo.Invoke(Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        #region Individual Argument Overloads

        public override object Invoke(object target)
        {
            return constructorInfo.Invoke(EmptyObjects);
        }

        public override object Invoke(object target, object arg0)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0, 0);
                return Invoke(target, Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0, 0);
                Args[1] = ConvertArg(arg1, 1);
                return Invoke(target, Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0, 0);
                Args[1] = ConvertArg(arg1, 1);
                Args[2] = ConvertArg(arg2, 2);
                return Invoke(target, Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0, 0);
                Args[1] = ConvertArg(arg1, 1);
                Args[2] = ConvertArg(arg2, 2);
                Args[3] = ConvertArg(arg3, 3);
                return Invoke(target, Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0, 0);
                Args[1] = ConvertArg(arg1, 1);
                Args[2] = ConvertArg(arg2, 2);
                Args[3] = ConvertArg(arg3, 3);
                Args[4] = ConvertArg(arg4, 4);
                return Invoke(target, Args);
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        #endregion

        #region ParameterValue Overloads

        public override ParameterValue Invoke(ParameterValue target, Span<ParameterValue> args)
        {
            object[] Args = threadArgs.Value;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    Args[i] = ConvertArg(args[i].ToObject(), i);
                }

                if (invoker != null)
                {
                    return new ParameterValue(invoker(Args));
                }

                return new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target)
        {
            return new ParameterValue(constructorInfo.Invoke(EmptyObjects));
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0.ToObject(), 0);
                return invoker != null ? new ParameterValue(invoker(Args)) : new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0.ToObject(), 0);
                Args[1] = ConvertArg(arg1.ToObject(), 1);
                return invoker != null ? new ParameterValue(invoker(Args)) : new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0.ToObject(), 0);
                Args[1] = ConvertArg(arg1.ToObject(), 1);
                Args[2] = ConvertArg(arg2.ToObject(), 2);
                return invoker != null ? new ParameterValue(invoker(Args)) : new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0.ToObject(), 0);
                Args[1] = ConvertArg(arg1.ToObject(), 1);
                Args[2] = ConvertArg(arg2.ToObject(), 2);
                Args[3] = ConvertArg(arg3.ToObject(), 3);
                return invoker != null ? new ParameterValue(invoker(Args)) : new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        public override ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            object[] Args = threadArgs.Value;
            try
            {
                Args[0] = ConvertArg(arg0.ToObject(), 0);
                Args[1] = ConvertArg(arg1.ToObject(), 1);
                Args[2] = ConvertArg(arg2.ToObject(), 2);
                Args[3] = ConvertArg(arg3.ToObject(), 3);
                Args[4] = ConvertArg(arg4.ToObject(), 4);
                return invoker != null ? new ParameterValue(invoker(Args)) : new ParameterValue(constructorInfo.Invoke(Args));
            }
            finally
            {
                Array.Clear(Args, 0, Args.Length);
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ConvertArg(object value, int index)
        {
            Type targetType = parameterTypes[index];
            return ConversionUtility.Convert(value, targetType);
        }

        protected override Type[] GetParameterTypes()
        {
            return constructorInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
        }

        protected override void CreateDelegate() { }
        protected override void VerifyTarget(object target) { }
        protected override void VerifyTarget(ParameterValue target) { }

        protected override void CompileExpression()
        {
        }
    }
}