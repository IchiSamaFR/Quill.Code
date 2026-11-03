# Quill.Code - Instructions partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

Quill.Code est une bibliothèque .NET (paquets NuGet) qui regroupe les briques communes aux éditeurs de code de **Querify** (MAUI) et **PocketForge** (WPF) : coloration syntaxique, autocomplétion, diagnostics, survol, aide à la signature et algorithmes d'édition.

**Règle fondatrice** : aucune dépendance à un framework d'interface. Pas de WPF, MAUI, `Brush`, `Color` de framework ni de contrôle. La bibliothèque produit des données (zones colorées, propositions, diagnostics) ; chaque application les affiche.

---

## Architecture

`Quill.Code` ← `Quill.Code.[Langage]` (un projet par langage). Un langage ne référence jamais un autre langage.

`Quill.Code.CSharp` ← `Quill.Code.CSharp.Linq` (C# dans un contexte EF, pas un autre langage, sans moteur) ← `Quill.Code.CSharp.Linq.[Moteur]` (un projet par moteur : `ILinqDatabaseProvider`).

`Quill.Data.Sql` ← `Quill.Data.Sql.[Moteur]` (un projet par moteur, seul à dépendre d'un pilote) ; `Quill.Data.Sql` ← `Quill.Code.SqlServer` et `Quill.Code.CSharp.Linq`. Un `Quill.Code.*` ne référence jamais un `Quill.Data.Sql.[Moteur]` : l'éditeur reste sans connexion. Seuls les exécuteurs se connectent : `SqlServerQueryExecutor` (Microsoft.Data.SqlClient) et `LinqQueryExecutor` (EF Core, par le fournisseur du moteur : `SqlServerLinqProvider`).

* **`Quill.Code`** (net8.0) : modèles et contrats (`Completion`, `Analysis`, `Text`, `Scripting.LogLevel`), coloration (`Highlighting`), édition (`Editing`), exécution commune aux langages (`Execution` : options, jeux de résultats, paramètres `QueryParameter` / `QueryParameterType` ; plafond `Constants/QueryExecutionLimits`). Aucune dépendance.
* **`Quill.Code.CSharp`** (net8.0) : C# via Roslyn. Public : `CSharpLanguageService`, `CSharpLanguageOptions`, `CSharpReferences` et, dans `Compilation`, `CSharpCompileOptions`, `CSharpCompilationResult`, `CSharpRunAbandonedException` ; tout le reste est interne.
  * `CSharpLanguageService(() => options)` est le seul point d'entrée : il possède son projet (créé à la première demande) et travaille directement sur le texte de l'éditeur. `SourceTransform` donne le source analysé, de même longueur que le texte (ex. directives neutralisées) ; `TextReferences` ajoute les références que le texte déclare ; `SetSource`/`SetSourceFileAsync`/`RemoveSource` ajoutent des sources à la volée (appliquées à la prochaine demande, compilées avec le texte). Positions toujours celles du texte de l'éditeur.
  * `Roslyn/` (interne) : `RoslynHost` (composition MEF unique, projets tirés en instantanés d'une solution vide : rien à libérer), `CSharpProject` (sources à la volée, références du texte), `CSharpDocument`.
  * `Features/` (interne) : un moteur statique par fonctionnalité (`CSharpCompletionEngine`, `CSharpDiagnosticEngine`, `CSharpHoverEngine`, `CSharpSignatureHelpEngine`) sur un `CSharpDocument` ; le service les appelle sur le pool de threads.
  * `Compilation/` : `CompileAsync` (émission, PDB facultatif, chargement dans un contexte déchargeable) et exécution (`CSharpLanguageService.RunAsync` : compilation, point d'entrée et déchargement en un appel, sans retour — sortie par `CSharpCompileOptions.Log`, échec de compilation levé en `CSharpCompilationException` ; ou en deux temps `CSharpCompilationResult.RunAsync` / `RunEntryPointAsync` : hors du thread appelant, abandon à l'annulation, déchargement à la fin réelle — le contrat d'appel reste à l'application). Runtime des scripts (`CSharpLanguageOptions.IncludeScriptRuntime`) : `Log`, `LogLevel`, `Script` générés dans chaque assembly et branchés par réflexion (`CSharpCompileOptions.Log`, `Script.CancellationToken`), donc propres à chaque exécution et sans référence vers l'application.
* **`Quill.Code.SqlServer`** (net8.0, ScriptDom, Microsoft.Data.SqlClient) : T-SQL sans connexion. `SqlLexer` (tolérant au texte en cours de frappe), `SqlContextAnalyzer` (contexte statique : `Analyze(texte, curseur, base)` donne la nature attendue, le qualificatif, les tables/alias du lot courant délimité par GO et la base du dernier `USE` ; `AnalyzeScope` pour le survol), `SqlSyntaxChecker` (ScriptDom `TSql170Parser`, noms non vérifiés) et `SqlLanguageService(SqlLanguageOptions)`, seul point d'entrée : diagnostics, autocomplétion (mots-clés statiques `SqlKeywords`, objets et colonnes du catalogue selon le contexte, mots du document, variables de l'hôte) et survol (colonnes, tables, alias, schémas, bases, variables déclarées ou fournies). `Schema/` : `ISqlCatalog` sur le modèle de `Quill.Data.Sql`, implémenté par l'hôte (Quill ne se connecte à rien ; une base indisponible se rend vide, une exception vaut absence de schéma) ou par `SqlCatalog` en mémoire. À la racine, à côté de `SqlLanguageService` : `ISqlQueryExecutor` (chaîne de connexion → `RunAsync` d'un script, lots GO compris, sans retour : tout passe par `DataRunOptions` ; `ReadAsync` d'une requête paramétrée, erreurs levées) et `SqlServerQueryExecutor` (Microsoft.Data.SqlClient, comportement de SSMS). `RunAsync(chaîne, script, DataRunOptions?)` remet PRINT (Information, `Code`), lignes affectées (Information, `Host`) et erreurs SQL (Error, les lots suivants s'exécutent quand même) par `Log`, chaque jeu par `Data` ; l'annulation lève `OperationCanceledException`. Le bilan affiché (durée, erreurs, annulation) est à l'application. `Execution/` : `SqlBatchSplitter` (interne) ; paramètres et plafond viennent de `Quill.Code`. Les services par fonctionnalité sont internes.
* **`Quill.Code.CSharp.Linq`** (net10.0, EF Core Relational sans moteur : EF Core 10 ne cible que net10.0) : requêtes LINQ / EF en C# natif (instructions de haut niveau) sur un DbContext généré depuis un `SqlDatabaseSchema`, contexte `Db`, paramètres utilisables par leur nom (sans @), données exposées par `.Dump(titre)`, `Log.*` du runtime de script. Public : `ILinqDatabaseProvider` (le moteur : type C# d'une colonne, assemblies du fournisseur EF ajoutées aux références, chaîne de connexion, `Configure` du contexte ; une instance par application, les DbContext compilés et les références étant mis en cache par fournisseur), `LinqLanguageService(LinqLanguageOptions)` (éditeur : `Provider`, structure relue à chaque demande par `Schema` ; sans fournisseur, null ou exception = C# seul ; `Parameters`) et `LinqQueryExecutor(provider)`, calqué sur `SqlServerQueryExecutor` : `RunAsync(chaîne, structure, script, DataRunOptions?)` sans retour (contexte, SQL généré et lignes lues en Information/Host, `Log.*` en Code, erreurs de compilation et exceptions du script en Error/Code, chaque Dump par `Data` ; annulation levée, `CSharpRunAbandonedException` si le script continue en arrière-plan) et `ReadAsync(chaîne, base, structure, requête, paramètres, maxRows)` (dernier Dump, erreurs levées en `InvalidOperationException`). Interne : `Modeling/` (`EfModelGenerator`), `Compilation/` (`LinqQuerySource` : cadre généré autour de la requête ; `LinqCompiler` : modèle, `QueryGlobals` et requêtes, un `CSharpLanguageService` par `QueryGlobals`, partagé par l'éditeur et l'exécution ; `LinqContexts` : cache fournisseur + structure → modèle compilé et modèle + paramètres → `QueryGlobals`, tenu par la structure, échec non gardé), `Execution/` (`LinqParameter`, `LinqResultConverter` : valeur d'un Dump → `QueryResultSet`).
* **`Quill.Code.CSharp.Linq.SqlServer`** (net10.0, EF Core SqlServer) : `SqlServerLinqProvider(applicationName)`, seul public : types SQL Server → C# (mapping EF Core par défaut ; `sql_variant`, `hierarchyid`… ignorés), `UseSqlServer`, chaîne de connexion par `SqlConnectionStringBuilder` (nom d'application, base).
* **`Quill.Data.Sql`** (net8.0, aucune dépendance) : structure d'une base — `SqlDatabaseInfo`, `SqlDatabaseSchema` (index par nom, `dbo` préféré), `SqlSchemaObject` (+ `Comment`), `SqlColumn` (type affiché et système, nullabilité, rang dans la clé primaire, précision/échelle, FK, identity, calculée, commentaire, `Description` façon SSMS), `SqlSchemaObjectKind` — et contrat de lecture `ISqlSchemaReader` (bases, objets d'une nature, colonnes d'un objet, schéma complet en un aller-retour).
* **`Quill.Data.Sql.SqlServer`** (net8.0, Microsoft.Data.SqlClient) : `SqlServerSchemaReader`, lecture des vues `sys.*`.
* **`Quill.Code.Tests`** (net10.0, xUnit).

Contrats sur le texte brut dans `Quill.Code` (`ICodeCompletion`, `ICodeDiagnostic`, `ICodeHover`, `ICodeSignature`, réunis par `ICodeLanguageService`), implémentés par `CSharpLanguageService` et `SqlLanguageService` (pas d'interface propre à un langage : les apps utilisent la classe pour la compilation et les sources) : un contrôle d'éditeur s'y lie sans dépendre de Roslyn. Les contrats plus riches d'une application (requête avec connexion, langage, paramètres…) restent chez elle et se branchent sur les moteurs de Quill.

Ce qui est propre à une application reste dans l'application : connexions et cache des structures de Querify (la même instance de `SqlDatabaseSchema` donne le même DbContext compilé), directives NuGet et point d'entrée des scripts de PocketForge, fichiers `.xshd` spécifiques.

---

## Conventions

* **Réutiliser avant de créer** : avant d'ajouter une classe, un enum ou un modèle, chercher un type existant qui couvre le besoin, de Quill en priorité puis du projet, quitte à l'étendre (une valeur d'enum, une propriété) ; ne créer un type que si aucun ne convient, et dans Quill, préférer un type commun (`Quill.Code`) aux variantes par langage (ex. les messages d'exécution sont des `LogEntry` de Quill, pas un modèle propre).
* **Un type par fichier**, nommé comme lui. Seule exception : une classe privée imbriquée.
* **Messages d'exécution** (`Quill.Code.Scripting`) : `LogEntry(Level, Text, Source)` horodaté (`Timestamp`), reçu par `RunOptions.Log` ; `LogLevel` est la gravité (noms et valeurs de Microsoft.Extensions.Logging), `LogSource` la provenance (`Host` : l'exécuteur ; `Code` : `Log.*` d'un script, PRINT et erreurs SQL d'un lot) ; aucun autre modèle de message.
* **Options d'exécution** (`Quill.Code.Execution`) : `RunOptions` (`Log`, commun à toute exécution) ← `CSharpCompileOptions` (compilation et `RunAsync` C#) et ← `DataRunOptions` (`Log` + `Data` : chaque `QueryResultSet` remis dès sa lecture ; exécution SQL, LINQ). Les jeux de résultats (`QueryResultSet`, `QueryResultColumn`, `QueryResultRow`) et les paramètres (`QueryParameter`, `QueryParameterType`) y sont aussi, communs aux langages. Même contrat pour chaque exécuteur : `RunAsync` sans retour (tout par `Log` et `Data`, erreurs du code en Error sans exception, annulation levée), `ReadAsync` rend un jeu et lève ses erreurs.
* **Constantes** : jamais déclarées dans une classe de travail. Chaque projet les range dans `Constants/` (namespace `<Projet>.Constants`), une classe statique par contexte : `CodeLanguages`, `CompletionConstants`, `EditingConstants`, `HighlightingConstants`, `QueryExecutionLimits` (Quill.Code) ; `CSharpScriptConstants`, `CSharpDiagnosticConstants` (CSharp) ; `LinqQueryConstants`, `EfModelConstants`, `LinqExecutionConstants` (CSharp.Linq) ; `QueryResultConstants`, `SqlAnalysisConstants`, `SqlServerConstants` (Sql) ; `SqlSchemaConstants` (Data.Sql) ; `SqlServerConstants`, `SqlServerQueries` (Data.Sql.SqlServer). `internal` sauf si la constante fait partie de l'API.
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
