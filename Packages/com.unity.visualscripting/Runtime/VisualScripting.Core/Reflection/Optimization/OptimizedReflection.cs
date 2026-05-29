using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Weather or not the methodInfo can be compiled into a delegate using Expression
    /// </summary>
    public enum DelegateCompatiblity
    {
        Compatible,
        Incompatible,
    }

    // Inspirations:
    // http://stackoverflow.com/a/26733318
    // http://stackoverflow.com/a/16136854
    // http://stackoverflow.com/a/321686

    public static class OptimizedReflection
    {
        static OptimizedReflection()
        {
            fieldAccessors = new Dictionary<FieldInfo, OptimizedAccessorBase>();
            propertyAccessors = new Dictionary<PropertyInfo, OptimizedAccessorBase>();
            methodInvokers = new Dictionary<MethodInfo, OptimizedInvokerBase>();
            constructorInvokers = new Dictionary<ConstructorInfo, OptimizedConstructorInvokerBase>();

            jitAvailable = PlatformUtility.supportsJit;
        }

        private static readonly Dictionary<FieldInfo, OptimizedAccessorBase> fieldAccessors;
        private static readonly Dictionary<PropertyInfo, OptimizedAccessorBase> propertyAccessors;
        private static readonly Dictionary<MethodInfo, OptimizedInvokerBase> methodInvokers;
        private static readonly Dictionary<ConstructorInfo, OptimizedConstructorInvokerBase> constructorInvokers;

        public static readonly bool jitAvailable;

        private static bool _useJitIfAvailable = true;

        internal static bool useJit => useJitIfAvailable && jitAvailable;

        public static bool useJitIfAvailable
        {
            get
            {
                return _useJitIfAvailable;
            }
            set
            {
                _useJitIfAvailable = value;
                ClearCache();
            }
        }

        public static bool safeMode { get; set; }

        internal static void OnRuntimeMethodLoad()
        {
            safeMode = Application.isEditor || Debug.isDebugBuild;
        }

        public static void ClearCache()
        {
            fieldAccessors.Clear();
            propertyAccessors.Clear();
            methodInvokers.Clear();
            constructorInvokers.Clear();
        }

        internal static void VerifyStaticTarget(Type targetType, object target)
        {
            VerifyTarget(targetType, target, true);
        }

        internal static void VerifyInstanceTarget<TTArget>(object target)
        {
            VerifyTarget(typeof(TTArget), target, false);
        }

        private static void VerifyTarget(Type targetType, object target, bool @static)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);

            if (@static)
            {
                if (target != null)
                {
                    throw new TargetException($"Superfluous target object for '{targetType}'.");
                }
            }
            else
            {
                if (target == null)
                {
                    throw new TargetException($"Missing target object for '{targetType}'.");
                }

                if (!targetType.IsAssignableFrom(targetType))
                {
                    throw new TargetException($"The target object does not match the target type.\nProvided: {target.GetType()}\nExpected: {targetType}");
                }
            }
        }

        internal static void VerifyStaticTarget(Type targetType, ParameterValue target)
        {
            VerifyTarget(targetType, target, true);
        }

        internal static void VerifyInstanceTarget<TTArget>(ParameterValue target)
        {
            VerifyTarget(typeof(TTArget), target, false);
        }

        private static void VerifyTarget(Type targetType, ParameterValue target, bool @static)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);

            if (@static)
            {
                if (!target.IsNull())
                {
                    throw new TargetException($"Superfluous target object for '{targetType}'.");
                }
            }
            else
            {
                if (target.IsNull())
                {
                    throw new TargetException($"Missing target object for '{targetType}'.");
                }

                if (!targetType.IsAssignableFrom(targetType))
                {
                    throw new TargetException($"The target object does not match the target type.\nProvided: {target.GetValueType()}\nExpected: {targetType}");
                }
            }
        }

        #region Fields

        public static OptimizedAccessorBase Prewarm(this FieldInfo fieldInfo)
        {
            return GetFieldAccessor(fieldInfo);
        }

        public static object GetValueOptimized(this FieldInfo fieldInfo, object target)
        {
            return GetFieldAccessor(fieldInfo).GetValue(target);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue GetValueOptimized(this FieldInfo fieldInfo, ParameterValue target)
        {
            return GetFieldAccessor(fieldInfo).GetValue(target);
        }

        public static void SetValueOptimized(this FieldInfo fieldInfo, object target, object value)
        {
            GetFieldAccessor(fieldInfo).SetValue(target, value);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static void SetValueOptimized(this FieldInfo fieldInfo, ParameterValue target, ParameterValue value)
        {
            GetFieldAccessor(fieldInfo).SetValue(target, value);
        }

        private static OptimizedAccessorBase GetFieldAccessor(FieldInfo fieldInfo)
        {
            Ensure.That(nameof(fieldInfo)).IsNotNull(fieldInfo);

            lock (fieldAccessors)
            {
                if (!fieldAccessors.TryGetValue(fieldInfo, out var accessor))
                {
                    Type accessorType;

                    if (fieldInfo.IsStatic)
                    {
                        accessorType = typeof(StaticFieldAccessor<>).MakeGenericType(fieldInfo.FieldType);
                    }
                    else
                    {
                        if (IsStructTarget(fieldInfo))
                            accessorType = typeof(StructInstanceFieldAccessor<,>).MakeGenericType(fieldInfo.DeclaringType, fieldInfo.FieldType);
                        else
                            accessorType = typeof(InstanceFieldAccessor<,>).MakeGenericType(fieldInfo.DeclaringType, fieldInfo.FieldType);
                    }

                    accessor = (OptimizedAccessorBase)Activator.CreateInstance(accessorType, fieldInfo);

                    accessor.Compile();

                    fieldAccessors.Add(fieldInfo, accessor);
                }

                return accessor;
            }
        }

        #endregion

        #region Properties

        public static OptimizedAccessorBase Prewarm(this PropertyInfo propertyInfo)
        {
            return GetPropertyAccessor(propertyInfo);
        }

        public static object GetValueOptimized(this PropertyInfo propertyInfo, object target)
        {
            return GetPropertyAccessor(propertyInfo).GetValue(target);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue GetValueOptimized(this PropertyInfo propertyInfo, ParameterValue target)
        {
            return GetPropertyAccessor(propertyInfo).GetValue(target);
        }

        public static void SetValueOptimized(this PropertyInfo propertyInfo, object target, object value)
        {
            GetPropertyAccessor(propertyInfo).SetValue(target, value);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static void SetValueOptimized(this PropertyInfo propertyInfo, ParameterValue target, ParameterValue value)
        {
            GetPropertyAccessor(propertyInfo).SetValue(target, value);
        }

        private static OptimizedAccessorBase GetPropertyAccessor(PropertyInfo propertyInfo)
        {
            Ensure.That(nameof(propertyInfo)).IsNotNull(propertyInfo);
            lock (propertyAccessors)
            {
                if (!propertyAccessors.TryGetValue(propertyInfo, out var accessor))
                {
                    Type accessorType;

                    if (propertyInfo.IsStatic())
                    {
                        accessorType = typeof(StaticPropertyAccessor<>).MakeGenericType(propertyInfo.PropertyType);
                    }
                    else
                    {
                        if (IsStructTarget(propertyInfo))
                            accessorType = typeof(StructInstancePropertyAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
                        else
                            accessorType = typeof(InstancePropertyAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
                    }

                    accessor = (OptimizedAccessorBase)Activator.CreateInstance(accessorType, propertyInfo);

                    accessor.Compile();

                    propertyAccessors.Add(propertyInfo, accessor);
                }

                return accessor;
            }
        }

        #endregion

        #region Methods

        public static OptimizedConstructorInvokerBase Prewarm(this ConstructorInfo constructorInfo)
        {
            return GetConstructorInvoker(constructorInfo);
        }

        public static OptimizedInvokerBase Prewarm(this MethodInfo methodInfo)
        {
            return GetMethodInvoker(methodInfo);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target)
        {
            return GetMethodInvoker(methodInfo).Invoke(target);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target)
        {
            return GetMethodInvoker(methodInfo).Invoke(target);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target, object arg0)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target, ParameterValue arg0)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target, object arg0, object arg1)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target, object arg0, object arg1, object arg2)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target, object arg0, object arg1, object arg2, object arg3)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2, arg3);
        }

        public static object InvokeOptimized(this MethodInfo methodInfo, object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// If using a object value you should manually free the index using <see cref="ParameterValue.FreeObject(int)"/>
        /// once done with the value.
        /// </summary>
        public static ParameterValue InvokeOptimized(this MethodInfo methodInfo, ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            return GetMethodInvoker(methodInfo).Invoke(target, arg0, arg1, arg2, arg3, arg4);
        }

        public static bool SupportsOptimization(this MethodInfo methodInfo, out DelegateCompatiblity delegateCompatible)
        {
            var parameters = methodInfo.GetParameters();

            if (parameters.Any(parameter => parameter.ParameterType.IsByRef || parameter.ParameterType.IsPointer))
            {
                delegateCompatible = DelegateCompatiblity.Incompatible;
                return false;
            }

            // I will test this but according to https://issuetracker-mig.prd.it.unity3d.com/issues/createdelegate-does-not-handle-virtual-methods-properly-in-il2cpp
            // this has been fixed?
            // 
            // // CreateDelegate in IL2CPP does not work properly for overridden methods, instead referring to the virtual method.
            // // https://support.ludiq.io/forums/5-bolt/topics/872-virtual-method-overrides-not-used-on-aot/
            // // https://fogbugz.unity3d.com/default.asp?980136_228np3be9idtbdtt
            // if (!jitAvailable && methodInfo.IsVirtual && !methodInfo.IsFinal)
            // {
            //     return false;
            // }

            // Undocumented __arglist keyword as used in the 4+ overload of String.Concat causes runtime crash
            if (methodInfo.CallingConvention == CallingConventions.VarArgs)
            {
                delegateCompatible = DelegateCompatiblity.Incompatible;
                return false;
            }

            if (parameters.Length > 5)
            {
                delegateCompatible = DelegateCompatiblity.Compatible;
                return false;
            }

            delegateCompatible = DelegateCompatiblity.Compatible;
            return true;
        }

        public static bool SupportsOptimization(this ConstructorInfo constructorInfo, out DelegateCompatiblity delegateCompatible)
        {
            Type declaringType = constructorInfo.DeclaringType;

            if (declaringType == null || declaringType.IsAbstract || declaringType.ContainsGenericParameters)
            {
                delegateCompatible = DelegateCompatiblity.Incompatible;
                return false;
            }

            var parameters = constructorInfo.GetParameters();

            if (constructorInfo.CallingConvention == CallingConventions.VarArgs)
            {
                delegateCompatible = DelegateCompatiblity.Incompatible;
                return false;
            }

            if (parameters.Any(p => p.ParameterType.IsByRef || p.ParameterType.IsPointer))
            {
                delegateCompatible = DelegateCompatiblity.Incompatible;
                return false;
            }

            if (parameters.Length > 5)
            {
                delegateCompatible = DelegateCompatiblity.Compatible;
                return false;
            }

            delegateCompatible = DelegateCompatiblity.Compatible;
            return true;
        }

        private static OptimizedInvokerBase GetMethodInvoker(MethodInfo methodInfo)
        {
            Ensure.That(nameof(methodInfo)).IsNotNull(methodInfo);

            lock (methodInvokers)
            {
                if (!methodInvokers.TryGetValue(methodInfo, out var invoker))
                {
                    var parameters = methodInfo.GetParameters();
                    if (SupportsOptimization(methodInfo, out DelegateCompatiblity delegateCompatible))
                    {
                        Type invokerType;

                        if (methodInfo.ReturnType == typeof(void))
                        {
                            if (methodInfo.IsStatic)
                            {
                                if (parameters.Length == 0)
                                {
                                    invokerType = typeof(StaticActionInvoker);
                                }
                                else if (parameters.Length == 1)
                                {
                                    invokerType = typeof(StaticActionInvoker<>).MakeGenericType(parameters[0].ParameterType);
                                }
                                else if (parameters.Length == 2)
                                {
                                    invokerType = typeof(StaticActionInvoker<,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType);
                                }
                                else if (parameters.Length == 3)
                                {
                                    invokerType = typeof(StaticActionInvoker<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
                                }
                                else if (parameters.Length == 4)
                                {
                                    invokerType = typeof(StaticActionInvoker<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
                                }
                                else if (parameters.Length == 5)
                                {
                                    invokerType = typeof(StaticActionInvoker<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            else
                            {
                                var isStructTarget = IsStructTarget(methodInfo);

                                if (parameters.Length == 0)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<>).MakeGenericType(methodInfo.DeclaringType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<>).MakeGenericType(methodInfo.DeclaringType);
                                }
                                else if (parameters.Length == 1)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType);
                                }
                                else if (parameters.Length == 2)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType);
                                }
                                else if (parameters.Length == 3)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
                                }
                                else if (parameters.Length == 4)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
                                }
                                else if (parameters.Length == 5)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceActionInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
                                    else
                                        invokerType = typeof(InstanceActionInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                        }
                        else
                        {
                            if (methodInfo.IsStatic)
                            {
                                if (parameters.Length == 0)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<>).MakeGenericType(methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 1)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<,>).MakeGenericType(parameters[0].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 2)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 3)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 4)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 5)
                                {
                                    invokerType = typeof(StaticFunctionInvoker<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, methodInfo.ReturnType);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            else
                            {
                                var isStructTarget = IsStructTarget(methodInfo);

                                if (parameters.Length == 0)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,>).MakeGenericType(methodInfo.DeclaringType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,>).MakeGenericType(methodInfo.DeclaringType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 1)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 2)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 3)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 4)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, methodInfo.ReturnType);
                                }
                                else if (parameters.Length == 5)
                                {
                                    if (isStructTarget)
                                        invokerType = typeof(StructInstanceFunctionInvoker<,,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, methodInfo.ReturnType);
                                    else
                                        invokerType = typeof(InstanceFunctionInvoker<,,,,,,>).MakeGenericType(methodInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, methodInfo.ReturnType);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                        }

                        invoker = (OptimizedInvokerBase)Activator.CreateInstance(invokerType, methodInfo);
                    }
                    else
                    {
                        invoker = new ReflectionInvoker(methodInfo, delegateCompatible);
                    }

                    invoker.Compile();

                    methodInvokers.Add(methodInfo, invoker);
                }

                return invoker;
            }
        }

        private static OptimizedConstructorInvokerBase GetConstructorInvoker(ConstructorInfo constructorInfo)
        {
            Ensure.That(nameof(constructorInfo)).IsNotNull(constructorInfo);

            lock (constructorInvokers)
            {
                if (!constructorInvokers.TryGetValue(constructorInfo, out var invoker))
                {
                    var parameters = constructorInfo.GetParameters();
                    if (SupportsOptimization(constructorInfo, out DelegateCompatiblity delegateCompatible))
                    {
                        Type invokerType;

                        switch (parameters.Length)
                        {
                            case 0:
                                invokerType = typeof(ConstructorInvoker<>).MakeGenericType(constructorInfo.DeclaringType);
                                break;

                            case 1:
                                invokerType = typeof(ConstructorInvoker<,>).MakeGenericType(constructorInfo.DeclaringType, parameters[0].ParameterType);
                                break;

                            case 2:
                                invokerType = typeof(ConstructorInvoker<,,>).MakeGenericType(constructorInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType);
                                break;

                            case 3:
                                invokerType = typeof(ConstructorInvoker<,,,>).MakeGenericType(constructorInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType);
                                break;

                            case 4:
                                invokerType = typeof(ConstructorInvoker<,,,,>).MakeGenericType(constructorInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType);
                                break;

                            case 5:
                                invokerType = typeof(ConstructorInvoker<,,,,,>).MakeGenericType(constructorInfo.DeclaringType, parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType);
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        invoker = (OptimizedConstructorInvokerBase)Activator.CreateInstance(invokerType, constructorInfo);
                    }
                    else
                    {
                        invoker = new ReflectionConstructorInvoker(constructorInfo, delegateCompatible);
                    }

                    invoker.Compile();

                    constructorInvokers.Add(constructorInfo, invoker);
                }

                return invoker;
            }
        }

        private static bool IsStructTarget(MemberInfo memberInfo)
        {
            return memberInfo.DeclaringType.IsValueType && !memberInfo.IsStatic();
        }

        #endregion
    }
}
