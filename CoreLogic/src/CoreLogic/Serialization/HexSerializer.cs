// <copyright file="HexSerializer.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Serialization
{
    using CoreLogic.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public static class HexSerializer
    {
        public static string SerializeList(IList<HexState> hexes)
        {
            return JsonConvert.SerializeObject(hexes);
        }

        public static IList<HexState> DeserializeList(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<HexState>();
            return JsonConvert.DeserializeObject<IList<HexState>>(json) ?? new List<HexState>();
        }
    }
}
