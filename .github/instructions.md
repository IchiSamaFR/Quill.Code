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
* **`Quill.Code.CSharp`** (net8.0) : C# via Roslyn. Public : `CSharpLanguageService`, `CSharpLanguageOptions`, `CSharpReferences` et, dans `Compilation`, `CSharpCompileOptions`, `CSharpCompilationResult`, `CSharpRunAbandonedException` ; tout le reste est interne.
  * `CSharpLanguageService(() => options)` est le seul point d'entrée : il possède son projet (créé à la première demande) et travaille directement sur le texte de l'éditeur. `SourceTransform` donne le source analysé, de même longueur que le texte (ex. directives neutralisées) ; `TextReferences` ajoute les références que le texte déclare ; `SetSource`/`SetSourceFileAsync`/`RemoveSource` ajoutent des sources à la volée (appliquées à la prochaine demande, compilées avec le texte). Positions toujours celles du texte de l'éditeur.
  * `Roslyn/` (interne) : `RoslynHost` (composition MEF unique, projets tirés en instantanés d'une solution vide : rien à libérer), `CSharpProject` (sources à la volée, références du texte), `CSharpDocument`.
  * `Features/` (interne) : un moteur statique par fonctionnalité (`CSharpCompletionEngine`, `CSharpDiagnosticEngine`, `CSharpHoverEngine`, `CSharpSignatureHelpEngine`) sur un `CSharpDocument` ; le service les appelle sur le pool de threads.
  * `Compilation/` : `CompileAsync` (émission, PDB facultatif, chargement dans un contexte déchargeable) et exécution (`CSharpCompilationResult.RunAsync` / `RunEntryPointAsync` : hors du thread appelant, abandon à l'annulation, déchargement à la fin réelle — le contrat d'appel reste à l'application). Runtime des scripts (`CSharpLanguageOptions.IncludeScriptRuntime`) : `Log`, `LogLevel`, `Script` générés dans chaque assembly et branchés par réflexion (`CSharpCompileOptions.Log`, `Script.CancellationToken`), donc propres à chaque exécution et sans référence vers l'application.
* **`Quill.Code.Sql`** (net8.0) : T-SQL sans connexion. `SqlLexer` (tolérant au texte en cours de frappe), `SqlContextAnalyzer` (contexte statique : `Analyze(texte, curseur, base)` donne la nature attendue, le qualificatif, les tables/alias du lot courant délimité par GO et la base du dernier `USE` ; `AnalyzeScope` pour le survol), `SqlSyntaxChecker` (ScriptDom `TSql170Parser`, noms non vérifiés) et `SqlLanguageService(SqlLanguageOptions)`, seul point d'entrée : diagnostics, autocomplétion (mots-clés statiques `SqlKeywords`, objets et colonnes du catalogue selon le contexte, mots du document, variables de l'hôte) et survol (colonnes, tables, alias, schémas, bases, variables déclarées ou fournies). `Schema/` : modèle (`SqlDatabaseInfo`, `SqlDatabaseSchema`, `SqlSchemaObject`, `SqlColumn`) et `ISqlCatalog`, implémenté par l'hôte (Quill ne se connecte à rien ; une base indisponible se rend vide, une exception vaut absence de schéma) ou par `SqlCatalog` en mémoire. Les services par fonctionnalité sont internes.
* **`Quill.Code.Tests`** (net10.0, xUnit).

Contrats sur le texte brut dans `Quill.Code` (`ICodeCompletion`, `ICodeDiagnostic`, `ICodeHover`, `ICodeSignature`, réunis par `ICodeLanguageService`), implémentés par `CSharpLanguageService` et `SqlLanguageService` (pas d'interface propre à un langage : les apps utilisent la classe pour la compilation et les sources) : un contrôle d'éditeur s'y lie sans dépendre de Roslyn. Les contrats plus riches d'une application (requête avec connexion, langage, paramètres…) restent chez elle et se branchent sur les moteurs de Quill.

Ce qui est propre à une application reste dans l'application : DbContext EF de Querify, directives NuGet et point d'entrée des scripts de PocketForge, fichiers `.xshd` spécifiques.

---

## Conventions

* **Un type par fichier**, nommé comme lui. Seule exception : une classe privée imbriquée.
* **Instanciation** : pas d'injection de dépendances dans Quill (ni conteneur, ni registre statique, ni `[AutoRegister]`). La règle ne vaut que pour la bibliothèque : les apps instancient les services (`new CSharpLanguageService(() => options)`) ou les enregistrent dans leur conteneur par une fabrique ; le travail coûteux (MEF, références) est différé à la première demande.
* **API publique minimale** : `internal` par défaut, `public` seulement ce que les apps consomment. `sealed` par défaut.
* **Async** : `CancellationToken` en dernier paramètre de toute méthode asynchrone ; le workspace Roslyn n'est jamais modifié (projets et documents en instantanés), les demandes concurrentes n'ont pas de verrou.
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
