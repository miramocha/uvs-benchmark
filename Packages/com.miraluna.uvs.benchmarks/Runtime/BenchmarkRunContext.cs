namespace Miraluna.Uvs.Benchmarks
{
    public static class BenchmarkRunContext
    {
        public const int DefaultWarmupFrames = 120;
        public const int DefaultMeasurementFrames = 300;

        /// <summary>Unity Profiler marker sampled with frame time (reported as nanoseconds per frame).</summary>
        public const string GcAllocProfilerMarker = "GC.Alloc";

        /// <summary>Profiler memory counter: managed allocation events per frame.</summary>
        public const string GcAllocationInFrameCountCounter = "GC Allocation In Frame Count";

        public static string GcAllocCountSampleGroup => $"{SampleGroupPrefix}_GCAllocCount";

        public static BenchmarkAgentKind AgentKind { get; private set; }
        public static int ObjectCount { get; private set; } = 100;
        public static int WarmupFrames { get; private set; } = DefaultWarmupFrames;
        public static int MeasurementFrames { get; private set; } = DefaultMeasurementFrames;

        public static string VersionLabel { get; private set; } = "unknown";
        public static string SourceLabel { get; private set; } = "unknown";

        public static void Configure(
            BenchmarkAgentKind agentKind,
            int objectCount,
            int warmupFrames = DefaultWarmupFrames,
            int measurementFrames = DefaultMeasurementFrames)
        {
            AgentKind = agentKind;
            ObjectCount = objectCount;
            WarmupFrames = warmupFrames;
            MeasurementFrames = measurementFrames;
        }

        public static void SetPackageLabels(string version, string source)
        {
            VersionLabel = version;
            SourceLabel = source;
        }

        public static string SampleGroupPrefix =>
            $"{AgentKind}_{ObjectCount}_{SourceLabel}_{VersionLabel}";
    }
}
