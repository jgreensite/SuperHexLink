using NUnit.Framework;
using SuperHexLink.Rules;
using SuperHexLink.Gameplay;
using SuperHexLink.Logging;
using System.Collections.Generic;

namespace SuperHexLink.Tests.Editor
{
    [TestFixture]
    public class EconomicBalancerTests
    {
        private EconomicBalancer _balancer;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings
            {
                IsEnabled = (category, severity) => true,
                LogLevel = ActionLogSeverity.Info
            };
            _balancer = new EconomicBalancer(_logSettings);
        }

        // ── CalculateExhaustedProduction ──────────────────────────────────────────

        [Test]
        public void CalculateExhaustedProduction_NoExhaustion_ReturnsSameValue()
        {
            var result = _balancer.CalculateExhaustedProduction(10, 0);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void CalculateExhaustedProduction_LevelOne_ReducesByTenPercent()
        {
            // 10 base, 1 exhaustion level = 10% reduction → 10 - 1 = 9
            var result = _balancer.CalculateExhaustedProduction(10, 1);
            Assert.AreEqual(9, result);
        }

        [Test]
        public void CalculateExhaustedProduction_LevelFive_ReducesByFiftyPercent()
        {
            // 10 base, 5 exhaustion levels = 50% reduction → 10 - 5 = 5
            var result = _balancer.CalculateExhaustedProduction(10, 5);
            Assert.AreEqual(5, result);
        }

        [Test]
        public void CalculateExhaustedProduction_HighExhaustion_NeverBelowOne()
        {
            // 1 base, 100 levels → would go to 0 or negative, must clamp to 1
            var result = _balancer.CalculateExhaustedProduction(1, 100);
            Assert.GreaterOrEqual(result, 1, "Production should never drop below 1");
        }

        [Test]
        public void CalculateExhaustedProduction_NegativeLevel_ReturnsSameValue()
        {
            // Guard: negative exhaustion treated as 0
            var result = _balancer.CalculateExhaustedProduction(10, -1);
            Assert.AreEqual(10, result);
        }

        // ── CanExpandTerritory ────────────────────────────────────────────────────

        [Test]
        public void CanExpandTerritory_BelowSaturationThreshold_ReturnsTrue()
        {
            // 5 of 20 territory = 25% saturation, well below 70%
            var canExpand = _balancer.CanExpandTerritory(0, 5, 20);
            Assert.IsTrue(canExpand);
        }

        [Test]
        public void CanExpandTerritory_AboveSaturationThreshold_ReturnsFalse()
        {
            // 15 of 20 territory = 75% saturation, above the 70% threshold
            var canExpand = _balancer.CanExpandTerritory(0, 15, 20);
            Assert.IsFalse(canExpand);
        }

        [Test]
        public void CanExpandTerritory_AtExactThreshold_ReturnsFalse()
        {
            // 14 of 20 = 70%, which equals the threshold exactly
            var canExpand = _balancer.CanExpandTerritory(0, 14, 20);
            Assert.IsFalse(canExpand);
        }

        // ── ShouldBalancePlayer ───────────────────────────────────────────────────

        [Test]
        public void ShouldBalancePlayer_WhenRunawayLeader_ReturnsTrue()
        {
            // Player has 3x resources of others → IsPotentialRunawayLeader = true
            var metrics = new EconomicMetrics
            {
                PlayerId = 0,
                TotalResources = 30,
                LiquidCommodities = 5,
                FrozenCommodities = 25
            };

            // 3 players, average = 10; 30 > 10 * 1.5 = 15 → runaway leader
            var result = _balancer.ShouldBalancePlayer(0, metrics, 3);
            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldBalancePlayer_WhenNormal_ReturnsFalse()
        {
            var metrics = new EconomicMetrics
            {
                PlayerId = 1,
                TotalResources = 9,       // Not a runaway leader (9 <= 10 * 1.5 with 3 players of 9 each)
                LiquidCommodities = 2,    // Balance ratio = 2/9 ≈ 0.22, below 0.8
                FrozenCommodities = 7
            };

            // 3 players, total = 27, average = 9; 9 is not > 9 * 1.5 = 13.5
            var result = _balancer.ShouldBalancePlayer(1, metrics, 3);
            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldBalancePlayer_WhenLiquidRatioHigh_ReturnsTrue()
        {
            // Balance ratio > 0.8 triggers balancing even without runaway lead
            var metrics = new EconomicMetrics
            {
                PlayerId = 2,
                TotalResources = 10,
                LiquidCommodities = 9,    // 9/10 = 0.9 balance ratio, above 0.8
                FrozenCommodities = 1
            };

            var result = _balancer.ShouldBalancePlayer(2, metrics, 4);
            Assert.IsTrue(result);
        }

        // ── GetEconomicRecommendations ────────────────────────────────────────────

        [Test]
        public void GetEconomicRecommendations_ReturnsNonNull()
        {
            var metrics = new EconomicMetrics
            {
                PlayerId = 0,
                TotalResources = 5,
                LiquidCommodities = 3,
                FrozenCommodities = 2,
                ResourceDistribution = new Dictionary<ResourceManager.ResourceType, int>
                {
                    { ResourceManager.ResourceType.Wood, 2 },
                    { ResourceManager.ResourceType.Brick, 1 },
                    { ResourceManager.ResourceType.Sheep, 2 }
                }
            };

            var recommendations = _balancer.GetEconomicRecommendations(0, metrics);
            Assert.IsNotNull(recommendations);
        }
    }
}
