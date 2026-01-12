using System.Net.MQTT.Broker;
using System.Net.MQTT.Broker.Bridge;

Console.WriteLine("=== 桥接测试 - 子 Broker ===");
Console.WriteLine("端口: 2883");
Console.WriteLine("桥接到: 127.0.0.1:1883 (父 Broker)");
Console.WriteLine();

var broker = new MqttBroker(new MqttBrokerOptions { Port = 2883 });

// 配置桥接
var bridge = broker.AddBridge(new MqttBridgeOptions
{
    Name = "parent-bridge",
    RemoteHost = "127.0.0.1",
    RemotePort = 1883,
    ClientId = "bridge-child-" + Environment.MachineName,

    // 上行规则：本地 sensor/# 和 device/# 消息同步到父 Broker（保持主题不变）
    UpstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "sensor/#",
            Enabled = true
        },
        new MqttBridgeRule
        {
            LocalTopicFilter = "device/#",
            Enabled = true
        }
    },

    // 下行规则：父 Broker 的 commands/# 消息同步到本地
    DownstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "commands/#",
            Enabled = true
        },
        new MqttBridgeRule
        {
            LocalTopicFilter = "config/#",
            Enabled = true
        }
    },

    ReconnectDelayMs = 3000,
    KeepAliveSeconds = 60
});

// 监听桥接事件
bridge.Connected += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [桥接] 已连接到父 Broker ({e.RemoteEndpoint})");
};

bridge.Disconnected += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [桥接] 与父 Broker 断开连接 (重连: {e.WillReconnect})");
};

bridge.MessageForwarded += (s, e) =>
{
    var direction = e.Direction == BridgeDirection.Upstream ? "上行" : "下行";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [桥接] {direction}转发: {e.OriginalTopic} -> {e.TransformedTopic}");
};

// 监听 Broker 事件
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

await broker.StartAsync();
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 子 Broker 已启动，监听端口 2883");
Console.WriteLine();
Console.WriteLine("桥接规则:");
Console.WriteLine("  上行: sensor/#, device/# -> 父 Broker (主题保持不变)");
Console.WriteLine("  下行: commands/#, config/# -> 本地");
Console.WriteLine();
Console.WriteLine("测试方法:");
Console.WriteLine("  1. 用 MQTT 客户端连接到 127.0.0.1:2883");
Console.WriteLine("  2. 发布消息到 sensor/temperature");
Console.WriteLine("  3. 查看父 Broker 是否收到 sensor/temperature");
Console.WriteLine();
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

// 显示统计
Console.WriteLine();
var stats = bridge.GetStatistics();
Console.WriteLine("桥接统计:");
Console.WriteLine($"  上行消息: {stats.UpstreamMessageCount} ({stats.UpstreamByteCount} 字节)");
Console.WriteLine($"  下行消息: {stats.DownstreamMessageCount} ({stats.DownstreamByteCount} 字节)");
Console.WriteLine($"  重连次数: {stats.ReconnectCount}");

Console.WriteLine("\n正在停止...");
await broker.StopAsync();
Console.WriteLine("子 Broker 已停止");
