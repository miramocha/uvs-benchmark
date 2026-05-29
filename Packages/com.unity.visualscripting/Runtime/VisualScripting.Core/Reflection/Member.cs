using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.VisualScripting
{
    [SerializationVersion("A")]
    public sealed class Member : ISerializationCallbackReceiver
    {
        public enum Source
        {
            Unknown,
            Field,
            Property,
            Method,
            Constructor
        }

        [Obsolete(Serialization.ConstructorWarning)]
        public Member() { }

        public Member(Type targetType, string name, Type[] parameterTypes = null)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);
            Ensure.That(nameof(name)).IsNotNull(name);

            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i] == null)
                    {
                        throw new ArgumentNullException(nameof(parameterTypes) + $"[{i}]");
                    }
                }
            }

            this.targetType = targetType;
            this.name = name;
            this.parameterTypes = parameterTypes;
        }

        public Member(Type targetType, FieldInfo fieldInfo)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);
            Ensure.That(nameof(fieldInfo)).IsNotNull(fieldInfo);

            source = Source.Field;
            this.fieldInfo = fieldInfo;
            this.targetType = targetType;
            name = fieldInfo.Name;
            parameterTypes = null;
            isReflected = true;
        }

        public Member(Type targetType, PropertyInfo propertyInfo)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);
            Ensure.That(nameof(propertyInfo)).IsNotNull(propertyInfo);

            source = Source.Property;
            this.propertyInfo = propertyInfo;
            this.targetType = targetType;
            name = propertyInfo.Name;
            parameterTypes = null;
            isReflected = true;
        }

        public Member(Type targetType, MethodInfo methodInfo)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);
            Ensure.That(nameof(methodInfo)).IsNotNull(methodInfo);

            source = Source.Method;
            this.methodInfo = methodInfo;
            this.targetType = targetType;
            name = methodInfo.Name;
            isExtension = methodInfo.IsExtension();
            isInvokedAsExtension = methodInfo.IsInvokedAsExtension(targetType);
            parameterTypes = methodInfo.GetInvocationParameters(_isInvokedAsExtension).Select(pi => pi.ParameterType).ToArray();
            isReflected = true;
        }

        public Member(Type targetType, ConstructorInfo constructorInfo)
        {
            Ensure.That(nameof(targetType)).IsNotNull(targetType);
            Ensure.That(nameof(constructorInfo)).IsNotNull(constructorInfo);

            source = Source.Constructor;
            this.constructorInfo = constructorInfo;
            this.targetType = targetType;
            name = constructorInfo.Name;
            parameterTypes = constructorInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
            isReflected = true;
        }

        [SerializeAs(nameof(name))]
        private string _name;

        [SerializeAs(nameof(parameterTypes))]
        private Type[] _parameterTypes;

        [SerializeAs(nameof(targetType))]
        private Type _targetType;

        [SerializeAs(nameof(targetTypeName))]
        private string _targetTypeName;

        [DoNotSerialize]
        private Source _source;

        [DoNotSerialize]
        private FieldInfo _fieldInfo;

        [DoNotSerialize]
        private PropertyInfo _propertyInfo;

        [DoNotSerialize]
        private MethodInfo _methodInfo;

        [DoNotSerialize]
        private ConstructorInfo _constructorInfo;

        [DoNotSerialize]
        private bool _isExtension;

        [DoNotSerialize]
        private bool _isInvokedAsExtension;

        [DoNotSerialize]
        private OptimizedAccessorBase fieldAccessor;

        [DoNotSerialize]
        private OptimizedAccessorBase propertyAccessor;

        [DoNotSerialize]
        private OptimizedInvokerBase methodInvoker;

        [DoNotSerialize]
        private OptimizedConstructorInvokerBase constructorInvoker;

        [DoNotSerialize]
        public Type targetType
        {
            get
            {
                return _targetType;
            }
            private set
            {
                if (value == targetType)
                {
                    return;
                }

                isReflected = false;

                _targetType = value;

                if (value == null)
                {
                    _targetTypeName = null;
                }
                else
                {
                    _targetTypeName = RuntimeCodebase.SerializeType(value);
                }
            }
        }

        [DoNotSerialize]
        public string targetTypeName => _targetTypeName;

        [DoNotSerialize]
        public string name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (value != name)
                {
                    isReflected = false;
                }

                _name = value;
            }
        }

        [DoNotSerialize]
        public bool isReflected { get; private set; }

        [DoNotSerialize]
        private bool hasValidated;

        [DoNotSerialize]
        public Source source
        {
            get
            {
                EnsureReflected();
                return _source;
            }
            private set
            {
                _source = value;
            }
        }

        [DoNotSerialize]
        public FieldInfo fieldInfo
        {
            get
            {
                EnsureReflected();
                return _fieldInfo;
            }
            private set
            {
                _fieldInfo = value;
            }
        }

        [DoNotSerialize]
        public PropertyInfo propertyInfo
        {
            get
            {
                EnsureReflected();
                return _propertyInfo;
            }
            private set
            {
                _propertyInfo = value;
            }
        }

        [DoNotSerialize]
        public MethodInfo methodInfo
        {
            get
            {
                EnsureReflected();
                return _methodInfo;
            }
            private set
            {
                _methodInfo = value;
            }
        }

        [DoNotSerialize]
        public ConstructorInfo constructorInfo
        {
            get
            {
                EnsureReflected();
                return _constructorInfo;
            }
            private set
            {
                _constructorInfo = value;
            }
        }

        [DoNotSerialize]
        public bool isExtension
        {
            get
            {
                EnsureReflected();
                return _isExtension;
            }
            private set
            {
                _isExtension = value;
            }
        }

        [DoNotSerialize]
        public bool isInvokedAsExtension
        {
            get
            {
                EnsureReflected();
                return _isInvokedAsExtension;
            }
            private set
            {
                _isInvokedAsExtension = value;
            }
        }

        [DoNotSerialize]
        public Type[] parameterTypes
        {
            get
            {
                return _parameterTypes;
            }
            private set
            {
                _parameterTypes = value;
                isReflected = false;
            }
        }

        public MethodBase methodBase
        {
            get
            {
                switch (source)
                {
                    case Source.Method:
                        return methodInfo;
                    case Source.Constructor:
                        return constructorInfo;
                    default:
                        return null;
                }
            }
        }

        private MemberInfo _info
        {
            get
            {
                switch (source)
                {
                    case Source.Field:
                        return _fieldInfo;
                    case Source.Property:
                        return _propertyInfo;
                    case Source.Method:
                        return _methodInfo;
                    case Source.Constructor:
                        return _constructorInfo;
                    default:
                        throw new UnexpectedEnumValueException<Source>(source);
                }
            }
        }

        public MemberInfo info
        {
            get
            {
                switch (source)
                {
                    case Source.Field:
                        return fieldInfo;
                    case Source.Property:
                        return propertyInfo;
                    case Source.Method:
                        return methodInfo;
                    case Source.Constructor:
                        return constructorInfo;
                    default:
                        throw new UnexpectedEnumValueException<Source>(source);
                }
            }
        }

        public Type type
        {
            get
            {
                switch (source)
                {
                    case Source.Field:
                        return fieldInfo.FieldType;
                    case Source.Property:
                        return propertyInfo.PropertyType;
                    case Source.Method:
                        return methodInfo.ReturnType;
                    case Source.Constructor:
                        return constructorInfo.DeclaringType;
                    default:
                        throw new UnexpectedEnumValueException<Source>(source);
                }
            }
        }

        public bool isCoroutine
        {
            get
            {
                if (!isGettable)
                {
                    return false;
                }

                return type == typeof(IEnumerator);
            }
        }

        public bool isYieldInstruction
        {
            get
            {
                if (!isGettable)
                {
                    return false;
                }

                return typeof(YieldInstruction).IsAssignableFrom(type);
            }
        }

        public bool isGettable => IsGettable(true);
        public bool isPubliclyGettable => IsGettable(false);

        public bool isSettable => IsSettable(true);
        public bool isPubliclySettable => IsSettable(false);

        public bool isInvocable => IsInvocable(true);
        public bool isPubliclyInvocable => IsInvocable(false);

        public bool isAccessor
        {
            get
            {
                switch (source)
                {
                    case Source.Field:
                        return true;
                    case Source.Property:
                        return true;
                    case Source.Method:
                        return false;
                    case Source.Constructor:
                        return false;
                    default:
                        throw new UnexpectedEnumValueException<Source>(source);
                }
            }
        }

        public bool isField => source == Source.Field;

        public bool isProperty => source == Source.Property;

        public bool isMethod => source == Source.Method;

        public bool isConstructor => source == Source.Constructor;

        [DoNotSerialize]
        private sbyte _requiresTargetCache = -1;

        public bool requiresTarget
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_requiresTargetCache != -1)
                {
                    return _requiresTargetCache == 1;
                }

                return ComputeRequiresTarget();
            }
        }

        private bool ComputeRequiresTarget()
        {
            bool result = source switch
            {
                Source.Field => !fieldInfo.IsStatic,
                Source.Constructor => false,
                Source.Method => !methodInfo.IsStatic || isInvokedAsExtension,
                Source.Property => CheckPropertyStatic(propertyInfo),
                _ => throw new UnexpectedEnumValueException<Source>(source)
            };

            _requiresTargetCache = result ? (sbyte)1 : (sbyte)0;
            return result;
        }

        private static bool CheckPropertyStatic(PropertyInfo pi)
        {
            var getter = pi.GetGetMethod(true);
            if (getter != null) return !getter.IsStatic;

            var setter = pi.GetSetMethod(true);
            return setter != null && !setter.IsStatic;
        }

        public bool isOperator => isMethod && methodInfo.IsOperator();

        public bool isConversion => isMethod && methodInfo.IsUserDefinedConversion();

        public int order => info.MetadataToken;

        public Type declaringType => info.ExtendedDeclaringType(isInvokedAsExtension);

        public bool isInherited => targetType != declaringType;

        public Type pseudoDeclaringType
        {
            get
            {
                // For Unity objects, we'll consider parent types to be only root types,
                // to allow common objects like BoxCollider to show Collider members as self-defined.
                // We'll also consider them as absolute roots, and therefore none of their members
                // should display as inherited.

                var declaringType = this.declaringType;

                if (typeof(UnityObject).IsAssignableFrom(targetType))
                {
                    if (targetType == typeof(GameObject) ||
                        targetType == typeof(Component) ||
                        targetType == typeof(ScriptableObject))
                    {
                        return targetType;
                    }
                    else
                    {
                        if (declaringType != typeof(UnityObject) &&
                            declaringType != typeof(GameObject) &&
                            declaringType != typeof(Component) &&
                            declaringType != typeof(MonoBehaviour) &&
                            declaringType != typeof(ScriptableObject) &&
                            declaringType != typeof(object))
                        {
                            return targetType;
                        }
                    }
                }

                return declaringType;
            }
        }

        public bool isPseudoInherited => targetType != pseudoDeclaringType || (isMethod && methodInfo.IsGenericExtension());

        public bool isIndexer => isProperty && propertyInfo.GetIndexParameters().Length > 0;

        public bool isPredictable => isField || info.HasAttribute<PredictableAttribute>();

        public bool allowsNull => isSettable && ((type.IsReferenceType() && info.HasAttribute<AllowsNullAttribute>()) || Nullable.GetUnderlyingType(type) != null);

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Attempt to preserve and restore the target type even if
            // it wasn't available during an assembly reload.
            if (targetType != null)
            {
                _targetTypeName = RuntimeCodebase.SerializeType(targetType);
            }
            else if (_targetTypeName != null)
            {
                try
                {
                    targetType = RuntimeCodebase.DeserializeType(_targetTypeName);
                }
                catch { }
            }
        }

        public bool IsGettable(bool nonPublic)
        {
            switch (source)
            {
                case Source.Field:
                    return nonPublic || fieldInfo.IsPublic;
                case Source.Property:
                    return propertyInfo.CanRead && (nonPublic || propertyInfo.GetGetMethod(false) != null);
                case Source.Method:
                    return methodInfo.ReturnType != typeof(void) && (nonPublic || methodInfo.IsPublic);
                case Source.Constructor:
                    return nonPublic || constructorInfo.IsPublic;
                default:
                    throw new UnexpectedEnumValueException<Source>(source);
            }
        }

        public bool IsSettable(bool nonPublic)
        {
            switch (source)
            {
                case Source.Field:
                    return !(fieldInfo.IsLiteral || fieldInfo.IsInitOnly) && (nonPublic || fieldInfo.IsPublic);
                case Source.Property:
                    return propertyInfo.CanWrite && (nonPublic || propertyInfo.GetSetMethod(false) != null);
                case Source.Method:
                    return false;
                case Source.Constructor:
                    return false;
                default:
                    throw new UnexpectedEnumValueException<Source>(source);
            }
        }

        public bool IsInvocable(bool nonPublic)
        {
            switch (source)
            {
                case Source.Field:
                    return false;
                case Source.Property:
                    return false;
                case Source.Method:
                    return nonPublic || methodInfo.IsPublic;
                case Source.Constructor:
                    return nonPublic || constructorInfo.IsPublic;
                default:
                    throw new UnexpectedEnumValueException<Source>(source);
            }
        }

        private void EnsureExplicitParameterTypes()
        {
            if (parameterTypes == null)
            {
                throw new InvalidOperationException("Missing parameter types.");
            }
        }

        public void Reflect()
        {
            // Cannot happen from the constructor, but will occur
            // if the type doesn't exist and fails to be deserialized
            if (targetType == null)
            {
                if (targetTypeName != null)
                {
                    throw new MissingMemberException(targetTypeName, name);
                }
                else
                {
                    throw new MissingMemberException("Target type not found.");
                }
            }

            _source = Source.Unknown;

            _fieldInfo = null;
            _propertyInfo = null;
            _methodInfo = null;
            _constructorInfo = null;

            fieldAccessor = null;
            propertyAccessor = null;
            methodInvoker = null;

            MemberInfo[] candidates;
            try
            {
                candidates = targetType.GetExtendedMember(name, SupportedMemberTypes, SupportedBindingFlags);
            }
            catch (NotSupportedException e)
            {
                throw new InvalidOperationException($"An error occured when trying to reflect the member '{name}' of the type '{targetType.FullName}' a '{GetType().Name}' unit. Supported member types: {SupportedMemberTypes}, supported binding flags: {SupportedBindingFlags}", e);
            }

            if (candidates.Length == 0) // Not found, check if it might have been renamed
            {
                var renamedMembers = RuntimeCodebase.RenamedMembers(targetType);

                string newName;

                if (renamedMembers.TryGetValue(name, out newName))
                {
                    name = newName;

                    try
                    {
                        candidates = targetType.GetExtendedMember(name, SupportedMemberTypes, SupportedBindingFlags);
                    }
                    catch (NotSupportedException e)
                    {
                        throw new InvalidOperationException($"An error occured when trying to reflect the renamed member '{name}' of the type '{targetType.FullName}' a '{GetType().Name}' unit. Supported member types: {SupportedMemberTypes}, supported binding flags: {SupportedBindingFlags}", e);
                    }
                }
            }

            if (candidates.Length == 0) // Nope, not even, abort
            {
                throw new MissingMemberException($"No matching member found: '{targetType.Name}.{name}'");
            }

            MemberTypes? memberType = null;

            foreach (var candidate in candidates)
            {
                if (memberType == null)
                {
                    memberType = candidate.MemberType;
                }
                else if (candidate.MemberType != memberType && !candidate.IsExtensionMethod())
                {
                    // This theoretically shouldn't happen according to the .NET specification, I believe
                    Debug.LogWarning($"Multiple members with the same name are of a different type: '{targetType.Name}.{name}'");
                    break;
                }
            }

            switch (memberType)
            {
                case MemberTypes.Field:
                    ReflectField(candidates);
                    break;

                case MemberTypes.Property:
                    ReflectProperty(candidates);
                    break;

                case MemberTypes.Method:
                    ReflectMethod(candidates);
                    break;

                case MemberTypes.Constructor:
                    ReflectConstructor(candidates);
                    break;

                default:
                    throw new UnexpectedEnumValueException<MemberTypes>(memberType.Value);
            }

            isReflected = true;

            hasValidated = false;
        }

        private void ReflectField(IEnumerable<MemberInfo> candidates)
        {
            _source = Source.Field;

            _fieldInfo = candidates.OfType<FieldInfo>().Disambiguate(targetType);

            if (_fieldInfo == null)
            {
                throw new MissingMemberException($"No matching field found: '{targetType.Name}.{name}'");
            }
        }

        private void ReflectProperty(IEnumerable<MemberInfo> candidates)
        {
            _source = Source.Property;

            _propertyInfo = candidates.OfType<PropertyInfo>().Disambiguate(targetType);

            if (_propertyInfo == null)
            {
                throw new MissingMemberException($"No matching property found: '{targetType.Name}.{name}'");
            }
        }

        private void ReflectConstructor(IEnumerable<MemberInfo> candidates)
        {
            _source = Source.Constructor;

            EnsureExplicitParameterTypes();

            // Exclude static constructors (type initializers) because calling them
            // is always a violation of types expecting it to be called only once.
            // http://stackoverflow.com/a/2524938
            _constructorInfo = candidates.OfType<ConstructorInfo>().Where(c => !c.IsStatic).Disambiguate(targetType, parameterTypes);

            if (_constructorInfo == null)
            {
                throw new MissingMemberException($"No matching constructor found: '{targetType.Name} ({parameterTypes.Select(t => t.Name).ToCommaSeparatedString()})'");
            }
        }

        private void ReflectMethod(IEnumerable<MemberInfo> candidates)
        {
            _source = Source.Method;

            EnsureExplicitParameterTypes();

            _methodInfo = candidates.OfType<MethodInfo>().Disambiguate(targetType, parameterTypes);

            if (_methodInfo == null)
            {
                throw new MissingMemberException($"No matching method found: '{targetType.Name}.{name} ({parameterTypes.Select(t => t.Name).ToCommaSeparatedString()})'\nCandidates:\n{candidates.ToLineSeparatedString()}");
            }

            _isExtension = _methodInfo.IsExtension();
            _isInvokedAsExtension = _methodInfo.IsInvokedAsExtension(targetType);
        }

        public void Prewarm()
        {
            if (fieldAccessor == null)
            {
                fieldAccessor = fieldInfo?.Prewarm();
            }

            if (propertyAccessor == null)
            {
                propertyAccessor = propertyInfo?.Prewarm();
            }

            if (methodInvoker == null)
            {
                methodInvoker = methodInfo?.Prewarm();
            }

            if (constructorInvoker == null)
            {
                constructorInvoker = constructorInfo?.Prewarm();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureReflected()
        {
            if (!isReflected)
            {
                Reflect();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureReady(object target)
        {
            EnsureReflected();

            var requiresTarget = this.requiresTarget;

            if (target == null && requiresTarget)
            {
                throw new InvalidOperationException($"Missing target object for '{targetType}.{name}'.");
            }
            else if (target != null && !requiresTarget)
            {
                throw new InvalidOperationException($"Superfluous target object for '{targetType}.{name}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureReady(ParameterValue target)
        {
            EnsureReflected();

            var requiresTarget = this.requiresTarget;

            var isNull = target.IsNull();

            if (isNull && requiresTarget)
            {
                throw new InvalidOperationException($"Missing target object for '{targetType}.{name}'.");
            }
            else if (!isNull && !requiresTarget)
            {
                throw new InvalidOperationException($"Superfluous target object for '{targetType}.{name}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Get(object target)
        {
            EnsureReady(target);

            if (source == Source.Field)
            {
                return (fieldAccessor ??= fieldInfo.Prewarm()).GetValue(target);
            }

            if (source == Source.Property)
            {
                return (propertyAccessor ??= propertyInfo.Prewarm()).GetValue(target);
            }

            return ThrowAccessorErrors();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Get(ParameterValue target)
        {
            EnsureReady(target);

            if (source == Source.Field)
            {
                var accessor = fieldAccessor ?? fieldInfo.Prewarm();
                return accessor.GetValue(target);
            }

            if (source == Source.Property)
            {
                var accessor = propertyAccessor ?? propertyInfo.Prewarm();
                return accessor.GetValue(target);
            }

            return ThrowAccessorErrors();
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue ThrowAccessorErrors()
        {
            if (source == Source.Method || source == Source.Constructor)
                throw new NotSupportedException("Member is a method or constructor. Use 'Invoke'.");
            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue GetRef(ref ParameterValue target)
        {
            EnsureReady(target);

            if (source == Source.Field)
            {
                var accessor = fieldAccessor ?? fieldInfo.Prewarm();
                return accessor.GetValueRef(ref target);
            }

            if (source == Source.Property)
            {
                var accessor = propertyAccessor ?? propertyInfo.Prewarm();
                return accessor.GetValueRef(ref target);
            }

            return ThrowAccessorErrors();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(object target)
        {
            return (T)Get(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(ParameterValue target)
        {
            return Get(target).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetRef<T>(ref ParameterValue target)
        {
            return GetRef(ref target).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Set(object target, object value)
        {
            EnsureReady(target);

            // When setting, we return the assigned value, not the updated field or property.
            // This is consistent with C# language behaviour: https://msdn.microsoft.com/en-us/library/sbkb459w.aspx
            // "The assignment operator (=) [...] returns the value as its result"
            // See confirmation here: https://dotnetfiddle.net/n4RZcW

            if (source == Source.Field)
            {
                var accessor = fieldAccessor ?? fieldInfo.Prewarm();
                accessor.SetValue(target, value);
                return value;
            }

            if (source == Source.Property)
            {
                var accessor = propertyAccessor ?? propertyInfo.Prewarm();
                accessor.SetValue(target, value);
                return value;
            }

            return ThrowAccessorErrors();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Set(ParameterValue target, ParameterValue value)
        {
            EnsureReady(target);

            // When setting, we return the assigned value, not the updated field or property.
            // This is consistent with C# language behaviour: https://msdn.microsoft.com/en-us/library/sbkb459w.aspx
            // "The assignment operator (=) [...] returns the value as its result"
            // See confirmation here: https://dotnetfiddle.net/n4RZcW

            if (source == Source.Field)
            {
                var accessor = fieldAccessor ?? fieldInfo.Prewarm();
                accessor.SetValue(target, value);
                return value;
            }

            if (source == Source.Property)
            {
                var accessor = propertyAccessor ?? propertyInfo.Prewarm();
                accessor.SetValue(target, value);
                return value;
            }

            return ThrowAccessorErrors();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue SetRef(ref ParameterValue target, ParameterValue value)
        {
            EnsureReady(target);

            // When setting, we return the assigned value, not the updated field or property.
            // This is consistent with C# language behaviour: https://msdn.microsoft.com/en-us/library/sbkb459w.aspx
            // "The assignment operator (=) [...] returns the value as its result"
            // See confirmation here: https://dotnetfiddle.net/n4RZcW

            if (source == Source.Field)
            {
                var accessor = fieldAccessor ?? fieldInfo.Prewarm();
                accessor.SetValueRef(ref target, value);
                return value;
            }

            if (source == Source.Property)
            {
                var accessor = propertyAccessor ?? propertyInfo.Prewarm();
                accessor.SetValueRef(ref target, value);
                return value;
            }

            return ThrowAccessorErrors();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInvocable(object target)
        {
            EnsureReady(target);

            if (hasValidated) return;

            ValidateInvocable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInvocable(ParameterValue target)
        {
            EnsureReady(target);

            if (hasValidated) return;

            ValidateInvocable();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ValidateInvocable()
        {
            if (source == Source.Field || source == Source.Property)
            {
                throw new NotSupportedException("Member is a field or property.");
            }
            else if (source == Source.Method)
            {
                if (methodInfo.ContainsGenericParameters)
                {
                    throw new NotSupportedException($"Trying to invoke an open-constructed generic method: '{methodInfo}'.");
                }

                if (methodInvoker == null)
                {
                    methodInvoker = methodInfo.Prewarm();
                }
            }
            else if (source == Source.Constructor)
            {
                if (constructorInfo.ContainsGenericParameters)
                {
                    throw new NotSupportedException($"Trying to invoke an open-constructed generic constructor: '{constructorInfo}'.");
                }

                if (constructorInvoker == null)
                {
                    constructorInvoker = constructorInfo.Prewarm();
                }
            }
            else
            {
                throw new UnexpectedEnumValueException<Source>(source);
            }

            hasValidated = true;
        }

        public IEnumerable<ParameterInfo> GetParameterInfos()
        {
            EnsureReflected();

            return methodBase.GetInvocationParameters(isInvokedAsExtension);
        }

        public object Invoke(object target)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target);
                }
                else
                {
                    return methodInvoker.Invoke(target);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null);
            }
        }

        public object Invoke(object target, object arg0)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target, arg0);
                }
                else
                {
                    return methodInvoker.Invoke(target, arg0);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null, arg0);
            }
        }

        public object Invoke(object target, object arg0, object arg1)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target, arg0, arg1);
                }
                else
                {
                    return methodInvoker.Invoke(target, arg0, arg1);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null, arg0, arg1);
            }
        }

        public object Invoke(object target, object arg0, object arg1, object arg2)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target, arg0, arg1, arg2);
                }
                else
                {
                    return methodInvoker.Invoke(target, arg0, arg1, arg2);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null, arg0, arg1, arg2);
            }
        }

        public object Invoke(object target, object arg0, object arg1, object arg2, object arg3)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target, arg0, arg1, arg2, arg3);
                }
                else
                {
                    return methodInvoker.Invoke(target, arg0, arg1, arg2, arg3);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null, arg0, arg1, arg2, arg3);
            }
        }

        public object Invoke(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    return methodInvoker.Invoke(null, target, arg0, arg1, arg2, arg3, arg4);
                }
                else
                {
                    return methodInvoker.Invoke(target, arg0, arg1, arg2, arg3, arg4);
                }
            }
            else // if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(null, arg0, arg1, arg2, arg3, arg4);
            }
        }

        public object Invoke(object target, params object[] arguments)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    var argumentsWithThis = new object[arguments.Length + 1];
                    argumentsWithThis[0] = target;
                    Array.Copy(arguments, 0, argumentsWithThis, 1, arguments.Length);
                    return methodInvoker.Invoke(null, argumentsWithThis);
                }
                else
                {
                    return methodInvoker.Invoke(target, arguments);
                }
            }
            else
            {
                return constructorInvoker.Invoke(null, arguments);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, Span<ParameterValue> arguments)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    Span<ParameterValue> argumentsWithThis = stackalloc ParameterValue[arguments.Length + 1];
                    argumentsWithThis[0] = target;
                    arguments.CopyTo(argumentsWithThis.Slice(1));
                    return methodInvoker.Invoke(ParameterValue.None, argumentsWithThis);
                }

                return methodInvoker.Invoke(target, arguments);
            }

            return InvokeConstructor(arguments);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, Span<ParameterValue> arguments)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                if (isInvokedAsExtension)
                {
                    Span<ParameterValue> argumentsWithThis = stackalloc ParameterValue[arguments.Length + 1];
                    argumentsWithThis[0] = target;
                    arguments.CopyTo(argumentsWithThis.Slice(1));
                    return methodInvoker.Invoke(ParameterValue.None, argumentsWithThis);
                }

                return methodInvoker.InvokeRef(ref target, arguments);
            }

            return InvokeConstructor(arguments);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor(Span<ParameterValue> arguments)
        {
            if (source != Source.Constructor)
            {
                throw new UnexpectedEnumValueException<Source>(source);
            }

            return constructorInvoker.Invoke(ParameterValue.None, arguments);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target)
                    : methodInvoker.Invoke(target);
            }

            return InvokeConstructor();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target)
                    : methodInvoker.InvokeRef(ref target);
            }

            return InvokeConstructor();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor()
        {
            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None);
            }

            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, ParameterValue arg0)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0)
                    : methodInvoker.Invoke(target, arg0);
            }

            return InvokeConstructor(arg0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0)
                    : methodInvoker.InvokeRef(ref target, arg0);
            }

            return InvokeConstructor(arg0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor(ParameterValue arg0)
        {
            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None, arg0);
            }
            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1)
                    : methodInvoker.Invoke(target, arg0, arg1);
            }

            return InvokeConstructor(arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1)
                    : methodInvoker.InvokeRef(ref target, arg0, arg1);
            }

            return InvokeConstructor(arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor(ParameterValue arg0, ParameterValue arg1)
        {
            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None, arg0, arg1);
            }

            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1, arg2)
                    : methodInvoker.Invoke(target, arg0, arg1, arg2);
            }

            return InvokeConstructor(arg0, arg1, arg2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1, arg2)
                    : methodInvoker.InvokeRef(ref target, arg0, arg1, arg2);
            }

            return InvokeConstructor(arg0, arg1, arg2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor(ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None, arg0, arg1, arg2);
            }

            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1, arg2, arg3)
                    : methodInvoker.Invoke(target, arg0, arg1, arg2, arg3);
            }

            return InvokeConstructor(arg0, arg1, arg2, arg3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            EnsureInvocable(target);

            if (source == Source.Method)
            {
                return isInvokedAsExtension
                    ? methodInvoker.Invoke(ParameterValue.None, target, arg0, arg1, arg2, arg3)
                    : methodInvoker.InvokeRef(ref target, arg0, arg1, arg2, arg3);
            }

            return InvokeConstructor(arg0, arg1, arg2, arg3);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeConstructor(ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None, arg0, arg1, arg2, arg3);
            }

            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue Invoke(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            EnsureInvocable(target);

            if (source == Source.Method && !isInvokedAsExtension)
            {
                return methodInvoker.Invoke(target, arg0, arg1, arg2, arg3, arg4);
            }

            return InvokeSlow(target, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParameterValue InvokeRef(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            EnsureInvocable(target);

            if (source == Source.Method && !isInvokedAsExtension)
            {
                return methodInvoker.InvokeRef(ref target, arg0, arg1, arg2, arg3, arg4);
            }

            return InvokeSlow(target, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ParameterValue InvokeSlow(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            if (source == Source.Method)
            {
                Span<ParameterValue> args = stackalloc ParameterValue[6];
                args[0] = target;
                args[1] = arg0;
                args[2] = arg1;
                args[3] = arg2;
                args[4] = arg3;
                args[5] = arg4;
                return methodInvoker.Invoke(ParameterValue.None, args);
            }

            if (source == Source.Constructor)
            {
                return constructorInvoker.Invoke(ParameterValue.None, arg0, arg1, arg2, arg3, arg4);
            }

            throw new UnexpectedEnumValueException<Source>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target)
        {
            return (T)Invoke(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, object arg0)
        {
            return (T)Invoke(target, arg0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, object arg0, object arg1)
        {
            return (T)Invoke(target, arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, object arg0, object arg1, object arg2)
        {
            return (T)Invoke(target, arg0, arg1, arg2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, object arg0, object arg1, object arg2, object arg3)
        {
            return (T)Invoke(target, arg0, arg1, arg2, arg3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            return (T)Invoke(target, arg0, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(object target, params object[] arguments)
        {
            return (T)Invoke(target, arguments);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, Span<ParameterValue> arguments)
        {
            return Invoke(target, arguments).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, Span<ParameterValue> arguments)
        {
            return InvokeRef(ref target, arguments).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target)
        {
            return Invoke(target).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, ParameterValue arg0)
        {
            return Invoke(target, arg0).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            return Invoke(target, arg0, arg1).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            return Invoke(target, arg0, arg1, arg2).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            return Invoke(target, arg0, arg1, arg2, arg3).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Invoke<T>(ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            return InvokeSlow(target, arg0, arg1, arg2, arg3, arg4).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target)
        {
            return InvokeRef(ref target).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, ParameterValue arg0)
        {
            return InvokeRef(ref target, arg0).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1)
        {
            return InvokeRef(ref target, arg0, arg1).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2)
        {
            return InvokeRef(ref target, arg0, arg1, arg2).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3)
        {
            return InvokeRef(ref target, arg0, arg1, arg2, arg3).Cast<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T InvokeRef<T>(ref ParameterValue target, ParameterValue arg0, ParameterValue arg1, ParameterValue arg2, ParameterValue arg3, ParameterValue arg4)
        {
            return InvokeRef(ref target, arg0, arg1, arg2, arg3, arg4).Cast<T>();
        }
        public override bool Equals(object obj)
        {
            var other = obj as Member;

            var equals = other != null &&
                targetType == other.targetType &&
                name == other.name;

            if (!equals)
            {
                return false;
            }

            var selfHasParameters = parameterTypes != null;
            var otherHasParameters = other.parameterTypes != null;

            if (selfHasParameters != otherHasParameters)
            {
                return false;
            }

            if (selfHasParameters /* && otherHasParameters */)
            {
                var selfCount = parameterTypes.Length;
                var otherCount = other.parameterTypes.Length;

                if (selfCount != otherCount)
                {
                    return false;
                }

                for (var i = 0; i < selfCount; i++)
                {
                    if (parameterTypes[i] != other.parameterTypes[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                hash = hash * 23 + (targetType?.GetHashCode() ?? 0);
                hash = hash * 23 + (name?.GetHashCode() ?? 0);

                if (parameterTypes != null)
                {
                    foreach (var parameterType in parameterTypes)
                    {
                        hash = hash * 23 + parameterType.GetHashCode();
                    }
                }
                else
                {
                    hash = hash * 23 + 0;
                }

                return hash;
            }
        }

        public static bool operator ==(Member a, Member b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Member a, Member b)
        {
            return !(a == b);
        }

        public string ToUniqueString()
        {
            var s = targetType.FullName + "." + this.name;

            if (parameterTypes != null)
            {
                s += "(";

                foreach (var parameterType in parameterTypes)
                {
                    s += parameterType.FullName;
                }

                s += ")";
            }

            return s;
        }

        public override string ToString()
        {
            return $"{targetType.CSharpName()}.{name}";
        }

        public Member ToDeclarer()
        {
            return new Member(declaringType, name, parameterTypes);
        }

        public Member ToPseudoDeclarer()
        {
            return new Member(pseudoDeclaringType, name, parameterTypes);
        }

        public const MemberTypes SupportedMemberTypes = MemberTypes.Property | MemberTypes.Field | MemberTypes.Method | MemberTypes.Constructor;

        public const BindingFlags SupportedBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static readonly object[] EmptyObjects = new object[0];
    }
}
