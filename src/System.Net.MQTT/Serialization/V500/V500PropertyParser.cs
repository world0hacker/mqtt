using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 属性解析器。
/// 提供从二进制数据解析各种属性的功能。
/// </summary>
public sealed class V500PropertyParser
{
    /// <summary>
    /// 解析 CONNECT 属性。
    /// </summary>
    /// <param name="reader">二进制读取器</param>
    /// <param name="propertiesLength">属性总长度</param>
    /// <returns>解析后的属性对象</returns>
    public MqttConnectProperties ParseConnectProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttConnectProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.SessionExpiryInterval:
                    props.SessionExpiryInterval = reader.ReadUInt32();
                    break;
                case MqttPropertyId.ReceiveMaximum:
                    props.ReceiveMaximum = reader.ReadUInt16();
                    break;
                case MqttPropertyId.MaximumPacketSize:
                    props.MaximumPacketSize = reader.ReadUInt32();
                    break;
                case MqttPropertyId.TopicAliasMaximum:
                    props.TopicAliasMaximum = reader.ReadUInt16();
                    break;
                case MqttPropertyId.RequestResponseInformation:
                    props.RequestResponseInformation = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.RequestProblemInformation:
                    props.RequestProblemInformation = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.AuthenticationMethod:
                    props.AuthenticationMethod = reader.ReadString();
                    break;
                case MqttPropertyId.AuthenticationData:
                    props.AuthenticationData = reader.ReadBinaryData();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"CONNECT 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 CONNACK 属性。
    /// </summary>
    public MqttConnAckProperties ParseConnAckProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttConnAckProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.SessionExpiryInterval:
                    props.SessionExpiryInterval = reader.ReadUInt32();
                    break;
                case MqttPropertyId.ReceiveMaximum:
                    props.ReceiveMaximum = reader.ReadUInt16();
                    break;
                case MqttPropertyId.MaximumQoS:
                    props.MaximumQoS = (MqttQualityOfService)reader.ReadByte();
                    break;
                case MqttPropertyId.RetainAvailable:
                    props.RetainAvailable = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.MaximumPacketSize:
                    props.MaximumPacketSize = reader.ReadUInt32();
                    break;
                case MqttPropertyId.AssignedClientIdentifier:
                    props.AssignedClientIdentifier = reader.ReadString();
                    break;
                case MqttPropertyId.TopicAliasMaximum:
                    props.TopicAliasMaximum = reader.ReadUInt16();
                    break;
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.WildcardSubscriptionAvailable:
                    props.WildcardSubscriptionAvailable = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.SubscriptionIdentifiersAvailable:
                    props.SubscriptionIdentifiersAvailable = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.SharedSubscriptionAvailable:
                    props.SharedSubscriptionAvailable = reader.ReadByte() != 0;
                    break;
                case MqttPropertyId.ServerKeepAlive:
                    props.ServerKeepAlive = reader.ReadUInt16();
                    break;
                case MqttPropertyId.ResponseInformation:
                    props.ResponseInformation = reader.ReadString();
                    break;
                case MqttPropertyId.ServerReference:
                    props.ServerReference = reader.ReadString();
                    break;
                case MqttPropertyId.AuthenticationMethod:
                    props.AuthenticationMethod = reader.ReadString();
                    break;
                case MqttPropertyId.AuthenticationData:
                    props.AuthenticationData = reader.ReadBinaryData();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"CONNACK 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 PUBLISH 属性。
    /// </summary>
    public MqttPublishProperties ParsePublishProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttPublishProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.PayloadFormatIndicator:
                    props.PayloadFormatIndicator = reader.ReadByte();
                    break;
                case MqttPropertyId.MessageExpiryInterval:
                    props.MessageExpiryInterval = reader.ReadUInt32();
                    break;
                case MqttPropertyId.TopicAlias:
                    props.TopicAlias = reader.ReadUInt16();
                    break;
                case MqttPropertyId.ResponseTopic:
                    props.ResponseTopic = reader.ReadString();
                    break;
                case MqttPropertyId.CorrelationData:
                    props.CorrelationData = reader.ReadBinaryData();
                    break;
                case MqttPropertyId.SubscriptionIdentifier:
                    props.SubscriptionIdentifiers.Add(reader.ReadVariableByteInteger());
                    break;
                case MqttPropertyId.ContentType:
                    props.ContentType = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"PUBLISH 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 PUBACK/PUBREC/PUBREL/PUBCOMP 属性。
    /// </summary>
    public MqttPubAckProperties ParsePubAckProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttPubAckProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"PUBACK/PUBREC/PUBREL/PUBCOMP 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 SUBSCRIBE 属性。
    /// </summary>
    public MqttSubscribeProperties ParseSubscribeProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttSubscribeProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.SubscriptionIdentifier:
                    props.SubscriptionIdentifier = reader.ReadVariableByteInteger();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"SUBSCRIBE 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 DISCONNECT 属性。
    /// </summary>
    public MqttDisconnectProperties ParseDisconnectProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttDisconnectProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.SessionExpiryInterval:
                    props.SessionExpiryInterval = reader.ReadUInt32();
                    break;
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.ServerReference:
                    props.ServerReference = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"DISCONNECT 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 AUTH 属性。
    /// </summary>
    public MqttAuthProperties ParseAuthProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttAuthProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.AuthenticationMethod:
                    props.AuthenticationMethod = reader.ReadString();
                    break;
                case MqttPropertyId.AuthenticationData:
                    props.AuthenticationData = reader.ReadBinaryData();
                    break;
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"AUTH 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 SUBACK 属性。
    /// </summary>
    public MqttSubAckProperties ParseSubAckProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttSubAckProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"SUBACK 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 UNSUBSCRIBE 属性。
    /// </summary>
    public MqttUnsubscribeProperties ParseUnsubscribeProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttUnsubscribeProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"UNSUBSCRIBE 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }

    /// <summary>
    /// 解析 UNSUBACK 属性。
    /// </summary>
    public MqttUnsubAckProperties ParseUnsubAckProperties(ref MqttBinaryReader reader, int propertiesLength)
    {
        var props = new MqttUnsubAckProperties();
        var endPosition = reader.Position + propertiesLength;

        while (reader.Position < endPosition)
        {
            var propertyId = (MqttPropertyId)reader.ReadByte();

            switch (propertyId)
            {
                case MqttPropertyId.ReasonString:
                    props.ReasonString = reader.ReadString();
                    break;
                case MqttPropertyId.UserProperty:
                    var name = reader.ReadString();
                    var value = reader.ReadString();
                    props.UserProperties.Add(new MqttUserProperty(name, value));
                    break;
                default:
                    throw new MqttProtocolException($"UNSUBACK 报文中不允许的属性: {propertyId}");
            }
        }

        return props;
    }
}
