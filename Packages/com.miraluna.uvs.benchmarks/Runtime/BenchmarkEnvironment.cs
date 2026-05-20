using UnityEngine;

namespace Miraluna.Uvs.Benchmarks
{
    public static class BenchmarkEnvironment
    {
        private static GameObject _root;

        public static void EnsureInitialized()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (BenchmarkSpawner.Instance != null)
            {
                return;
            }

            _root = new GameObject("BenchmarkEnvironment");
            Object.DontDestroyOnLoad(_root);
            _root.AddComponent<BenchmarkSpawner>();
        }

        public static void SpawnCurrent()
        {
            EnsureInitialized();
            BenchmarkSpawner.Instance.Spawn(BenchmarkRunContext.AgentKind, BenchmarkRunContext.ObjectCount);
        }

        public static void Teardown()
        {
            if (BenchmarkSpawner.Instance != null)
            {
                BenchmarkSpawner.Instance.Clear();
            }

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }
    }
}
