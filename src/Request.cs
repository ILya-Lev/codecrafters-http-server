namespace codecrafters_http_server.src;

public record struct Request(
    string Verb,
    string Url,
    string Protocol,
    Dictionary<string, string> Headers,
    string? Body);
