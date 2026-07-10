using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Invokes a method or a constructor via reflection.
    /// </summary>
    public unsafe sealed class InvokeMember : MemberUnit
    {
        public InvokeMember() : base() { }

        public InvokeMember(Member member) : base(member) { }

        private bool useExpandedParameters;

        /// <summary>
        /// Whether the target should be output to allow for chaining.
        /// </summary>
        [Serialize]
        [InspectableIf(nameof(supportsChaining))]
        public bool chainable { get; set; }

        [DoNotSerialize]
        public bool supportsChaining => member.requiresTarget;

        [DoNotSerialize]
        [MemberFilter(Methods = true, Constructors = true)]
        public Member invocation
        {
            get { return member; }
            set { member = value; }
        }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput enter { get; private set; }

        /// <summary>
        /// The target object used when setting the value.
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Target")]
        [PortLabelHidden]
        public ValueOutput targetOutput { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ValueOutput result { get; private set; }

        [DoNotSerialize]
        public ValueInput[] inputParameters { get; private set; }

        [DoNotSerialize]
        public ValueOutput[] outputParameters { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlOutput exit { get; private set; }

        [DoNotSerialize]
        private int parameterCount;

        [Serialize]
        internal List<string> parameterNames;

        public override bool HandleDependencies()
        {
            if (!base.HandleDependencies())
                return false;

            // Here we have a chance to do a bit of post processing after deserialization of this node has occured.

            // In the past we did not serialize parameter names explicitly (only parameter types), however, if we have
            // exactly the same number of defaults as parameters, we happen to know what the original parameter names were.
            // Note there is one specific exception that must be handled carefully, the base class (MemberUnit) adds a
            // default value for the "target" (aka. the "this" instance) of the invocation; this does not correspond to
            // a real parameter member so it is excluded here when trying to reconstruct the missing parameter names.
            if (parameterNames == null && member.parameterTypes.Length == defaultValues.Count(d => d.Key != nameof(target)))
            {
                // Note that we strip the "%" prefix from the parameter name in the default values (the "%" denotes that
                // it is a parameter input)
                parameterNames = defaultValues
                    .Where(d => d.Key != nameof(target))
                    .Select(defaultValue => defaultValue.Key.Substring(1))
                    .ToList();
            }

            return true;
        }

        protected override void Definition()
        {
            base.Definition();

            useExpandedParameters = true;

            enter = ControlInput(nameof(enter), Enter);
            exit = ControlOutput(nameof(exit));
            Succession(enter, exit);

            if (member.requiresTarget)
            {
                Requirement(target, enter);
            }

            if (supportsChaining && chainable)
            {
                targetOutput = ValueOutput(member.targetType, nameof(targetOutput));
                Assignment(enter, targetOutput);
            }

            if (member.isGettable)
            {
                result = ValueOutput(member.type, nameof(result), Result);

                if (member.requiresTarget)
                {
                    Requirement(target, result);
                }
            }

            var parameterInfos = member.GetParameterInfos().ToArray();

            parameterCount = parameterInfos.Length;

            inputParameters = new ValueInput[parameterCount];
            outputParameters = new ValueOutput[parameterCount];

            bool needsParameterRemapping = false;
            for (int parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var parameterInfo = parameterInfos[parameterIndex];

                var parameterType = parameterInfo.UnderlyingParameterType();

                if (!parameterInfo.HasOutModifier())
                {
                    var inputParameterKey = "%" + parameterInfo.Name;

                    // Changes in parameter names are tolerated, use the old parameter naming for now and fix it later.
                    if (parameterNames != null && parameterNames[parameterIndex] != parameterInfo.Name)
                    {
                        inputParameterKey = "%" + parameterNames[parameterIndex];
                        needsParameterRemapping = true;
                    }

                    var inputParameter = ValueInput(parameterType, inputParameterKey);

                    inputParameters[parameterIndex] = inputParameter;

                    inputParameter.SetDefaultValue(parameterInfo.PseudoDefaultValue());

                    if (parameterInfo.AllowsNull())
                    {
                        inputParameter.AllowsNull();
                    }

                    Requirement(inputParameter, enter);

                    if (member.isGettable)
                    {
                        Requirement(inputParameter, result);
                    }
                }

                if (parameterInfo.ParameterType.IsByRef || parameterInfo.IsOut)
                {
                    var outputParameterKey = "&" + parameterInfo.Name;

                    // Changes in parameter names are tolerated, use the old parameter naming for now and fix it later.
                    if (parameterNames != null && parameterNames[parameterIndex] != parameterInfo.Name)
                    {
                        outputParameterKey = "&" + parameterNames[parameterIndex];
                        needsParameterRemapping = true;
                    }

                    var outputParameter = ValueOutput(parameterType, outputParameterKey);

                    outputParameters[parameterIndex] = outputParameter;

                    Assignment(enter, outputParameter);

                    useExpandedParameters = false;
                }
            }

            if (inputParameters.Length > 5)
            {
                useExpandedParameters = false;
            }

            if (parameterNames == null)
            {
                parameterNames = parameterInfos.Select(pInfo => pInfo.Name).ToList();
            }

            if (needsParameterRemapping)
            {
                // Note, this will have no effect unless we are in an Editor context. This is okay since for runtime
                // purposes as it is actually fine to continue to use the old parameter names for the sake of setting up
                // connections and default values. The only reason it is interesting to update to the new parameter
                // names is for UI purposes.
                UnityThread.EditorAsync(PostDeserializeRemapParameterNames);
            }
            Initialize();
        }

        private void PostDeserializeRemapParameterNames()
        {
            var parameterInfos = member.GetParameterInfos().ToArray();

            // Sanity check
            if (parameterNames?.Count != parameterInfos.Length)
                return;

            // Check if any of the method parameter names have changed (Note: handling of parameter type changes is not
            // supported here, it is detected and handled elsewhere)
            List<(ValueInput port, ValueOutput[] connectedSources)> renamedInputs = null;
            List<(ValueOutput port, ValueInput[] connectedDestinations)> renamedOutputs = null;
            List<(string name, object value)> renamedDefaults = null;
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var paramInfo = parameterInfos[i];
                var oldParamName = parameterNames[i];

                if (paramInfo.Name != oldParamName)
                {
                    // Phase 1 of parameter renaming: disconnect any nodes connected to affected ports, remove affected
                    // ports from port definition, and remove any default values associated with affected ports.
                    if (valueInputs.TryGetValue("%" + oldParamName, out var oldInput))
                    {
                        var connectionSources = oldInput.validConnections.Select(con => con.source).ToArray();
                        foreach (var source in connectionSources)
                            source.DisconnectFromValid(oldInput);

                        valueInputs.Remove(oldInput);

                        if (renamedInputs == null)
                            renamedInputs = new List<(ValueInput, ValueOutput[])>(1);
                        renamedInputs.Add((new ValueInput("%" + paramInfo.Name, paramInfo.ParameterType), connectionSources));

                        if (defaultValues.TryGetValue(oldInput.key, out var defaultValue))
                        {
                            defaultValues.Remove(oldInput.key);
                            if (renamedDefaults == null)
                                renamedDefaults = new List<(string, object)>(1);
                            renamedDefaults.Add(("%" + paramInfo.Name, defaultValue));
                        }
                    }
                    else if (valueOutputs.TryGetValue("&" + oldParamName, out var oldOutput))
                    {
                        var connectionDestinations = oldOutput.validConnections.Select(con => con.destination).ToArray();
                        foreach (var destination in connectionDestinations)
                            destination.DisconnectFromValid(oldOutput);

                        valueOutputs.Remove(oldOutput);

                        if (renamedOutputs == null)
                            renamedOutputs = new List<(ValueOutput, ValueInput[])>(1);
                        renamedOutputs.Add((new ValueOutput("&" + paramInfo.Name, paramInfo.ParameterType), connectionDestinations));
                    }

                    parameterNames[i] = paramInfo.Name;
                }
            }

            // Phase 2 of parameter renaming: add renamed version of affected ports back to the port definition, reconnect
            // nodes back to those renamed ports, and redefine default values for those ports.
            if (renamedInputs != null)
            {
                foreach (var renamedInput in renamedInputs)
                {
                    valueInputs.Add(renamedInput.port);
                    foreach (var source in renamedInput.connectedSources)
                        source.ConnectToValid(renamedInput.port);
                }
                if (renamedDefaults != null)
                {
                    foreach (var renamedDefault in renamedDefaults)
                        defaultValues[renamedDefault.name] = renamedDefault.value;
                }
            }

            if (renamedOutputs != null)
            {
                foreach (var renamedOutput in renamedOutputs)
                {
                    valueOutputs.Add(renamedOutput.port);
                    foreach (var destination in renamedOutput.connectedDestinations)
                        destination.ConnectToValid(renamedOutput.port);
                }
            }


            if (renamedInputs != null || renamedOutputs != null)
            {
                Define();
            }
        }

        public bool TryGetInput(int index, out ValueInput value)
        {
            var array = inputParameters;
            if (array != null && index >= 0 && index < array.Length)
            {
                value = array[index];
                return value != null;
            }

            value = default;
            return false;
        }

        public bool TryGetOutput(int index, out ValueOutput value)
        {
            var array = outputParameters;
            if (array != null && index >= 0 && index < array.Length)
            {
                value = array[index];
                return value != null;
            }

            value = default;
            return false;
        }

        protected override bool IsMemberValid(Member member)
        {
            return member.isInvocable;
        }

        private delegate* managed<InvokeMember, ref ParameterValue, Flow, ParameterValue> cachedInvoke;

        protected override void Initialize()
        {
            base.Initialize();

            if (useExpandedParameters && parameterCount >= 0 && parameterCount <= 5)
            {
                cachedInvoke = strategy == AccessStrategy.Reference
                    ? GetRefInvoker(parameterCount)
                    : GetValueInvoker(parameterCount);
            }
            else
            {
                cachedInvoke = &Invoke_Fallback;
            }
        }
        // Experiementing to see if this implementation is faster than normal delegates
        #region Static Function Pointers

        private static delegate*<InvokeMember, ref ParameterValue, Flow, ParameterValue> GetRefInvoker(int count)
        {
            return count switch
            {
                0 => &InvokeRef_0,
                1 => &InvokeRef_1,
                2 => &InvokeRef_2,
                3 => &InvokeRef_3,
                4 => &InvokeRef_4,
                5 => &InvokeRef_5,
                _ => &Invoke_Fallback,
            };
        }

        private static delegate*<InvokeMember, ref ParameterValue, Flow, ParameterValue> GetValueInvoker(int count)
        {
            return count switch
            {
                0 => &Invoke_0,
                1 => &Invoke_1,
                2 => &Invoke_2,
                3 => &Invoke_3,
                4 => &Invoke_4,
                5 => &Invoke_5,
                _ => &Invoke_Fallback,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_0(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_1(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t, f.GetValueData(@this.inputParameters[0]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_2(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_3(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_4(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]), f.GetValueData(@this.inputParameters[3]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue InvokeRef_5(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.InvokeRef(ref t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]), f.GetValueData(@this.inputParameters[3]), f.GetValueData(@this.inputParameters[4]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_0(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_1(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t, f.GetValueData(@this.inputParameters[0]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_2(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_3(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_4(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]), f.GetValueData(@this.inputParameters[3]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_5(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.member.Invoke(t, f.GetValueData(@this.inputParameters[0]), f.GetValueData(@this.inputParameters[1]), f.GetValueData(@this.inputParameters[2]), f.GetValueData(@this.inputParameters[3]), f.GetValueData(@this.inputParameters[4]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParameterValue Invoke_Fallback(InvokeMember @this, ref ParameterValue t, Flow f) =>
            @this.InvokeFallback(ref t, f);

        #endregion

        private ParameterValue InvokeFallback(ref ParameterValue target, Flow flow)
        {
            Span<ParameterValue> arguments = stackalloc ParameterValue[parameterCount];

            for (int i = 0; i < parameterCount; i++)
            {
                var input = inputParameters[i];
                arguments[i] = (input != null) ? flow.GetValueData(input) : ParameterValue.None;
            }

            ParameterValue res;
            switch (strategy)
            {
                case AccessStrategy.Static:
                    res = member.Invoke(ParameterValue.None, arguments);
                    break;

                case AccessStrategy.Instance:
                    var value = flow.GetValueData(this.target);
                    res = member.Invoke(value, arguments);
                    break;

                case AccessStrategy.Reference:
                    var reference = flow.GetValueData(this.target);
                    res = member.InvokeRef(ref reference, arguments);
                    break;

                default:
                    res = ParameterValue.None;
                    break;
            }

            for (int i = 0; i < parameterCount; i++)
            {
                var output = outputParameters[i];
                if (output != null) flow.SetValue(output, arguments[i]);
            }

            return res;
        }

        private ControlOutput Enter(Flow flow)
        {
            var target = requiresTarget ? flow.GetValueData(this.target) : ParameterValue.None;
            var resultValue = cachedInvoke(this, ref target, flow);

            if (requiresTarget && chainable) flow.SetValue(targetOutput, target);
            if (result != null) flow.SetValue(result, resultValue);

            return exit;
        }

        private ParameterValue Result(Flow flow)
        {
            var target = requiresTarget ? flow.GetValueData(this.target) : ParameterValue.None;
            var resultValue = cachedInvoke(this, ref target, flow);

            if (requiresTarget && chainable) flow.SetValue(targetOutput, target);

            return resultValue;
        }

        #region Analytics

        public override AnalyticsIdentifier GetAnalyticsIdentifier()
        {
            const int maxNumParameters = 5;
            var s = $"{member.targetType.FullName}.{member.name}";

            if (member.parameterTypes != null)
            {
                s += "(";

                for (var i = 0; i < member.parameterTypes.Length; ++i)
                {
                    if (i >= maxNumParameters)
                    {
                        s += $"->{i}";
                        break;
                    }

                    s += member.parameterTypes[i].FullName;
                    if (i < member.parameterTypes.Length - 1)
                        s += ", ";
                }

                s += ")";
            }

            var aid = new AnalyticsIdentifier
            {
                Identifier = s,
                Namespace = member.targetType.Namespace
            };
            aid.Hashcode = aid.Identifier.GetHashCode();
            return aid;
        }

        #endregion
    }
}
