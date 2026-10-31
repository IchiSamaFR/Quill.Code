namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>Plafonds et libelles de l'execution LINQ.</summary>
internal static class LinqExecutionConstants
{
    /// <summary>Sortie Log d'un script : une boucle d'affichage ne doit pas saturer la memoire ni l'onglet Messages.</summary>
    internal const int MaxLogLength = 200_000;

    /// <summary>Erreurs remises quand le code genere (modele, parametres) ne compile pas.</summary>
    internal const int MaxGeneratedErrors = 20;

    /// <summary>Erreurs de compilation reprises dans l'exception d'une lecture (ReadAsync).</summary>
    internal const int MaxReadErrors = 5;

    /// <summary>Octets affiches d'une valeur binaire, comme l'execution SQL.</summary>
    internal const int MaxBinaryBytes = 64;

    /// <summary>Colonne d'un Dump de valeurs simples.</summary>
    internal const string ScalarColumn = "Valeur";
}
