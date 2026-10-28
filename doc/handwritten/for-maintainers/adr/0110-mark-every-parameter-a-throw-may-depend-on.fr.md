# ADR-0110 | Marquer tout paramètre dont un throw peut dépendre, et tous là où la piste se perd

🌍 🇬🇧 [English](0110-mark-every-parameter-a-throw-may-depend-on.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-28
**Accepted:** 2026-10-28
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

L'[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) fait bloquer la
compilation par `unread guards`. Cet enregistrement décide de ce qui se passe une fois la marque
posée ; ce qui la mérite se décide au §5.3, et la section Risques de l'ADR-0083 nomme ce couplage.

Aujourd'hui, une instruction qui lève une exception sous une forme que le §5.3 ne sait pas lire
marque chaque paramètre qu'elle nomme. Elle suit aussi chaque variable locale, champ ou propriété
qu'elle nomme d'exactement un pas en arrière — jusqu'à l'initialiseur d'une locale, ou jusqu'aux
affectations antérieures d'un champ ou d'une propriété — et marque les paramètres qui s'y trouvent.
La spécification justifie l'arrêt après un pas par
l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) : suivre la valeur plus
loin « serait de la propagation ».

Une garde à deux copies de son paramètre ne marque donc rien. Une garde atteinte par un argument
`out`, une déconstruction ou une propriété dont le getter calcule sa valeur non plus. Le §9 liste les
formes qui échappent à la marque, et celles-ci n'en font pas partie. Un tel constructeur reçoit le
générateur neutre de la table de base sous un récapitulatif disant `inferred`, et il rejette les
tirages que sa garde cachée refuse. L'ADR-0083 classe cette issue — un fichier qui compile et tire
une valeur que le constructeur rejette — comme la plus coûteuse.

Les problèmes d'honnêteté du générateur de squelettes sont déjà venus de cette direction. Les quatre
constats de gravité élevée de l'essai d'expérience développeur étaient des récapitulatifs disant
`inferred` sur du code que le constructeur rejette (#297 à #300). La revue de #331 a ensuite trouvé
quatre règles de la forme « cette syntaxe est inoffensive » qui ne l'étaient pas : un membre
calculé, un cast défini par l'utilisateur, un `==` surchargé et un accesseur `set` écrit à la main.
Chacune a été trouvée en lançant l'outil à la main sur un nouveau cas. L'issue #333 proposait
d'inverser le défaut pour les `throw` visibles.

Un prototype de cette inversion a été mesuré avant cet enregistrement
([#333](https://github.com/Reefact/just-dummies/issues/333#issuecomment-6467986950)). Sur les 20
domaines de l'essai — 174 types générés et 428 paramètres — le lecteur actuel n'a aucun oubli
silencieux : aucun type non marqué ne voit ses propres tirages rejetés. Suivre les dépendances d'un
`throw` à travers un nombre quelconque de copies n'y ajoute aucune marque, et marquer chaque
paramètre là où une dépendance ne peut pas être tracée non plus. La suite unitaire du moteur et le
balayage génératif complet ne changent pas davantage. Sur six sondes écrites à la main, le lecteur
actuel reste silencieux à chaque fois. Suivre les copies jusqu'au bout en ferme cinq ; la sixième,
un getter calculé, ne se ferme qu'en marquant chaque paramètre sur la valeur impossible à tracer.

L'[ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md) place une garde par
portée syntaxique et fait reposer sa justesse sur une propriété : une région qui est un sur-ensemble
ne peut qu'ajouter des refus, jamais en retirer. Sa justification pose que le sens du défaut se
décide avant toute question de précision, parce que le silence se lit « rien ici ».

L'[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) ne laisse le §5.3
changer que sur un rapport de terrain correspondant à une signature écrite, et lie un retrait autant
qu'un ajout. Il ne retient jamais un défaut de justesse : un résultat faux présenté comme inféré
porte son rapport par définition, et un défaut mesuré par le corpus et l'oracle de tirage de ce dépôt
y est recevable.

Certaines marques actuelles tombent sur des gardes que le générateur satisfait toujours : un `throw`
dans une lambda que le constructeur ne fait que stocker, `double.IsFinite`, le bras de rejet d'un
`switch` sur un enum, un nombre d'éléments au-delà du plafond de taille. L'ADR-0083 a accepté cette
famille dans ses conséquences négatives, et a rejeté la distinction entre un abandon prouvé sans
risque et un doute réel.

## Décision

Une instruction qui lève une exception marque `unread guards` sur chaque paramètre dont elle peut
dépendre — à travers un nombre quelconque de copies, et sur chaque paramètre dès qu'une valeur qui la
décide ne peut pas être ramenée jusqu'à eux — sauf si le §5.3 lit la garde ou la reconnaît comme
satisfaite par construction.

## Justification

**Lire et marquer vont dans des sens opposés.** Lire à travers une copie tirerait une borne sur un
paramètre d'une valeur calculée à partir de lui ; c'est la propagation que l'ADR-0046 refuse, et
elle reste refusée. Marquer à travers une copie ne tire rien. Cela refuse seulement de se porter
garant d'un générateur, ce qui est le refus que l'ADR-0046 demande. Suivre le doute plus loin ne
peut qu'ajouter des refus, la propriété même sur laquelle repose l'ADR-0084. La limite d'un pas a
emprunté au marquage la justification de la lecture, où elle ne s'applique pas.

**Là où la piste se perd, le défaut doit être la marque.** La poursuite s'arrête là où le moteur ne
peut pas regarder : un getter qu'il ne lit pas, une méthode appelée sur l'objet en construction. Là,
le choix est entre le silence et une marque, et le silence se lit « ce paramètre est libre ». Seule
la marque empêche un cas auquel personne n'a pensé de devenir un `inferred` faux. S'arrêter sans
rien dire au dernier pas traçable reconstruirait la liste blanche que #333 voulait quitter, un pas
plus loin. La revue de #331 a trouvé quatre fuites exactement de ce genre.

**C'est un défaut de justesse, et il entre comme tel.** Un silence au-delà d'un pas est un
générateur présenté comme inféré que le constructeur rejette. L'ADR-0085 ne retient jamais un tel
défaut. Son rapport est une forme générée qui le démontre avant le changement, soit la voie du corpus
et de l'oracle que cet enregistrement nomme.

**Le coût a été mesuré, pas supposé.** Le risque nommé par #333 est la lassitude des TODO : des
développeurs qui suppriment la ligne bloquante sans la lire videraient la marque de son sens. Sur
l'essai, le changement n'ajoute aucune marque. Le coût à la compilation que l'ADR-0083 a attaché à la
marque ne tombe que sur un constructeur qui cache réellement une garde derrière une chaîne, ou
derrière une valeur que le moteur ne sait pas suivre.

**L'effort reste borné.** Le parcours couvre la syntaxe propre du corps du constructeur et s'arrête
là où le corps s'arrête. Il ne demande aucun graphe de flot de contrôle (ADR-0084), ne suit aucun
appel (§9), et là où il devrait regarder dans un getter ou une méthode, il s'arrête et marque tout à
la place. La frontière est nommée plutôt que franchie.

**Aucune marque n'est retirée.** Retirer une marque est un rétrécissement, qui demande un rapport de
terrain (ADR-0085). Les sur-marquages qu'il toucherait — la lambda stockée, `double.IsFinite`, le
bras de rejet d'un enum, le plafond de taille — relèvent d'une famille que l'ADR-0083 a tranchée, et
y revenir est une décision à part entière.

## Alternatives considérées

### Garder la limite d'un pas

Considérée parce que l'essai n'a écrit aucune garde qu'une poursuite plus profonde marquerait, et
parce que l'ADR-0085 fait de l'immobilité le défaut en l'absence de preuve.

Rejetée parce que ce silence n'est pas un non-objectif documenté mais un récapitulatif disant
`inferred` sur un constructeur qui rejette les tirages. C'est un défaut de justesse, que l'ADR-0085
ne retient pas. Chaque fuite de ce genre a jusqu'ici été trouvée après coup, à la main.

### Suivre les copies jusqu'au bout, mais rester silencieux là où la piste se perd

Considérée parce que c'est la règle la plus précise : elle ne marque jamais un paramètre que la
valeur cachée n'implique pas réellement.

Rejetée parce qu'elle garde le silence à la frontière — le sens que l'ADR-0084 refuse. Une garde
derrière un getter calculé resterait présentée comme inférée, et la prochaine forme impossible à
tracer serait trouvée comme les quatre dernières.

### Remplacer le marquage forme par forme par cette seule règle

Considérée parce qu'une règle est plus courte que les mécanismes qu'elle engloberait, et parce
qu'elle ferait tomber un sur-marquage connu : un `throw` dans une lambda que le constructeur ne fait
que stocker.

Rejetée pour l'instant parce qu'elle retire des marques sans rapport de terrain derrière ce retrait
(ADR-0085), et parce qu'un argument tiré du nombre de lignes est celui que l'ADR-0085 consigne comme
ayant échoué face à la mesure. Elle reste ouverte à un rapport.

### Marquer chaque paramètre de tout constructeur qui lève une exception

Considérée parce que c'est la règle la plus simple, et qu'elle est juste.

Rejetée parce qu'elle bloque chaque paramètre de chaque constructeur gardé, y compris ceux qu'aucune
règle ne touche. Elle jette les propositions que le moteur a réussies, ce que l'ADR-0083 a rejeté
pour le refus équivalent du type entier.

### Demander les dépendances à l'analyse de flot de données de Roslyn

Considérée parce que le moteur demande déjà à cette analyse quelles écritures d'un paramètre
existent (ADR-0084).

Rejetée parce qu'elle indique quelles variables une région lit et écrit, pas de quoi une valeur a
été calculée. La remonter à travers chaque définition revient au même parcours, sur un modèle qui
compte moins de lecteurs.

## Conséquences

### Positives

* Une garde atteinte par une chaîne quelconque de copies, un argument `out` ou une déconstruction est
  marquée. Une garde décidée par une valeur que le moteur ne sait pas tracer marque chaque paramètre.
  Un `throw` visible dans le corps du constructeur ne reste silencieux que là où le §5.3 l'a lu ou l'a
  prouvé satisfait par construction.
* La différence entre lire et marquer est écrite, si bien que le prochain lecteur de « suivre plus
  loin serait de la propagation » sait quelle moitié elle gouverne.
* Le résidu de formes silencieuses du §9 ne garde que ce que l'outil ne voit jamais : les appels
  qu'il ne suit pas, et les validateurs qui renvoient la valeur qu'ils vérifient.

### Négatives

* Un constructeur dont le `throw` est décidé par un getter écrit à la main ou par une méthode appelée
  sur `this` bloque désormais sur chaque paramètre, y compris ceux que la règle cachée n'implique
  peut-être pas. Le développeur confirme chacun.
* Les sur-marquages connus restent tels quels, chacun coûtant toujours une ligne que le développeur
  supprime.

### Risques

* Le repli sur chaque paramètre est large. Si du code réel décide souvent un `throw` par une méthode
  d'instance — un `IsValid()` sur l'objet en construction —, la lassitude des TODO devient réelle.
  L'essai n'en a mesuré aucune. Le remède est de rétrécir ce qui compte comme impossible à tracer,
  sur rapport (ADR-0085), et non d'abandonner le repli.
* Le balayage ne juge que les formes qu'il déclare, et avant cet enregistrement il ne déclarait
  aucune chaîne plus longue qu'un pas. Les sondes le rejoignent comme formes à part entière, faute de
  quoi cette décision resterait non mesurée.

## Actions de suivi

* Ajouter les formes des sondes à la famille `placement` du balayage, démontrées en échec avant le
  changement, comme l'exige la voie du corpus et de l'oracle de l'ADR-0085.
* Consigner à part s'il faut prouver certaines gardes inoffensives par construction —
  `double.IsFinite` (ADR-0031), le bras de rejet d'un `switch` sur un enum, un nombre d'éléments
  au-delà du plafond de taille (ADR-0076). Cela revient sur l'alternative que l'ADR-0083 a rejetée,
  avec les sur-marquages de l'essai comme rapports de terrain.
* Ne retirer le sur-marquage de la lambda stockée que si un rapport le demande.

## Références

* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — ce que coûte la
  marque, et le couplage sur lequel agit cet enregistrement.
* [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.fr.md) — le sens du défaut,
  et la propriété de sur-ensemble que cet enregistrement étend au marquage.
* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) — la voie d'entrée de ce
  changement, et pourquoi rien n'est retiré.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la propagation qui
  reste refusée à la lecture.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) et
  [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — l'ensemble fermé lu, et le
  contrôle de null satisfait par construction.
* Issue [#333](https://github.com/Reefact/just-dummies/issues/333) et sa
  [mesure](https://github.com/Reefact/just-dummies/issues/333#issuecomment-6467986950) ; pull
  requests #331 et #335.
* §5.3 et §9 de la spécification.
