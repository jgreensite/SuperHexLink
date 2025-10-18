using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class HexStateTests
    {
        [Test]
        public void Initialize_BindsBackingState()
        {
            var go = new GameObject("hex_test");
            var hex = go.AddComponent<Hex>();
            var spawnerGO = new GameObject("hex_spawner");
            var spawner = spawnerGO.AddComponent<HexSpawner>();

            var backing = new Hex.BaseHexState { Col = 3, Row = 4, Selected = true };
            hex.Initialize(spawner, backing);

            Assert.AreEqual(3, hex.Col);
            Assert.AreEqual(4, hex.Row);
            Assert.IsTrue(hex.hexState.Selected);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(spawnerGO);
        }

        [Test]
        public void ToggleSelect_ChangesSelectedFlag()
        {
            var go = new GameObject("hex_test2");
            var hex = go.AddComponent<Hex>();
            var spawnerGO = new GameObject("hex_spawner2");
            var spawner = spawnerGO.AddComponent<HexSpawner>();

            var backing = new Hex.BaseHexState { Col = 1, Row = 2, Selected = false };
            hex.Initialize(spawner, backing);

            Assert.IsFalse(hex.hexState.Selected);
            hex.ToggleSelect();
            Assert.IsTrue(hex.hexState.Selected);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(spawnerGO);
        }

        [Test]
        public void Refresh_PreservesSavedHexTypes_EditMode()
        {
            // Arrange
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();

            var hsGo = new GameObject("HexSpawner");
            var hexSpawner = hsGo.AddComponent<HexSpawner>();

            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.State.hexGridConfig = new HexGridConfig { cols = 2, rows = 2, radius = 1, height = 1f };

            var s = new HexSpawner.HexSpawnerState();
            s.hexes = new System.Collections.Generic.List<System.Collections.Generic.List<Hex.HexState>>();
            for (int c = 0; c < 2; c++)
            {
                s.hexes.Add(new System.Collections.Generic.List<Hex.HexState>());
                for (int r = 0; r < 2; r++)
                {
                    var state = new Hex.HexState { Col = c, Row = r, HexType = "FIELD", Selected = false };
                    s.hexes[c].Add(state);
                }
            }

            hexSpawner.State = s;

            var hexPrefab = new GameObject("hex_prefab");
            var hexComp = hexPrefab.AddComponent<Hex>();
            hexSpawner.hexPrefab = hexComp;

            hexSpawner.hexTextPrefab = new GameObject("text_prefab").AddComponent<HexText>();
            hexSpawner.hexLandPrefab = new GameObject("land_prefab").AddComponent<HexLandModel>();

            hexSpawner.GetType().GetField("gameSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hexSpawner, gameSpawner);

            // Act
            hexSpawner.BuildMe(true);

            var hexInstances = UnityEngine.Object.FindObjectsOfType<Hex>();
            Assert.IsTrue(hexInstances.Length > 0, "No hexes created by BuildMe");
            var sample = hexInstances[0];
            string beforeType = sample.hexState.HexType;

            hexSpawner.Refresh();

            string afterType = sample.hexState.HexType;
            Assert.AreEqual(beforeType, afterType, "HexType changed after Refresh when it should have been preserved");

            // Cleanup
            Object.DestroyImmediate(gsGo);
            Object.DestroyImmediate(hsGo);
            Object.DestroyImmediate(hexPrefab);
        }
    }
}
