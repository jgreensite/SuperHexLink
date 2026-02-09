// <copyright file="BaseHexStateBoundaryTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Models;
    using CoreLogic.Serialization;

    /// <summary>
    /// Tests for BaseHexState boundary conditions: null fields, empty strings,
    /// missing JSON properties, and extreme values.
    /// </summary>
    public class BaseHexStateBoundaryTests
    {
        [Test]
        public void Serialize_NullHexType_RoundTrips()
        {
            var state = new BaseHexState { Col = 1, Row = 2, HexType = null };
            var json = BaseHexStateSerializer.Serialize(state);
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.IsNull(result.HexType);
            Assert.AreEqual(1, result.Col);
            Assert.AreEqual(2, result.Row);
        }

        [Test]
        public void Serialize_EmptyStringFields_RoundTrips()
        {
            var state = new BaseHexState
            {
                Col = 0,
                Row = 0,
                HexType = "",
                HexSubType = "",
                GroupID = "",
            };
            var json = BaseHexStateSerializer.Serialize(state);
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.AreEqual("", result.HexType);
            Assert.AreEqual("", result.HexSubType);
            Assert.AreEqual("", result.GroupID);
        }

        [Test]
        public void Serialize_NullHexNum_RoundTrips()
        {
            var state = new BaseHexState { Col = 3, Row = 4, HexNum = null };
            var json = BaseHexStateSerializer.Serialize(state);
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.IsNull(result.HexNum);
        }

        [Test]
        public void Serialize_MaxIntCoords_RoundTrips()
        {
            var state = new BaseHexState { Col = int.MaxValue, Row = int.MinValue };
            var json = BaseHexStateSerializer.Serialize(state);
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.AreEqual(int.MaxValue, result.Col);
            Assert.AreEqual(int.MinValue, result.Row);
        }

        [Test]
        public void Serialize_AllFieldsPopulated_RoundTrips()
        {
            var state = new BaseHexState
            {
                Col = 5,
                Row = 10,
                HexType = "HARBOUR",
                HexSubType = "GENERIC",
                Rotation = 300,
                HexNum = 12,
                GroupID = "group-42",
                Selected = true,
            };
            var json = BaseHexStateSerializer.Serialize(state);
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.AreEqual(state, result);
        }

        [Test]
        public void Deserialize_MissingFields_DefaultsToZeroOrNull()
        {
            // Minimal JSON — only col and row
            var json = "{\"col\":7,\"row\":8}";
            var result = BaseHexStateSerializer.Deserialize(json);

            Assert.AreEqual(7, result.Col);
            Assert.AreEqual(8, result.Row);
            Assert.IsNull(result.HexType);
            Assert.AreEqual(0, result.Rotation);
            Assert.IsNull(result.HexNum);
            Assert.IsFalse(result.Selected);
        }

        [Test]
        public void Deserialize_EmptyJson_DoesNotThrow()
        {
            var result = BaseHexStateSerializer.Deserialize("{}");

            Assert.AreEqual(0, result.Col);
            Assert.AreEqual(0, result.Row);
            Assert.IsNull(result.HexType);
        }

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = new BaseHexState { Col = 1, Row = 2, HexType = "SEA" };
            var b = new BaseHexState { Col = 1, Row = 2, HexType = "SEA" };

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            var a = new BaseHexState { Col = 1, Row = 2, HexType = "SEA" };
            var b = new BaseHexState { Col = 1, Row = 2, HexType = "LAND" };

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Equals_Null_ReturnsFalse()
        {
            var a = new BaseHexState { Col = 1, Row = 2 };
            Assert.IsFalse(a.Equals(null));
        }
    }
}
