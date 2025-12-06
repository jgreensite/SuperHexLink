// <copyright file="HexState.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Small testable POCO representing a persisted hex minimal state.
    /// </summary>
    public class HexState
    {
        [JsonProperty("col")]
        public int Col { get; set; }

        [JsonProperty("row")]
        public int Row { get; set; }

        [JsonProperty("hexType")]
        public string HexType { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is HexState o)
            {
                return Col == o.Col && Row == o.Row && HexType == o.HexType;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (Col, Row, HexType).GetHashCode();
        }
    }
}
