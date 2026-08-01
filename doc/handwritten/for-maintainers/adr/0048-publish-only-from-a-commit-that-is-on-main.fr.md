# ADR-0048 | Ne publier qu'à partir d'un commit présent sur main

🌍 🇬🇧 [English](0048-publish-only-from-a-commit-that-is-on-main.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-01
**Decision Makers:** Reefact

## Contexte

Une release se déclenche en poussant un tag préfixé par son train (`lib-v*`, `xunit-v*`, `cli-v*`), ce
qui lance `.github/workflows/release.yml`. Ce workflow récupère **le commit taggué** — son étape de
checkout ne déclare aucun `ref:`, donc la référence qui déclenche le run est celle qui est construite —,
le packague, et pousse le résultat vers nuget.org via le trusted publishing.

`main` est protégée et exige ses checks. **Un tag n'est pas une branche**, et la protection de branche
ne l'atteint pas. Rien, entre le push d'un tag et une publication, n'établissait que le commit taggué
avait été relu ou vérifié.

Ce commit peut même n'avoir jamais été vu par personne : `git push origin <tag>` emporte les objets dont
le tag a besoin, si bien qu'un commit n'existant que dans un clone local arrive avec le tag et sert de
base à la publication.

Le workflow relance bien `dotnet build` et `dotnet test` sur le commit taggué, mais c'est un
sous-ensemble de ce qui protège `main`. Le plancher .NET Framework 4.7.2, le plancher Roslyn des
analyseurs, la vérification de compatibilité des assets packagés, CodeQL et l'analyse Sonar s'exécutent
sur les pull requests et pas dans le chemin de release.

Une version publiée sur nuget.org est immuable. On peut la délister, on ne peut pas la corriger. La
première release, `JustDummies 0.1.0-preview.1`, en a déjà donné une démonstration en petit : elle est
partie avec l'icône d'un autre produit, et le correctif a exigé une nouvelle version plutôt qu'une
correction.

## Décision

Une release ne publie qu'à partir d'un commit qui est un ancêtre de `main` ; le workflow de release le
vérifie avant de packager et refuse dans le cas contraire.

## Justification

**Le coût est asymétrique, donc la vérification a sa place avant la publication.** Tout le reste de ce
pipeline se rattrape en poussant un autre commit. Une mauvaise publication non : la version est
consommée, et le seul remède est une autre version. Un garde-fou dont le mode d'échec est « la release
s'arrête et vous re-taguez » est bon marché face à cela.

**L'ascendance de `main` est une preuve suffisante, pas une approximation.** `main` est protégée et
exige ses checks, donc un commit atteignable depuis `main` est prouvé être passé par eux. La
vérification n'a pas besoin de savoir *quels* checks ont tourné ni ce qu'ils ont conclu : elle emprunte
la garantie que la branche porte déjà.

**C'est une vérification bornée qui refuse franchement, ce que ce dépôt préfère** (ADR-0046).
Interroger les check runs du commit taggué via l'API serait plus précis, exigerait un jeton et une API
joignable, obligerait à décider ce que signifie un check absent ou sauté — et donnerait le même verdict
dans tous les cas qui comptent. La version bon marché n'est pas ici un compromis, c'est la version
proportionnée.

**La panne probable est une erreur, pas une attaque.** Taguer un vieux commit, taguer avant qu'un merge
soit terminé, taguer une branche locale que l'on croit à jour : ce sont des faux pas ordinaires, et ce
sont exactement ceux qu'une vérification d'ascendance attrape. Qu'elle bloque aussi une publication
délibérée depuis un commit non relu est un bénéfice second, pas la prémisse.

**Elle répond à une autre question que la protection de tag, et les deux sont souhaitables.** Une règle
de protection de tag GitHub restreint *qui* peut créer un tag de release. Cette décision restreint *ce
qui* peut être publié. Aucune n'implique l'autre, et seule la seconde est exprimable dans le dépôt.

## Alternatives considérées

### S'en remettre à une seule règle de protection de tag GitHub

Restreindre la création de tags aux mainteneurs vaut la peine et n'entre pas en conflit avec cet
enregistrement. Rejeté comme suffisant : cela contraint qui pousse le tag, pas où le tag pointe. Le
mainteneur habilité à publier est précisément la personne capable de taguer le mauvais commit par
inadvertance.

### Vérifier les check runs du commit taggué via l'API GitHub

La lecture directe de « ce commit est-il passé ? ». Rejeté : cela demande un jeton et une API joignable
dans le chemin de publication, cela oblige à trancher le cas des checks sautés, périmés ou absents, et
pour tout commit de `main` cela renvoie ce que l'ascendance établit déjà. Plus de pièces mobiles, même
réponse.

### Relancer toute la suite de checks dans le workflow de release

Rejeté : cela duplique le pipeline, allonge la publication, et ne prouve toujours rien sur la
relecture — un commit peut passer tous les checks sans que personne ne l'ait regardé.

### Accepter le risque

Défendable tant que le dépôt n'a qu'un mainteneur. Rejeté parce que l'artefact est immuable et que le
garde-fou coûte un `git fetch` : l'arbitrage n'est pas serré.

## Conséquences

### Positives

* Un tag sur un commit qui n'a jamais atteint `main` arrête la release au lieu de la publier.
* Le chemin de release hérite de la protection de `main` sans la dupliquer.
* Le message d'échec dit quoi faire — taguer un commit présent sur `main` — plutôt que de rapporter une
  condition interne.

### Négatives

* Publier depuis une branche n'est plus possible, y compris pour un correctif urgent. C'est la
  décision, pas un oubli : le correctif passe par `main` d'abord.
* `workflow_dispatch` est exempté, sa référence étant déjà une branche et non un tag. Un déclenchement
  manuel depuis une branche autre que `main` reste donc possible pour qui peut le déclencher.

### Risques

* La prémisse du garde-fou lui est extérieure. Si la protection de `main` était relâchée ou ses checks
  requis retirés, l'ascendance passerait toujours en prouvant beaucoup moins. Rien dans ce dépôt ne le
  détecte : c'est une propriété des réglages du dépôt.
* Un futur changement de GitHub sur la façon dont un checkout déclenché par un tag configure son remote
  pourrait casser le fetch dont dépend la vérification. Elle échoue fermée — l'étape sort en erreur au
  lieu de se sauter — donc la panne arrêterait une release plutôt que d'en laisser passer une.

## Actions de suivi

* Envisager un ruleset GitHub de protection de tag sur `lib-v*`, `xunit-v*` et `cli-v*`. C'est un
  réglage de dépôt, complémentaire de cet enregistrement, et qui ne peut pas être commité ici.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la préférence pour une
  vérification bornée qui refuse franchement plutôt qu'un mécanisme plus puissant.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) — l'autre décision du
  chemin de release prise en même temps.
* `.github/workflows/release.yml` — où la vérification est appliquée, et pourquoi chacune de ses deux
  mécaniques est porteuse.
