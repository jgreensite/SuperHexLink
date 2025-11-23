using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Linq;
using Sirenix.Serialization;

namespace Tests.Editor
{
    public class MapLifecycleTests
    {
        private const string TempMapDir = "./data/maps/testmap_dir/";
        private const string TempMapPath = TempMapDir + "map.json";

        [SetUp]
        public void SetUp()
        {
            // Ensure directory exists and is clean
            if (Directory.Exists(TempMapDir)) Directory.Delete(TempMapDir, true);
            Directory.CreateDirectory(TempMapDir);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any created GameObjects
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("hex") || go.name.StartsWith("HexSpawner") || go.name.StartsWith("GameSpawner") || go.name.StartsWith("text_") || go.name.StartsWith("landModel") || go.name.StartsWith("hex_prefab"))
                {
                    Object.DestroyImmediate(go);
                }
            }
            // Remove temp map dir
            if (Directory.Exists(TempMapDir)) Directory.Delete(TempMapDir, true);
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

            // Save using GameSpawner.SaveHexes (saves to directory + "map.json")
            gameSpawner.GetType().GetMethod("SaveHexes").Invoke(gameSpawner, new object[] { TempMapDir });

            // Read original saved bytes
            Assert.IsTrue(File.Exists(TempMapPath), "map.json should have been written by SaveHexes");
            var beforeBytes = File.ReadAllBytes(TempMapPath);

            // Clear all and ensure no hexes
            hexSpawner.Clear();
            Assert.AreEqual(0, Object.FindObjectsOfType<Hex>().Length, "Clear should remove created hexes before Load");

            // Load via GameSpawner.LoadState using the exact map.json path
            gameSpawner.GetType().GetMethod("LoadState").Invoke(gameSpawner, new object[] { TempMapPath });

            // After loading, re-save to the same directory
            gameSpawner.GetType().GetMethod("SaveHexes").Invoke(gameSpawner, new object[] { TempMapDir });

            Assert.IsTrue(File.Exists(TempMapPath), "map.json should exist after re-save");
            var afterBytes = File.ReadAllBytes(TempMapPath);

            // The saved file before load and after load->save should be semantically equal.
            var beforeObj = SerializationUtility.DeserializeValue<GameSpawner.CombinedSpawnerState>(beforeBytes, DataFormat.JSON);
            var afterObj = SerializationUtility.DeserializeValue<GameSpawner.CombinedSpawnerState>(afterBytes, DataFormat.JSON);

            AssertCombinedSpawnerStateEqual(beforeObj, afterObj);

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

        private void AssertCombinedSpawnerStateEqual(GameSpawner.CombinedSpawnerState a, GameSpawner.CombinedSpawnerState b)
        {
            Assert.IsNotNull(a);
            Assert.IsNotNull(b);

            // Compare GameState basics (hexGridConfig)
            Assert.IsNotNull(a.GameState);
            Assert.IsNotNull(b.GameState);
            Assert.AreEqual(a.GameState.hexGridConfig.cols, b.GameState.hexGridConfig.cols, "cols differ");
            Assert.AreEqual(a.GameState.hexGridConfig.rows, b.GameState.hexGridConfig.rows, "rows differ");

            // Compare HexSpawnerState structure
            Assert.IsNotNull(a.HexState);
            Assert.IsNotNull(b.HexState);
            var ah = a.HexState.hexes;
            var bh = b.HexState.hexes;
            Assert.AreEqual(ah.Count, bh.Count, "Hex columns count differ");
            for (int c = 0; c < ah.Count; c++)
            {
                Assert.AreEqual(ah[c].Count, bh[c].Count, $"Hex rows count differ at col {c}");
                for (int r = 0; r < ah[c].Count; r++)
                {
                    var x = ah[c][r];
                    var y = bh[c][r];
                    Assert.AreEqual(x.Col, y.Col, $"Col mismatch at {c},{r}");
                    Assert.AreEqual(x.Row, y.Row, $"Row mismatch at {c},{r}");
                    Assert.AreEqual(x.HexType, y.HexType, $"HexType mismatch at {c},{r}");
                    Assert.AreEqual(x.HexSubType, y.HexSubType, $"HexSubType mismatch at {c},{r}");
                    Assert.AreEqual(x.Rotation, y.Rotation, $"Rotation mismatch at {c},{r}");
                    Assert.AreEqual(x.HexNum, y.HexNum, $"HexNum mismatch at {c},{r}");
                    Assert.AreEqual(x.GroupID, y.GroupID, $"GroupID mismatch at {c},{r}");
                    Assert.AreEqual(x.Selected, y.Selected, $"Selected mismatch at {c},{r}");
                }
            }
        }
    }
}
