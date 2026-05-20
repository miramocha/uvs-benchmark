using System.Collections;
using Miraluna.Uvs.Benchmarks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Miraluna.Uvs.Benchmarks.Tests
{
    [TestFixture]
    public sealed class UpdateOverheadTests : BenchmarkPerformanceTestBase
    {
        [UnityTest, Performance]
        public IEnumerator UvsOverhead_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsOverhead, 100);
        }

        [UnityTest, Performance]
        public IEnumerator UvsOverhead_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsOverhead, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator UvsOverhead_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.UvsOverhead, 5000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpOverhead_100()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpOverhead, 100);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpOverhead_1000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpOverhead, 1000);
        }

        [UnityTest, Performance]
        public IEnumerator CSharpOverhead_5000()
        {
            yield return RunFrameBenchmark(BenchmarkAgentKind.CSharpOverhead, 5000);
        }
    }
}
