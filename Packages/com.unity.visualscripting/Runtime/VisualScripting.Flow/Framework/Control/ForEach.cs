using System;
using System.Collections;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Loops over each element of a collection.
    /// </summary>
    [UnitTitle("For Each Loop")]
    [UnitCategory("Control")]
    [UnitOrder(10)]
    public class ForEach : LoopUnit
    {
        /// <summary>
        /// The collection over which to loop.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueInput collection { get; private set; }

        /// <summary>
        /// The current index of the loop.
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Index")]
        public ValueOutput currentIndex { get; private set; }

        /// <summary>
        /// The key of the current item of the loop.
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Key")]
        public ValueOutput currentKey { get; private set; }

        /// <summary>
        /// The current item of the loop.
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Item")]
        public ValueOutput currentItem { get; private set; }

        [Serialize]
        [Inspectable, UnitHeaderInspectable("Dictionary")]
        [InspectorToggleLeft]
        public bool dictionary { get; set; }

        protected override void Definition()
        {
            base.Definition();

            if (dictionary)
            {
                collection = ValueInput<IDictionary>(nameof(collection));
            }
            else
            {
                collection = ValueInput<IEnumerable>(nameof(collection));
            }

            currentIndex = ValueOutput<int>(nameof(currentIndex));

            if (dictionary)
            {
                currentKey = ValueOutput<object>(nameof(currentKey));
            }

            currentItem = ValueOutput<object>(nameof(currentItem));

            Requirement(collection, enter);
            Assignment(enter, currentIndex);
            Assignment(enter, currentItem);

            if (dictionary)
            {
                Assignment(enter, currentKey);
            }
        }

        protected override ControlOutput Loop(Flow flow)
        {
            var rawCollection = flow.GetValueData(collection).ToObject();

            if (rawCollection == null) return exit;

            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();
            int index = 0;
            IDictionaryEnumerator dictionaryEnumerator = null;
            IEnumerator enumerator = null;

            try
            {
                if (dictionary)
                {
                    if (rawCollection is IDictionary dict)
                    {
                        dictionaryEnumerator = dict.GetEnumerator();
                        enumerator = dictionaryEnumerator;

                        while (flow.LoopIsNotBroken(loop) && dictionaryEnumerator.MoveNext())
                        {

                            flow.SetValue(currentKey, dictionaryEnumerator.Key);
                            flow.SetValue(currentItem, dictionaryEnumerator.Value);
                            flow.SetValue(currentIndex, index);

                            flow.Invoke(body);

                            flow.RestoreStack(stack);

                            index++;
                        }
                    }
                    else if (rawCollection is IEnumerable enumerable)
                    {
                        enumerator = enumerable.GetEnumerator();
                        while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
                        {
                            flow.SetValue(currentItem, enumerator.Current);
                            flow.SetValue(currentIndex, index);

                            flow.Invoke(body);

                            flow.RestoreStack(stack);

                            index++;
                        }
                    }
                }
                else
                {
                    if (rawCollection is IList list)
                    {
                        var count = list.Count;
                        for (index = 0; index < count; index++)
                        {
                            if (!flow.LoopIsNotBroken(loop)) break;

                            flow.SetValue(currentItem, list[index]);
                            flow.SetValue(currentIndex, index);
                            flow.Invoke(body);

                            flow.RestoreStack(stack);

                        }
                    }
                    else if (rawCollection is IEnumerable enumerable)
                    {
                        enumerator = enumerable.GetEnumerator();

                        while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
                        {
                            flow.SetValue(currentItem, enumerator.Current);
                            flow.SetValue(currentIndex, index);

                            flow.Invoke(body);

                            flow.RestoreStack(stack);

                            index++;
                        }
                    }
                }
            }
            catch
            {
                flow.RestoreStack(stack);
                throw;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
                (dictionaryEnumerator as IDisposable)?.Dispose();
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            return exit;
        }

        protected override IEnumerator LoopCoroutine(Flow flow)
        {
            var rawCollection = flow.GetValueData(collection).ToObject();

            if (rawCollection == null)
            {
                yield return exit;
                yield break;
            }

            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();
            int index = 0;
            IDictionaryEnumerator dictionaryEnumerator = null;
            IEnumerator enumerator = null;

            try
            {
                if (dictionary)
                {
                    if (rawCollection is IDictionary dict)
                    {
                        dictionaryEnumerator = dict.GetEnumerator();
                        enumerator = dictionaryEnumerator;

                        while (flow.LoopIsNotBroken(loop) && dictionaryEnumerator.MoveNext())
                        {
                            flow.SetValue(currentKey, dictionaryEnumerator.Key);
                            flow.SetValue(currentItem, dictionaryEnumerator.Value);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                            index++;
                        }
                    }
                    else if (rawCollection is IEnumerable enumerable)
                    {
                        enumerator = enumerable.GetEnumerator();
                        while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
                        {
                            flow.SetValue(currentItem, enumerator.Current);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);

                            index++;
                        }
                    }
                }
                else
                {
                    if (rawCollection is IList list)
                    {
                        for (index = 0; index < list.Count; index++)
                        {
                            if (!flow.LoopIsNotBroken(loop)) break;

                            flow.SetValue(currentItem, list[index]);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                        }
                    }
                    else if (rawCollection is IEnumerable enumerable)
                    {
                        enumerator = enumerable.GetEnumerator();

                        while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
                        {
                            flow.SetValue(currentItem, enumerator.Current);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                            index++;
                        }
                    }
                }
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
                (dictionaryEnumerator as IDisposable)?.Dispose();
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            yield return exit;
        }
    }
}
