using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class P2PHandler
{
    public static void ProcessMessage(string messageJson, Blockchain blockchain, WebSocket socket)
    {
        try{
            var doc = JsonDocument.Parse(messageJson);
        string type = doc.RootElement.GetProperty("Type").GetString() ?? string.Empty;

        if(type == "QUERY_ALL")
        {
            var response = JsonSerializer.Serialize(new {Type="RESPONSE_CHAIN", Data=blockchain});
            var bytes = Encoding.UTF8.GetBytes(response);
            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else if(type == "RESPONSE_CHAIN")
        {
            // Recibimos la blockchain de otro nodo. Aquí aplicarías tu método 'IsValid' 
                // para ver si su cadena es más larga y válida que la nuestra.
            var rawData = doc.RootElement.GetProperty("Data").GetRawText();
            var incomingBlockchain = JsonSerializer.Deserialize<Blockchain>(rawData);

            if(incomingBlockchain != null)
                {
                    if(incomingBlockchain.Chain.Count > blockchain.Chain.Count && incomingBlockchain.IsValid())
                    {
                        Console.WriteLine("[P2P] Consenso alcanzado: Reemplazando cadena local por una más larga y válida.");
                        blockchain.Chain = incomingBlockchain.Chain;
                        blockchain.PendingTransactions = incomingBlockchain.PendingTransactions;
                    }
                    else
                    {
                        Console.WriteLine("[P2P] Cadena recibida no es más larga o no es válida. No se reemplaza la cadena local.");
                    }
                }
            Console.WriteLine("[P2P] Cadena de bloques recibida del nodo.");
            // TODO: Lógica para reemplazar la cadena si es válida y más larga
        }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[P2P] Error al procesar el mensaje: {ex.Message}");
        }
    }
}