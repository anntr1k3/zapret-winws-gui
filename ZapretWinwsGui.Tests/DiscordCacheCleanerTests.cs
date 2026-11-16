using ZapretWinwsGui.Core.Discord;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class DiscordCacheCleanerTests
{
    [Fact]
    public void CleanCache_RemovesTargetDirectoriesAndTerminatesProcess()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ZapretDiscordTest_" + Guid.NewGuid().ToString("N"));
        var discordCache = Path.Combine(tempDir, "discord", "Cache");
        var discordCodeCache = Path.Combine(tempDir, "discord", "Code Cache");

        Directory.CreateDirectory(discordCache);
        Directory.CreateDirectory(discordCodeCache);

        File.WriteAllText(Path.Combine(discordCache, "data_0"), "dummy cache content");
        File.WriteAllText(Path.Combine(discordCodeCache, "code_0"), "dummy code cache content");

        try
        {
            var runner = new MockProcessRunner();
            runner.RunningProcessNames.Add("Discord");

            var cleaner = new DiscordCacheCleaner(runner, tempDir);
            var result = cleaner.CleanCache();

            Assert.True(result.Success);
            Assert.Equal(1, result.TerminatedProcesses);
            Assert.Equal(2, result.RemovedDirectories);
            Assert.False(Directory.Exists(discordCache));
            Assert.False(Directory.Exists(discordCodeCache));
            Assert.Contains("Discord", runner.KilledProcessNames);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
