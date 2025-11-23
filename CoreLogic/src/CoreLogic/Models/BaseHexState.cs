// <copyright file="BaseHexState.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Lightweight POCO representing the serializable backing state for a hex.
    /// Kept minimal so it can be used in dotnet tests outside Unity.
    /// </summary>
    public class BaseHexState
    {
        [JsonProperty("col")]
        public int Col { get; set; }

        [JsonProperty("row")]
        public int Row { get; set; }

        [JsonProperty("hexType")]
        public string HexType { get; set; }

        [JsonProperty("hexSubType")]
        public string HexSubType { get; set; }

        [JsonProperty("rotation")]
        public int Rotation { get; set; }

        [JsonProperty("hexNum")]
        public int? HexNum { get; set; }

        [JsonProperty("groupId")]
        public string GroupID { get; set; }

        [JsonProperty("selected")]
        public bool Selected { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is BaseHexState o)
            {
                return Col == o.Col && Row == o.Row && HexType == o.HexType && HexSubType == o.HexSubType && Rotation == o.Rotation && HexNum == o.HexNum && GroupID == o.GroupID && Selected == o.Selected;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (Col, Row, HexType, HexSubType, Rotation, HexNum, GroupID, Selected).GetHashCode();
        }
    }
}
