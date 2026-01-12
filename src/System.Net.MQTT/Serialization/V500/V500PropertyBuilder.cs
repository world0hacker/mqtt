using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 属性构建器。
/// 提供将属性对象编码为二进制数据的功能。
/// </summary>
public sealed class V500PropertyBuilder
{
    /// <summary>
    /// 计算 CONNECT 属性的编码长度。
    /// </summary>
    public int CalculateConnectPropertiesSize(MqttConnectProperties? props)
    {
        if (props == null) return 0;

        var size = 0;

        if (props.SessionExpiryInterval.HasValue) size += 1 + 4;
        if (props.ReceiveMaximum.HasValue) size += 1 + 2;
        if (props.MaximumPacketSize.HasValue) size += 1 + 4;
        if (props.TopicAliasMaximum.HasValue) size += 1 + 2;
        if (props.RequestResponseInformation.HasValue) size += 1 + 1;
        if (props.RequestProblemInformation.HasValue) size += 1 + 1;
        if (!string.IsNullOrEmpty(props.AuthenticationMethod))
            size += 1 + MqttBinaryWriter.GetStringSize(props.AuthenticationMethod);
        if (!props.AuthenticationData.IsEmpty)
            size += 1 + MqttBinaryWriter.GetBinaryDataSize(props.AuthenticationData);
        foreach (var prop in props.UserProperties)
        {
            size += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        return size;
    }

    /// <summary>
    /// 写入 CONNECT 属性。
    /// </summary>
    public void WriteConnectProperties(ref MqttBinaryWriter writer, MqttConnectProperties? props)
    {
        if (props == null)
        {
            writer.WriteVariableByteInteger(0);
            return;
        }

        var size = CalculateConnectPropertiesSize(props);
        writer.WriteVariableByteInteger((uint)size);

        if (props.SessionExpiryInterval.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.SessionExpiryInterval);
            writer.WriteUInt32(props.SessionExpiryInterval.Value);
        }
        if (props.ReceiveMaximum.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.ReceiveMaximum);
            writer.WriteUInt16(props.ReceiveMaximum.Value);
        }
        if (props.MaximumPacketSize.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.MaximumPacketSize);
            writer.WriteUInt32(props.MaximumPacketSize.Value);
        }
        if (props.TopicAliasMaximum.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.TopicAliasMaximum);
            writer.WriteUInt16(props.TopicAliasMaximum.Value);
        }
        if (props.RequestResponseInformation.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.RequestResponseInformation);
            writer.WriteByte(props.RequestResponseInformation.Value ? (byte)1 : (byte)0);
        }
        if (props.RequestProblemInformation.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.RequestProblemInformation);
            writer.WriteByte(props.RequestProblemInformation.Value ? (byte)1 : (byte)0);
        }
        if (!string.IsNullOrEmpty(props.AuthenticationMethod))
        {
            writer.WriteByte((byte)MqttPropertyId.AuthenticationMethod);
            writer.WriteString(props.AuthenticationMethod);
        }
        if (!props.AuthenticationData.IsEmpty)
        {
            writer.WriteByte((byte)MqttPropertyId.AuthenticationData);
            writer.WriteBinaryData(props.AuthenticationData.Span);
        }
        foreach (var prop in props.UserProperties)
        {
            writer.WriteByte((byte)MqttPropertyId.UserProperty);
            writer.WriteString(prop.Name);
            writer.WriteString(prop.Value);
        }
    }

    /// <summary>
    /// 计算 CONNACK 属性的编码长度。
    /// </summary>
    public int CalculateConnAckPropertiesSize(MqttConnAckProperties? props)
    {
        if (props == null) return 0;

        var size = 0;

        if (props.SessionExpiryInterval.HasValue) size += 1 + 4;
        if (props.ReceiveMaximum.HasValue) size += 1 + 2;
        if (props.MaximumQoS.HasValue) size += 1 + 1;
        if (props.RetainAvailable.HasValue) size += 1 + 1;
        if (props.MaximumPacketSize.HasValue) size += 1 + 4;
        if (!string.IsNullOrEmpty(props.AssignedClientIdentifier))
            size += 1 + MqttBinaryWriter.GetStringSize(props.AssignedClientIdentifier);
        if (props.TopicAliasMaximum.HasValue) size += 1 + 2;
        if (!string.IsNullOrEmpty(props.ReasonString))
            size += 1 + MqttBinaryWriter.GetStringSize(props.ReasonString);
        if (props.WildcardSubscriptionAvailable.HasValue) size += 1 + 1;
        if (props.SubscriptionIdentifiersAvailable.HasValue) size += 1 + 1;
        if (props.SharedSubscriptionAvailable.HasValue) size += 1 + 1;
        if (props.ServerKeepAlive.HasValue) size += 1 + 2;
        if (!string.IsNullOrEmpty(props.ResponseInformation))
            size += 1 + MqttBinaryWriter.GetStringSize(props.ResponseInformation);
        if (!string.IsNullOrEmpty(props.ServerReference))
            size += 1 + MqttBinaryWriter.GetStringSize(props.ServerReference);
        if (!string.IsNullOrEmpty(props.AuthenticationMethod))
            size += 1 + MqttBinaryWriter.GetStringSize(props.AuthenticationMethod);
        if (!props.AuthenticationData.IsEmpty)
            size += 1 + MqttBinaryWriter.GetBinaryDataSize(props.AuthenticationData);
        foreach (var prop in props.UserProperties)
        {
            size += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        return size;
    }

    /// <summary>
    /// 写入 CONNACK 属性。
    /// </summary>
    public void WriteConnAckProperties(ref MqttBinaryWriter writer, MqttConnAckProperties? props)
    {
        if (props == null)
        {
            writer.WriteVariableByteInteger(0);
            return;
        }

        var size = CalculateConnAckPropertiesSize(props);
        writer.WriteVariableByteInteger((uint)size);

        if (props.SessionExpiryInterval.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.SessionExpiryInterval);
            writer.WriteUInt32(props.SessionExpiryInterval.Value);
        }
        if (props.ReceiveMaximum.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.ReceiveMaximum);
            writer.WriteUInt16(props.ReceiveMaximum.Value);
        }
        if (props.MaximumQoS.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.MaximumQoS);
            writer.WriteByte((byte)props.MaximumQoS.Value);
        }
        if (props.RetainAvailable.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.RetainAvailable);
            writer.WriteByte(props.RetainAvailable.Value ? (byte)1 : (byte)0);
        }
        if (props.MaximumPacketSize.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.MaximumPacketSize);
            writer.WriteUInt32(props.MaximumPacketSize.Value);
        }
        if (!string.IsNullOrEmpty(props.AssignedClientIdentifier))
        {
            writer.WriteByte((byte)MqttPropertyId.AssignedClientIdentifier);
            writer.WriteString(props.AssignedClientIdentifier);
        }
        if (props.TopicAliasMaximum.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.TopicAliasMaximum);
            writer.WriteUInt16(props.TopicAliasMaximum.Value);
        }
        if (!string.IsNullOrEmpty(props.ReasonString))
        {
            writer.WriteByte((byte)MqttPropertyId.ReasonString);
            writer.WriteString(props.ReasonString);
        }
        if (props.WildcardSubscriptionAvailable.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.WildcardSubscriptionAvailable);
            writer.WriteByte(props.WildcardSubscriptionAvailable.Value ? (byte)1 : (byte)0);
        }
        if (props.SubscriptionIdentifiersAvailable.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.SubscriptionIdentifiersAvailable);
            writer.WriteByte(props.SubscriptionIdentifiersAvailable.Value ? (byte)1 : (byte)0);
        }
        if (props.SharedSubscriptionAvailable.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.SharedSubscriptionAvailable);
            writer.WriteByte(props.SharedSubscriptionAvailable.Value ? (byte)1 : (byte)0);
        }
        if (props.ServerKeepAlive.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.ServerKeepAlive);
            writer.WriteUInt16(props.ServerKeepAlive.Value);
        }
        if (!string.IsNullOrEmpty(props.ResponseInformation))
        {
            writer.WriteByte((byte)MqttPropertyId.ResponseInformation);
            writer.WriteString(props.ResponseInformation);
        }
        if (!string.IsNullOrEmpty(props.ServerReference))
        {
            writer.WriteByte((byte)MqttPropertyId.ServerReference);
            writer.WriteString(props.ServerReference);
        }
        if (!string.IsNullOrEmpty(props.AuthenticationMethod))
        {
            writer.WriteByte((byte)MqttPropertyId.AuthenticationMethod);
            writer.WriteString(props.AuthenticationMethod);
        }
        if (!props.AuthenticationData.IsEmpty)
        {
            writer.WriteByte((byte)MqttPropertyId.AuthenticationData);
            writer.WriteBinaryData(props.AuthenticationData.Span);
        }
        foreach (var prop in props.UserProperties)
        {
            writer.WriteByte((byte)MqttPropertyId.UserProperty);
            writer.WriteString(prop.Name);
            writer.WriteString(prop.Value);
        }
    }

    /// <summary>
    /// 计算 PUBLISH 属性的编码长度。
    /// </summary>
    public int CalculatePublishPropertiesSize(MqttPublishProperties? props)
    {
        if (props == null) return 0;

        var size = 0;

        if (props.PayloadFormatIndicator.HasValue) size += 1 + 1;
        if (props.MessageExpiryInterval.HasValue) size += 1 + 4;
        if (props.TopicAlias.HasValue) size += 1 + 2;
        if (!string.IsNullOrEmpty(props.ResponseTopic))
            size += 1 + MqttBinaryWriter.GetStringSize(props.ResponseTopic);
        if (!props.CorrelationData.IsEmpty)
            size += 1 + MqttBinaryWriter.GetBinaryDataSize(props.CorrelationData);
        foreach (var subId in props.SubscriptionIdentifiers)
        {
            size += 1 + MqttBinaryWriter.GetVariableByteIntegerSize(subId);
        }
        if (!string.IsNullOrEmpty(props.ContentType))
            size += 1 + MqttBinaryWriter.GetStringSize(props.ContentType);
        foreach (var prop in props.UserProperties)
        {
            size += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        return size;
    }

    /// <summary>
    /// 写入 PUBLISH 属性。
    /// </summary>
    public void WritePublishProperties(ref MqttBinaryWriter writer, MqttPublishProperties? props)
    {
        if (props == null)
        {
            writer.WriteVariableByteInteger(0);
            return;
        }

        var size = CalculatePublishPropertiesSize(props);
        writer.WriteVariableByteInteger((uint)size);

        if (props.PayloadFormatIndicator.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.PayloadFormatIndicator);
            writer.WriteByte(props.PayloadFormatIndicator.Value);
        }
        if (props.MessageExpiryInterval.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.MessageExpiryInterval);
            writer.WriteUInt32(props.MessageExpiryInterval.Value);
        }
        if (props.TopicAlias.HasValue)
        {
            writer.WriteByte((byte)MqttPropertyId.TopicAlias);
            writer.WriteUInt16(props.TopicAlias.Value);
        }
        if (!string.IsNullOrEmpty(props.ResponseTopic))
        {
            writer.WriteByte((byte)MqttPropertyId.ResponseTopic);
            writer.WriteString(props.ResponseTopic);
        }
        if (!props.CorrelationData.IsEmpty)
        {
            writer.WriteByte((byte)MqttPropertyId.CorrelationData);
            writer.WriteBinaryData(props.CorrelationData.Span);
        }
        foreach (var subId in props.SubscriptionIdentifiers)
        {
            writer.WriteByte((byte)MqttPropertyId.SubscriptionIdentifier);
            writer.WriteVariableByteInteger(subId);
        }
        if (!string.IsNullOrEmpty(props.ContentType))
        {
            writer.WriteByte((byte)MqttPropertyId.ContentType);
            writer.WriteString(props.ContentType);
        }
        foreach (var prop in props.UserProperties)
        {
            writer.WriteByte((byte)MqttPropertyId.UserProperty);
            writer.WriteString(prop.Name);
            writer.WriteString(prop.Value);
        }
    }
}
