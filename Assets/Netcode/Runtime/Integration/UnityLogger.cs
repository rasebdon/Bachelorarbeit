using Netcode.Runtime.Communication.Common.Logging;
using System;
using UnityEngine;

namespace Netcode.Runtime.Integration
{
    public class UnityLogger<T> : ILogger<T>
    {
        private readonly LogLevel _logLevel = LogLevel.Error;
        private readonly string _objectName = "<error>";

        public UnityLogger(LogLevel logLevel)
        {
            _objectName = typeof(T).Name;
            _logLevel = logLevel;
        }

        public void LogDetail(object message)
        {
            if (_logLevel > LogLevel.Detail)
            {
                return;
            }

            Debug.Log($"[{_objectName}][Detail]{message}");
        }

        public void LogInfo(object message)
        {
            if (_logLevel > LogLevel.Info)
            {
                return;
            }

            Debug.Log($"[{_objectName}][Info]{message}");
        }

        public void LogWarning(object message)
        {
            if (_logLevel > LogLevel.Warning)
            {
                return;
            }

            Debug.LogWarning($"[{_objectName}][Warning]{message}");
        }

        public void LogError(object message, Exception ex)
        {
            if (_logLevel > LogLevel.Error)
            {
                return;
            }

            Debug.LogError($"[{_objectName}][Error]{message}");
            Debug.LogException(ex);
        }

        public void LogFatal(object message, Exception ex)
        {
            if (_logLevel > LogLevel.Fatal)
            {
                return;
            }

            Debug.LogError($"[{_objectName}][Fatal]{message}");
            Debug.LogException(ex);
        }

        public void LogError(Exception ex)
        {
            LogError("", ex);
        }

        public void LogFatal(Exception ex)
        {
            LogFatal("", ex);
        }
    }
}