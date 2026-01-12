using System.Diagnostics;
using System.Net.MQTT;

namespace MqttBenchmark;

/// <summary>
/// MQTT 性能基准测试程序。
/// 支持发布者模式和订阅者模式，实时显示每秒消息数统计。
/// </summary>
class Program
{
    // 统计变量
    private static long _messageCount;
    private static long _byteCount;
    private static long _lastMessageCount;
    private static long _lastByteCount;
    private static readonly Stopwatch _stopwatch = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("      MQTT 性能基准测试 (Benchmark)");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // 解析命令行参数
        var config = ParseArguments(args);
        if (config == null)
        {
            ShowUsage();
            return;
        }

        Console.WriteLine($"模式: {(config.IsPublisher ? "发布者 (Publisher)" : "订阅者 (Subscriber)")}");
        Console.WriteLine($"服务器: {config.Host}:{config.Port}");
        Console.WriteLine($"主题: {config.Topic}");
        Console.WriteLine($"QoS: {config.QoS}");
        if (config.IsPublisher)
        {
            Console.WriteLine($"消息大小: {config.MessageSize} 字节");
            Console.WriteLine($"并发客户端: {config.ClientCount}");
            if (config.MessageCount > 0)
                Console.WriteLine($"消息数量: {config.MessageCount}");
            else
                Console.WriteLine($"消息数量: 无限制 (按 Ctrl+C 停止)");
        }
        Console.WriteLine();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n正在停止...");
        };

        // 启动统计显示任务
        var statsTask = ShowStatisticsAsync(config.IsPublisher, cts.Token);

        try
        {
            _stopwatch.Start();

            if (config.IsPublisher)
            {
                await RunPublisherBenchmarkAsync(config, cts.Token);
            }
            else
            {
                await RunSubscriberBenchmarkAsync(config, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n错误: {ex.Message}");
        }
        finally
        {
            _stopwatch.Stop();
            cts.Cancel();

            try
            {
                await statsTask;
            }
            catch { }

            ShowFinalStatistics(config.IsPublisher);
        }
    }

    /// <summary>
    /// 运行发布者基准测试。
    /// </summary>
    private static async Task RunPublisherBenchmarkAsync(BenchmarkConfig config, CancellationToken cancellationToken)
    {
        var clients = new List<MqttClient>();
        var tasks = new List<Task>();

        try
        {
            // 创建并连接所有客户端
            Console.WriteLine($"正在连接 {config.ClientCount} 个客户端...");

            for (int i = 0; i < config.ClientCount; i++)
            {
                var options = new MqttClientOptions
                {
                    Host = config.Host,
                    Port = config.Port,
                    ClientId = $"bench-pub-{Environment.MachineName}-{i}-{DateTime.Now.Ticks}",
                    CleanSession = true,
                    KeepAliveSeconds = 60,
                    ProtocolVersion = config.ProtocolVersion
                };

                var client = new MqttClient(options);
                var result = await client.ConnectAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"客户端 {i} 连接失败: {result.ResultCode}");
                    client.Dispose();
                    continue;
                }

                clients.Add(client);
            }

            Console.WriteLine($"已连接 {clients.Count} 个客户端");
            Console.WriteLine("开始发布消息...\n");

            // 生成测试消息
            var payload = new byte[config.MessageSize];
            Random.Shared.NextBytes(payload);

            // 每个客户端启动发布任务
            var messagesPerClient = config.MessageCount > 0
                ? config.MessageCount / clients.Count
                : long.MaxValue;

            foreach (var client in clients)
            {
                var task = PublishMessagesAsync(client, config.Topic, payload, config.QoS, messagesPerClient, cancellationToken);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            // 断开所有客户端
            foreach (var client in clients)
            {
                try
                {
                    await client.DisconnectAsync();
                    client.Dispose();
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// 单个客户端发布消息循环。
    /// </summary>
    private static async Task PublishMessagesAsync(
        MqttClient client,
        string topic,
        byte[] payload,
        MqttQualityOfService qos,
        long maxMessages,
        CancellationToken cancellationToken)
    {
        long count = 0;
        var message = MqttApplicationMessage.Create(topic, payload, qos, false);
        var errorCount = 0;

        while (!cancellationToken.IsCancellationRequested && count < maxMessages)
        {
            try
            {
                if (!client.IsConnected)
                {
                    Console.WriteLine($"\n[错误] 客户端已断开连接，已发送 {count} 条消息");
                    break;
                }

                await client.PublishAsync(message, cancellationToken);
                Interlocked.Increment(ref _messageCount);
                Interlocked.Add(ref _byteCount, payload.Length);
                count++;
                errorCount = 0; // 重置错误计数
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                errorCount++;
                if (errorCount >= 3)
                {
                    Console.WriteLine($"\n[错误] 连续发送失败 {errorCount} 次: {ex.Message}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 运行订阅者基准测试。
    /// </summary>
    private static async Task RunSubscriberBenchmarkAsync(BenchmarkConfig config, CancellationToken cancellationToken)
    {
        var options = new MqttClientOptions
        {
            Host = config.Host,
            Port = config.Port,
            ClientId = $"bench-sub-{Environment.MachineName}-{DateTime.Now.Ticks}",
            CleanSession = true,
            KeepAliveSeconds = 60,
            ProtocolVersion = config.ProtocolVersion
        };

        using var client = new MqttClient(options);

        client.MessageReceived += (_, e) =>
        {
            Interlocked.Increment(ref _messageCount);
            Interlocked.Add(ref _byteCount, e.Message.Payload.Length);
        };

        Console.WriteLine("正在连接...");
        var result = await client.ConnectAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"连接失败: {result.ResultCode}");
            return;
        }

        Console.WriteLine("已连接");
        Console.WriteLine($"正在订阅主题: {config.Topic}");

        await client.SubscribeAsync(config.Topic, config.QoS);
        Console.WriteLine("等待接收消息...\n");

        // 等待取消
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }

        await client.DisconnectAsync();
    }

    /// <summary>
    /// 实时显示统计信息。
    /// </summary>
    private static async Task ShowStatisticsAsync(bool isPublisher, CancellationToken cancellationToken)
    {
        var mode = isPublisher ? "发送" : "接收";

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var currentMessages = Interlocked.Read(ref _messageCount);
            var currentBytes = Interlocked.Read(ref _byteCount);
            var lastMessages = Interlocked.Exchange(ref _lastMessageCount, currentMessages);
            var lastBytes = Interlocked.Exchange(ref _lastByteCount, currentBytes);

            var messagesPerSec = currentMessages - lastMessages;
            var bytesPerSec = currentBytes - lastBytes;
            var elapsed = _stopwatch.Elapsed;

            var avgMessagesPerSec = elapsed.TotalSeconds > 0
                ? currentMessages / elapsed.TotalSeconds
                : 0;

            Console.Write($"\r[{elapsed:mm\\:ss}] {mode}: {messagesPerSec,8:N0} msg/s | ");
            Console.Write($"{FormatBytes(bytesPerSec)}/s | ");
            Console.Write($"总计: {currentMessages,12:N0} msg | ");
            Console.Write($"平均: {avgMessagesPerSec,8:N0} msg/s    ");
        }
    }

    /// <summary>
    /// 显示最终统计信息。
    /// </summary>
    private static void ShowFinalStatistics(bool isPublisher)
    {
        var mode = isPublisher ? "发送" : "接收";
        var elapsed = _stopwatch.Elapsed;
        var totalMessages = Interlocked.Read(ref _messageCount);
        var totalBytes = Interlocked.Read(ref _byteCount);

        Console.WriteLine("\n");
        Console.WriteLine("===========================================");
        Console.WriteLine("              最终统计结果");
        Console.WriteLine("===========================================");
        Console.WriteLine($"运行时间:     {elapsed:hh\\:mm\\:ss\\.fff}");
        Console.WriteLine($"总{mode}消息: {totalMessages:N0}");
        Console.WriteLine($"总{mode}数据: {FormatBytes(totalBytes)}");

        if (elapsed.TotalSeconds > 0)
        {
            var avgMsgPerSec = totalMessages / elapsed.TotalSeconds;
            var avgBytesPerSec = totalBytes / elapsed.TotalSeconds;
            Console.WriteLine($"平均速率:     {avgMsgPerSec:N0} msg/s ({FormatBytes((long)avgBytesPerSec)}/s)");
        }

        Console.WriteLine("===========================================");
    }

    /// <summary>
    /// 格式化字节数为可读字符串。
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 解析命令行参数。
    /// </summary>
    private static BenchmarkConfig? ParseArguments(string[] args)
    {
        var config = new BenchmarkConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-m" or "--mode":
                    if (i + 1 < args.Length)
                    {
                        config.IsPublisher = args[++i].ToLowerInvariant() switch
                        {
                            "pub" or "publisher" or "p" => true,
                            "sub" or "subscriber" or "s" => false,
                            _ => config.IsPublisher
                        };
                    }
                    break;

                case "-h" or "--host":
                    if (i + 1 < args.Length)
                        config.Host = args[++i];
                    break;

                case "-p" or "--port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var port))
                        config.Port = port;
                    break;

                case "-t" or "--topic":
                    if (i + 1 < args.Length)
                        config.Topic = args[++i];
                    break;

                case "-q" or "--qos":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var qos))
                        config.QoS = (MqttQualityOfService)qos;
                    break;

                case "-s" or "--size":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var size))
                        config.MessageSize = size;
                    break;

                case "-c" or "--clients":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var clients))
                        config.ClientCount = clients;
                    break;

                case "-n" or "--count":
                    if (i + 1 < args.Length && long.TryParse(args[++i], out var count))
                        config.MessageCount = count;
                    break;

                case "-v" or "--version":
                    if (i + 1 < args.Length)
                    {
                        config.ProtocolVersion = args[++i].ToLowerInvariant() switch
                        {
                            "3" or "3.1.1" or "311" => MqttProtocolVersion.V311,
                            "5" or "5.0" or "500" => MqttProtocolVersion.V500,
                            _ => config.ProtocolVersion
                        };
                    }
                    break;

                case "--help":
                    return null;
            }
        }

        return config;
    }

    /// <summary>
    /// 显示使用说明。
    /// </summary>
    private static void ShowUsage()
    {
        Console.WriteLine("用法: MqttBenchmark [选项]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  -m, --mode <pub|sub>    模式: pub=发布者, sub=订阅者 (默认: pub)");
        Console.WriteLine("  -h, --host <host>       MQTT 服务器地址 (默认: localhost)");
        Console.WriteLine("  -p, --port <port>       MQTT 服务器端口 (默认: 1883)");
        Console.WriteLine("  -t, --topic <topic>     主题 (默认: benchmark/test)");
        Console.WriteLine("  -q, --qos <0|1|2>       QoS 级别 (默认: 0)");
        Console.WriteLine("  -s, --size <bytes>      消息大小 (默认: 128 字节)");
        Console.WriteLine("  -c, --clients <count>   并发客户端数 (默认: 1)");
        Console.WriteLine("  -n, --count <count>     发送消息总数, 0=无限 (默认: 0)");
        Console.WriteLine("  -v, --version <3|5>     MQTT 协议版本: 3=3.1.1, 5=5.0 (默认: 5)");
        Console.WriteLine("  --help                  显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  # 发布者模式，发送 100000 条消息");
        Console.WriteLine("  MqttBenchmark -m pub -n 100000 -s 256 -q 0");
        Console.WriteLine();
        Console.WriteLine("  # 订阅者模式，接收消息");
        Console.WriteLine("  MqttBenchmark -m sub -t benchmark/test");
        Console.WriteLine();
        Console.WriteLine("  # 多客户端并发发布");
        Console.WriteLine("  MqttBenchmark -m pub -c 4 -s 64 -q 0");
        Console.WriteLine();
        Console.WriteLine("  # 使用 MQTT 3.1.1 协议");
        Console.WriteLine("  MqttBenchmark -m pub -v 3");
    }
}

/// <summary>
/// 基准测试配置。
/// </summary>
class BenchmarkConfig
{
    public bool IsPublisher { get; set; } = true;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Topic { get; set; } = "benchmark/test";
    public MqttQualityOfService QoS { get; set; } = MqttQualityOfService.AtMostOnce;
    public int MessageSize { get; set; } = 128;
    public int ClientCount { get; set; } = 1;
    public long MessageCount { get; set; } = 0;
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V500;
}
