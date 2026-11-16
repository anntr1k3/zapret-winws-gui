using System.Net;
using ZapretWinwsGui.Core.Tester;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class StrategyTesterTests
{
    private sealed class MockHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                RequestMessage = request
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task TestEndpointsAsync_SuccessHttpResponses_AllReportSuccess()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var tester = new StrategyTester(httpClient);

        var reported = new List<ProbeResult>();
        var progress = new Progress<ProbeResult>(reported.Add);

        var results = await tester.TestEndpointsAsync(progress);

        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Equal(200, r.StatusCode));
        Assert.Contains(results, r => r.TargetName == "YouTube Web");
        Assert.Contains(results, r => r.TargetName == "GoogleVideo CDN");
        Assert.Contains(results, r => r.TargetName == "Discord Gateway");
    }

    [Fact]
    public async Task TestEndpointsAsync_ServerFailureResponse_ReportsFailure()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var tester = new StrategyTester(httpClient);

        var results = await tester.TestEndpointsAsync();

        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        Assert.All(results, r => Assert.Equal(500, r.StatusCode));
    }
}
