using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace codecrafters_http_server.src;

public partial class RequestHandler(IResponseComposer responseSelector)
{
    public const string? NewLine = "\r\n";
    
    //language=regex
    private const string UrlRegex = @"^(?<verb>(GET|POST))\s+(?<url>.*?)(?<protocol>\s*HTTP/[\d\.]+)$";

    [GeneratedRegex(pattern: UrlRegex, RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1_000)]
    private static partial Regex UrlRegexGenerated();
    
    //language=regex
    private const string HeaderRegex = @"^(?<name>(.*?)):\s+(?<value>.*?)$";

    [GeneratedRegex(pattern: HeaderRegex, RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1_000)]
    private static partial Regex HeaderRegexGenerated();

    public async Task ProcessRequest(NetworkStream stream)
    {
        var request = await ReadRequest(stream);

        var requestParts = ParseRequest(request);

        var response = responseSelector.GetResponse(requestParts);

        await WriteResponse(stream, response);
    }

    async Task<string> ReadRequest(NetworkStream stream)
    {
        byte[] requestBuffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length);
        string s = Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);
        //Console.WriteLine($"Received request: {s}");
        return s;
    }
    
    Request? ParseRequest(string request)
    {
        var parts = request.Split(NewLine);
        Console.WriteLine($"request first part: {parts[0]}");

        var match = UrlRegexGenerated().Match(parts[0]);
        if (!match.Success)
            return null;

        int i = 1;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (; i < parts.Length; i++)
        {
            var m = HeaderRegexGenerated().Match(parts[i]);
            if (!m.Success) break;

            headers.TryAdd(m.Groups["name"].Value, m.Groups["value"].Value);
        }

        var body = new StringBuilder(Math.Max(parts.Length - i, 1));
        for (; i < parts.Length; i++)
            body.AppendLine(parts[i]);

        return new(match.Groups["verb"].Value, match.Groups["url"].Value, match.Groups["protocol"].Value
            , headers
            , body.ToString().Trim());
    }

    async Task WriteResponse(NetworkStream stream, string response)
    {
        byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
    }
}