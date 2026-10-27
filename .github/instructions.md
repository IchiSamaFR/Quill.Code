# Quill.Code - Instructions partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

Quill.Code est une bibliothèque .NET (paquets NuGet) qui regroupe les briques communes aux éditeurs de code de **Querify** (MAUI) et **PocketForge** (WPF) : coloration syntaxique, autocomplétion, diagnostics, survol, aide à la signature et algorithmes d'édition.

**Règle fondatrice** : aucune dépendance à un framework d'interface. Pas de WPF, MAUI, `Brush`, `Color` de framework ni de contrôle. La bibliothèque produit des données (zones colorées, propositions, diagnostics) ; chaque application les affiche.

---

## Architecture

`Quill.Code` ← `Quill.Code.[Langage]` (un projet par langage). Un langage ne référence jamais un autre langage.

* **`Quill.Code`** (net8.0) : modèles (`Completion`, `Analysis`, `Text`), lecture des `.xshd`, découpage en zones colorées, algorithmes d'édition. Seule dépendance : `Microsoft.Extensions.DependencyInjection.Abstractions`.
* **`Quill.Code.CSharp`** (net8.0) : C# via Roslyn. `CSharpWorkspace` (composition MEF unique) → `CSharpProject` (références, options) → `CSharpDocument` (texte de l'éditeur placé dans un source, à la position `TextStart`). `ICSharpLanguageService` fournit autocomplétion, diagnostics, survol et signatures ; ses positions sont toujours celles du texte de l'éditeur.
* **`Quill.Code.Sql`** (à venir) : lexer, contexte de complétion, diagnostics T-SQL.
* **`Quill.Code.Tests`** (net10.0, xUnit).

Les contrats « fournisseur » (requête avec connexion, langage, paramètres…) restent dans les applications : Quill expose des moteurs, chaque app les branche sur ses propres contrats.

Ce qui est propre à une application reste dans l'application : DbContext EF de Querify, directives NuGet et exécution des scripts de PocketForge, fichiers `.xshd` spécifiques.

---

## Conventions

* **Un type par fichier**, nommé comme lui. Seule exception : une classe privée imbriquée.
* **Injection de dépendances** : pas d'attribut `[AutoRegister]` (propre à chaque app). Chaque paquet expose une méthode `AddQuillXxx(this IServiceCollection)`.
* **API publique minimale** : `internal` par défaut, `public` seulement ce que les apps consomment. `sealed` par défaut.
* **Async** : `CancellationToken` en dernier paramètre de toute méthode asynchrone ; accès concurrents à un workspace Roslyn protégés par un `SemaphoreSlim`.
* **Versions de paquets** : uniquement dans `src/Directory.Packages.props`, jamais de `Version` dans un `PackageReference`.
* **Changements cassants** : notés dans `CHANGELOG.md` ; version majeure à partir de 1.0.

## Commentaires et langue

* Noms de types et de membres en anglais ; commentaires, documentation XML et messages de commit en français.
* `/// <summary>` sur tout membre public (documentation embarquée dans le paquet).
* Commentaires succincts : le **pourquoi**, jamais le quoi.

## Commits

Format `type: description` en français (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `test:`).

## Validation

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```
