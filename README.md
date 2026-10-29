# Quill.Code

Briques communes aux éditeurs de code .NET, indépendantes du framework d'interface (WPF, MAUI…) : coloration syntaxique, autocomplétion, diagnostics et algorithmes d'édition.

## Paquets

| Paquet | Contenu |
|---|---|
| `Quill.Code` | Modèles, contrats, lecture des définitions `.xshd`, découpage en zones colorées, algorithmes d'édition. Aucune dépendance d'interface. |
| `Quill.Code.CSharp` | C# via Roslyn : autocomplétion, diagnostics, survol, aide à la signature, compilation et chargement. |
| `Quill.Code.Sql` | T-SQL : lexer, analyse du contexte de saisie (ce qu'attend la grammaire au curseur, tables et alias du lot, base du `USE`) et diagnostics de syntaxe par ScriptDom. Autocomplétion, survol et signature : à venir (schéma de la base requis). |

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
using var language = new CSharpLanguageService("Script", () => new CSharpProjectOptions
{
    References = CSharpReferences.FromTrustedPlatformAssemblies(),
    CompilationOptions = new(OutputKind.ConsoleApplication),
    AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    SourceTransform = code => code,   // facultatif : texte -> source, à longueur égale
    TextReferences = code => [],      // facultatif : références déclarées par le texte
});

var completions = await language.GetCompletionsAsync(code, caret);
var change = await language.GetChangeAsync(code, completions.Items[0]);
var diagnostics = await language.GetDiagnosticsAsync(code);
```

Texte placé dans du code généré (ex. requête LINQ dans une méthode d'un DbContext) : `SourceTransform` rend le source et la position du texte.

```csharp
var language = new CSharpLanguageService("Linq", () => new CSharpProjectOptions
{
    References = references,
    SourceTransform = query => new CSharpSource(prefix + query + suffix, prefix.Length),
    IncludeSemanticDiagnostics = hasModel,  // faux : fautes de syntaxe seulement
    AllowImports = false,                   // types non importés insérés avec leur nom complet
});
```

Pour plusieurs textes dans des contextes différents d'un même projet, `CSharpDocument` reste disponible (`language.Project.CreateDocument(source, text, start)` et les surcharges qui prennent un document).

Compilation du même projet que l'éditeur, chargée dans un contexte déchargeable (l'appel du code reste à l'application) :

```csharp
using var compiled = await language.CompileAsync(code, new CSharpCompileOptions
{
    EmitDebugInformation = true,           // numéros de ligne dans les piles d'appels
    AssemblyFiles = packageAssemblies,     // assemblies chargeables, lues en mémoire
});

if (!compiled.Success)
    foreach (var error in compiled.Errors)
        Console.WriteLine(error.Describe(code)); // « Ligne 2, colonne 6 : CS0103 … »
else
    Run(compiled.Assembly!);
```

Les positions reçues et rendues sont toujours celles du texte de l'éditeur.

Sources ajoutées à la volée (ex. classe utilitaire) : visibles par la complétion et l'analyse, compilées avec le texte.

```csharp
language.Project.SetSource("FileHelper.cs", helperCode);                // nom → code
var key = await language.Project.SetSourceFileAsync(@"C:\Helpers\FileHelper.cs"); // relu à chaque appel
language.Project.RemoveSource(key);
language.Project.SourcesChanged += (_, _) => RefreshDiagnostics();
```

Une erreur dans une source est rendue en position 0 avec le fichier : « FileHelper.cs (12, 5) : … ».

## Développement

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publier une version : `git tag v0.1.0 && git push origin v0.1.0`.

## Licence

MIT
