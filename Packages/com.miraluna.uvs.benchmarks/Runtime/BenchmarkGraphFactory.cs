using UnityEngine;
using Unity.VisualScripting;

namespace Miraluna.Uvs.Benchmarks
{
    public static class BenchmarkGraphFactory
    {
        public const string CounterVariableName = "counter";

        public static FlowGraph CreateOverheadGraph()
        {
            var graph = new FlowGraph();
            graph.units.Add(new Update { position = Vector2.zero });
            return graph;
        }

        public static FlowGraph CreateCounterGraph()
        {
            var graph = new FlowGraph();

            var update = new Update { position = new Vector2(-400f, 0f) };
            var getVar = new GetVariable { kind = VariableKind.Object };
            getVar.defaultValues["name"] = CounterVariableName;

            var literalOne = new Literal(typeof(int), 1) { position = new Vector2(-100f, 120f) };
            var add = new GenericSum { position = new Vector2(-100f, 0f) };

            var setVar = new SetVariable { kind = VariableKind.Object };
            setVar.defaultValues["name"] = CounterVariableName;

            graph.units.Add(update);
            graph.units.Add(getVar);
            graph.units.Add(literalOne);
            graph.units.Add(add);
            graph.units.Add(setVar);

            update.trigger.ConnectToValid(setVar.assign);
            getVar.value.ConnectToValid(add.multiInputs[0]);
            literalOne.output.ConnectToValid(add.multiInputs[1]);
            add.sum.ConnectToValid(setVar.input);

            return graph;
        }

        public static FlowGraph Create(BenchmarkGraphKind kind)
        {
            return kind switch
            {
                BenchmarkGraphKind.Overhead => CreateOverheadGraph(),
                BenchmarkGraphKind.Counter => CreateCounterGraph(),
                _ => CreateOverheadGraph(),
            };
        }
    }
}
