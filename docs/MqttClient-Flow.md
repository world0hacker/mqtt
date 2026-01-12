# MqttClient 整体流程文档

## 概述

`MqttClient` 是 MQTT 客户端的核心实现类，支持 MQTT 3.1.1 和 MQTT 5.0 协议。通过 `IMqttProtocolHandler` 抽象层实现协议版本无关的报文处理。

## 架构设计

```
┌─────────────────────────────────────────────────────────────────┐
│                         MqttClient                               │
├─────────────────────────────────────────────────────────────────┤
│  公开接口：                                                      │
│  - ConnectAsync()     连接到 MQTT 服务器                        │
│  - DisconnectAsync()  断开连接                                   │
│  - PublishAsync()     发布消息                                   │
│  - SubscribeAsync()   订阅主题                                   │
│  - UnsubscribeAsync() 取消订阅                                   │
├─────────────────────────────────────────────────────────────────┤
│  内部组件：                                                      │
│  - _protocolHandler   协议处理器（V311/V500）                    │
│  - _stream            网络流（TCP/TLS）                          │
│  - _pendingPackets    等待响应的报文字典                         │
│  - _keepAliveTimer    保活定时器                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              IMqttProtocolHandler (协议抽象层)                   │
├─────────────────────────────────────────────────────────────────┤
│  - ConnectBuilder     构建 CONNECT 报文                         │
│  - DisconnectBuilder  构建 DISCONNECT 报文                      │
│  - PublishBuilder     构建 PUBLISH 报文                         │
│  - SubscribeBuilder   构建 SUBSCRIBE 报文                       │
│  - UnsubscribeBuilder 构建 UNSUBSCRIBE 报文                     │
│  - PubAckBuilder      构建 PUBACK/PUBREC/PUBREL/PUBCOMP 报文   │
│  - PingHandler        处理 PINGREQ/PINGRESP                     │
│  - ParsePacket()      解析报文                                   │
│  - WritePacket()      序列化报文                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 连接流程 (ConnectAsync)

```
客户端                                服务器
   │                                    │
   │  1. TCP 连接                       │
   │ ─────────────────────────────────► │
   │                                    │
   │  2. TLS 握手（可选）                │
   │ ◄────────────────────────────────► │
   │                                    │
   │  3. CONNECT 报文                   │
   │    - 协议名称/版本                  │
   │    - 客户端 ID                      │
   │    - 用户名/密码（可选）            │
   │    - 遗嘱消息（可选）               │
   │    - 保活间隔                       │
   │ ─────────────────────────────────► │
   │                                    │
   │  4. CONNACK 报文                   │
   │    - 连接返回码                     │
   │    - 会话存在标志                   │
   │ ◄───────────────────────────────── │
   │                                    │
   │  5. 启动接收循环                    │
   │  6. 启动保活定时器                  │
   │                                    │
```

### 代码流程

```csharp
ConnectAsync()
    │
    ├─► 初始化协议处理器
    │   _protocolHandler = MqttProtocolHandlerFactory.GetHandler(Options.ProtocolVersion)
    │
    ├─► 建立 TCP 连接
    │   await _tcpClient.ConnectAsync(host, port)
    │
    ├─► TLS 握手（如果启用）
    │   await sslStream.AuthenticateAsClientAsync()
    │
    ├─► 构建并发送 CONNECT 报文
    │   var connectPacket = _protocolHandler.ConnectBuilder.CreateFromOptions(Options)
    │   await SendPacketAsync(connectPacket)
    │
    ├─► 接收并解析 CONNACK
    │   var (packetType, flags, payload) = await ReceiveAndParsePacketAsync()
    │   var connAckPacket = _protocolHandler.ParsePacket(packetType, flags, payload)
    │
    ├─► 启动接收循环
    │   StartReceiveLoop()
    │
    └─► 启动保活定时器
        StartKeepAlive()
```

## 发布流程 (PublishAsync)

### QoS 0: 最多一次

```
客户端                                服务器
   │                                    │
   │  PUBLISH (QoS=0)                   │
   │ ─────────────────────────────────► │
   │                                    │
   │  （无确认）                         │
   │                                    │
```

### QoS 1: 至少一次

```
客户端                                服务器
   │                                    │
   │  PUBLISH (QoS=1)                   │
   │ ─────────────────────────────────► │
   │                                    │
   │  PUBACK                            │
   │ ◄───────────────────────────────── │
   │                                    │
```

### QoS 2: 恰好一次

```
客户端                                服务器
   │                                    │
   │  PUBLISH (QoS=2)                   │
   │ ─────────────────────────────────► │
   │                                    │
   │  PUBREC                            │
   │ ◄───────────────────────────────── │
   │                                    │
   │  PUBREL                            │
   │ ─────────────────────────────────► │
   │                                    │
   │  PUBCOMP                           │
   │ ◄───────────────────────────────── │
   │                                    │
```

### 代码流程

```csharp
PublishAsync(message)
    │
    ├─► 获取报文 ID（QoS > 0）
    │   packetId = GetNextPacketId()
    │
    ├─► 构建并发送 PUBLISH 报文
    │   var packet = _protocolHandler.PublishBuilder.CreateFromMessage(message, packetId)
    │   await SendPacketAsync(packet)
    │
    └─► 等待确认（QoS 1/2）
        await WaitForPacketAsync(packetId)
```

## 订阅流程 (SubscribeAsync)

```
客户端                                服务器
   │                                    │
   │  SUBSCRIBE                         │
   │    - 报文 ID                        │
   │    - 主题过滤器列表                 │
   │    - QoS 列表                       │
   │ ─────────────────────────────────► │
   │                                    │
   │  SUBACK                            │
   │    - 报文 ID                        │
   │    - 订阅结果码列表                 │
   │ ◄───────────────────────────────── │
   │                                    │
```

### 代码流程

```csharp
SubscribeAsync(subscriptions)
    │
    ├─► 获取报文 ID
    │   packetId = GetNextPacketId()
    │
    ├─► 构建并发送 SUBSCRIBE 报文
    │   var packet = _protocolHandler.SubscribeBuilder.Create(packetId, subscriptions)
    │   await SendPacketAsync(packet)
    │
    └─► 等待 SUBACK 响应
        var response = await WaitForPacketAsync(packetId)
```

## 接收循环 (ReceiveLoopAsync)

```
                    ┌──────────────────┐
                    │   ReceiveLoop    │
                    │    启动          │
                    └────────┬─────────┘
                             │
                             ▼
               ┌─────────────────────────┐
               │  等待接收报文            │◄─────────────────┐
               │  ReceivePacketAsync()   │                  │
               └────────────┬────────────┘                  │
                            │                               │
                            ▼                               │
               ┌─────────────────────────┐                  │
               │  解析报文类型            │                  │
               │  packetType = packet[0] │                  │
               └────────────┬────────────┘                  │
                            │                               │
            ┌───────────────┼───────────────┐               │
            │               │               │               │
            ▼               ▼               ▼               │
     ┌──────────┐    ┌──────────┐    ┌──────────┐          │
     │ PUBLISH  │    │ PUBACK   │    │ PINGRESP │          │
     │          │    │ PUBREC   │    │          │          │
     │          │    │ PUBCOMP  │    │          │          │
     │          │    │ SUBACK   │    │          │          │
     │          │    │ UNSUBACK │    │          │          │
     └────┬─────┘    └────┬─────┘    └────┬─────┘          │
          │               │               │                 │
          ▼               ▼               ▼                 │
   ┌────────────┐  ┌────────────┐  ┌────────────┐          │
   │ 触发消息   │  │ 完成等待   │  │   忽略     │          │
   │ 接收事件   │  │ 中的任务   │  │            │          │
   │            │  │            │  │            │          │
   │ 发送确认   │  │            │  │            │          │
   │ (QoS>0)   │  │            │  │            │          │
   └────────────┘  └────────────┘  └────────────┘          │
          │               │               │                 │
          └───────────────┴───────────────┴─────────────────┘
```

### 代码流程

```csharp
ReceiveLoopAsync()
    │
    └─► while (IsConnected)
        │
        ├─► 接收报文
        │   packet = await ReceivePacketAsync()
        │
        └─► 处理报文
            await HandlePacketAsync(packetType, packet)
                │
                ├─► PUBLISH:    HandlePublishAsync()
                │   ├─► 解析主题、QoS、Payload
                │   ├─► 触发 MessageReceived 事件
                │   └─► 发送确认 (PUBACK/PUBREC)
                │
                ├─► PUBACK/PUBREC/PUBCOMP/SUBACK/UNSUBACK:
                │   └─► HandleAcknowledgement()
                │       └─► 完成 _pendingPackets 中等待的 Task
                │
                ├─► PUBREL:     HandlePubRelAsync()
                │   └─► 发送 PUBCOMP
                │
                └─► PINGRESP:   忽略
```

## 断开连接流程 (DisconnectAsync)

```
客户端                                服务器
   │                                    │
   │  DISCONNECT                        │
   │ ─────────────────────────────────► │
   │                                    │
   │  关闭 TCP 连接                     │
   │ ────────────────────────────────── │
   │                                    │
```

### 代码流程

```csharp
DisconnectAsync()
    │
    ├─► 发送 DISCONNECT 报文
    │   var packet = _protocolHandler.DisconnectBuilder.Create()
    │   await SendPacketAsync(packet)
    │
    └─► 清理资源
        await CleanupAsync(clientInitiated: true)
            │
            ├─► 停止保活定时器
            ├─► 取消接收循环
            ├─► 关闭网络流
            ├─► 触发 Disconnected 事件
            └─► 自动重连（如果配置）
```

## 保活机制 (KeepAlive)

```
                    ┌──────────────────┐
                    │   保活定时器      │
                    │   (75% 间隔)     │
                    └────────┬─────────┘
                             │
                             ▼
               ┌─────────────────────────┐
               │  发送 PINGREQ 报文      │
               │  _protocolHandler       │
               │    .PingHandler         │
               │    .GetPingReqBytes()   │
               └────────────┬────────────┘
                            │
                            ▼
               ┌─────────────────────────┐
               │  服务器响应 PINGRESP    │
               │  （在接收循环中忽略）    │
               └─────────────────────────┘
```

## 报文 ID 管理

```csharp
GetNextPacketId()
    │
    └─► 返回下一个报文 ID (1-65535 循环)
        确保 ID 不为 0

WaitForPacketAsync(packetId)
    │
    ├─► 创建 TaskCompletionSource
    ├─► 添加到 _pendingPackets 字典
    ├─► 等待响应（30秒超时）
    └─► 从字典移除
```

## 关键数据结构

### 等待报文字典
```csharp
Dictionary<ushort, TaskCompletionSource<object?>> _pendingPackets
```
- 键: 报文 ID
- 值: 等待响应的 TaskCompletionSource
- 用于 QoS 1/2 的 PUBLISH、SUBSCRIBE、UNSUBSCRIBE

### 发送锁
```csharp
SemaphoreSlim _sendLock
```
- 确保报文发送的原子性
- 防止多线程同时写入网络流

## MQTT 5.0 特性支持

### 属性处理
- CONNECT: 支持会话过期、认证方法等属性
- PUBLISH: 支持消息过期、用户属性等
- SUBSCRIBE/UNSUBSCRIBE: 支持用户属性
- 所有 ACK 报文: 支持原因码和属性

### 属性跳过逻辑
```csharp
// MQTT 5.0: 解码并跳过属性长度
if (isV5)
{
    var propertiesLength = 0;
    var multiplier = 1;
    do
    {
        encodedByte = packet[index++];
        propertiesLength += (encodedByte & 127) * multiplier;
        multiplier *= 128;
    } while ((encodedByte & 128) != 0);

    index += propertiesLength; // 跳过属性内容
}
```

## 错误处理

1. **连接超时**: 使用 CancellationTokenSource 设置超时
2. **流关闭**: EndOfStreamException 触发清理
3. **协议错误**: MqttProtocolException 表示协议违规
4. **报文等待超时**: 30 秒后自动取消

## 性能优化

1. **ArrayPool**: 减少缓冲区分配
2. **Memory/Span**: 零拷贝操作
3. **AggressiveInlining**: 关键路径内联
4. **AggressiveOptimization**: 循环和热路径优化
5. **ConfigureAwait(false)**: 避免同步上下文切换
