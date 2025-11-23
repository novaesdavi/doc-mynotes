using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

// Minimal logger provider that writes log lines to Console.Error
public sealed class ErrorConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ErrorConsoleLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new ErrorConsoleLogger(name));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class ErrorConsoleLogger : ILogger
    {
        private readonly string _categoryName;

        public ErrorConsoleLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) return;
            var message = formatter(state, exception);
            var time = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            var level = logLevel.ToString();
            var outMsg = $"{time} [{level}] {_categoryName}: {message}";
            if (exception != null)
            {
                outMsg += "\n" + exception;
            }
            Console.Error.WriteLine(outMsg);
        }
    }
}
