using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Unity.VisualScripting
{
    public abstract class EventMachine<TGraph, TMacro> : Machine<TGraph, TMacro>, IEventMachine
        where TGraph : class, IGraph, new()
        where TMacro : Macro<TGraph>, new()
    {
#if ENABLE_UVS_PROFILING
        private static readonly Dictionary<string, ProfilerMarker> RegisteredEventMarkers = new Dictionary<string, ProfilerMarker>();
        private static readonly Dictionary<string, ProfilerMarker> UnregisteredEventMarkers = new Dictionary<string, ProfilerMarker>();

        private static ProfilerMarker GetOrCreateMarker(string name, bool isRegistered, GraphReference reference)
        {
            var cache = isRegistered ? RegisteredEventMarkers : UnregisteredEventMarkers;
            if (!cache.TryGetValue(name, out var marker))
            {
                marker = new ProfilerMarker($"[{name}] {Graph.GetGraphName(reference)}");
                cache[name] = marker;
            }
            return marker;
        }

        protected void TriggerEvent(string name)
        {
            if (hasGraph)
            {
                var marker = GetOrCreateMarker(name, isRegistered: true, reference);
                try
                {
                    marker.Begin(this);
                    TriggerRegisteredEvent(new EventHook(name, this), new EmptyEventArgs());
                }
                finally
                {
                    marker.End();
                }
            }
        }

        protected void TriggerEvent<TArgs>(string name, TArgs args)
        {
            if (hasGraph)
            {
                var marker = GetOrCreateMarker(name, isRegistered: true, reference);
                try
                {
                    marker.Begin(this);
                    TriggerRegisteredEvent(new EventHook(name, this), args);
                }
                finally
                {
                    marker.End();
                }
            }
        }

        protected void TriggerUnregisteredEvent(string name)
        {
            if (hasGraph)
            {
                var marker = GetOrCreateMarker(name, isRegistered: false, reference);
                try
                {
                    marker.Begin(this);
                    TriggerUnregisteredEvent(name, new EmptyEventArgs());
                }
                finally
                {
                    marker.End();
                }
            }
        }
#else
        protected void TriggerEvent(string name)
        {
            if (hasGraph)
            {
                TriggerRegisteredEvent(new EventHook(name, this), new EmptyEventArgs());
            }
        }

        protected void TriggerEvent<TArgs>(string name, TArgs args)
        {
            if (hasGraph)
            {
                TriggerRegisteredEvent(new EventHook(name, this), args);
            }
        }

        protected void TriggerUnregisteredEvent(string name)
        {
            if (hasGraph)
            {
                TriggerUnregisteredEvent(name, new EmptyEventArgs());
            }
        }
#endif

        protected virtual void TriggerRegisteredEvent<TArgs>(EventHook hook, TArgs args)
        {
            EventBus.Trigger(hook, args);
        }

        protected virtual void TriggerUnregisteredEvent<TArgs>(EventHook hook, TArgs args)
        {
            using (var stack = reference.ToStackPooled())
            {
                stack.TriggerEventHandler(_hook => _hook == hook, args, parent => true, true);

                stack.ClearReference();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            GlobalMessageListener.Require();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetupProxies();

            TriggerEvent(EventHooks.OnEnable);
        }

        protected void SetupProxies()
        {
            RegisterUpdate();
            RegisterFixedUpdate();
            RegisterLateUpdate();
        }

        protected void RemoveProxies()
        {
            UnregisterUpdate();
            UnregisterFixedUpdate();
            UnregisterLateUpdate();
        }

        protected virtual void Start()
        {
            TriggerEvent(EventHooks.Start);
        }

        protected override void OnInstantiateWhileEnabled()
        {
            base.OnInstantiateWhileEnabled();

            TriggerEvent(EventHooks.OnEnable);
        }

        protected virtual void TriggerUpdate()
        {
            if (hasGraph && enabled)
                TriggerEvent(EventHooks.Update);
        }

        protected virtual void TriggerFixedUpdate()
        {
            if (hasGraph && enabled)
                TriggerEvent(EventHooks.FixedUpdate);
        }

        protected virtual void TriggerLateUpdate()
        {
            if (hasGraph && enabled)
                TriggerEvent(EventHooks.LateUpdate);
        }

        protected override void OnUninstantiateWhileEnabled()
        {
            TriggerEvent(EventHooks.OnDisable);

            base.OnUninstantiateWhileEnabled();
        }

        protected override void OnDisable()
        {
            TriggerEvent(EventHooks.OnDisable);

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            try
            {
                TriggerEvent(EventHooks.OnDestroy);
            }
            finally
            {
                base.OnDestroy();

                SetupProxies();
            }
        }

#if MODULE_ANIMATION_EXISTS
        public override void TriggerAnimationEvent(AnimationEvent animationEvent)
        {
            TriggerEvent(EventHooks.AnimationEvent, animationEvent);
        }
#endif

        public override void TriggerUnityEvent(string name)
        {
            TriggerEvent(EventHooks.UnityEvent, name);
        }

        protected virtual void OnDrawGizmos()
        {
            TriggerUnregisteredEvent(EventHooks.OnDrawGizmos);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            TriggerUnregisteredEvent(EventHooks.OnDrawGizmosSelected);
        }

        void IEventMachine.TriggerUpdate()
        {
            TriggerUpdate();
        }
        void IEventMachine.TriggerFixedUpdate()
        {
            TriggerFixedUpdate();
        }
        void IEventMachine.TriggerLateUpdate()
        {
            TriggerLateUpdate();
        }

        public void RegisterUpdate()
        {
            var updateHook = new EventHook(EventHooks.Update, this);

            if (EventBus.HasHook(updateHook))
            {
                if (!gameObject.TryGetComponent<GraphUpdateManager>(out var manager))
                {
                    manager = gameObject.AddComponent<GraphUpdateManager>();
                    manager.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
                }

                manager.Register(this);
            }
        }

        public void RegisterFixedUpdate()
        {
            var fixedUpdateHook = new EventHook(EventHooks.FixedUpdate, this);

            if (EventBus.HasHook(fixedUpdateHook))
            {
                if (!gameObject.TryGetComponent<GraphFixedUpdateManager>(out var manager))
                {
                    manager = gameObject.AddComponent<GraphFixedUpdateManager>();
                    manager.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
                }

                manager.Register(this);
            }

        }

        public void RegisterLateUpdate()
        {
            var lateUpdateHook = new EventHook(EventHooks.LateUpdate, this);
            if (EventBus.HasHook(lateUpdateHook))
            {
                if (!gameObject.TryGetComponent<GraphLateUpdateManager>(out var manager))
                {
                    manager = gameObject.AddComponent<GraphLateUpdateManager>();
                    manager.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
                }

                manager.Register(this);
            }
        }

        public void UnregisterUpdate()
        {
            var updateHook = new EventHook(EventHooks.Update, this);

            if (EventBus.HasHook(updateHook))
            {
                if (gameObject.TryGetComponent<GraphUpdateManager>(out var manager))
                {
                    manager.Unregister(this);
                }
            }
        }

        public void UnregisterFixedUpdate()
        {
            var fixedUpdateHook = new EventHook(EventHooks.FixedUpdate, this);

            if (EventBus.HasHook(fixedUpdateHook))
            {
                if (gameObject.TryGetComponent<GraphFixedUpdateManager>(out var manager))
                {
                    manager.Unregister(this);
                }
            }
        }

        public void UnregisterLateUpdate()
        {
            var lateUpdateHook = new EventHook(EventHooks.LateUpdate, this);
            if (EventBus.HasHook(lateUpdateHook))
            {
                if (gameObject.TryGetComponent<GraphLateUpdateManager>(out var manager))
                {
                    manager.Unregister(this);
                }
            }
        }
    }
}
