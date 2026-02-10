using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SuperHexLink.Logging;
using SuperHexLink.Performance;

namespace SuperHexLink.Performance
{
    /// <summary>
    /// Real-time performance dashboard UI.
    /// Provides visual monitoring of system performance metrics and alerts.
    /// </summary>
    public class PerformanceDashboard : MonoBehaviour
    {
        [Header("Dashboard Settings")]
        public bool showOnStart = false;
        public float updateInterval = 1.0f;
        public int maxHistoryPoints = 60;
        
        [Header("UI References")]
        public GameObject dashboardPanel;
        public TextMeshProUGUI fpsText;
        public TextMeshProUGUI memoryText;
        public TextMeshProUGUI metricsCountText;
        public TextMeshProUGUI alertsText;
        public TextMeshProUGUI timestampText;
        
        [Header("Metric Displays")]
        public Transform metricsContainer;
        public GameObject metricDisplayPrefab;
        
        [Header("Alert Display")]
        public Transform alertsContainer;
        public GameObject alertDisplayPrefab;
        
        [Header("Charts")]
        public RectTransform fpsChart;
        public RectTransform memoryChart;
        public Image fpsChartLine;
        public Image memoryChartLine;
        
        [Header("Controls")]
        public Button toggleButton;
        public Button resetButton;
        public Button clearAlertsButton;
        
        private ActionLogSettings _logSettings;
        private float _lastUpdateTime;
        private readonly Dictionary<string, MetricDisplay> _metricDisplays = new Dictionary<string, MetricDisplay>();
        private readonly List<float> _fpsHistory = new List<float>();
        private readonly List<float> _memoryHistory = new List<float>();
        private bool _isVisible = false;

        [Serializable]
        public class MetricDisplay
        {
            public GameObject gameObject;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI valueText;
            public TextMeshProUGUI unitText;
            public TextMeshProUGUI trendText;
            public Image statusImage;
            public double lastValue;
            public Color normalColor = Color.green;
            public Color warningColor = Color.yellow;
            public Color criticalColor = Color.red;
        }

        void Start()
        {
            _logSettings = new ActionLogSettings();
            _logSettings.EnableCategory(ActionLogCategory.Performance, true);
            
            // Initialize performance monitoring
            PerformanceMetrics.Initialize(_logSettings);
            
            // Set up UI
            SetupUI();
            
            // Show dashboard if configured
            if (showOnStart)
            {
                ShowDashboard();
            }
            
            // Log dashboard initialization
            ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                "Performance dashboard initialized");
        }

        void Update()
        {
            if (!_isVisible) return;
            
            // Update dashboard at specified interval
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateDashboard();
                _lastUpdateTime = Time.time;
            }
        }

        void OnDestroy()
        {
            PerformanceMetrics.Shutdown();
        }

        /// <summary>
        /// Set up UI components and event handlers.
        /// </summary>
        private void SetupUI()
        {
            // Set up button events
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleDashboard);
            }
            
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetMetrics);
            }
            
            if (clearAlertsButton != null)
            {
                clearAlertsButton.onClick.AddListener(ClearAlerts);
            }
            
            // Initially hide dashboard
            if (dashboardPanel != null)
            {
                dashboardPanel.SetActive(false);
            }
            
            // Create metric displays for common metrics
            CreateMetricDisplay("fps", "FPS", "fps");
            CreateMetricDisplay("frame_time", "Frame Time", "ms");
            CreateMetricDisplay("memory_usage_mb", "Memory", "MB");
            CreateMetricDisplay("database-query_duration", "DB Query", "ms");
            CreateMetricDisplay("network-request_duration", "Network", "ms");
            CreateMetricDisplay("file-io_duration", "File I/O", "ms");
        }

        /// <summary>
        /// Toggle dashboard visibility.
        /// </summary>
        public void ToggleDashboard()
        {
            if (_isVisible)
            {
                HideDashboard();
            }
            else
            {
                ShowDashboard();
            }
        }

        /// <summary>
        /// Show the performance dashboard.
        /// </summary>
        public void ShowDashboard()
        {
            if (dashboardPanel != null)
            {
                dashboardPanel.SetActive(true);
                _isVisible = true;
                UpdateDashboard();
                
                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                    "Performance dashboard shown");
            }
        }

        /// <summary>
        /// Hide the performance dashboard.
        /// </summary>
        public void HideDashboard()
        {
            if (dashboardPanel != null)
            {
                dashboardPanel.SetActive(false);
                _isVisible = false;
                
                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                    "Performance dashboard hidden");
            }
        }

        /// <summary>
        /// Update all dashboard displays.
        /// </summary>
        private void UpdateDashboard()
        {
            try
            {
                var summary = PerformanceMetrics.GetSummary();
                var metrics = PerformanceMetrics.GetAllMetrics();
                var recentEvents = PerformanceMetrics.GetRecentEvents(10);
                
                // Update summary displays
                UpdateSummaryDisplay(summary);
                
                // Update metric displays
                UpdateMetricDisplays(metrics);
                
                // Update alerts
                UpdateAlertsDisplay(recentEvents);
                
                // Update charts
                UpdateCharts(metrics);
                
                // Update timestamp
                if (timestampText != null)
                {
                    timestampText.text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Error, 
                    $"Error updating dashboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Update summary display.
        /// </summary>
        private void UpdateSummaryDisplay(PerformanceSummary summary)
        {
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {summary.averageFrameRate:F1}";
                fpsText.color = GetStatusColor(summary.averageFrameRate, 30.0, 15.0, false);
            }
            
            if (memoryText != null)
            {
                memoryText.text = $"Memory: {summary.averageMemoryUsage:F1} MB";
                memoryText.color = GetStatusColor(summary.averageMemoryUsage, 500.0, 1000.0, true);
            }
            
            if (metricsCountText != null)
            {
                metricsCountText.text = $"Metrics: {summary.totalMetrics}";
            }
            
            if (alertsText != null)
            {
                alertsText.text = $"Alerts: {summary.activeAlerts}";
            }
        }

        /// <summary>
        /// Update individual metric displays.
        /// </summary>
        private void UpdateMetricDisplays(Dictionary<string, PerformanceMetrics.MetricData> metrics)
        {
            foreach (var kvp in _metricDisplays)
            {
                string metricName = kvp.Key;
                MetricDisplay display = kvp.Value;
                
                if (metrics.TryGetValue(metricName, out var metric))
                {
                    display.nameText.text = metric.name;
                    display.valueText.text = metric.value.ToString("F2");
                    display.unitText.text = metric.unit;
                    
                    // Calculate trend
                    double trend = metric.value - display.lastValue;
                    if (Math.Abs(trend) < 0.01)
                    {
                        display.trendText.text = "→";
                        display.trendText.color = Color.gray;
                    }
                    else if (trend > 0)
                    {
                        display.trendText.text = "↑";
                        display.trendText.color = Color.red;
                    }
                    else
                    {
                        display.trendText.text = "↓";
                        display.trendText.color = Color.green;
                    }
                    
                    // Set status color based on thresholds
                    bool isMemory = metricName.Contains("memory");
                    bool isDuration = metricName.Contains("duration");
                    bool isFps = metricName.Contains("fps");
                    
                    Color statusColor = display.normalColor;
                    if (isFps)
                    {
                        statusColor = GetStatusColor(metric.value, 30.0, 15.0, false);
                    }
                    else if (isMemory)
                    {
                        statusColor = GetStatusColor(metric.value, 500.0, 1000.0, true);
                    }
                    else if (isDuration)
                    {
                        statusColor = GetStatusColor(metric.value, 100.0, 500.0, true);
                    }
                    
                    display.statusImage.color = statusColor;
                    display.lastValue = metric.value;
                }
                else
                {
                    display.nameText.text = metricName;
                    display.valueText.text = "N/A";
                    display.unitText.text = "";
                    display.trendText.text = "";
                    display.statusImage.color = Color.gray;
                }
            }
        }

        /// <summary>
        /// Update alerts display.
        /// </summary>
        private void UpdateAlertsDisplay(List<PerformanceMetrics.PerformanceEvent> events)
        {
            // Clear existing alert displays
            foreach (Transform child in alertsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Show recent alerts
            var alertEvents = events.Where(e => e.eventType == "threshold-alert").Take(5);
            
            foreach (var alertEvent in alertEvents)
            {
                if (alertDisplayPrefab != null)
                {
                    GameObject alertObj = Instantiate(alertDisplayPrefab, alertsContainer);
                    var alertText = alertObj.GetComponent<TextMeshProUGUI>();
                    
                    if (alertText != null)
                    {
                        Color alertColor = alertEvent.severity == "critical" ? Color.red : Color.yellow;
                        alertText.color = alertColor;
                        alertText.text = $"{alertEvent.metricName}: {alertEvent.value:F2} ({alertEvent.severity})";
                    }
                }
            }
        }

        /// <summary>
        /// Update performance charts.
        /// </summary>
        private void UpdateCharts(Dictionary<string, PerformanceMetrics.MetricData> metrics)
        {
            // Update FPS history
            if (metrics.TryGetValue("fps", out var fpsMetric))
            {
                _fpsHistory.Add((float)fpsMetric.value);
                if (_fpsHistory.Count > maxHistoryPoints)
                {
                    _fpsHistory.RemoveAt(0);
                }
            }
            
            // Update memory history
            if (metrics.TryGetValue("memory_usage_mb", out var memoryMetric))
            {
                _memoryHistory.Add((float)memoryMetric.value);
                if (_memoryHistory.Count > maxHistoryPoints)
                {
                    _memoryHistory.RemoveAt(0);
                }
            }
            
            // Update chart visualizations (simplified line chart)
            UpdateChartVisualization(fpsChart, fpsChartLine, _fpsHistory, 60.0f);
            UpdateChartVisualization(memoryChart, memoryChartLine, _memoryHistory, 1000.0f);
        }

        /// <summary>
        /// Update chart visualization.
        /// </summary>
        private void UpdateChartVisualization(RectTransform chart, Image line, List<float> data, float maxValue)
        {
            if (chart == null || line == null || data.Count < 2) return;
            
            // Simple line chart using UI elements
            float width = chart.rect.width;
            float height = chart.rect.height;
            
            // Calculate line points (simplified - just showing trend)
            float firstValue = data[0];
            float lastValue = data[data.Count - 1];
            
            float firstY = (firstValue / maxValue) * height;
            float lastY = (lastValue / maxValue) * height;
            
            // Update line position and scale to represent the trend
            line.rectTransform.anchoredPosition = new Vector2(0, firstY);
            line.rectTransform.sizeDelta = new Vector2(width, Math.Abs(lastY - firstY));
            
            // Color based on trend
            if (lastValue > firstValue * 1.1f)
            {
                line.color = Color.red;
            }
            else if (lastValue < firstValue * 0.9f)
            {
                line.color = Color.green;
            }
            else
            {
                line.color = Color.yellow;
            }
        }

        /// <summary>
        /// Create metric display UI element.
        /// </summary>
        private void CreateMetricDisplay(string metricName, string displayName, string unit)
        {
            if (metricDisplayPrefab != null && metricsContainer != null)
            {
                GameObject metricObj = Instantiate(metricDisplayPrefab, metricsContainer);
                
                var display = new MetricDisplay
                {
                    gameObject = metricObj,
                    nameText = metricObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
                    valueText = metricObj.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>(),
                    unitText = metricObj.transform.Find("UnitText")?.GetComponent<TextMeshProUGUI>(),
                    trendText = metricObj.transform.Find("TrendText")?.GetComponent<TextMeshProUGUI>(),
                    statusImage = metricObj.transform.Find("StatusImage")?.GetComponent<Image>()
                };
                
                _metricDisplays[metricName] = display;
            }
        }

        /// <summary>
        /// Get status color based on thresholds.
        /// </summary>
        private Color GetStatusColor(double value, double warningThreshold, double criticalThreshold, bool higherIsWorse)
        {
            if (higherIsWorse)
            {
                if (value >= criticalThreshold) return Color.red;
                if (value >= warningThreshold) return Color.yellow;
                return Color.green;
            }
            else
            {
                if (value <= criticalThreshold) return Color.red;
                if (value <= warningThreshold) return Color.yellow;
                return Color.green;
            }
        }

        /// <summary>
        /// Reset all performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            PerformanceMetrics.ResetMetrics();
            
            // Clear history
            _fpsHistory.Clear();
            _memoryHistory.Clear();
            
            // Reset displays
            foreach (var display in _metricDisplays.Values)
            {
                display.lastValue = 0;
            }
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                "Performance metrics reset from dashboard");
        }

        /// <summary>
        /// Clear alerts display.
        /// </summary>
        public void ClearAlerts()
        {
            // Clear alert displays
            foreach (Transform child in alertsContainer)
            {
                Destroy(child.gameObject);
            }
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                "Performance alerts cleared from dashboard");
        }

        /// <summary>
        /// Add custom metric to dashboard.
        /// </summary>
        public void AddCustomMetric(string metricName, string displayName, string unit)
        {
            if (!_metricDisplays.ContainsKey(metricName))
            {
                CreateMetricDisplay(metricName, displayName, unit);
            }
        }

        /// <summary>
        /// Set dashboard update interval.
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.1f, interval);
        }

        /// <summary>
        /// Get current dashboard visibility state.
        /// </summary>
        public bool IsVisible()
        {
            return _isVisible;
        }
    }
}
