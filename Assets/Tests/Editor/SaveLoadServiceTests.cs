using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using SuperHexLink.Services;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for SaveLoadService.
    /// Verifies centralized save/load functionality, error handling, and async operations.
    /// </summary>
    public class SaveLoadServiceTests
    {
        private SaveLoadService _saveLoadService;
        private string _testDirectory;
        private string _testFilePath;

        [SetUp]
        public void SetUp()
        {
            _saveLoadService = new SaveLoadService(new ActionLogSettings());
            _testDirectory = Path.Combine(Application.temporaryCachePath, "SaveLoadTests");
            _testFilePath = Path.Combine(_testDirectory, "test_save.json");

            // Ensure test directory exists
            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Test]
        public void SaveLoadService_SaveGameState_ValidState_ReturnsSuccess()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();

            // Act
            SaveResult result = _saveLoadService.SaveGameState(gameState, _testFilePath);

            // Assert
            Assert.IsTrue(result.Success, "Save should succeed");
            Assert.AreEqual("Save successful", result.Message, "Should have success message");
            Assert.AreEqual(_testFilePath, result.FilePath, "Should return correct file path");
            Assert.Greater(result.FileSize, 0, "File size should be greater than 0");
            Assert.IsTrue(File.Exists(_testFilePath), "File should exist after save");
        }

        [Test]
        public void SaveLoadService_SaveGameState_NullState_ReturnsFailure()
        {
            // Act
            SaveResult result = _saveLoadService.SaveGameState(null, _testFilePath);

            // Assert
            Assert.IsFalse(result.Success, "Save should fail for null state");
            Assert.AreEqual("Game state is null", result.Message, "Should have null state error");
            Assert.AreEqual(_testFilePath, result.FilePath, "Should return provided file path");
            Assert.AreEqual(0, result.FileSize, "File size should be 0 for failed save");
        }

        [Test]
        public void SaveLoadService_SaveGameState_InvalidDirectory_ReturnsFailure()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            string invalidPath = "C:\invalid\path\that\doesnt\exist\file.json";

            // Act
            SaveResult result = _saveLoadService.SaveGameState(gameState, invalidPath);

            // Assert
            Assert.IsFalse(result.Success, "Save should fail for invalid directory");
            Assert.IsTrue(result.Message.Contains("Invalid directory path"), "Should have directory error");
        }

        [Test]
        public void SaveLoadService_LoadGameState_ExistingFile_ReturnsSuccess()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            // First save a game state
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, _testFilePath);
            Assert.IsTrue(saveResult.Success, "Setup: Save should succeed");

            // Act
            LoadResult loadResult = _saveLoadService.LoadGameState(_testFilePath);

            // Assert
            Assert.IsTrue(loadResult.Success, "Load should succeed");
            Assert.AreEqual("Load successful", loadResult.Message, "Should have success message");
            Assert.IsNotNull(loadResult.GameState, "Game state should not be null");
            Assert.AreEqual(_testFilePath, loadResult.FilePath, "Should return correct file path");
            Assert.AreEqual(saveResult.FileSize, loadResult.FileSize, "File sizes should match");
        }

        [Test]
        public void SaveLoadService_LoadGameState_NonExistentFile_ReturnsFailure()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            LoadResult result = _saveLoadService.LoadGameState(nonExistentPath);

            // Assert
            Assert.IsFalse(result.Success, "Load should fail for non-existent file");
            Assert.IsTrue(result.Message.Contains("File not found"), "Should have file not found error");
            Assert.IsNull(result.GameState, "Game state should be null");
            Assert.AreEqual(nonExistentPath, result.FilePath, "Should return provided file path");
        }

        [Test]
        public void SaveLoadService_LoadGameState_UnsafePath_ReturnsFailure()
        {
            // Arrange
            string unsafePath = @"../../../etc/passwd"; // Directory traversal attempt

            // Act
            LoadResult result = _saveLoadService.LoadGameState(unsafePath);

            // Assert
            Assert.IsFalse(result.Success, "Load should fail for unsafe path");
            Assert.IsTrue(result.Message.Contains("directory traversal"), "Should have traversal error");
            Assert.IsNull(result.GameState, "Game state should be null");
        }

        [Test]
        public void SaveLoadService_SaveLoadRoundTrip_ValidState_PreservesData()
        {
            // Arrange
            var originalState = new GameSpawner.GameSpawnerState();
            originalState.hexGridConfig = new HexGridConfig
            {
                cols = 5,
                rows = 7,
                radius = 1.5f,
                height = 2.0f,
                minHeight = 0.5f,
                maxHeight = 3.0f,
                Apothem = 1.299f
            };

            // Act
            SaveResult saveResult = _saveLoadService.SaveGameState(originalState, _testFilePath);
            LoadResult loadResult = _saveLoadService.LoadGameState(_testFilePath);

            // Assert
            Assert.IsTrue(saveResult.Success, "Save should succeed");
            Assert.IsTrue(loadResult.Success, "Load should succeed");
            
            var loadedState = loadResult.GameState;
            Assert.IsNotNull(loadedState, "Loaded state should not be null");
            
            // Verify grid config preservation
            Assert.AreEqual(originalState.hexGridConfig.cols, loadedState.hexGridConfig.cols, "Columns should match");
            Assert.AreEqual(originalState.hexGridConfig.rows, loadedState.hexGridConfig.rows, "Rows should match");
            Assert.AreEqual(originalState.hexGridConfig.radius, loadedState.hexGridConfig.radius, "Radius should match");
            Assert.AreEqual(originalState.hexGridConfig.height, loadedState.hexGridConfig.height, "Height should match");
        }

        [Test]
        public void SaveLoadService_SaveGameStateAsync_ValidState_ReturnsSuccess()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();

            // Act
            Task<SaveResult> saveTask = _saveLoadService.SaveGameStateAsync(gameState, _testFilePath);
            SaveResult result = saveTask.GetAwaiter().GetResult();

            // Assert
            Assert.IsTrue(result.Success, "Async save should succeed");
            Assert.AreEqual("Save successful", result.Message, "Should have success message");
            Assert.IsTrue(File.Exists(_testFilePath), "File should exist after async save");
        }

        [Test]
        public void SaveLoadService_LoadGameStateAsync_ExistingFile_ReturnsSuccess()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            // First save a game state
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, _testFilePath);
            Assert.IsTrue(saveResult.Success, "Setup: Save should succeed");

            // Act
            Task<LoadResult> loadTask = _saveLoadService.LoadGameStateAsync(_testFilePath);
            LoadResult result = loadTask.GetAwaiter().GetResult();

            // Assert
            Assert.IsTrue(result.Success, "Async load should succeed");
            Assert.IsNotNull(result.GameState, "Game state should not be null");
        }

        [Test]
        public void SaveLoadService_SaveGameStateAsync_WithProgressCallback_ReportsProgress()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            var progressValues = new System.Collections.Generic.List<float>();

            // Act
            Task<SaveResult> saveTask = _saveLoadService.SaveGameStateAsync(
                gameState, _testFilePath, null, new Progress<float>(p => progressValues.Add(p)));
            SaveResult result = saveTask.GetAwaiter().GetResult();

            // Assert
            Assert.IsTrue(result.Success, "Async save should succeed");
            Assert.Greater(progressValues.Count, 0, "Should receive progress updates");
            Assert.AreEqual(1.0f, progressValues[progressValues.Count - 1], "Final progress should be 1.0");
        }

        [Test]
        public void SaveLoadService_LoadGameStateAsync_WithProgressCallback_ReportsProgress()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            // First save a game state
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, _testFilePath);
            Assert.IsTrue(saveResult.Success, "Setup: Save should succeed");
            
            var progressValues = new System.Collections.Generic.List<float>();

            // Act
            Task<LoadResult> loadTask = _saveLoadService.LoadGameStateAsync(
                _testFilePath, null, new Progress<float>(p => progressValues.Add(p)));
            LoadResult result = loadTask.GetAwaiter().GetResult();

            // Assert
            Assert.IsTrue(result.Success, "Async load should succeed");
            Assert.Greater(progressValues.Count, 0, "Should receive progress updates");
            Assert.AreEqual(1.0f, progressValues[progressValues.Count - 1], "Final progress should be 1.0");
        }

        [Test]
        public void SaveLoadService_ValidatePath_SafePath_ReturnsTrue()
        {
            // Arrange
            string safePath = Path.Combine(_testDirectory, "safe_file.json");

            // Act
            bool isValid = _saveLoadService.ValidatePath(safePath);

            // Assert
            Assert.IsTrue(isValid, "Safe path should be valid");
        }

        [Test]
        public void SaveLoadService_ValidatePath_UnsafePath_ReturnsFalse()
        {
            // Arrange
            string unsafePath = "../../../etc/passwd";

            // Act
            bool isValid = _saveLoadService.ValidatePath(unsafePath);

            // Assert
            Assert.IsFalse(isValid, "Unsafe path should be invalid");
        }

        [Test]
        public void SaveLoadService_GetFileInfo_ExistingFile_ReturnsFileInfo()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            // Save a file first
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, _testFilePath);
            Assert.IsTrue(saveResult.Success, "Setup: Save should succeed");

            // Act
            FileInfo fileInfo = _saveLoadService.GetFileInfo(_testFilePath);

            // Assert
            Assert.IsNotNull(fileInfo, "Should return file info");
            Assert.AreEqual(_testFilePath, fileInfo.FullName, "Should return correct file path");
            Assert.AreEqual(saveResult.FileSize, fileInfo.Length, "File sizes should match");
        }

        [Test]
        public void SaveLoadService_GetFileInfo_NonExistentFile_ReturnsNull()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            FileInfo fileInfo = _saveLoadService.GetFileInfo(nonExistentPath);

            // Assert
            Assert.IsNull(fileInfo, "Should return null for non-existent file");
        }

        [Test]
        public void SaveLoadService_SaveGameState_UsesDefaultPath_WhenPathIsNull()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            string defaultPath = Path.Combine(_testDirectory, "default_save.json");

            // Act
            SaveResult result = _saveLoadService.SaveGameState(gameState, null, defaultPath);

            // Assert
            Assert.IsTrue(result.Success, "Save should succeed with default path");
            Assert.AreEqual(defaultPath, result.FilePath, "Should use default path");
            Assert.IsTrue(File.Exists(defaultPath), "File should exist at default path");
        }

        [Test]
        public void SaveLoadService_LoadGameState_UsesDefaultPath_WhenPathIsNull()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            string defaultPath = Path.Combine(_testDirectory, "default_load.json");
            
            // Save to default path first
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, null, defaultPath);
            Assert.IsTrue(saveResult.Success, "Setup: Save should succeed");

            // Act
            LoadResult result = _saveLoadService.LoadGameState(null, defaultPath);

            // Assert
            Assert.IsTrue(result.Success, "Load should succeed with default path");
            Assert.AreEqual(defaultPath, result.FilePath, "Should use default path");
        }

        [Test]
        public void SaveResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var result = new SaveResult(true, "Test message", "/path/to/file.json", 1024);

            // Act
            string resultString = result.ToString();

            // Assert
            Assert.IsTrue(resultString.Contains("Success=True"), "Should contain success status");
            Assert.IsTrue(resultString.Contains("Test message"), "Should contain message");
            Assert.IsTrue(resultString.Contains("/path/to/file.json"), "Should contain file path");
            Assert.IsTrue(resultString.Contains("1,024 bytes"), "Should contain formatted file size");
        }

        [Test]
        public void LoadResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            var result = new LoadResult(true, "Test message", gameState, "/path/to/file.json", 2048, false);

            // Act
            string resultString = result.ToString();

            // Assert
            Assert.IsTrue(resultString.Contains("Success=True"), "Should contain success status");
            Assert.IsTrue(resultString.Contains("Test message"), "Should contain message");
            Assert.IsTrue(resultString.Contains("/path/to/file.json"), "Should contain file path");
            Assert.IsTrue(resultString.Contains("2,048 bytes"), "Should contain formatted file size");
            Assert.IsTrue(resultString.Contains("Legacy=False"), "Should contain legacy format flag");
        }

        [Test]
        public void SaveLoadService_Performance_LargeSaveLoad_OperatesEfficiently()
        {
            // Arrange
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = HexGridConfig.CreateDefault();
            
            // Create a larger state for performance testing
            gameState.hexGridConfig.cols = 50;
            gameState.hexGridConfig.rows = 50;

            // Act
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            SaveResult saveResult = _saveLoadService.SaveGameState(gameState, _testFilePath);
            LoadResult loadResult = _saveLoadService.LoadGameState(_testFilePath);
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(saveResult.Success, "Save should succeed");
            Assert.IsTrue(loadResult.Success, "Load should succeed");
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Save and load should complete within 1 second");
        }
    }
}
