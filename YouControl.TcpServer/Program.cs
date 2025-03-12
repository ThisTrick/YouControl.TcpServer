// See https://aka.ms/new-console-template for more information


using NLog;
using NLog.Config;
using NLog.Targets;
using YouControl.TcpServer;

if (args.Length != 1 || !int.TryParse(args[0], out int port))
{
    Console.WriteLine("Вкажіть порт як число! Наприклад: dotnet run 5000");
    return;
}

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += async (sender, e) =>
{
    await cts.CancelAsync();
};

var config = new LoggingConfiguration();

var consoleTarget = new ColoredConsoleTarget("console")
{
    Layout = "${longdate} [${level}] ${message}"
};
config.AddTarget(consoleTarget);
config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
LogManager.Configuration = config;

var logger =  LogManager.GetCurrentClassLogger();

var server = new TcpServer(logger, port);

await server.StartAsync(cts.Token);
