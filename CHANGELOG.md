# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versions [SemVer](https://semver.org/lang/fr/).

## [Non publié]

### Ajouté
- Structure de la solution : `Quill.Code`, `Quill.Code.CSharp`, `Quill.Code.Tests`.
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
- `CSharpDiagnosticMapper.MapAsync` : diagnostics Roslyn calculés hors de Quill (erreurs d'émission) ramenés au texte de l'éditeur.
