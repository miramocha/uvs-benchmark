using System;
using System.Collections;
using System.Runtime.CompilerServices;

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
            var rawCollection = flow.GetValue(collection);
            if (rawCollection == null) return exit;

            var loop = flow.EnterLoop();
            var stack = flow.PreserveStack();
            IEnumerator enumerator = null;

            try
            {
                if (dictionary)
                {
                    if (rawCollection is AotDictionary aotDictionary)
                        LoopDictionary(flow, loop, stack, aotDictionary);
                    else if (rawCollection is IDictionary dict)
                    {
                        enumerator = dict.GetEnumerator();
                        LoopDictionary(flow, loop, stack, (IDictionaryEnumerator)enumerator);
                    }
                }
                else if (!dictionary)
                {
                    if (rawCollection is AotList aotList)
                        LoopList(flow, loop, stack, aotList);
                    else if (rawCollection is IList ilist)
                        LoopList(flow, loop, stack, ilist);
                }
                else if (rawCollection is IEnumerable enumerable)
                {
                    enumerator = enumerable.GetEnumerator();
                    LoopEnumerable(flow, loop, stack, enumerator);
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
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            return exit;
        }

        private void LoopDictionary(Flow flow, int loop, GraphStack stack, IDictionaryEnumerator dictEnum)
        {
            int index = 0;

            var keyItem = new ParameterValue((object)null);
            var valueItem = new ParameterValue((object)null);

            flow.SetValue(currentKey, keyItem);
            flow.SetValue(currentItem, valueItem);

            while (flow.LoopIsNotBroken(loop) && dictEnum.MoveNext())
            {
                keyItem.UpdateObject(dictEnum.Key);
                valueItem.UpdateObject(dictEnum.Value);
                flow.SetValue(currentIndex, index);

                flow.Invoke(body);
                flow.RestoreStack(stack);

                index++;
            }
        }

        private void LoopDictionary(Flow flow, int loop, GraphStack stack, AotDictionary dict)
        {
            int index = 0;

            var keyItem = new ParameterValue((object)null);
            var valueItem = new ParameterValue((object)null);

            flow.SetValue(currentKey, keyItem);
            flow.SetValue(currentItem, valueItem);
            foreach (DictionaryEntry item in dict)
            {
                if (!flow.LoopIsNotBroken(loop)) break;

                keyItem.UpdateObject(item.Key);
                valueItem.UpdateObject(item.Value);
                flow.SetValue(currentIndex, index);

                flow.Invoke(body);
                flow.RestoreStack(stack);

                index++;
            }
        }

        private void LoopList(Flow flow, int loop, GraphStack stack, IList list)
        {
            int count = list.Count;

            var item = new ParameterValue(list.Count > 0 ? list[0] : null);
            flow.SetValue(currentItem, item);

            for (int index = 0; index < count; index++)
            {
                if (!flow.LoopIsNotBroken(loop)) break;

                item.UpdateObject(list[index]);
                flow.SetValue(currentIndex, index);

                flow.Invoke(body);
                flow.RestoreStack(stack);
            }
        }

        private void LoopList(Flow flow, int loop, GraphStack stack, AotList list)
        {
            int count = list.Count;

            var item = new ParameterValue(list.Count > 0 ? list[0] : null);
            flow.SetValue(currentItem, item);

            for (int index = 0; index < count; index++)
            {
                if (!flow.LoopIsNotBroken(loop)) break;

                item.UpdateObject(list[index]);
                flow.SetValue(currentIndex, index);

                flow.Invoke(body);
                flow.RestoreStack(stack);
            }
        }

        private void LoopEnumerable(Flow flow, int loop, GraphStack stack, IEnumerator enumerator)
        {
            int index = 0;

            var item = new ParameterValue((object)null);
            flow.SetValue(currentItem, item);

            while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
            {
                item.UpdateObject(enumerator.Current);
                flow.SetValue(currentIndex, index);

                flow.Invoke(body);
                flow.RestoreStack(stack);

                index++;
            }
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
            IEnumerator enumerator = null;

            try
            {
                if (dictionary)
                {
                    if (rawCollection is AotDictionary aotDictionary)
                    {
                        var keyItem = new ParameterValue((object)null);
                        var valueItem = new ParameterValue((object)null);
                        flow.SetValue(currentKey, keyItem);
                        flow.SetValue(currentItem, valueItem);

                        foreach (DictionaryEntry item in aotDictionary)
                        {
                            if (!flow.LoopIsNotBroken(loop)) break;

                            keyItem.UpdateObject(item.Key);
                            valueItem.UpdateObject(item.Value);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                            index++;
                        }
                    }
                    else if (rawCollection is IDictionary dict)
                    {
                        var dictEnum = dict.GetEnumerator();
                        enumerator = dictEnum;

                        var keyItem = new ParameterValue((object)null);
                        var valueItem = new ParameterValue((object)null);
                        flow.SetValue(currentKey, keyItem);
                        flow.SetValue(currentItem, valueItem);

                        while (flow.LoopIsNotBroken(loop) && dictEnum.MoveNext())
                        {
                            keyItem.UpdateObject(dictEnum.Key);
                            valueItem.UpdateObject(dictEnum.Value);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                            index++;
                        }
                    }
                }
                else if (!dictionary)
                {
                    if (rawCollection is AotList aotlist)
                    {
                        int count = aotlist.Count;

                        var item = new ParameterValue(aotlist.Count > 0 ? aotlist[0] : null);
                        flow.SetValue(currentItem, item);

                        for (index = 0; index < count; index++)
                        {
                            if (!flow.LoopIsNotBroken(loop)) break;

                            item.UpdateObject(aotlist[index]);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                        }
                    }
                    else if (rawCollection is IList ilist)
                    {
                        int count = ilist.Count;

                        var item = new ParameterValue(ilist.Count > 0 ? ilist[0] : null);
                        flow.SetValue(currentItem, item);

                        for (index = 0; index < count; index++)
                        {
                            if (!flow.LoopIsNotBroken(loop)) break;

                            item.UpdateObject(ilist[index]);
                            flow.SetValue(currentIndex, index);

                            yield return body;

                            flow.RestoreStack(stack);
                        }
                    }
                }
                else if (rawCollection is IEnumerable enumerable)
                {
                    enumerator = enumerable.GetEnumerator();

                    var item = new ParameterValue((object)null);
                    flow.SetValue(currentItem, item);

                    while (flow.LoopIsNotBroken(loop) && enumerator.MoveNext())
                    {
                        item.UpdateObject(enumerator.Current);
                        flow.SetValue(currentIndex, index);

                        yield return body;

                        flow.RestoreStack(stack);
                        index++;
                    }
                }
            }
            finally
            {
                flow.RestoreStack(stack);
                (enumerator as IDisposable)?.Dispose();
                flow.DisposePreservedStack(stack);
                flow.ExitLoop(loop);
            }

            yield return exit;
        }
    }
}
