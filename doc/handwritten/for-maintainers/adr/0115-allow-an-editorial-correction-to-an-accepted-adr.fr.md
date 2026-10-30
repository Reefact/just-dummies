# ADR-0115 | Permettre une correction éditoriale d'une ADR acceptée, jamais un changement de fond

🌍 🇬🇧 [English](0115-allow-an-editorial-correction-to-an-accepted-adr.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-30
**Decision Makers:** Reefact

## Contexte

La base de décisions traite un enregistrement accepté comme une pièce d'histoire et non comme un brouillon. Son
README dit que le corps d'un enregistrement superséedé n'est jamais retouché, et que la citation d'un enregistrement
superséedé ne se repinne pas, sauf si ce qu'elle affirme a cessé d'être vrai. Il ne nomme aucune exception : pris à
la lettre, corriger une coquille ou un lien mort dans un enregistrement accepté est interdit aussi.

Un enregistrement continue d'être lu longtemps après son acceptation, et les parties qui ne portent aucune décision
se dégradent : un lien dont la cible a bougé, une coquille, un jumeau français qui a dérivé de l'anglais, la citation
d'un enregistrement superséedé depuis. L'ADR-0080 consigne le coût du dernier cas parmi ses conséquences négatives :
« un lecteur qui suit un lien depuis JD031 arrive sur un enregistrement dont le successeur porte le texte en
vigueur … le détour est réel ».

La même question atteint la documentation utilisateur. L'issue #329 a trouvé une page utilisateur qui citait
l'ADR-0029 pour une règle que l'ADR-0076 avait remplacée depuis. Cinq pages utilisateur citent des enregistrements
superséedés, pour des affirmations toujours vraies, et les repinner contredirait la règle du README ; les laisser
envoie un utilisateur, qui lit pour connaître le comportement en vigueur, dans le détour que regrette l'ADR-0080.

Ce que dit un enregistrement est fixé par la date de son acceptation : son contexte décrit le monde à cette date, et
sa décision comme sa justification répondent à ce monde.

## Decision

Un enregistrement accepté peut recevoir une correction éditoriale — une correction après laquelle l'enregistrement,
lu à la date de son acceptation, dit exactement ce qu'il disait avant — et jamais un changement de fond, qui passe par
un nouvel enregistrement.

## Rationale

**Le critère protège ce à quoi sert l'histoire.** On garde un enregistrement pour expliquer pourquoi une décision
était juste quand elle a été prise. Une coquille corrigée, un lien déplacé réparé ou une phrase française réalignée
sur l'anglais canonique laisse cette explication intacte ; une phrase de contexte rendue « plus juste aujourd'hui »
la réécrit. Se demander si l'enregistrement dit la même chose à sa propre date sépare les deux sans liste à tenir.

**Une citation se garde et s'annote, elle ne se remplace jamais.** Remplacer la citation d'un enregistrement
superséedé par son successeur ferait entrer dans le contexte un enregistrement qui n'existait pas à cette date, ce
qui change ce que dit le contexte. Écrire le successeur à côté ajoute un pointeur et ne change rien de ce que
l'enregistrement affirme : le détour que nomme l'ADR-0080 disparaît sans toucher à l'histoire.

**Les pages utilisateur suivent la même règle, pour la même raison.** Une page utilisateur qui cite un
enregistrement superséedé garde la citation, qui peut porter une part de l'affirmation que le successeur ne reprend
pas, et nomme le successeur sur la même ligne, si bien qu'un utilisateur atteint le texte en vigueur en une étape.
L'appariement est mécanique : la suite de documentation peut le faire respecter.

**L'historique git garde chaque retouche visible.** Une correction éditoriale est un commit comme un autre : rien
d'un enregistrement ne change sans être vu.

## Alternatives Considered

### Garder l'enregistrement intact, sans exception

Envisagé parce que c'est la règle actuelle et la plus simple à énoncer. Écarté parce qu'elle interdit des réparations
qui ne changent rien à ce que dit un enregistrement, et laisse en place, pour chaque lecteur, le détour que regrette
l'ADR-0080.

### Repinner une citation vers le successeur

Envisagé parce qu'il envoie le lecteur droit au texte en vigueur. Écarté parce qu'un successeur peut ne reprendre
qu'une partie de ce que l'original décidait, et parce que, dans le contexte d'un enregistrement, il décrirait un monde
qui n'existait pas encore.

### Lister les retouches permises

Envisagé parce qu'une liste s'applique facilement. Écarté comme règle en soi : une liste vieillit et invite à y faire
entrer un nouveau cas par l'argumentation. Le critère tranche ; le README en donne des exemples.

## Consequences

### Positive

* Un lien mort, une coquille ou un jumeau qui a dérivé dans un enregistrement accepté se corrige sans nouvel
  enregistrement.
* Le lecteur d'une page utilisateur, ou d'un enregistrement citant un enregistrement superséedé, voit l'enregistrement
  en vigueur en une étape.

### Negative

* « Dit la même chose » est un jugement, porté par qui retouche et vérifié en revue.

### Risks

* Une retouche présentée comme éditoriale peut déplacer un sens. Parade : le critère est énoncé avec la règle, le
  commit montre la retouche, et le mainteneur relit tout changement de la base de décisions.

## Follow-up Actions

* Énoncer le critère et la règle de citation dans le README de la base de décisions (EN et FR).
* Nommer le successeur à côté de chaque citation d'un enregistrement superséedé dans la documentation utilisateur, et
  le faire respecter par la suite de documentation.
* Annoter les citations d'enregistrements superséedés à l'intérieur d'autres enregistrements est désormais permis ;
  c'est laissé à un changement ultérieur.

## References

* [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md) — une ligne datée par état, jamais écrasée.
* [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.fr.md) — le détour que coûte une citation superséedée.
* Issue [#329](https://github.com/Reefact/just-dummies/issues/329).
