namespace Miraluna.Uvs.Benchmarks
{
    public static class BenchmarkRunContext
    {
        public const int DefaultWarmupFrames = 120;
        public const int DefaultMeasurementFrames = 300;

        public static BenchmarkAgentKind AgentKind { get; private set; }
        public static int ObjectCount { get; private set; } = 100;
        public static int WarmupFrames { get; private set; } = DefaultWarmupFrames;
        public static int MeasurementFrames { get; private set; } = DefaultMeasurementFrames;

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

        public static string SampleGroupPrefix =>
            $"{AgentKind}_{ObjectCount}_{UvsPackageProbe.SourceLabel}_{UvsPackageProbe.VersionLabel}";
    }
}
