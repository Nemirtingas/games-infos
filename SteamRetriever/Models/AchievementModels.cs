using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SteamRetriever.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum SchemaStatType
{
    [EnumMember(Value = "int")]
    Int = 1,
    [EnumMember(Value = "float")]
    Float = 2,
    [EnumMember(Value = "avgrate")]
    AvgRate = 3,
    [EnumMember(Value = "bits")]
    Bits = 4, // Achievements
}

public class AchievementStatsThresholdsModel
{
    [JsonProperty("stat_name")]
    public string StatName { get; set; }

    [JsonProperty("min_val")]
    public long MinVal { get; set; }

    [JsonProperty("max_val")]
    public long MaxVal { get; set; }
}

public class AchievementModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("hidden")]
    public bool Hidden { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonProperty("icongray")]
    public string IconGray { get; set; }

    [JsonProperty("displayName")]
    public SortedDictionary<string, string> DisplayName { get; set; }

    [JsonProperty("description")]
    public SortedDictionary<string, string> Description { get; set; }

    [JsonProperty("stats_thresholds")]
    public List<AchievementStatsThresholdsModel> StatsThresholds { get; set; }
}

public class StatModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("type")]
    public SchemaStatType Type { get; set; }

    [JsonProperty("incrementonly")]
    public bool IncrementOnly { get; set; }

    [JsonProperty("aggregated")]
    public bool Aggregated { get; set; }

    [JsonProperty("maxchange")]
    public object MaxChange { get; set; }

    [JsonProperty("min")]
    public object Min { get; set; }

    [JsonProperty("max")]
    public object Max { get; set; }

    [JsonProperty("default")]
    public object Default { get; set; }

    [JsonProperty("windowsize")]
    public decimal? WindowSize { get; set; }

}