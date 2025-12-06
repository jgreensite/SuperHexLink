// <copyright file="HexStateTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Models;
    using Newtonsoft.Json;

    public class HexStateTests
    {
        [Test]
        public void Json_Serialize_Deserialize_Roundtrip()
        {
            var original = new HexState { Col = 1, Row = 2, HexType = "FIELD" };
            var json = JsonConvert.SerializeObject(original);
            Assert.IsNotNull(json);

            var deserialized = JsonConvert.DeserializeObject<HexState>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(original, deserialized);
        }
    }
}
