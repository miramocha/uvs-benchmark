using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Gets the value of a variable.
    /// </summary>
    public sealed class GetVariable : UnifiedVariableUnit
    {
        /// <summary>
        /// The value of the variable.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueOutput value { get; private set; }

        /// <summary>
        /// The value to return if the variable is not defined.
        /// </summary>
        [DoNotSerialize]
        public ValueInput fallback { get; private set; }

        /// <summary>
        /// Whether a fallback value should be provided if the
        /// variable is not defined.
        /// </summary>
        [Serialize]
        [Inspectable]
        [InspectorLabel("Fallback")]
        public bool specifyFallback { get; set; } = false;

        protected override void Definition()
        {
            base.Definition();

            value = ValueOutput(typeof(object), nameof(value), Get).PredictableIf(IsDefined);

            Requirement(name, value);

            if (kind == VariableKind.Object)
            {
                Requirement(@object, value);
            }

            if (specifyFallback)
            {
                fallback = ValueInput<object>(nameof(fallback));
                Requirement(fallback, value);
            }
        }

        private bool IsDefined(Flow flow)
        {
            var name = flow.GetValue<string>(this.name);

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            GameObject @object = null;

            if (kind == VariableKind.Object)
            {
                @object = flow.GetValue<GameObject>(this.@object);

                if (@object == null)
                {
                    return false;
                }
            }

            var scene = flow.stack.scene;

            if (kind == VariableKind.Scene)
            {
                if (scene == null || !scene.Value.IsValid() || !scene.Value.isLoaded || !Variables.ExistInScene(scene))
                {
                    return false;
                }
            }

            switch (kind)
            {
                case VariableKind.Flow:
                    return flow.variables.IsDefined(name);
                case VariableKind.Graph:
                    return Variables.Graph(flow.stack).IsDefined(name);
                case VariableKind.Object:
                    return Variables.Object(@object).IsDefined(name);
                case VariableKind.Scene:
                    return Variables.Scene(scene.Value).IsDefined(name);
                case VariableKind.Application:
                    return Variables.Application.IsDefined(name);
                case VariableKind.Saved:
                    return Variables.Saved.IsDefined(name);
                default:
                    throw new UnexpectedEnumValueException<VariableKind>(kind);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParameterValue Get(Flow flow)
        {
            var name = flow.GetValue<string>(this.name);
            VariableDeclarations variables = kind switch
            {
                VariableKind.Flow => flow.variables,
                VariableKind.Graph => Variables.Graph(flow.stack),
                VariableKind.Object => Variables.Object(flow.GetValue<GameObject>(@object)),
                VariableKind.Scene => Variables.Scene(flow.stack.scene),
                VariableKind.Application => Variables.Application,
                VariableKind.Saved => Variables.Saved,
                _ => throw new UnexpectedEnumValueException<VariableKind>(kind),
            };

            if (variables.TryGetValue(name, out var variableValue))
            {
                return new ParameterValue(variableValue);
            }

            if (specifyFallback)
            {
                return flow.GetValueData(fallback);
            }

            throw new InvalidOperationException($"Variable not found: '{name}'.");
        }
    }
}
