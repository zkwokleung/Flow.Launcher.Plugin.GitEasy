using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.GitEasy.Models;

public enum OpenOption
{
    None = 0,
    VSCode,
    FileExplorer,
    Cursor,
}

public sealed class Settings
{
    private List<string> _reposPaths = new();

    public string GitPath { get; set; } = string.Empty;

    public OpenOption OpenReposIn { get; set; } = OpenOption.None;

    public List<string> ReposPaths
    {
        get => _reposPaths;
        set
        {
            ReposPathsWasNull = value == null;
            _reposPaths = value ?? new List<string>();
            ReposPathsWasDeserialized = true;
        }
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = new();

    [JsonIgnore]
    internal bool ReposPathsWasDeserialized { get; private set; }

    [JsonIgnore]
    internal bool ReposPathsWasNull { get; private set; }

    internal bool TryConsumeLegacyRepositoryPath(out string repositoryPath)
    {
        repositoryPath = string.Empty;
        string legacyKey = null;

        foreach (string key in AdditionalData.Keys)
        {
            if (string.Equals(key, "ReposPath", StringComparison.OrdinalIgnoreCase))
            {
                legacyKey = key;
                break;
            }
        }

        if (legacyKey == null)
        {
            return false;
        }

        JsonElement legacyValue = AdditionalData[legacyKey];
        AdditionalData.Remove(legacyKey);

        if (legacyValue.ValueKind == JsonValueKind.String)
        {
            repositoryPath = legacyValue.GetString() ?? string.Empty;
        }

        return true;
    }
}
