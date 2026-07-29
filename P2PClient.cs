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
            
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[P2P] Error al conectar con el nodo {url}: {ex.Message}");
        }
    }
}