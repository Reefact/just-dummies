# ADR-0068 | Porter l'inspection de pool partout où l'appelant fournit les valeurs, et nulle part ailleurs

🌍 🇬🇧 [English](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-11
**Accepted:** 2026-08-11
**Decision Makers:** Reefact

## Contexte

L'[ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.fr.md) a établi l'inspection de pool et
a laissé la portée par famille ouverte, en action de suivi. Elle a d'abord atterri sur deux générateurs : le
value set de chaîne et le pool de premier niveau.

Cette portée suivait l'exemple à partir duquel la décision avait été argumentée. Le problème était posé comme
celui d'un **catalogue** — une liste trop grande pour être lue d'un coup d'œil, maintenue à la main, qui dérive
des invariants déclarés à côté d'elle — et l'exemple était un fichier de prénoms. Donc des chaînes, et les
types propres de l'appelant via le pool de premier niveau.

**Le cadrage était incomplet, et c'est l'exemple qui le faisait paraître complet.** Un catalogue se définit par
sa provenance, pas par ce qu'il contient. Un calendrier de jours de bourse, une liste de références produit,
une table de paliers tarifaires : chacun est chargé d'un fichier ou d'une table, chacun est maintenu par
quelqu'un qui n'a jamais lu le test qui y puise, et chacun dérive de ses invariants exactement comme une liste
de prénoms. Les pools de types propres à l'appelant étaient déjà traités, puisque le pool de premier niveau
porte l'inspection. Les familles typées, non.

Quatre faits supplémentaires cadrent le choix :

* **Le coût est par substrat, pas par famille.** Treize familles atteignent leur pool par un moteur ordinal,
  trois par le moteur continu, deux par le moteur large, une par le moteur décimal ; trois autres détiennent
  leur pool elles-mêmes. Dès qu'un moteur calcule la réponse, chaque famille n'ajoute que trois membres
  explicites d'une ligne. La projection depuis la monnaie privée du moteur — un ordinal, un double — existe
  déjà dans chaque famille : c'est celle dont `Generate` se sert.
* **Vingt-deux familles exposent `OneOf`.** `AnyBoolean` non : son univers est deux valeurs qui appartiennent à
  la bibliothèque. Le générateur de motif non plus, pour la raison que l'[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md)
  consigne — une contrainte de forme sur un motif supposerait de construire dans l'intersection de deux
  langages réguliers.
* **L'interface est implémentée explicitement et documentée comme optionnelle** : une famille qui ne la porte
  pas ne change rien à aucune liste de complétion. Ce n'est pas l'asymétrie qu'ADR-0033 a supprimée ; celle-là
  portait sur la surface fluide de contraintes qu'un appelant lit en écrivant.
* **« Un pool est en vigueur » et « le domaine est dénombrable » coïncidaient par accident.** Une chaîne
  construite n'annonce aucune cardinalité : sur `AnyString`, les deux questions avaient la même réponse. Sur un
  scalaire, non — `Between(1, 1_000_000)` annonce un million et n'a aucun pool.

La surface gèle à la `1.0`. Ajouter l'interface à une famille ensuite est additif ; la retirer ne l'est pas.

## Décision

L'inspection de pool est portée par tout générateur qui admet un value set fourni par l'appelant et par aucun
autre, et la présence d'un pool est déterminée par l'allow-list que l'appelant a confiée — jamais par le fait
que le domaine du générateur se trouve être dénombrable.

## Justification

**La première portée répondait à l'exemple plutôt qu'au critère.** La phrase de décision d'ADR-0067 disait déjà
*un value set fourni par l'appelant* ; c'est l'illustration qui était faite de chaînes. Appliquer le critère
tel qu'il est écrit n'est pas tant une extension de cette décision que son achèvement.

**Dès qu'un substrat calcule la réponse, toute ligne tracée à travers ses familles est arbitraire.**
L'économie faite en excluant une famille au catalogue improbable — un `ushort`, un `Half` — est nulle, puisque
le moteur derrière elle fait déjà le travail. Ce qu'une telle ligne coûterait est réel : un lecteur qui trouve
l'inspection sur `Any.Int32()` et pas sur `Any.UInt32()` n'a aucun moyen d'en déduire la règle, et à la `1.0`
cette question doit être tranchée pour toujours.

**La ligne qui reste est celle qui porte déjà partout ailleurs dans la bibliothèque : l'appelant a-t-il fourni
ces valeurs ?** C'est la question qui décide déjà si un conflit nomme un value set, si les doublons
s'effondrent, et ce qu'un rejet peut accuser. Un critère qu'un lecteur peut dériver vaut mieux qu'une table
qu'il doit mémoriser.

**L'étendue est gratuite pour le lectorat, et c'est ce qui la rend abordable.** L'inspection s'atteint par un
cast et n'apparaît dans aucune liste de complétion : la porter sur vingt-quatre types ne coûte rien à tous ceux
qui ne la demandent jamais. C'était déjà l'argument de l'implémentation explicite dans ADR-0067 ; ici, c'est ce
qui permet d'être généreux sur la portée sans le facturer à personne.

**Adosser `IsPooled` à l'allow-list garde le rapport centré sur la liste de l'appelant, et la mécanique
concorde.** Le principe : un domaine que l'appelant n'a pas fourni n'a rien de lui à auditer — rendre un
intervalle lui remettrait une plage qu'il a déjà sous les yeux. La conséquence mécanique pointe au même
endroit : une plage entière bornée est dénombrable, donc l'autre lecture compilerait puis énumérerait un
million de valeurs dont personne n'a parlé. Quand le principe et le moteur atteignent la même frontière, c'est
une conception et non une esquive.

## Alternatives envisagées

### Conserver la portée à deux familles

Envisagée parce qu'elle était déjà livrée, et parce que le cas des chaînes est celui sur lequel le besoin a été
ressenti.

Rejetée parce qu'elle répond à l'exemple au lieu du critère. Un calendrier de dates chargé d'un fichier est la
même panne qu'un fichier de prénoms, avec un autre type d'élément — et l'appelant qui la rencontre trouverait
la bibliothèque répondant pour l'un et muette pour l'autre, sans raison énonçable.

### N'étendre que là où un catalogue chargé est plausible

Sérieusement envisagée : la plausibilité diffère réellement. Les dates, les entiers larges, `decimal` et `Guid`
portent de vrais catalogues ; un `ushort` ou un `Half`, non.

Rejetée parce que la plausibilité est un mauvais critère de découpe dès lors que le coût est par substrat.
L'économie est nulle, et la ligne qu'elle trace est indérivable pour un lecteur — *pourquoi `Int32` et pas
`UInt32` ?* n'a pas de réponse qui survive à être posée deux fois.

### Exclure le générateur d'enum

Envisagée parce que l'univers d'un enum est sa propre déclaration, jamais un fichier : l'argument du catalogue
ne l'atteint pas du tout.

Rejetée parce que son `OneOf` reste un sous-ensemble fourni par l'appelant, ce qui est exactement le critère.
C'est aussi la moins chère des vingt-deux à porter — son pool est une simple liste — donc l'exclure
n'achèterait rien et coûterait une phrase de documentation plus difficile à justifier que son inclusion.

### Répondre sur la cardinalité plutôt que sur l'allow-list

Envisagée parce que chacune de ces familles annonce déjà une cardinalité distincte pour l'arbitrage des
collections distinctes : l'inspection aurait pu être dérivée d'un état déjà présent.

Rejetée parce qu'elle répond à une autre question. La cardinalité dit combien de valeurs le générateur peut
produire ; l'inspection dit ce que sont devenues les valeurs que l'appelant a fournies. Les confondre
signalerait un intervalle borné comme un pool et tenterait de l'énumérer.

### La mettre sur tous les générateurs, avec un rapport vide là où il n'y a pas de pool

Envisagée pour l'uniformité : le cast réussirait toujours et l'appelant n'aurait jamais à le tester.

Rejetée parce qu'un générateur sans rien à rendre annoncerait une inspection qui ne dit jamais rien, en
reportant tout le sens sur `IsPooled`. Ne pas porter l'interface est l'énoncé le plus clair, et cela laisse le
cast être lui-même la question.

## Conséquences

### Positives

* Un catalogue est traité quel que soit son type d'élément, ce qui est l'objet de la fonctionnalité une fois le
  cadrage corrigé.
* La portée se dérive d'une question au lieu de se mémoriser sur une liste, et cette même question gouverne
  déjà le reste du comportement des value sets.
* Une convention par réflexion la tient : un générateur qui gagne `OneOf` sans l'inspection fait échouer un
  test au lieu de livrer une asymétrie.

### Négatives

* Vingt-quatre types publics portent désormais une interface qui gèle à la `1.0`.
* La forme déclarations-et-coupables existe maintenant dans cinq moteurs. Ils doivent rester en phase, et rien
  d'autre que la revue ne le dit — la duplication est ce qui permet à chaque moteur de juger dans sa monnaie.

### Risques

* **La projection vers le type de l'appelant est par famille.** Une conversion inverse fautive se manifesterait
  par une valeur mal rendue plutôt que par un tirage faux, ce qui est une panne plus discrète que celles du
  générateur lui-même.
* **Le critère est un jugement aux bords.** *L'appelant a fourni ces valeurs* est clair pour `OneOf` et pour le
  pool de premier niveau ; un futur générateur dont le domaine serait pour partie fourni et pour partie
  construit demanderait une décision que cet enregistrement ne prend pas.

## Actions de suivi

* Examiner si JD029, qui signale une valeur de pool qu'aucun tirage ne peut rendre, doit s'étendre des value
  sets de chaîne aux pools scalaires désormais couverts à l'exécution — le cas écrit à l'appel, qu'elle sait
  voir, est précisément là où vivent les pools scalaires.

## Références

* [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.fr.md) — l'inspection dont cet
  enregistrement fixe la portée, et l'action de suivi qu'il referme.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — l'asymétrie dont
  celui-ci se distingue, et la raison pour laquelle un motif ne porte pas de value set.
* [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.fr.md) — l'espace ordinal que traverse la
  projection vers le type de l'appelant.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la frontière d'ambition à
  laquelle une portée élargie est mesurée.
