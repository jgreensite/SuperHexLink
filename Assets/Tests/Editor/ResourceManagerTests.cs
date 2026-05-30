using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Gameplay;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Unit tests for ResourceManager functionality.
    /// Tests resource tracking, transactions, and economic balancing.
    /// </summary>
    [TestFixture]
    public class ResourceManagerTests
    {
        private ResourceManager _resourceManager;
        private EconomicBalancer _economicBalancer;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings
            {
                IsEnabled = (category, severity) => true,
                LogLevel = ActionLogSeverity.Info
            };
            _economicBalancer = new EconomicBalancer(_logSettings);
            _resourceManager = new ResourceManager(_logSettings, _economicBalancer);
        }

        [Test]
        public void InitializePlayer_CreatesPlayerWithZeroResources()
        {
            // Arrange
            const int playerId = 0;

            // Act
            _resourceManager.InitializePlayer(playerId);

            // Assert
            var resources = _resourceManager.GetPlayerResources(playerId);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Wood]);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Brick]);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Sheep]);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Wheat]);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Ore]);
            Assert.AreEqual(0, resources[ResourceManager.ResourceType.Gold]);
        }

        [Test]
        public void AddResources_ValidAmount_IncreasesPlayerResources()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);

            // Act
            var result = _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 5);

            // Assert
            Assert.IsTrue(result);
            var resources = _resourceManager.GetPlayerResources(playerId);
            Assert.AreEqual(5, resources[ResourceManager.ResourceType.Wood]);
        }

        [Test]
        public void AddResources_NonExistentPlayer_ReturnsFalse()
        {
            // Arrange
            const int playerId = 999;

            // Act
            var result = _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 5);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void AddResources_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);

            // Act
            var result = _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 0);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveResources_SufficientAmount_DecreasesPlayerResources()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 10);

            // Act
            var result = _resourceManager.RemoveResources(playerId, ResourceManager.ResourceType.Wood, 3);

            // Assert
            Assert.IsTrue(result);
            var resources = _resourceManager.GetPlayerResources(playerId);
            Assert.AreEqual(7, resources[ResourceManager.ResourceType.Wood]);
        }

        [Test]
        public void RemoveResources_InsufficientAmount_ReturnsFalse()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 2);

            // Act
            var result = _resourceManager.RemoveResources(playerId, ResourceManager.ResourceType.Wood, 5);

            // Assert
            Assert.IsFalse(result);
            var resources = _resourceManager.GetPlayerResources(playerId);
            Assert.AreEqual(2, resources[ResourceManager.ResourceType.Wood]); // Unchanged
        }

        [Test]
        public void HasSufficientResources_True_WhenPlayerHasEnough()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 10);

            // Act
            var result = _resourceManager.HasSufficientResources(playerId, ResourceManager.ResourceType.Wood, 5);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void HasSufficientResources_False_WhenPlayerDoesNotHaveEnough()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 3);

            // Act
            var result = _resourceManager.HasSufficientResources(playerId, ResourceManager.ResourceType.Wood, 5);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ExecuteTransaction_ValidTransaction_TransfersResources()
        {
            // Arrange
            const int fromPlayerId = 0;
            const int toPlayerId = 1;
            _resourceManager.InitializePlayer(fromPlayerId);
            _resourceManager.InitializePlayer(toPlayerId);
            _resourceManager.AddResources(fromPlayerId, ResourceManager.ResourceType.Wood, 10);

            var transaction = new Transaction
            {
                FromPlayerId = fromPlayerId,
                ToPlayerId = toPlayerId,
                ResourceType = ResourceManager.ResourceType.Wood,
                Amount = 3,
                Reason = "Test trade"
            };

            // Act
            var result = _resourceManager.ExecuteTransaction(transaction);

            // Assert
            Assert.IsTrue(result);
            var fromResources = _resourceManager.GetPlayerResources(fromPlayerId);
            var toResources = _resourceManager.GetPlayerResources(toPlayerId);
            Assert.AreEqual(7, fromResources[ResourceManager.ResourceType.Wood]);
            Assert.AreEqual(3, toResources[ResourceManager.ResourceType.Wood]);
        }

        [Test]
        public void ExecuteTransaction_InsufficientResources_ReturnsFalse()
        {
            // Arrange
            const int fromPlayerId = 0;
            const int toPlayerId = 1;
            _resourceManager.InitializePlayer(fromPlayerId);
            _resourceManager.InitializePlayer(toPlayerId);
            _resourceManager.AddResources(fromPlayerId, ResourceManager.ResourceType.Wood, 2);

            var transaction = new Transaction
            {
                FromPlayerId = fromPlayerId,
                ToPlayerId = toPlayerId,
                ResourceType = ResourceManager.ResourceType.Wood,
                Amount = 5,
                Reason = "Test trade"
            };

            // Act
            var result = _resourceManager.ExecuteTransaction(transaction);

            // Assert
            Assert.IsFalse(result);
            var fromResources = _resourceManager.GetPlayerResources(fromPlayerId);
            var toResources = _resourceManager.GetPlayerResources(toPlayerId);
            Assert.AreEqual(2, fromResources[ResourceManager.ResourceType.Wood]); // Unchanged
            Assert.AreEqual(0, toResources[ResourceManager.ResourceType.Wood]); // Unchanged
        }

        [Test]
        public void GetTotalResources_ReturnsSumAcrossAllPlayers()
        {
            // Arrange
            const int player1Id = 0;
            const int player2Id = 1;
            _resourceManager.InitializePlayer(player1Id);
            _resourceManager.InitializePlayer(player2Id);
            _resourceManager.AddResources(player1Id, ResourceManager.ResourceType.Wood, 5);
            _resourceManager.AddResources(player2Id, ResourceManager.ResourceType.Wood, 3);

            // Act
            var totalResources = _resourceManager.GetTotalResources();

            // Assert
            Assert.AreEqual(8, totalResources[ResourceManager.ResourceType.Wood]);
        }

        [Test]
        public void GetEconomicMetrics_ReturnsCorrectMetrics()
        {
            // Arrange
            const int playerId = 0;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 3);  // Frozen
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Sheep, 2); // Liquid
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wheat, 4);  // Frozen
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Gold, 1);   // Liquid

            // Act
            var metrics = _resourceManager.GetEconomicMetrics(playerId);

            // Assert
            Assert.AreEqual(playerId, metrics.PlayerId);
            Assert.AreEqual(3, metrics.LiquidCommodities);  // Sheep + Gold
            Assert.AreEqual(7, metrics.FrozenCommodities);   // Wood + Wheat
            Assert.AreEqual(10, metrics.TotalResources);
            Assert.AreEqual(0.3f, metrics.GetBalanceRatio(), 0.01f); // 3/10 = 0.3
        }

        [Test]
        public void GetEconomicMetrics_IsPotentialRunawayLeader_WhenDominant()
        {
            // Arrange
            const int playerId = 0;
            const int totalPlayers = 3;
            _resourceManager.InitializePlayer(playerId);
            _resourceManager.InitializePlayer(1);
            _resourceManager.InitializePlayer(2);
            
            // Give player 0 significantly more resources
            _resourceManager.AddResources(playerId, ResourceManager.ResourceType.Wood, 15);
            _resourceManager.AddResources(1, ResourceManager.ResourceType.Wood, 3);
            _resourceManager.AddResources(2, ResourceManager.ResourceType.Wood, 2);

            // Act
            var metrics = _resourceManager.GetEconomicMetrics(playerId);

            // Assert
            Assert.IsTrue(metrics.IsPotentialRunawayLeader(totalPlayers));
        }
    }
}
