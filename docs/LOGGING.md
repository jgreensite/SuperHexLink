# Logging Guide

This document covers the enhanced structured logging system in SuperHexLink, including correlation IDs, JSON formatting, and performance monitoring.

**Last updated**: February 2026

---

## Overview

SuperHexLink uses a sophisticated structured logging system that provides:

- **Correlation IDs**: Track operations across the entire system
- **JSON Logging**: Machine-readable structured logs for analysis
- **Performance Monitoring**: Built-in performance metrics logging
- **Security Events**: Dedicated security event logging
- **Global Context**: Automatic inclusion of system context
- **Log Levels**: Configurable minimum log levels
- **Multiple Formats**: Plain text, JSON, or both simultaneously

---

## Quick Start

### Basic Usage

```csharp
// Initialize the logging system
ActionLogger.Initialize("my-session-id", LogFormat.Both, ActionLogSeverity.Info);

// Add global context
ActionLogger.AddGlobalContext("userId", "user123");
ActionLogger.AddGlobalContext("sessionId", "session-456");

// Log messages
ActionLogger.Log(settings, ActionLogCategory.General, ActionLogSeverity.Info, "User logged in");

// Log with structured context
var context = new Dictionary<string, object>
{
    ["operation"] = "login",
    ["ipAddress"] = "192.168.1.100",
    ["success"] = true
};
ActionLogger.LogStructured(settings, ActionLogCategory.Security, ActionLogSeverity.Info, 
    "Login attempt completed", context);
```

### Performance Logging

```csharp
// Log performance metrics
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... perform operation
stopwatch.Stop();

var perfContext = new Dictionary<string, object>
{
    ["database"] = "users",
    ["queryType"] = "select",
    ["resultCount"] = 42
};

ActionLogger.LogPerformance(settings, "database-query", stopwatch.Elapsed, perfContext);
```

### Security Event Logging

```csharp
// Log security events
var secContext = new Dictionary<string, object>
{
    ["ipAddress"] = "192.168.1.100",
    ["userAgent"] = "Mozilla/5.0...",
    ["attemptCount"] = 3
};

ActionLogger.LogSecurity(settings, "login-failure", "Invalid credentials", secContext);
```

---

## Configuration

### Initialization Options

```csharp
// Initialize with custom correlation ID
ActionLogger.Initialize("custom-correlation-id");

// Initialize with JSON logging only
ActionLogger.Initialize(null, LogFormat.Json, ActionLogSeverity.Debug);

// Initialize with minimum log level
ActionLogger.Initialize(null, LogFormat.Both, ActionLogSeverity.Warning);
```

### Log Formats

| Format | Description | Use Case |
|--------|-------------|----------|
| **Plain** | Human-readable text format | Development, local debugging |
| **Json** | Structured JSON format | Production, log analysis tools |
| **Both** | Output both formats simultaneously | Development with structured analysis |

### Log Levels

| Level | Priority | Description |
|-------|----------|-------------|
| **Debug** | 0 | Detailed debugging information |
| **Info** | 1 | General information messages |
| **Warning** | 2 | Warning conditions |
| **Error** | 3 | Error conditions |
| **Critical** | 4 | Critical error conditions |

### Log Categories

| Category | Description |
|----------|-------------|
| **General** | General application messages |
| **HexLifecycle** | Hex tile lifecycle events |
| **HexLand** | Hex land management events |
| **GameLifecycle** | Game state lifecycle events |
| **Network** | Network communication events |
| **Performance** | Performance and timing events |
| **Security** | Security-related events |
| **Configuration** | Configuration and settings events |

---

## Correlation IDs

### Automatic Generation

```csharp
// Auto-generates 8-character correlation ID
ActionLogger.Initialize();
string correlationId = ActionLogger.GetCorrelationId(); // e.g., "a1b2c3d4"
```

### Manual Correlation IDs

```csharp
// Set custom correlation ID
ActionLogger.SetCorrelationId("user-session-123");

// Get current correlation ID
string currentId = ActionLogger.GetCorrelationId();
```

### Correlation ID Persistence

```csharp
// Correlation IDs persist across the session
ActionLogger.SetCorrelationId("persistent-id");

// Even after re-initialization, the ID is preserved
ActionLogger.Initialize(); // Keeps existing correlation ID
```

---

## Structured Logging

### Context Data

```csharp
var context = new Dictionary<string, object>
{
    ["userId"] = "user123",
    ["operation"] = "save-game",
    ["gameId"] = "game-456",
    ["timestamp"] = DateTime.UtcNow,
    ["metadata"] = new { version = "1.0", mode = "multiplayer" }
};

ActionLogger.LogStructured(settings, ActionLogCategory.GameLifecycle, 
    ActionLogSeverity.Info, "Game saved successfully", context);
```

### Global Context

```csharp
// Add context that appears in ALL log entries
ActionLogger.AddGlobalContext("serverVersion", "2.1.0");
ActionLogger.AddGlobalContext("environment", "production");
ActionLogger.AddGlobalContext("cluster", "us-east-1");

// View current global context
var config = ActionLogger.GetConfiguration();
var globalContext = (Dictionary<string, object>)config["globalContext"];
```

### Automatic Global Context

The system automatically includes:

- **unityVersion**: Unity engine version
- **platform**: Running platform (Windows, macOS, Linux, etc.)
- **version**: Application version
- **startTime**: Application start time

---

## Performance Monitoring

### Built-in Performance Logging

```csharp
// Simple performance logging
var duration = TimeSpan.FromMilliseconds(150.5);
ActionLogger.LogPerformance(settings, "database-query", duration);

// Performance logging with context
var perfContext = new Dictionary<string, object>
{
    ["database"] = "users",
    ["queryType"] = "select",
    ["resultCount"] = 42,
    ["cacheHit"] = false
};

ActionLogger.LogPerformance(settings, "database-query", duration, perfContext);
```

### Performance Context Examples

```csharp
// Database operations
var dbContext = new Dictionary<string, object>
{
    ["database"] = "game_state",
    ["operation"] = "save",
    ["table"] = "games",
    ["rowsAffected"] = 1,
    ["transactionId"] = "txn-123"
};

// Network operations
var netContext = new Dictionary<string, object>
{
    ["endpoint"] = "/api/games",
    ["method"] = "POST",
    ["statusCode"] = 200,
    ["responseSize"] = 1024,
    ["latency"] = 45.2
};

// File operations
var fileContext = new Dictionary<string, object>
{
    ["filePath"] = "/data/games/save.json",
    ["operation"] = "write",
    ["fileSize"] = 2048,
    ["success"] = true
};
```

---

## Security Logging

### Security Event Types

```csharp
// Authentication events
ActionLogger.LogSecurity(settings, "login-success", "User authenticated successfully", authContext);
ActionLogger.LogSecurity(settings, "login-failure", "Invalid credentials", authContext);
ActionLogger.LogSecurity(settings, "logout", "User logged out", authContext);

// Authorization events
ActionLogger.LogSecurity(settings, "access-denied", "Insufficient permissions", authContext);
ActionLogger.LogSecurity(settings, "privilege-escalation", "Admin access granted", authContext);

// Data protection events
ActionLogger.LogSecurity(settings, "data-access", "Sensitive data accessed", dataContext);
ActionLogger.LogSecurity(settings, "data-modification", "Game state modified", dataContext);

// System events
ActionLogger.LogSecurity(settings, "configuration-change", "Security settings updated", configContext);
ActionLogger.LogSecurity(settings, "suspicious-activity", "Unusual login pattern", activityContext);
```

### Security Context Examples

```csharp
// Authentication context
var authContext = new Dictionary<string, object>
{
    ["userId"] = "user123",
    ["ipAddress"] = "192.168.1.100",
    ["userAgent"] = "Mozilla/5.0...",
    ["loginMethod"] = "password",
    ["mfaVerified"] = true
};

// Data access context
var dataContext = new Dictionary<string, object>
{
    ["resource"] = "game-state",
    ["resourceId"] = "game-456",
    ["operation"] = "read",
    ["userId"] = "user123",
    ["sensitivity"] = "high"
};

// Suspicious activity context
var activityContext = new Dictionary<string, object>
{
    ["pattern"] = "multiple-login-failures",
    ["ipAddress"] = "192.168.1.100",
    ["attemptCount"] = 5,
    ["timeWindow"] = "5-minutes",
    ["blocked"] = true
};
```

---

## JSON Log Format

### Plain Text Format

```
[ActionLog/GameLifecycle] [a1b2c3d4] [2026-02-09T23:45:12Z] Game saved successfully
```

### JSON Format

```json
{
  "keys": [
    "timestamp",
    "level",
    "category",
    "message",
    "correlationId",
    "global.unityVersion",
    "global.platform",
    "global.version",
    "global.startTime",
    "operation",
    "gameId",
    "success"
  ],
  "values": [
    "2026-02-09T23:45:12Z",
    "Info",
    "GameLifecycle",
    "Game saved successfully",
    "a1b2c3d4",
    "2026.2.0f1",
    "WindowsPlayer",
    "1.0.0",
    "2026-02-09T22:30:00Z",
    "save-game",
    "game-456",
    true
  ]
}
```

### Log Analysis

The JSON format enables:

- **Structured Querying**: Filter by specific fields
- **Aggregation**: Group by correlation ID, category, or level
- **Time Series Analysis**: Track performance over time
- **Security Monitoring**: Analyze security events patterns
- **Error Tracking**: Correlate errors across the system

---

## Configuration Management

### Runtime Configuration

```csharp
// Get current configuration
var config = ActionLogger.GetConfiguration();

// Change log format at runtime
ActionLogger.SetLogFormat(LogFormat.Json);

// Change minimum level at runtime
ActionLogger.SetMinimumLevel(ActionLogSeverity.Warning);

// Add global context at runtime
ActionLogger.AddGlobalContext("deployment", "v2.1.0");
```

### Configuration Example

```csharp
// Production configuration
ActionLogger.Initialize(
    correlationId: Environment.GetEnvironmentVariable("CORRELATION_ID"),
    format: LogFormat.Json,
    minimumLevel: ActionLogSeverity.Info
);

ActionLogger.AddGlobalContext("environment", "production");
ActionLogger.AddGlobalContext("cluster", "us-east-1");
ActionLogger.AddGlobalContext("service", "superhexlink-game");

// Development configuration
ActionLogger.Initialize(
    correlationId: "dev-session",
    format: LogFormat.Both,
    minimumLevel: ActionLogSeverity.Debug
);

ActionLogger.AddGlobalContext("environment", "development");
ActionLogger.AddGlobalContext("developer", Environment.UserName);
```

---

## Best Practices

### Correlation ID Management

1. **Session-Level IDs**: Use one correlation ID per user session
2. **Request-Level IDs**: Use different IDs for independent operations
3. **Propagation**: Pass correlation IDs to external services
4. **Persistence**: Keep correlation IDs consistent across the session

### Structured Context

1. **Consistent Keys**: Use consistent naming conventions
2. **Relevant Data**: Include only relevant context information
3. **Data Types**: Use appropriate data types (strings, numbers, booleans)
4. **Performance**: Avoid large objects in context

### Performance Logging

1. **Key Operations**: Log performance for critical operations
2. **Thresholds**: Log when operations exceed expected thresholds
3. **Context**: Include relevant context for performance analysis
4. **Aggregation**: Use consistent naming for aggregation

### Security Logging

1. **All Events**: Log all security-relevant events
2. **Context**: Include IP addresses, user IDs, and timestamps
3. **Sensitivity**: Avoid logging sensitive data (passwords, tokens)
4. **Retention**: Consider security log retention policies

---

## Troubleshooting

### Common Issues

**Correlation ID Not Appearing**:
```csharp
// Ensure logger is initialized
ActionLogger.Initialize();

// Check current correlation ID
string currentId = ActionLogger.GetCorrelationId();
Debug.Log($"Current correlation ID: {currentId}");
```

**JSON Serialization Errors**:
```csharp
// The system automatically falls back to plain format
// Check for unsupported object types in context
var safeContext = new Dictionary<string, object>
{
    ["string"] = "value",           // OK
    ["number"] = 42,                // OK
    ["boolean"] = true,             // OK
    ["datetime"] = DateTime.UtcNow, // OK
    // Avoid complex objects or circular references
};
```

**Performance Impact**:
```csharp
// Use appropriate minimum levels
ActionLogger.SetMinimumLevel(ActionLogSeverity.Info); // Production
ActionLogger.SetMinimumLevel(ActionLogSeverity.Debug); // Development

// Use JSON only when needed
ActionLogger.SetLogFormat(LogFormat.Json); // Production analysis
ActionLogger.SetLogFormat(LogFormat.Plain); // Development
```

### Debug Information

```csharp
// Get current logger configuration
var config = ActionLogger.GetConfiguration();
Debug.Log($"Logger Configuration:");
Debug.Log($"  Correlation ID: {config["correlationId"]}");
Debug.Log($"  Log Format: {config["logFormat"]}");
Debug.Log($"  Minimum Level: {config["minimumLevel"]}");

// View global context
var globalContext = (Dictionary<string, object>)config["globalContext"];
foreach (var kvp in globalContext)
{
    Debug.Log($"  Global.{kvp.Key}: {kvp.Value}");
}
```

---

## Integration Examples

### Game Integration

```csharp
public class GameManager : MonoBehaviour
{
    private ActionLogSettings _logSettings;
    
    void Start()
    {
        _logSettings = new ActionLogSettings();
        _logSettings.EnableAll();
        
        // Initialize logging for this game session
        string sessionId = Guid.NewGuid().ToString("N")[..8];
        ActionLogger.Initialize(sessionId, LogFormat.Both, ActionLogSeverity.Info);
        
        ActionLogger.AddGlobalContext("gameMode", "multiplayer");
        ActionLogger.AddGlobalContext("mapId", "map-forest-01");
        
        ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, 
            ActionLogSeverity.Info, "Game session started");
    }
    
    public void SaveGame()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // ... save game logic
            
            stopwatch.Stop();
            
            var context = new Dictionary<string, object>
            {
                ["saveSlot"] = 1,
                ["fileSize"] = 1024,
                ["success"] = true
            };
            
            ActionLogger.LogPerformance(_logSettings, "game-save", stopwatch.Elapsed, context);
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, 
                ActionLogSeverity.Info, "Game saved successfully", context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var context = new Dictionary<string, object>
            {
                ["saveSlot"] = 1,
                ["error"] = ex.Message,
                ["success"] = false
            };
            
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, 
                ActionLogSeverity.Error, "Game save failed", context);
        }
    }
}
```

### Network Integration

```csharp
public class NetworkManager : MonoBehaviour
{
    private ActionLogSettings _logSettings;
    
    void Start()
    {
        _logSettings = new ActionLogSettings();
        _logSettings.EnableCategory(ActionLogCategory.Network, true);
    }
    
    public async Task<bool> ConnectToServer(string host, int port)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // ... connection logic
            
            stopwatch.Stop();
            
            var context = new Dictionary<string, object>
            {
                ["host"] = host,
                ["port"] = port,
                ["success"] = true,
                ["latency"] = stopwatch.Elapsed.TotalMilliseconds
            };
            
            ActionLogger.LogPerformance(_logSettings, "server-connect", stopwatch.Elapsed, context);
            ActionLogger.Log(_logSettings, ActionLogCategory.Network, 
                ActionLogSeverity.Info, "Connected to server successfully", context);
            
            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var context = new Dictionary<string, object>
            {
                ["host"] = host,
                ["port"] = port,
                ["error"] = ex.Message,
                ["success"] = false
            };
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Network, 
                ActionLogSeverity.Error, "Failed to connect to server", context);
            
            return false;
        }
    }
}
```

---

## Related Documentation

- [Development Setup](DEV_SETUP.md) - Local development environment
- [Security Guidelines](SECURITY.md) - Security policies and procedures
- [Architecture](ARCHITECTURE.md) - System architecture and design
- [Testing Guide](TESTING.md) - Testing strategies and procedures
