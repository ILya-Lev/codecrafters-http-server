using System.IO.Compression;
using System.Text;

namespace codecrafters_http_server.src;
using static RequestHandler;
public interface IResponseComposer
{
    bool CanResponse(Request? request);
    string GetResponse(Request? request);
}

public class ResponseSelector : IResponseComposer
{
    private readonly IReadOnlyCollection<IResponseComposer> _composers;
    private readonly IResponseComposer _notFound = new NotFoundResponseComposer();

    public ResponseSelector(IEnumerable<IResponseComposer> composers) => _composers = composers.ToArray();

    public bool CanResponse(Request? request) => true;

    public string GetResponse(Request? request)
    {
        var composer = (request is not null
                           ? _composers.FirstOrDefault(c => c.CanResponse(request))
                           : null)
                       ?? _notFound;

        return composer.GetResponse(request);
    }
}

public class NotFoundResponseComposer : IResponseComposer
{
    public bool CanResponse(Request? request) => true;

    public string GetResponse(Request? request) => $"HTTP/1.1 404 Not Found{NewLine}{NewLine}";
}

public class EmptyResponseComposer : IResponseComposer
{
    public bool CanResponse(Request? request) => request!.Value.Url == "/";

    public string GetResponse(Request? request) => $"HTTP/1.1 200 OK{NewLine}{NewLine}";
}

public class EchoResponseComposer : IResponseComposer
{
    private const string SupportedEncoding = "gzip";
    public bool CanResponse(Request? request) => request!.Value.Url.StartsWith("/echo/", StringComparison.OrdinalIgnoreCase);

    public string GetResponse(Request? request)
    {
        var toReflect = request!.Value.Url[6..];

        var shouldEncode =
            request!.Value.Headers.TryGetValue("Accept-Encoding", out var encoding)
            && encoding.Split(",").Select(p => p.Trim())
                .Any(p => p.Equals(SupportedEncoding, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"should encode: {shouldEncode}");
        int? length = null;
        if (shouldEncode)
            (toReflect, length) = Encode(toReflect);

        var parts = new List<string>
        {
            $"HTTP/1.1 200 OK",
            "Content-Type: text/plain",
            $"Content-Length: {length ?? toReflect.Length}",
            "",
            toReflect
        };

        if (shouldEncode)
            parts.Insert(1, $"Content-Encoding: {SupportedEncoding}");

        return string.Join(NewLine, parts);
    }

    private (string, int) Encode(string body)
    {
        Console.WriteLine($"before encoding: {body}");
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        
        using var stream = new MemoryStream();
        using var gzipStream = new GZipStream(stream, CompressionMode.Compress);
        gzipStream.Write(bodyBytes, 0, bodyBytes.Length);

        var encodedBuffer = stream.ToArray();
        //var encoded = Convert.ToBase64String(encodedBuffer);

        //Console.WriteLine($"after encoding: {encoded}");
        //return encoded;
        return (string.Join(" ", encodedBuffer), encodedBuffer.Length);
    }
}

public class UserAgentResponseComposer : IResponseComposer
{
    public bool CanResponse(Request? request) => request!.Value.Url.Equals("/user-agent", StringComparison.OrdinalIgnoreCase);

    public string GetResponse(Request? request)
    {
        var toReflect = request!.Value.Headers.TryGetValue("user-agent", out var userAgent)
            ? userAgent
            : "";//todo: what about 400 ? or CanResponse to be false in such case

        var parts = new[]
        {
            $"HTTP/1.1 200 OK",
            "Content-Type: text/plain",
            $"Content-Length: {toReflect.Length}",
            "",
            toReflect
        };
        return string.Join(NewLine, parts);
    }
}

public class GetFileResponseComposer : IResponseComposer
{
    private readonly string _directory;

    public GetFileResponseComposer(string directory) => _directory = directory;

    public bool CanResponse(Request? request)
    {
        var isGet = request!.Value.Verb.Equals("get", StringComparison.OrdinalIgnoreCase);
        if (!isGet) return false;

        var isFiles = request!.Value.Url.StartsWith("/files/", StringComparison.OrdinalIgnoreCase);
        if (!isFiles) return false;

        if (_directory.StartsWith(@"C:\System", StringComparison.OrdinalIgnoreCase)
            || _directory.StartsWith(@"C:\Windows", StringComparison.OrdinalIgnoreCase))
        {
            //a sort of security reason
            return false;
        }

        var fullName = GetFullName(request!.Value.Url);
        return File.Exists(fullName);
    }

    public string GetResponse(Request? request)
    {
        //todo: what if the file is too big to fit in memory?
        var toReflect = File.ReadAllText(GetFullName(request!.Value.Url));

        var parts = new[]
        {
            $"HTTP/1.1 200 OK",
            "Content-Type: application/octet-stream",
            $"Content-Length: {toReflect.Length}",
            "",
            toReflect
        };

        return string.Join(NewLine, parts);
    }

    private string GetFullName(string url) => Path.Combine(_directory, url[7..]);
}

public class PostFileResponseComposer : IResponseComposer
{
    private readonly string _directory;

    public PostFileResponseComposer(string directory) => _directory = directory;

    public bool CanResponse(Request? request)
    {
        var isPost = request!.Value.Verb.Equals("post", StringComparison.OrdinalIgnoreCase);
        if (!isPost) return false;

        var isFiles = request!.Value.Url.StartsWith("/files/", StringComparison.OrdinalIgnoreCase);
        if (!isFiles) return false;

        if (_directory.StartsWith(@"C:\System", StringComparison.OrdinalIgnoreCase)
            || _directory.StartsWith(@"C:\Windows", StringComparison.OrdinalIgnoreCase))
        {
            //a sort of security reason
            return false;
        }

        return true;
    }

    public string GetResponse(Request? request)
    {
        File.WriteAllText(GetFullName(request!.Value.Url), request!.Value.Body);

        return $"HTTP/1.1 201 Created{NewLine}{NewLine}";
    }

    private string GetFullName(string url) => Path.Combine(_directory, url[7..]);
}