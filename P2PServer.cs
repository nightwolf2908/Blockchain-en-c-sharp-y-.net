using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

/*
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
*/



public class P2PServer{
    private static readonly List<WebSocket> _sockets = new();
    private static readonly object _socketLock = new();
    private static readonly Dictionary<WebSocket, string> _nodeIdentifiers = new();
    private readonly List<string> _knownPeers = new();

    public void AddKnownPeer(string peerAddress)
    {
        _knownPeers.Add(peerAddress);
    }

    // Conectarse a todos los peers conocidos
    public async Task ConnectToKnownPeers()
    {
        foreach (var peerAddress in _knownPeers)
        {
            try
            {
                Console.WriteLine($"[P2P] 🔄 Conectando a {peerAddress}...");
                await ConnectToPeer(peerAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[P2P] ❌ Error conectando a {peerAddress}: {ex.Message}");
            }
        }
    }

    // Conectarse a un peer específico
    public async Task ConnectToPeer(string peerAddress)
    {
        try
        {
            // Verificar si ya estamos conectados a este peer
            if (_nodeIdentifiers.Values.Any(id => id == peerAddress))
            {
                Console.WriteLine($"[P2P] ⏭️ Ya conectado a {peerAddress}");
                return;
            }
            
            // Crear cliente WebSocket
            using var client = new ClientWebSocket();
            var uri = new Uri($"ws://{peerAddress}/ws");
            
            // Conectar
            await client.ConnectAsync(uri, CancellationToken.None);
            
            // Agregar a la lista de peers
            lock (_socketLock)
            {
                _sockets.Add(client);
                _nodeIdentifiers[client] = peerAddress;
            }
            
            Console.WriteLine($"[P2P] ✅ Conectado exitosamente a {peerAddress}");
            
            // Iniciar escucha de mensajes de este peer
            _ = Listen(client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] ❌ Falló conexión a {peerAddress}: {ex.Message}");
            throw;
        }
    }

    // Método Listen existente (debe manejar WebSocket)
    private async Task Listen(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"[P2P] 🔌 Nodo desconectado: {_nodeIdentifiers[webSocket]}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
                
                // Procesar mensaje
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[P2P] 📨 Mensaje recibido de {_nodeIdentifiers[webSocket]}: {message}");
                
                // Broadcast a otros peers (opcional)
                //await BroadcastMessage(message, webSocket);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] ❌ Error en Listen: {ex.Message}");
        }
        finally
        {
            // Remover socket desconectado
            lock (_socketLock)
            {
                _sockets.Remove(webSocket);
                _nodeIdentifiers.Remove(webSocket);
            }
            
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                try { await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); }
                catch { }
            }
            webSocket.Dispose();
        }
    }

    // Broadcast a todos los peers excepto el remitente
    public async Task BroadcastMessage(string message, WebSocket sender = null)
    {
        var socketsCopy = new List<WebSocket>();
        lock (_socketLock)
        {
            socketsCopy.AddRange(_sockets);
        }
        
        foreach (var socket in socketsCopy)
        {
            if (socket == sender) continue;
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[P2P] ❌ Error enviando mensaje a {_nodeIdentifiers[socket]}: {ex.Message}");
                }
            }
        }
    }

    // HandleConnection (tu código existente, ligeramente modificado)
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
}
