namespace KoshelekRuWebService;

using System;

public static class MyLogger
{
    private static readonly Action<ILogger, string, Exception?> _infoLoggerMessage = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(0, "Info"),
        "{Param1}");

    private static readonly Action<ILogger, string, Exception?> _errorLoggerMessage = LoggerMessage.Define<string>(
    LogLevel.Error,
    new EventId(1, "Error"),
    "{Param1}");

    public static void Info(ILogger logger, string message) => _infoLoggerMessage(
        logger, message, null);

    public static void Error(ILogger logger, string message, Exception? ex = null) => _errorLoggerMessage(
        logger, message, ex);
}
