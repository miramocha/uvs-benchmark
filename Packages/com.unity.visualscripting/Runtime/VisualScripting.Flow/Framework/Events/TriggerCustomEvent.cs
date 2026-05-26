using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Triggers a custom event.
    /// </summary>
    [UnitSurtitle("Custom Event")]
    [UnitShortTitle("Trigger")]
    [TypeIcon(typeof(CustomEvent))]
    [UnitCategory("Events")]
    [UnitOrder(1)]
    public sealed class TriggerCustomEvent : Unit
    {
        [SerializeAs(nameof(argumentCount))]
        private int _argumentCount;

        [DoNotSerialize]
        public List<ValueInput> arguments { get; private set; }

        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Arguments")]
        public int argumentCount
        {
            get => _argumentCount;
            set => _argumentCount = Mathf.Clamp(value, 0, 10);
        }

        /// <summary>
        /// The entry point to trigger the event.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput enter { get; private set; }

        /// <summary>
        /// The name of the event.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueInput name { get; private set; }

        /// <summary>
        /// The target of the event.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        [NullMeansSelf]
        public ValueInput target { get; private set; }

        /// <summary>
        /// The action to do after the event has been triggered.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlOutput exit { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), Trigger);

            exit = ControlOutput(nameof(exit));

            name = ValueInput<string>(nameof(name), string.Empty);

            target = ValueInput<GameObject>(nameof(target), null).NullMeansSelf();

            arguments = new List<ValueInput>();

            for (var i = 0; i < argumentCount; i++)
            {
                var argument = ValueInput<object>("argument_" + i);
                arguments.Add(argument);
                Requirement(argument, enter);
            }

            Requirement(name, enter);
            Requirement(target, enter);
            Succession(enter, exit);
        }

        private ControlOutput Trigger(Flow flow)
        {
            var target = flow.GetValue<GameObject>(this.target);
            var name = flow.GetValue<string>(this.name);

            var values = new NativeArray<ParameterValue>(argumentCount, Allocator.Temp);

            try
            {
                for (int i = 0; i < argumentCount; i++)
                {
                    values[i] = flow.GetValueData(arguments[i]);
                }

                CustomEvent.Trigger(target, name, values, flow.useDebugFlow);
            }
            finally
            {
                values.Dispose();
            }

            return exit;
        }

        protected override string GetUnitName()
        {
            var id = "";

            if (name != null)
            {
                if (!name.hasValidConnection)
                {
                    id = $"({(string)defaultValues[name.key]})";
                }
                else
                {
                    id = "(Connected Name)";
                }
            }

            return $"TriggerCustomEvent{id}";
        }
    }
}
