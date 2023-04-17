using Netcode.Runtime.Communication.Common.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public struct PipelineOutputObject
    {
        public WriteOnlyByteBuffer OutputData;
        public NetworkMessage[] Messages;
    }

    public struct PipelineInputObject
    {
        public Stream InputStream;
        public ReadOnlyByteBuffer InputBuffer;
        public NetworkMessage[] Messages;
    }

    public struct WriteOnlyByteBuffer
    {
        private byte[] _data;
        
        public int Length => _data == null ? 0 : _data.Length;

        public WriteOnlyByteBuffer(byte[] data)
        {
            _data = data;
        }

        public void InsertBeginning(byte[] data)
        {
            if(_data == null)
            {
                _data = data;
                return;
            }

            byte[] newData = new byte[data.Length + _data.Length];
            data.CopyTo(newData, 0);
            _data.CopyTo(newData, data.Length);
            _data = newData;
        }

        public void InsertEnd(byte[] data)
        {
            if (_data == null)
            {
                _data = data;
                return;
            }

            byte[] newData = new byte[data.Length + _data.Length];
            _data.CopyTo(newData, 0);
            data.CopyTo(newData, _data.Length);
            _data = newData;
        }

        public byte[] ToArray()
        {
            return _data ?? (new byte[0]);
        }
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
