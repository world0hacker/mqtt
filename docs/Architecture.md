# System.Net.MQTT 项目架构文档

## 项目概述

System.Net.MQTT 是一个高性能、轻量级的 MQTT 客户端和 Broker 库，支持 .NET 6.0、.NET 8.0 和 .NET 10.0。采用分层架构，支持 MQTT 3.1.1 和 MQTT 5.0 协议。

---

## 目录结构

```
D:\test\MQTT\
├── src/
│   ├── System.Net.MQTT/                 # 核心客户端库
│   └── System.Net.MQTT.Broker/          # Broker 服务器库
├── samples/
│   ├── MqttClient.Sample/               # 客户端示例
│   └── MqttBroker.Sample/               # Broker 示例
├── docs/                                # 文档
├── MQTT.slnx                            # 解决方案文件
└── README.md                            # 项目说明
```

---

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              应用层                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  MqttClient.Sample              MqttBroker.Sample                           │
│  (客户端应用示例)                 (服务器应用示例)                             │
└───────────────┬─────────────────────────────────┬───────────────────────────┘
                │                                 │
                ▼                                 ▼
┌───────────────────────────────┐   ┌─────────────────────────────────────────┐
│     System.Net.MQTT           │   │     System.Net.MQTT.Broker              │
│     (客户端核心库)              │   │     (服务器核心库)                       │
├───────────────────────────────┤   ├─────────────────────────────────────────┤
│  MqttClient                   │   │  MqttBroker                             │
│  - ConnectAsync()             │   │  - StartAsync()                         │
│  - PublishAsync()             │   │  - StopAsync()                          │
│  - SubscribeAsync()           │   │  - 客户端会话管理                         │
│  - DisconnectAsync()          │   │  - 消息路由分发                           │
└───────────────┬───────────────┘   └───────────────────┬─────────────────────┘
                │                                       │
                └───────────────────┬───────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          协议抽象层 (Serialization)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  IMqttProtocolHandler (协议处理器接口)                                        │
│  ├── ConnectParser / ConnectBuilder                                         │
│  ├── PublishParser / PublishBuilder                                         │
│  ├── SubscribeParser / SubscribeBuilder                                     │
│  ├── PubAckParser / PubAckBuilder                                           │
│  └── ... 其他报文解析器/构建器                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  MqttProtocolHandlerFactory (工厂类)                                         │
│  └── GetHandler(version) → V311ProtocolHandler / V500ProtocolHandler        │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │                                 │
                    ▼                                 ▼
┌───────────────────────────────┐   ┌─────────────────────────────────────────┐
│   V311 (MQTT 3.1.1)           │   │   V500 (MQTT 5.0)                       │
├───────────────────────────────┤   ├─────────────────────────────────────────┤
│  V311ProtocolHandler          │   │  V500ProtocolHandler                    │
│  V311ConnectPacketParser      │   │  V500ConnectPacketParser                │
│  V311ConnectPacketBuilder     │   │  V500ConnectPacketBuilder               │
│  V311PublishPacketParser      │   │  V500PublishPacketParser                │
│  V311PublishPacketBuilder     │   │  V500PublishPacketBuilder               │
│  ... 其他解析器/构建器          │   │  V500PropertyParser (属性解析)           │
│                               │   │  V500PropertyBuilder (属性构建)          │
│  SupportsAuth = false         │   │  V500AuthPacketParser (AUTH 报文)       │
│  SupportsProperties = false   │   │  SupportsAuth = true                    │
│                               │   │  SupportsProperties = true              │
└───────────────────────────────┘   └─────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          报文数据层 (Protocol)                               │
├─────────────────────────────────────────────────────────────────────────────┤
│  Protocol/Packets/                    Protocol/Properties/                  │
│  ├── MqttConnectPacket                ├── MqttConnectProperties            │
│  ├── MqttConnAckPacket                ├── MqttPublishProperties             │
│  ├── MqttPublishPacket                ├── MqttSubscribeProperties           │
│  ├── MqttPubAckPacket                 ├── MqttUserProperty                  │
│  ├── MqttSubscribePacket              └── ... (MQTT 5.0 属性类)             │
│  ├── MqttSubAckPacket                                                       │
│  ├── MqttUnsubscribePacket                                                  │
│  ├── MqttDisconnectPacket                                                   │
│  ├── MqttAuthPacket (V5.0)                                                  │
│  └── MqttPingReqPacket/PingRespPacket                                       │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          公共工具层 (Common)                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  MqttBinaryReader (ref struct)  - 高性能二进制读取器                          │
│  MqttBinaryWriter (ref struct)  - 高性能二进制写入器                          │
│  MqttPingPacketHandler          - PING 报文处理器                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 核心组件详解

### 1. 客户端库 (System.Net.MQTT)

#### 1.1 公开接口

| 类/接口 | 说明 |
|---------|------|
| `IMqttClient` | 客户端接口定义 |
| `MqttClient` | 客户端实现（约 960 行） |
| `MqttClientOptions` | 客户端配置选项 |
| `MqttApplicationMessage` | 应用消息模型 |

#### 1.2 枚举类型

| 枚举 | 说明 |
|------|------|
| `MqttPacketType` | 报文类型（1-15） |
| `MqttProtocolVersion` | 协议版本（V310/V311/V500） |
| `MqttQualityOfService` | QoS 等级（0/1/2） |

#### 1.3 事件系统

```csharp
public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;
public event EventHandler<MqttConnectedEventArgs>? Connected;
public event EventHandler<MqttDisconnectedEventArgs>? Disconnected;
```

---

### 2. Broker 库 (System.Net.MQTT.Broker)

#### 2.1 核心类

| 类 | 说明 |
|----|------|
| `MqttBroker` | Broker 服务器实现 |
| `MqttBrokerOptions` | Broker 配置选项 |
| `MqttClientSession` | 客户端会话表示 |
| `MqttBrokerEventDispatcher` | 事件分发器 |

#### 2.2 认证授权

| 接口/类 | 说明 |
|---------|------|
| `IMqttAuthenticator` | 认证接口 |
| `IMqttAuthorizer` | 授权接口 |
| `AllowAllAuthenticator` | 允许所有连接 |
| `SimpleAuthenticator` | 用户名密码认证 |

#### 2.3 事件系统

```csharp
public event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
public event EventHandler<MqttClientDisconnectedEventArgs>? ClientDisconnected;
public event EventHandler<MqttMessagePublishingEventArgs>? MessagePublishing;
public event EventHandler<MqttMessagePublishedEventArgs>? MessagePublished;
public event EventHandler<MqttClientSubscribingEventArgs>? ClientSubscribing;
public event EventHandler<MqttClientSubscribedEventArgs>? ClientSubscribed;
```

---

### 3. 协议处理器架构

#### 3.1 工厂模式

```csharp
// 获取协议处理器
var handler = MqttProtocolHandlerFactory.GetHandler(MqttProtocolVersion.V500);

// 使用处理器构建报文
var connectPacket = handler.ConnectBuilder.CreateFromOptions(options);

// 使用处理器解析报文
var packet = handler.ParsePacket(packetType, flags, payload);
```

#### 3.2 IMqttProtocolHandler 接口

```csharp
public interface IMqttProtocolHandler
{
    // 版本信息
    MqttProtocolVersion ProtocolVersion { get; }
    bool SupportsAuth { get; }           // V311=false, V500=true
    bool SupportsProperties { get; }     // V311=false, V500=true

    // 解析器
    IConnectPacketParser ConnectParser { get; }
    IConnAckPacketParser ConnAckParser { get; }
    IPublishPacketParser PublishParser { get; }
    IPubAckPacketParser PubAckParser { get; }
    ISubscribePacketParser SubscribeParser { get; }
    ISubAckPacketParser SubAckParser { get; }
    IUnsubscribePacketParser UnsubscribeParser { get; }
    IUnsubAckPacketParser UnsubAckParser { get; }
    IDisconnectPacketParser DisconnectParser { get; }
    IAuthPacketParser? AuthParser { get; }  // V311=null

    // 构建器
    IConnectPacketBuilder ConnectBuilder { get; }
    IConnAckPacketBuilder ConnAckBuilder { get; }
    IPublishPacketBuilder PublishBuilder { get; }
    IPubAckPacketBuilder PubAckBuilder { get; }
    ISubscribePacketBuilder SubscribeBuilder { get; }
    ISubAckPacketBuilder SubAckBuilder { get; }
    IUnsubscribePacketBuilder UnsubscribeBuilder { get; }
    IUnsubAckPacketBuilder UnsubAckBuilder { get; }
    IDisconnectPacketBuilder DisconnectBuilder { get; }
    IAuthPacketBuilder? AuthBuilder { get; }  // V311=null

    // PING 处理器
    IPingPacketHandler PingHandler { get; }

    // 高层方法
    IMqttPacket ParsePacket(MqttPacketType type, byte flags, ReadOnlySpan<byte> data);
    void WritePacket(IMqttPacket packet, IBufferWriter<byte> writer);
}
```

#### 3.3 版本差异对比

| 功能 | MQTT 3.1.1 | MQTT 5.0 |
|------|-----------|----------|
| 属性支持 | 否 | 是 |
| AUTH 报文 | 不支持 | 支持 |
| 原因码 | 有限 | 完整 |
| 用户属性 | 否 | 是 |
| 消息过期 | 否 | 是 |
| 订阅标识符 | 否 | 是 |

---

## 数据流程图

### 1. 客户端连接流程

```
┌──────────┐                    ┌──────────┐                    ┌──────────┐
│  应用层   │                    │ MqttClient│                    │  Broker  │
└────┬─────┘                    └────┬─────┘                    └────┬─────┘
     │                               │                               │
     │  ConnectAsync()               │                               │
     │──────────────────────────────►│                               │
     │                               │                               │
     │                               │  1. 初始化协议处理器            │
     │                               │  _protocolHandler =           │
     │                               │    Factory.GetHandler(V5)     │
     │                               │                               │
     │                               │  2. 建立 TCP 连接              │
     │                               │──────────────────────────────►│
     │                               │◄──────────────────────────────│
     │                               │                               │
     │                               │  3. TLS 握手（可选）           │
     │                               │◄─────────────────────────────►│
     │                               │                               │
     │                               │  4. 构建 CONNECT 报文          │
     │                               │  var packet = handler         │
     │                               │    .ConnectBuilder            │
     │                               │    .CreateFromOptions(opts)   │
     │                               │                               │
     │                               │  5. 发送 CONNECT               │
     │                               │──────────────────────────────►│
     │                               │                               │
     │                               │  6. 接收 CONNACK               │
     │                               │◄──────────────────────────────│
     │                               │                               │
     │                               │  7. 解析 CONNACK               │
     │                               │  handler.ParsePacket(...)     │
     │                               │                               │
     │                               │  8. 启动接收循环               │
     │                               │  StartReceiveLoop()           │
     │                               │                               │
     │                               │  9. 启动保活定时器             │
     │                               │  StartKeepAlive()             │
     │                               │                               │
     │  返回 MqttConnectResult       │                               │
     │◄──────────────────────────────│                               │
     │                               │                               │
```

### 2. 消息发布流程 (QoS 1)

```
┌──────────┐                    ┌──────────┐                    ┌──────────┐
│  应用层   │                    │ MqttClient│                    │  Broker  │
└────┬─────┘                    └────┬─────┘                    └────┬─────┘
     │                               │                               │
     │  PublishAsync(message)        │                               │
     │──────────────────────────────►│                               │
     │                               │                               │
     │                               │  1. 获取报文 ID                │
     │                               │  packetId = GetNextPacketId() │
     │                               │                               │
     │                               │  2. 构建 PUBLISH 报文          │
     │                               │  var packet = handler         │
     │                               │    .PublishBuilder            │
     │                               │    .CreateFromMessage(msg,id) │
     │                               │                               │
     │                               │  3. 发送 PUBLISH               │
     │                               │──────────────────────────────►│
     │                               │                               │
     │                               │  4. 等待确认                   │
     │                               │  WaitForPacketAsync(packetId) │
     │                               │                               │
     │                               │  5. 接收 PUBACK                │
     │                               │◄──────────────────────────────│
     │                               │                               │
     │                               │  6. 完成等待任务               │
     │                               │  HandleAcknowledgement()      │
     │                               │                               │
     │  返回                         │                               │
     │◄──────────────────────────────│                               │
     │                               │                               │
```

### 3. 消息接收流程

```
┌──────────┐                    ┌──────────┐                    ┌──────────┐
│  应用层   │                    │ MqttClient│                    │  Broker  │
└────┬─────┘                    └────┬─────┘                    └────┬─────┘
     │                               │                               │
     │                               │  ReceiveLoopAsync()           │
     │                               │  (后台任务持续运行)             │
     │                               │                               │
     │                               │  1. 接收报文                   │
     │                               │◄──────────────────────────────│
     │                               │     PUBLISH (QoS 1)           │
     │                               │                               │
     │                               │  2. 解析报文类型               │
     │                               │  packetType = packet[0] >> 4  │
     │                               │                               │
     │                               │  3. 处理 PUBLISH               │
     │                               │  HandlePublishAsync()         │
     │                               │                               │
     │  触发 MessageReceived 事件    │                               │
     │◄──────────────────────────────│                               │
     │                               │                               │
     │                               │  4. 发送 PUBACK                │
     │                               │  var pubAck = handler         │
     │                               │    .PubAckBuilder             │
     │                               │    .CreatePubAck(packetId)    │
     │                               │──────────────────────────────►│
     │                               │                               │
```

### 4. Broker 消息路由流程

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│ Client A │     │  Broker  │     │ Session B│     │ Client B │
└────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │                │
     │  PUBLISH       │                │                │
     │  topic: "test" │                │                │
     │───────────────►│                │                │
     │                │                │                │
     │                │  1. 认证/授权检查               │
     │                │  authorizer.CanPublishAsync()  │
     │                │                │                │
     │                │  2. 触发 MessagePublishing 事件 │
     │                │                │                │
     │                │  3. 查找订阅者                  │
     │                │  foreach session in _sessions  │
     │                │    if session.IsSubscribed()   │
     │                │                │                │
     │                │  4. 转发消息   │                │
     │                │───────────────►│                │
     │                │                │                │
     │                │                │  5. 发送给客户端
     │                │                │───────────────►│
     │                │                │                │
     │                │  6. 处理保留消息               │
     │                │  if (retain)                    │
     │                │    _retainedMessages[topic]=msg │
     │                │                │                │
     │                │  7. 触发 MessagePublished 事件  │
     │                │                │                │
     │  PUBACK        │                │                │
     │◄───────────────│                │                │
     │                │                │                │
```

---

## 性能优化技术

### 1. 内存优化

```csharp
// ArrayPool 减少内存分配
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // 使用缓冲区
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ref struct 避免堆分配
public ref struct MqttBinaryWriter
{
    private readonly IBufferWriter<byte> _writer;
    // ...
}
```

### 2. 方法内联

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private ushort GetNextPacketId()
{
    return ++_packetId == 0 ? ++_packetId : _packetId;
}

[MethodImpl(MethodImplOptions.AggressiveOptimization)]
private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
{
    // 热路径优化
}
```

### 3. 零拷贝操作

```csharp
// 使用 Span<T> 避免数组复制
Span<byte> remainingLengthBytes = stackalloc byte[4];
var size = EncodeRemainingLength(length, remainingLengthBytes);

// 使用 Memory<T> 进行异步操作
await stream.WriteAsync(packet.AsMemory(), cancellationToken);
```

### 4. 两条解析路径

```csharp
// 小报文优化路径 - 直接栈处理
TPacket Parse(ReadOnlySpan<byte> data, byte flags);

// 大报文路径 - 处理跨段数据
TPacket Parse(ReadOnlySequence<byte> data, byte flags);
```

---

## 文件清单

### 客户端库 (System.Net.MQTT)

```
src/System.Net.MQTT/
├── 核心类
│   ├── IMqttClient.cs
│   ├── MqttClient.cs (960 行)
│   ├── MqttClientOptions.cs
│   ├── MqttApplicationMessage.cs
│   ├── MqttConnectResult.cs
│   ├── MqttEventArgs.cs
│   ├── MqttSubscription.cs
│   └── MqttExceptions.cs
│
├── 枚举
│   ├── MqttPacketType.cs
│   ├── MqttProtocolVersion.cs
│   └── MqttQualityOfService.cs
│
├── Protocol/
│   ├── IMqttPacket.cs
│   ├── Packets/ (14 个报文类)
│   ├── Properties/ (13 个属性类)
│   └── ReasonCodes/ (原因码定义)
│
└── Serialization/
    ├── IMqttProtocolHandler.cs
    ├── MqttProtocolHandlerFactory.cs
    ├── Common/ (工具类)
    ├── Interfaces/ (22 个接口)
    ├── V311/ (18 个解析器/构建器)
    └── V500/ (22 个解析器/构建器 + 属性处理)
```

### Broker 库 (System.Net.MQTT.Broker)

```
src/System.Net.MQTT.Broker/
├── MqttBroker.cs
├── MqttBrokerOptions.cs
├── MqttClientSession.cs
├── MqttBrokerEventDispatcher.cs
├── MqttBrokerEventArgs.cs
└── MqttAuthentication.cs
```

---

## 使用示例

### 客户端示例

```csharp
// 创建客户端
var options = new MqttClientOptions
{
    Host = "localhost",
    Port = 1883,
    ClientId = "test-client",
    ProtocolVersion = MqttProtocolVersion.V500,
    KeepAliveSeconds = 60
};

using var client = new MqttClient(options);

// 注册事件
client.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"收到消息: {e.Message.Topic} = {e.Message.PayloadString}");
};

// 连接
await client.ConnectAsync();

// 订阅
await client.SubscribeAsync("test/topic", MqttQualityOfService.AtLeastOnce);

// 发布
await client.PublishAsync("test/topic", "Hello MQTT!", MqttQualityOfService.AtLeastOnce);

// 断开
await client.DisconnectAsync();
```

### Broker 示例

```csharp
// 创建 Broker
var options = new MqttBrokerOptions
{
    Port = 1883,
    EnableTls = false
};

var broker = new MqttBroker(options);

// 设置认证
broker.Authenticator = new SimpleAuthenticator(new Dictionary<string, string>
{
    { "user1", "password1" }
});

// 注册事件
broker.ClientConnected += (sender, e) =>
{
    Console.WriteLine($"客户端连接: {e.ClientId}");
};

broker.MessagePublished += (sender, e) =>
{
    Console.WriteLine($"消息发布: {e.Topic}");
};

// 启动
await broker.StartAsync();
```

---

## 扩展指南

### 添加新协议版本（如 MQTT 6.0）

1. 创建 `Serialization/V600/` 目录
2. 实现 `V600ProtocolHandler : IMqttProtocolHandler`
3. 为每种报文类型实现解析器和构建器
4. 在 `MqttProtocolHandlerFactory` 中添加版本映射
5. 在 `MqttProtocolVersion` 枚举中添加新版本

### 添加自定义认证

```csharp
public class CustomAuthenticator : IMqttAuthenticator
{
    public async Task<MqttAuthenticationResult> AuthenticateAsync(
        MqttAuthenticationContext context,
        CancellationToken cancellationToken)
    {
        // 自定义认证逻辑
        if (await ValidateCredentialsAsync(context.Username, context.Password))
        {
            return MqttAuthenticationResult.Success();
        }
        return MqttAuthenticationResult.Failure(MqttReasonCode.BadUserNameOrPassword);
    }
}
```

---

## 总结

System.Net.MQTT 采用分层、模块化架构：

1. **协议层**：通过 `IMqttProtocolHandler` 抽象，支持 MQTT 3.1.1 和 5.0
2. **序列化层**：统一的解析器/构建器接口，版本特定实现
3. **客户端层**：高性能异步实现，事件驱动
4. **服务器层**：完整的 Broker 功能，支持认证授权
5. **性能优化**：ArrayPool、ref struct、内联、零拷贝

关键设计原则：**高性能 + 低延迟 + 零不必要分配 + 版本可扩展**
