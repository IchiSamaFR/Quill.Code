# Quill.Code - Instructions pour Claude

Bibliothèque .NET des briques communes aux éditeurs de code (coloration, autocomplétion, diagnostics, édition), sans dépendance à un framework d'interface. Consommée par Querify (MAUI) et PocketForge (WPF).

**Les instructions complètes se trouvent dans `.github/instructions.md`** : à lire avant toute modification.

Points clés :
1. Aucune référence à WPF, MAUI ou à un type d'interface : la bibliothèque produit des données, les apps les affichent.
2. `Quill.Code` ← `Quill.Code.[Langage]` ; un langage ne référence jamais un autre langage.
3. Pas d'injection de dépendances : les apps instancient les services (`new CSharpLanguageService(nom, options)`).
4. Un type par fichier, `internal` / `sealed` par défaut, documentation XML en français sur l'API publique.
5. Versions de paquets uniquement dans `src/Directory.Packages.props`.

Compiler : `dotnet build src/Quill.Code.slnx` — Tester : `dotnet test src/Quill.Code.slnx`
