using System.Collections;
using Miraluna.Uvs.Benchmarks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Miraluna.Uvs.Benchmarks.Tests
{
    [TestFixture]
    public sealed class RotateUpdateTests : BenchmarkPerformanceTestBase
    {
        [UnityTest, Performance]
        public IEnumerator UvsRotate_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsRotate, 100);
        }

        [UnityTest, Performance]
        public IEnumerator UvsRotate_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsRotate, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator UvsRotate_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsRotate, 5000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpRotate_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpRotate, 100);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpRotate_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpRotate, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpRotate_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpRotate, 5000);
        }
    }
}
