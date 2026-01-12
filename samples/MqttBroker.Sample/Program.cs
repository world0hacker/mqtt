using System.Net.MQTT;
using System.Net.MQTT.Broker;

Console.WriteLine("MQTT Broker Sample");
Console.WriteLine("==================");

var options = new MqttBrokerOptions
{
    Port = 1883,
    AllowAnonymous = true,
    EnableRetainedMessages = true,
    EnablePersistentSessions = true,
    MaxConnections = 1000
};

using var broker = new MqttBroker(options);

// 设置认证（可选）
broker.Authenticator = new SimpleAuthenticator()
    .AddUser("admin", "password123")
    .AddUser("user1", "user1pass");

// 事件处理器
broker.ClientConnected += (sender, e) =>
{
   
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client connected: {e.Session.ClientId} from {e.Session.RemoteEndpoint} {e.Session.RemoteEndpoint}");
};
broker.ClientDisconnected += (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client disconnected: {e.Session.ClientId} (Graceful: {e.Graceful}) {e.Session.RemoteEndpoint}");
};
broker.MessagePublishing += Broker_MessagePublishing;

void Broker_MessagePublishing(object? sender, MqttMessagePublishingEventArgs e)
{
    if (e.Message.Topic == "A")
    {
        e.ProcessMessage = false;
    }
}

broker.MessagePublished += (sender, e) =>
{
   // Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message published by {e.Session.ClientId}: {e.Message.Topic} = {e.Message.PayloadAsString}");
};

broker.ClientSubscribed += (sender, e) =>
{
    var topics = string.Join(", ", e.Subscriptions.Select(s => s.Topic));
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client {e.Session.ClientId} subscribed to: {topics}");
};

broker.ClientUnsubscribed += (sender, e) =>
{
    var topics = string.Join(", ", e.Topics);
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Client {e.Session.ClientId} unsubscribed from: {topics}");
};
broker.MessageNotDelivered += (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message not delivered to {e.Session.ClientId}: {e.Message.Topic}");
};
// 启动 Broker
await broker.StartAsync();
Console.WriteLine($"Broker started on port {options.Port}");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// 处理 Ctrl+C
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// 状态显示循环
var statusTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(10000, cts.Token);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connected clients: {broker.ConnectedClients}");
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
});

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
}

Console.WriteLine("\nStopping broker...");
await broker.StopAsync();
Console.WriteLine("Broker stopped.");
