using codecrafters_http_server.src;
using System.Net;
using System.Net.Sockets;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");
//foreach (var arg in args)
//{
//    Console.WriteLine(arg);
//}

var directory = args.Length > 1 ? args[1] : "";

var responseSelector = new ResponseSelector(new IResponseComposer[]
{
    new EchoResponseComposer(),
    new UserAgentResponseComposer(),
    new GetFileResponseComposer(directory),
    new PostFileResponseComposer(directory),
    new EmptyResponseComposer()
});
var requestHandler = new RequestHandler(responseSelector);

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    using TcpClient client = await server.AcceptTcpClientAsync();
    await using NetworkStream stream = client.GetStream();

    await requestHandler.ProcessRequest(stream);
}
