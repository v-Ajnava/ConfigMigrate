// Copyright (c) Kamesh Tanneru. All rights reserved.

namespace ConfigMigrate.Models
{
    using Newtonsoft.Json;

    using Z.Core.Extensions;

    public class ConfigEntry
    {
        [JsonProperty("configEntryKey")]
        public ConfigEntryKey ConfigEntryKey { get; set; }

        [JsonProperty("configValue")]
        public string ConfigValue { get; set; }

        public static ConfigEntry GetConfigEntry(string scope, string key, string value)
        {
            if (scope.EqualsIgnoreCase("notificationhubsystem"))
            {
                return null;
            }

            if (scope.EqualsIgnoreCase("admin"))
            {
                scope = "$system";
                key = "admin." + key;
            }

            var entry = new ConfigEntry
            {
                ConfigEntryKey = new ConfigEntryKey { ScopeName = scope, ConfigName = key },
                ConfigValue = value
            };

            return entry;
        }
    }
}
