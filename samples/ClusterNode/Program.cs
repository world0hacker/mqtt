using System.Net.MQTT.Broker;
using System.Net.MQTT.Broker.Cluster;

// 解析命令行参数
var nodeId = args.Length > 0 ? args[0] : "node-1";
var mqttPort = args.Length > 1 ? int.Parse(args[1]) : 1883;
var clusterPort = args.Length > 2 ? int.Parse(args[2]) : 11883;
var seedNodes = args.Length > 3 ? args[3].Split(',').ToList() : new List<string>();

Console.WriteLine("=== 集群测试 - 集群节点 ===");
Console.WriteLine($"节点 ID: {nodeId}");
Console.WriteLine($"MQTT 端口: {mqttPort}");
Console.WriteLine($"集群端口: {clusterPort}");
Console.WriteLine($"种子节点: {(seedNodes.Count > 0 ? string.Join(", ", seedNodes) : "无")}");
Console.WriteLine();

var broker = new MqttBroker(new MqttBrokerOptions { Port = mqttPort });

// 配置集群
broker.EnableCluster(new MqttClusterOptions
{
    NodeId = nodeId,
    ClusterName = "test-cluster",
    ClusterPort = clusterPort,
    BindAddress = "0.0.0.0",
    SeedNodes = seedNodes,
    HeartbeatIntervalMs = 5000,
    NodeTimeoutMs = 15000,
    EnableDeduplication = true,
    MessageIdCacheExpirySeconds = 60
});

// 监听集群事件
broker.Cluster!.PeerJoined += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [集群] 节点加入: {e.Peer.NodeId} ({e.Peer.Address})");
};

broker.Cluster!.PeerLeft += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [集群] 节点离开: {e.Peer.NodeId}");
};

broker.Cluster!.MessageForwarded += (s, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [集群] 消息转发: {e.Topic} (来自 {e.SourceNodeId})");
};

broker.Cluster!.SubscriptionSynced += (s, e) =>
{
    var action = e.IsSubscribe ? "订阅" : "取消订阅";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [集群] 同步{action}: {e.TopicFilter}");
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
    if (e.Session != null) // 仅显示本地客户端发布的消息
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 本地发布: {e.Message.Topic} = {e.Message.PayloadAsString}");
    }
};

broker.ClientSubscribed += (s, e) =>
{
    foreach (var sub in e.Subscriptions)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端订阅: {e.Session.ClientId} -> {sub.Topic}");
    }
};

await broker.StartAsync();
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 节点 {nodeId} 已启动");
Console.WriteLine();

// 定期显示集群状态
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(10000);
        var peers = broker.Cluster!.Peers;
        if (peers.Count > 0)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [状态] 已连接 {peers.Count} 个对等节点:");
            foreach (var peer in peers)
            {
                Console.WriteLine($"  - {peer.NodeId} (在线 {peer.Uptime.TotalSeconds:F0}秒)");
            }
        }
    }
});

Console.WriteLine("用法示例:");
Console.WriteLine("  启动节点 1: dotnet run -- node-1 1883 11883");
Console.WriteLine("  启动节点 2: dotnet run -- node-2 1884 11884 127.0.0.1:11883");
Console.WriteLine("  启动节点 3: dotnet run -- node-3 1885 11885 127.0.0.1:11883");
Console.WriteLine();
Console.WriteLine("测试方法:");
Console.WriteLine("  1. 用 MQTT 客户端连接到不同节点");
Console.WriteLine("  2. 在节点 A 订阅主题");
Console.WriteLine("  3. 在节点 B 发布消息");
Console.WriteLine("  4. 查看节点 A 的客户端是否收到消息");
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

Console.WriteLine("\n正在停止...");
await broker.StopAsync();
Console.WriteLine($"节点 {nodeId} 已停止");
