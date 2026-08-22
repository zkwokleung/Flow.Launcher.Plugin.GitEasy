using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

internal enum CloneQueryStatus
{
    Hint,
    NoRepository,
    Invalid,
    Valid
}

internal sealed record CloneQueryResult(
    CloneQueryStatus Status,
    string Repository,
    string RepositoryName,
    IReadOnlyList<string> Arguments);

internal static class CloneQueryParser
{
    public static CloneQueryResult Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return CreateResult(CloneQueryStatus.Hint);
        }

        List<string> terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        List<string> repositories = terms
            .Where(term => TryExtractRepositoryName(term, out _))
            .ToList();

        if (repositories.Count == 0)
        {
            return CreateResult(CloneQueryStatus.NoRepository);
        }

        if (repositories.Count != 1)
        {
            return CreateResult(CloneQueryStatus.Invalid);
        }

        string repository = repositories[0];
        terms.RemoveAt(terms.IndexOf(repository));

        if (!TryParseArguments(terms, out IReadOnlyList<string> arguments)
            || !TryExtractRepositoryName(repository, out string repositoryName))
        {
            return CreateResult(CloneQueryStatus.Invalid);
        }

        return new CloneQueryResult(
            CloneQueryStatus.Valid,
            repository,
            repositoryName,
            arguments);
    }

    private static CloneQueryResult CreateResult(CloneQueryStatus status)
    {
        return new CloneQueryResult(
            status,
            string.Empty,
            string.Empty,
            Array.Empty<string>());
    }

    private static bool TryParseArguments(
        IReadOnlyList<string> terms,
        out IReadOnlyList<string> arguments)
    {
        List<string> parsedArguments = new();

        for (int index = 0; index < terms.Count; index++)
        {
            string term = terms[index];

            switch (term)
            {
                case "--branch":
                case "-b":
                    if (++index >= terms.Count
                        || string.IsNullOrWhiteSpace(terms[index])
                        || terms[index].StartsWith("-", StringComparison.Ordinal))
                    {
                        arguments = Array.Empty<string>();
                        return false;
                    }

                    parsedArguments.Add(term);
                    parsedArguments.Add(terms[index]);
                    break;

                case "--depth":
                    if (++index >= terms.Count
                        || !int.TryParse(terms[index], out int depth)
                        || depth <= 0)
                    {
                        arguments = Array.Empty<string>();
                        return false;
                    }

                    parsedArguments.Add(term);
                    parsedArguments.Add(terms[index]);
                    break;

                case "--recurse-submodules":
                case "--single-branch":
                    parsedArguments.Add(term);
                    break;

                default:
                    arguments = Array.Empty<string>();
                    return false;
            }
        }

        arguments = parsedArguments.ToArray();
        return true;
    }

    private static bool TryExtractRepositoryName(string repositoryUrl, out string repositoryName)
    {
        string repositoryPath;

        if (Uri.TryCreate(repositoryUrl, UriKind.Absolute, out Uri repositoryUri)
            && repositoryUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            repositoryPath = Uri.UnescapeDataString(repositoryUri.AbsolutePath);
        }
        else
        {
            int separatorIndex = repositoryUrl.IndexOf(':');

            if (!repositoryUrl.StartsWith("git@", StringComparison.Ordinal)
                || separatorIndex <= "git@".Length
                || separatorIndex == repositoryUrl.Length - 1)
            {
                repositoryName = string.Empty;
                return false;
            }

            string host = repositoryUrl["git@".Length..separatorIndex];
            if (host.Any(char.IsWhiteSpace))
            {
                repositoryName = string.Empty;
                return false;
            }

            repositoryPath = repositoryUrl[(separatorIndex + 1)..];
        }

        repositoryPath = repositoryPath.TrimEnd('/');
        int lastSeparatorIndex = repositoryPath.LastIndexOf('/');
        repositoryName = repositoryPath[(lastSeparatorIndex + 1)..];

        if (repositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repositoryName = repositoryName[..^4];
        }

        return !string.IsNullOrWhiteSpace(repositoryName)
            && repositoryName is not "." and not ".."
            && repositoryName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}