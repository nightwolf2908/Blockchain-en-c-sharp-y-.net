using System.Net.WebSockets;
using System.Text;

public class P2PClient
{
    private readonly Blockchain _blockchain;
    public P2PClient(Blockchain blockchain)
    {
        _blockchain = blockchain;
    }

    public async Task ConnectToPeer(string url)
    {
        try
        {
            using var clientSocket = new ClientWebSocket();
            await clientSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            Console.WriteLine($"[P2P] Conectado al nodo {url}");

            typeof(P2PServer).GetField("_sockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, clientSocket);

            var requestMessage = System.Text.Json.JsonSerializer.Serialize(new{Type = "QUERY_ALL"});
            var bytes = Encoding.UTF8.GetBytes(requestMessage);
            await clientSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            var buffer = new byte[1024*4];
            while(clientSocket.State == WebSocketState.Open)
            {
                var result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if(result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    P2PHandler.ProcessMessage(message, _blockchain, clientSocket);
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[P2P] Error al conectar con el nodo {url}: {ex.Message}");
        }
    }
}