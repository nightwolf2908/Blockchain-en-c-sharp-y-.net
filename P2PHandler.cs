using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class P2PHandler
{
    public static void ProcessMessage(string messageJson, Blockchain blockchain, WebSocket socket)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            string type = doc.RootElement.GetProperty("Type").GetString() ?? string.Empty;

            switch (type)
            {
                case "QUERY_ALL":
                    // Responder con la blockchain completa
                    var response = new
                    {
                        Type = "RESPONSE_CHAIN",
                        Data = new
                        {
                            blockchain.Chain,
                            blockchain.PendingTransactions,
                            blockchain.Difficulty
                        }
                    };
                    var responseJson = JsonSerializer.Serialize(response);
                    var bytes = Encoding.UTF8.GetBytes(responseJson);
                    socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine("[P2P] 📤 Blockchain enviada al nodo solicitante.");
                    break;

                case "RESPONSE_CHAIN":
                    // Recibir blockchain de otro nodo
                    var rawData = doc.RootElement.GetProperty("Data");
                    var incomingChain = JsonSerializer.Deserialize<List<Block>>(rawData.GetProperty("Chain").GetRawText());
                    
                    if (incomingChain != null && incomingChain.Count > blockchain.Chain.Count)
                    {
                        Console.WriteLine($"[P2P] 📥 Recibida blockchain con {incomingChain.Count} bloques (local: {blockchain.Chain.Count})");
                        blockchain.ReplaceChain(incomingChain);
                    }
                    else
                    {
                        Console.WriteLine("[P2P] ℹ️ Cadena recibida no es más larga que la local.");
                    }
                    break;

                case "NEW_BLOCK":
                    // Recibir un nuevo bloque minado
                    var newBlock = JsonSerializer.Deserialize<Block>(doc.RootElement.GetProperty("Data").GetRawText());
                    if (newBlock != null)
                    {
                        Console.WriteLine($"[P2P] 📥 Recibido nuevo bloque #{newBlock.Index}");
                        
                        // Verificar que el bloque sea válido
                        var latestBlock = blockchain.GetLatestBlock();
                        if (newBlock.PreviousHash == latestBlock.Hash && newBlock.Index == latestBlock.Index + 1)
                        {
                            blockchain.Chain.Add(newBlock);
                            blockchain.SaveToFile();
                            Console.WriteLine($"[P2P] ✅ Bloque #{newBlock.Index} agregado a la cadena local.");
                        }
                        else
                        {
                            Console.WriteLine("[P2P] ⚠️ Bloque no válido o fuera de orden. Solicitando cadena completa...");
                            // Solicitar la cadena completa
                            var request = new { Type = "QUERY_ALL" };
                            var requestJson = JsonSerializer.Serialize(request);
                            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                            socket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    break;

                default:
                    Console.WriteLine($"[P2P] ⚠️ Tipo de mensaje desconocido: {type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] ❌ Error al procesar el mensaje: {ex.Message}");
        }
    }
}