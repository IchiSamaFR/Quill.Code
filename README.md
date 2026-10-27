# Quill.Code

Briques communes aux éditeurs de code .NET, indépendantes du framework d'interface (WPF, MAUI…) : coloration syntaxique, autocomplétion, diagnostics et algorithmes d'édition.

## Paquets

| Paquet | Contenu |
|---|---|
| `Quill.Code` | Modèles, contrats, lecture des définitions `.xshd`, découpage en zones colorées, algorithmes d'édition. Aucune dépendance d'interface. |
| `Quill.Code.CSharp` | C# via Roslyn : autocomplétion, diagnostics, survol, aide à la signature. |

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

Les positions reçues et rendues sont toujours celles du texte de l'éditeur.

## Développement

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publier une version : `git tag v0.1.0 && git push origin v0.1.0`.

## Licence

MIT
