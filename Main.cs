using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        int port = args.Length > 0 ? int.Parse(args[0]) : 5000;

        // Crear AuthService que maneja blockchain y p2pserver
        var authService = new AuthService(port);
        
        // Iniciar servidor P2P en segundo plano
        var p2pServer = authService.GetP2PServer();
        _ = Task.Run(() => StartP2PServer(p2pServer, port));

        // Esperar un momento para que el servidor inicie
        await Task.Delay(1000);

        // Iniciar autenticación
        authService.autenticacion();

        // Guardar al salir
        authService.GetBlockchain().SaveToFile();
    
    }

    static async Task StartP2PServer(P2PServer p2pserver, int port, List<string> initialPeers = null)
{
    try
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Error);

        var app = builder.Build();
        app.UseWebSockets();
        
        app.Map("/ws", async context =>
        {
            await p2pserver.HandleConnection(context);
        });
        
        // ¡CRUCIAL! Escuchar en todas las interfaces para conexiones entrantes
        // y también en localhost para conexiones locales
        app.Urls.Clear();
        app.Urls.Add($"http://0.0.0.0:{port}");
        app.Urls.Add($"http://localhost:{port}");
        
        Console.WriteLine($"[P2P] 🚀 Servidor iniciado en puerto {port}");
        Console.WriteLine($"[P2P] 📡 Escuchando en:");
        Console.WriteLine($"   - ws://0.0.0.0:{port}/ws (red)");
        Console.WriteLine($"   - ws://localhost:{port}/ws (local)");
        
        // Mostrar IPs locales
        var localIPs = GetLocalIPAddresses();
        foreach (var ip in localIPs)
        {
            Console.WriteLine($"   - ws://{ip}:{port}/ws");
        }
        
        // Conectarse a peers iniciales si existen
        if (initialPeers != null && initialPeers.Any())
        {
            foreach (var peer in initialPeers)
            {
                p2pserver.AddKnownPeer(peer);
            }
            
            // Esperar un poco para que el servidor esté listo
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Dar tiempo al servidor
                await p2pserver.ConnectToKnownPeers();
            });
        }
        
        await app.RunAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al iniciar el servidor P2P: {ex.Message}");
    }
}

static List<string> GetLocalIPAddresses()
{
    var ips = new List<string>();
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            ips.Add(ip.ToString());
        }
    }
    return ips;
}
}