using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        int port = args.Length > 0 ? int.Parse(args[0]) : 5000;
        
        Console.WriteLine($"🚀 Iniciando Nodo Blockchain en puerto {port}");
        Console.WriteLine($"🌐 P2P URL: ws://localhost:{port}/ws");
        Console.WriteLine("========================================\n");

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
        Console.WriteLine("👋 ¡Hasta luego!");
    }

    static async Task StartP2PServer(P2PServer p2pserver, int port)
    {
        try
        {
            var builder = WebApplication.CreateBuilder();

            // Silenciar logs de Microsoft
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Error);

            var app = builder.Build();

            app.UseWebSockets();
            app.Map("/ws", async context =>
            {
                await p2pserver.HandleConnection(context);
            });
            
            await app.RunAsync($"http://localhost:{port}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al iniciar el servidor P2P: {ex.Message}");
        }
    }
}