using System.Diagnostics;
using System.Net.Http.Headers;

namespace ZapretWinwsGui.Core.Tester;

public record ProbeTarget(
    string Name,
    string Url,
    string Category,
    bool RequiresRangeHeader = false
);

public record ProbeResult(
    string TargetName,
    string Category,
    bool Success,
    int StatusCode,
    long LatencyMs,
    string Details
);

public class StrategyTester
{
    private static readonly ProbeTarget[] Targets =
    [
        new("YouTube Web", "https://www.youtube.com/", "YouTube"),
        new("GoogleVideo CDN", "https://redirector.googlevideo.com/videoplayback?id=test", "YouTube", RequiresRangeHeader: true),
        new("Discord Web", "https://discord.com/", "Discord"),
        new("Discord Gateway", "https://gateway.discord.gg/", "Discord")
    ];

    private readonly HttpClient _httpClient;

    public StrategyTester(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            AllowAutoRedirect = true
        })
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
    }

    public async Task<List<ProbeResult>> TestEndpointsAsync(IProgress<ProbeResult>? progress = null, CancellationToken ct = default)
    {
        var results = new List<ProbeResult>();

        foreach (var target in Targets)
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, target.Url);
                if (target.RequiresRangeHeader)
                {
                    request.Headers.Range = new RangeHeaderValue(0, 1024);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                sw.Stop();

                int code = (int)response.StatusCode;
                // For googlevideo range test, 200, 206, 302, 403 are all responses received through DPI (connection established!)
                bool ok = code is >= 200 and < 500;
                var res = new ProbeResult(
                    target.Name,
                    target.Category,
                    ok,
                    code,
                    sw.ElapsedMilliseconds,
                    ok ? $"Ответ получен (HTTP {code})" : $"Ошибка сервера (HTTP {code})"
                );

                results.Add(res);
                progress?.Report(res);
            }
            catch (Exception ex)
            {
                sw.Stop();
                var res = new ProbeResult(
                    target.Name,
                    target.Category,
                    false,
                    0,
                    sw.ElapsedMilliseconds,
                    $"Сбой соединения: {ex.Message}"
                );

                results.Add(res);
                progress?.Report(res);
            }
        }

        return results;
    }
}
