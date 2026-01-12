using System.Net.MQTT.Broker;
using System.Net.MQTT.Broker.CoAP;

Console.WriteLine("=== CoAP-MQTT Gateway Server ===\n");

// 配置 Broker
var options = new MqttBrokerOptions
{
    BindAddress = "0.0.0.0",
    Port = 1883,                    // MQTT TCP 端口
    EnableCoAP = true,              // 启用 CoAP
    CoapPort = 5683,                // CoAP UDP 端口
    CoapMqttPrefix = "mqtt",        // CoAP URI 前缀: coap://host/mqtt/topic
    AllowAnonymous = true,
    EnableRetainedMessages = true
};

// 创建 Broker
var broker = new MqttBroker(options);

// 订阅消息事件
broker.MessagePublishing += (s, e) =>
{
    Console.WriteLine($"[事件] 消息发布中: 协议={e.Protocol}, 来源={e.SourceClientId ?? "N/A"}");
    Console.WriteLine($"       主题='{e.Message.Topic}', 载荷='{e.Message.PayloadAsString}'");
};

broker.MessagePublished += (s, e) =>
{
    Console.WriteLine($"[事件] 消息已发布: 协议={e.Protocol}, 主题='{e.Message.Topic}', 投递数={e.DeliveredCount}");
};

// 创建 CoAP 网关
var coapGateway = new CoapMqttGateway(broker, options);

var cts = new CancellationTokenSource();

// 处理 Ctrl+C
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    // 启动 Broker
    await broker.StartAsync(cts.Token);
    Console.WriteLine($"[Broker] MQTT Broker 已启动，TCP 端口: {options.Port}");

    // 启动 CoAP 网关
    await coapGateway.StartAsync(cts.Token);
    Console.WriteLine($"[CoAP] 网关已启动，UDP 端口: {options.CoapPort}");

    Console.WriteLine("\n服务已启动，等待 CoAP 客户端连接...");
    Console.WriteLine("CoAP 资源路径格式: coap://localhost:5683/mqtt/{topic}");
    Console.WriteLine("\n支持的操作:");
    Console.WriteLine("  - GET  /mqtt/{topic}         : 获取保留消息");
    Console.WriteLine("  - GET  /mqtt/{topic} +Observe: 订阅主题");
    Console.WriteLine("  - PUT  /mqtt/{topic}         : 发布消息（保留）");
    Console.WriteLine("  - POST /mqtt/{topic}         : 发布消息（保留）");
    Console.WriteLine("  - DELETE /mqtt/{topic}       : 删除保留消息");
    Console.WriteLine("\n按 Ctrl+C 停止服务...\n");

    // 等待取消
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n正在停止服务...");
}
finally
{
    await coapGateway.DisposeAsync();
    await broker.StopAsync();
    Console.WriteLine("服务已停止。");
}
