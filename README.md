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

```csharp
services.AddQuillCSharp(); // CSharpWorkspace + ICSharpLanguageService
```

```csharp
// Une fois : projet Roslyn avec les références et options de l'exécution.
var project = workspace.AddProject(new CSharpProjectOptions
{
    Name = "Script",
    References = CSharpReferences.FromTrustedPlatformAssemblies(),
    CompilationOptions = new(OutputKind.ConsoleApplication),
    AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
});

// À chaque demande : instantané du texte de l'éditeur.
var document = project.CreateDocument(text);              // texte = source
var wrapped = project.CreateDocument(source, text, start); // texte placé dans un source généré

var completions = await languageService.GetCompletionsAsync(document, caret);
var change = await languageService.GetChangeAsync(document, completions.Items[0]);
var diagnostics = await languageService.GetDiagnosticsAsync(document);
```

Les positions reçues et rendues sont toujours celles du texte de l'éditeur.

## Développement

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publier une version : `git tag v0.1.0 && git push origin v0.1.0`.

## Licence

MIT
