using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class P2PServer
{
    private readonly Blockchain _blockchain;
    private static readonly List<WebSocket> _sockets = new();
    private static readonly Dictionary<WebSocket, string> _nodeIdentifiers = new();
    private static readonly object _socketLock = new object();

    public P2PServer(Blockchain blockchain)
    {
        _blockchain = blockchain;
    }

    public async Task HandleConnection(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            
            lock (_socketLock)
            {
                _sockets.Add(webSocket);
                var nodeId = $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}";
                _nodeIdentifiers[webSocket] = nodeId;
            }
            
            Console.WriteLine($"[P2P] 🔗 Nuevo nodo conectado: {_nodeIdentifiers[webSocket]}");

            await Listen(webSocket);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    public async Task Listen(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[P2P] 📨 Mensaje recibido de {_nodeIdentifiers[socket]}");
                    
                    // Procesar el mensaje
                    P2PHandler.ProcessMessage(message, _blockchain, socket);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseConnection(socket);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] ❌ Error en Listen: {ex.Message}");
            await CloseConnection(socket);
        }
    }

    private async Task CloseConnection(WebSocket socket)
    {
        lock (_socketLock)
        {
            _sockets.Remove(socket);
            _nodeIdentifiers.Remove(socket);
        }
        
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }
        catch { }
        
        Console.WriteLine("[P2P] 🔌 Nodo desconectado.");
    }

    public async Task Broadcast(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var disconnectedSockets = new List<WebSocket>();

        lock (_socketLock)
        {
            foreach (var socket in _sockets.ToList())
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        disconnectedSockets.Add(socket);
                    }
                }
                else
                {
                    disconnectedSockets.Add(socket);
                }
            }

            foreach (var socket in disconnectedSockets)
            {
                _sockets.Remove(socket);
                _nodeIdentifiers.Remove(socket);
            }
        }
    }

    public static List<WebSocket> GetConnectedSockets()
    {
        lock (_socketLock)
        {
            return _sockets.ToList();
        }
    }

    public static Dictionary<WebSocket, string> GetNodeIdentifiers()
    {
        lock (_socketLock)
        {
            return _nodeIdentifiers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public int GetConnectedSocketsCount()
    {
        lock (_socketLock)
        {
            return _sockets.Count;
        }
    }
}