# Quill.Code - Instructions partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

Quill.Code est une bibliothèque .NET (paquets NuGet) qui regroupe les briques communes aux éditeurs de code de **Querify** (MAUI) et **PocketForge** (WPF) : coloration syntaxique, autocomplétion, diagnostics, survol, aide à la signature et algorithmes d'édition.

**Règle fondatrice** : aucune dépendance à un framework d'interface. Pas de WPF, MAUI, `Brush`, `Color` de framework ni de contrôle. La bibliothèque produit des données (zones colorées, propositions, diagnostics) ; chaque application les affiche.

---

## Architecture

`Quill.Code` ← `Quill.Code.[Langage]` (un projet par langage). Un langage ne référence jamais un autre langage.

* **`Quill.Code`** (net8.0) : modèles et contrats (`Completion`, `Analysis`, `Text`, `Scripting.LogLevel`), coloration (`Highlighting`), édition (`Editing`). Aucune dépendance.
* **`Quill.Code.CSharp`** (net8.0) : C# via Roslyn. `CSharpLanguageService(nom, options)` possède son projet (créé à la première demande) et travaille directement sur le texte de l'éditeur : `SourceTransform` le place dans son source (tel quel, neutralisé ou enveloppé de code généré), `TextReferences` ajoute les références qu'il déclare ; `Project.SetSource`/`SetSourceFileAsync`/`RemoveSource` ajoutent des sources à la volée (appliquées à la prochaine demande, compilées avec le texte). Autocomplétion, diagnostics, survol et signatures (un service par fonctionnalité — `CSharpCompletionService`, `CSharpDiagnosticService`, `CSharpHoverService`, `CSharpSignatureService`, construits sur le `CSharpProject` — que `CSharpLanguageService` ne fait que relayer) ; compilation (`CompileAsync` : émission, PDB facultatif, chargement dans un contexte déchargeable) et exécution (`CSharpCompilationResult.RunAsync` / `RunEntryPointAsync` : hors du thread appelant, abandon à l'annulation, déchargement à la fin réelle — le contrat d'appel reste à l'application). Runtime des scripts (`CSharpProjectOptions.IncludeScriptRuntime`) : `Log`, `LogLevel`, `Script` générés dans chaque assembly et branchés par réflexion (`CSharpCompileOptions.Log`, `Script.CancellationToken`), donc propres à chaque exécution et sans référence vers l'application. Positions toujours celles du texte de l'éditeur.
* **`Quill.Code.Sql`** (net8.0) : T-SQL sans connexion. `SqlLexer` (tolérant au texte en cours de frappe), `SqlContextAnalyzer` (contexte statique : `Analyze(texte, curseur, base)` donne la nature attendue, le qualificatif, les tables/alias du lot courant délimité par GO et la base du dernier `USE` ; `AnalyzeScope` pour le survol), `SqlSyntaxChecker` (ScriptDom `TSql170Parser`, noms non vérifiés) et `SqlLanguageService : ICodeLanguageProvider`, simple relais vers `SqlCompletionService`, `SqlDiagnosticService`, `SqlHoverService`, `SqlSignatureService` (diagnostics réels ; les autres vides tant que l'hôte ne fournit pas le schéma de la base, par ses propres `ICodeCompletion`/`ICodeHover`/`ICodeSignature` passés au constructeur).
* **`Quill.Code.Tests`** (net10.0, xUnit).

Contrats sur le texte brut dans `Quill.Code` (`ICodeCompletion`, `ICodeDiagnostic`, `ICodeHover`, `ICodeSignature`, réunis par `ICodeLanguageProvider`), implémentés par `CSharpLanguageService` et `SqlLanguageService` (pas d'interface propre à un langage : les apps utilisent la classe pour la compilation et le projet) : un contrôle d'éditeur s'y lie sans dépendre de Roslyn. Les contrats plus riches d'une application (requête avec connexion, langage, paramètres…) restent chez elle et se branchent sur les moteurs de Quill.

Ce qui est propre à une application reste dans l'application : DbContext EF de Querify, directives NuGet et point d'entrée des scripts de PocketForge, fichiers `.xshd` spécifiques.

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
