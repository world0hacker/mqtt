namespace System.Net.MQTT;

/// <summary>
/// MQTT 协议版本。
/// </summary>
public enum MqttProtocolVersion : byte
{
    /// <summary>
    /// MQTT 3.1 版本
    /// </summary>
    V310 = 3,

    /// <summary>
    /// MQTT 3.1.1 版本
    /// </summary>
    V311 = 4,

    /// <summary>
    /// MQTT 5.0 版本
    /// </summary>
    V500 = 5
}
