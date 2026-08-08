# ADR-0053 | Réécrire l'historique publié en une seule ligne, et y porter les tags de release

🌍 🇬🇧 [English](0053-rewrite-the-published-history-to-a-single-line.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-08
**Accepted:** 2026-08-08
**Decision Makers:** Reefact

## Contexte

L'[ADR-0051](0051-land-pull-requests-by-rebase.fr.md) a fait du rebase la seule méthode d'intégration,
si bien que toute pull request atterrit désormais comme une suite de commits. Il a consigné ce qu'il
laissait volontairement derrière lui : « `main` conserve les commits de merge des pull requests
intégrées avant cette décision. Les outils qui lisent l'historique doivent continuer à les filtrer ».
`main` portait donc deux formes à la fois — linéaire après la décision, en merge avant elle.

Avant elle, `main` comptait 485 commits, dont 162 merges : 121 écrits par GitHub sous la forme
`Merge pull request #NN`, et 41 back-merges qu'une branche produisait en rapatriant `main` dans
elle-même, ce que `CONTRIBUTING.md` autorise dès qu'une branche est partagée. 120 des 121 merges de
pull request étaient fast-forwardables — la branche était déjà à jour, donc l'arbre du commit de
merge était identique à celui de la tête de branche et il n'apportait aucun contenu propre.

Cinq tags sont publiés, dont quatre portent une GitHub Release, et les paquets qu'ils ont produits
sont sur nuget.org. Une version publiée y est immuable : on peut la délister, on ne peut pas la
corriger ([ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md)). Ces paquets sont
construits avec SourceLink, qui grave l'URL du dépôt **et le SHA du commit** dans les symboles
publiés : un débogueur récupère donc les sources par SHA, et non par tag.

L'[ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) a par ailleurs décidé qu'une
release ne publie qu'à partir d'un commit ancêtre de `main`, et son suivi consigne un ruleset de
protection des tags restreignant leur création, leur mise à jour et leur suppression. Cette note
donne le déplacement d'un tag comme l'un des dommages que le ruleset existe pour empêcher : il
« romprait le lien entre un artefact publié immuable et le commit que son tag nomme ».

Aucune version stable n'est publiée. Tout ce qui est sur nuget.org est une preview :
`JustDummies 0.1.0-preview.1` et `1.0.0-preview.1`, `JustDummies.Xunit 1.0.0-preview.1`,
`JustDummies.DiagnosticCatalog 1.0.0-preview.2`. 42 commits de `main` portaient une signature, dont
33 commits hors merge que toute réécriture doit recréer.

## Décision

L'historique que `main` portait déjà est réécrit en la ligne unique que
l'[ADR-0051](0051-land-pull-requests-by-rebase.fr.md) impose pour la suite, et chaque tag de release
publié est repointé sur le commit portant l'arbre identique.

## Justification

**L'avant-1.0 est la seule fenêtre où cela coûte peu, et elle se referme d'elle-même.** Le coût d'une
réécriture d'historique publié est proportionnel à ce qui dépend des anciens identifiants de commit.
Aujourd'hui, cela se limite à quatre paquets preview vieux de quelques jours, sans aucune release
stable derrière eux. Après la 1.0.0, la même opération toucherait des versions que les consommateurs
sont fondés à tenir pour définitives, et la réponse devrait être non. La décision n'est donc pas
« une réécriture est-elle jamais acceptable » mais « l'est-elle maintenant », et la fenêtre est tout
l'argument.

**Le lien que porte un tag est préservé dans le sens qui tranche : l'arbre.** Chaque tag repointé
nomme un commit dont l'arbre est byte-identique à celui qu'il nommait avant, si bien que les sources
ayant produit un paquet publié restent exactement joignables, sous le même tag, à un identifiant
différent. Ce qui change est l'identifiant, pas le contenu — et la provenance d'un paquet est une
affirmation sur le contenu. C'est la lecture étroite de la note de suivi de l'ADR-0048, et c'est
celle que cette décision adopte : cette note a été écrite pour interdire de déplacer un tag vers des
sources *différentes*, l'accident qu'un ruleset ne sait pas distinguer de celui-ci et qu'il refuse à
juste titre par défaut.

**Laisser les tags en arrière casserait quelque chose sur quoi l'ADR-0048 s'appuie réellement.** La
prémisse de cette décision est qu'un tag de release nomme un commit atteignable depuis `main`, parce
que la protection de `main` est ce qui prouve que le commit a été vérifié. Des tags laissés sur les
commits d'avant la réécriture nommeraient des commits qui ne sont plus du tout sur `main` :
l'ascendance dont le chemin de release emprunte sa garantie aurait disparu, et les tags dépendraient
pour leur survie d'une référence d'archive que personne n'est tenu de conserver. Les porter est
l'option qui garde l'invariant vrai.

**C'est le dernier moment où une telle réécriture peut être prise, et la prendre est ce qui permet à
la règle de tenir ensuite.** Porter les tags a exigé de suspendre le ruleset de protection des tags,
et l'argument justifiant cette suspension — aucune release stable, quatre previews vieilles de
quelques jours — expire à la 1.0.0. Rétablir le ruleset n'est donc pas de l'intendance mais l'acte
qui referme la fenêtre : après lui, un tag de release ne peut plus être déplacé du tout, et la note
de l'ADR-0048 gouverne sans exception. Cette décision achète une fois un historique linéaire, au prix
d'une suspension qu'elle met elle-même fin — c'est pourquoi elle est une exception qui confirme cette
note plutôt qu'une lecture qui l'affaiblit.

**Une réécriture est sûre ici parce que sa correction est vérifiable, pas seulement intentionnelle.**
L'opération a un critère de réussite exact — l'arbre du tip doit être inchangé — qui tient ou ne
tient pas. Autour de lui, le nombre de commits, le message, l'auteur et les dates de chacun,
l'absence de marqueurs de conflit et l'identité d'arbre des tags sont tous décidables avant toute
publication. C'est ce qui sépare ceci d'une réécriture dont on espère qu'elle s'est bien passée, et
c'est pourquoi la frontière que ce dépôt trace ailleurs — tenter moins, refuser franchement, vérifier
ce que l'on affirme ([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md)) —
est satisfaite plutôt que contournée.

**La moitié du travail aurait conservé le défaut qu'il s'agissait de retirer.** Ne supprimer que les
120 merges de pull request fast-forwardables est prouvablement sans perte et ne demande aucune
décision de contenu, mais cela laisse 41 back-merges et donc laisse `main` se ramifier. La raison de
toucher à l'historique était que `main` n'avait pas la forme que l'ADR-0051 suppose ; un historique
qui se ramifie encore ne l'a pas acquise.

## Alternatives envisagées

### Conserver les commits de merge

Le statu quo que l'ADR-0051 avait explicitement accepté, et la seule option qui ne coûte rien.

Rejetée parce qu'elle laisse `main` sous deux formes indéfiniment, et que chaque outil lisant
l'historique continue de le payer. Le coût de l'alternative — réécrire — ne fait que monter : garder
les commits de merge est donc une décision de les garder pour toujours, prise par défaut plutôt que
sur ses mérites.

### Ne retirer que les merges de pull request, et garder les back-merges

Prouvablement sans perte : ces 120 merges étaient fast-forwardables avec un arbre identique, donc les
supprimer est de la chirurgie de graphe sans décision de contenu, sans conflit et sans rien à
vérifier au-delà de la forme.

Rejetée comme insuffisante une fois son résultat observé. Elle retire le bruit qu'un lecteur remarque
en premier — les lignes `Merge pull request #NN` — mais `main` se ramifie encore en 41 points : la
forme linéaire que le reste des conventions suppose n'est toujours pas celle que `main` a.

### Laisser les tags sur les commits d'avant la réécriture, préservés par une référence d'archive

Elle garde valide chaque SHA publié, donc SourceLink continue de résoudre et aucun identifiant
d'artefact publié ne change.

Rejetée parce qu'elle échange une perte bornée et unique contre une obligation sans fin. La référence
d'archive devient porteuse pour toujours, sans que rien n'en consigne la raison, et le premier
nettoyage qui la supprime casse silencieusement ce qu'elle protégeait. Elle laisse en outre les tags
de release hors de `main`, qui est l'ascendance dont l'ADR-0048 argumente.

### Repartir d'un historique neuf sur l'arbre courant

Le `main` le plus propre possible : un commit, aucun passé à réconcilier.

Rejetée parce que le relevé commit par commit est ce sur quoi les conventions de ce dépôt sont
bâties — une intention par commit, un en-tête conforme, un scope qui décide quel train de release le
publie. Jeter 323 commits de cette nature pour gagner une forme est l'échange que l'ADR-0051 a déjà
refusé en écartant le squash.

## Conséquences

### Positives

* `main` est une ligne unique de 323 commits conventionnels, sans aucun commit de merge ni aucun
  commit écrit par GitHub plutôt que par un auteur.
* La prémisse de l'ADR-0051 est désormais vraie de tout l'historique, et pas seulement de ce qui
  atterrit après lui : les règles qui en argumentent ne portent plus d'exception.
* Les outils qui lisent l'historique n'ont plus besoin d'un filtre à merges pour être corrects.
* Chaque tag de release est de nouveau ancêtre de `main`, donc la vérification de l'ADR-0048 garde le
  sens qu'on lui a donné.

### Négatives

* Tous les identifiants de commit de `main` ont changé. Un SHA cité hors de ce dépôt — une issue, une
  revue, un signet — ne résout plus, et l'historique d'avant la réécriture n'est pas récupérable.
* `main` ne porte plus aucun commit signé. 33 commits hors merge l'étaient, et une réécriture ne peut
  pas les re-signer : les signatures sont perdues, non invalidées.
* SourceLink ne peut plus résoudre pour les versions déjà publiées : leurs symboles nomment des
  commits qui n'existent plus. Ces versions sont délistées et dépréciées en conséquence (voir Actions
  de suivi).
* Quelques commits au milieu de l'historique portent un contenu qui diffère momentanément de ce que
  leur branche tenait, parce que linéariser deux lignées divergentes doit bien les réconcilier
  quelque part. Le tip est exact ; un commit pris au hasard au milieu peut ne pas compiler.

### Risques

* **`git bisect` peut tomber sur un commit qui ne compile pas.** Les états transitoires ci-dessus sont
  ordinaires pour toute linéarisation, y compris un simple rebase, mais ils sont nouveaux pour le
  `main` de ce dépôt.
* **La décision n'est pas reproductible, et ne doit pas l'être.** Elle repose sur l'absence de
  release stable, ce qui cesse d'être vrai à la 1.0.0. Le ruleset de protection des tags rétabli est
  ce qui l'impose en pratique : une réécriture ultérieure devrait le lever délibérément, et c'est ce
  moment-là qu'il faut saisir pour relire ce document. Rien d'autre dans le dépôt ne détecte que
  l'argument a expiré.

## Actions de suivi

* Délister **et** déprécier sur nuget.org toute version publiée depuis l'historique d'avant la
  réécriture, avec un message disant ce qu'elle est et que le pas-à-pas dans les sources ne résout
  plus :
  *« Preview published before the repository's history was rewritten. Superseded — this version is
  unsupported and will receive no fixes. Source-stepping (SourceLink) does not resolve for it. »*
  L'ensemble est constitué de toutes les versions publiées avant cette décision :
  `JustDummies 0.1.0-preview.1` et `1.0.0-preview.1`, `JustDummies.Xunit 1.0.0-preview.1`, et
  `JustDummies.DiagnosticCatalog 1.0.0-preview.2`. Délister ne suffirait pas : une version délistée se
  restaure encore par version exacte, donc c'est la dépréciation qui atteint réellement un
  consommateur qui en détient une.
* Réactiver le ruleset de protection des tags, la protection de branche de `main` et le workflow de
  release, tous trois suspendus pour la réécriture. Ce sont des réglages du dépôt, non committables
  ici ; cette ligne consigne qu'ils doivent être rétablis. Rétablir le ruleset est l'étape qui met fin
  à l'exception portée par cette décision : c'est donc celle qu'il ne faut pas oublier.
* La note de suivi de l'ADR-0048 n'est **pas** amendée. Elle soutient que déplacer un tag publié rompt
  le lien entre l'artefact et son commit, et elle continue de gouverner tous les tags à partir d'ici.
  Cette décision en est l'exception unique et bornée, prise tant qu'aucune release stable n'existe et
  prise précisément pour que cette note puisse être appliquée ensuite sans qu'un historique d'avant
  la 1.0 la contredise.

## Références

* [ADR-0051](0051-land-pull-requests-by-rebase.fr.md) — la forme linéaire que cette décision applique
  rétroactivement.
* [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.fr.md) — la vérification d'ascendance, et
  la note sur la protection des tags que cette décision lit étroitement.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la préférence pour une
  opération bornée dont la correction est vérifiée plutôt qu'espérée.
* [ADR-0045](0045-renumber-the-decision-base.fr.md) — la renumérotation dont l'explication supposait
  que l'historique git n'avait jamais été réécrit ; corrigée par ce changement.
