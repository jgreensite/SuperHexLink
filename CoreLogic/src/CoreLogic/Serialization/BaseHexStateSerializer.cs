// <copyright file="BaseHexStateSerializer.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Serialization
{
    using CoreLogic.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public static class BaseHexStateSerializer
    {
        public static string Serialize(BaseHexState b)
        {
            return JsonConvert.SerializeObject(b);
        }

        public static BaseHexState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonConvert.DeserializeObject<BaseHexState>(json);
        }
    }
}
