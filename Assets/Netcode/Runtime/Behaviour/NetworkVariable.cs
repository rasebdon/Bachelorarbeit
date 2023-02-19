using Netcode.Behaviour;
using System;

namespace Netcode.Runtime.Behaviour
{
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase
    {
        public NetworkVariable(T value) : base(value) { }
        
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
