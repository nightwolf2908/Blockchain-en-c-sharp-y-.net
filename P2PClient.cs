using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class P2PClient
{
    private readonly Blockchain _blockchain;
    private ClientWebSocket? _clientSocket;

    public P2PClient(Blockchain blockchain)
    {
        _blockchain = blockchain;
    }

    public async Task ConnectToPeer(string url)
    {
        try
        {
            _clientSocket = new ClientWebSocket();
            await _clientSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            
            Console.WriteLine($"[P2P] ✅ Conectado al nodo {url}");

            // Solicitar la blockchain completa
            var requestMessage = JsonSerializer.Serialize(new { Type = "QUERY_ALL" });
            var bytes = Encoding.UTF8.GetBytes(requestMessage);
            await _clientSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            Console.WriteLine("[P2P] 📤 Solicitando blockchain al nodo...");

            // Escuchar mensajes
            var buffer = new byte[1024 * 4];
            while (_clientSocket.State == WebSocketState.Open)
            {
                var result = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    P2PHandler.ProcessMessage(message, _blockchain, _clientSocket);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] ❌ Error al conectar con el nodo {url}: {ex.Message}");
        }
    }

    public async Task Disconnect()
    {
        if (_clientSocket != null && _clientSocket.State == WebSocketState.Open)
        {
            await _clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
        }
    }
}