﻿using System;

namespace Netcode.Runtime.Communication.Common.Logging
{
    public interface ILogger<T>
    {
        public void LogDetail(object message);
        public void LogInfo(object message);
        public void LogWarning(object message);
        public void LogError(object message, Exception ex);
        public void LogError(Exception ex);
        public void LogFatal(object message, Exception ex);
        public void LogFatal(Exception ex);
    }
}
