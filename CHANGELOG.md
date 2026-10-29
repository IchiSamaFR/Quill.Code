# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versions [SemVer](https://semver.org/lang/fr/).

## [Non publié]

### Modifié
- `Quill.Code.CSharp` simplifié : `CSharpLanguageService(Func<CSharpLanguageOptions>?)` est le seul point d'entrée (nom du projet dans les options) ; les sources à la volée y sont exposées directement (`SetSource`, `SetSourceFileAsync`, `RemoveSource`, `ClearSources`, `Sources`, `SourcesChanged`). Le service n'est plus `IDisposable` : l'hôte Roslyn est partagé et les projets sont des instantanés.
- `CSharpProjectOptions` devient `CSharpLanguageOptions` ; `CSharpReferences` passe dans `Quill.Code.CSharp`. `SourceTransform` rend une chaîne de même longueur que le texte (plus d'enveloppe de code généré).
- `ICodeLanguageProvider` devient `ICodeLanguageService`.

### Supprimé
- `CSharpWorkspace`, `CSharpCompletionService`, `CSharpDiagnosticService`, `CSharpHoverService`, `CSharpSignatureService`, `CSharpSource`, `CSharpLanguageOptions.AllowImports` ; `CSharpProject`, `CSharpDocument` et `CSharpDiagnosticMapper` deviennent internes.
- `ICodeCompilation` (vide) et `CodeLanguageServiceProvider` (registre statique inutilisé).

### Ajouté
- `Quill.Code.Sql` : exécution SQL sur une chaîne de connexion (`ISqlQueryExecutor`, `SqlServerQueryExecutor` : scripts découpés sur GO comme SSMS, requêtes paramétrées) ; `Quill.Code.Sql.Execution` : les résultats (`QueryExecutionResult`, `QueryResultSet`, `QueryResultColumn`, `QueryResultRow`, `QueryMessage`, `QueryMessageKind`, `QueryParameter`, `QueryParameterType`, `QueryExecutionLimits`), repris de Querify. `Quill.Code.Sql` dépend désormais de Microsoft.Data.SqlClient.
- `Quill.Data.Sql` : structure d'une base (`SqlDatabaseInfo`, `SqlDatabaseSchema`, `SqlSchemaObject`, `SqlColumn`, `SqlSchemaObjectKind`), reprise de Querify.Databases, et contrat `ISqlSchemaReader` ; `Quill.Data.Sql.SqlServer` : `SqlServerSchemaReader`. `Quill.Code.Sql` utilise ce modèle (ses propres types de schéma sont supprimés) ; `SqlColumn` se construit désormais par propriétés (`new SqlColumn(nom, type) { IsNullable = false, PrimaryKeyOrdinal = 1 }`).
- `Quill.Code.Sql` : autocomplétion et survol sur un catalogue fourni par l'hôte (`ISqlCatalog`, modèle `SqlDatabaseInfo` / `SqlDatabaseSchema` / `SqlSchemaObject` / `SqlColumn`, `SqlCatalog` en mémoire), mots-clés T-SQL intégrés, variables de l'hôte (`SqlVariable`) ; `SqlLanguageService(SqlLanguageOptions)` remplace le constructeur à services substituables, les services par fonctionnalité deviennent internes.
- `CompletionFilter.Filter` : filtre et tri des propositions sur le mot en cours (commence par, début de segment, contient), repris de Querify.
- Structure de la solution : `Quill.Code`, `Quill.Code.CSharp`, `Quill.Code.Sql`, `Quill.Code.Tests`.
- `Quill.Code.CSharp` : `CSharpLanguageService` relaie à `CSharpCompletionService`, `CSharpDiagnosticService`, `CSharpHoverService` et `CSharpSignatureService` (`ICodeCompletion`, `ICodeDiagnostic`, `ICodeHover`, `ICodeSignature`), utilisables seuls sur un `CSharpProject` (exposés par `CSharpLanguageService.Completion`, `.Diagnostics`, `.Hover`, `.Signature`, avec les surcharges sur un `CSharpDocument`).
- Suppression de `ICSharpLanguageService` (les apps utilisent `CSharpLanguageService`, qui implémente `ICodeLanguageProvider`) et de `CSharpLanguageServiceExtensions` (remplacées par les services de fonctionnalité).
- `Quill.Code.Sql` : `SqlLexer`, `SqlContextAnalyzer` (`Analyze`, `AnalyzeScope` : nature de la saisie attendue, qualificatif, tables et alias du lot GO courant, base du dernier `USE`), `SqlSyntaxChecker` (diagnostics ScriptDom, lots GO compris) et `SqlLanguageService` (`ICodeLanguageProvider`) qui relaie à `SqlCompletionService`, `SqlDiagnosticService`, `SqlHoverService` et `SqlSignatureService` (diagnostics réels ; les autres ne proposent rien) ; l'hôte peut substituer chaque service par le sien.
- Versions de paquets centralisées, métadonnées NuGet, Source Link.
- CI : build et tests, publication sur GitHub Packages à chaque tag `v*`.
- Modèles communs : autocomplétion (`CompletionItem`, `CompletionItemKind`, `CompletionList`, `CompletionChange`, `CompletionTrigger`), analyse (`CodeDiagnostic`, `CodeDiagnosticSeverity`, `HoverInfo`, `SignatureHelpInfo`, `SignatureInfo`) et `LinePosition`.
- `Quill.Code.CSharp` : `CSharpWorkspace`, `CSharpProject`, `CSharpDocument`, `CSharpReferences` ; `ICSharpLanguageService` (autocomplétion avec calcul de la modification, diagnostics, survol, aide à la signature) ; `AddQuillCSharp()`.
- Coloration syntaxique (`Quill.Code.Highlighting`) : `XshdReader` (format .xshd d'AvalonEdit), `SyntaxDefinition`, `SyntaxColor`, `SyntaxKeyword`, `SyntaxHighlighter` ligne par ligne (`SyntaxLineState`) ou texte entier, `SyntaxToken`.
- Édition (`Quill.Code.Editing`) : `CodeEditing` (Entrée, lignes vides, paires et caractères tapés, retour arrière, tabulation et indentation des lignes, commentaires de ligne, duplication, suppression et déplacement de lignes, touche Début), `SourceScanner` / `SourceSyntax` (code, commentaires, chaînes ; C# et T-SQL), `BracketMatcher`, `TextLines`, `TextSelection`, `TextEdit`, `EditResult`. Sauts de ligne « \n » et « \r\n ».
- Compilation (`Quill.Code.CSharp.Compilation`) : `CompileAsync` émet le projet du service (PDB facultatif), rend les erreurs aux positions de l'éditeur et charge l'assembly dans un contexte déchargeable (`CSharpCompilationResult`, `CSharpCompileOptions` : fichiers d'assemblies, résolution personnalisée). `CodeDiagnostic.Describe` (« Ligne x, colonne y : … »). Sources en UTF-8 pour les informations de débogage.
- `SourceTransform` rend un `CSharpSource` (source et position du texte) : texte neutralisé ou enveloppé de code généré ; `CSharpProjectOptions.IncludeSemanticDiagnostics` et `AllowImports`.
- Suppression de `AddQuillCSharp` : les apps instancient les services ; `Quill.Code` n'a plus aucune dépendance.
- Contrats sur le texte brut `ICodeCompletionProvider` (Completion) et `ICodeDiagnosticProvider` (Analysis), implémentés par `ICSharpLanguageService`.
- `CSharpLanguageService(string projectName, Func<CSharpProjectOptions>?)` : service sur son propre projet, méthodes sur le code (`GetCompletionsAsync(code, position)`…) ; `CSharpProjectOptions` devient un record au nom facultatif.
- `CSharpWorkspace.AddProject` ne crée le projet qu'à sa première utilisation (surcharge avec options différées) ; `CSharpProjectOptions.SourceTransform` et `TextReferences` ; extensions de `ICSharpLanguageService` sur le texte brut (`CSharpLanguageServiceExtensions`).
- Sources ajoutées à la volée (`CSharpProject.SetSource`, `SetSourceFileAsync`, `RemoveSource`, `ClearSources`, `Sources`, `SourcesChanged`) : vues par l'éditeur et compilées avec lui ; leurs erreurs de compilation citent le fichier.
- `CSharpCompileOptions.Collectible` (assembly chargée pour toute la vie de l'application) et `CSharpCompilationResult.Reference` (compiler un autre projet contre l'assembly émise).
- Exécution (`Quill.Code.CSharp.Compilation`) : `CSharpCompilationResult.RunAsync` (hors du thread appelant, exception du code relevée telle quelle ; à l'annulation, la main est rendue tout de suite, `CSharpRunAbandonedException`, et `Dispose` attend la fin réelle pour décharger) et `RunEntryPointAsync` (code de sortie).
- Runtime des scripts : `CSharpProjectOptions.IncludeScriptRuntime` génère `Log`, `LogLevel` et `Script` dans l'assembly, branchés par réflexion (`CSharpCompileOptions.Log`, `Script.CancellationToken` fixé par `RunAsync`) : propres à chaque compilation, sans référence vers l'application. `Quill.Code.Scripting.LogLevel`, calqué sur `Microsoft.Extensions.Logging`.
- `CSharpDiagnosticMapper.MapAsync` : diagnostics Roslyn calculés hors de Quill (erreurs d'émission) ramenés au texte de l'éditeur.
