// Fixtures/MockHttpHandler.cs
using System.Net;
using System.Text;

namespace HitBTC.Connector.Tests.Fixtures;

public class MockHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Content)> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    public void SetupResponse(string pathContains, string content, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[pathContains] = (status, content);
    }

    public void SetupResponse(string pathContains, HttpStatusCode status)
    {
        _responses[pathContains] = (status, string.Empty);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        foreach (var (pattern, response) in _responses)
        {
            if (path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(response.Status)
                {
                    Content = new StringContent(response.Content, Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\": \"Not found\"}", Encoding.UTF8, "application/json")
        });
    }

    public void Clear()
    {
        _requests.Clear();
        _responses.Clear();
    }
}