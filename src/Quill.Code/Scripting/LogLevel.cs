namespace Quill.Code.Scripting;

/// <summary>
/// Severity of a message written during an execution (a script's <c>Log</c>, SQL PRINT and errors...): same names and
/// values as <c>Microsoft.Extensions.Logging.LogLevel</c>. The origin is carried separately (<see cref="LogSource"/>).
/// Its names and order also form the <c>LogLevel</c> enum generated in scripts.
/// </summary>
public enum LogLevel
{
    /// <summary>Most detailed messages.</summary>
    Trace,

    /// <summary>Investigation during debugging.</summary>
    Debug,

    /// <summary>General flow of the script.</summary>
    Information,

    /// <summary>Abnormal or unexpected event that does not stop the script.</summary>
    Warning,

    /// <summary>Failure of the current operation.</summary>
    Error,

    /// <summary>Serious failure requiring immediate attention.</summary>
    Critical,

    /// <summary>Writes nothing.</summary>
    None,
}
