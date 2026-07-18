using System.Collections.Concurrent;
using System.Net;

namespace Nano.GameEngine;

/// <summary>Small non-blocking HTTP client designed to be polled from the Lua game loop.</summary>
internal sealed class NanoNetworkService : IDisposable
{
    private const int MaxResponseBytes = 2 * 1024 * 1024;
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly ConcurrentDictionary<int, Request> _requests = new();
    private int _nextHandle;

    public int Get(string url)
    {
        var handle = Interlocked.Increment(ref _nextHandle);
        var cancellation = new CancellationTokenSource();
        var request = new Request(cancellation);
        _requests[handle] = request;
        request.Task = DownloadAsync(url, request, cancellation.Token);
        return handle;
    }

    public string Status(int handle) => _requests.TryGetValue(handle, out var request)
        ? request.Status
        : "missing";

    public string Body(int handle) =>
        _requests.TryGetValue(handle, out var request) ? request.Body : string.Empty;

    public string Error(int handle) =>
        _requests.TryGetValue(handle, out var request) ? request.Error : "Request not found.";

    public int Code(int handle) =>
        _requests.TryGetValue(handle, out var request) ? request.Code : 0;

    public void Cancel(int handle)
    {
        if (_requests.TryGetValue(handle, out var request))
            request.Cancellation.Cancel();
    }

    public void Dispose()
    {
        foreach (var request in _requests.Values)
            request.Cancellation.Cancel();
        _client.Dispose();
        foreach (var request in _requests.Values)
            request.Cancellation.Dispose();
    }

    private async Task DownloadAsync(string url, Request request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
                throw new InvalidOperationException("Only http and https URLs are supported.");

            using var response = await _client.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            request.Code = (int)response.StatusCode;
            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > MaxResponseBytes)
                throw new InvalidOperationException("The response is larger than 2 MB.");

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var destination = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;
                if (destination.Length + read > MaxResponseBytes)
                    throw new InvalidOperationException("The response is larger than 2 MB.");
                destination.Write(buffer, 0, read);
            }

            request.Body = System.Text.Encoding.UTF8.GetString(destination.ToArray());
            request.Status = response.StatusCode is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices
                ? "done"
                : "error";
            if (request.Status == "error")
                request.Error = $"HTTP {(int)response.StatusCode}.";
        }
        catch (OperationCanceledException)
        {
            request.Status = "cancelled";
        }
        catch (Exception exception)
        {
            request.Error = exception.Message;
            request.Status = "error";
        }
    }

    private sealed class Request(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Task? Task { get; set; }
        public string Status { get; set; } = "pending";
        public string Body { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int Code { get; set; }
    }
}
