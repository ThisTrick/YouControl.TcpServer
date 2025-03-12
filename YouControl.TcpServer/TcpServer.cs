using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;

namespace YouControl.TcpServer;

public class TcpServer
{
    private readonly ILogger _logger;
    private readonly int _port;
    private TcpListener _listener;
    private readonly ConcurrentDictionary<TcpClient, (string Ip, int Sum)> _clients = new();

    public TcpServer(ILogger logger, int port)
    {
        _logger = logger;
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var localhost = await Dns.GetHostEntryAsync("localhost", cancellationToken);
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], _port);
        _listener = new TcpListener(ipEndPoint);

        _listener.Start();
        
        _logger.Info("The server is running on port {port}.", _port);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(async () => await ClientHandleAsync(client, cancellationToken), cancellationToken);
        }
        
        _listener.Stop();
    }
    
    private async Task ClientHandleAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            _clients[client] = (clientIp, 0);

            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            
            await writer.WriteLineAsync("Введіть число або команду 'list':");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var input = await reader.ReadLineAsync(cancellationToken);
                
                _logger.Info("Client {clientIp} said: {input}", clientIp, input);
                
                if (input is null or "exit")
                {
                    await writer.WriteLineAsync("Роботу завершено. Пака");
                    break;
                }
                
                if (input == "list")
                {
                    await writer.WriteLineAsync(GetClientList());
                    continue;
                }
                
                if (int.TryParse(input, out int number))
                {
                    _clients[client] = (clientIp, _clients[client].Sum + number);
                    await writer.WriteLineAsync($"Поточна сума для {clientIp}: {_clients[client].Sum}");
                    continue;
                }
                
                await writer.WriteLineAsync("Помилка! Введіть число або команду 'list'.");
            }
            
            _logger.Info($"Client {clientIp} has disconnected.", clientIp);
            _clients.TryRemove(client, out _);
            client.Close();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling client");
        }
    }
    
    private string GetClientList()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Список підключених клієнтів:");
        foreach (var kvp in _clients)
        {
            stringBuilder.AppendLine($"Ip {kvp.Value.Ip} -> Сума: {kvp.Value.Sum}");
        }
        return stringBuilder.ToString();
    }
    
}