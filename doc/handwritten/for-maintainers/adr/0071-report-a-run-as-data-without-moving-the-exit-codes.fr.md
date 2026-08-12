# ADR-0071 | Rendre compte d'une exécution en données sans déplacer les codes de sortie

🌍 🇬🇧 [English](0071-report-a-run-as-data-without-moving-the-exit-codes.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-12
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le §7 fait d'un fichier écrit avec des paramètres ouverts un **succès** : l'écriture a réussi, et le
build du développeur signale le reste, ce qui est le mécanisme que consigne
[l'ADR-0060](0060-seed-generators-from-constructor-guards.fr.md). Le code `0` se lit donc pareil que
tous les paramètres aient résolu ou qu'un tiers d'entre eux non.

Une seule invocation prend plusieurs arguments de type, traités indépendamment, et sort avec le pire
d'entre eux (§7). Un appelant qui scaffolde quarante types en une commande n'a qu'un nombre pour
l'ensemble.

La surface publique de l'outil est sa ligne de commande, et il a publié une version,
`cli-v1.0.0-beta.1`. Les codes de sortie du §7 font partie de cette surface : un script les lit déjà.

Le moteur retourne son modèle de résultat et la CLI le rend ; le récapitulatif du §6 est un rendu, et
la spécification dit en toutes lettres que la provenance est une donnée, pas une sortie.

`--dry-run` dépense déjà stdout : le récapitulatif va sur stderr et le fichier sur stdout, de sorte
que l'un peut être redirigé pendant que l'autre est lu (§6).

La régénération et la détection de dérive sont abandonnées, non reportées (§16), donc rien d'autre
dans l'outil ne rend compte d'un arbre de travail.

## Décision

Une exécution rend compte d'elle-même en un unique document JSON sur stdout quand `--format json` le
demande, portant les faits que le code de sortie ne peut pas porter, et les codes de sortie du §7
gardent le sens avec lequel ils ont été publiés.

## Justification

**Le fait manquant a une forme, et ce n'est pas un code de sortie.** Ce qu'un bootstrap scripté a
besoin de savoir, c'est *combien de paramètres sont restés ouverts*, par type et pour l'exécution —
un nombre, pas un verdict. L'exprimer en code de sortie supposerait soit de surcharger `0` d'un
second sens, soit d'en frapper un troisième, et les deux réécrivent un contrat déjà livré. Ajouter un
canal ne coûte rien de publié et répond exactement à la question.

**Refuser de déplacer les codes de sortie est le fond, pas une précaution.** Un outil qui
redéfinirait discrètement le succès casserait les scripts qui le lisaient correctement, et il les
casserait en silence — le pire mode de défaillance d'une livraison. Le rapport est additif pour la
même raison que le point d'entrée de
[l'ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md) l'est : le comportement
par défaut doit continuer de vouloir dire ce qu'il voulait dire.

**Un rendu, deux publics, un seul ensemble de faits.** Le moteur retourne déjà le modèle et la
console le rend déjà, donc le rapport est un second rendu et non une seconde source de vérité. Les
mots de provenance viennent de la table du récapitulatif pour exactement cette raison : deux tables
dériveraient, et un script et un lecteur finiraient en désaccord sur la même exécution.

**stdout est le canal machine, et il doit être propre.** Le récapitulatif y est supprimé sous `json`,
et tout ce qui est écrit pour une personne continue d'aller sur stderr, de sorte que `2>/dev/null`
laisse un tuyau analysable. `--dry-run` n'a alors plus où mettre le fichier qu'il aurait affiché,
donc le texte voyage dans le document — le perdre rendrait les deux options exclusives sans raison
qu'un appelant puisse traiter.

**Un contrat total vaut mieux qu'un contrat plus court.** Une exécution qui s'arrête avant son premier
scaffold produit un document elle aussi, nommant le refus. L'alternative — n'écrire rien — force
chaque consommateur à distinguer une sortie vide d'une analyse en échec avant même de pouvoir
regarder l'exécution, ce qui est un trou dans le contrat déguisé en concision.

## Alternatives envisagées

##### Un nouveau code de sortie pour « écrit, mais incomplet »

Envisagé parce qu'il ne demande aucune analyse, et qu'un script branche déjà sur le code de sortie.

Rejeté parce qu'il change le sens d'un contrat publié. Un appelant qui lit `0` aujourd'hui verrait
apparaître le nouveau code pour des exécutions qu'il traitait comme réussies, et l'outil l'aurait
cassé sans le dire. Il ne porte en outre qu'un bit là où la réponse utile est un décompte et une
liste.

##### Faire d'un paramètre ouvert un échec

Envisagé parce que le code de sortie répondrait alors directement à la question.

Rejeté parce que cela contredit l'ADR-0060 : le paramètre ouvert *est* le mécanisme, le fichier est
écrit exprès, et le build du développeur est là où cela doit être signalé. En faire un échec ferait
refuser à l'outil le cas même qu'il a été conçu pour remettre entre les mains du développeur.

##### Rendre compte par un fichier plutôt que par stdout

Envisagé parce qu'un fichier survit à un tuyau et se relit après coup.

Rejeté parce que cela fait écrire à l'outil quelque chose que personne n'a demandé, dans un endroit
qu'il devrait inventer, et le laisse traîner à l'exécution suivante. stdout est déjà le canal que
l'appelant a choisi en lançant la commande.

## Conséquences

**Positives.** Un bootstrap scripté sur de nombreux types peut distinguer une exécution complète
d'une incomplète, et dire quels paramètres sont restés ouverts, sans analyser de la prose. Rien de
déjà publié ne change de sens. Le récapitulatif et le rapport ne peuvent pas diverger, puisqu'ils
lisent une seule table.

**Négatives.** L'outil a désormais deux contrats de sortie à tenir, et les noms de clés du document
en font partie — renommer une clé est une rupture pour un appelant, même si aucun type d'aucun
assembly n'a bougé. `--dry-run` se comporte différemment sous chaque format, ce qui est une chose de
plus à savoir.

**Risques.** Un consommateur peut en venir à dépendre d'une clé que la spécification ne décrit pas.
Atténué par le fait que le document est petit, plat, et écrit noir sur blanc au §6.1 plutôt que laissé
à découvrir depuis un échantillon.

## Actions de suivi

* Aucune. Le manque que ceci comble était l'action de suivi consignée sur
  [l'ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).

## Références

* §3, §6, §6.1, §7, §10.3, §16 de la spécification.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md),
  [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).
