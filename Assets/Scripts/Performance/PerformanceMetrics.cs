using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Performance
{
    /// <summary>
    /// Performance metrics collection and analysis system.
    /// Provides real-time performance monitoring with configurable thresholds and alerts.
    /// </summary>
    public static class PerformanceMetrics
    {
        private static readonly ConcurrentDictionary<string, MetricData> _metrics = new ConcurrentDictionary<string, MetricData>();
        private static readonly ConcurrentDictionary<string, AlertThreshold> _thresholds = new ConcurrentDictionary<string, AlertThreshold>();
        private static readonly ConcurrentQueue<PerformanceEvent> _eventHistory = new ConcurrentQueue<PerformanceEvent>();
        private static Timer _collectionTimer;
        private static ActionLogSettings _logSettings;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Performance metric data structure.
        /// </summary>
        [Serializable]
        public class MetricData
        {
            public string name;
            public double value;
            public string unit;
            public DateTime timestamp;
            public double min;
            public double max;
            public double average;
            public long sampleCount;
            public double sum;
            public double variance;
            public double standardDeviation;

            public MetricData(string name, double value, string unit)
            {
                this.name = name;
                this.value = value;
                this.unit = unit;
                this.timestamp = DateTime.UtcNow;
                this.min = value;
                this.max = value;
                this.average = value;
                this.sampleCount = 1;
                this.sum = value;
                this.variance = 0;
                this.standardDeviation = 0;
            }

            /// <summary>
            /// Update metric with new value.
            /// </summary>
            public void Update(double newValue)
            {
                timestamp = DateTime.UtcNow;
                value = newValue;
                sampleCount++;
                sum += newValue;
                average = sum / sampleCount;
                
                if (newValue < min) min = newValue;
                if (newValue > max) max = newValue;

                // Calculate variance and standard deviation
                double oldVariance = variance;
                variance = ((sampleCount - 1) * oldVariance + Math.Pow(newValue - average, 2)) / sampleCount;
                standardDeviation = Math.Sqrt(variance);
            }
        }

        /// <summary>
        /// Alert threshold configuration.
        /// </summary>
        [Serializable]
        public class AlertThreshold
        {
            public string metricName;
            public double warningThreshold;
            public double criticalThreshold;
            public string comparisonOperator; // ">", "<", ">=", "<=", "=="
            public bool isEnabled;
            public DateTime lastAlert;
            public TimeSpan cooldownPeriod;

            public AlertThreshold(string metricName, double warning, double critical, string op = ">")
            {
                this.metricName = metricName;
                this.warningThreshold = warning;
                this.criticalThreshold = critical;
                this.comparisonOperator = op;
                this.isEnabled = true;
                this.lastAlert = DateTime.MinValue;
                this.cooldownPeriod = TimeSpan.FromMinutes(5);
            }
        }

        /// <summary>
        /// Performance event for history tracking.
        /// </summary>
        [Serializable]
        public class PerformanceEvent
        {
            public string eventType;
            public string metricName;
            public double value;
            public DateTime timestamp;
            public string severity;
            public Dictionary<string, object> context;

            public PerformanceEvent(string eventType, string metricName, double value, string severity, Dictionary<string, object> context = null)
            {
                this.eventType = eventType;
                this.metricName = metricName;
                this.value = value;
                this.timestamp = DateTime.UtcNow;
                this.severity = severity;
                this.context = context ?? new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Initialize the performance monitoring system.
        /// </summary>
        public static void Initialize(ActionLogSettings logSettings = null, int collectionIntervalMs = 1000)
        {
            if (_isInitialized) return;

            lock (_lockObject)
            {
                if (_isInitialized) return;

                _logSettings = logSettings ?? new ActionLogSettings();
                _logSettings.EnableCategory(ActionLogCategory.Performance, true);

                // Set up default thresholds
                SetupDefaultThresholds();

                // Start collection timer
                _collectionTimer = new Timer(CollectMetrics, null, 0, collectionIntervalMs);

                _isInitialized = true;

                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                    "Performance monitoring system initialized");
            }
        }

        /// <summary>
        /// Record a performance metric.
        /// </summary>
        public static void RecordMetric(string name, double value, string unit = "ms")
        {
            if (!_isInitialized) Initialize();

            var metric = _metrics.AddOrUpdate(name, 
                new MetricData(name, value, unit),
                (key, existing) => { existing.Update(value); return existing; });

            // Check thresholds
            CheckThresholds(name, value);

            // Add to event history
            var eventData = new PerformanceEvent("metric-recorded", name, value, "info");
            _eventHistory.Enqueue(eventData);

            // Trim history if too large
            while (_eventHistory.Count > 1000)
            {
                _eventHistory.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Record execution time for an operation.
        /// </summary>
        public static void RecordExecutionTime(string operationName, TimeSpan duration)
        {
            RecordMetric($"{operationName}_duration", duration.TotalMilliseconds, "ms");
            RecordMetric($"{operationName}_timestamp", DateTime.UtcNow.Ticks, "ticks");
        }

        /// <summary>
        /// Record memory usage.
        /// </summary>
        public static void RecordMemoryUsage()
        {
            long memoryUsage = GC.GetTotalMemory(false);
            RecordMetric("memory_usage", memoryUsage, "bytes");
            RecordMetric("memory_usage_mb", memoryUsage / (1024.0 * 1024.0), "MB");
        }

        /// <summary>
        /// Record frame rate.
        /// </summary>
        public static void RecordFrameRate()
        {
            float fps = 1.0f / Time.unscaledDeltaTime;
            RecordMetric("fps", fps, "fps");
            RecordMetric("frame_time", Time.unscaledDeltaTime * 1000.0, "ms");
        }

        /// <summary>
        /// Set alert threshold for a metric.
        /// </summary>
        public static void SetThreshold(string metricName, double warningThreshold, double criticalThreshold, string comparisonOperator = ">")
        {
            var threshold = new AlertThreshold(metricName, warningThreshold, criticalThreshold, comparisonOperator);
            _thresholds.AddOrUpdate(metricName, threshold, (key, existing) => threshold);

            ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                $"Alert threshold set for {metricName}: Warning={warningThreshold}, Critical={criticalThreshold}");
        }

        /// <summary>
        /// Get current metric data.
        /// </summary>
        public static MetricData GetMetric(string name)
        {
            _metrics.TryGetValue(name, out var metric);
            return metric;
        }

        /// <summary>
        /// Get all current metrics.
        /// </summary>
        public static Dictionary<string, MetricData> GetAllMetrics()
        {
            return new Dictionary<string, MetricData>(_metrics);
        }

        /// <summary>
        /// Get performance summary.
        /// </summary>
        public static PerformanceSummary GetSummary()
        {
            var summary = new PerformanceSummary
            {
                timestamp = DateTime.UtcNow,
                totalMetrics = _metrics.Count,
                activeAlerts = _thresholds.Values.Count(t => t.isEnabled),
                eventHistorySize = _eventHistory.Count
            };

            // Calculate aggregates
            if (_metrics.Any())
            {
                summary.averageMemoryUsage = _metrics.ContainsKey("memory_usage_mb") ? 
                    _metrics["memory_usage_mb"].average : 0;
                summary.averageFrameRate = _metrics.ContainsKey("fps") ? 
                    _metrics["fps"].average : 0;
                summary.averageExecutionTime = _metrics.Where(m => m.Key.Contains("_duration"))
                    .Average(m => m.Value.average);
            }

            return summary;
        }

        /// <summary>
        /// Get recent performance events.
        /// </summary>
        public static List<PerformanceEvent> GetRecentEvents(int count = 100)
        {
            return _eventHistory.ToArray().TakeLast(count).ToList();
        }

        /// <summary>
        /// Reset all metrics.
        /// </summary>
        public static void ResetMetrics()
        {
            _metrics.Clear();
            _eventHistory.Clear();

            ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                "All performance metrics reset");
        }

        /// <summary>
        /// Shutdown the performance monitoring system.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lockObject)
            {
                if (!_isInitialized) return;

                _collectionTimer?.Dispose();
                _collectionTimer = null;

                _isInitialized = false;

                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                    "Performance monitoring system shutdown");
            }
        }

        /// <summary>
        /// Timer callback for periodic metric collection.
        /// </summary>
        private static void CollectMetrics(object state)
        {
            try
            {
                // Collect system metrics
                RecordMemoryUsage();
                RecordFrameRate();

                // Log summary periodically (every 30 seconds)
                if (DateTime.UtcNow.Second % 30 == 0)
                {
                    var summary = GetSummary();
                    ActionLogger.LogStructured(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                        "Performance summary", new Dictionary<string, object>
                        {
                            ["totalMetrics"] = summary.totalMetrics,
                            ["averageMemoryMB"] = summary.averageMemoryUsage.ToString("F2"),
                            ["averageFPS"] = summary.averageFrameRate.ToString("F1"),
                            ["activeAlerts"] = summary.activeAlerts
                        });
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Error, 
                    $"Error collecting metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if metric exceeds thresholds and trigger alerts.
        /// </summary>
        private static void CheckThresholds(string metricName, double value)
        {
            if (!_thresholds.TryGetValue(metricName, out var threshold) || !threshold.isEnabled)
                return;

            // Check cooldown period
            if (DateTime.UtcNow - threshold.lastAlert < threshold.cooldownPeriod)
                return;

            bool alertTriggered = false;
            string severity = "info";

            switch (threshold.comparisonOperator)
            {
                case ">":
                    alertTriggered = value > threshold.criticalThreshold;
                    if (alertTriggered) severity = "critical";
                    else if (value > threshold.warningThreshold) { alertTriggered = true; severity = "warning"; }
                    break;
                case "<":
                    alertTriggered = value < threshold.criticalThreshold;
                    if (alertTriggered) severity = "critical";
                    else if (value < threshold.warningThreshold) { alertTriggered = true; severity = "warning"; }
                    break;
                case ">=":
                    alertTriggered = value >= threshold.criticalThreshold;
                    if (alertTriggered) severity = "critical";
                    else if (value >= threshold.warningThreshold) { alertTriggered = true; severity = "warning"; }
                    break;
                case "<=":
                    alertTriggered = value <= threshold.criticalThreshold;
                    if (alertTriggered) severity = "critical";
                    else if (value <= threshold.warningThreshold) { alertTriggered = true; severity = "warning"; }
                    break;
            }

            if (alertTriggered)
            {
                threshold.lastAlert = DateTime.UtcNow;

                var alertEvent = new PerformanceEvent("threshold-alert", metricName, value, severity, 
                    new Dictionary<string, object>
                    {
                        ["threshold"] = severity == "critical" ? threshold.criticalThreshold : threshold.warningThreshold,
                        ["operator"] = threshold.comparisonOperator
                    });

                _eventHistory.Enqueue(alertEvent);

                ActionLogger.LogSecurity(_logSettings, "performance-threshold", 
                    $"Performance alert: {metricName} = {value:F2} {threshold.comparisonOperator} {threshold.warningThreshold}/{threshold.criticalThreshold}",
                    new Dictionary<string, object>
                    {
                        ["metricName"] = metricName,
                        ["value"] = value,
                        ["severity"] = severity,
                        ["threshold"] = severity == "critical" ? threshold.criticalThreshold : threshold.warningThreshold
                    });
            }
        }

        /// <summary>
        /// Set up default performance thresholds.
        /// </summary>
        private static void SetupDefaultThresholds()
        {
            // FPS thresholds
            SetThreshold("fps", 30.0, 15.0, "<");  // Warning below 30, Critical below 15
            
            // Memory thresholds (in MB)
            SetThreshold("memory_usage_mb", 500.0, 1000.0, ">");  // Warning above 500MB, Critical above 1GB
            
            // Frame time thresholds (in ms)
            SetThreshold("frame_time", 33.3, 66.6, ">");  // Warning above 33ms, Critical above 66ms
            
            // Generic operation duration thresholds
            SetThreshold("database-query_duration", 100.0, 500.0, ">");  // Warning above 100ms, Critical above 500ms
            SetThreshold("network-request_duration", 1000.0, 5000.0, ">");  // Warning above 1s, Critical above 5s
            SetThreshold("file-io_duration", 50.0, 200.0, ">");  // Warning above 50ms, Critical above 200ms
        }
    }

    /// <summary>
    /// Performance summary data structure.
    /// </summary>
    [Serializable]
    public class PerformanceSummary
    {
        public DateTime timestamp;
        public int totalMetrics;
        public double averageMemoryUsage;
        public double averageFrameRate;
        public double averageExecutionTime;
        public int activeAlerts;
        public int eventHistorySize;
    }

    /// <summary>
    /// Helper class for measuring execution time.
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        public PerformanceTimer(string operationName)
        {
            _operationName = operationName;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            PerformanceMetrics.RecordExecutionTime(_operationName, _stopwatch.Elapsed);
        }
    }
}
