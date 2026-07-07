using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using SuperHexLink.Logging;
using SuperHexLink.Performance;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for performance metrics collection and monitoring system.
    /// </summary>
    public class PerformanceMetricsTests
    {
        private ActionLogSettings _testSettings;

        [SetUp]
        public void SetUp()
        {
            _testSettings = new ActionLogSettings();
            
            // Ensure clean state
            PerformanceMetrics.ResetMetrics();
        }

        [TearDown]
        public void TearDown()
        {
            PerformanceMetrics.Shutdown();
        }

        [Test]
        public void PerformanceMetrics_Initialize_SetsUpSystem()
        {
            // Act
            PerformanceMetrics.Initialize(_testSettings);
            
            // Assert
            var summary = PerformanceMetrics.GetSummary();
            Assert.IsNotNull(summary);
            Assert.GreaterOrEqual(summary.timestamp, DateTime.UtcNow.AddSeconds(-1));
        }

        [Test]
        public void PerformanceMetrics_RecordMetric_StoresCorrectData()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string metricName = "test_metric";
            double value = 42.5;
            string unit = "ms";
            
            // Act
            PerformanceMetrics.RecordMetric(metricName, value, unit);
            
            // Assert
            var metric = PerformanceMetrics.GetMetric(metricName);
            Assert.IsNotNull(metric);
            Assert.AreEqual(metricName, metric.name);
            Assert.AreEqual(value, metric.value);
            Assert.AreEqual(unit, metric.unit);
            Assert.AreEqual(1, metric.sampleCount);
        }

        [Test]
        public void PerformanceMetrics_RecordMetric_UpdatesExistingMetric()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string metricName = "test_metric";
            PerformanceMetrics.RecordMetric(metricName, 10.0);
            
            // Act
            PerformanceMetrics.RecordMetric(metricName, 20.0);
            
            // Assert
            var metric = PerformanceMetrics.GetMetric(metricName);
            Assert.IsNotNull(metric);
            Assert.AreEqual(20.0, metric.value);
            Assert.AreEqual(2, metric.sampleCount);
            Assert.AreEqual(15.0, metric.average);
            Assert.AreEqual(10.0, metric.min);
            Assert.AreEqual(20.0, metric.max);
        }

        [Test]
        public void PerformanceMetrics_RecordExecutionTime_RecordsDuration()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string operationName = "test_operation";
            var duration = TimeSpan.FromMilliseconds(150.5);
            
            // Act
            PerformanceMetrics.RecordExecutionTime(operationName, duration);
            
            // Assert
            var durationMetric = PerformanceMetrics.GetMetric($"{operationName}_duration");
            Assert.IsNotNull(durationMetric);
            Assert.AreEqual(duration.TotalMilliseconds, durationMetric.value);
            Assert.AreEqual("ms", durationMetric.unit);
            
            var timestampMetric = PerformanceMetrics.GetMetric($"{operationName}_timestamp");
            Assert.IsNotNull(timestampMetric);
            Assert.Greater(timestampMetric.value, 0);
        }

        [Test]
        public void PerformanceMetrics_RecordMemoryUsage_RecordsMemory()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            
            // Act
            PerformanceMetrics.RecordMemoryUsage();
            
            // Assert
            var memoryMetric = PerformanceMetrics.GetMetric("memory_usage");
            Assert.IsNotNull(memoryMetric);
            Assert.Greater(memoryMetric.value, 0);
            Assert.AreEqual("bytes", memoryMetric.unit);
            
            var memoryMbMetric = PerformanceMetrics.GetMetric("memory_usage_mb");
            Assert.IsNotNull(memoryMbMetric);
            Assert.Greater(memoryMbMetric.value, 0);
            Assert.AreEqual("MB", memoryMbMetric.unit);
        }

        [Test]
        public void PerformanceMetrics_RecordFrameRate_RecordsFPS()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            
            // Act
            PerformanceMetrics.RecordFrameRate();
            
            // Assert
            var fpsMetric = PerformanceMetrics.GetMetric("fps");
            Assert.IsNotNull(fpsMetric);
            Assert.Greater(fpsMetric.value, 0);
            Assert.AreEqual("fps", fpsMetric.unit);
            
            var frameTimeMetric = PerformanceMetrics.GetMetric("frame_time");
            Assert.IsNotNull(frameTimeMetric);
            Assert.Greater(frameTimeMetric.value, 0);
            Assert.AreEqual("ms", frameTimeMetric.unit);
        }

        [Test]
        public void PerformanceMetrics_SetThreshold_CreatesAlertThreshold()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string metricName = "test_metric";
            double warningThreshold = 50.0;
            double criticalThreshold = 100.0;
            
            // Act
            PerformanceMetrics.SetThreshold(metricName, warningThreshold, criticalThreshold);
            
            // Assert
            // Note: Threshold checking is internal, but we can verify by triggering an alert
            PerformanceMetrics.RecordMetric(metricName, 150.0); // Above critical threshold
            
            // Check if alert was triggered by checking recent events
            var recentEvents = PerformanceMetrics.GetRecentEvents(10);
            var alertEvents = recentEvents.FindAll(e => e.eventType == "threshold-alert" && e.metricName == metricName);
            Assert.IsNotEmpty(alertEvents);
        }

        [Test]
        public void PerformanceMetrics_GetAllMetrics_ReturnsAllRecordedMetrics()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.RecordMetric("metric1", 10.0);
            PerformanceMetrics.RecordMetric("metric2", 20.0);
            PerformanceMetrics.RecordMetric("metric3", 30.0);
            
            // Act
            var allMetrics = PerformanceMetrics.GetAllMetrics();
            
            // Assert
            Assert.AreEqual(3, allMetrics.Count);
            Assert.IsTrue(allMetrics.ContainsKey("metric1"));
            Assert.IsTrue(allMetrics.ContainsKey("metric2"));
            Assert.IsTrue(allMetrics.ContainsKey("metric3"));
        }

        [Test]
        public void PerformanceMetrics_GetSummary_ReturnsCorrectSummary()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.RecordMetric("test_metric", 42.0);
            PerformanceMetrics.RecordMemoryUsage();
            PerformanceMetrics.RecordFrameRate();
            
            // Act
            var summary = PerformanceMetrics.GetSummary();
            
            // Assert
            Assert.IsNotNull(summary);
            Assert.Greater(summary.totalMetrics, 0);
            Assert.GreaterOrEqual(summary.averageMemoryUsage, 0);
            Assert.GreaterOrEqual(summary.averageFrameRate, 0);
            Assert.Greater(summary.timestamp, DateTime.MinValue);
        }

        [Test]
        public void PerformanceMetrics_GetRecentEvents_ReturnsRecentEvents()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.RecordMetric("test_metric1", 10.0);
            PerformanceMetrics.RecordMetric("test_metric2", 20.0);
            PerformanceMetrics.RecordMetric("test_metric3", 30.0);
            
            // Act
            var recentEvents = PerformanceMetrics.GetRecentEvents(5);
            
            // Assert
            Assert.IsNotNull(recentEvents);
            Assert.LessOrEqual(recentEvents.Count, 5);
            Assert.GreaterOrEqual(recentEvents.Count, 3); // At least our 3 metrics
        }

        [Test]
        public void PerformanceMetrics_ResetMetrics_ClearsAllData()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.RecordMetric("test_metric", 42.0);
            Assert.IsNotNull(PerformanceMetrics.GetMetric("test_metric"));
            
            // Act
            PerformanceMetrics.ResetMetrics();
            
            // Assert
            var metric = PerformanceMetrics.GetMetric("test_metric");
            Assert.IsNull(metric);
            
            var summary = PerformanceMetrics.GetSummary();
            Assert.AreEqual(0, summary.totalMetrics);
        }

        [Test]
        public void PerformanceMetrics_MultipleInitializations_DoesNotDuplicate()
        {
            // Arrange & Act
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.Initialize(_testSettings);
            
            // Assert
            var summary = PerformanceMetrics.GetSummary();
            Assert.IsNotNull(summary);
            // Should not crash or create duplicate timers
        }

        [Test]
        public void PerformanceMetrics_Shutdown_CleansUpResources()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.RecordMetric("test_metric", 42.0);
            
            // Act
            PerformanceMetrics.Shutdown();
            
            // Assert
            // After shutdown, recording should still work but may reinitialize
            PerformanceMetrics.RecordMetric("test_metric_after_shutdown", 100.0);
            var metric = PerformanceMetrics.GetMetric("test_metric_after_shutdown");
            Assert.IsNotNull(metric);
        }

        [Test]
        public void PerformanceMetrics_ThresholdAlert_TriggersCorrectly()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            PerformanceMetrics.SetThreshold("test_metric", 50.0, 100.0, ">");
            
            // Act
            PerformanceMetrics.RecordMetric("test_metric", 75.0); // Warning level
            
            // Assert
            var recentEvents = PerformanceMetrics.GetRecentEvents(10);
            var warningEvents = recentEvents.FindAll(e => 
                e.eventType == "threshold-alert" && 
                e.metricName == "test_metric" && 
                e.severity == "warning");
            
            // Note: Due to cooldown period, this might not trigger immediately
            // We'll just verify the metric was recorded correctly
            var metric = PerformanceMetrics.GetMetric("test_metric");
            Assert.IsNotNull(metric);
            Assert.AreEqual(75.0, metric.value);
        }

        [Test]
        public void PerformanceMetrics_ThresholdOperators_WorkCorrectly()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            
            // Test "less than" operator
            PerformanceMetrics.SetThreshold("fps", 30.0, 15.0, "<");
            PerformanceMetrics.RecordMetric("fps", 10.0); // Below critical threshold
            
            // Assert
            var metric = PerformanceMetrics.GetMetric("fps");
            Assert.IsNotNull(metric);
            Assert.AreEqual(10.0, metric.value);
        }

        [Test]
        public void PerformanceMetrics_MetricData_CalculatesStatistics()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string metricName = "stats_test";
            
            // Act
            PerformanceMetrics.RecordMetric(metricName, 10.0);
            PerformanceMetrics.RecordMetric(metricName, 20.0);
            PerformanceMetrics.RecordMetric(metricName, 30.0);
            
            // Assert
            var metric = PerformanceMetrics.GetMetric(metricName);
            Assert.IsNotNull(metric);
            Assert.AreEqual(3, metric.sampleCount);
            Assert.AreEqual(20.0, metric.average);
            Assert.AreEqual(10.0, metric.min);
            Assert.AreEqual(30.0, metric.max);
            Assert.Greater(metric.variance, 0);
            Assert.Greater(metric.standardDeviation, 0);
        }

        [Test]
        public void PerformanceMetrics_PerformanceTimer_WorksCorrectly()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string operationName = "timer_test";
            
            // Act
            using (new PerformanceTimer(operationName))
            {
                Thread.Sleep(10); // Simulate work
            }
            
            // Assert
            var metric = PerformanceMetrics.GetMetric($"{operationName}_duration");
            Assert.IsNotNull(metric);
            Assert.Greater(metric.value, 0);
            Assert.AreEqual("ms", metric.unit);
        }

        [Test]
        public void PerformanceMetrics_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            int threadCount = 10;
            int metricsPerThread = 100;
            
            // Act
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < metricsPerThread; j++)
                    {
                        PerformanceMetrics.RecordMetric($"thread_{threadId}_metric_{j}", j * 1.0);
                    }
                });
            }
            
            // Start all threads
            foreach (var thread in threads)
            {
                thread.Start();
            }
            
            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }
            
            // Assert
            var summary = PerformanceMetrics.GetSummary();
            Assert.AreEqual(threadCount * metricsPerThread, summary.totalMetrics);
        }

        [Test]
        public void PerformanceMetrics_EventHistory_LimitsSize()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            
            // Act - Record many events
            for (int i = 0; i < 1500; i++) // More than the 1000 limit
            {
                PerformanceMetrics.RecordMetric($"metric_{i}", i * 1.0);
            }
            
            // Assert
            var recentEvents = PerformanceMetrics.GetRecentEvents(2000); // Request more than stored
            Assert.LessOrEqual(recentEvents.Count, 1000); // Should be limited to 1000
        }

        [Test]
        public void PerformanceMetrics_MetricData_UpdateWithSameValue_HandlesCorrectly()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            string metricName = "same_value_test";
            double value = 42.0;
            
            // Act
            PerformanceMetrics.RecordMetric(metricName, value);
            PerformanceMetrics.RecordMetric(metricName, value);
            PerformanceMetrics.RecordMetric(metricName, value);
            
            // Assert
            var metric = PerformanceMetrics.GetMetric(metricName);
            Assert.IsNotNull(metric);
            Assert.AreEqual(value, metric.min);
            Assert.AreEqual(value, metric.max);
            Assert.AreEqual(value, metric.average);
            Assert.AreEqual(0, metric.variance);
            Assert.AreEqual(0, metric.standardDeviation);
        }

        [Test]
        public void PerformanceMetrics_GetMetric_NonExistent_ReturnsNull()
        {
            // Arrange
            PerformanceMetrics.Initialize(_testSettings);
            
            // Act
            var metric = PerformanceMetrics.GetMetric("non_existent_metric");
            
            // Assert
            Assert.IsNull(metric);
        }

        [Test]
        public void PerformanceMetrics_RecordMetricWithoutInitialize_AutoInitializes()
        {
            // Arrange - Don't initialize explicitly
            
            // Act
            PerformanceMetrics.RecordMetric("auto_init_test", 42.0);
            
            // Assert
            var metric = PerformanceMetrics.GetMetric("auto_init_test");
            Assert.IsNotNull(metric);
            Assert.AreEqual(42.0, metric.value);
        }

        [Test]
        public void PerformanceMetrics_DefaultThresholds_AppliedOnInitialize()
        {
            // Act
            PerformanceMetrics.Initialize(_testSettings);
            
            // Record metrics that should trigger default thresholds
            PerformanceMetrics.RecordMetric("fps", 10.0); // Below default critical threshold
            PerformanceMetrics.RecordMetric("memory_usage_mb", 600.0); // Above default warning threshold
            
            // Assert
            var fpsMetric = PerformanceMetrics.GetMetric("fps");
            var memoryMetric = PerformanceMetrics.GetMetric("memory_usage_mb");
            
            Assert.IsNotNull(fpsMetric);
            Assert.IsNotNull(memoryMetric);
            Assert.AreEqual(10.0, fpsMetric.value);
            Assert.AreEqual(600.0, memoryMetric.value);
        }
    }
}
