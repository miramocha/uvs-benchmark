using System;
using System.Collections;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;

namespace Miraluna.Uvs.Benchmarks.Tests
{
    internal static class BenchmarkGcAllocCountMeasurement
    {
        public static IEnumerator Run(string sampleGroupName)
        {
            var sampleGroup = new SampleGroup(sampleGroupName, SampleUnit.Undefined);

            using var recording = CreateRecorder();
            if (!recording.Valid)
            {
                Debug.LogError(
                    "GC alloc count recorder is invalid. Expected counter '"
                        + BenchmarkRunContext.GcAllocationInFrameCountCounter
                        + "' or marker '"
                        + BenchmarkRunContext.GcAllocProfilerMarker
                        + "'."
                );
                yield break;
            }

            for (var i = 0; i < BenchmarkRunContext.WarmupFrames; i++)
            {
                yield return null;
            }

            for (var i = 0; i < BenchmarkRunContext.MeasurementFrames; i++)
            {
                yield return null;
                Measure.Custom(sampleGroup, ReadCount(recording));
            }
        }

        static GcAllocCountRecording CreateRecorder()
        {
            var capacity =
                BenchmarkRunContext.WarmupFrames + BenchmarkRunContext.MeasurementFrames + 32;

            var counter = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                BenchmarkRunContext.GcAllocationInFrameCountCounter,
                capacity,
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached
            );

            if (counter.Valid)
            {
                return new GcAllocCountRecording(counter, usesMarkerSubsampleCount: false);
            }

            counter.Dispose();

            var marker = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                BenchmarkRunContext.GcAllocProfilerMarker,
                capacity,
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached
                    | ProfilerRecorderOptions.SumAllSamplesInFrame
            );

            return new GcAllocCountRecording(marker, usesMarkerSubsampleCount: true);
        }

        static double ReadCount(GcAllocCountRecording recording)
        {
            var recorder = recording.Recorder;
            if (recorder.Count == 0)
            {
                return 0;
            }

            if (recording.UsesMarkerSubsampleCount)
            {
                return recorder.GetSample(recorder.Count - 1).Count;
            }

            return recorder.LastValueAsDouble;
        }

        readonly struct GcAllocCountRecording : IDisposable
        {
            public readonly ProfilerRecorder Recorder;
            public readonly bool UsesMarkerSubsampleCount;

            public GcAllocCountRecording(ProfilerRecorder recorder, bool usesMarkerSubsampleCount)
            {
                Recorder = recorder;
                UsesMarkerSubsampleCount = usesMarkerSubsampleCount;
            }

            public bool Valid => Recorder.Valid;

            public void Dispose()
            {
                Recorder.Dispose();
            }
        }
    }
}
