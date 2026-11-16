using ZapretWinwsGui.Core.Config;
using ZapretWinwsGui.ViewModels;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class SettingsManagerTests
{
    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid():N}.json");
        var manager = new SettingsManager(tempFile);

        var settings = manager.Load();

        Assert.NotNull(settings);
        Assert.Equal("general", settings.LastSelectedStrategyId);
        Assert.False(settings.AutoStartWinwsOnLaunch);
        Assert.True(settings.MinimizeToTrayOnClose);
    }

    [Fact]
    public void SaveAndLoad_PersistsCustomValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}.json");

        try
        {
            var manager = new SettingsManager(tempFile);
            var initialSettings = new AppSettings
            {
                LastSelectedStrategyId = "discord",
                AutoStartWinwsOnLaunch = true,
                MinimizeToTrayOnClose = false,
                StartMinimized = true
            };

            var saved = manager.Save(initialSettings);
            Assert.True(saved);
            Assert.True(File.Exists(tempFile));

            var loadedSettings = manager.Load();
            Assert.Equal("discord", loadedSettings.LastSelectedStrategyId);
            Assert.True(loadedSettings.AutoStartWinwsOnLaunch);
            Assert.False(loadedSettings.MinimizeToTrayOnClose);
            Assert.True(loadedSettings.StartMinimized);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsDefaultSettingsGracefully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"corrupt-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ broken json corrupt [!]");

        try
        {
            var manager = new SettingsManager(tempFile);
            var settings = manager.Load();

            Assert.NotNull(settings);
            Assert.Equal("general", settings.LastSelectedStrategyId);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void MainViewModel_RestoresLastSelectedStrategyFromSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"vm-settings-{Guid.NewGuid():N}.json");

        try
        {
            var manager = new SettingsManager(tempFile);
            manager.Save(new AppSettings { LastSelectedStrategyId = "general_alt" });

            var runner = new MockProcessRunner();
            var vm = new MainViewModel(runner: runner, settingsManager: manager);

            Assert.NotNull(vm.SelectedStrategy);
            Assert.Equal("general_alt", vm.SelectedStrategy.Id);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
