# ADR-0074 | Rédiger à la main les notes GitHub d'une release à partir du changelog, et refuser sans elles

🌍 🇬🇧 [English](0074-draft-a-releases-github-notes-by-hand-and-refuse-without-them.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-18
**Decision Makers:** Reefact

## Contexte

JustDummies publie quatre trains de release versionnés indépendamment (`tools/trains.sh`).
Chaque train tient un `CHANGELOG.md`, au format Keep a Changelog, rédigé par le workflow GitHub
Actions `changelog` à partir des pull requests mergées, et relu par un humain dans une pull
request avant d'être mergé sur `main`.

Jusqu'ici, `tools/packaging/release-notes.sh` — invoqué par `release.yml` au moment du tag, et
répété par `release-dryrun.yml` à chaque push sur `main` — construisait le corps d'une Release
GitHub directement depuis `git log` : il parcourait les commits depuis le tag précédent du train
et gardait ceux dont le scope Conventional Commit appartenait au train publié.
[ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.fr.md) exige ce scope sur tout
commit qui fait avancer la version précisément pour que ce filtre partitionne correctement.

Ce corps est ce qu'un consommateur lit sur la page des Releases du dépôt, et ce vers quoi pointe
la page du package sur NuGet — le seul texte orienté produit, par version, que ce projet publie,
distinct du registre cumulatif et technique de `CHANGELOG.md`, qui détaille chaque contrainte et
chaque cas limite. Un sujet de commit est écrit pour le relecteur de ce diff précis, pas pour un
développeur qui décide si une nouvelle version vaut la peine d'être adoptée : une release passée
listait `refactor(cli): guard through ArgumentNullException.ThrowIfNull` à côté de `feat(cli):
read project defaults the command line overrides`, dans la même liste, sans rien qui distingue ce
qui intéresserait un consommateur de ce qui ne l'intéresserait pas. Le repli du script précédent —
imprimer `_No user-facing changes in this component._` quand aucun commit ne correspondait —
concédait déjà qu'un journal de commits répond à « qu'est-ce qui a changé pour le mainteneur », pas
à « qu'est-ce qui a changé pour vous ».

Au moment où la version d'un train est taguée, un texte décrivant cette release en termes produit
existe déjà dans le dépôt : la section du changelog pour cette version, relue par un humain avant
d'être mergée. Rien dans le pipeline de `release.yml` ne la lisait.

## Décision

Les notes d'une Release GitHub publiée sont désormais lues telles quelles depuis un fichier
committé, rédigé à la main, orienté produit — un par train de release et par version majeure — et
`release.yml` refuse de publier, plutôt que de retomber sur quoi que ce soit dérivé de
l'historique des commits, quand ce fichier ou la section propre à la version n'existe pas.

## Justification

**Le texte destiné au consommateur et le texte destiné au mainteneur répondent à des questions
différentes.** Un message de commit explique un diff à un relecteur ; une note de release explique
une version à quelqu'un qui décide de monter de version. Dériver la seconde de la première,
mécaniquement, confondait les deux et ne servait bien ni l'une ni l'autre — ce que confirme le
besoin du script précédent de son propre repli « aucun changement visible » pour le cas où la
dérivation mécanique n'avait rien d'utile à dire.

**La source que lit cette décision n'est pas une invention nouvelle.** La section du changelog
pour une version est déjà relue avant d'être mergée, et déjà écrite en termes produit — la plupart
de ses puces ouvrent déjà sur une phrase d'accroche autonome. Produire une note de release à partir
d'elle est une étape de mise en forme, pas une demande d'origine à du contenu que personne n'a
relu.

**Refuser en l'absence du fichier suit le précédent déjà posé par l'ADR-0013.** Là-bas, un commit
sans scope est refusé plutôt que deviné vers un train par défaut, parce que deviner produit une
erreur plus subtile et plus difficile à remarquer que refuser. Le même arbitrage s'applique ici :
une release publiée avec un texte dérivé des commits ressemble à une note de release sans en être
une, alors qu'un `release.yml` en échec est bruyant, immédiat, et pointe exactement ce qui manque.

**La rédaction se fait en amont du tag, pas dans sa précipitation.** La skill `release-train` pose
déjà que taguer et publier sont les actions du mainteneur, préparées à l'avance. Garder la
rédaction de ce fichier entièrement hors de `release.yml` — aucun appel modèle au moment du tag —
évite qu'une release d'un artefact immuable et publié dépende jamais d'une étape de génération non
relue, dans la précipitation de la publication.

## Alternatives considérées

### Continuer à dériver de git log, en durcissant la convention des messages de commit

Envisagé parce que cela ne demande ni nouveau fichier ni nouvelle étape manuelle. Rejeté : un
message de commit est voué à décrire un diff, c'est sa raison d'être ; aucune convention de
rédaction ne transforme `refactor(cli): guard through ArgumentNullException.ThrowIfNull` en prose
orientée produit sans inventer un contenu que le commit n'a jamais été écrit pour porter.

### Générer la note de release en CI au moment du tag, comme le workflow `changelog` rédige un changelog

Envisagé parce que le motif existe déjà et fonctionne bien pour le changelog : un modèle rédige,
un humain relit dans une pull request, puis c'est mergé. Rejeté spécifiquement pour une release :
le workflow changelog peut se permettre ce cycle de relecture parce que rien n'a encore été publié
quand il tourne. `release.yml` tourne sur un tag sur le point de produire un package NuGet immuable
et une Release GitHub permanente — atteindre ce point avec une prose non relue sur le point de
devenir le texte public de la release supprime précisément le garde-fou sur lequel repose le
workflow changelog. Une future variante qui rédigerait dans une pull request relue *avant* le tag,
de la même façon que le changelog, ne contredirait pas cette décision — elle produirait toujours le
fichier committé que ce design lit ; seule la *manière* de le produire est rejetée ici, pas la
possibilité d'assister la rédaction.

### Garder la liste dérivée des commits comme repli quand le fichier rédigé à la main manque

Envisagé comme un atterrissage plus doux qu'un refus pur et simple. Rejeté : un repli qui produit
silencieusement un artefact de moindre qualité est précisément ce que cette décision écarte. Une
note de release manquante doit apparaître comme un manque à combler avant qu'un tag soit poussé,
pas être discrètement rattrapée par le mécanisme qu'elle était censée remplacer.

## Conséquences

### Positives

* Le corps d'une Release GitHub est lisible par un développeur qui décide de monter de version,
  dans le même registre sur les quatre trains.
* L'historique des commits, le changelog et la note de release répondent chacun à une question,
  au lieu qu'un seul artefact réponde mal à deux questions.
* Une note de release manquante est détectée par une release en échec, avant la publication,
  plutôt que découverte après coup sur la page des Releases.

### Négatives

* Rédiger la note de release est désormais une étape manuelle — la skill `release-notes` — qu'un
  mainteneur ou un agent doit penser à faire avant de taguer ; plus rien dans le dépôt ne la
  produit de bout en bout automatiquement comme le faisait l'ancien script.
* Deux fichiers doivent désormais rester alignés : la section du changelog et la note de release
  qui en est tirée. Rien ne force leur cohérence au-delà de la discipline décrite par la skill
  `release-notes`.

### Risques

* Un tag poussé avant que la note de release soit écrite fait échouer la release purement et
  simplement. Mitigation : la checklist « avant une release » de la skill `release-train` liste
  désormais sa rédaction avant l'étape du socle d'API publique, et échouer bruyamment ici est le
  but recherché, pas un défaut à contourner.
* La note de release peut diverger du changelog dont elle a été tirée, si le changelog change
  après que la note a été écrite. Mitigation : le même risque que `CHANGELOG.md` lui-même porte
  déjà entre la rédaction et le merge, traité de la même façon — une relecture humaine au moment
  de la rédaction, pas un contrôle automatisé.

## Actions de suivi

* Aucune requise. `.claude/skills/release-notes/SKILL.md` porte l'instruction opérationnelle,
  `tools/packaging/release-notes.sh` applique le refus, et `release-dryrun.yml` le répète contre
  le dernier tag publié de chaque train.

## Références

* [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.fr.md) — la partition par
  scope. Son Contexte et ses Références décrivent encore `tools/packaging/release-notes.sh` comme
  sélectionnant les commits par scope ; ce dossier change ce mécanisme sans modifier la décision
  propre de l'ADR-0013, qui continue de gouverner `CHANGELOG.md` et reste non modifiée, comme doit
  le rester un dossier accepté.
* [ADR-0051](0051-land-pull-requests-by-rebase.fr.md) — ses Conséquences
  notent que `tools/packaging/release-notes.sh` doit continuer à filtrer les commits de merge de
  l'historique ; ce script ne lit plus du tout l'historique des commits, pour la même raison : ce
  dossier change le mécanisme, pas la décision de l'ADR-0051, qui reste non modifiée.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — l'éthique de refuser
  bruyamment plutôt que de dégrader en silence, empruntée ici pour une note de release manquante.
* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) — non affecté ; continue de
  gouverner quel commit un tag peut viser.
* `.claude/skills/release-notes/SKILL.md`, `.claude/skills/release-train/SKILL.md` — où vivent le
  format et la procédure.
