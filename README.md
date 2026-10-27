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

## Utilisation (à venir)

```csharp
services.AddQuillCode();
services.AddQuillCSharp();
```

## Développement

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publier une version : `git tag v0.1.0 && git push origin v0.1.0`.

## Licence

MIT
