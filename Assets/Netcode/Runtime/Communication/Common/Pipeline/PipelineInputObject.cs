using Netcode.Runtime.Communication.Common.Messaging;
using System.Collections.Generic;
using System.IO;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public struct PipelineOutputObject
    {
        public List<byte> OutputData;
        public NetworkMessage[] Messages;
    }

    public struct PipelineInputObject
    {
        public Stream InputStream;
        public ReadOnlyByteBuffer InputBuffer;
        public NetworkMessage[] Messages;
    }

    public struct ReadOnlyByteBuffer
    {
        private readonly byte[] _data;
        private int _position;

        public ReadOnlyByteBuffer(byte[] data)
        {
            _data = new byte[data.Length];
            data.CopyTo(_data, 0);
            _position = 0;
        }

        public byte[] Consume(int amount)
        {
            byte[] data = GetRange(_position, amount);
            _position += amount;
            return data;
        }

        public byte[] ToArray()
        {
            return GetRange(_position, _data.Length - _position);
        }

        public byte[] ToArrayFull()
        {
            return _data;
        }

        private byte[] GetRange(int position, int amount)
        {
            byte[] data = new byte[amount];
            for (int i = 0; i < amount; i++)
            {
                data[i] = _data[position + i];
            }
            return data;
        }
    }
}
