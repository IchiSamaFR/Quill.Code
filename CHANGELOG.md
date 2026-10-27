# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versions [SemVer](https://semver.org/lang/fr/).

## [Non publié]

### Ajouté
- Structure de la solution : `Quill.Code`, `Quill.Code.CSharp`, `Quill.Code.Tests`.
- Versions de paquets centralisées, métadonnées NuGet, Source Link.
- CI : build et tests, publication sur GitHub Packages à chaque tag `v*`.
- Modèles communs : autocomplétion (`CompletionItem`, `CompletionItemKind`, `CompletionList`, `CompletionChange`, `CompletionTrigger`), analyse (`CodeDiagnostic`, `CodeDiagnosticSeverity`, `HoverInfo`, `SignatureHelpInfo`, `SignatureInfo`) et `LinePosition`.
- `Quill.Code.CSharp` : `CSharpWorkspace`, `CSharpProject`, `CSharpDocument`, `CSharpReferences` ; `ICSharpLanguageService` (autocomplétion avec calcul de la modification, diagnostics, survol, aide à la signature) ; `AddQuillCSharp()`.
