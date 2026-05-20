using System.Collections;
using Miraluna.Uvs.Benchmarks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Miraluna.Uvs.Benchmarks.Tests
{
    public abstract class BenchmarkPerformanceTestBase
    {
        [SetUp]
        public void SetUp()
        {
            BenchmarkEnvironment.Teardown();
            UvsPackageProbe.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            BenchmarkEnvironment.Teardown();
        }

        protected IEnumerator RunFrameBenchmark(BenchmarkAgentKind kind, int objectCount)
        {
            BenchmarkRunContext.Configure(kind, objectCount);
            BenchmarkEnvironment.EnsureInitialized();
            BenchmarkEnvironment.SpawnCurrent();

            yield return null;

            var sampleGroup = BenchmarkRunContext.SampleGroupPrefix;

            yield return Measure
                .Frames()
                .SampleGroup(sampleGroup)
                .WarmupCount(BenchmarkRunContext.WarmupFrames)
                .MeasurementCount(BenchmarkRunContext.MeasurementFrames)
                .Run();

            BenchmarkEnvironment.Teardown();
        }
    }
}
