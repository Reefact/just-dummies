# ADR-0045 | Renuméroter la base de décisions en une séquence contiguë

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0045-renumber-the-decision-base.md)

**Statut :** Accepté
**Proposé :** 2026-07-31
**Accepté :** 2026-07-31
**Décideurs :** Reefact

## Contexte

Les 34 décisions héritées de `Reefact/first-class-errors`
([ADR-0044](0044-extract-justdummies-into-its-own-repository.fr.md)) sont arrivées avec les numéros sous
lesquels elles y avaient été acceptées — 0011, 0013, 0015, 0020, 0022, 0025, 0030–0033, … — une séquence
trouée, parce que les numéros intercalaires appartiennent à des décisions FirstClassErrors restées là-bas.

L'ADR-0044 avait conservé ces numéros, au motif qu'une renumérotation casserait les références croisées
internes aux textes acceptés. Ce raisonnement confondait deux choses. Un numéro est un **identifiant**, pas
une part de la décision : réécrire `ADR-0045` en `ADR-0024` dans une citation la laisse désigner la même
décision, et ne change ni contexte, ni décision, ni rationale, ni alternatives, ni conséquences, ni statut,
ni dates, ni attribution. Ce que l'[ADR-0024 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md)
refuse d'ériger en précédent, c'est de modifier des **décisions** acceptées sur place. Ici, il s'agit
d'indexation.

Un second manque est apparu au même moment, et c'est lui qui rend la renumérotation autre que cosmétique.
Neuf décisions gouvernent ce dépôt — le plancher Roslyn de son analyseur, sa règle de scope de commit, sa
porte de mutation, ses règles de codage, son jeu de règles Sonar, son processus d'ADR — et aucune n'était
ici. Le build les appliquait pendant que leur enregistrement ne vivait que dans
`Reefact/first-class-errors`, ce qui signifiait qu'une supersession là-bas changeait silencieusement les
règles ici.

## Décision

Les neuf décisions que ce dépôt applique sont **adoptées** dans cette base, et l'ensemble est renuméroté en
une séquence contiguë **0001–0045**, ordonnée par le numéro que chaque décision portait dans
`Reefact/first-class-errors` — ainsi aucune décision existante ne bouge par rapport à une autre, et les
adoptions se placent à leur rang historique.

Une adoption n'est pas une copie. Les deux enregistrements sont désormais vivants, et chaque dépôt peut
superséder le sien sans toucher à l'autre — ce qui est le comportement correct pour deux produits qui ne
partagent plus de build.

Trois provenances sont distinguées, dans l'en-tête de chaque ADR et dans la colonne **Origin** de l'index :

| Note d'en-tête | Signification |
| --- | --- |
| *Enregistré à l'origine dans `Reefact/first-class-errors` sous le numéro ADR-NNNN* | la décision a **déménagé** ici avec son code |
| *Adopté depuis `Reefact/first-class-errors`, ADR-NNNN* | la décision est **vivante des deux côtés** |
| *(aucune)* | décidée ici |

Une citation d'un ADR n'existant que dans l'autre dépôt s'écrit **`ADR-00NN (first-class-errors)`**. Un
numéro non qualifié désigne toujours cette base.

## Conséquences

### L'historique git garde les anciens numéros, définitivement

420 messages de commit citent des numéros d'ADR — `docs: draft ADR-0010 hosting Dummies as a standalone
package` désigne l'ADR-0003 de ce dépôt. Ils n'ont pas été réécrits : `main` est publiée, et les réécrire
imposerait une seconde passe `filter-repo` sur un historique que d'autres possèdent peut-être déjà.
`git log --grep ADR-0045` trouve donc des commits portant sur ce qui est aujourd'hui l'ADR-0024, et rien ne
changera jamais cela. Les notes d'en-tête et la table ci-dessous sont le seul décodeur — d'où le fait que
supprimer l'un ou l'autre casse quelque chose d'irréparable.

### Qualifier les citations étrangères n'était pas optionnel

Avant la renumérotation, les numéros de ce dépôt commençaient à 0011 et ne croisaient jamais les numéros
FirstClassErrors cités par ses textes. Après, plusieurs d'entre eux tombent dans 0001–0045. Chaque citation
d'une décision restée là-bas a été qualifiée dans le même changement ; les laisser nues aurait fait désigner
à `ADR-0006` deux décisions différentes dans la même phrase, en silence.

### La renumérotation dépasse largement les textes d'ADR

Les sources des analyseurs (`Descriptors.cs`, `CollectionConstraintsAdmitNoValueAnalyzer.cs`), les suites de
tests, `Directory.Build.props`, les fichiers projet et les workflows citent tous des numéros d'ADR. Ce sont
ces citations hors Markdown qui auraient pourri en silence, puisque rien ne compile un commentaire.

### Deux décisions n'ont délibérément pas été adoptées

[`ADR-0002 (first-class-errors)`](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0002-floor-the-tooling-runtime.fr.md) plafonne le runtime de l'outillage au plus ancien LTS supporté. Son sujet est
l'outil `fce` et son worker de documentation ; ce dépôt n'a pas encore d'outil, et l'adopter reviendrait à
décider d'un plancher pour un binaire qui n'existe pas. Sa place est ici le jour où le scaffolder `dum` sera
construit.

[`ADR-0024 (first-class-errors)`](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.fr.md) autorise une migration éditoriale bornée des ADR acceptés. C'est une
autorisation historique et ponctuelle, accordée à ce dépôt-là pour une migration que celui-ci n'a jamais
faite ; adopter la permission d'un acte non commis consignerait une décision jamais prise.

## Alternatives considérées

### Conserver les trous et les expliquer dans l'index

Considérée, et même faite d'abord : l'index avait gagné un paragraphe expliquant pourquoi la séquence
démarrait à 0011. Rejetée parce que l'explication doit être relue chaque fois qu'un numéro est rencontré
ailleurs — dans un commentaire de code, dans un message de commit, dans une référence croisée — et que
l'index n'y est pas.

### Ajouter les décisions adoptées à la fin plutôt que les insérer

Considérée, au motif qu'une décision n'atteint l'état *Accepté* « dans ce dépôt » que le jour de son
adoption, et que sa ligne datée — donc sa position — devrait porter la date du jour. Rejetée parce que le
même argument vaudrait pour les 34 décisions déménagées, qui ont gardé leurs dates d'origine : ce dépôt n'a
rien décidé en juillet non plus, il n'existait pas. Traiter les deux groupes différemment aurait fait dire à
la séquence une chose pour 34 entrées et une autre pour 9.

### Renuméroter sans consigner les anciens numéros

Rejetée d'emblée. L'historique git ne peut pas suivre : abandonner la table laisserait 420 messages de commit
face à une base dont la numérotation ne correspond plus, sans rien pour la reconstituer.

## Correspondance

| Avant (FCE) | Ici | Origine | Décision |
|---|---|---|---|
| ADR-0001 | [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) | adoptée | Lock the analyzer's Roslyn floor |
| ADR-0004 | [ADR-0002](0002-check-every-pull-request-against-the-adr-base.fr.md) | adoptée | Check every pull request against the ADR base |
| ADR-0011 | [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) | déplacée | Host JustDummies as a standalone package in this repository |
| ADR-0013 | [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) | déplacée | Gate distinct collections by cardinality, otherwise by a bounded draw |
| ADR-0015 | [ADR-0005](0005-cap-any-combine-at-arity-eight.fr.md) | déplacée | Cap Any.Combine at arity eight |
| ADR-0020 | [ADR-0006](0006-materialize-dummies-only-through-generate.fr.md) | déplacée | Materialize dummies only through Generate() |
| ADR-0022 | [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.fr.md) | déplacée | Floor the library's .NET Framework support at 4.7.2 |
| ADR-0025 | [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md) | déplacée | Generate matching strings from a home-grown regular subset |
| ADR-0030 | [ADR-0009](0009-draw-arbitrary-strings-from-an-explicit-terminal-set.fr.md) | déplacée | Draw arbitrary strings from an explicit, terminal value set |
| ADR-0031 | [ADR-0010](0010-name-any-factories-after-their-clr-type.fr.md) | déplacée | Name Any's scalar factories after their CLR type |
| ADR-0032 | [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.fr.md) | déplacée | Draw arbitrary values from an explicit, top-level choice pool |
| ADR-0033 | [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.fr.md) | déplacée | Meet string exclusions with a bounded redraw |
| ADR-0034 | [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.fr.md) | adoptée | Require a scope on the version-driving commit types |
| ADR-0035 | [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.fr.md) | déplacée | Enforce structural Any conflicts at compile time, value-dependent ones at run time |
| ADR-0036 | [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.fr.md) | déplacée | Draw lattice-constrained scalars on the grid |
| ADR-0037 | [ADR-0016](0016-vary-the-datetimeoffset-offset-dimension.fr.md) | déplacée | Vary the DateTimeOffset offset dimension |
| ADR-0038 | [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.fr.md) | déplacée | Open the ambient seed scope to test-framework adapters |
| ADR-0039 | [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md) | déplacée | Adapt JustDummies to xUnit v3 through a companion package |
| ADR-0040 | [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) | déplacée | Split the JustDummies test bed between an example suite and a property suite |
| ADR-0041 | [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md) | déplacée | Draw flag-enum combinations behind an opt-in |
| ADR-0042 | [ADR-0021](0021-serialize-draws-on-a-random-source.fr.md) | déplacée | Serialize draws on a random source, and scope reproducibility to the draw sequence |
| ADR-0043 | [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md) | adoptée | Gate pull requests on the mutation score of what they changed |
| ADR-0044 | [ADR-0023](0023-ship-justdummies-analyzers.fr.md) | déplacée | Ship first-party JustDummies analyzers, and guard the reproducible async surface with them |
| ADR-0045 | [ADR-0024](0024-guard-public-and-internal-arguments-against-null.fr.md) | déplacée | Guard public and internal arguments against null, enforced by a reflection convention |
| ADR-0046 | [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) | adoptée | Make the per-pull-request mutation gate advisory |
| ADR-0047 | [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md) | déplacée | Measure JustDummies mutation against the deterministic unit suite only |
| ADR-0048 | [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md) | déplacée | Guarantee a generated regex value matches its pattern, by bounded redraw |
| ADR-0049 | [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md) | déplacée | Drop the JustDummies generator from the per-pull-request mutation matrix |
| ADR-0050 | [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) | déplacée | Let a size maximum cap without steering the draw, and ceiling an explicitly demanded size |
| ADR-0051 | [ADR-0030](0030-filter-the-datetimeoffset-pool-by-the-declared-offset.fr.md) | déplacée | Filter the DateTimeOffset pool by the declared offset |
| ADR-0052 | [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) | déplacée | Draw arbitrary numbers within an ordinary magnitude |
| ADR-0053 | [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.fr.md) | déplacée | Unify discrete generation in one ordinal space, with a dedicated engine only where the arithmetic substrate forces one |
| ADR-0054 | [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) | déplacée | Decide a generator's constraint surface by constructive versus rejective, not by terminality |
| ADR-0055 | [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md) | adoptée | Enforce the style rules the compiler can express, and keep the DotSettings authoritative for the rest |
| ADR-0056 | [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) | adoptée | State the coding rules where an agent can act on them, and check them at the edit |
| ADR-0057 | [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md) | adoptée | Keep one dated line per state an ADR reached, and never overwrite one |
| ADR-0058 | [ADR-0037](0037-suppress-ca1510-while-the-netstandard-floor-stands.fr.md) | déplacée | Suppress CA1510 while the pre-.NET-6 floor stands |
| ADR-0059 | [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) | déplacée | Guard the recipe-versus-value boundary with analyzers where the type system cannot reach it |
| ADR-0062 | [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.fr.md) | adoptée | Derive the build's Sonar rule set from the quality profile |
| ADR-0063 | [ADR-0040](0040-throw-the-library-s-own-exceptions-through-named-factories.fr.md) | déplacée | Throw the library's own exceptions through named factories |
| ADR-0064 | [ADR-0041](0041-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.fr.md) | déplacée | Exempt the whole failure-reporting path from the null-guard convention |
| ADR-0065 | [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.fr.md) | déplacée | Carry a declared constraint as a value object, not as its rendered text |
| ADR-0066 | [ADR-0043](0043-declare-a-value-object-and-enforce-its-identity.fr.md) | déplacée | Declare a value object with an attribute, and enforce its identity by convention |
| — | [ADR-0044](0044-extract-justdummies-into-its-own-repository.fr.md) | créée ici | Extract JustDummies into its own repository |
| — | [ADR-0045](0045-renumber-the-decision-base.fr.md) | créée ici | Renumber the decision base into a contiguous sequence |
