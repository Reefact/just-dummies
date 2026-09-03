# ADR-0097 | Renommer la façade Any en Dummy

🌍 🇬🇧 [English](0097-rename-the-any-facade-to-dummy.md) · 🇫🇷 Français (ce fichier)

**Statut :** Proposé
**Proposé :** 2026-09-03
**Décideurs :** Reefact

## Contexte

* Depuis le premier commit du projet, le point d'entrée de `JustDummies` est une classe
  statique nommée `Any`, et toute la surface de générateurs a suivi ce mot : la famille de
  builders `Any{Type}` (`AnyString`, `AnyGuid`, …), le miroir de contexte germé
  `AnyContext`, l'interface `IAny<T>` que tout générateur implémente, et
  `ConflictingAnyConstraintException`.
* Le package, le dépôt et la propre documentation de la bibliothèque se nomment
  `JustDummies` et décrivent partout chaque valeur tirée par la bibliothèque comme un
  « dummy » — la définition dans `CLAUDE.md`, les guides utilisateur, le README. La façade
  qu'un consommateur écrit réellement, `Any.Int32()`, nommait le concept identique par un
  mot différent depuis le début.
* `JustDummies.GenAny`, le moteur derrière le scaffolder `dum`, et l'outil
  `JustDummies.Cli` construit dessus portent le même mot plus loin : un générateur
  scaffoldé est émis sous le nom `Any{Type}` (ex. `AnyOrder`), et
  `dum generate --entry-point any` demande un point d'entrée atteignant la façade de la
  bibliothèque elle-même.
* `JustDummies` publie des versions preview sur nuget.org depuis le 31/07/2026, la plus
  récente étant `1.0.0-preview.6` le 02/09/2026. `JustDummies.Cli` publie des versions beta
  depuis `cli-v1.0.0-beta.1`, la plus récente étant `1.1.0-beta.6` le 03/09/2026 ; selon
  `.claude/rules/cli-and-scaffolder.md`, ce qu'une version `cli` engage, c'est la ligne de
  commande elle-même, et chaque option gagnée depuis `1.0.0-beta.1` — `--entry-point`
  compris — a jusqu'ici été additive, jamais le renommage d'une option existante.
* Le mainteneur a dirigé ce renommage directement, en deux temps : d'abord la façade et
  toute sa famille de générateurs, à l'échelle de la bibliothèque, moteur
  `JustDummies.GenAny` compris ; puis, en revoyant la conséquence que la valeur
  `--entry-point any` de `JustDummies.Cli` était restée inchangée, a confirmé qu'elle devait
  suivre le même renommage.
* Certaines parties de ce dépôt sont des enregistrements d'un état passé plutôt que des
  descriptions de l'état courant. Une ADR acceptée est un enregistrement historique
  immuable — le README de la base le dit, et l'ADR-0036 conserve une ligne datée par état
  qu'une décision a atteint précisément pour qu'aucun état ne soit jamais écrasé. Une
  section de changelog sous une version publiée, et la release note qui en est tirée,
  décrivent un artefact déjà publié sur nuget.org. Les audits datés et l'enregistrement
  d'extraction sous `audit/` et `migration/` rapportent ce qui a été constaté à leur propre
  date ; `.claude/rules/documentation.md` les regroupe comme « dated records of a past
  state, not current rules ».

## Décision

Chaque occurrence de `Any` nommant ce concept dans la surface vivante et sa documentation
vivante — la façade et sa famille de générateurs, l'interface `IAny<T>`,
`ConflictingAnyConstraintException`, le moteur `JustDummies.GenAny` et la valeur
`--entry-point any` du CLI `dum` — est renommée en `Dummy` sans alias de compatibilité,
tandis que chaque enregistrement d'un état passé — une ADR acceptée, une section de
changelog publiée et sa release note, un audit daté — garde les noms avec lesquels il a été
écrit.

## Justification

* Le package, le dépôt et la documentation appellent déjà chaque valeur tirée un « dummy » ;
  une façade nommée `Any` était un second mot pour le concept identique, que chaque nouveau
  lecteur devait apprendre n'être pas une distinction. Nommer la façade `Dummy` referme cet
  écart plutôt que de demander à chaque lecteur de le franchir.
* Le scaffolder émet du code qui appelle la propre surface de la bibliothèque, et l'option
  `--entry-point` du CLI existe pour nommer ce que cet appel émis atteint. Renommer la
  façade sans le scaffolder ni l'option laisserait `--entry-point any` atteindre un appel
  désormais orthographié `Dummy.Order()` — le mot de l'option et la surface qu'il nomme se
  contrediraient alors, exactement l'écart que ce renommage existe pour supprimer,
  réintroduit à la frontière de l'outil plutôt qu'à celle de la bibliothèque.
* Pas d'alias de compatibilité, parce qu'un second nom pour la façade reproduit
  l'ambiguïté même que cette décision supprime — un consommateur lisant `Any.Int32()` à
  côté de `Dummy.Int32()` dans la même base de code aurait de nouveau deux mots à
  concilier, indéfiniment, plutôt qu'une fois à la mise à niveau.
* Un enregistrement d'un état passé est exempté pour la raison inverse de celle qui motive
  le renommage partout ailleurs. Ailleurs, l'ancien nom décrit quelque chose qui n'existe
  plus, donc le garder induit en erreur ; dans un enregistrement, l'ancien nom est le *fait
  rapporté* — le package `1.0.0-preview.6` réellement publié, la surface sur laquelle
  l'ADR-0010 a réellement décidé — et le remplacer rend l'enregistrement faux. Cela le rend
  aussi inutile pour ce à quoi sert un enregistrement : un lecteur ne peut plus rapprocher
  une release note du package sur nuget.org qu'elle décrit, et une décision acceptée se
  lirait comme si elle avait été prise à propos d'un nom qui n'existait pas à sa date. À
  moitié appliqué, c'est pire encore : une ligne de changelog était devenue
  « `Any.SetOf(...)` is typed `IDummy<HashSet<T>>` », nommant les deux surfaces dans une
  seule phrase, et la décision de l'ADR-0010 « renaming `Dummy.Bool()` … / `AnyBool` »
  décrivait un renommage s'éloignant d'un nom qu'elle venait de dire déjà en place.
* Payer le coût de migration maintenant plutôt que plus tard est un arbitrage délibéré :
  `JustDummies` et `JustDummies.Cli` ont déjà de vrais consommateurs, certes précoces, en
  preview et en beta, donc le nombre de consommateurs devant migrer ne fait que croître
  plus le renommage attend.

## Alternatives envisagées

### Garder `Any`, et introduire `Dummy` comme alias

Envisagée parce qu'elle laisse un consommateur déjà publié continuer de compiler sans
changement.

Rejetée parce que deux noms pour une seule façade doublent la surface découvrable et ne
referment que partiellement l'écart de nommage que cette décision existe pour clore — une
base de code mêlant `Any.Int32()` et `Dummy.Int32()` a toujours deux mots à concilier,
cette fois à l'intérieur même de la surface publique de la bibliothèque plutôt qu'entre la
bibliothèque et le nom de son package seulement.

### Renommer seulement la façade, et laisser `JustDummies.GenAny` et `--entry-point any` inchangés

Envisagée parce que le scaffolder et le CLI s'expédient comme un package séparé, sans
baseline d'API publique partagée, donc rien ne force l'un ou l'autre à suivre les noms de
la bibliothèque.

Rejetée parce que le scaffolder a pour seule raison d'être d'émettre des appels contre la
surface de la bibliothèque : laisser `--entry-point any` en l'état lui ferait atteindre un
appel déjà orthographié `Dummy.Order()`, si bien que le mot de l'option cesserait de nommer
quoi que ce soit dans la surface qu'il atteint.

### Différer le renommage à la première version stable, quand rien ne dépend encore des noms actuels

Envisagée parce que c'est normalement le moment le moins coûteux pour renommer une surface
publique, comme le Contexte note que l'ADR-0010 l'a fait pour l'unique fabrique
pré-1.0 qu'elle a renommée.

Rejetée parce que ce moment est déjà passé : les deux packages ont publié des versions
preview et beta avec de vrais consommateurs, certes précoces. Attendre une version stable
ne supprime pas le coût de migration, il ne fait que croître l'ensemble des consommateurs
qui le paient.

## Conséquences

### Positives

* Un seul nom, `Dummy`, nomme désormais le concept partout où un consommateur le
  rencontre : le package, le dépôt, la documentation, la façade, le code émis par le
  scaffolder et l'option du CLI elle-même.
* L'appel émis par le scaffolder et l'option du CLI qui le demande s'accordent de
  nouveau : `--entry-point dummy` atteint `Dummy.Order()`.
* La base de décisions, les sections de changelog publiées et les audits datés se lisent
  encore tels qu'ils ont été écrits, si bien que chacun reste rapprochable de l'artefact ou
  de la décision qu'il rapporte.

### Négatives

* Chaque consommateur d'une preview ou d'une beta déjà publiée de `JustDummies` ou de
  `JustDummies.Cli` doit migrer à la main : `Any` en `Dummy`, `IAny<T>` en `IDummy<T>`,
  `Any{Type}` en `Dummy{Type}`, `--entry-point any` en `--entry-point dummy`.
* Le renommage touche d'un coup la surface publique des deux packages : chaque type
  générateur, les noms émis par le scaffolder, la ligne de commande du CLI elle-même, les
  baselines PublicAPI validées et la documentation anglais/français appariée — un coût
  mécanique large, ponctuel.

### Risques

* Un consommateur qui met à niveau sans lire le changelog rencontre une rupture de
  compilation sans transition douce, puisqu'aucun alias n'a été gardé. Atténué par les
  entrées de changelog que ce renommage ajoute aux deux trains, et par SemVer : une version
  preview ou beta ne promet aucune compatibilité entre versions.
* Les deux noms cohabitent désormais dans le dépôt, et un lecteur qui croise `Any` dans une
  ADR ou une vieille section de changelog peut le prendre pour un oubli. C'est la frontière
  qui les distingue, elle doit donc tenir à chaque changement ultérieur : un enregistrement
  d'un état passé n'est jamais balayé.

## Actions de suivi

* Aucune — le renommage est complet : la façade et sa famille de générateurs,
  `JustDummies.GenAny` en `JustDummies.GenDummy`, et `--entry-point any` en
  `--entry-point dummy` *(fait dans les changements que cet enregistrement documente)*.

## Références

* [ADR-0010](0010-name-any-factories-after-their-clr-type.fr.md) — la décision de nommage
  des fabriques par type CLR sur la même surface scalaire ; non affectée par ce renommage,
  qui renomme le préfixe de la surface, non ses noms de fabriques. Son texte est aussi le
  cas le plus net de la frontière que cet enregistrement trace : il décide *à propos du*
  nom `Any`, si bien que le réécrire aurait détruit la décision qu'il enregistre.
* [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md) — une ligne datée par
  état qu'une décision a atteint, jamais écrasée ; le même principe appliqué au corps d'un
  enregistrement plutôt qu'à son en-tête.
* [`doc/handwritten/for-maintainers/specifications/justdummies-tool.fr.md`](../specifications/justdummies-tool.fr.md),
  §4.5 — la mécanique du point d'entrée que touche le volet CLI de cette décision.
* `CONTRIBUTING.md`, « Public API baseline » — le mécanisme qui a fait de ce renommage un
  diff revu sur la surface validée des deux packages.
