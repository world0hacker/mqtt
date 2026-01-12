using System.Net.MQTT.Broker;

Console.WriteLine("=== 桥接测试 - 父 Broker ===");
Console.WriteLine("端口: 1883");
Console.WriteLine();

var broker = new MqttBroker(new MqttBrokerOptions { Port = 1883 });

// 监听事件
broker.ClientConnected += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端连接: {e.Session.ClientId}");
};

broker.ClientDisconnected += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端断开: {e.Session.ClientId}");
};

broker.MessagePublished += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
};

broker.ClientSubscribed += (s, e) =>
{
    foreach (var sub in e.Subscriptions)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端订阅: {e.Session.ClientId} -> {sub.Topic}");
    }
};

await broker.StartAsync();
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 父 Broker 已启动，监听端口 1883");
Console.WriteLine();
Console.WriteLine("等待子 Broker 桥接连接...");
Console.WriteLine("按 Ctrl+C 停止");

// 等待退出
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
}

Console.WriteLine("\n正在停止...");
await broker.StopAsync();
Console.WriteLine("父 Broker 已停止");
