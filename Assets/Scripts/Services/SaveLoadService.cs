using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Sirenix.Serialization;
using SuperHexLink.Logging;

namespace SuperHexLink.Services
{
    /// <summary>
    /// Centralized service for save/load operations.
    /// Extracts persistence logic from GameSpawner for better separation of concerns.
    /// Handles file I/O, serialization, validation, and error reporting.
    /// </summary>
    public class SaveLoadService
    {
        private readonly ActionLogSettings _logSettings;
        private const long MaxMapFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public SaveLoadService(ActionLogSettings logSettings = null)
        {
            _logSettings = logSettings ?? new ActionLogSettings();
        }

        /// <summary>
        /// Saves the current game state to the specified file path.
        /// </summary>
        /// <param name="gameState">Current game state to save</param>
        /// <param name="filePath">Target file path (optional, uses default if null)</param>
        /// <param name="defaultPath">Default path to use when filePath is null</param>
        /// <returns>Save result with success status and metadata</returns>
        public SaveResult SaveGameState(GameSpawner.GameSpawnerState gameState, string filePath = null, string defaultPath = "./data/maps/map.json")
        {
            if (gameState == null)
            {
                return new SaveResult(false, "Game state is null", null, 0);
            }

            string targetPath = ResolveMapPath(filePath, defaultPath);
            string directory = Path.GetDirectoryName(targetPath);

            if (string.IsNullOrEmpty(directory))
            {
                return new SaveResult(false, "Invalid directory path", targetPath, 0);
            }

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(directory);

                // Serialize the game state
                byte[] serializedData = SirenixSerializationUtility.SerializeUnityObject(gameState, DataFormat.JSON);
                string jsonContent = System.Text.Encoding.UTF8.GetString(serializedData);

                // Write to file
                File.WriteAllText(targetPath, jsonContent);

                long fileSize = new FileInfo(targetPath).Length;

                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                    $"SaveLoadService: Successfully saved game state to {targetPath} ({fileSize:N0} bytes)");

                return new SaveResult(true, "Save successful", targetPath, fileSize);
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"SaveLoadService: Failed to save game state to {targetPath}: {ex.Message}");

                return new SaveResult(false, $"Save failed: {ex.Message}", targetPath, 0);
            }
        }

        /// <summary>
        /// Loads game state from the specified file path.
        /// </summary>
        /// <param name="filePath">File path to load from (optional, uses default if null)</param>
        /// <param name="defaultPath">Default path to use when filePath is null</param>
        /// <returns>Load result with game state and metadata</returns>
        public LoadResult LoadGameState(string filePath = null, string defaultPath = "./data/maps/map.json")
        {
            string targetPath = ResolveMapPath(filePath, defaultPath);

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                $"SaveLoadService: Loading game state from {targetPath}");

            // Validate path safety
            if (!IsPathSafe(targetPath))
            {
                string error = $"Path rejected (directory traversal detected): {targetPath}";
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"SaveLoadService: {error}");

                return new LoadResult(false, error, null, targetPath);
            }

            // Check file existence
            if (!File.Exists(targetPath))
            {
                string error = $"File not found: {targetPath}";
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"SaveLoadService: {error}");

                return new LoadResult(false, error, null, targetPath);
            }

            // Check file size
            FileInfo fileInfo = new FileInfo(targetPath);
            long fileSize = fileInfo.Length;

            if (fileSize > MaxMapFileSizeBytes)
            {
                string error = $"File too large ({fileSize:N0} bytes, max {MaxMapFileSizeBytes:N0}): {targetPath}";
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"SaveLoadService: {error}");

                return new LoadResult(false, error, null, targetPath);
            }

            try
            {
                // Read file content
                string jsonContent = File.ReadAllText(targetPath);

                // Check for legacy format
                bool isLegacy = IsLegacyFormat(jsonContent);

                if (isLegacy)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                        $"SaveLoadService: Detected legacy format in {targetPath}, attempting conversion");

                    var conversionResult = LegacyMapConverter.TryConvertLegacyJson(jsonContent);
                    if (!conversionResult.Success)
                    {
                        string error = $"Legacy conversion failed for {targetPath}: {conversionResult.ErrorMessage}";
                        ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                            $"SaveLoadService: {error}");

                        return new LoadResult(false, error, null, targetPath);
                    }

                    jsonContent = conversionResult.ConvertedJson;
                }

                // Deserialize game state
                byte[] serializedData = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                GameSpawner.GameSpawnerState gameState = SirenixSerializationUtility.DeserializeUnityObject<GameSpawner.GameSpawnerState>(serializedData, DataFormat.JSON);

                if (gameState == null)
                {
                    string error = $"Deserialized null from {targetPath}";
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: {error}");

                    return new LoadResult(false, error, null, targetPath);
                }

                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                    $"SaveLoadService: Successfully loaded game state from {targetPath} ({fileSize:N0} bytes)");

                return new LoadResult(true, "Load successful", gameState, targetPath, fileSize, isLegacy);
            }
            catch (Exception ex)
            {
                string error = $"Load failed for {targetPath}: {ex.Message}";
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"SaveLoadService: {error}");

                return new LoadResult(false, error, null, targetPath);
            }
        }

        /// <summary>
        /// Asynchronously saves game state with progress reporting.
        /// </summary>
        /// <param name="gameState">Game state to save</param>
        /// <param name="filePath">Target file path</param>
        /// <param name="defaultPath">Default path</param>
        /// <param name="progressCallback">Progress callback (0.0 to 1.0)</param>
        /// <returns>Async save result</returns>
        public async Task<SaveResult> SaveGameStateAsync(GameSpawner.GameSpawnerState gameState, 
            string filePath = null, string defaultPath = "./data/maps/map.json", 
            IProgress<float> progressCallback = null)
        {
            return await Task.Run(() =>
            {
                progressCallback?.Report(0.1f);

                string targetPath = ResolveMapPath(filePath, defaultPath);
                string directory = Path.GetDirectoryName(targetPath);

                if (string.IsNullOrEmpty(directory))
                {
                    return new SaveResult(false, "Invalid directory path", targetPath, 0);
                }

                progressCallback?.Report(0.2f);

                try
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(directory);

                    progressCallback?.Report(0.4f);

                    // Serialize the game state
                    byte[] serializedData = SirenixSerializationUtility.SerializeUnityObject(gameState, DataFormat.JSON);
                    string jsonContent = System.Text.Encoding.UTF8.GetString(serializedData);

                    progressCallback?.Report(0.7f);

                    // Write to file
                    File.WriteAllText(targetPath, jsonContent);

                    progressCallback?.Report(0.9f);

                    long fileSize = new FileInfo(targetPath).Length;

                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                        $"SaveLoadService: Successfully saved game state to {targetPath} ({fileSize:N0} bytes)");

                    progressCallback?.Report(1.0f);

                    return new SaveResult(true, "Save successful", targetPath, fileSize);
                }
                catch (Exception ex)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: Failed to save game state to {targetPath}: {ex.Message}");

                    return new SaveResult(false, $"Save failed: {ex.Message}", targetPath, 0);
                }
            });
        }

        /// <summary>
        /// Asynchronously loads game state with progress reporting.
        /// </summary>
        /// <param name="filePath">File path to load from</param>
        /// <param name="defaultPath">Default path</param>
        /// <param name="progressCallback">Progress callback (0.0 to 1.0)</param>
        /// <returns>Async load result</returns>
        public async Task<LoadResult> LoadGameStateAsync(string filePath = null, 
            string defaultPath = "./data/maps/map.json", 
            IProgress<float> progressCallback = null)
        {
            return await Task.Run(() =>
            {
                progressCallback?.Report(0.1f);

                string targetPath = ResolveMapPath(filePath, defaultPath);

                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                    $"SaveLoadService: Loading game state from {targetPath}");

                // Validate path safety
                if (!IsPathSafe(targetPath))
                {
                    string error = $"Path rejected (directory traversal detected): {targetPath}";
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: {error}");

                    return new LoadResult(false, error, null, targetPath);
                }

                progressCallback?.Report(0.2f);

                // Check file existence
                if (!File.Exists(targetPath))
                {
                    string error = $"File not found: {targetPath}";
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: {error}");

                    return new LoadResult(false, error, null, targetPath);
                }

                progressCallback?.Report(0.3f);

                // Check file size
                FileInfo fileInfo = new FileInfo(targetPath);
                long fileSize = fileInfo.Length;

                if (fileSize > MaxMapFileSizeBytes)
                {
                    string error = $"File too large ({fileSize:N0} bytes, max {MaxMapFileSizeBytes:N0}): {targetPath}";
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: {error}");

                    return new LoadResult(false, error, null, targetPath);
                }

                progressCallback?.Report(0.4f);

                try
                {
                    // Read file content
                    string jsonContent = File.ReadAllText(targetPath);

                    progressCallback?.Report(0.6f);

                    // Check for legacy format
                    bool isLegacy = IsLegacyFormat(jsonContent);

                    if (isLegacy)
                    {
                        ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                            $"SaveLoadService: Detected legacy format in {targetPath}, attempting conversion");

                        var conversionResult = LegacyMapConverter.TryConvertLegacyJson(jsonContent);
                        if (!conversionResult.Success)
                        {
                            string error = $"Legacy conversion failed for {targetPath}: {conversionResult.ErrorMessage}";
                            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                                $"SaveLoadService: {error}");

                            return new LoadResult(false, error, null, targetPath);
                        }

                        jsonContent = conversionResult.ConvertedJson;
                    }

                    progressCallback?.Report(0.8f);

                    // Deserialize game state
                    byte[] serializedData = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    GameSpawner.GameSpawnerState gameState = SirenixSerializationUtility.DeserializeUnityObject<GameSpawner.GameSpawnerState>(serializedData, DataFormat.JSON);

                    if (gameState == null)
                    {
                        string error = $"Deserialized null from {targetPath}";
                        ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                            $"SaveLoadService: {error}");

                        return new LoadResult(false, error, null, targetPath);
                    }

                    progressCallback?.Report(1.0f);

                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                        $"SaveLoadService: Successfully loaded game state from {targetPath} ({fileSize:N0} bytes)");

                    return new LoadResult(true, "Load successful", gameState, targetPath, fileSize, isLegacy);
                }
                catch (Exception ex)
                {
                    string error = $"Load failed for {targetPath}: {ex.Message}";
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"SaveLoadService: {error}");

                    return new LoadResult(false, error, null, targetPath);
                }
            });
        }

        /// <summary>
        /// Validates if a file path is safe for loading.
        /// </summary>
        /// <param name="filePath">File path to validate</param>
        /// <returns>True if path is safe</returns>
        public bool ValidatePath(string filePath)
        {
            return IsPathSafe(filePath);
        }

        /// <summary>
        /// Gets file information for a save file.
        /// </summary>
        /// <param name="filePath">File path to check</param>
        /// <param name="defaultPath">Default path</param>
        /// <returns>File information or null if file doesn't exist</returns>
        public FileInfo GetFileInfo(string filePath = null, string defaultPath = "./data/maps/map.json")
        {
            string targetPath = ResolveMapPath(filePath, defaultPath);

            if (!File.Exists(targetPath))
            {
                return null;
            }

            return new FileInfo(targetPath);
        }

        /// <summary>
        /// Checks if the JSON content is in legacy format.
        /// </summary>
        private bool IsLegacyFormat(string jsonContent)
        {
            // Simple heuristic to detect legacy format
            // This would need to be implemented based on actual legacy format detection logic
            return jsonContent.Contains("\"hexGrid\"") || jsonContent.Contains("\"HexGrid\"");
        }

        /// <summary>
        /// Resolves map path with fallback to default.
        /// </summary>
        private static string ResolveMapPath(string providedPath, string fallbackPath)
        {
            if (!string.IsNullOrWhiteSpace(providedPath))
            {
                return providedPath;
            }

            return fallbackPath;
        }

        /// <summary>
        /// Validates that a path doesn't contain directory traversal attempts.
        /// </summary>
        private static bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                string currentDirectory = Directory.GetCurrentDirectory();

                return fullPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result of a save operation.
    /// </summary>
    public class SaveResult
    {
        public bool Success { get; }
        public string Message { get; }
        public string FilePath { get; }
        public long FileSize { get; }

        public SaveResult(bool success, string message, string filePath, long fileSize)
        {
            Success = success;
            Message = message;
            FilePath = filePath;
            FileSize = fileSize;
        }

        public override string ToString()
        {
            return $"SaveResult: Success={Success}, Message='{Message}', Path='{FilePath}', Size={FileSize:N0} bytes";
        }
    }

    /// <summary>
    /// Result of a load operation.
    /// </summary>
    public class LoadResult
    {
        public bool Success { get; }
        public string Message { get; }
        public GameSpawner.GameSpawnerState GameState { get; }
        public string FilePath { get; }
        public long FileSize { get; }
        public bool WasLegacyFormat { get; }

        public LoadResult(bool success, string message, GameSpawner.GameSpawnerState gameState, string filePath, long fileSize = 0, bool wasLegacyFormat = false)
        {
            Success = success;
            Message = message;
            GameState = gameState;
            FilePath = filePath;
            FileSize = fileSize;
            WasLegacyFormat = wasLegacyFormat;
        }

        public override string ToString()
        {
            return $"LoadResult: Success={Success}, Message='{Message}', Path='{FilePath}', Size={FileSize:N0} bytes, Legacy={WasLegacyFormat}";
        }
    }
}
