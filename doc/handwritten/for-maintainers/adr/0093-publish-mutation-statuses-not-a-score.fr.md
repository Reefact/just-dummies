# ADR-0093 | Publier des statuts de mutation, pas un score

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0093-publish-mutation-statuses-not-a-score.md)

**Statut :** Proposed
**Proposé :** 2026-09-01
**Décideurs :** Reefact

## Contexte

ADR-0022 a fait du test de mutation une vérification cantonnée au diff sur chaque pull
request, adossée à un balayage complet hebdomadaire. ADR-0025 a rendu la vérification
par-PR consultative et a posé l'application sur ce balayage : *« le balayage hebdomadaire
est le vrai signal »*. Tous les seuils de score de `build/stryker/` ont été fixés de la même
manière — à partir du score complet mesuré du composant, arrondi vers le bas — et celui de
la bibliothèque a été délibérément laissé à zéro, en attente du premier balayage qui le
mesurerait.

Ce balayage s'est terminé le **2026-09-01**, le premier jamais mené au bout pour la
bibliothèque. Il a annoncé **100 %**. Sur 4 575 mutants jugés, **2 070 ont été tués par un
test en échec et 2 505 se sont terminés en timeout**, que Stryker compte comme un kill ;
rien n'a survécu parce que plus de la moitié du composant n'a jamais été jugée. Les timeouts
ne se concentrent pas sur du code qui boucle : ils sont répartis sur tous les fichiers du
composant.

La cause est mesurée et banale. L'outil calibre son budget de temps par mutant sur une
exécution initiale de la suite **seule**, puis lance les mutants dans des sessions
concurrentes. Sur la suite de ce dépôt, quatre sessions concurrentes mettent chacune environ
**le double** du temps d'une seule. La limite est donc fixée dans des conditions et
appliquée dans d'autres, et une session peut la dépasser avant que le mutant ait fait quoi
que ce soit.

Un `Timeout` compté comme un kill inverse le signal : plus la machine est chargée, plus le
score monte. Rien dans la sortie de l'outil n'invite au doute — il ne rapporte pas le budget
qu'il a retenu, et le résumé du run écrivait de lui-même « tous les mutants ont été tués ».

Une seconde observation porte sur la même question et n'a **aucune cause identifiée**. Sur
`JustDummies.GenAny/Guards.cs`, à un commit donné et avec le même oracle déclaré de 495
tests lu dans les deux journaux, le runner de la CI a annoncé 38 survivants et un conteneur
Linux 52 : dix-sept mutants que le runner dit tués survivent dans le conteneur. Pour l'un
d'eux, appliquer la mutation au source à la main laisse toute la suite verte, sous les deux
versions de SDK impliquées. Neuf explications ont été éliminées par la mesure — dont la
concurrence de l'outil et la différence de SDK — et aucune n'en rend compte. Deux exécutions
locales identiques s'accordent à deux mutants sur 623.

Rien dans ce dépôt ne barre aujourd'hui sur un score de mutation : chaque configuration
porte `break: 0` et le balayage désactive le seuil par construction.

## Décision

Le dépôt publie les résultats de mutation en **comptes par statut**, jamais en score, et
aucun seuil n'est fixé à partir d'un run dont les statuts sans verdict ne sont pas un
résidu.

## Justification

* **Un timeout n'est pas la preuve que quoi que ce soit ait remarqué le changement.** C'est
  l'absence de verdict dans un budget, et ici le budget était faux pour des raisons
  étrangères au code testé. Le fondre dans « tué » n'est pas une erreur d'arrondi, c'est une
  erreur de catégorie, et c'est elle qui a transformé « la moitié du composant n'a jamais
  été jugée » en « 100 % ».
* **Le sens de la défaillance est ce qui la rend dangereuse.** Un défaut qui rend un chiffre
  pessimiste finit par se voir parce qu'il gêne ; celui-ci déplace le chiffre dans le sens
  que tout le monde a envie de croire, et il le déplace avec la charge de la machine plutôt
  qu'avec la suite. Un nombre qui ne peut pas descendre pour une mauvaise raison ne se lit
  plus du tout.
* **Cesser de publier un score ne coûte rien, parce que rien n'en consomme un.** Aucune
  vérification n'échoue sur un chiffre de mutation, donc cette décision ne retire aucune
  application — elle retire une affirmation. Ce qui la remplace est strictement plus
  informatif : les comptes dont un score était tiré, qu'un lecteur peut combiner lui-même
  une fois qu'il sait ce que chaque statut signifie.
* **La règle sur le seuil découle du même fait.** La méthode qui a fixé toutes les barres
  existantes — prendre le score mesuré, arrondir vers le bas — présuppose la mesure.
  Appliquée à ce balayage, elle aurait calé le cliquet de la bibliothèque sur un artefact et
  fait passer chaque run ultérieur pour une régression contre un nombre qui n'a jamais
  existé. Nommer la condition préalable coûte moins cher que de la redécouvrir.
* **La divergence inexpliquée plaide pour cette forme plutôt que contre elle.** Sa cause est
  inconnue et peut le rester un moment ; un rapport qui publie ce qu'il a observé, statut par
  statut, survit à cette ignorance, là où un nombre unique qui moyenne dessus en silence n'y
  survit pas.
* **Ceci n'affaiblit pas ADR-0025, ceci répare la prémisse sur laquelle elle repose.** Cette
  décision a déplacé le signal appliqué sur le balayage hebdomadaire. Le balayage reste ce
  signal — la décision ici porte sur ce que le balayage a le droit d'affirmer, pour que le
  jour où une barre sera fixée, elle le soit à partir de quelque chose de mesuré.

## Alternatives considérées

### Corriger le budget de temps et continuer à publier un score

Considérée parce que l'inflation par timeout a une cause connue et ordinaire, et que
corriger le budget rend le nombre à nouveau défendable pour ce composant.

Rejetée comme substitut, pas comme complément : corriger un budget est une estimation à
maintenir par composant et par machine, et la seconde observation montre que le chiffre peut
être faux pour des raisons qu'aucun budget n'adresse.

La correction a ensuite été mesurée plutôt que supposée, sur un fichier de 205 mutants. Le
budget par défaut laisse 173 timeouts contre 32 kills ; dix secondes de plus ne changent
strictement rien ; trente secondes de plus transforment 112 de ces timeouts en vrais kills
et coûtent 2,8 fois le temps d'horloge. Le correctif est donc réel — la plupart de ces
mutants étaient attrapés par un test en échec et n'ont jamais pu le dire — et aucun réglage
abordable ne le délivre : la valeur qui marche projette la jambe de la bibliothèque au-delà
du plafond de son job. Découper cette jambe entre plusieurs jobs est le levier, pas un
nombre, et c'est un chantier plus grand que cette décision. C'est bien là le point :
publier les statuts ne l'attend pas.

### Compter un timeout comme un survivant plutôt que comme un kill

Considérée parce qu'elle échoue du bon côté : un mutant sans verdict est traité comme un
mutant que personne n'a attrapé, donc le nombre ne peut que sous-estimer.

Rejetée parce que c'est la même erreur de catégorie avec le signe inversé, et qu'elle
rapporterait un composant criblé de trous un jour où le runner était chargé. Un mutant qui
ne termine réellement jamais **est** détecté, et l'appeler survivant est aussi faux que
d'appeler kill un timeout provoqué par le harnais. Aucune des deux réponses n'est disponible
sans savoir lequel des deux cas s'est produit — ce que publier le statut préserve et ce
qu'un score détruit.

### Ne rien dire tant que la cause de la divergence n'est pas trouvée

Considérée parce qu'un rapport qui dit « ces nombres sont en partie inexpliqués » est
inconfortable, et que la tentation est d'attendre une histoire propre.

Rejetée : le balayage hebdomadaire publie un chiffre chaque lundi, que quelqu'un l'ait
expliqué ou non. Attendre ne suspend pas l'affirmation, ça laisse seulement la mauvaise en
place.

## Conséquences

### Positives

* Un lecteur voit ce qui a été mesuré — combien de mutants un test a attrapés, combien n'ont
  eu aucun verdict — au lieu d'un chiffre unique incapable de les distinguer.
* Le seuil de la bibliothèque cesse d'attendre le mauvais événement. Il attend un balayage
  dont les timeouts sont un résidu, ce qui est une condition vérifiable.
* Un run dominé par des non-verdicts s'annonce, au lieu de se lire comme un score parfait.

### Négatives

* Plus de nombre unique à mettre dans une courbe de tendance ou un badge. Suivre la posture
  de mutation du dépôt dans le temps demande désormais de lire un petit tableau plutôt qu'un
  chiffre.
* Deux des quatre composants n'ont jamais été mesurés dans des conditions où leur score
  voudrait dire quelque chose, donc la réponse honnête à « à quel point est-ce testé ? » est
  plus longue qu'avant.

### Risques

* **Un compte peut être sur-lu exactement comme un score l'était.** « 2 070 tués » invite à
  la même fausse précision si le lecteur oublie qu'un mutant peut être équivalent, ou que le
  périmètre du diff a changé. Les statuts sont plus honnêtes, pas auto-explicatifs.
* **La divergence inexpliquée le reste.** Cette décision fait qu'un rapport décrit fidèlement
  son propre run ; elle ne fait pas s'accorder deux runs, et rien ici ne doit se lire comme
  ayant refermé cette question.

## Actions de suivi

* Découper le balayage de la bibliothèque entre plusieurs jobs. Le budget par mutant est
  mesuré, et aucune de ses valeurs n'est à la fois efficace et abordable dans un seul job ;
  c'est le parallélisme entre runners qui permettrait au budget d'être honnête.
* Rouvrir la question du seuil de la bibliothèque une fois qu'un balayage produira un chiffre
  dont les timeouts sont un résidu.
* Garder la divergence de `Guards.cs` comme une question et non comme un incident : elle est
  reproductible dans les deux sens et l'arbitre est une mutation appliquée à la main.

## Références

* [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md) — la
  vérification à travers laquelle cette décision rapporte.
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) — la décision qui a
  déplacé le signal appliqué sur le balayage hebdomadaire.
* [ADR-0092](0092-run-every-mutation-leg-from-its-own-source-project.fr.md) — la réparation
  précédente du même instrument, et la raison pour laquelle son oracle est désormais celui
  qui est déclaré.
* [`justdummies-mutation.fr.md`](../workflows/justdummies-mutation.fr.md) — ce que chaque
  jambe exécute, ce que le résumé publie, et les mesures dont ce record argumente.
