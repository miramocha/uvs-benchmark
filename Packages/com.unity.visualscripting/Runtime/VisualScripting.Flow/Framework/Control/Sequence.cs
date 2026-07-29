using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Executes the output ports in order.
    /// </summary>
    [UnitCategory("Control")]
    [UnitOrder(13)]
    public sealed class Sequence : Unit
    {
        [SerializeAs(nameof(outputCount))]
        private int _outputCount = 2;

        /// <summary>
        /// The entry point for the sequence.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput enter { get; private set; }

        [DoNotSerialize]
        [Inspectable, InspectorLabel("Steps"), UnitHeaderInspectable("Steps")]
        public int outputCount
        {
            get => _outputCount;
            set => _outputCount = Mathf.Clamp(value, 1, 10);
        }

        [DoNotSerialize]
        public ControlOutput[] multiOutputs { get; private set; }

        protected override void Definition()
        {
            enter = ControlInputCoroutine(nameof(enter), Enter, EnterCoroutine);

            multiOutputs = new ControlOutput[outputCount];

            for (var i = 0; i < outputCount; i++)
            {
                var output = ControlOutput(i.ToString());

                Succession(enter, output);

                multiOutputs[i] = output;
            }
        }

        private ControlOutput Enter(Flow flow)
        {
            var length = multiOutputs.Length;

            if (length == 1)
            {
                return multiOutputs[0];
            }

            var stack = flow.PreserveStack();

            for (int i = 0; i < length; i++)
            {
                flow.Invoke(multiOutputs[i]);

                flow.RestoreStack(stack);
            }

            flow.DisposePreservedStack(stack);

            return null;
        }

        private IEnumerator EnterCoroutine(Flow flow)
        {
            var length = multiOutputs.Length;

            if (length == 1)
            {
                yield return multiOutputs[0];
                yield break;
            }

            var stack = flow.PreserveStack();

            for (int i = 0; i < length; i++)
            {
                yield return multiOutputs[i];

                flow.RestoreStack(stack);
            }

            flow.DisposePreservedStack(stack);
        }

        public void CopyFrom(Sequence source)
        {
            base.CopyFrom(source);
            outputCount = source.outputCount;
        }
    }
}