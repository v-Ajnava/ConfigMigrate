// Copyright (c) Kamesh Tanneru. All rights reserved.

namespace ConfigMigrate.Models
{
    using System;
    using System.Diagnostics;

    using Newtonsoft.Json;
    using Z.Core.Extensions;

    [DebuggerDisplay("{ConfigName}")]
    public class ConfigEntryKey : IEquatable<ConfigEntryKey>, IComparable<ConfigEntryKey>
    {
        [JsonProperty("configName")]
        public string ConfigName { get; set; }

        [JsonProperty("scopeName")]
        public string ScopeName { get; set; }

        public int CompareTo(ConfigEntryKey other)
        {
            var spaces = "                ";
            return StringComparer.OrdinalIgnoreCase.Compare($"{ScopeName}{spaces}{ConfigName}", $"{other.ScopeName}{spaces}{other.ConfigName}");
        }

        public bool Equals(ConfigEntryKey other)
        {
            return ScopeName.EqualsIgnoreCase(other.ScopeName)
                && ConfigName.EqualsIgnoreCase(other.ConfigName);
        }

        public override int GetHashCode()
        {
            return ScopeName.ToLowerInvariant().GetHashCode()
                ^ ConfigName.ToLowerInvariant().GetHashCode();
        }
    }
}
