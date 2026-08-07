# ADR-0051 | Intégrer les pull requests par rebase

🌍 🇬🇧 [English](0051-land-pull-requests-by-rebase.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-07
**Decision Makers:** Reefact

## Contexte

Jusqu'ici, ce dépôt intégrait chaque pull request par un **commit de merge**. `main` en porte le
résultat : `Merge pull request #5` jusqu'à `Merge pull request #9`, chacun encadrant les commits de
la branche qu'il a fait entrer.

Ce choix n'a jamais été enregistré comme une décision. Il vivait comme une prémisse dans la prose,
affirmée à sept endroits — `CONTRIBUTING.md` deux fois, `AGENTS.md`, `CLAUDE.md`, la commande
`/tidy-history`, le hook `history-hygiene` et le linter de commits — chacun répétant « ce dépôt
fusionne avec un commit de merge » pour justifier une règle qui en dépend. Rien ne les reliait, et
rien n'a détecté qu'elles étaient toutes devenues fausses d'un coup.

Trois règles argumentent aujourd'hui à partir de cette prémisse :

* **Ranger une branche avant relecture est obligatoire**, parce que les commits d'une branche
  atteignent l'historique protégé tels quels (`AGENTS.md`, « Tidying history before a pull request »).
* **Le titre d'une pull request se lit à trois endroits**, dont le commit `Merge pull request #NN` que
  GitHub écrit (`CONTRIBUTING.md`, « Pull request titles »).
* **Une branche est jetable**, parce que le commit de merge en préserve l'historique
  (`CONTRIBUTING.md`, « La doctrine »).

Une version se coupe en taguant un commit, et le workflow de release refuse de publier depuis un
commit qui n'est pas un ancêtre de `main`
([ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md)). Sous un commit de merge, les
commits d'une branche sont ancêtres de `main` une fois celle-ci intégrée, donc taguer la tête de
branche publiait.

Les pull requests Dependabot sont intégrées par un workflow plutôt qu'à la main
(`.github/workflows/dependabot-automerge.yml`), qui nomme explicitement la méthode de merge et ne
peut pas se rabattre sur une autre. Chacune de ces pull requests porte exactement un commit, déjà
conventionnel, dont le type est imposé par `.github/dependabot.yml` — un commit de merge double donc
l'historique qu'une montée de version d'une ligne écrit.

Le réglage de méthode de merge du dépôt est le point d'application : GitHub refuse toute méthode que
le dépôt n'autorise pas, quel que soit le demandeur et la façon de demander.

## Décision

Les pull requests sont intégrées à `main` par **rebase**, et le dépôt n'autorise aucune autre méthode
de merge.

## Justification

**Un `main` linéaire est la forme que le reste des conventions suppose déjà.** Toutes les règles de
`CONTRIBUTING.md` placent le témoignage sur le *commit* : une intention par commit, un en-tête
conforme sur chacun, le scope qui partitionne les trains de version, le footer `Refs:` qui relie un
commit à son issue. Le commit de merge ajoutait par-dessus un second témoignage, plus faible — un
titre écrit par GitHub, non linté, sans scope et donc exclu des notes de version. Le rebase retire la
couche qui ne portait rien que les commits ne portaient déjà mieux.

**Cela aiguise la règle qui compte le plus ici, au lieu de l'affaiblir.** Intégrer par rebase ne rend
pas une branche mal rangée moins coûteuse ; cela la rend plus coûteuse. Sous un commit de merge, les
commits d'une branche arrivaient au moins encadrés — le commit de merge marquait où ils commençaient
et finissaient, et un lecteur pouvait sauter la plage. Rebasés, ils arrivent un par un sur la ligne,
indistinguables de tous les autres, sans rien pour les marquer comme une unité. L'obligation de
ranger une branche *avant* qu'elle n'atterrisse gagne donc en force sous cette décision, raison pour
laquelle les règles qui l'énoncent sont réaffirmées plutôt qu'assouplies.

**L'alternative qui cache le désordre est celle qu'il faut refuser.** Le squash produirait lui aussi
un `main` linéaire, et le ferait en jetant le témoignage par commit sur lequel les conventions sont
bâties. C'est le compromis inverse : il rendrait le rangement de l'historique cosmétique au lieu de
porteur, et il écraserait une pull request portant une fonctionnalité, le refactor qui l'a préparée
et ses tests en un seul commit dont le type ne peut pas la nommer honnêtement — précisément la
situation que `CONTRIBUTING.md` cite pour expliquer pourquoi une *branche* n'a pas de type.

**Nommer une méthode, et n'autoriser qu'elle, est ce qui fait tenir la décision.** Le réglage du
dépôt refuse les autres d'emblée, donc aucun contributeur et aucun workflow ne peut intégrer une pull
request autrement par habitude ou par accident. La prémisse dont la prose argumente est alors imposée
par la plateforme plutôt qu'affirmée par sept paragraphes.

## Alternatives envisagées

### Conserver le commit de merge

Le statu quo, et la prémisse contre laquelle la documentation a été écrite : le conserver n'aurait
rien coûté à rédiger.

Écarté parce que la seule contribution propre du commit de merge à `main` est le titre de la pull
request, et que ce titre est non linté, sans scope et absent des notes de version — un témoignage que
les commits tiennent déjà, tenu moins bien. Sur les pull requests Dependabot, qui atterrissent sans
qu'un humain y touche, il double l'historique qu'une montée de version d'une ligne écrit.

### Squash and merge

Il produit le même `main` linéaire, et il pardonne une branche mal rangée : ce que la branche portait
arrive en un commit unique.

Écarté parce que ce pardon est le défaut. Les conventions de ce dépôt consignent le changement sur le
commit — une intention par commit, un en-tête conforme sur chacun, un scope qui décide quel train de
version le publie. Le squash écrase tout cela en un commit dont le type unique ne peut pas nommer une
pull request qui en porte légitimement plusieurs, et il ferait passer la règle de rangement de
l'historique d'une exigence à une coquetterie.

### Autoriser toutes les méthodes et choisir au cas par cas

L'option la plus souple : un commit de merge pour une large branche de fonctionnalité, un rebase pour
un bump d'un commit.

Écarté parce qu'une méthode de merge choisie au cas par cas est une méthode sur laquelle personne ne
peut s'appuyer. Les règles de `CONTRIBUTING.md` et d'`AGENTS.md` argumentent à partir de ce qui
arrive aux commits d'une branche quand elle atterrit ; si cette réponse varie d'une pull request à
l'autre, aucune ne peut en énoncer la conséquence, et l'obligation de ranger l'historique devient
conditionnée à un choix fait après que la branche est déjà écrite.

## Conséquences

### Positives

* `main` devient une ligne unique de commits conventionnels, chacun linté, scopé et lisible seul.
* La règle de rangement de l'historique gagne sa justification la plus forte : plus rien n'encadre les
  commits d'une branche, donc plus rien ne cache un désordre.
* Une montée de version Dependabot coûte à `main` exactement un commit, celui que Dependabot a écrit.
* Le réglage de méthode de merge impose la prémisse dont la documentation argumente, si bien que les
  deux ne peuvent plus diverger.

### Négatives

* Le titre de la pull request n'apparaît plus nulle part dans l'historique de `main` ; l'identité de
  la demande vit dans la pull request elle-même et dans ses commits.
* Les hashes de commit d'une branche n'atteignent jamais `main` — le rebase les rejoue en nouveaux
  commits — donc un hash lu sur une branche ne peut pas être cité comme un commit de `main`.
* `main` conserve les commits de merge des pull requests intégrées avant cette décision. Les outils
  qui lisent l'historique doivent continuer de les filtrer ;
  `tools/packaging/release-notes.sh` et le job CI de commit-lint le font déjà.

### Risques

* **Taguer une tête de branche ne publie plus.** Puisque les commits d'une branche ne sont pas
  ancêtres de `main` après intégration, la vérification de
  [l'ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) refuse un tag posé sur la
  branche plutôt que sur le commit reçu par `main`. C'est la vérification qui fonctionne comme prévu,
  mais cela transforme une habitude jusque-là inoffensive en release refusée.
* **Une branche ayant mergé `origin/main` en elle emporte des commits de merge dans le rebase.**
  `CONTRIBUTING.md` autorise ce merge dès qu'une branche est partagée. La façon dont GitHub rejoue
  une telle branche relève de son comportement, pas de ce dépôt, et mérite d'être confirmée sur la
  première branche partagée qui en aura besoin.

## Actions de suivi

* N'autoriser que le rebase merging dans les réglages de pull request du dépôt — le point
  d'application de cette décision.
* Confirmer, sur la première branche partagée ayant mergé `origin/main`, que son intégration se
  comporte comme attendu.

## Références

* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) — publier exige que le commit
  tagué soit un ancêtre de `main`, ce que cette décision rend plus strict en pratique.
* [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.fr.md) — le scope d'un commit
  décide quel train de version le publie, l'un des témoignages par commit que le squash aurait
  coûtés.
* `CONTRIBUTING.md` (« Branches », « Pull request titles »), `AGENTS.md` (« Tidying history before a
  pull request ») — les règles réaffirmées contre cette prémisse.
