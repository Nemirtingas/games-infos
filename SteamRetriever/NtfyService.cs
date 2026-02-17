using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SteamRetriever;

internal enum NtfyPriority
{
    [EnumMember(Value = "min")]
    Min = 1,
    [EnumMember(Value = "low")]
    Low = 2,
    [EnumMember(Value = "default")]
    Default = 3,
    [EnumMember(Value = "high")]
    High = 4,
    [EnumMember(Value = "max")]
    Max = 5
}

internal class NtfyParameters
{
    public Uri ServerEndpoint { get; set; }
    public NtfyPriority Priority { get; set; }
    public string Authorization { get; set; }
    public bool Markdown { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
}

internal class NtfyService
{
    internal async Task NotifyAsync(NtfyParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters?.ServerEndpoint?.ToString()))
            return;

        using var httpClient = new HttpClient();

        if (!string.IsNullOrWhiteSpace(parameters.Authorization))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", parameters.Authorization);

        if (parameters.Markdown)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Markdown", "yes");

        if (!string.IsNullOrWhiteSpace(parameters.Title))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Title", parameters.Title);

        if (parameters.Priority != NtfyPriority.Default)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Priority", parameters.Priority.ToString());

        var httpContent = new StringContent(parameters.Message);
        await httpClient.PostAsync(parameters.ServerEndpoint, httpContent);
    }
}
