using Microsoft.EntityFrameworkCore;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq;

/// <summary>
/// Moteur de base d'une requete LINQ (ex. <c>SqlServerLinqProvider</c> de <c>Quill.Code.CSharp.Linq.SqlServer</c>) :
/// types des colonnes du DbContext genere, references du fournisseur EF et connexion. Une meme instance pour
/// l'editeur et l'execution : les DbContext compiles et les references sont mis en cache par fournisseur.
/// </summary>
public interface ILinqDatabaseProvider
{
    /// <summary>Assemblies du fournisseur EF et de son pilote, referencees par les requetes (ex. <c>EF.Functions</c> propres au moteur).</summary>
    IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>Type C# d'une colonne, sans nullabilite (ex. <c>int</c>, <c>string</c>) ; null : non mappable, la colonne est ignoree.</summary>
    string? GetClrType(SqlColumn column);

    /// <summary>
    /// Chaine de connexion d'une execution, sur <paramref name="database"/> (null : celle de la chaine) ;
    /// leve <see cref="ArgumentException"/> ou <see cref="FormatException"/> si elle est invalide.
    /// </summary>
    string BuildConnectionString(string connectionString, string? database);

    /// <summary>Branche le contexte EF sur la base (ex. <c>UseSqlServer</c>).</summary>
    void Configure(DbContextOptionsBuilder builder, string connectionString);
}
