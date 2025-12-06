// <copyright file="HexSerializerTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Models;
    using CoreLogic.Serialization;
    using System.Collections.Generic;

    public class HexSerializerTests
    {
        [Test]
        public void SerializeDeserialize_List_Roundtrip()
        {
            var list = new List<HexState> {
                new HexState { Col = 0, Row = 0, HexType = "SEA" },
                new HexState { Col = 1, Row = 0, HexType = "FIELD" }
            };

            var json = HexSerializer.SerializeList(list);
            Assert.IsNotNull(json);

            var outList = HexSerializer.DeserializeList(json);
            Assert.IsNotNull(outList);
            Assert.AreEqual(list.Count, outList.Count);
            Assert.AreEqual(list[0], outList[0]);
            Assert.AreEqual(list[1], outList[1]);
        }

        [Test]
        public void Deserialize_EmptyOrNull_ReturnsEmptyList()
        {
            var a = HexSerializer.DeserializeList(null);
            Assert.IsNotNull(a);
            Assert.AreEqual(0, a.Count);

            var b = HexSerializer.DeserializeList("");
            Assert.IsNotNull(b);
            Assert.AreEqual(0, b.Count);
        }
    }
}
