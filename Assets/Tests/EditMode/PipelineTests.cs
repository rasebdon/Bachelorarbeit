using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Pipeline;
using Netcode.Runtime.Communication.Common.Security;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

public class PipelineTests
{
    public IPipeline Pipeline;
    public MemoryStream Stream;

    [SetUp]
    public void Setup()
    {
        Pipeline = PipelineFactory.CreatePipeline();
        Stream = new();
    }

    [Test]
    public void ReadWriteNormal()
    {
        // Arrange
        var messages = new NetworkMessage[]
        {
                new DestroyNetworkObjectMessage(Guid.NewGuid()),
                new SyncNetworkVariableMessage(new byte[] {0, 0, 1, 1}, "test", Guid.NewGuid())
        };
        var output = new PipelineOutputObject
        {
            Messages = messages,
            OutputData = new()
        };

        // Act
        Stream.Write(Pipeline.RunPipeline(output).OutputData.ToArray());
        Stream.Position = 0;

        // Assert
        var input = new PipelineInputObject
        {
            InputStream = Stream,
        };
        var receivedMessages = Pipeline.RunPipeline(input).Result.Messages;

        Assert.That(receivedMessages.FirstOrDefault(m => m is DestroyNetworkObjectMessage msg && msg.Identity == ((DestroyNetworkObjectMessage)messages[0]).Identity), Is.Not.Null);
        Assert.That(receivedMessages.FirstOrDefault(m => m is SyncNetworkVariableMessage msg && msg.NetworkIdentity == ((SyncNetworkVariableMessage)messages[1]).NetworkIdentity), Is.Not.Null);
    }

    [Test]
    public void ReadWriteMac()
    {
        // Arrange
        Pipeline.AddMAC(new HMAC256Handler());
        var messages = new NetworkMessage[]
        {
                new DestroyNetworkObjectMessage(Guid.NewGuid()),
                new SyncNetworkVariableMessage(new byte[] {0, 0, 1, 1}, "test", Guid.NewGuid())
        };
        var output = new PipelineOutputObject
        {
            Messages = messages,
            OutputData = new()
        };

        // Act
        Stream.Write(Pipeline.RunPipeline(output).OutputData.ToArray());
        Stream.Position = 0;

        // Assert
        var input = new PipelineInputObject
        {
            InputStream = Stream,
        };
        var receivedMessages = Pipeline.RunPipeline(input).Result.Messages;

        Assert.That(receivedMessages.FirstOrDefault(m => m is DestroyNetworkObjectMessage msg && msg.Identity == ((DestroyNetworkObjectMessage)messages[0]).Identity), Is.Not.Null);
        Assert.That(receivedMessages.FirstOrDefault(m => m is SyncNetworkVariableMessage msg && msg.NetworkIdentity == ((SyncNetworkVariableMessage)messages[1]).NetworkIdentity), Is.Not.Null);
    }

    [Test]
    public void ReadWriteEncryption()
    {
        // Arrange
        Pipeline.AddEncryption(new AES256Encryption());
        var messages = new NetworkMessage[]
        {
                new DestroyNetworkObjectMessage(Guid.NewGuid()),
                new SyncNetworkVariableMessage(new byte[] {0, 0, 1, 1}, "test", Guid.NewGuid())
        };
        var output = new PipelineOutputObject
        {
            Messages = messages,
            OutputData = new()
        };

        // Act
        Stream.Write(Pipeline.RunPipeline(output).OutputData.ToArray());
        Stream.Position = 0;

        // Assert
        var input = new PipelineInputObject
        {
            InputStream = Stream,
        };
        var receivedMessages = Pipeline.RunPipeline(input).Result.Messages;

        Assert.That(receivedMessages.FirstOrDefault(m => m is DestroyNetworkObjectMessage msg && msg.Identity == ((DestroyNetworkObjectMessage)messages[0]).Identity), Is.Not.Null);
        Assert.That(receivedMessages.FirstOrDefault(m => m is SyncNetworkVariableMessage msg && msg.NetworkIdentity == ((SyncNetworkVariableMessage)messages[1]).NetworkIdentity), Is.Not.Null);
    }

    [Test]
    public void ReadWriteMacAndEncryption()
    {
        // Arrange
        Pipeline.AddEncryption(new AES256Encryption());
        Pipeline.AddMAC(new HMAC256Handler());
        var messages = new NetworkMessage[]
        {
                new DestroyNetworkObjectMessage(Guid.NewGuid()),
                new SyncNetworkVariableMessage(new byte[] {0, 0, 1, 1}, "test", Guid.NewGuid())
        };
        var output = new PipelineOutputObject
        {
            Messages = messages,
            OutputData = new()
        };

        // Act
        Stream.Write(Pipeline.RunPipeline(output).OutputData.ToArray());
        Stream.Position = 0;

        // Assert
        var input = new PipelineInputObject
        {
            InputStream = Stream,
        };
        var receivedMessages = Pipeline.RunPipeline(input).Result.Messages;

        Assert.That(receivedMessages.FirstOrDefault(m => m is DestroyNetworkObjectMessage msg && msg.Identity == ((DestroyNetworkObjectMessage)messages[0]).Identity), Is.Not.Null);
        Assert.That(receivedMessages.FirstOrDefault(m => m is SyncNetworkVariableMessage msg && msg.NetworkIdentity == ((SyncNetworkVariableMessage)messages[1]).NetworkIdentity), Is.Not.Null);
    }
}
