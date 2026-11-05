# ADR-0134 | Dire à qui ouvre une portée de graine si l'on en a tiré

🌍 🇬🇧 [English](0134-tell-the-opener-of-a-seed-scope-whether-it-was-drawn-from.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-05
**Accepted:** 2026-11-05
**Decision Makers:** Reefact

## Contexte

L'[ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.fr.md) a ouvert la portée de graine ambiante aux adaptateurs
de framework de test sous la forme d'une poignée publique et libérable, `Any.UseSeed(...)`, typée `IDisposable`. Qui
ouvre la portée se charge de dire au lecteur quelle graine rejouer, et l'adaptateur xUnit le fait quand un test échoue.

La règle de l'adaptateur est de ne jamais nommer une graine qu'aucun tirage n'a utilisée : un niveau extérieur supplanté
par un niveau plus précis n'écrit rien, puisque rejouer sa graine ne reproduirait rien. Le troisième essai UX
développeur a rencontré le cas que cette règle n'atteignait pas, consigné dans dix-sept constats (XU-1) : un test qui
échouait sans rien tirer, sur un littéral, recevait quand même une graine, et partait chercher une valeur tirée qui
n'existait pas. Sous `[assembly: Reproducible]`, un corps enveloppé dans `Any.Reproducibly(...)` écrivait deux lignes
de graine, dont l'une nommait la graine de l'attribut, que rien n'avait utilisée.

La bibliothèque connaît déjà la réponse. Chaque source à graine compte ses tirages primitifs, la quantité que le golden
master des graines épingle ([ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md)). La poignée que
renvoie `UseSeed` ne l'expose pas, et `Any.Reproducibly` rapportait lui aussi la graine sur tout échec.

`UseSeed` n'est sorti dans aucune version stable : ses signatures sont encore dans la base de référence de l'API
publique non livrée.

## Décision

`Any.UseSeed(...)` renvoie une poignée publique `SeedScope`, toujours libérable, qui dit aussi si une valeur a été tirée
de sa graine (`HasDrawn`) ; l'adaptateur xUnit et `Any.Reproducibly` ne rapportent une graine que dans ce cas.

## Justification

**La règle déjà énoncée, appliquée au cas qu'elle manquait.** Une graine est nommée pour que la rejouer reproduise
l'échec. Une graine dont rien n'a été tiré ne reproduit rien, qu'un niveau plus précis ait pris les tirages ou que le
test n'ait jamais tiré ; le second cas relève de la règle du premier, pas d'une nouvelle.

**Sur la poignée que l'ADR-0017 a rendue publique, pour que tout adaptateur l'ait.** Le point d'entrée a été ouvert
pour qu'un adaptateur pour n'importe quel framework puisse s'écrire sur la surface publique. La réponse a sa place sur ce
point d'entrée plutôt que derrière un accès accordé à un seul paquet.

**Un oui ou un non, pas un compte.** Un nombre de tirages primitifs n'est pas un nombre de valeurs — une chaîne en
consomme plusieurs — et le publier ferait du motif de tirage un contrat. La seule question d'un adaptateur est de savoir
si rejouer la graine reproduirait quelque chose.

## Alternatives envisagées

### Donner à `JustDummies.Xunit` l'accès aux internes de la bibliothèque

Envisagée parce qu'elle n'ajoute aucune surface publique avant le gel de la 1.0. Rejetée : elle ne sert que
l'adaptateur maison, ce qui contredit la raison pour laquelle l'ADR-0017 a rendu la portée publique, et un membre
interne lu d'un paquet à l'autre lie l'adaptateur à une version exacte de la bibliothèque là où les trains sont
indépendants ([ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md)).

### Garder le comportement et le documenter

Envisagée parce que le cas est de faible gravité. Rejetée : dix-sept constats consignent la ligne lue comme désignant une
valeur tirée, et la correction tient en une propriété en lecture seule sur une poignée qui existe déjà.

### Exposer le nombre de tirages

Envisagée parce que le compte est ce que la source détient. Rejetée pour la raison donnée plus haut : elle publierait le
motif de tirage, dont aucun appelant n'a besoin.

## Conséquences

### Positives

* Un test qui a échoué sans tirer ne reçoit plus de graine, ni de l'adaptateur ni d'`Any.Reproducibly`.
* Sous un attribut au niveau de l'assembly, un corps enveloppé dans `Any.Reproducibly(...)` n'écrit que la graine dont
  il a tiré.
* Tout adaptateur peut appliquer la même règle par la poignée publique.

### Négatives

* Un type et un membre publics de plus à maintenir après la 1.0.
* Le type de retour d'`UseSeed` passe d'`IDisposable` à `SeedScope` : le code source écrit pour lui compile toujours,
  mais du code déjà compilé contre une préversion doit être recompilé.

### Risques

* Les tirages faits hors du flux de la portée — un thread lancé sans le contexte d'exécution, un contexte explicite
  `Any.WithSeed(...)` — ne comptent pas, si bien qu'un test qui n'a tiré que là ne reçoit pas la graine. Rejouer cette
  graine ne reproduirait pas non plus ces tirages : le silence est la bonne réponse.

## Actions de suivi

* Aucune au-delà de l'implémentation et de sa documentation.

## Références

* [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.fr.md) — la décision que celle-ci amende. Elle garde son
  statut ; la poignée qu'elle a rendue publique dit désormais aussi si sa graine a servi à un tirage.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) — le compte de tirages que celle-ci lit.
* [Essai UX 3, XU-1](../../../../tools/ux-trial/REPORT-3.fr.md#xu-1).
* Issue [#440](https://github.com/Reefact/just-dummies/issues/440).
