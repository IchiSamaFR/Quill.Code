namespace Quill.Code.SqlServer.Constants;

/// <summary>Constantes de connexion et d'erreurs SQL Server.</summary>
internal static class SqlServerConstants
{
    internal const int ReadConnectTimeoutSeconds = 8;

    internal const int ReadCommandTimeoutSeconds = 120;

    /// <summary>« Must declare the scalar variable ».</summary>
    internal const int UndeclaredVariableError = 137;
}
