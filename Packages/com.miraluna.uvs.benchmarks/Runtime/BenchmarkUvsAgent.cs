using UnityEngine;
using Unity.VisualScripting;

namespace Miraluna.Uvs.Benchmarks
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(ScriptMachine))]
    [RequireComponent(typeof(Variables))]
    public sealed class BenchmarkUvsAgent : MonoBehaviour
    {
        public BenchmarkGraphKind graphKind = BenchmarkGraphKind.Counter;

        private void Awake()
        {
            var machine = GetComponent<ScriptMachine>();
            machine.nest.SwitchToEmbed(BenchmarkGraphFactory.Create(graphKind));

            if (graphKind == BenchmarkGraphKind.Counter)
            {
                GetComponent<Variables>().declarations.Set(BenchmarkGraphFactory.CounterVariableName, 0);
            }
        }
    }
}
