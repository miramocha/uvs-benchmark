using UnityEngine;

namespace Unity.VisualScripting
{
    [AddComponentMenu("Visual Scripting/State Machine")]
    [RequireComponent(typeof(Variables))]
    [DisableAnnotation]
    [VisualScriptingHelpURL(typeof(StateMachine))]
    public sealed class StateMachine : EventMachine<StateGraph, StateGraphAsset>
    {
        protected override void OnEnable()
        {
            if (hasGraph)
            {
                using (var flow = Flow.New(reference))
                {
                    graph.Start(flow);
                }
            }

            base.OnEnable();
        }

        protected override void OnInstantiateWhileEnabled()
        {
            if (hasGraph)
            {
                using (var flow = Flow.New(reference))
                {
                    graph.Start(flow);
                }
            }

            SetupProxies();

            base.OnInstantiateWhileEnabled();
        }

        protected override void OnUninstantiateWhileEnabled()
        {
            base.OnUninstantiateWhileEnabled();

            if (hasGraph)
            {
                using (var flow = Flow.New(reference))
                {
                    graph.Stop(flow);
                }
            }

            RemoveProxies();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (hasGraph)
            {
                using (var flow = Flow.New(reference))
                {
                    graph.Stop(flow);
                }
            }

            RemoveProxies();
        }

        [ContextMenu("Show Data...")]
        protected override void ShowData()
        {
            base.ShowData();
        }

        public override StateGraph DefaultGraph()
        {
            return StateGraph.WithStart();
        }
    }
}
