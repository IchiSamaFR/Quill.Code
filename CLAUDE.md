# Quill.Code - Instructions pour Claude

Bibliothèque .NET des briques communes aux éditeurs de code (coloration, autocomplétion, diagnostics, édition), sans dépendance à un framework d'interface. Consommée par Querify (MAUI) et PocketForge (WPF).

**Les instructions complètes se trouvent dans `.github/instructions.md`** : à lire avant toute modification.

Points clés :
1. Aucune référence à WPF, MAUI ou à un type d'interface : la bibliothèque produit des données, les apps les affichent.
2. `Quill.Code` ← `Quill.Code.[Langage]` ; un langage ne référence jamais un autre langage. `Quill.Data.Sql` (structure d'une base, sans dépendance) ← `Quill.Data.Sql.[Moteur]` (lecture du catalogue) et ← `Quill.Code.Sql` (et bientôt `Quill.Code.Linq`) ; un `Quill.Code.*` ne référence jamais un `Quill.Data.Sql.[Moteur]`. Exception : `Quill.Code.Sql` dépend de Microsoft.Data.SqlClient pour l'exécution (`SqlServerQueryExecutor`), l'analyse et l'autocomplétion restent sans connexion.
3. Pas d'injection de dépendances dans Quill (ni conteneur, ni registre statique) : les apps instancient les services (`new CSharpLanguageService(() => options)`) ou les enregistrent dans leur propre conteneur.
4. Un type par fichier, `internal` / `sealed` par défaut, documentation XML en français sur l'API publique.
5. Aucune constante déclarée dans une classe de travail : elles vont dans le dossier `Constants/` du projet (namespace `<Projet>.Constants`), une classe statique par contexte (`SqlServerConstants`, `EditingConstants`, `SqlServerQueries`…), `internal` sauf si elle fait partie de l'API.
6. Versions de paquets uniquement dans `src/Directory.Packages.props`.

Compiler : `dotnet build src/Quill.Code.slnx` — Tester : `dotnet test src/Quill.Code.slnx`
