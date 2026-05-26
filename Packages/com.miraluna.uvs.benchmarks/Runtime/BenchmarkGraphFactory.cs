using System.Reflection;
using UnityEngine;
using Unity.VisualScripting;

namespace Miraluna.Uvs.Benchmarks
{
    public static class BenchmarkGraphFactory
    {
        public const string CounterVariableName = "counter";
        public const float RotateRandomMin = 0f;
        public const float RotateRandomMax = 222f;

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

        public static FlowGraph CreateRotateGraph()
        {
            var graph = new FlowGraph();

            var update = new Update { position = new Vector2(-700f, 0f) };
            var literalMin = new Literal(typeof(float), RotateRandomMin) { position = new Vector2(-500f, 120f) };
            var literalMax = new Literal(typeof(float), RotateRandomMax) { position = new Vector2(-500f, -120f) };

            var randomRangeMethod = typeof(Random).GetMethod(
                nameof(Random.Range),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(float), typeof(float) },
                null
            );
            var randomRange = new InvokeMember(new Member(typeof(Random), randomRangeMethod))
            {
                position = new Vector2(-350f, 0f),
            };

            var vector3Constructor = typeof(Vector3).GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            var vector3Create = new InvokeMember(new Member(typeof(Vector3), vector3Constructor))
            {
                position = new Vector2(-150f, 0f),
            };

            var thisUnit = new This { position = new Vector2(-350f, 200f) };
            var transformProperty = typeof(Component).GetProperty(nameof(Component.transform));
            var getTransform = new GetMember(new Member(typeof(Component), transformProperty))
            {
                position = new Vector2(-150f, 200f),
            };

            var rotateMethod = typeof(Transform).GetMethod(
                nameof(Transform.Rotate),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Vector3) },
                null
            );
            var rotate = new InvokeMember(new Member(typeof(Transform), rotateMethod)) { position = new Vector2(50f, 0f) };

            graph.units.Add(update);
            graph.units.Add(literalMin);
            graph.units.Add(literalMax);
            graph.units.Add(randomRange);
            graph.units.Add(vector3Create);
            graph.units.Add(thisUnit);
            graph.units.Add(getTransform);
            graph.units.Add(rotate);

            update.trigger.ConnectToValid(rotate.enter);

            literalMin.output.ConnectToValid(randomRange.inputParameters[0]);
            literalMax.output.ConnectToValid(randomRange.inputParameters[1]);

            var randomValue = randomRange.result;
            randomValue.ConnectToValid(vector3Create.inputParameters[0]);
            randomValue.ConnectToValid(vector3Create.inputParameters[1]);
            randomValue.ConnectToValid(vector3Create.inputParameters[2]);

            thisUnit.self.ConnectToValid(getTransform.target);
            getTransform.value.ConnectToValid(rotate.target);
            vector3Create.result.ConnectToValid(rotate.inputParameters[0]);

            return graph;
        }

        public static FlowGraph Create(BenchmarkGraphKind kind)
        {
            return kind switch
            {
                BenchmarkGraphKind.Overhead => CreateOverheadGraph(),
                BenchmarkGraphKind.Counter => CreateCounterGraph(),
                BenchmarkGraphKind.Rotate => CreateRotateGraph(),
                _ => CreateOverheadGraph(),
            };
        }
    }
}
