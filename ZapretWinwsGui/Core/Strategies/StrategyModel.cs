namespace ZapretWinwsGui.Core.Strategies;

public enum StrategyCategory
{
    General,
    Discord,
    Custom
}

public class StrategyModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public StrategyCategory Category { get; set; } = StrategyCategory.General;
    public string Description { get; set; } = string.Empty;
    public string RawArguments { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = [];

    // Desync features detected
    public bool UsesFakeTls { get; set; }
    public bool UsesSplit { get; set; }
    public bool UsesDisorder { get; set; }
    public bool UsesFakeUdp { get; set; }
    public bool HasGameFilter { get; set; }
    public bool IsRecommended { get; set; }

    public string Summary =>
        $"{(UsesFakeTls ? "[Fake TLS] " : "")}{(UsesSplit ? "[Split] " : "")}{(UsesDisorder ? "[Disorder] " : "")}{(UsesFakeUdp ? "[Fake UDP] " : "")}".Trim();

    public override string ToString() => Name;
}
