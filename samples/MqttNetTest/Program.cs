using System.Buffers;
using System.Diagnostics;
using MQTTnet;
using MQTTnet.Protocol;

if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
{
    Console.WriteLine("MQTTnet 测试工具");
    Console.WriteLine("================");
    Console.WriteLine();
    Console.WriteLine("用法:");
    Console.WriteLine("  接收端: dotnet run -- sub [host] [port] [v3|v5]");
    Console.WriteLine("  发送端: dotnet run -- pub [host] [port] [v3|v5] [消息数] [消息大小] [客户端数]");
    Console.WriteLine("  保留消息测试: dotnet run -- retain [host] [port]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  dotnet run -- sub 127.0.0.1 1883 v5       # V5.0 接收端");
    Console.WriteLine("  dotnet run -- sub 127.0.0.1 1883 v3       # V3.1.1 接收端");
    Console.WriteLine("  dotnet run -- pub 127.0.0.1 1883 v5 100000 128 4   # V5.0 发送 10万消息");
    Console.WriteLine("  dotnet run -- pub 127.0.0.1 1883 v3 100000 128 4   # V3.1.1 发送 10万消息");
    Console.WriteLine("  dotnet run -- retain 127.0.0.1 1883       # 测试保留消息");
    return;
}

var mode = args[0].ToLower();
var host = args.Length > 1 ? args[1] : "127.0.0.1";
var port = args.Length > 2 ? int.Parse(args[2]) : 1883;
var version = args.Length > 3 ? args[3].ToLower() : "v5";

var protocolVersion = version == "v3" || version == "v311" || version == "3"
    ? MQTTnet.Formatter.MqttProtocolVersion.V311
    : MQTTnet.Formatter.MqttProtocolVersion.V500;

var versionName = protocolVersion == MQTTnet.Formatter.MqttProtocolVersion.V500 ? "MQTT 5.0" : "MQTT 3.1.1";

if (mode == "sub" || mode == "subscriber" || mode == "recv")
{
    await RunSubscriber(host, port, protocolVersion, versionName);
}
else if (mode == "pub" || mode == "publisher" || mode == "send")
{
    var messageCount = args.Length > 4 ? int.Parse(args[4]) : 100000;
    var messageSize = args.Length > 5 ? int.Parse(args[5]) : 128;
    var clientCount = args.Length > 6 ? int.Parse(args[6]) : 1;
    await RunPublisher(host, port, protocolVersion, versionName, messageCount, messageSize, clientCount);
}
else if (mode == "retain" || mode == "retained")
{
    await MqttNetTest.RetainTest.RunAsync(host, port);
}
else
{
    Console.WriteLine($"未知模式: {mode}，使用 sub, pub 或 retain");
}

async Task RunSubscriber(string host, int port, MQTTnet.Formatter.MqttProtocolVersion protocolVersion, string versionName)
{
    Console.WriteLine($"MQTTnet 接收端 ({versionName})");
    Console.WriteLine("==========================");
    Console.WriteLine($"服务器: {host}:{port}");
    Console.WriteLine("按 Ctrl+C 退出");
    Console.WriteLine();

    var factory = new MqttClientFactory();
    var client = factory.CreateMqttClient();
    var receivedCount = 0L;
    var receivedBytes = 0L;
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    client.ApplicationMessageReceivedAsync += e =>
    {
        Interlocked.Increment(ref receivedCount);
        Interlocked.Add(ref receivedBytes, e.ApplicationMessage.Payload.Length);
        return Task.CompletedTask;
    };

    client.DisconnectedAsync += e =>
    {
        if (!cts.Token.IsCancellationRequested)
        {
            Console.WriteLine($"[断开连接] 原因: {e.Reason}");
        }
        return Task.CompletedTask;
    };

    try
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId($"mqttnet-sub-{versionName.Replace(" ", "").Replace(".", "")}-{Environment.ProcessId}")
            .WithProtocolVersion(protocolVersion)
            .WithCleanStart(true)
            .Build();

        Console.WriteLine("连接中...");
        var result = await client.ConnectAsync(options);
        Console.WriteLine($"连接结果: {result.ResultCode}");

        if (result.ResultCode != MqttClientConnectResultCode.Success)
        {
            Console.WriteLine("连接失败");
            return;
        }

        await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("stress/#", MqttQualityOfServiceLevel.AtMostOnce)
            .Build());
        Console.WriteLine("已订阅 stress/#");
        Console.WriteLine();

        var lastCount = 0L;
        var lastBytes = 0L;
        var sw = Stopwatch.StartNew();

        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(1000);
            var currentCount = Interlocked.Read(ref receivedCount);
            var currentBytes = Interlocked.Read(ref receivedBytes);
            var countPerSec = currentCount - lastCount;
            var bytesPerSec = currentBytes - lastBytes;

            Console.WriteLine($"[{sw.Elapsed:mm\\:ss}] 接收: {currentCount} 条 ({countPerSec}/s, {bytesPerSec / 1024.0:F1} KB/s)");

            lastCount = currentCount;
            lastBytes = currentBytes;
        }

        Console.WriteLine();
        Console.WriteLine($"总计接收: {receivedCount} 条消息, {receivedBytes / 1024.0 / 1024.0:F2} MB");
        await client.DisconnectAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
    }
}

async Task RunPublisher(string host, int port, MQTTnet.Formatter.MqttProtocolVersion protocolVersion, string versionName, int messageCount, int messageSize, int clientCount)
{
    Console.WriteLine($"MQTTnet 发送端 ({versionName})");
    Console.WriteLine("==========================");
    Console.WriteLine($"服务器: {host}:{port}");
    Console.WriteLine($"消息数量: {messageCount}");
    Console.WriteLine($"消息大小: {messageSize} 字节");
    Console.WriteLine($"客户端数: {clientCount}");
    Console.WriteLine();

    var payload = new byte[messageSize];
    Random.Shared.NextBytes(payload);

    var factory = new MqttClientFactory();
    var sentCount = 0L;
    var cts = new CancellationTokenSource();

    try
    {
        // 创建发布者
        var publishers = new List<IMqttClient>();
        Console.WriteLine($"连接 {clientCount} 个发布者...");

        for (int i = 0; i < clientCount; i++)
        {
            var pub = factory.CreateMqttClient();
            var pubOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithClientId($"mqttnet-pub-{versionName.Replace(" ", "").Replace(".", "")}-{i}-{Environment.ProcessId}")
                .WithProtocolVersion(protocolVersion)
                .WithCleanStart(true)
                .Build();
            await pub.ConnectAsync(pubOptions);
            publishers.Add(pub);
        }
        Console.WriteLine("发布者已就绪");
        Console.WriteLine();

        // 统计线程
        var statsTask = Task.Run(async () =>
        {
            var lastSent = 0L;
            var sw = Stopwatch.StartNew();

            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000);
                var currentSent = Interlocked.Read(ref sentCount);
                var sentPerSec = currentSent - lastSent;

                Console.WriteLine($"[{sw.Elapsed:mm\\:ss}] 发送: {currentSent}/{messageCount} ({sentPerSec}/s)");

                lastSent = currentSent;

                if (currentSent >= messageCount) break;
            }
        });

        // 开始压力测试
        Console.WriteLine("开始发送...");
        var stopwatch = Stopwatch.StartNew();

        var messagesPerClient = messageCount / clientCount;
        var tasks = new List<Task>();

        for (int c = 0; c < clientCount; c++)
        {
            var clientIndex = c;
            var publisher = publishers[c];

            tasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < messagesPerClient; i++)
                {
                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic($"stress/client{clientIndex}/msg{i}")
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                        .Build();

                    await publisher.PublishAsync(msg);
                    Interlocked.Increment(ref sentCount);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        cts.Cancel();
        await Task.Delay(200);

        // 统计结果
        Console.WriteLine();
        Console.WriteLine("========== 发送结果 ==========");
        Console.WriteLine($"协议版本: {versionName}");
        Console.WriteLine($"发送消息: {sentCount}");
        Console.WriteLine($"发送耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"发送速率: {sentCount * 1000.0 / stopwatch.ElapsedMilliseconds:F0} msg/s");
        Console.WriteLine($"发送吞吐: {sentCount * messageSize * 1000.0 / stopwatch.ElapsedMilliseconds / 1024 / 1024:F2} MB/s");

        // 断开连接
        Console.WriteLine();
        Console.WriteLine("断开连接...");
        foreach (var pub in publishers)
        {
            await pub.DisconnectAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }

    Console.WriteLine("完成");
}
