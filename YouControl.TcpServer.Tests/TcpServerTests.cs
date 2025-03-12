using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;

namespace YouControl.TcpServer.Tests;

public class TcpServerTests : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private const int TestPort = 6000;

    public TcpServerTests()
    {
        _ = Task.Run(async () =>
        {
            var server = new TcpServer(LogManager.GetCurrentClassLogger(), TestPort);
            await server.StartAsync(_cancellationTokenSource.Token);
        }, _cancellationTokenSource.Token);
        Task.Delay(500).Wait();
    }

    private async Task<string> SendMessageAsync(string message)
    {
        using TcpClient client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        await writer.WriteLineAsync(message);
        return await reader.ReadLineAsync();
    }

    [Fact]
    public async Task TestServerConnection()
    {
        using TcpClient client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("10");
        
        var response = await reader.ReadLineAsync();
        Assert.Contains("Поточна сума", response);

        client.Close();
    }

    [Fact]
    public async Task TestSumCalculation()
    {
        using TcpClient client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("5");
        var response1 = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("10");
        var response2 = await reader.ReadLineAsync();
        Assert.Contains("5", response1);
        Assert.Contains("15", response2);

        client.Close();
    }

    [Fact]
    public async Task TestInvalidInput()
    {
        using TcpClient client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("error");
        
        var response = await reader.ReadLineAsync();
        Assert.Contains("Помилка", response);

        client.Close();
    }

    [Fact]
    public async Task TestListCommand()
    {
        using TcpClient client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("list");
        
        var response = await reader.ReadLineAsync();
        Assert.Contains("Список підключених клієнтів", response);

        client.Close();
    }
    
    [Fact]
    public async Task Test2UsersCommand()
    {
        using var client = new TcpClient();
        var localhost = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint = new IPEndPoint(localhost.AddressList[0], TestPort);
        
        await client.ConnectAsync(ipEndPoint);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader.ReadLineAsync();
        
        await writer.WriteLineAsync("10");
        
        var response = await reader.ReadLineAsync();
        
        using var client2 = new TcpClient();
        var localhost2 = await Dns.GetHostEntryAsync("localhost");
        var ipEndPoint2 = new IPEndPoint(localhost2.AddressList[0], TestPort);
        
        await client2.ConnectAsync(ipEndPoint2);

        await using var stream2 = client2.GetStream();
        using var reader2 = new StreamReader(stream2, Encoding.UTF8);
        await using var writer2 = new StreamWriter(stream2, Encoding.UTF8) { AutoFlush = true };
        
        _ = await reader2.ReadLineAsync();
        
        await writer2.WriteLineAsync("2");
        
        var response2 = await reader2.ReadLineAsync();
        
        Assert.Contains("10", response);
        Assert.Contains("2", response2);

        client.Close();
        client2.Close();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }
}