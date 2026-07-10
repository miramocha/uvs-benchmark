using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// A global container that maps Event names and Component references to actions for registered listeners.
    /// </summary>
    /// <remarks>
    /// <para>It is the EventMachine base class for ScriptMachine and StateMachine that triggers events.
    /// This overrides almost all Unity callbacks (such as Awake, OnEnable, Update, etc.) and triggers an event
    /// on the EventBus.</para>
    /// </remarks>
    /// <example>
    /// <para>The following example shows how to use the EventBus to send a custom event from a script to a node in
    /// a graph. It also shows how to use the EventBus as a global event manager by executing a callback in a
    /// script, not just a node.
    ///
    /// For more information on how to create custom event nodes refer to the
    /// <a href="../manual/vs-create-own-custom-event-node.html">User Manual</a>.
    ///
    /// In this example we've added some code to a GameObject. This code checks for when the user presses a sequence of
    /// keys to enable a cheat code, then triggers the <c>CheatCodeActivated</c> event. We register the
    /// <c>CheatCodeActivated</c> event in the <c>Start</c> method. The <c>Update</c> method triggers the event twice
    /// with 2 different targets: one for the <c>CheatCodeActivated</c> callback and the other to trigger the
    /// CheatCodeEnabled Node.</para>
    ///
    /// <code source="../../../DocCodeExamples/EventBusExamples.cs" region="CheatCodeController" title="CheatCodeController"/>
    ///
    /// <para>The CheatCodeEnabled Node:</para>
    ///
    /// <code source="../../../DocCodeExamples/EventBusExamples.cs" region="CheatCodeEnabled" title="CheatCodeEnabled"/>
    /// </example>
    public static class EventBus
    {
        private static readonly Dictionary<EventHook, HandlerList> events;

        internal static Dictionary<EventHook, List<Delegate>> testAccessEvents
        {
            get
            {
                var dict = new Dictionary<EventHook, List<Delegate>>();
                foreach (var kvp in events)
                {
                    dict[kvp.Key] = kvp.Value.Handlers;
                }
                return dict;
            }
        }

        static EventBus()
        {
            events = new Dictionary<EventHook, HandlerList>(256, new EventHookComparer());
        }

        private class HandlerList
        {
            public readonly List<Delegate> Handlers = new List<Delegate>();
            public int InvokeDepth;
            public bool NeedsCleanup;

            public void Add(Delegate handler)
            {
                if (!Handlers.Contains(handler))
                {
                    Handlers.Add(handler);
                }
            }

            public bool Remove(Delegate handler)
            {
                int index = Handlers.IndexOf(handler);
                if (index < 0) return false;

                if (InvokeDepth > 0)
                {
                    Handlers[index] = null;
                    NeedsCleanup = true;
                }
                else
                {
                    Handlers.RemoveAt(index);
                }
                return true;
            }

            public void Cleanup()
            {
                if (NeedsCleanup && InvokeDepth == 0)
                {
                    int writeIndex = 0;
                    for (int readIndex = 0; readIndex < Handlers.Count; readIndex++)
                    {
                        if (Handlers[readIndex] != null)
                        {
                            Handlers[writeIndex++] = Handlers[readIndex];
                        }
                    }

                    Handlers.RemoveRange(writeIndex, Handlers.Count - writeIndex);
                    NeedsCleanup = false;
                }
            }
        }

        public static void Register<TArgs>(EventHook hook, Action<TArgs> handler)
        {
            if (!events.TryGetValue(hook, out var list))
            {
                list = new HandlerList();
                events.Add(hook, list);
            }

            list.Add(handler);
        }

        public static void Unregister(EventHook hook, Delegate handler)
        {
            if (events.TryGetValue(hook, out var list))
            {
                if (list.Remove(handler))
                {
                    if (list.Handlers.Count == 0 && list.InvokeDepth == 0)
                    {
                        events.Remove(hook);
                    }
                }
            }
        }

        public static void Trigger<TArgs>(EventHook hook, TArgs args)
        {
            if (!events.TryGetValue(hook, out var list))
            {
                return;
            }

            list.InvokeDepth++;

            int count = list.Handlers.Count;

            for (int i = 0; i < count; i++)
            {
                var del = list.Handlers[i];

                if (del != null && del is Action<TArgs> handler)
                {
                    handler.Invoke(args);
                }
            }

            list.InvokeDepth--;

            if (list.InvokeDepth == 0 && list.NeedsCleanup)
            {
                list.Cleanup();

                if (list.Handlers.Count == 0)
                {
                    events.Remove(hook);
                }
            }
        }

        public static void Trigger<TArgs>(string name, GameObject target, TArgs args)
        {
            Trigger(new EventHook(name, target), args);
        }

        private static readonly EmptyEventArgs emptyArgs = new EmptyEventArgs();

        public static void Trigger(EventHook hook)
        {
            Trigger(hook, emptyArgs);
        }

        public static void Trigger(string name, GameObject target)
        {
            Trigger(new EventHook(name, target));
        }

        public static bool HasHook(EventHook hook)
        {
            return events.ContainsKey(hook);
        }

        internal static bool WillRemoveHook(EventHook hook)
        {
            return events.TryGetValue(hook, out var list) && list.Handlers.Count == 1;
        }
    }
}