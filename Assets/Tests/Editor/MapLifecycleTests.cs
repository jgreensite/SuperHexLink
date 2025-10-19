using NUnit.Framework;
using UnityEngine;
using System.IO;

namespace Tests.Editor
{
    public class MapLifecycleTests
    {
        private const string TempMapPath = "./data/maps/test_map.json";

        [SetUp]
        public void SetUp()
        {
            // Ensure cleaned environment
            if (File.Exists(TempMapPath)) File.Delete(TempMapPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any created GameObjects
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("hex") || go.name.StartsWith("HexSpawner") || go.name.StartsWith("GameSpawner") || go.name.StartsWith("text_") || go.name.StartsWith("landModel"))
                {
                    Object.DestroyImmediate(go);
                }
            }
            if (File.Exists(TempMapPath)) File.Delete(TempMapPath);
        }

        [Test]
        public void SpawnCreatesCorrectGrid()
        {
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();

            var hsGo = new GameObject("HexSpawner");
            var hexSpawner = hsGo.AddComponent<HexSpawner>();

            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.State.hexGridConfig = new HexGridConfig { cols = 3, rows = 2, radius = 1, height = 1f };

            // Assign prefabs
            var hexPrefab = new GameObject("hex_prefab");
            var hexComp = hexPrefab.AddComponent<Hex>();
            hexSpawner.hexPrefab = hexComp;
            hexSpawner.hexTextPrefab = new GameObject("text_prefab").AddComponent<HexText>();
            hexSpawner.hexLandPrefab = new GameObject("land_prefab").AddComponent<HexLandModel>();

            hexSpawner.GetType().GetField("gameSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hexSpawner, gameSpawner);

            hexSpawner.Spawn();

            var hexes = Object.FindObjectsOfType<Hex>();
            Assert.AreEqual(3 * 2, hexes.Length, "Spawn should create cols*rows hexes");

            Object.DestroyImmediate(gsGo);
            Object.DestroyImmediate(hsGo);
            Object.DestroyImmediate(hexPrefab);
        }

        [Test]
        public void SaveThenLoad_PreservesHexTypes()
        {
            // Arrange
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();

            var hsGo = new GameObject("HexSpawner");
            var hexSpawner = hsGo.AddComponent<HexSpawner>();

            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.State.hexGridConfig = new HexGridConfig { cols = 2, rows = 2, radius = 1, height = 1f };

            // Create a hex state grid with set types
            var s = new HexSpawner.HexSpawnerState();
            s.hexes = new System.Collections.Generic.List<System.Collections.Generic.List<Hex.HexState>>();
            for (int c = 0; c < 2; c++)
            {
                s.hexes.Add(new System.Collections.Generic.List<Hex.HexState>());
                for (int r = 0; r < 2; r++)
                {
                    s.hexes[c].Add(new Hex.HexState { Col = c, Row = r, HexType = (c == 0 && r == 0) ? "FIELD" : "SEA" });
                }
            }
            hexSpawner.State = s;

            var hexPrefab = new GameObject("hex_prefab");
            var hexComp = hexPrefab.AddComponent<Hex>();
            hexSpawner.hexPrefab = hexComp;
            hexSpawner.hexTextPrefab = new GameObject("text_prefab").AddComponent<HexText>();
            hexSpawner.hexLandPrefab = new GameObject("land_prefab").AddComponent<HexLandModel>();

            hexSpawner.GetType().GetField("gameSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hexSpawner, gameSpawner);

            // Act: Build (should respect provided state)
            hexSpawner.BuildMe(true);

            var created = Object.FindObjectsOfType<Hex>();
            Assert.IsTrue(created.Length > 0, "BuildMe should create hex GameObjects");

            // Save using GameSpawner.SaveHexes
            gameSpawner.GetType().GetMethod("SaveHexes").Invoke(gameSpawner, new object[] { Path.GetDirectoryName(TempMapPath) + "/" });

            // Clear all
            hexSpawner.Clear();

            // Load via GameSpawner.LoadState
            gameSpawner.GetType().GetMethod("LoadState").Invoke(gameSpawner, new object[] { TempMapPath });

            // After loading, update hexes
            hexSpawner.UpdateHexes();

            var post = Object.FindObjectsOfType<Hex>();
            Assert.IsTrue(post.Length > 0, "Load should re-create hex GameObjects");
            // Check that at least one hex preserved its type
            bool foundField = false;
            foreach (var h in post)
            {
                if (h.hexState.HexType == "FIELD") foundField = true;
            }
            Assert.IsTrue(foundField, "Saved FIELD type should be present after Load");

            // Cleanup
            Object.DestroyImmediate(gsGo);
            Object.DestroyImmediate(hsGo);
            Object.DestroyImmediate(hexPrefab);
        }

        [Test]
        public void ClearRemovesGameObjects()
        {
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();

            var hsGo = new GameObject("HexSpawner");
            var hexSpawner = hsGo.AddComponent<HexSpawner>();

            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.State.hexGridConfig = new HexGridConfig { cols = 2, rows = 2, radius = 1, height = 1f };

            var hexPrefab = new GameObject("hex_prefab");
            var hexComp = hexPrefab.AddComponent<Hex>();
            hexSpawner.hexPrefab = hexComp;
            hexSpawner.hexTextPrefab = new GameObject("text_prefab").AddComponent<HexText>();
            hexSpawner.hexLandPrefab = new GameObject("land_prefab").AddComponent<HexLandModel>();

            hexSpawner.GetType().GetField("gameSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hexSpawner, gameSpawner);

            hexSpawner.Spawn();
            var countBefore = Object.FindObjectsOfType<Hex>().Length;
            Assert.Greater(countBefore, 0);

            hexSpawner.Clear();
            var countAfter = Object.FindObjectsOfType<Hex>().Length;
            Assert.AreEqual(0, countAfter, "Clear should remove all hex GameObjects");

            Object.DestroyImmediate(gsGo);
            Object.DestroyImmediate(hsGo);
            Object.DestroyImmediate(hexPrefab);
        }

        [Test]
        public void SpawnMultipleTimes_NoStacking()
        {
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();

            var hsGo = new GameObject("HexSpawner");
            var hexSpawner = hsGo.AddComponent<HexSpawner>();

            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.State.hexGridConfig = new HexGridConfig { cols = 3, rows = 2, radius = 1, height = 1f };

            var hexPrefab = new GameObject("hex_prefab");
            var hexComp = hexPrefab.AddComponent<Hex>();
            hexSpawner.hexPrefab = hexComp;
            hexSpawner.hexTextPrefab = new GameObject("text_prefab").AddComponent<HexText>();
            hexSpawner.hexLandPrefab = new GameObject("land_prefab").AddComponent<HexLandModel>();

            hexSpawner.GetType().GetField("gameSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hexSpawner, gameSpawner);

            // Perform spawn multiple times
            hexSpawner.Spawn();
            var firstCount = Object.FindObjectsOfType<Hex>().Length;
            hexSpawner.Spawn();
            var secondCount = Object.FindObjectsOfType<Hex>().Length;

            Assert.AreEqual(firstCount, secondCount, "Spawning twice should not stack additional hexes");

            Object.DestroyImmediate(gsGo);
            Object.DestroyImmediate(hsGo);
            Object.DestroyImmediate(hexPrefab);
        }
    }
}
