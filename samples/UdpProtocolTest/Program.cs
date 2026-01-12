using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.MQTT.Broker;
using System.Net.MQTT.Broker.MqttSn;
using System.Net.MQTT.Broker.CoAP;
using System.Net.MQTT.MqttSn.Protocol;
using System.Net.MQTT.MqttSn.Protocol.Packets;
using System.Net.MQTT.MqttSn.Serialization;
using System.Net.MQTT.CoAP.Protocol;
using System.Net.MQTT.CoAP.Serialization;
using System.Net.MQTT;

Console.WriteLine("=== MQTT Broker with UDP Protocol Support Test ===\n");

// 配置 Broker
var options = new MqttBrokerOptions
{
    BindAddress = "0.0.0.0",
    Port = 1883,                    // MQTT TCP 端口
    EnableMqttSn = true,            // 启用 MQTT-SN
    MqttSnPort = 1885,              // MQTT-SN UDP 端口
    EnableCoAP = true,              // 启用 CoAP
    CoapPort = 5683,                // CoAP UDP 端口
    CoapMqttPrefix = "mqtt",        // CoAP URI 前缀
    AllowAnonymous = true,
    EnableRetainedMessages = true
};

// 创建并启动 Broker
var broker = new MqttBroker(options);

// 订阅消息发布事件，显示协议来源信息
broker.MessagePublishing += (s, e) =>
{
    Console.WriteLine($"[Broker] 收到消息: 协议={e.Protocol}, 来源={e.SourceClientId ?? "N/A"}, 主题='{e.Message.Topic}', 载荷='{e.Message.PayloadAsString}'");
};

broker.MessagePublished += (s, e) =>
{
    Console.WriteLine($"[Broker] 消息已发布: 协议={e.Protocol}, 主题='{e.Message.Topic}', 投递数={e.DeliveredCount}");
};
broker.MessageDelivered += (s, e) =>
{
    Console.WriteLine($"[Broker] 消息已投递: 协议={e.Protocol}, 主题='{e.Message.Topic}', 客户端={e.TargetSession.ClientId} 消息来源：{e.SourceClientId}");
};
// 创建 MQTT-SN 网关
var mqttSnGateway = new MqttSnGateway(broker, options);

// 创建 CoAP 网关
var coapGateway = new CoapMqttGateway(broker, options);
var cts = new CancellationTokenSource();

try
{
    // 启动 Broker
    await broker.StartAsync(cts.Token);
    Console.WriteLine($"[Broker] MQTT Broker 已启动，TCP 端口: {options.Port}");

    // 启动 MQTT-SN 网关
    await mqttSnGateway.StartAsync(cts.Token);
    Console.WriteLine($"[MQTT-SN] 网关已启动，UDP 端口: {options.MqttSnPort}");

    // 启动 CoAP 网关
    await coapGateway.StartAsync(cts.Token);
    Console.WriteLine($"[CoAP] 网关已启动，UDP 端口: {options.CoapPort}");

    Console.WriteLine("\n所有服务已启动！\n");

    // 等待服务完全启动
    await Task.Delay(500);

    // 运行测试
    Console.WriteLine("\n--- 开始测试 ---\n");

    // 测试 MQTT-SN
    await TestMqttSnAsync();

    Console.WriteLine();

    // 测试 CoAP
    await TestCoapAsync();

    Console.WriteLine("\n--- 测试完成 ---\n");
}
finally
{
    cts.Cancel();
    await coapGateway.DisposeAsync();
    await mqttSnGateway.DisposeAsync();
    await broker.StopAsync();
    Console.WriteLine("\n所有服务已停止。");
}

/// <summary>
/// 测试 MQTT-SN 协议
/// </summary>
async Task TestMqttSnAsync()
{
    Console.WriteLine("=== MQTT-SN 测试 ===\n");

    using var udpClient = new UdpClient();
    var serverEndpoint = new IPEndPoint(IPAddress.Loopback, options.MqttSnPort);

    // 1. 发送 SEARCHGW 请求
    Console.WriteLine("[MQTT-SN] 发送 SEARCHGW 请求...");
    var searchGw = new MqttSnSearchGwPacket { Radius = 0 };
    var searchGwData = new byte[searchGw.Length];
    searchGw.WriteTo(searchGwData);
    await udpClient.SendAsync(searchGwData, serverEndpoint);

    // 接收 GWINFO 响应
    var receiveResult = await udpClient.ReceiveAsync();
    var gwInfo = MqttSnSerializer.Deserialize(receiveResult.Buffer) as MqttSnGwInfoPacket;
    Console.WriteLine($"[MQTT-SN] 收到 GWINFO: GatewayId={gwInfo?.GatewayId}");

    // 2. 发送 CONNECT 请求
    Console.WriteLine("[MQTT-SN] 发送 CONNECT 请求...");
    var connect = new MqttSnConnectPacket
    {
        Flags = MqttSnFlags.Create().WithCleanSession(true).Build(),
        Duration = 60,
        ClientId = "mqtt-sn-test-client"
    };
    var connectData = new byte[connect.Length];
    connect.WriteTo(connectData);
    await udpClient.SendAsync(connectData, serverEndpoint);

    // 接收 CONNACK 响应
    receiveResult = await udpClient.ReceiveAsync();
    var connAck = MqttSnSerializer.Deserialize(receiveResult.Buffer) as MqttSnConnAckPacket;
    Console.WriteLine($"[MQTT-SN] 收到 CONNACK: ReturnCode={connAck?.ReturnCode}");

    // 3. 注册主题
    Console.WriteLine("[MQTT-SN] 发送 REGISTER 请求...");
    var register = new MqttSnRegisterPacket
    {
        TopicId = 0,
        MessageId = 1,
        TopicName = "test/mqtt-sn"
    };
    var registerData = new byte[register.Length];
    register.WriteTo(registerData);
    await udpClient.SendAsync(registerData, serverEndpoint);

    // 接收 REGACK 响应
    receiveResult = await udpClient.ReceiveAsync();
    var regAck = MqttSnSerializer.Deserialize(receiveResult.Buffer) as MqttSnRegAckPacket;
    Console.WriteLine($"[MQTT-SN] 收到 REGACK: TopicId={regAck?.TopicId}, ReturnCode={regAck?.ReturnCode}");

    // 4. 发布消息
    Console.WriteLine("[MQTT-SN] 发送 PUBLISH 请求...");
    var publish = new MqttSnPublishPacket
    {
        Flags = MqttSnFlags.Create()
            .WithQoS(MqttQualityOfService.AtLeastOnce)
            .WithRetain(true)
            .WithTopicType(MqttSnTopicType.Normal)
            .Build(),
        TopicId = regAck?.TopicId ?? 0,
        MessageId = 2,
        Data = Encoding.UTF8.GetBytes("Hello from MQTT-SN!")
    };
    var publishData = new byte[publish.Length];
    publish.WriteTo(publishData);
    await udpClient.SendAsync(publishData, serverEndpoint);

    // 接收 PUBACK 响应
    receiveResult = await udpClient.ReceiveAsync();
    var pubAck = MqttSnSerializer.Deserialize(receiveResult.Buffer) as MqttSnPubAckPacket;
    Console.WriteLine($"[MQTT-SN] 收到 PUBACK: TopicId={pubAck?.TopicId}, ReturnCode={pubAck?.ReturnCode}");

    // 5. 发送 DISCONNECT
    Console.WriteLine("[MQTT-SN] 发送 DISCONNECT 请求...");
    var disconnect = new MqttSnDisconnectPacket();
    var disconnectData = new byte[disconnect.Length];
    disconnect.WriteTo(disconnectData);
    await udpClient.SendAsync(disconnectData, serverEndpoint);

    // 接收 DISCONNECT 响应
    receiveResult = await udpClient.ReceiveAsync();
    var disconnectResp = MqttSnSerializer.Deserialize(receiveResult.Buffer) as MqttSnDisconnectPacket;
    Console.WriteLine("[MQTT-SN] 收到 DISCONNECT 响应");

    Console.WriteLine("[MQTT-SN] 测试完成！");
    Console.ReadLine();
}

/// <summary>
/// 测试 CoAP 协议
/// </summary>
async Task TestCoapAsync()
{
    Console.WriteLine("=== CoAP 测试 ===\n");

    using var udpClient = new UdpClient();
    var serverEndpoint = new IPEndPoint(IPAddress.Loopback, options.CoapPort);

    // 1. PUT 请求 - 发布保留消息
    Console.WriteLine("[CoAP] 发送 PUT 请求 (发布消息)...");
    var putRequest = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Put,
        MessageId = 1001,
        Token = new byte[] { 0x01 }
    };
    putRequest.SetUriPath($"{options.CoapMqttPrefix}/test/coap");
    putRequest.Payload = Encoding.UTF8.GetBytes("Hello from CoAP!");
    putRequest.SetContentFormat(CoapContentFormat.TextPlain);

    var putData = new byte[CoapSerializer.CalculateSize(putRequest)];
    CoapSerializer.Serialize(putRequest, putData);
    await udpClient.SendAsync(putData, serverEndpoint);

    // 接收响应
    var receiveResult = await udpClient.ReceiveAsync();
    var putResponse = CoapSerializer.Deserialize(receiveResult.Buffer);
    Console.WriteLine($"[CoAP] 收到响应: {putResponse.Code} ({(putResponse.Code.IsSuccess ? "成功" : "失败")})");

    // 2. GET 请求 - 获取保留消息
    Console.WriteLine("[CoAP] 发送 GET 请求 (获取保留消息)...");
    var getRequest = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Get,
        MessageId = 1002,
        Token = new byte[] { 0x02 }
    };
    getRequest.SetUriPath($"{options.CoapMqttPrefix}/test/coap");

    var getData = new byte[CoapSerializer.CalculateSize(getRequest)];
    CoapSerializer.Serialize(getRequest, getData);
    await udpClient.SendAsync(getData, serverEndpoint);

    // 接收响应
    receiveResult = await udpClient.ReceiveAsync();
    var getResponse = CoapSerializer.Deserialize(receiveResult.Buffer);
    Console.WriteLine($"[CoAP] 收到响应: {getResponse.Code}");
    if (getResponse.Payload.Length > 0)
    {
        Console.WriteLine($"[CoAP] 载荷内容: {Encoding.UTF8.GetString(getResponse.Payload)}");
    }

    // 3. GET + Observe - 订阅主题
    Console.WriteLine("[CoAP] 发送 GET + Observe 请求 (订阅主题)...");
    var observeRequest = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Get,
        MessageId = 1003,
        Token = new byte[] { 0x03 }
    };
    observeRequest.SetUriPath($"{options.CoapMqttPrefix}/test/coap");
    observeRequest.SetObserve(0); // 0 = 注册观察

    var observeData = new byte[CoapSerializer.CalculateSize(observeRequest)];
    CoapSerializer.Serialize(observeRequest, observeData);
    await udpClient.SendAsync(observeData, serverEndpoint);

    // 接收响应
    receiveResult = await udpClient.ReceiveAsync();
    var observeResponse = CoapSerializer.Deserialize(receiveResult.Buffer);
    Console.WriteLine($"[CoAP] 收到 Observe 响应: {observeResponse.Code}, Observe={observeResponse.GetObserve()}");

    // 4. DELETE 请求 - 删除保留消息
    Console.WriteLine("[CoAP] 发送 DELETE 请求 (删除保留消息)...");
    var deleteRequest = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Delete,
        MessageId = 1004,
        Token = new byte[] { 0x04 }
    };
    deleteRequest.SetUriPath($"{options.CoapMqttPrefix}/test/coap");

    var deleteData = new byte[CoapSerializer.CalculateSize(deleteRequest)];
    CoapSerializer.Serialize(deleteRequest, deleteData);
    await udpClient.SendAsync(deleteData, serverEndpoint);

    // 接收响应
    receiveResult = await udpClient.ReceiveAsync();
    var deleteResponse = CoapSerializer.Deserialize(receiveResult.Buffer);
    Console.WriteLine($"[CoAP] 收到响应: {deleteResponse.Code} ({(deleteResponse.Code == CoapCode.Deleted ? "已删除" : "失败")})");

    // 5. GET 请求验证删除
    Console.WriteLine("[CoAP] 发送 GET 请求 (验证删除)...");
    var verifyRequest = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Get,
        MessageId = 1005,
        Token = new byte[] { 0x05 }
    };
    verifyRequest.SetUriPath($"{options.CoapMqttPrefix}/test/coap");

    var verifyData = new byte[CoapSerializer.CalculateSize(verifyRequest)];
    CoapSerializer.Serialize(verifyRequest, verifyData);
    await udpClient.SendAsync(verifyData, serverEndpoint);

    // 接收响应
    receiveResult = await udpClient.ReceiveAsync();
    var verifyResponse = CoapSerializer.Deserialize(receiveResult.Buffer);
    Console.WriteLine($"[CoAP] 收到响应: {verifyResponse.Code} ({(verifyResponse.Code == CoapCode.NotFound ? "资源已删除" : "意外响应")})");

    Console.WriteLine("[CoAP] 测试完成！");
}
