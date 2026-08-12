using System;
using System.Collections;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Loops between a first and last index at a specified step.
    /// </summary>
    [UnitTitle("For Loop")]
    [UnitCategory("Control")]
    [UnitOrder(9)]
    public sealed class For : LoopUnit
    {
        /// <summary>
        /// The index at which to start the loop (inclusive).
        /// </summary>
        [PortLabel("First")]
        [DoNotSerialize]
        public ValueInput firstIndex { get; private set; }

        /// <summary>
        /// The index at which to end the loop (exclusive).
        /// </summary>
        [PortLabel("Last")]
        [DoNotSerialize]
        public ValueInput lastIndex { get; private set; }

        /// <summary>
        /// The value by which the index will be incremented (or decremented, if negative) after each loop.
        /// </summary>
        [DoNotSerialize]
        public ValueInput step { get; private set; }

        /// <summary>
        /// The current index of the loop.
        /// </summary>
        [PortLabel("Index")]
        [DoNotSerialize]
        public ValueOutput currentIndex { get; private set; }

        protected override void Definition()
        {
            firstIndex = ValueInput(nameof(firstIndex), 0);
            lastIndex = ValueInput(nameof(lastIndex), 10);
            step = ValueInput(nameof(step), 1);
            currentIndex = ValueOutput<int>(nameof(currentIndex));
            base.Definition();

            Requirement(firstIndex, enter);
            Requirement(lastIndex, enter);
            Requirement(step, enter);
            Assignment(enter, currentIndex);
        }

        protected override ControlOutput Loop(Flow flow)
        {
            var stepVal = flow.GetValueData(step).ToInt32();

            if (stepVal == 0) return exit;

            var first = flow.GetValueData(firstIndex).ToInt32();
            var last = flow.GetValueData(lastIndex).ToInt32();

            var ascending = first <= last;

            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();

            ref var indexValue = ref flow.GetValueRefOrAddForPort(currentIndex, out _);

            try
            {
                for (int current = first; ascending ? current < last : current > last; current += stepVal)
                {
                    if (!flow.LoopIsNotBroken(loop)) break;

                    indexValue = new ParameterValue(current);

                    flow.Invoke(body);
                    flow.RestoreStack(stack);
                }
            }
            catch
            {
                flow.RestoreStack(stack);
            }
            finally
            {
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            return exit;
        }

        protected override IEnumerator LoopCoroutine(Flow flow)
        {
            var stepVal = flow.GetValue<int>(step);

            if (stepVal == 0)
            {
                yield return exit;
                yield break;
            }

            var first = flow.GetValueData(firstIndex).ToInt32();
            var last = flow.GetValueData(lastIndex).ToInt32();

            var ascending = first <= last;

            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();

            try
            {
                for (int current = first; ascending ? current < last : current > last; current += stepVal)
                {
                    if (!flow.LoopIsNotBroken(loop)) break;

                    flow.SetValue(currentIndex, current);

                    yield return body;

                    flow.RestoreStack(stack);
                }
            }
            finally
            {
                flow.RestoreStack(stack);
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            yield return exit;
        }

        public bool IsStepValueZero()
        {
            var isDefaultZero = !step.hasValidConnection && (int)defaultValues[step.key] == 0;
            var isConnectedToLiteralZero = false;

            if (step.hasValidConnection && step.connection.source.unit is Literal literal)
            {
                if (Convert.ToInt32(literal.value) == 0)
                {
                    isConnectedToLiteralZero = true;
                }
            }

            return isDefaultZero || isConnectedToLiteralZero;
        }
    }
}
