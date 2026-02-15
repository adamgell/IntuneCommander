using System.Text.Json.Serialization;

namespace IntuneManager.Core.Models;

public class ProfileStore
{
    [JsonPropertyName("profiles")]
    public List<TenantProfile> Profiles { get; set; } = [];

    [JsonPropertyName("activeProfileId")]
    public string? ActiveProfileId { get; set; }
}
