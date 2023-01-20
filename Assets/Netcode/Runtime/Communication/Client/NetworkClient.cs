using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Client
{
    public class NetworkClient : NetworkServerClient
    {
        public NetworkClient(IMessageProtocolHandler protocolHandler) 
            : base(0, new TcpClient(), new UdpClient(), protocolHandler)
        { }

        public async void Connect(string hostname, ushort tcpPort, ushort udpPort)
        {
            await _tcpClient.ConnectAsync(hostname, tcpPort);

            BeginReceiveTcpAsync();
            BeginReceiveUdpAsync();
        }

        private async void BeginReceiveUdpAsync()
        {
            UdpReceiveResult received = await _udpClient.ReceiveAsync();

            ReceiveDatagramAsync(received.Buffer);
            BeginReceiveUdpAsync();
        }
    }
}
