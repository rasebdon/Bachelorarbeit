using Netcode.Channeling;
using System;

namespace Netcode.Runtime.Behaviour
{
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase
    {
        public NetworkVariable(T value) : this(value, ChannelType.Environment, true) { }
        public NetworkVariable(T value, ChannelType channelType, bool reliable) : base(value, channelType, reliable) 
        {
            _onValueChange += InvokeOnValueChange;
        }

        private void InvokeOnValueChange(object arg1, object arg2)
        {
            OnValueChange?.Invoke((T)arg1, (T)arg2);
        }

        public Action<T, T> OnValueChange;

        public void SetValue(T value)
        {
            Value = value;
        }

        public T GetValue()
        {
            return (T)Value;
        }

        public override string ToString()
        {
            return ((T)Value).ToString();
        }
    }
}
