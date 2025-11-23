// <copyright file="BaseHexStateSerializerTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Models;
    using CoreLogic.Serialization;

    public class BaseHexStateSerializerTests
    {
        [Test]
        public void SerializeDeserialize_BaseHexState_Roundtrip()
        {
            var b = new BaseHexState { Col = 3, Row = 4, HexType = "FIELD", HexSubType = "A", Rotation = 1, HexNum = 7, GroupID = "g1", Selected = true };
            var json = BaseHexStateSerializer.Serialize(b);
            Assert.IsNotNull(json);

            var outB = BaseHexStateSerializer.Deserialize(json);
            Assert.IsNotNull(outB);
            Assert.AreEqual(b, outB);
        }

        [Test]
        public void Deserialize_NullOrEmpty_ReturnsNull()
        {
            var a = BaseHexStateSerializer.Deserialize(null);
            Assert.IsNull(a);

            var b = BaseHexStateSerializer.Deserialize(string.Empty);
            Assert.IsNull(b);
        }
    }
}
