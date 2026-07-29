using System.Net.WebSockets;
using System.Text;

public class P2PServer
{
    private readonly Blockchain _blockchain;
    private static readonly List<WebSocket> _sockets = new();

    public P2PServer(Blockchain blockchain)
    {
        _blockchain = blockchain;
    }

    public async Task HandleConnection(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            _sockets.Add(webSocket);
            Console.WriteLine("[P2P] Nuevo nodo conectado (Servidor).");

            await Listen(webSocket);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    public async Task Listen(WebSocket socket)
    {
        var buffer = new byte[1024*4];
        while(socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if(result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer,0,result.Count);
                Console.WriteLine($"[P2P] Mensaje recibido: {message}");

            }
            else if(result.MessageType == WebSocketMessageType.Close)
            {
                _sockets.Remove(socket);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                Console.WriteLine("[P2P] Nodo desconectado (Servidor).");
            }
        }
    }

    public async Task Broadcast(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        foreach(var socket in _sockets.ToList())
        {
            if(socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}