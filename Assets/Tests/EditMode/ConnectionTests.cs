using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Server;
using Netcode.Runtime.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConnectionTests
{
    public NetworkServer _server;
    public NetworkClient _client;

    public const ushort _udpPort = 50000;
    public const ushort _tcpPort = 50000;
    public const string _hostname = "127.0.0.1";

    public bool _clientConnected = false;

    [SetUp]
    public void SetUp()
    {
        _server = new(_tcpPort, _udpPort, 100, new UnityLoggerFactory(LogLevel.Info));
        _client = new(new UnityLoggerFactory(LogLevel.Info).CreateLogger<NetworkClient>());
        _clientConnected = false;
        _client.OnConnect += ClientOnConnectCallback;
    }

    private void ClientOnConnectCallback(uint id)
    {
        _clientConnected = true;
    }

    [Test]
    public void StartStopServer()
    {
        _server.Start();
        _server.Stop();
    }

    [Test]
    public async void ConnectSingleClientToServer()
    {
        // Arrange
        _server.Start();

        // Act
        await _client.Connect(_hostname, _udpPort, _tcpPort);

        // Assert
        Assert.That(_clientConnected, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        _server.Stop();
        _client.Dispose();
    }
}