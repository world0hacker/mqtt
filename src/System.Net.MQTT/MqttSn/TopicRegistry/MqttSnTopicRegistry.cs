using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.TopicRegistry;

/// <summary>
/// MQTT-SN 主题注册表。
/// 管理主题名和主题 ID 之间的映射关系。
///
/// MQTT-SN 主题 ID 范围:
/// - 0x0000: 保留
/// - 0x0001-0x7FFF: 预定义主题 ID
/// - 0x8000-0xFFFE: 动态注册主题 ID
/// - 0xFFFF: 短主题名标识
/// </summary>
public sealed class MqttSnTopicRegistry
{
    /// <summary>
    /// 动态主题 ID 起始值。
    /// </summary>
    public const ushort DynamicTopicIdStart = 0x8000;

    /// <summary>
    /// 动态主题 ID 结束值。
    /// </summary>
    public const ushort DynamicTopicIdEnd = 0xFFFE;

    /// <summary>
    /// 预定义主题 ID 起始值。
    /// </summary>
    public const ushort PredefinedTopicIdStart = 0x0001;

    /// <summary>
    /// 预定义主题 ID 结束值。
    /// </summary>
    public const ushort PredefinedTopicIdEnd = 0x7FFF;

    private readonly ConcurrentDictionary<string, ClientTopicRegistry> _clientRegistries = new();
    private readonly ConcurrentDictionary<ushort, string> _predefinedTopics = new();

    /// <summary>
    /// 注册预定义主题。
    /// 预定义主题在所有客户端间共享。
    /// </summary>
    /// <param name="topicId">预定义主题 ID (0x0001-0x7FFF)</param>
    /// <param name="topicName">主题名</param>
    public void RegisterPredefinedTopic(ushort topicId, string topicName)
    {
        if (topicId < PredefinedTopicIdStart || topicId > PredefinedTopicIdEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(topicId),
                $"预定义主题 ID 必须在 {PredefinedTopicIdStart}-{PredefinedTopicIdEnd} 范围内");
        }

        _predefinedTopics[topicId] = topicName ?? throw new ArgumentNullException(nameof(topicName));
    }

    /// <summary>
    /// 获取预定义主题名。
    /// </summary>
    /// <param name="topicId">预定义主题 ID</param>
    /// <returns>主题名，如果不存在返回 null</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetPredefinedTopic(ushort topicId)
    {
        return _predefinedTopics.TryGetValue(topicId, out var topic) ? topic : null;
    }

    /// <summary>
    /// 为客户端注册主题并获取主题 ID。
    /// 如果主题已注册，返回已有的 ID。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    /// <param name="topicName">主题名</param>
    /// <returns>分配的主题 ID</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort RegisterTopic(string clientId, string topicName)
    {
        var registry = GetOrCreateClientRegistry(clientId);
        return registry.RegisterTopic(topicName);
    }

    /// <summary>
    /// 获取客户端的主题名。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    /// <param name="topicId">主题 ID</param>
    /// <returns>主题名，如果不存在返回 null</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetTopic(string clientId, ushort topicId)
    {
        // 先检查预定义主题
        if (topicId >= PredefinedTopicIdStart && topicId <= PredefinedTopicIdEnd)
        {
            return GetPredefinedTopic(topicId);
        }

        // 检查客户端注册的主题
        if (_clientRegistries.TryGetValue(clientId, out var registry))
        {
            return registry.GetTopic(topicId);
        }

        return null;
    }

    /// <summary>
    /// 获取客户端已注册主题的 ID。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    /// <param name="topicName">主题名</param>
    /// <returns>主题 ID，如果未注册返回 null</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort? GetTopicId(string clientId, string topicName)
    {
        if (_clientRegistries.TryGetValue(clientId, out var registry))
        {
            return registry.GetTopicId(topicName);
        }

        return null;
    }

    /// <summary>
    /// 清理客户端的主题注册。
    /// 在客户端断开连接时调用。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    public void ClearClientTopics(string clientId)
    {
        _clientRegistries.TryRemove(clientId, out _);
    }

    /// <summary>
    /// 获取或创建客户端注册表。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ClientTopicRegistry GetOrCreateClientRegistry(string clientId)
    {
        return _clientRegistries.GetOrAdd(clientId, _ => new ClientTopicRegistry());
    }

    /// <summary>
    /// 客户端主题注册表。
    /// </summary>
    private sealed class ClientTopicRegistry
    {
        private readonly ConcurrentDictionary<string, ushort> _topicToId = new();
        private readonly ConcurrentDictionary<ushort, string> _idToTopic = new();
        private ushort _nextTopicId = DynamicTopicIdStart;
        private readonly object _lock = new();

        /// <summary>
        /// 注册主题。
        /// </summary>
        public ushort RegisterTopic(string topicName)
        {
            // 快速路径：已存在
            if (_topicToId.TryGetValue(topicName, out var existingId))
            {
                return existingId;
            }

            // 慢速路径：需要分配新 ID
            lock (_lock)
            {
                // 双重检查
                if (_topicToId.TryGetValue(topicName, out existingId))
                {
                    return existingId;
                }

                // 分配新 ID
                var newId = _nextTopicId++;
                if (_nextTopicId > DynamicTopicIdEnd)
                {
                    _nextTopicId = DynamicTopicIdStart; // 循环使用
                }

                _topicToId[topicName] = newId;
                _idToTopic[newId] = topicName;

                return newId;
            }
        }

        /// <summary>
        /// 获取主题名。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetTopic(ushort topicId)
        {
            return _idToTopic.TryGetValue(topicId, out var topic) ? topic : null;
        }

        /// <summary>
        /// 获取主题 ID。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort? GetTopicId(string topicName)
        {
            return _topicToId.TryGetValue(topicName, out var id) ? id : null;
        }
    }
}
