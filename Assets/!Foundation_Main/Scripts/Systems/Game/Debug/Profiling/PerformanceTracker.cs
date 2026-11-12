using System;
using System.Collections.Generic;
using UnityEngine;
using static Foundation.Systems.Game.DebugController;

namespace Foundation.Systems.Game
{
    public class PerformanceTracker : MonoBehaviour, IGameService
    {
        #region PUBLIC PARAMETERS
        public PerformanceSnapshot CurrentSnapshot { get; private set; }
        public event Action<PerformanceSnapshot> SnapshotUpdated;
        #endregion

        #region PRIVATE PARAMETERS
        [SerializeField, Min(0.1f)]
        private float sampleIntervalSeconds = 5f;
        [SerializeField, Min(0.05f)]
        private float publishSnapshotIntervalSeconds = 0.25f;
        [SerializeField, Min(1f)]
        private float hitchThresholdMs = 50f;

        private readonly Queue<FrameSample> samples = new();
        private readonly FrameTiming[] frameTimings = new FrameTiming[1];

        private float elapsedSeconds;
        private float publishTimer;
        private float currentFps;
        private float cpuFrameTimeMs;
        private float gpuFrameTimeMs;
        private bool hasFrameTiming;
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION
        private DebugController debugController;
        private bool initialized;
        public void Initialize(DebugController debug)
        {
            if (initialized) throw new InvalidOperationException("PerformanceTracker is already initialized.");

            debugController = debug;
            initialized = true;

            debugController.Log(DebugCategory.General, DebugPersistence.RuntimeOnly, "PerformanceTracker Initialized", "PerformanceTracker");
        }

        public void Shutdown()
        {
            if (!initialized) return;

            initialized = false;
            debugController.Log(DebugCategory.General, DebugPersistence.RuntimeOnly, "PerformanceTracker Shutdown", "PerformanceTracker");
            debugController = null;
        }
        #endregion

        #region UNITY CALLBACKS
        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f) return;

            elapsedSeconds += deltaTime;
            publishTimer += deltaTime;
            currentFps = 1f / deltaTime;

            samples.Enqueue(new FrameSample(elapsedSeconds, deltaTime));
            TrimSampleWindow();
            CaptureFrameTiming();

            if (publishTimer >= publishSnapshotIntervalSeconds)
            {
                publishTimer = 0f;
                PublishSnapshot();
            }

        }
        #endregion

        #region PUBLIC METHODS
        public void ResetHitchCount()
        {

        }
        #endregion

        #region PRIVATE METHODS
        private void TrimSampleWindow()
        {
            while (samples.Count > 0 && elapsedSeconds - samples.Peek().Time > sampleIntervalSeconds)
            {
                samples.Dequeue();
            }
        }

        private void CaptureFrameTiming()
        {
            FrameTimingManager.CaptureFrameTimings();

            if (FrameTimingManager.GetLatestTimings(1, frameTimings) == 0) return;

            cpuFrameTimeMs = (float)frameTimings[0].cpuFrameTime;
            gpuFrameTimeMs = (float)frameTimings[0].gpuFrameTime;
            hasFrameTiming = true;
        }

        private void PublishSnapshot()
        {
            float totalFrameTime = 0f;
            float maxFrameTime = 0f;
            int hitchesInWindow = 0;
            float[] sortedFrameTimes = new float[samples.Count];
            int index = 0;

            foreach (FrameSample sample in samples)
            {
                totalFrameTime += sample.DeltaTime;
                maxFrameTime = Mathf.Max(maxFrameTime, sample.DeltaTime);

                if (sample.DeltaTime * 1000f > hitchThresholdMs) hitchesInWindow++;

                sortedFrameTimes[index++] = sample.DeltaTime * 1000f;
            }

            Array.Sort(sortedFrameTimes);

            float averageFps = totalFrameTime > 0f ? samples.Count / totalFrameTime : 0f;
            float averageFrameTimeMs = samples.Count > 0 ? totalFrameTime / samples.Count * 1000f : 0f;
            float p95FrameTimeMs = sortedFrameTimes.Length > 0 ? sortedFrameTimes[(int)(sortedFrameTimes.Length * 0.95f)] : 0f;

            CurrentSnapshot = new PerformanceSnapshot(
                currentFps,
                averageFps,
                averageFrameTimeMs,
                p95FrameTimeMs,
                maxFrameTime * 1000f,
                cpuFrameTimeMs,
                gpuFrameTimeMs,
                hasFrameTiming
            );
            SnapshotUpdated?.Invoke(CurrentSnapshot);
        }
        #endregion

        #region NESTED TYPES
        public readonly struct PerformanceSnapshot
        {
            //FPS METRICS
            public float CurrentFPS { get; }
            public float AverageFPS { get; }
            public float AverageFrameTime { get; }
            public float P95FrameTime { get; }
            public float MaxFrameTime { get; }
            public float CpuFrameTimeMs { get; }
            public float GpuFrameTimeMs { get; }
            public bool HasFrameTiming { get; }

            internal PerformanceSnapshot(
                float currentFPS,
                float averageFPS,
                float averageFrameTime,
                float p95FrameTime,
                float maxFrameTime,
                float cpuFrameTimeMs,
                float gpuFrameTimeMs,
                bool hasFrameTiming)
            {
                CurrentFPS = currentFPS;
                AverageFPS = averageFPS;
                AverageFrameTime = averageFrameTime;
                P95FrameTime = p95FrameTime;
                MaxFrameTime = maxFrameTime;
                CpuFrameTimeMs = cpuFrameTimeMs;
                GpuFrameTimeMs = gpuFrameTimeMs;
                HasFrameTiming = hasFrameTiming;
            }

        }

        private struct FrameSample
        {
            public float Time;
            public float DeltaTime;

            public FrameSample(float time, float deltaTime)
            {
                Time = time;
                DeltaTime = deltaTime;
            }
        }
        #endregion
    }
}
