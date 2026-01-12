using System.Net.MQTT;
using System.Net.MQTT.Broker;
using System.Net.MQTT.Broker.Bridge;
using System.Net.MQTT.Broker.Cluster;

Console.WriteLine("=== MQTT 桥接与集群功能测试 ===\n");
Console.WriteLine("请选择测试模式:");
Console.WriteLine("1. 桥接测试 (Bridge)");
Console.WriteLine("2. 集群测试 (Cluster)");
Console.WriteLine("3. 退出");
Console.Write("\n请输入选项 (1-3): ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        await RunBridgeTestAsync();
        break;
    case "2":
        await RunClusterTestAsync();
        break;
    case "3":
        return;
    default:
        Console.WriteLine("无效选项");
        break;
}

/// <summary>
/// 桥接测试：父 Broker (1883) + 子 Broker (2883) + 桥接
/// </summary>
async Task RunBridgeTestAsync()
{
    Console.WriteLine("\n=== 桥接测试 ===");
    Console.WriteLine("架构: 子 Broker (2883) --桥接--> 父 Broker (1883)");
    Console.WriteLine();

    // 1. 启动父 Broker
    Console.WriteLine("[1] 启动父 Broker (端口 1883)...");
    var parentBroker = new MqttBroker(new MqttBrokerOptions { Port = 1883 });
    parentBroker.MessagePublished += (s, e) =>
    {
        Console.WriteLine($"  [父Broker] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };
    await parentBroker.StartAsync();
    Console.WriteLine("  父 Broker 已启动");

    // 2. 启动子 Broker，配置桥接到父 Broker
    Console.WriteLine("\n[2] 启动子 Broker (端口 2883) 并配置桥接...");
    var childBroker = new MqttBroker(new MqttBrokerOptions { Port = 2883 });

    // 配置桥接
    var bridge = childBroker.AddBridge(new MqttBridgeOptions
    {
        Name = "parent-bridge",
        RemoteHost = "127.0.0.1",
        RemotePort = 1883,
        ClientId = "bridge-client-" + Guid.NewGuid().ToString("N")[..8],

        // 上行规则：本地 sensor/# 消息同步到父 Broker，添加前缀 site1/
        UpstreamRules =
        {
            new MqttBridgeRule
            {
                LocalTopicFilter = "sensor/#",
                RemoteTopicPrefix = "site1/",
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
            }
        },

        ReconnectDelayMs = 3000
    });

    // 监听桥接事件
    bridge.Connected += (s, e) => Console.WriteLine($"  [桥接] 已连接到父 Broker");
    bridge.Disconnected += (s, e) => Console.WriteLine($"  [桥接] 与父 Broker 断开连接");
    bridge.MessageForwarded += (s, e) => Console.WriteLine($"  [桥接] 消息转发: {e.OriginalTopic} -> {e.TransformedTopic}");

    childBroker.MessagePublished += (s, e) =>
    {
        Console.WriteLine($"  [子Broker] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };

    await childBroker.StartAsync();
    Console.WriteLine("  子 Broker 已启动");

    // 等待桥接连接
    await Task.Delay(2000);

    // 3. 创建测试客户端
    Console.WriteLine("\n[3] 创建测试客户端...");

    // 连接到子 Broker 的客户端
    var childClient = new MqttClient(new MqttClientOptions
    {
        Host = "127.0.0.1",
        Port = 2883,
        ClientId = "child-client"
    });
    await childClient.ConnectAsync();
    Console.WriteLine("  客户端已连接到子 Broker (2883)");

    // 连接到父 Broker 的客户端（用于验证消息是否同步）
    var parentClient = new MqttClient(new MqttClientOptions
    {
        Host = "127.0.0.1",
        Port = 1883,
        ClientId = "parent-client"
    });
    await parentClient.ConnectAsync();
    await parentClient.SubscribeAsync("site1/sensor/#");
    parentClient.MessageReceived += (s, e) =>
    {
        Console.WriteLine($"  [父Broker客户端] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };
    Console.WriteLine("  客户端已连接到父 Broker (1883) 并订阅 site1/sensor/#");

    // 4. 测试上行同步
    Console.WriteLine("\n[4] 测试上行同步 (子 -> 父)...");
    Console.WriteLine("  在子 Broker 发布: sensor/temperature = 25.5");
    await childClient.PublishAsync("sensor/temperature", "25.5");
    await Task.Delay(1000);

    Console.WriteLine("  在子 Broker 发布: sensor/humidity = 60");
    await childClient.PublishAsync("sensor/humidity", "60");
    await Task.Delay(1000);

    // 5. 测试下行同步
    Console.WriteLine("\n[5] 测试下行同步 (父 -> 子)...");

    // 子 Broker 客户端订阅 commands
    await childClient.SubscribeAsync("commands/#");
    childClient.MessageReceived += (s, e) =>
    {
        Console.WriteLine($"  [子Broker客户端] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };

    Console.WriteLine("  在父 Broker 发布: commands/restart = true");
    await parentClient.PublishAsync("commands/restart", "true");
    await Task.Delay(1000);

    // 6. 显示统计信息
    Console.WriteLine("\n[6] 桥接统计信息:");
    var stats = bridge.GetStatistics();
    Console.WriteLine($"  上行消息数: {stats.UpstreamMessageCount}");
    Console.WriteLine($"  下行消息数: {stats.DownstreamMessageCount}");
    Console.WriteLine($"  上行字节数: {stats.UpstreamByteCount}");
    Console.WriteLine($"  下行字节数: {stats.DownstreamByteCount}");
    Console.WriteLine($"  重连次数: {stats.ReconnectCount}");

    // 清理
    Console.WriteLine("\n按任意键停止测试...");
    Console.ReadKey();

    await childClient.DisconnectAsync();
    await parentClient.DisconnectAsync();
    await childBroker.StopAsync();
    await parentBroker.StopAsync();

    Console.WriteLine("\n桥接测试完成!");
}

/// <summary>
/// 集群测试：3 个节点组成集群
/// </summary>
async Task RunClusterTestAsync()
{
    Console.WriteLine("\n=== 集群测试 ===");
    Console.WriteLine("架构: Node-1 (1883) <--> Node-2 (1884) <--> Node-3 (1885)");
    Console.WriteLine("集群端口: 11883, 11884, 11885");
    Console.WriteLine();

    // 1. 启动节点 1
    Console.WriteLine("[1] 启动 Node-1 (MQTT: 1883, 集群: 11883)...");
    var broker1 = new MqttBroker(new MqttBrokerOptions { Port = 1883 });
    broker1.EnableCluster(new MqttClusterOptions
    {
        NodeId = "node-1",
        ClusterName = "test-cluster",
        ClusterPort = 11883,
        BindAddress = "0.0.0.0",
        SeedNodes = new List<string> { "127.0.0.1:11884", "127.0.0.1:11885" },
        HeartbeatIntervalMs = 5000,
        NodeTimeoutMs = 15000,
        EnableDeduplication = true
    });
    broker1.Cluster!.PeerJoined += (s, e) => Console.WriteLine($"  [Node-1] 对等节点加入: {e.Peer.NodeId}");
    broker1.Cluster!.PeerLeft += (s, e) => Console.WriteLine($"  [Node-1] 对等节点离开: {e.Peer.NodeId}");
    broker1.MessagePublished += (s, e) =>
    {
        if (e.Session != null) // 仅显示来自客户端的消息
            Console.WriteLine($"  [Node-1] 客户端发布: {e.Message.Topic}");
    };
    await broker1.StartAsync();
    Console.WriteLine("  Node-1 已启动");

    // 2. 启动节点 2
    Console.WriteLine("\n[2] 启动 Node-2 (MQTT: 1884, 集群: 11884)...");
    var broker2 = new MqttBroker(new MqttBrokerOptions { Port = 1884 });
    broker2.EnableCluster(new MqttClusterOptions
    {
        NodeId = "node-2",
        ClusterName = "test-cluster",
        ClusterPort = 11884,
        BindAddress = "0.0.0.0",
        SeedNodes = new List<string> { "127.0.0.1:11883" },
        HeartbeatIntervalMs = 5000,
        NodeTimeoutMs = 15000,
        EnableDeduplication = true
    });
    broker2.Cluster!.PeerJoined += (s, e) => Console.WriteLine($"  [Node-2] 对等节点加入: {e.Peer.NodeId}");
    broker2.Cluster!.PeerLeft += (s, e) => Console.WriteLine($"  [Node-2] 对等节点离开: {e.Peer.NodeId}");
    broker2.MessagePublished += (s, e) =>
    {
        if (e.Session != null)
            Console.WriteLine($"  [Node-2] 客户端发布: {e.Message.Topic}");
    };
    await broker2.StartAsync();
    Console.WriteLine("  Node-2 已启动");

    // 3. 启动节点 3
    Console.WriteLine("\n[3] 启动 Node-3 (MQTT: 1885, 集群: 11885)...");
    var broker3 = new MqttBroker(new MqttBrokerOptions { Port = 1885 });
    broker3.EnableCluster(new MqttClusterOptions
    {
        NodeId = "node-3",
        ClusterName = "test-cluster",
        ClusterPort = 11885,
        BindAddress = "0.0.0.0",
        SeedNodes = new List<string> { "127.0.0.1:11883" },
        HeartbeatIntervalMs = 5000,
        NodeTimeoutMs = 15000,
        EnableDeduplication = true
    });
    broker3.Cluster!.PeerJoined += (s, e) => Console.WriteLine($"  [Node-3] 对等节点加入: {e.Peer.NodeId}");
    broker3.Cluster!.PeerLeft += (s, e) => Console.WriteLine($"  [Node-3] 对等节点离开: {e.Peer.NodeId}");
    broker3.MessagePublished += (s, e) =>
    {
        if (e.Session != null)
            Console.WriteLine($"  [Node-3] 客户端发布: {e.Message.Topic}");
    };
    await broker3.StartAsync();
    Console.WriteLine("  Node-3 已启动");

    // 等待集群建立连接
    Console.WriteLine("\n[4] 等待集群建立连接...");
    await Task.Delay(3000);

    // 显示集群状态
    Console.WriteLine("\n[5] 集群状态:");
    Console.WriteLine($"  Node-1 对等节点: {broker1.Cluster!.Peers.Count} 个");
    foreach (var peer in broker1.Cluster!.Peers)
    {
        Console.WriteLine($"    - {peer.NodeId} ({peer.Address}:{peer.ListenPort})");
    }
    Console.WriteLine($"  Node-2 对等节点: {broker2.Cluster!.Peers.Count} 个");
    Console.WriteLine($"  Node-3 对等节点: {broker3.Cluster!.Peers.Count} 个");

    // 4. 创建测试客户端
    Console.WriteLine("\n[6] 创建测试客户端...");

    // 客户端 A 连接到 Node-1
    var clientA = new MqttClient(new MqttClientOptions
    {
        Host = "127.0.0.1",
        Port = 1883,
        ClientId = "client-A"
    });
    await clientA.ConnectAsync();
    await clientA.SubscribeAsync("test/#");
    clientA.MessageReceived += (s, e) =>
    {
        Console.WriteLine($"  [客户端A@Node-1] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };
    Console.WriteLine("  客户端 A 已连接到 Node-1 (1883) 并订阅 test/#");

    // 客户端 B 连接到 Node-2
    var clientB = new MqttClient(new MqttClientOptions
    {
        Host = "127.0.0.1",
        Port = 1884,
        ClientId = "client-B"
    });
    await clientB.ConnectAsync();
    await clientB.SubscribeAsync("test/#");
    clientB.MessageReceived += (s, e) =>
    {
        Console.WriteLine($"  [客户端B@Node-2] 收到消息: {e.Message.Topic} = {e.Message.PayloadAsString}");
    };
    Console.WriteLine("  客户端 B 已连接到 Node-2 (1884) 并订阅 test/#");

    // 客户端 C 连接到 Node-3
    var clientC = new MqttClient(new MqttClientOptions
    {
        Host = "127.0.0.1",
        Port = 1885,
        ClientId = "client-C"
    });
    await clientC.ConnectAsync();
    Console.WriteLine("  客户端 C 已连接到 Node-3 (1885) 用于发布消息");

    await Task.Delay(1000);

    // 5. 测试跨节点消息传递
    Console.WriteLine("\n[7] 测试跨节点消息传递...");
    Console.WriteLine("  客户端 C (Node-3) 发布消息: test/hello = Hello Cluster!");
    await clientC.PublishAsync("test/hello", "Hello Cluster!");
    await Task.Delay(2000);

    Console.WriteLine("\n  客户端 C (Node-3) 发布消息: test/data = {\"value\": 42}");
    await clientC.PublishAsync("test/data", "{\"value\": 42}");
    await Task.Delay(2000);

    // 测试从不同节点发布
    Console.WriteLine("\n  客户端 A (Node-1) 发布消息: test/from-node1 = Message from Node-1");
    await clientA.PublishAsync("test/from-node1", "Message from Node-1");
    await Task.Delay(2000);

    // 清理
    Console.WriteLine("\n按任意键停止测试...");
    Console.ReadKey();

    await clientA.DisconnectAsync();
    await clientB.DisconnectAsync();
    await clientC.DisconnectAsync();
    await broker3.StopAsync();
    await broker2.StopAsync();
    await broker1.StopAsync();

    Console.WriteLine("\n集群测试完成!");
}
