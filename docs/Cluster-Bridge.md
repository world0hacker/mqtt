# MQTT Broker 集群与桥接功能文档

## 概述

System.Net.MQTT.Broker 支持两种分布式部署模式：

| 模式 | 架构类型 | 适用场景 |
|------|---------|---------|
| **集群 (Cluster)** | 对等 P2P | 多节点负载均衡、高可用 |
| **桥接 (Bridge)** | 主从层级 | 跨网络、边缘计算、数据聚合 |

---

## 一、集群模式 (Cluster)

### 1.1 架构设计

集群采用**去中心化 P2P 架构**，所有节点地位平等，无主节点依赖。

```
┌─────────────────────────────────────────────────────────────────┐
│                        MQTT 集群                                 │
│                                                                  │
│   ┌──────────┐      TCP:11883       ┌──────────┐                │
│   │ Node-1   │◄────────────────────►│ Node-2   │                │
│   │ :1883    │                      │ :1883    │                │
│   │ (对等)    │                      │ (对等)    │                │
│   └────┬─────┘                      └────┬─────┘                │
│        │                                 │                       │
│        │      TCP:11883                  │                       │
│        └───────────┬─────────────────────┘                       │
│                    │                                             │
│              ┌─────┴─────┐                                       │
│              │  Node-3   │                                       │
│              │  :1883    │                                       │
│              │  (对等)    │                                       │
│              └───────────┘                                       │
│                                                                  │
│  核心特性：                                                       │
│  • 无主节点 - 任何节点都可以下线，不影响其他节点                    │
│  • 消息广播 - 自动转发到所有对等节点                               │
│  • 去重机制 - MessageId + TTL 缓存防止循环转发                    │
│  • 订阅同步 - 集群内共享订阅信息                                   │
│  • 保留消息同步 - 新节点加入时自动同步保留消息                      │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 核心特性

| 特性 | 说明 |
|------|------|
| **无主节点** | 所有节点地位平等，任何节点都可以独立运行 |
| **种子节点** | 仅用于初始发现，不是必须存活的主节点 |
| **自动发现** | 通过种子节点发现其他对等节点 |
| **消息去重** | 基于 MessageId + 时间戳的 TTL 缓存，防止消息循环 |
| **心跳检测** | 定期心跳检测节点健康状态 |
| **自动重连** | 节点断开后可重新加入集群 |
| **订阅同步** | 集群内自动同步订阅信息 |
| **保留消息同步** | 新节点加入时自动同步保留消息 |

### 1.3 消息流程

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│ Client A │     │  Node-1  │     │  Node-2  │     │ Client B │
│          │     │          │     │          │     │          │
└────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │                │
     │  PUBLISH       │                │                │
     │  topic: test   │                │                │
     │───────────────►│                │                │
     │                │                │                │
     │                │  1. 生成 MessageId              │
     │                │  id = hash(topic+payload+node) │
     │                │                │                │
     │                │  2. 缓存 MessageId（去重）      │
     │                │  _cache[id] = now              │
     │                │                │                │
     │                │  3. 广播到 Node-2              │
     │                │  ClusterMessage.Publish        │
     │                │───────────────►│                │
     │                │                │                │
     │                │                │  4. 检查去重   │
     │                │                │  if !cached    │
     │                │                │                │
     │                │                │  5. 发布本地   │
     │                │                │───────────────►│
     │                │                │     PUBLISH    │
     │                │                │                │
     │  PUBACK        │                │                │
     │◄───────────────│                │                │
     │                │                │                │
```

### 1.4 集群协议

集群节点间使用自定义轻量级 TCP 二进制协议通信：

```
消息格式：
┌─────────────┬──────────────┬──────────────────────┐
│ 消息类型 (1B) │ 内容长度 (4B) │ 消息内容 (N Bytes)    │
└─────────────┴──────────────┴──────────────────────┘
```

**消息类型：**

| 类型 | 值 | 说明 |
|------|-----|------|
| Heartbeat | 0x01 | 心跳消息 |
| HandshakeRequest | 0x02 | 握手请求 |
| HandshakeResponse | 0x03 | 握手响应 |
| Publish | 0x10 | MQTT 消息转发 |
| Subscribe | 0x20 | 订阅同步 |
| Unsubscribe | 0x21 | 取消订阅同步 |
| NodeLeave | 0x30 | 节点离开通知 |
| DiscoverRequest | 0x40 | 节点发现请求 |
| DiscoverResponse | 0x41 | 节点发现响应 |
| RetainedSyncRequest | 0x50 | 保留消息同步请求 |
| RetainedSyncData | 0x51 | 保留消息同步数据 |

### 1.5 配置选项

```csharp
public sealed class MqttClusterOptions
{
    /// <summary>节点唯一标识</summary>
    public string NodeId { get; set; }

    /// <summary>集群名称（用于隔离不同集群）</summary>
    public string ClusterName { get; set; } = "default-cluster";

    /// <summary>集群通信端口</summary>
    public int ClusterPort { get; set; } = 11883;

    /// <summary>绑定地址</summary>
    public string BindAddress { get; set; } = "0.0.0.0";

    /// <summary>种子节点列表（格式：host:port）</summary>
    public List<string> SeedNodes { get; set; } = new();

    /// <summary>心跳间隔（毫秒）</summary>
    public int HeartbeatIntervalMs { get; set; } = 5000;

    /// <summary>节点超时时间（毫秒）</summary>
    public int NodeTimeoutMs { get; set; } = 15000;

    /// <summary>是否启用消息去重</summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>消息 ID 缓存过期时间（秒）</summary>
    public int MessageIdCacheExpirySeconds { get; set; } = 60;
}
```

### 1.6 使用示例

#### 启动集群节点

```csharp
// 节点 1 - 第一个节点（无种子节点）
var broker1 = new MqttBroker(new MqttBrokerOptions { Port = 1883 });
broker1.EnableCluster(new MqttClusterOptions
{
    NodeId = "node-1",
    ClusterName = "my-cluster",
    ClusterPort = 11883,
    HeartbeatIntervalMs = 5000,
    NodeTimeoutMs = 15000
});
await broker1.StartAsync();

// 节点 2 - 加入集群
var broker2 = new MqttBroker(new MqttBrokerOptions { Port = 1884 });
broker2.EnableCluster(new MqttClusterOptions
{
    NodeId = "node-2",
    ClusterName = "my-cluster",
    ClusterPort = 11884,
    SeedNodes = { "127.0.0.1:11883" }  // 指向节点 1
});
await broker2.StartAsync();

// 节点 3 - 加入集群
var broker3 = new MqttBroker(new MqttBrokerOptions { Port = 1885 });
broker3.EnableCluster(new MqttClusterOptions
{
    NodeId = "node-3",
    ClusterName = "my-cluster",
    ClusterPort = 11885,
    SeedNodes = { "127.0.0.1:11883" }  // 指向节点 1
});
await broker3.StartAsync();
```

#### 命令行启动

```bash
# 启动节点 1（无种子节点）
dotnet run -- node-1 1883 11883

# 启动节点 2（种子节点指向节点 1）
dotnet run -- node-2 1884 11884 127.0.0.1:11883

# 启动节点 3（种子节点指向节点 1）
dotnet run -- node-3 1885 11885 127.0.0.1:11883
```

#### 监听集群事件

```csharp
broker.Cluster!.PeerJoined += (s, e) =>
{
    Console.WriteLine($"节点加入: {e.Peer.NodeId} ({e.Peer.Address})");
};

broker.Cluster!.PeerLeft += (s, e) =>
{
    Console.WriteLine($"节点离开: {e.Peer.NodeId}");
};

broker.Cluster!.MessageForwarded += (s, e) =>
{
    Console.WriteLine($"消息转发: {e.Topic} (来自 {e.SourceNodeId})");
};

broker.Cluster!.SubscriptionSynced += (s, e) =>
{
    var action = e.IsSubscribe ? "订阅" : "取消订阅";
    Console.WriteLine($"同步{action}: {e.TopicFilter}");
};

broker.Cluster!.RetainedMessagesSynced += (s, e) =>
{
    Console.WriteLine($"保留消息同步: {e.MessageCount} 条 (来自 {e.SourceNodeId})");
};
```

### 1.7 测试场景

**场景 1：消息跨节点分发**
```
1. 客户端 A 连接到 Node-1，订阅 "test/#"
2. 客户端 B 连接到 Node-2，发布消息到 "test/hello"
3. 验证：客户端 A 收到消息
```

**场景 2：节点故障恢复**
```
1. 启动 3 个节点的集群
2. 关闭 Node-1（种子节点）
3. 验证：Node-2 和 Node-3 之间通信正常
4. 重启 Node-1
5. 验证：Node-1 重新加入集群
```

**场景 3：保留消息同步**
```
1. 在 Node-1 发布保留消息 "config/setting" = "value"
2. 启动 Node-2 加入集群
3. 验证：Node-2 收到保留消息同步
4. 客户端连接 Node-2 订阅 "config/#"
5. 验证：客户端收到保留消息
```

---

## 二、桥接模式 (Bridge)

### 2.1 架构设计

桥接采用**主从层级架构**，子 Broker 作为 MQTT 客户端连接到父 Broker。

```
┌─────────────────────────────────────────────────────────────────┐
│                        桥接架构                                  │
│                                                                  │
│                   ┌─────────────────────┐                       │
│                   │    父 Broker         │                       │
│                   │    (Master)          │                       │
│                   │    :1883             │                       │
│                   └──────────┬──────────┘                       │
│                              │                                   │
│              ┌───────────────┼───────────────┐                  │
│              │               │               │                   │
│              ▼               ▼               ▼                   │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│   │  子 Broker 1  │  │  子 Broker 2  │  │  子 Broker 3  │         │
│   │  site-a:2883 │  │  site-b:2883 │  │  site-c:2883 │         │
│   │              │  │              │  │              │         │
│   │ 上行: sensor/#│  │ 上行: sensor/#│  │ 上行: sensor/#│         │
│   │ 下行: cmd/#  │  │ 下行: cmd/#  │  │ 下行: cmd/#  │         │
│   └──────────────┘  └──────────────┘  └──────────────┘         │
│                                                                  │
│  核心特性：                                                       │
│  • 主从层级 - 子 Broker 依赖父 Broker                             │
│  • 单向连接 - 子 Broker 主动连接父 Broker                         │
│  • 主题转换 - 支持添加/移除主题前缀                                │
│  • 自动重连 - 断开后自动重连                                      │
│  • 保留消息同步 - 重连时自动同步保留消息                           │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 核心特性

| 特性 | 说明 |
|------|------|
| **主从架构** | 子 Broker 作为客户端连接到父 Broker |
| **双向同步** | 支持上行（本地→远程）和下行（远程→本地）规则 |
| **主题转换** | 可添加/移除主题前缀，实现命名空间隔离 |
| **QoS 控制** | 可为桥接消息指定独立的 QoS 级别 |
| **自动重连** | 断开后自动重连，消息不缓存（断开时丢弃） |
| **保留消息同步** | 重连时自动同步本地保留消息到远程 |
| **认证支持** | 支持用户名/密码认证连接父 Broker |

### 2.3 消息流程

#### 上行同步（本地 → 远程）

```
┌──────────┐     ┌──────────────┐     ┌──────────┐
│ 本地客户端 │     │   子 Broker   │     │  父 Broker │
└────┬─────┘     └──────┬───────┘     └────┬─────┘
     │                  │                  │
     │  PUBLISH         │                  │
     │  sensor/temp     │                  │
     │─────────────────►│                  │
     │                  │                  │
     │                  │  1. 匹配上行规则  │
     │                  │  sensor/# ✓      │
     │                  │                  │
     │                  │  2. 转换主题      │
     │                  │  → site-a/sensor/temp
     │                  │                  │
     │                  │  3. 转发到父 Broker
     │                  │─────────────────►│
     │                  │                  │
```

#### 下行同步（远程 → 本地）

```
┌──────────┐     ┌──────────────┐     ┌──────────┐
│  父 Broker │     │   子 Broker   │     │ 本地客户端 │
└────┬─────┘     └──────┬───────┘     └────┬─────┘
     │                  │                  │
     │  PUBLISH         │                  │
     │  cmd/reboot      │                  │
     │─────────────────►│                  │
     │                  │                  │
     │                  │  1. 匹配下行规则  │
     │                  │  cmd/# ✓         │
     │                  │                  │
     │                  │  2. 发布到本地    │
     │                  │─────────────────►│
     │                  │                  │
```

### 2.4 配置选项

```csharp
public sealed class MqttBridgeOptions
{
    /// <summary>桥接名称</summary>
    public string Name { get; set; }

    /// <summary>远程 Broker 地址</summary>
    public string RemoteHost { get; set; } = "localhost";

    /// <summary>远程 Broker 端口</summary>
    public int RemotePort { get; set; } = 1883;

    /// <summary>桥接客户端 ID</summary>
    public string ClientId { get; set; }

    /// <summary>认证用户名</summary>
    public string? Username { get; set; }

    /// <summary>认证密码</summary>
    public string? Password { get; set; }

    /// <summary>是否使用 TLS</summary>
    public bool UseTls { get; set; }

    /// <summary>MQTT 协议版本</summary>
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

    /// <summary>保活间隔（秒）</summary>
    public ushort KeepAliveSeconds { get; set; } = 60;

    /// <summary>重连延迟（毫秒）</summary>
    public int ReconnectDelayMs { get; set; } = 5000;

    /// <summary>上行同步规则（本地 → 远程）</summary>
    public List<MqttBridgeRule> UpstreamRules { get; set; } = new();

    /// <summary>下行同步规则（远程 → 本地）</summary>
    public List<MqttBridgeRule> DownstreamRules { get; set; } = new();

    /// <summary>桥接消息的 QoS 级别</summary>
    public MqttQualityOfService QualityOfService { get; set; } = MqttQualityOfService.AtLeastOnce;

    /// <summary>是否同步保留消息标志</summary>
    public bool SyncRetainFlag { get; set; } = true;

    /// <summary>是否在重连时同步保留消息</summary>
    public bool SyncRetainedMessages { get; set; } = true;

    /// <summary>连接超时时间（秒）</summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
```

#### 桥接规则配置

```csharp
public sealed class MqttBridgeRule
{
    /// <summary>本地主题过滤器（支持 + 和 # 通配符）</summary>
    public string LocalTopicFilter { get; set; }

    /// <summary>远程主题前缀（上行时添加，下行时移除）</summary>
    public string? RemoteTopicPrefix { get; set; }

    /// <summary>本地主题前缀（下行时添加）</summary>
    public string? LocalTopicPrefix { get; set; }

    /// <summary>规则专用 QoS 级别</summary>
    public MqttQualityOfService? QualityOfService { get; set; }

    /// <summary>是否启用规则</summary>
    public bool Enabled { get; set; } = true;
}
```

### 2.5 使用示例

#### 基本桥接配置

```csharp
var broker = new MqttBroker(new MqttBrokerOptions { Port = 2883 });

// 添加桥接到父 Broker
var bridge = broker.AddBridge(new MqttBridgeOptions
{
    Name = "parent-bridge",
    RemoteHost = "127.0.0.1",
    RemotePort = 1883,
    ClientId = "bridge-site-a",

    // 上行规则：本地 sensor/# → 父 Broker sensor/#
    UpstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "sensor/#",
            Enabled = true
        }
    },

    // 下行规则：父 Broker commands/# → 本地 commands/#
    DownstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "commands/#",
            Enabled = true
        }
    }
});

await broker.StartAsync();
```

#### 带主题前缀的桥接

```csharp
// 场景：多站点数据汇聚到中心 Broker
// 本地: sensor/temperature → 远程: site-a/sensor/temperature

var bridge = broker.AddBridge(new MqttBridgeOptions
{
    Name = "site-a-bridge",
    RemoteHost = "central-broker.example.com",
    RemotePort = 1883,

    UpstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "sensor/#",
            RemoteTopicPrefix = "site-a/"  // 添加站点前缀
        },
        new MqttBridgeRule
        {
            LocalTopicFilter = "device/#",
            RemoteTopicPrefix = "site-a/"
        }
    },

    DownstreamRules =
    {
        new MqttBridgeRule
        {
            LocalTopicFilter = "commands/site-a/#",
            LocalTopicPrefix = ""  // 移除站点前缀后转发
        }
    }
});
```

#### 监听桥接事件

```csharp
bridge.Connected += (s, e) =>
{
    Console.WriteLine($"桥接已连接: {e.RemoteEndpoint}");
};

bridge.Disconnected += (s, e) =>
{
    Console.WriteLine($"桥接断开: 重连={e.WillReconnect}");
};

bridge.MessageForwarded += (s, e) =>
{
    var direction = e.Direction == BridgeDirection.Upstream ? "上行" : "下行";
    Console.WriteLine($"{direction}: {e.OriginalTopic} → {e.TransformedTopic}");
};
```

#### 获取桥接统计

```csharp
var stats = bridge.GetStatistics();
Console.WriteLine($"上行消息: {stats.UpstreamMessageCount} ({stats.UpstreamByteCount} 字节)");
Console.WriteLine($"下行消息: {stats.DownstreamMessageCount} ({stats.DownstreamByteCount} 字节)");
Console.WriteLine($"重连次数: {stats.ReconnectCount}");
```

### 2.6 测试场景

**场景 1：基本上行同步**
```
1. 启动父 Broker（端口 1883）
2. 启动子 Broker（端口 2883）+ 桥接配置
3. 客户端连接子 Broker，发布 "sensor/temperature" = "25.5"
4. 验证：父 Broker 收到消息
```

**场景 2：基本下行同步**
```
1. 启动父 Broker（端口 1883）
2. 启动子 Broker（端口 2883）+ 桥接配置
3. 客户端连接子 Broker，订阅 "commands/#"
4. 在父 Broker 发布 "commands/reboot"
5. 验证：客户端收到消息
```

**场景 3：断线重连**
```
1. 启动完整的桥接环境
2. 关闭父 Broker
3. 验证：子 Broker 触发 Disconnected 事件，显示 WillReconnect=true
4. 重启父 Broker
5. 验证：子 Broker 自动重连，触发 Connected 事件
```

**场景 4：保留消息同步**
```
1. 子 Broker 发布保留消息 "sensor/config" = "{...}"
2. 断开桥接连接
3. 重新连接
4. 验证：保留消息自动同步到父 Broker
```

---

## 三、模式对比

### 3.1 架构对比

| 对比项 | 集群 (Cluster) | 桥接 (Bridge) |
|--------|---------------|---------------|
| 架构类型 | 去中心化 P2P | 主从层级 |
| 主节点 | 无 | 有（父 Broker） |
| 节点关系 | 对等 | 从属 |
| 通信协议 | 自定义 TCP | MQTT 协议 |
| 连接方向 | 双向 | 单向（子→父） |

### 3.2 功能对比

| 功能 | 集群 (Cluster) | 桥接 (Bridge) |
|------|---------------|---------------|
| 消息自动转发 | ✓ 广播到所有节点 | ✓ 按规则同步 |
| 主题转换 | ✗ | ✓ 支持前缀 |
| 订阅同步 | ✓ 自动同步 | ✗ 手动订阅 |
| 保留消息同步 | ✓ 节点加入时同步 | ✓ 重连时同步 |
| 消息去重 | ✓ MessageId 缓存 | ✗ 不需要 |
| 认证支持 | ✓ 集群名称隔离 | ✓ 用户名/密码 |
| TLS 支持 | ✗ | ✓ |

### 3.3 适用场景

#### 集群模式适用于：
- **负载均衡**：客户端可连接任意节点
- **高可用**：单点故障不影响整体
- **同一数据中心**：低延迟、高带宽环境
- **订阅者分散**：订阅者分布在不同节点

#### 桥接模式适用于：
- **边缘计算**：边缘站点数据上报到中心
- **跨网络**：不同网络之间的消息同步
- **数据聚合**：多站点数据汇聚到一点
- **命名空间隔离**：添加站点前缀区分来源
- **安全隔离**：子 Broker 在内网，父 Broker 在外网

---

## 四、文件结构

```
src/System.Net.MQTT.Broker/
├── Bridge/
│   ├── IMqttBridge.cs              # 桥接接口
│   ├── MqttBridge.cs               # 桥接实现（核心）
│   ├── MqttBridgeOptions.cs        # 桥接配置
│   ├── MqttBridgeRule.cs           # 主题同步规则
│   ├── MqttBridgeEventArgs.cs      # 桥接事件参数
│   ├── MqttBridgeStatistics.cs     # 统计信息
│   └── MqttBridgeException.cs      # 桥接异常
│
├── Cluster/
│   ├── IMqttClusterNode.cs         # 集群节点接口
│   ├── MqttCluster.cs              # 集群实现（核心）
│   ├── MqttClusterOptions.cs       # 集群配置
│   ├── ClusterPeer.cs              # 对等节点连接
│   ├── ClusterPeerInfo.cs          # 对等节点信息
│   ├── ClusterMessage.cs           # 集群内部消息
│   ├── ClusterMessageType.cs       # 消息类型枚举
│   ├── ClusterHandshake.cs         # 握手协议
│   ├── ClusterSerializer.cs        # 二进制序列化
│   └── ClusterEventArgs.cs         # 集群事件参数
│
└── MqttBroker.cs                   # Broker 主类（包含桥接/集群管理）
```

---

## 五、API 参考

### 5.1 MqttBroker 扩展方法

```csharp
public sealed class MqttBroker
{
    /// <summary>添加桥接</summary>
    public IMqttBridge AddBridge(MqttBridgeOptions options);

    /// <summary>启用集群</summary>
    public void EnableCluster(MqttClusterOptions options);

    /// <summary>获取集群管理器</summary>
    public IMqttClusterNode? Cluster { get; }

    /// <summary>获取保留消息（供集群/桥接同步使用）</summary>
    public IReadOnlyDictionary<string, MqttApplicationMessage> RetainedMessages { get; }

    /// <summary>设置保留消息（供集群/桥接同步使用）</summary>
    public void SetRetainedMessage(MqttApplicationMessage message);

    /// <summary>批量设置保留消息</summary>
    public void SetRetainedMessages(IEnumerable<MqttApplicationMessage> messages);
}
```

### 5.2 IMqttBridge 接口

```csharp
public interface IMqttBridge : IAsyncDisposable
{
    string Name { get; }
    bool IsConnected { get; }
    MqttBridgeOptions Options { get; }

    event EventHandler<MqttBridgeConnectedEventArgs>? Connected;
    event EventHandler<MqttBridgeDisconnectedEventArgs>? Disconnected;
    event EventHandler<MqttBridgeMessageForwardedEventArgs>? MessageForwarded;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    MqttBridgeStatistics GetStatistics();
}
```

### 5.3 IMqttClusterNode 接口

```csharp
public interface IMqttClusterNode : IAsyncDisposable
{
    string NodeId { get; }
    bool IsRunning { get; }
    IReadOnlyCollection<ClusterPeerInfo> Peers { get; }
    MqttClusterOptions Options { get; }

    event EventHandler<ClusterPeerEventArgs>? PeerJoined;
    event EventHandler<ClusterPeerEventArgs>? PeerLeft;
    event EventHandler<ClusterMessageForwardedEventArgs>? MessageForwarded;
    event EventHandler<ClusterSubscriptionSyncEventArgs>? SubscriptionSynced;
    event EventHandler<ClusterRetainedSyncEventArgs>? RetainedMessagesSynced;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task BroadcastMessageAsync(MqttApplicationMessage message, string sourceNodeId, CancellationToken cancellationToken = default);
    Task SyncSubscriptionAsync(string topic, bool isSubscribe, CancellationToken cancellationToken = default);
}
```

---

## 六、最佳实践

### 6.1 集群部署建议

1. **种子节点数量**：建议至少配置 2-3 个种子节点
2. **心跳间隔**：根据网络状况调整，建议 3-10 秒
3. **超时时间**：建议为心跳间隔的 3 倍以上
4. **去重缓存**：根据消息量调整过期时间
5. **节点命名**：使用有意义的 NodeId，便于调试

### 6.2 桥接部署建议

1. **重连延迟**：根据网络稳定性调整，建议 3-10 秒
2. **QoS 级别**：重要消息使用 QoS 1 或 2
3. **主题规划**：提前规划好上行/下行主题结构
4. **认证安全**：生产环境启用 TLS 和认证
5. **监控统计**：定期检查桥接统计信息

### 6.3 混合部署

可以同时使用集群和桥接：

```
                    ┌─────────────────┐
                    │   中心 Broker    │
                    │   (接收汇聚)     │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
       ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
       │  站点 A 集群  │ │  站点 B 集群  │ │  站点 C 集群  │
       │             │ │             │ │             │
       │ Node1─Node2 │ │ Node1─Node2 │ │ Node1─Node2 │
       │      \  /   │ │      \  /   │ │      \  /   │
       │      Node3  │ │      Node3  │ │      Node3  │
       │ (一个节点桥接)│ │ (一个节点桥接)│ │ (一个节点桥接)│
       └─────────────┘ └─────────────┘ └─────────────┘
```

每个站点内部使用集群实现高可用，站点之间通过桥接汇聚数据到中心。
