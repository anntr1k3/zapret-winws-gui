using ZapretWinwsGui.Core.Strategies;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class StrategyParserTests
{
    private const string SampleFlowsealBat = @"@echo off
chcp 65001 > nul
set ""BIN=%~dp0bin\""
set ""LISTS=%~dp0lists\""
cd /d %BIN%

start ""zapret: %~n0"" /min ""%BIN%winws.exe"" --wf-tcp=80,443,%GameFilterTCP% --wf-udp=443,%GameFilterUDP% ^
--filter-udp=443 --hostlist=""%LISTS%list-general.txt"" --dpi-desync=fake --dpi-desync-fake-quic=""%BIN%quic_initial_www_google_com.bin"" --new ^
--filter-tcp=80,443 --hostlist=""%LISTS%list-google.txt"" --dpi-desync=multisplit --dpi-desync-split-pos=1 --dpi-desync-split-seqovl-pattern=""%BIN%tls_clienthello_www_google_com.bin""
";

    [Fact]
    public void ParseContent_ValidFlowsealBat_ExtractsStrategyCorrectly()
    {
        var parser = new StrategyParser();
        var strategy = parser.ParseContent("general.bat", SampleFlowsealBat, @"C:\zapret");

        Assert.NotNull(strategy);
        Assert.Equal("general", strategy.Id);
        Assert.Equal("General (Основная рекомендация)", strategy.Name);
        Assert.Equal("general.bat", strategy.FileName);
        Assert.True(strategy.IsRecommended);
        Assert.Equal(StrategyCategory.General, strategy.Category);

        // Feature flags
        Assert.True(strategy.UsesFakeUdp);
        Assert.True(strategy.UsesSplit);
        Assert.True(strategy.UsesFakeTls);
        Assert.True(strategy.HasGameFilter);

        // Arguments tokenized
        Assert.NotEmpty(strategy.Arguments);
        Assert.Contains(strategy.Arguments, a => a.StartsWith("--wf-tcp="));
        Assert.Contains(strategy.Arguments, a => a == "--new");
        Assert.Contains(strategy.Arguments, a => a.StartsWith("--dpi-desync="));
    }

    [Fact]
    public void ParseContent_DiscordBat_CategorizedAsDiscord()
    {
        var parser = new StrategyParser();
        var bat = @"start ""zapret"" /min winws.exe --wf-udp=50000-50100 --dpi-desync=fake";
        var strategy = parser.ParseContent("discord.bat", bat);

        Assert.NotNull(strategy);
        Assert.Equal("discord", strategy.Id);
        Assert.Equal(StrategyCategory.Discord, strategy.Category);
        Assert.False(strategy.UsesSplit);
    }

    [Fact]
    public void ParseContent_EmptyBat_ReturnsNull()
    {
        var parser = new StrategyParser();
        var strategy = parser.ParseContent("empty.bat", "");
        Assert.Null(strategy);
    }
}
