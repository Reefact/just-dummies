# ADR-0132 | Laisser un exemple de déclarations importer ce qu'il utilise

🌍 🇬🇧 [English](0132-let-a-declarations-sample-import-what-it-uses.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-04
**Accepted:** 2026-11-04
**Decision Makers:** Reefact

## Contexte

[ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md) tient chaque exemple C# de la
documentation utilisateur à un contrat de compilation que le build vérifie. Ses conséquences consignent le contrat
selon lequel un exemple est écrit : les modes déclarés, le domaine d'illustration partagé, et aucune directive
d'import dans un exemple. Le harnais tenait ce dernier terme en compilant chaque exemple sous les mêmes imports,
dont `JustDummies` et xUnit, et dans un espace de noms placé sous `JustDummies`.

Un exemple existe sous deux modes. Un exemple d'instructions est un fragment de corps de méthode, où C# ne laisse
aucune place pour un import. Un exemple de déclarations est fait de types entiers, ce qu'un lecteur copie dans un
fichier à lui.

Le troisième essai d'expérience développeur a rencontré la conséquence des imports injectés dans cinq domaines
(DOCS-11) : un lecteur qui copiait un exemple de déclarations dans un fichier de test obtenait `CS0103` sur `Any`, ou
un `[Fact]` introuvable, parce que la page compilait avec des imports que son fichier n'avait pas. Le harnais cachait
le manque au build : sous un espace de noms intérieur à `JustDummies`, les types de la bibliothèque se résolvaient par
l'espace de noms englobant, même sans import. Un essai UX antérieur avait déjà recommandé de retirer l'injection
([XU-D1](../../../../tools/ux-trial/REPORT.fr.md#xu-d1)) ; cette recommandation n'avait été appliquée qu'à la page de
l'adaptateur xUnit.

Un projet de test sur le SDK reçoit un ensemble d'imports implicites, dont `System`, sans en écrire une ligne : les
modèles de projet du SDK les activent par défaut. Ils restent toutefois un réglage du projet, et un projet qui les
désactive doit écrire ces imports dans chaque fichier.

## Decision

Un exemple de déclarations énonce les imports qu'il utilise au-delà des imports implicites du SDK, et le harnais le
compile avec seulement ce qu'un projet de test aux imports implicites activés fournit sans qu'on l'écrive — ces imports
implicites et le domaine d'illustration partagé — dans un espace de noms hors de `JustDummies` ; un exemple
d'instructions garde les imports partagés.

## Rationale

**Un exemple qu'un lecteur copie doit compiler là où il est copié.** ADR-0055 existe pour que la documentation ne
puisse pas dériver de ce qu'un lecteur peut exécuter. Un import que le harnais fournit et que le fichier du lecteur
n'a pas est exactement une telle dérive, et le build déclarait correct chacun de ces exemples.

**Le terme du contrat était la cause, pas un choix de style.** « Aucune directive d'import dans un exemple » était
tenu en fournissant les imports : il ne pouvait donc tenir qu'en cachant la seule chose que le lecteur doit écrire en
premier. Le renverser pour les exemples de déclarations fait montrer cette ligne par la page au lieu de la supposer.

**Les exemples d'instructions gardent l'ancien terme parce que rien d'autre n'est possible.** Un corps de méthode n'a
aucune place pour un import ; là, les imports partagés sont le seul moyen de compiler, et le lecteur copie un fragment
dans une méthode qui a déjà les imports de son fichier.

**Le projet de test par défaut est la base honnête, et elle est nommée comme telle.** Les imports implicites du SDK
sont dans le projet d'un lecteur sans une ligne de sa part tant que le projet garde le défaut des modèles, et le domaine
d'illustration est la seule chose qu'ADR-0055 permet à une page de supposer. Tout le reste est une ligne que le lecteur
doit écrire : l'exemple l'écrit donc aussi. Un projet qui désactive les imports implicites écrit déjà `System` et ses
voisins dans chacun de ses fichiers ; les répéter dans chaque exemple montrerait à la plupart des lecteurs des lignes
qu'ils n'écrivent jamais, si bien que la base reste le défaut et que la décision le dit au lieu de prétendre valoir pour
tout projet.

## Alternatives Considered

### Garder le terme et documenter les imports en prose

Envisagé parce que cela laisse les exemples et le harnais intacts. Rejeté parce que le build ne verrait toujours pas
un import manquant, ce qui a laissé passer DOCS-11 ; une phrase ne se tient pas à un contrat comme un exemple.

### Retirer les imports partagés de tous les exemples

Envisagé pour une règle unique. Rejeté parce qu'un exemple d'instructions ne peut pas porter d'import : il cesserait
de compiler, alors que le contrat de compilation d'ADR-0055 est l'objet même.

## Consequences

### Positive

* Un exemple de déclarations se copie tel quel dans un projet de test qui garde les imports implicites du SDK : la
  page montre les imports dont le fichier du lecteur a besoin au-delà de ceux-là.
* Un exemple qui oublie un import fait échouer le build, et un test de contrat empêche le harnais de le fournir à
  nouveau.

### Negative

* Un exemple de déclarations s'allonge de ses lignes d'import, et chaque nouvel exemple doit les énoncer.
* Le contrat d'ADR-0055 diffère désormais selon le mode, ce qu'un rédacteur doit savoir.
* Un lecteur dont le projet désactive les imports implicites ajoute encore `System` et ses voisins à un exemple copié,
  comme il le fait pour chacun de ses fichiers.

### Risks

* Un lecteur d'ADR-0055 seul trouve toujours « aucune directive d'import dans un exemple ». Atténuation : cette
  décision l'amende et figure à côté de lui dans l'index.

## Follow-up Actions

* Aucune : le changement du harnais, les imports des exemples existants et le test de contrat sont arrivés avec cette
  décision (#516).

## References

* [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md) — la décision que celle-ci
  amende. Il garde son statut ; seul le terme d'import de son contrat d'exemple change, pour les exemples de
  déclarations.
* [ADR-0113](0113-check-a-distinct-collections-cardinality-when-the-specification-is-whole.fr.md) — un amendement
  qui laisse la décision amendée en place.
* [Essai UX 3, DOCS-11](../../../../tools/ux-trial/REPORT-3.fr.md#docs-11).
* Issue [#443](https://github.com/Reefact/just-dummies/issues/443), pull request
  [#516](https://github.com/Reefact/just-dummies/pull/516).
