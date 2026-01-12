using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.MQTT.CoAP.Protocol;
using System.Net.MQTT.CoAP.Serialization;

Console.WriteLine("=== CoAP Client ===\n");

var serverHost = "127.0.0.1";
var serverPort = 5683;
var mqttPrefix = "mqtt";

using var udpClient = new UdpClient();
var serverEndpoint = new IPEndPoint(IPAddress.Parse(serverHost), serverPort);

ushort messageId = 1;

Console.WriteLine($"服务器: {serverHost}:{serverPort}");
Console.WriteLine($"URI 前缀: /{mqttPrefix}/\n");

while (true)
{
    Console.WriteLine("--- 选择操作 ---");
    Console.WriteLine("1. PUT  - 发布消息（保留）");
    Console.WriteLine("2. GET  - 获取保留消息");
    Console.WriteLine("3. GET+Observe - 订阅主题");
    Console.WriteLine("4. DELETE - 删除保留消息");
    Console.WriteLine("5. 自定义请求");
    Console.WriteLine("0. 退出");
    Console.Write("\n请选择: ");

    var choice = Console.ReadLine()?.Trim();

    if (choice == "0") break;

    try
    {
        switch (choice)
        {
            case "1":
                await DoPutAsync();
                break;
            case "2":
                await DoGetAsync();
                break;
            case "3":
                await DoObserveAsync();
                break;
            case "4":
                await DoDeleteAsync();
                break;
            case "5":
                await DoCustomAsync();
                break;
            default:
                Console.WriteLine("无效选择\n");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}\n");
    }
}

Console.WriteLine("再见！");

// PUT - 发布消息
async Task DoPutAsync()
{
    Console.Write("输入主题 (如 sensor/temp): ");
    var topic = Console.ReadLine()?.Trim() ?? "test/topic";

    Console.Write("输入消息内容: ");
    var payload = Console.ReadLine() ?? "";

    var request = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Put,
        MessageId = messageId++,
        Token = BitConverter.GetBytes(messageId)
    };
    request.SetUriPath($"{mqttPrefix}/{topic}");
    request.Payload = Encoding.UTF8.GetBytes(payload);
    request.SetContentFormat(CoapContentFormat.TextPlain);

    Console.WriteLine($"\n发送 PUT /{mqttPrefix}/{topic}");
    var response = await SendAndReceiveAsync(request);
    PrintResponse(response);
}

// GET - 获取保留消息
async Task DoGetAsync()
{
    Console.Write("输入主题 (如 sensor/temp): ");
    var topic = Console.ReadLine()?.Trim() ?? "test/topic";

    var request = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Get,
        MessageId = messageId++,
        Token = BitConverter.GetBytes(messageId)
    };
    request.SetUriPath($"{mqttPrefix}/{topic}");

    Console.WriteLine($"\n发送 GET /{mqttPrefix}/{topic}");
    var response = await SendAndReceiveAsync(request);
    PrintResponse(response);
}

// GET + Observe - 订阅主题
async Task DoObserveAsync()
{
    Console.Write("输入主题 (如 sensor/temp): ");
    var topic = Console.ReadLine()?.Trim() ?? "test/topic";

    Console.Write("观察时长(秒, 默认30): ");
    var durationStr = Console.ReadLine()?.Trim();
    var duration = int.TryParse(durationStr, out var d) ? d : 30;

    var request = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Get,
        MessageId = messageId++,
        Token = BitConverter.GetBytes(messageId)
    };
    request.SetUriPath($"{mqttPrefix}/{topic}");
    request.SetObserve(0); // 0 = 注册观察

    Console.WriteLine($"\n发送 GET + Observe /{mqttPrefix}/{topic}");
    Console.WriteLine($"开始观察 {duration} 秒，按任意键提前结束...\n");

    // 发送请求
    var data = new byte[CoapSerializer.CalculateSize(request)];
    CoapSerializer.Serialize(request, data);
    await udpClient.SendAsync(data, serverEndpoint);

    // 设置超时
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration));

    // 启动键盘监听
    var keyTask = Task.Run(() =>
    {
        Console.ReadKey(true);
        cts.Cancel();
    });

    // 接收通知
    var notificationCount = 0;
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var receiveTask = udpClient.ReceiveAsync(cts.Token);
            var result = await receiveTask;
            var response = CoapSerializer.Deserialize(result.Buffer);
            notificationCount++;
            Console.WriteLine($"[通知 #{notificationCount}] Observe={response.GetObserve()}");
            PrintResponse(response);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n观察结束，共收到 {notificationCount} 条通知\n");
    }
}

// DELETE - 删除保留消息
async Task DoDeleteAsync()
{
    Console.Write("输入主题 (如 sensor/temp): ");
    var topic = Console.ReadLine()?.Trim() ?? "test/topic";

    var request = new CoapMessage
    {
        Type = CoapMessageType.Confirmable,
        Code = CoapCode.Delete,
        MessageId = messageId++,
        Token = BitConverter.GetBytes(messageId)
    };
    request.SetUriPath($"{mqttPrefix}/{topic}");

    Console.WriteLine($"\n发送 DELETE /{mqttPrefix}/{topic}");
    var response = await SendAndReceiveAsync(request);
    PrintResponse(response);
}

// 自定义请求
async Task DoCustomAsync()
{
    Console.WriteLine("选择方法: 1=GET, 2=PUT, 3=POST, 4=DELETE");
    Console.Write("方法: ");
    var methodStr = Console.ReadLine()?.Trim();
    var code = methodStr switch
    {
        "1" => CoapCode.Get,
        "2" => CoapCode.Put,
        "3" => CoapCode.Post,
        "4" => CoapCode.Delete,
        _ => CoapCode.Get
    };

    Console.Write("完整 URI 路径 (如 mqtt/sensor/temp): ");
    var path = Console.ReadLine()?.Trim() ?? "mqtt/test";

    Console.Write("消息类型: 1=CON(确认), 2=NON(非确认): ");
    var typeStr = Console.ReadLine()?.Trim();
    var msgType = typeStr == "2" ? CoapMessageType.NonConfirmable : CoapMessageType.Confirmable;

    string? payload = null;
    if (code == CoapCode.Put || code == CoapCode.Post)
    {
        Console.Write("载荷内容 (可选): ");
        payload = Console.ReadLine();
    }

    var request = new CoapMessage
    {
        Type = msgType,
        Code = code,
        MessageId = messageId++,
        Token = BitConverter.GetBytes(messageId)
    };
    request.SetUriPath(path);

    if (!string.IsNullOrEmpty(payload))
    {
        request.Payload = Encoding.UTF8.GetBytes(payload);
        request.SetContentFormat(CoapContentFormat.TextPlain);
    }

    Console.WriteLine($"\n发送 {code} /{path} ({msgType})");
    var response = await SendAndReceiveAsync(request);
    PrintResponse(response);
}

// 发送请求并接收响应
async Task<CoapMessage> SendAndReceiveAsync(CoapMessage request)
{
    var data = new byte[CoapSerializer.CalculateSize(request)];
    CoapSerializer.Serialize(request, data);
    await udpClient.SendAsync(data, serverEndpoint);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var result = await udpClient.ReceiveAsync(cts.Token);
    return CoapSerializer.Deserialize(result.Buffer);
}

// 打印响应
void PrintResponse(CoapMessage response)
{
    var codeClass = ((byte)response.Code >> 5) & 0x07;
    var codeDetail = (byte)response.Code & 0x1F;

    Console.WriteLine($"响应: {codeClass}.{codeDetail:D2} ({GetCodeName(response.Code)})");
    Console.WriteLine($"类型: {response.Type}");

    if (response.Payload.Length > 0)
    {
        var payloadStr = Encoding.UTF8.GetString(response.Payload);
        Console.WriteLine($"载荷: {payloadStr}");
    }

    Console.WriteLine();
}

string GetCodeName(CoapCode code)
{
    if (code == CoapCode.Created) return "Created";
    if (code == CoapCode.Deleted) return "Deleted";
    if (code == CoapCode.Valid) return "Valid";
    if (code == CoapCode.Changed) return "Changed";
    if (code == CoapCode.Content) return "Content";
    if (code == CoapCode.BadRequest) return "Bad Request";
    if (code == CoapCode.Unauthorized) return "Unauthorized";
    if (code == CoapCode.BadOption) return "Bad Option";
    if (code == CoapCode.Forbidden) return "Forbidden";
    if (code == CoapCode.NotFound) return "Not Found";
    if (code == CoapCode.MethodNotAllowed) return "Method Not Allowed";
    if (code == CoapCode.NotAcceptable) return "Not Acceptable";
    if (code == CoapCode.PreconditionFailed) return "Precondition Failed";
    if (code == CoapCode.RequestEntityTooLarge) return "Request Entity Too Large";
    if (code == CoapCode.UnsupportedContentFormat) return "Unsupported Content Format";
    if (code == CoapCode.InternalServerError) return "Internal Server Error";
    if (code == CoapCode.NotImplemented) return "Not Implemented";
    if (code == CoapCode.BadGateway) return "Bad Gateway";
    if (code == CoapCode.ServiceUnavailable) return "Service Unavailable";
    if (code == CoapCode.GatewayTimeout) return "Gateway Timeout";
    if (code == CoapCode.ProxyingNotSupported) return "Proxying Not Supported";
    return "Unknown";
}
