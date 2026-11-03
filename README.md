# Quill.Code

Briques communes aux éditeurs de code .NET, indépendantes du framework d'interface (WPF, MAUI…) : coloration syntaxique, autocomplétion, diagnostics et algorithmes d'édition.

## Paquets

| Paquet | Contenu |
|---|---|
| `Quill.Code` | Modèles, contrats, lecture des définitions `.xshd`, découpage en zones colorées, algorithmes d'édition. Aucune dépendance d'interface. |
| `Quill.Code.CSharp` | C# via Roslyn : autocomplétion, diagnostics, survol, aide à la signature, compilation et chargement. |
| `Quill.Code.CSharp.Linq` | LINQ / Entity Framework Core (net10.0), sans moteur : DbContext généré depuis la structure d'une base (`Quill.Data.Sql`), autocomplétion, diagnostics et survol C# dans ce contexte (`LinqLanguageService`), exécution (`LinqQueryExecutor`, même contrat que `SqlServerQueryExecutor`) ; le moteur est un `ILinqDatabaseProvider`. |
| `Quill.Code.CSharp.Linq.SqlServer` | `SqlServerLinqProvider` : LINQ sur SQL Server (types SQL Server, EF Core SqlServer, Microsoft.Data.SqlClient). |
| `Quill.Code.SqlServer` | T-SQL : lexer, analyse du contexte de saisie (ce qu'attend la grammaire au curseur, tables et alias du lot, base du `USE`) diagnostics de syntaxe par ScriptDom, autocomplétion et survol sur le catalogue fourni par l'hôte (`ISqlCatalog`) ; exécution sur SQL Server (`ISqlQueryExecutor`, `SqlServerQueryExecutor`). |
| `Quill.Data.Sql` | Structure d'une base (bases, schémas, tables, vues, procédures, colonnes) et contrat de lecture du catalogue (`ISqlSchemaReader`). Aucune dépendance. |
| `Quill.Data.Sql.SqlServer` | Lecture du catalogue SQL Server (`sys.*`) via Microsoft.Data.SqlClient. |

L'affichage reste à la charge de l'application : chaque app traduit les zones colorées, propositions et diagnostics vers ses propres contrôles.

## Installation

Les paquets sont publiés sur GitHub Packages. Ajouter la source dans le `nuget.config` de la solution consommatrice :

```xml
<configuration>
  <packageSources>
    <add key="github-ichisamafr" value="https://nuget.pkg.github.com/IchiSamaFR/index.json" />
  </packageSources>
</configuration>
```

Puis s'authentifier avec un jeton GitHub (scope `read:packages`) :

```
dotnet nuget update source github-ichisamafr --username IchiSamaFR --password <jeton> --store-password-in-clear-text
```

## Utilisation

Un service par contexte, avec son propre projet Roslyn, créé à la première demande (hors du thread de l'interface) :

```csharp
var language = new CSharpLanguageService(() => new CSharpLanguageOptions
{
    Name = "Script",
    References = CSharpReferences.FromTrustedPlatformAssemblies(),
    CompilationOptions = new(OutputKind.ConsoleApplication),
    AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    SourceTransform = code => code,   // facultatif : source analysé, de même longueur que le texte
    TextReferences = code => [],      // facultatif : références déclarées par le texte
    IncludeSemanticDiagnostics = true, // faux : fautes de syntaxe seulement
});

var completions = await language.GetCompletionsAsync(code, caret);
var change = await language.GetChangeAsync(code, completions.Items[0]);
var diagnostics = await language.GetDiagnosticsAsync(code);
```

`CSharpLanguageService` implémente `ICodeLanguageService` : un contrôle d'éditeur s'y lie sans dépendre de Roslyn. Sans options, le service analyse une bibliothèque qui référence le framework.

Compilation du même projet que l'éditeur, chargée dans un contexte déchargeable (l'appel du code reste à l'application) :

```csharp
using var compiled = await language.CompileAsync(code, new CSharpCompileOptions
{
    AssemblyFiles = packageAssemblies,     // assemblies chargeables, lues en mémoire
});

if (!compiled.Success)
    foreach (var error in compiled.Errors)
        Console.WriteLine(error.Describe(code)); // « Ligne 2, colonne 6 : CS0103 … »
else
    Run(compiled.Assembly!);
```

Script (point d'entrée ou instructions de niveau supérieur) compilé et exécuté en un appel, sans retour : la sortie passe par `Log`, l'assembly est déchargée à la fin.

```csharp
try
{
    await language.RunAsync(code, new CSharpCompileOptions { Log = e => Console.WriteLine($"{e.Timestamp:HH:mm:ss} {e.Level} : {e.Text}") }, token);
}
catch (CSharpCompilationException ex)
{
    foreach (var error in ex.Errors)
        Console.WriteLine(error.Describe(code));
}
```

Les positions reçues et rendues sont toujours celles du texte de l'éditeur.

Sources ajoutées à la volée (ex. classe utilitaire) : visibles par la complétion et l'analyse, compilées avec le texte.

```csharp
language.SetSource("FileHelper.cs", helperCode);                       // nom → code
var key = await language.SetSourceFileAsync(@"C:\Helpers\FileHelper.cs"); // relu à chaque appel
language.RemoveSource(key);
language.SourcesChanged += (_, _) => RefreshDiagnostics();
```

Une erreur dans une source est rendue en position 0 avec le fichier : « FileHelper.cs (12, 5) : … ».

### T-SQL

Quill ne se connecte à aucune base : l'hôte fournit un `ISqlCatalog` (bases, puis schéma complet d'une base, colonnes comprises), ou un `SqlCatalog` en mémoire.

```csharp
var language = new SqlLanguageService(new SqlLanguageOptions
{
    Catalog = new SqlCatalog([new SqlDatabaseSchema("Shop", objects)]), // ou l'implémentation de l'hôte
    Database = "Shop",                                                   // null : base par défaut du catalogue
    Variables = [new SqlVariable("@MaxRows", "int", "Paramètre")],       // connues sans DECLARE
});

var completions = await language.GetCompletionsAsync("SELECT u.| FROM dbo.Users u", caret); // colonnes de Users
```

Sans catalogue, l'autocomplétion propose les mots-clés, les mots du document et les variables. Un catalogue indisponible rend une liste vide (ou null) ; une exception est traitée comme une absence de schéma.

## Développement

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publier une version : `git tag v0.1.0 && git push origin v0.1.0`.

## Licence

MIT
