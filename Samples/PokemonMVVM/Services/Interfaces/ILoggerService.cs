using System;
using System.Collections.Generic;
using System.Text;

namespace Pokemon.Services.Interfaces;

public interface ILoggerService
{
    void Log(string message);
    void LogInfo(string message);
    void LogWarn(string message);
    void LogError(string message);
}
