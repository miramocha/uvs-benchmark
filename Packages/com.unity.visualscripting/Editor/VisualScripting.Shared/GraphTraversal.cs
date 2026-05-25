using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using SUnit = Unity.VisualScripting.SubgraphUnit;
using SMachine = Unity.VisualScripting.ScriptMachine;

namespace Unity.VisualScripting
{
    public static class GraphTraversal
    {
        public static string GetParentName(GraphReference reference)
        {
            if (reference.IsWithin<INesterUnit>())
            {
                var parent = reference.GetParent<INesterUnit>();
                return GetNesterUnitName(parent);
            }
            else if (reference.IsWithin<INesterState>())
            {
                var parent = reference.GetParent<INesterState>();
                return GetNesterStateName(parent);
            }
            else if (reference.IsWithin<INesterStateTransition>())
            {
                var parent = reference.GetParent<INesterStateTransition>();
                return GetNesterStateTransitionName(parent);
            }
            return "Embed " + reference.parent.GetType().Name;
        }

        public static string GetNesterUnitName(INesterUnit nester)
        {
            if (!string.IsNullOrEmpty(nester.nest.graph.title))
            {
                return nester.nest.graph.title;
            }
            else if (nester.nest.source == GraphSource.Macro && nester.nest.macro is Object @object)
            {
                return @object.name;
            }
            else if (nester is SUnit) return "Embed Subgraph";
            else if (nester is StateUnit) return "Embed StateUnit";
            else return $"Embed {nester.GetType().Name}";
        }

        public static string GetNesterStateName(INesterState nester)
        {
            if (!string.IsNullOrEmpty(nester.nest.graph.title))
            {
                return nester.nest.graph.title;
            }
            else if (nester.nest.source == GraphSource.Macro && nester.nest.macro is Object @object)
            {
                return @object.name;
            }
            else if (nester is FlowState) return "Embed FlowState";
            else if (nester is SuperState) return "Embed SuperState";
            else return $"Embed {nester.GetType().Name}";
        }

        public static string GetNesterStateTransitionName(INesterStateTransition nester)
        {
            if (!string.IsNullOrEmpty(nester.nest.graph.title))
            {
                return nester.nest.graph.title;
            }
            else if (nester.nest.source == GraphSource.Macro && nester.nest.macro is Object @object)
            {
                return @object.name;
            }
            else if (nester is FlowStateTransition) return "Embed FlowStateTransition";
            else return $"Embed {nester.GetType().Name}";
        }

        public static void TraverseGraph(IGraph graph, Action<Unit> visit)
        {
            if (graph == null || visit == null) return;

            var visitedGraphs = new HashSet<IGraph>();
            if (graph is FlowGraph flowGraph)
                TraverseInternal(flowGraph, visit, visitedGraphs, true);
            else if (graph is StateGraph stateGraph)
                TraverseInternal(stateGraph, visit, visitedGraphs);
        }

        public static void TraverseFlowGraph(FlowGraph graph, Action<Unit> visit, bool enterStates = false)
        {
            if (graph == null || visit == null) return;

            var visitedGraphs = new HashSet<IGraph>();
            TraverseInternal(graph, visit, visitedGraphs, enterStates);
        }

        public static void TraverseFlowGraph(GraphReference graph, Action<(GraphReference reference, Unit unit)> visit)
        {
            if (graph == null || visit == null) return;

            var visitedGraphs = new HashSet<GraphReference>();
            TraverseInternal(graph, visit, visitedGraphs);
        }

        public static void TraverseStateGraph(StateGraph graph, System.Action<Unit> visit)
        {
            if (graph == null || visit == null) return;

            var visitedGraphs = new HashSet<IGraph>();
            TraverseInternal(graph, visit, visitedGraphs);
        }

        private static void TraverseInternal(GraphReference reference, Action<(GraphReference reference, Unit unit)> visit, HashSet<GraphReference> visitedGraphs)
        {
            if (!visitedGraphs.Add(reference))
                return;

            var graph = reference.graph as FlowGraph;

            if (graph == null) return;

            foreach (var unit in graph.units)
            {
                if (unit == null) continue;

                visit((reference, (Unit)unit));
                if (unit is SUnit subgraph)
                {
                    if (subgraph.nest.graph is FlowGraph)
                    {
                        TraverseInternal(reference.ChildReference(subgraph, false), visit, visitedGraphs);
                    }
                }
            }
        }

        private static void TraverseInternal(FlowGraph graph, System.Action<Unit> visit, HashSet<IGraph> visitedGraphs, bool enterStates)
        {
            if (!visitedGraphs.Add(graph))
                return;

            foreach (var unit in graph.units)
            {
                if (unit == null) continue;

                visit((Unit)unit);
                if (unit is SUnit subgraph)
                {
                    if (subgraph.nest.graph is FlowGraph flowGraph)
                    {
                        TraverseInternal(flowGraph, visit, visitedGraphs, enterStates);
                    }
                }
                else if (enterStates && unit is StateUnit stateUnit && stateUnit.nest != null)
                {
                    TraverseInternal(stateUnit.nest.graph, visit, visitedGraphs);
                }
            }
        }

        private static void TraverseInternal(StateGraph graph, System.Action<Unit> visit, HashSet<IGraph> visitedGraphs)
        {
            if (!visitedGraphs.Add(graph))
                return;

            foreach (var state in graph.states)
            {
                if (state is INesterState nesterState && nesterState.nest != null)
                {
                    if (nesterState.nest.graph is FlowGraph flowGraph)
                        TraverseInternal(flowGraph, visit, visitedGraphs, true);
                    else if (nesterState.nest.graph is StateGraph stateGraph)
                        TraverseInternal(stateGraph, visit, visitedGraphs);
                }
            }

            foreach (var transition in graph.transitions)
            {
                if (transition is INesterStateTransition nesterStateTransition && nesterStateTransition.nest != null)
                {
                    if (nesterStateTransition.nest.graph is FlowGraph flowGraph)
                        TraverseInternal(flowGraph, visit, visitedGraphs, true);
                    else if (nesterStateTransition.nest.graph is StateGraph stateGraph)
                        TraverseInternal(stateGraph, visit, visitedGraphs);
                }
            }
        }

        public static IEnumerable<Unit> RetrieveUnits(IGraph graph)
        {
            if (graph == null) yield break;

            var visitedGraphs = new HashSet<IGraph>();
            if (graph is FlowGraph flowGraph)
                RetrieveInternal(flowGraph, visitedGraphs, true);
            else if (graph is StateGraph stateGraph)
                RetrieveInternal(stateGraph, visitedGraphs);
        }

        public static IEnumerable<Unit> RetrieveFlowGraphUnits(FlowGraph graph, bool enterStates = false)
        {
            if (graph == null) yield break;

            var visitedGraphs = new HashSet<IGraph>();
            RetrieveInternal(graph, visitedGraphs, enterStates);
        }

        public static IEnumerable<Unit> RetrieveStateGraphUnits(StateGraph graph)
        {
            if (graph == null) yield break;

            var visitedGraphs = new HashSet<IGraph>();
            RetrieveInternal(graph, visitedGraphs);
        }

        private static IEnumerable<Unit> RetrieveInternal(FlowGraph graph, HashSet<IGraph> visitedGraphs, bool enterStates)
        {
            if (!visitedGraphs.Add(graph))
                yield break;

            foreach (var unit in graph.units)
            {
                if (unit == null) continue;

                yield return (Unit)unit;

                if (unit is SUnit subgraph)
                {
                    if (subgraph.nest.graph is FlowGraph flowGraph)
                    {
                        foreach (var u in RetrieveInternal(flowGraph, visitedGraphs, enterStates))
                        {
                            yield return u;
                        }
                    }
                }
                else if (enterStates && unit is StateUnit stateUnit && stateUnit.nest != null)
                {
                    foreach (var u in RetrieveInternal(stateUnit.nest.graph, visitedGraphs))
                    {
                        yield return u;
                    }
                }
            }
        }

        private static IEnumerable<Unit> RetrieveInternal(StateGraph graph, HashSet<IGraph> visitedGraphs)
        {
            if (!visitedGraphs.Add(graph))
                yield break;

            foreach (var state in graph.states)
            {
                if (state is INesterState nesterState && nesterState.nest != null)
                {
                    if (nesterState.nest.graph is FlowGraph flowGraph)
                        foreach (var u in RetrieveInternal(flowGraph, visitedGraphs, true)) yield return u;
                    else if (nesterState.nest.graph is StateGraph stateGraph)
                        foreach (var u in RetrieveInternal(stateGraph, visitedGraphs)) yield return u;
                }
            }

            foreach (var transition in graph.transitions)
            {
                if (transition is INesterStateTransition nesterStateTransition && nesterStateTransition.nest != null)
                {
                    if (nesterStateTransition.nest.graph is FlowGraph flowGraph)
                        foreach (var u in RetrieveInternal(flowGraph, visitedGraphs, true)) yield return u;
                    else if (nesterStateTransition.nest.graph is StateGraph stateGraph)
                        foreach (var u in RetrieveInternal(stateGraph, visitedGraphs)) yield return u;
                }
            }
        }

        public static IEnumerable<(GraphReference, T)> TraverseFlowGraph<T>(GraphReference graphReference) where T : IGraphElement
        {
            if (!(graphReference.graph is FlowGraph)) yield break;

            FlowGraph flowGraph = (FlowGraph)graphReference.graph;

            foreach (var element in flowGraph.elements)
            {
                if (element == null || !(element is T)) continue;

                switch (element)
                {
                    case SubgraphUnit subgraphUnit:
                        {
                            var subGraph = subgraphUnit.nest.graph;
                            if (subGraph == null) continue;
                            var item = (graphReference, subgraphUnit as IGraphElement);
                            if (item.Item2 is T typedElement)
                            {
                                yield return (item.graphReference, typedElement);
                            }

                            var childReference = graphReference.ChildReference(subgraphUnit, false);
                            foreach (var childItem in TraverseFlowGraph<T>(childReference))
                            {
                                yield return childItem;
                            }
                            break;
                        }
                    case StateUnit stateUnit:
                        {
                            var stateGraph = stateUnit.nest.graph;
                            if (stateGraph == null) continue;

                            var item = (graphReference, stateUnit as IGraphElement);

                            if (item.Item2 is T typedElement)
                            {
                                yield return (item.graphReference, typedElement);
                            }

                            var childReference = graphReference.ChildReference(stateUnit, false);
                            foreach (var childItem in TraverseStateGraph<T>(childReference))
                            {
                                yield return childItem;
                            }
                            break;
                        }
                    default:
                        {
                            var defaultItem = (graphReference, element);
                            if (defaultItem.element is T typedElement)
                            {
                                yield return (defaultItem.graphReference, typedElement);
                            }
                            break;
                        }
                }
            }
        }

        public static IEnumerable<(GraphReference, T)> TraverseStateGraph<T>(GraphReference graphReference) where T : IGraphElement
        {
            if (!(graphReference.graph is StateGraph)) yield break;

            StateGraph stateGraph = (StateGraph)graphReference.graph;

            foreach (var element in stateGraph.states)
            {
                if (element == null || !(element is T)) continue;

                switch (element)
                {
                    case FlowState flowState:
                        {
                            var graph = flowState.nest.graph;
                            if (graph == null) continue;

                            if (flowState is T _state)
                            {
                                yield return (graphReference, _state);
                            }

                            var childReference = graphReference.ChildReference(flowState, false);
                            foreach (var childItem in TraverseFlowGraph<T>(childReference))
                            {
                                yield return childItem;
                            }
                            break;
                        }
                    case SuperState superState:
                        {
                            var subStateGraph = superState.nest.graph;
                            if (subStateGraph == null) continue;

                            if (superState is T _state)
                            {
                                yield return (graphReference, _state);
                            }
                            var childReference = graphReference.ChildReference(superState, false);
                            foreach (var childItem in TraverseStateGraph<T>(childReference))
                            {
                                yield return childItem;
                            }
                            break;
                        }
                    case AnyState:
                        continue;
                    default:
                        {
                            var defaultItem = (graphReference, element);
                            if (defaultItem.element is T typedElement)
                            {
                                yield return (defaultItem.graphReference, typedElement);
                            }
                            break;
                        }
                }
            }

            foreach (var transition in stateGraph.transitions)
            {
                if (!(transition is FlowStateTransition)) continue;

                FlowStateTransition flowStateTransition = (FlowStateTransition)transition;

                var graph = flowStateTransition.nest.graph;
                if (graph == null) continue;

                if (flowStateTransition is T _stateTransition)
                {
                    yield return (graphReference, _stateTransition);
                }
                var childReference = graphReference.ChildReference(flowStateTransition, false);
                foreach (var childItem in TraverseFlowGraph<T>(childReference))
                {
                    yield return childItem;
                }

            }
        }
    }
}