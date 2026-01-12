using System.Buffers;
using MQTTnet;
using MQTTnet.Protocol;

namespace MqttNetTest;

public static class RetainTest
{
    public static async Task RunAsync(string host, int port)
    {
        Console.WriteLine("保留消息测试");
        Console.WriteLine("=============");
        Console.WriteLine($"服务器: {host}:{port}");
        Console.WriteLine();

        var factory = new MqttClientFactory();

        // 步骤 1: 发布保留消息
        Console.WriteLine("步骤 1: 发布保留消息...");
        var publisher = factory.CreateMqttClient();
        var pubOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("retain-test-publisher")
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithCleanStart(true)
            .Build();

        await publisher.ConnectAsync(pubOptions);
        Console.WriteLine("发布者已连接");

        // 发布保留消息
        var retainedMsg = new MqttApplicationMessageBuilder()
            .WithTopic("retain/test")
            .WithPayload("这是保留消息内容")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(true)  // 设置保留标志
            .Build();

        await publisher.PublishAsync(retainedMsg);
        Console.WriteLine("已发布保留消息到 retain/test");

        await publisher.DisconnectAsync();
        Console.WriteLine("发布者已断开");
        Console.WriteLine();

        // 等待一下确保消息已处理
        await Task.Delay(500);

        // 步骤 2: 新订阅者连接并订阅
        Console.WriteLine("步骤 2: 新订阅者连接并订阅...");
        var subscriber = factory.CreateMqttClient();
        var receivedRetained = false;
        var receivedTopic = "";
        var receivedPayload = "";
        var receivedRetainFlag = false;

        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            receivedRetained = true;
            receivedTopic = e.ApplicationMessage.Topic;
            receivedPayload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            receivedRetainFlag = e.ApplicationMessage.Retain;
            Console.WriteLine($"[收到消息] 主题: {receivedTopic}");
            Console.WriteLine($"[收到消息] 内容: {receivedPayload}");
            Console.WriteLine($"[收到消息] Retain: {receivedRetainFlag}");
            return Task.CompletedTask;
        };

        var subOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("retain-test-subscriber")
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithCleanStart(true)
            .Build();

        await subscriber.ConnectAsync(subOptions);
        Console.WriteLine("订阅者已连接");

        await subscriber.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("retain/#", MqttQualityOfServiceLevel.AtMostOnce)
            .Build());
        Console.WriteLine("已订阅 retain/#");

        // 等待接收保留消息
        Console.WriteLine("等待接收保留消息...");
        await Task.Delay(2000);

        await subscriber.DisconnectAsync();
        Console.WriteLine();

        // 步骤 3: 清除保留消息
        Console.WriteLine("步骤 3: 清除保留消息...");
        await publisher.ConnectAsync(pubOptions);

        var clearMsg = new MqttApplicationMessageBuilder()
            .WithTopic("retain/test")
            .WithPayload(Array.Empty<byte>())  // 空载荷清除保留消息
            .WithRetainFlag(true)
            .Build();

        await publisher.PublishAsync(clearMsg);
        Console.WriteLine("已发送空保留消息清除");
        await publisher.DisconnectAsync();
        Console.WriteLine();

        // 结果
        Console.WriteLine("========== 测试结果 ==========");
        if (receivedRetained)
        {
            Console.WriteLine($"收到保留消息: 是");
            Console.WriteLine($"主题正确: {receivedTopic == "retain/test"}");
            Console.WriteLine($"内容正确: {receivedPayload == "这是保留消息内容"}");
            Console.WriteLine($"Retain 标志: {receivedRetainFlag}");

            if (receivedTopic == "retain/test" && receivedPayload == "这是保留消息内容")
            {
                Console.WriteLine("测试通过！保留消息功能正常");
            }
            else
            {
                Console.WriteLine("测试失败：内容不匹配");
            }
        }
        else
        {
            Console.WriteLine("测试失败：未收到保留消息");
        }
    }
}
