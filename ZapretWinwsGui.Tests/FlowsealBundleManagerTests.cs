using System.IO.Compression;
using System.Net;
using System.Text;
using ZapretWinwsGui.Core.Bundling;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class FlowsealBundleManagerTests
{
    [Fact]
    public void CheckBundle_EmptyDirectory_ReturnsIncomplete()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bundle-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var manager = new FlowsealBundleManager();
            var status = manager.CheckBundle(tempDir);

            Assert.False(status.IsComplete);
            Assert.False(status.HasWinws);
            Assert.False(status.HasWinDivert);
            Assert.False(status.HasLists);
            Assert.Equal(0, status.StrategyBatCount);
            Assert.Contains("Отсутствуют компоненты", status.Details);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CheckBundle_CompleteDirectory_ReturnsComplete()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bundle-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "bin"));
        Directory.CreateDirectory(Path.Combine(tempDir, "lists"));

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "bin", "winws.exe"), "dummy exe");
            File.WriteAllText(Path.Combine(tempDir, "bin", "WinDivert.dll"), "dummy dll");
            File.WriteAllText(Path.Combine(tempDir, "bin", "WinDivert64.sys"), "dummy sys");
            File.WriteAllText(Path.Combine(tempDir, "lists", "list-general.txt"), "google.com");
            File.WriteAllText(Path.Combine(tempDir, "general.bat"), "@echo off");

            var manager = new FlowsealBundleManager();
            var status = manager.CheckBundle(tempDir);

            Assert.True(status.IsComplete);
            Assert.True(status.HasWinws);
            Assert.True(status.HasWinDivert);
            Assert.True(status.HasLists);
            Assert.Equal(1, status.StrategyBatCount);
            Assert.Contains("Компоненты ядра готовы", status.Details);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAndDeployAsync_WithMockZip_ExtractsSuccessfully()
    {
        // 1. Create a mock zip in memory
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var winwsEntry = archive.CreateEntry("flowseal-repo/bin/winws.exe");
            using (var writer = new StreamWriter(winwsEntry.Open()))
                writer.Write("mock winws binary");

            var dllEntry = archive.CreateEntry("flowseal-repo/bin/WinDivert.dll");
            using (var writer = new StreamWriter(dllEntry.Open()))
                writer.Write("mock dll binary");

            var listEntry = archive.CreateEntry("flowseal-repo/lists/list-general.txt");
            using (var writer = new StreamWriter(listEntry.Open()))
                writer.Write("youtube.com");

            var batEntry = archive.CreateEntry("flowseal-repo/general.bat");
            using (var writer = new StreamWriter(batEntry.Open()))
                writer.Write("start winws.exe");
        }

        memoryStream.Position = 0;
        var zipBytes = memoryStream.ToArray();

        // 2. Mock HttpMessageHandler
        var mockHandler = new MockHttpMessageHandler(zipBytes);
        var httpClient = new HttpClient(mockHandler);

        var manager = new FlowsealBundleManager(httpClient);
        var targetDir = Path.Combine(Path.GetTempPath(), $"bundle-deploy-{Guid.NewGuid():N}");

        try
        {
            var reports = new List<string>();
            var progress = new Progress<string>(reports.Add);

            var success = await manager.DownloadAndDeployAsync(targetDir, "https://mock.test/repo.zip", progress);

            Assert.True(success);
            Assert.True(File.Exists(Path.Combine(targetDir, "bin", "winws.exe")));
            Assert.True(File.Exists(Path.Combine(targetDir, "bin", "WinDivert.dll")));
            Assert.True(File.Exists(Path.Combine(targetDir, "lists", "list-general.txt")));
            Assert.True(File.Exists(Path.Combine(targetDir, "general.bat")));
        }
        finally
        {
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, recursive: true);
        }
    }

    private sealed class MockHttpMessageHandler(byte[] responseBytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(responseBytes)
            };
            return Task.FromResult(response);
        }
    }
}
