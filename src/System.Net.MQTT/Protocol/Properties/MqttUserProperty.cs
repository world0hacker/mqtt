namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 用户属性。
/// 用户属性是一个键值对，可以出现在几乎所有 MQTT 5.0 报文中。
/// </summary>
/// <remarks>
/// 使用 readonly struct 以避免堆分配，提高性能。
/// </remarks>
public readonly struct MqttUserProperty : IEquatable<MqttUserProperty>
{
    /// <summary>
    /// 获取属性名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取属性值。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 创建用户属性。
    /// </summary>
    /// <param name="name">属性名称，不能为 null</param>
    /// <param name="value">属性值，不能为 null</param>
    /// <exception cref="ArgumentNullException">当 name 或 value 为 null 时抛出</exception>
    public MqttUserProperty(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public bool Equals(MqttUserProperty other) =>
        string.Equals(Name, other.Name, StringComparison.Ordinal) &&
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is MqttUserProperty other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Name, Value);

    /// <summary>
    /// 相等运算符。
    /// </summary>
    public static bool operator ==(MqttUserProperty left, MqttUserProperty right) => left.Equals(right);

    /// <summary>
    /// 不等运算符。
    /// </summary>
    public static bool operator !=(MqttUserProperty left, MqttUserProperty right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"{Name}={Value}";
}
