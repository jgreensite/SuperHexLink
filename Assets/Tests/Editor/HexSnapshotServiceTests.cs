using NUnit.Framework;
using SuperHexLink.Utils;
using System.Collections.Generic;

public class HexSnapshotServiceTests
{
    private HexSnapshotService _service;

    [SetUp]
    public void Setup()
    {
        _service = new HexSnapshotService();
    }

    [Test]
    public void CreateSnapshot_WithValidData_ReturnsBytes()
    {
        var data = new List<string> { "Test" };
        var bytes = _service.CreateSnapshot(data);
        
        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 0);
    }

    [Test]
    public void SerializeDeserialize_RoundTrip_RestoresData()
    {
        var original = new HexSpawner.HexSpawnerState(); // Assuming this is public or we use a simple test class
        // Let's use a simple dictionary or list to test the serialization logic itself, 
        // effectively testing Sirenix integration via our wrapper.
        var testData = new Dictionary<string, int> { { "Life", 42 } };

        var snapshot = _service.CreateSnapshot(testData);
        var restored = _service.RestoreSnapshot<Dictionary<string, int>>(snapshot);

        Assert.IsNotNull(restored);
        Assert.AreEqual(42, restored["Life"]);
    }

    [Test]
    public void CreateSnapshot_NullInput_ReturnsNull()
    {
        var bytes = _service.CreateSnapshot<object>(null);
        Assert.IsNull(bytes);
    }

    [Test]
    public void RestoreSnapshot_NullOrEmpty_ReturnsDefault()
    {
        var res1 = _service.RestoreSnapshot<object>(null);
        Assert.IsNull(res1);

        var res2 = _service.RestoreSnapshot<object>(new byte[0]);
        Assert.IsNull(res2);
    }

    [Test]
    public void SerializeDeserialize_HexSpawnerState_DeepObject_RestoresCorrectly()
    {
        var originalState = new HexSpawner.HexSpawnerState();
        originalState.hexes = new List<List<Hex.HexState>>();
        originalState.hexes.Add(new List<Hex.HexState>());
        originalState.hexes[0].Add(new Hex.HexState
        {
            Col = 0, Row = 0, HexType = "Grass", HexNum = 5, Rotation = 60
        });

        var bytes = _service.CreateSnapshot(originalState);
        Assert.IsNotNull(bytes);
        Assert.Greater(bytes.Length, 0);

        var restoredState = _service.RestoreSnapshot<HexSpawner.HexSpawnerState>(bytes);
        
        Assert.IsNotNull(restoredState);
        Assert.IsNotNull(restoredState.hexes);
        Assert.AreEqual(1, restoredState.hexes.Count);
        Assert.AreEqual(1, restoredState.hexes[0].Count);
        Assert.AreEqual("Grass", restoredState.hexes[0][0].HexType);
        Assert.AreEqual(5, restoredState.hexes[0][0].HexNum);
        Assert.AreEqual(60, restoredState.hexes[0][0].Rotation);
    }
}
