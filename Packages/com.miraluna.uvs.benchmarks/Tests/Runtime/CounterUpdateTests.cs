using System.Collections;
using Miraluna.Uvs.Benchmarks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Miraluna.Uvs.Benchmarks.Tests
{
    [TestFixture]
    public sealed class CounterUpdateTests : BenchmarkPerformanceTestBase
    {
        [UnityTest, Performance]
        public IEnumerator UvsCounter_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsCounter, 100);
        }

        [UnityTest, Performance]
        public IEnumerator UvsCounter_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsCounter, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator UvsCounter_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsCounter, 5000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpCounter_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpCounter, 100);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpCounter_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpCounter, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpCounter_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpCounter, 5000);
        }
    }
}
