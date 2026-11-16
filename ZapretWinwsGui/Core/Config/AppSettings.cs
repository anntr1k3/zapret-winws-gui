namespace ZapretWinwsGui.Core.Config;

public class AppSettings
{
    public string? LastSelectedStrategyId { get; set; } = "general";
    public bool AutoStartWinwsOnLaunch { get; set; } = false;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
}
