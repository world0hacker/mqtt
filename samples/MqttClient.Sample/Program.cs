using System.Net.MQTT;

Console.WriteLine("MQTT Client Sample");
Console.WriteLine("==================");

var options = new MqttClientOptions
{
    Host = "localhost",
    Port = 1883,
    ClientId = $"sample-client-{Environment.MachineName}-{DateTime.Now.Ticks}",
    CleanSession = true,
    KeepAliveSeconds = 60,
    AutoReconnect = true
};

using var client = new MqttClient(options);

// 事件处理器
client.Connected += (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connected to broker (Session present: {e.Result.SessionPresent})");
};

client.Disconnected += (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Disconnected (Client initiated: {e.ClientInitiated}, Will reconnect: {e.WillReconnect})");
    if (e.Exception != null)
    {
        Console.WriteLine($"  Exception: {e.Exception.Message}");
    }
};

client.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message received:");
    Console.WriteLine($"  Topic: {e.Message.Topic}");
    Console.WriteLine($"  Payload: {e.Message.PayloadAsString}");
    Console.WriteLine($"  QoS: {e.Message.QualityOfService}");
};

// 连接
Console.WriteLine($"Connecting to {options.Host}:{options.Port}...");
var result = await client.ConnectAsync();

if (!result.IsSuccess)
{
    Console.WriteLine($"Connection failed: {result.ResultCode}");
    return;
}

Console.WriteLine("Connected successfully!");

// 订阅主题
Console.WriteLine("\nSubscribing to topics...");
await client.SubscribeAsync("test/#", MqttQualityOfService.AtLeastOnce);
await client.SubscribeAsync("sensors/+/temperature", MqttQualityOfService.AtMostOnce);
Console.WriteLine("Subscribed to: test/#, sensors/+/temperature");

// 处理 Ctrl+C
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("\nCommands:");
Console.WriteLine("  p <topic> <message> - Publish a message");
Console.WriteLine("  s <topic>           - Subscribe to a topic");
Console.WriteLine("  u <topic>           - Unsubscribe from a topic");
Console.WriteLine("  q                   - Quit");
Console.WriteLine();

// 主循环
while (!cts.Token.IsCancellationRequested)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    var parts = input.Split(' ', 3);
    var command = parts[0].ToLowerInvariant();

    try
    {
        switch (command)
        {
            case "p" when parts.Length >= 3:
                await client.PublishAsync(parts[1], parts[2], MqttQualityOfService.AtLeastOnce);
                Console.WriteLine($"Published to {parts[1]}");
                break;

            case "s" when parts.Length >= 2:
                await client.SubscribeAsync(parts[1]);
                Console.WriteLine($"Subscribed to {parts[1]}");
                break;

            case "u" when parts.Length >= 2:
                await client.UnsubscribeAsync(parts[1]);
                Console.WriteLine($"Unsubscribed from {parts[1]}");
                break;

            case "q":
                cts.Cancel();
                break;

            default:
                Console.WriteLine("Invalid command. Use: p <topic> <message>, s <topic>, u <topic>, or q");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

Console.WriteLine("\nDisconnecting...");
await client.DisconnectAsync();
Console.WriteLine("Disconnected.");
