using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace Miraluna.Uvs.Benchmarks
{
    public sealed class BenchmarkSpawner : MonoBehaviour
    {
        public static BenchmarkSpawner Instance { get; private set; }

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Clear();
        }

        public void Spawn(BenchmarkAgentKind kind, int count)
        {
            Clear();

            for (var i = 0; i < count; i++)
            {
                var position = new Vector3((i % 32) * 2f, 0f, (i / 32) * 2f);
                _spawned.Add(CreateAgent(kind, position));
            }
        }

        public void Clear()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        private static GameObject CreateAgent(BenchmarkAgentKind kind, Vector3 position)
        {
            var go = new GameObject($"Agent_{kind}");
            go.transform.position = position;

            switch (kind)
            {
                case BenchmarkAgentKind.UvsOverhead:
                    go.AddComponent<Variables>();
                    go.AddComponent<ScriptMachine>();
                    var overhead = go.AddComponent<BenchmarkUvsAgent>();
                    overhead.graphKind = BenchmarkGraphKind.Overhead;
                    break;

                case BenchmarkAgentKind.UvsCounter:
                    go.AddComponent<Variables>();
                    go.AddComponent<ScriptMachine>();
                    var counter = go.AddComponent<BenchmarkUvsAgent>();
                    counter.graphKind = BenchmarkGraphKind.Counter;
                    break;

                case BenchmarkAgentKind.CSharpOverhead:
                    go.AddComponent<EmptyUpdateBehaviour>();
                    break;

                case BenchmarkAgentKind.CSharpCounter:
                    go.AddComponent<CounterBehaviour>();
                    break;
            }

            return go;
        }
    }
}
