# Quill.Code - Instructions partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

Quill.Code est une bibliothèque .NET (paquets NuGet) qui regroupe les briques communes aux éditeurs de code de **Querify** (MAUI) et **PocketForge** (WPF) : coloration syntaxique, autocomplétion, diagnostics, survol, aide à la signature et algorithmes d'édition.

**Règle fondatrice** : aucune dépendance à un framework d'interface. Pas de WPF, MAUI, `Brush`, `Color` de framework ni de contrôle. La bibliothèque produit des données (zones colorées, propositions, diagnostics) ; chaque application les affiche.

---

## Architecture

`Quill.Code` ← `Quill.Code.[Langage]` (un projet par langage). Un langage ne référence jamais un autre langage.

* **`Quill.Code`** (net8.0) : modèles et contrats (`Completion`, `Analysis`, `Text`), coloration (`Highlighting`), édition (`Editing`). Aucune dépendance.
* **`Quill.Code.CSharp`** (net8.0) : C# via Roslyn. `CSharpLanguageService(nom, options)` possède son projet (créé à la première demande) et travaille directement sur le texte de l'éditeur : `SourceTransform` le place dans son source (tel quel, neutralisé ou enveloppé de code généré), `TextReferences` ajoute les références qu'il déclare. Autocomplétion, diagnostics, survol et signatures ; compilation (`CompileAsync` : émission, PDB facultatif, chargement dans un contexte déchargeable — l'appel du code compilé reste à l'application). Positions toujours celles du texte de l'éditeur.
* **`Quill.Code.Sql`** (à venir) : lexer, contexte de complétion, diagnostics T-SQL.
* **`Quill.Code.Tests`** (net10.0, xUnit).

Contrats sur le texte brut dans `Quill.Code` (`ICodeCompletionProvider`, `ICodeDiagnosticProvider`), implémentés par `ICSharpLanguageService` : un contrôle d'éditeur s'y lie sans dépendre de Roslyn. Les contrats plus riches d'une application (requête avec connexion, langage, paramètres…) restent chez elle et se branchent sur les moteurs de Quill.

Ce qui est propre à une application reste dans l'application : DbContext EF de Querify, directives NuGet et exécution des scripts de PocketForge, fichiers `.xshd` spécifiques.

---

## Conventions

* **Un type par fichier**, nommé comme lui. Seule exception : une classe privée imbriquée.
* **Instanciation** : pas d'injection de dépendances dans Quill ni d'attribut `[AutoRegister]` (propre à chaque app). Les apps instancient les services (`new CSharpLanguageService(nom, options)`) ou les enregistrent par une fabrique ; le travail coûteux (MEF, références) est différé à la première demande.
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
