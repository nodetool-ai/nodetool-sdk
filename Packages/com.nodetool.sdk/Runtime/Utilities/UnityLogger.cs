using System;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace Nodetool.SDK.Unity
{
    public sealed class UnityLogger : ILogger
    {
        private readonly string _category;

        public UnityLogger(string category)
        {
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            if (exception != null)
            {
                Debug.LogException(exception);
            }

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Debug.LogError($"[{_category}] {message}");
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"[{_category}] {message}");
                    break;
                case LogLevel.Information:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    Debug.Log($"[{_category}] {message}");
                    break;
            }
        }
    }
}
