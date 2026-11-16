using System.Text;
using System.Text.RegularExpressions;

namespace ZapretWinwsGui.Core.Strategies;

public class StrategyParser
{
    private static readonly Regex WinwsStartRegex = new(
        @"(?:start\s+(?:""[^""]*""\s+)?(?:\/[a-zA-Z]+\s+)*)?[""']?(?:%BIN%|%~dp0bin\\|bin\\)?winws(?:\.exe)?[""']?\s+(.*)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Parses a single strategy from the raw content of a Flowseal .bat file.
    /// </summary>
    public StrategyModel? ParseContent(string fileName, string content, string basePath = "")
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Unfold multi-line caret continuations in Windows batch files
        var unfolded = UnfoldBatchLines(content);

        // Find the line that actually runs winws.exe
        string rawArgs = string.Empty;
        using var reader = new StringReader(unfolded);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("::") || trimmed.StartsWith("rem", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = WinwsStartRegex.Match(trimmed);
            if (match.Success)
            {
                rawArgs = match.Groups[1].Value.Trim();
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(rawArgs))
            return null;

        // Normalize Windows batch variables into concrete paths / values
        var normalizedArgs = NormalizeBatchVariables(rawArgs, basePath);
        var tokens = TokenizeArguments(normalizedArgs);

        var id = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var displayName = FormatDisplayName(id);

        var model = new StrategyModel
        {
            Id = id,
            Name = displayName,
            FileName = Path.GetFileName(fileName),
            RawArguments = normalizedArgs,
            Arguments = tokens,
            Category = CategorizeStrategy(id),
            UsesFakeTls = rawArgs.Contains("--dpi-desync-fake-tls") || rawArgs.Contains("tls_clienthello"),
            UsesSplit = rawArgs.Contains("--dpi-desync=split") || rawArgs.Contains("--dpi-desync=multisplit") || rawArgs.Contains("--dpi-desync-split"),
            UsesDisorder = rawArgs.Contains("--dpi-desync=disorder") || rawArgs.Contains("--dpi-desync=multidisorder"),
            UsesFakeUdp = rawArgs.Contains("--dpi-desync-fake-quic") || rawArgs.Contains("--dpi-desync-fake-discord"),
            HasGameFilter = rawArgs.Contains("%GameFilter", StringComparison.OrdinalIgnoreCase),
            IsRecommended = id == "general" || id == "general_alt"
        };

        return model;
    }

    /// <summary>
    /// Scans a directory for all .bat strategy files and returns parsed strategies.
    /// </summary>
    public List<StrategyModel> ScanDirectory(string directoryPath)
    {
        var result = new List<StrategyModel>();
        if (!Directory.Exists(directoryPath))
            return result;

        var files = Directory.GetFiles(directoryPath, "*.bat");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            // Skip service management and utility scripts
            if (fileName.Equals("service.bat", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("service_", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("check_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var parsed = ParseContent(fileName, content, directoryPath);
                if (parsed != null)
                {
                    result.Add(parsed);
                }
            }
            catch
            {
                // Continue scanning remaining files if one fails
            }
        }

        return result
            .OrderByDescending(s => s.IsRecommended)
            .ThenBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToList();
    }

    private static string UnfoldBatchLines(string content)
    {
        var sb = new StringBuilder();
        using var reader = new StringReader(content);
        string? line;
        bool continuing = false;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.TrimEnd();
            bool hasCaret = trimmed.EndsWith('^');

            if (hasCaret)
            {
                trimmed = trimmed[..^1].TrimEnd();
            }

            if (continuing)
            {
                sb.Append(' ').Append(trimmed);
            }
            else
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(trimmed);
            }

            continuing = hasCaret;
        }

        return sb.ToString();
    }

    private static string NormalizeBatchVariables(string args, string basePath)
    {
        var binPath = Path.Combine(basePath, "bin");
        var listsPath = Path.Combine(basePath, "lists");

        var normalized = args
            .Replace("%BIN%", binPath.EndsWith(Path.DirectorySeparatorChar) ? binPath : binPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("%~dp0bin\\", binPath.EndsWith(Path.DirectorySeparatorChar) ? binPath : binPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("%LISTS%", listsPath.EndsWith(Path.DirectorySeparatorChar) ? listsPath : listsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("%~dp0lists\\", listsPath.EndsWith(Path.DirectorySeparatorChar) ? listsPath : listsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("%~dp0", basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            .Replace("%GameFilterTCP%", "", StringComparison.OrdinalIgnoreCase)
            .Replace("%GameFilterUDP%", "", StringComparison.OrdinalIgnoreCase);

        // Clean up trailing commas or double commas from removed empty GameFilter
        normalized = Regex.Replace(normalized, @",\s*,+", ",");
        normalized = Regex.Replace(normalized, @",\s*--", " --");
        normalized = Regex.Replace(normalized, @",\s*$", "");

        return normalized.Trim();
    }

    private static List<string> TokenizeArguments(string commandLine)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static StrategyCategory CategorizeStrategy(string id)
    {
        if (id.StartsWith("discord"))
            return StrategyCategory.Discord;
        if (id.StartsWith("general"))
            return StrategyCategory.General;
        return StrategyCategory.Custom;
    }

    private static string FormatDisplayName(string id)
    {
        return id switch
        {
            "general" => "General (Основная рекомендация)",
            "general_alt" => "General Alt (Альтернативная 1)",
            "general_alt2" => "General Alt 2",
            "general_alt3" => "General Alt 3",
            "general_fake_tls_auto" => "General Fake TLS Auto",
            "general_simple_fake" => "General Simple Fake",
            "discord" => "Discord Only",
            _ => char.ToUpper(id[0]) + id[1..].Replace('_', ' ')
        };
    }
}
