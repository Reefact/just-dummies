# ADR-0069 | Répondre à une borne de cardinalité sous le comparateur qui s'en servira

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.md)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-12
**Decision Makers:** Reefact

## Contexte

Une collection distincte refuse un compte impossible **à la déclaration**, avant tout tirage : demander cinq
valeurs deux à deux distinctes à un générateur qui n'en produit que trois est une contradiction que l'appelant
doit entendre tout de suite, et non une fois le budget de retirage épuisé. Pour cela, la collection demande au
générateur d'éléments une borne sur le nombre de valeurs distinctes qu'il peut produire — l'indice de
cardinalité — et la compare au compte demandé.

Une collection distincte peut aussi porter **son propre** comparateur d'égalité. La distinction se juge alors
sous ce comparateur, et non sous `EqualityComparer<T>.Default`, et les deux peuvent être en désaccord sur le
nombre de valeurs que contient un ensemble.

Le raisonnement consigné avec l'indice tenait que la borne y survit : une borne est une borne supérieure, et
aucun comparateur ne peut faire produire à un générateur plus de valeurs distinctes qu'il n'en a. Ce
raisonnement repose sur une prémisse implicite — que le comparateur par défaut est l'égalité la **plus fine**
que le type admette. Sous cette prémisse, un comparateur ne peut que fusionner des valeurs, jamais les scinder,
et une fusion ne peut que faire baisser le compte.

La prémisse tombe pour les types dont la BCL définit l'égalité **plus grossière que leur propre
représentation** :

* `DateTimeOffset.Equals` compare l'instant et ignore l'offset. Deux graphies d'un même instant sont égales et
  ont le même hachage ; `EqualsExact` existe précisément pour les distinguer à nouveau.
* `DateTime.Equals` compare les ticks et ignore `Kind`.
* L'égalité de `decimal` ignore l'échelle — `1.0m` et `1.00m` sont égaux et s'affichent différemment.

Pour un tel type, un comparateur peut **scinder** une valeur en plusieurs, et une borne comptée sous l'égalité
par défaut n'est plus une borne supérieure de ce qu'un comparateur plus fin verra.

Un générateur de cette bibliothèque atteint cet état en pratique. `DummyDateTimeOffset` admet une plage d'offsets
déclarée et y tire une minute, si bien qu'un instant unique revient sous n'importe laquelle des graphies que
cette plage autorise. Compté en instants, le domaine vaut une valeur ; sous un comparateur bâti sur
`EqualsExact`, il en vaut autant que la plage compte de minutes. Le contrôle anticipé refusait un compte de
trois face à une borne de un, sur une spécification pour laquelle plusieurs centaines de graphies distinctes
étaient tirables — un refus faux, produit par le mécanisme dont la raison d'être est d'énoncer honnêtement les
contradictions.

La condition est étroite : il lui faut à la fois un type à égalité grossière **et** un générateur qui tire une
plage sur la dimension que l'égalité par défaut efface. `DummyDateTime` tire toujours un seul `Kind`, et l'échelle
d'un décimal est fixée à une valeur unique plutôt qu'à une plage : aucun des deux ne l'atteint. Vingt-cinq des
vingt-six générateurs porteurs d'un indice de cardinalité ne sont pas concernés.

Les deux membres de l'indice sont interrogés à des moments différents. La borne est demandée à la création de
la collection ; le comparateur peut n'être déclaré qu'ensuite, sur un appel plus tardif de la chaîne.

## Décision

Une borne de cardinalité est donnée sous l'égalité avec laquelle la collection dédupliquera réellement, et un
générateur dont un comparateur plus fin peut dépasser la borne déclare ce fait dans son type plutôt que de le
laisser à la connaissance de qui interroge l'indice.

## Justification

Le contrôle anticipé existe pour transformer une spécification impossible en un refus immédiat et nommé. Le
refus qu'il produit est donc lu comme faisant autorité — il arrive chiffré, avant tout tirage, dans la voix que
la bibliothèque emploie pour les vraies contradictions. Cette autorité est précisément ce qui rend un refus
**faux** pire qu'un échec tardif : il refuse une spécification que l'appelant peut satisfaire, et le chiffre
qu'il cite l'invite à croire son domaine plus petit qu'il n'est. Une borne susceptible d'être fausse dans le
sens du refus est pire qu'une absence de borne, car l'absence de borne se contente de renvoyer au tirage-dédup
borné, qui n'échoue que lorsque la pénurie est réelle.

Répondre sous le comparateur en vigueur est ce qui fait que la borne signifie ce que le contrôle lui fait dire.
Celui-ci compare un nombre de valeurs-distinctes-sous-l'égalité-de-la-collection à une borne mesurée sous une
égalité possiblement différente ; faire parler les deux côtés de la même égalité est le minimum pour que la
comparaison ait un sens.

Déclarer la condition dans le type, plutôt que de faire reconnaître les générateurs concernés par le
consommateur de l'indice, garde la connaissance là où le fait réside. Qu'un comparateur plus fin puisse scinder
le domaine d'un générateur dépend de ce que ce générateur tire et de l'égalité que définit son type d'éléments —
deux choses que la collection ne voit pas. Un générateur, lui, connaît les deux.

La condition est énoncée comme une propriété du générateur et non du type, parce que le type seul ne tranche
pas : le même générateur de `DateTimeOffset` garde une borne saine quand ses valeurs viennent d'un pool fourni,
où un instant donne une graphie, et ne la perd que lorsqu'une plage d'offsets déclarée laisse le tirage choisir
parmi les graphies. Abandonner le contrôle anticipé pour le type entier sacrifierait des refus corrects dans des
cas qui n'ont jamais eu le défaut.

Refuser de compter — plutôt que compter des graphies au lieu de valeurs — est la réponse honnête là où la
scission est réelle. Une borne comptée sous un comparateur plus fin serait fausse dans l'autre sens pour un
comparateur plus **grossier**, et rien ne distingue un comparateur plus fin d'un plus grossier sans le sonder.
Refuser de répondre coûte un refus anticipé et renvoie au tirage-dédup borné, qui rend compte fidèlement d'une
pénurie réelle ; répondre par une supposition garderait le refus anticipé en le rendant peu fiable. C'est la
limite que trace [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md), appliquée à une
affirmation plutôt qu'à un tirage : borner ce qui est tenté, et refuser au bord plutôt que d'y paraître réussir.

Redemander la borne à chaque reconstruction de la collection, au lieu de reporter la première réponse, découle
de ce que le comparateur arrive après la première demande. Une valeur capturée avant que soient connues toutes
les dimensions qui la déterminent n'est pas cette valeur : c'est la réponse à une question antérieure. La même
forme a produit un défaut sans rapport dans les pools date-offset, et le même remède s'applique : ne rien
retenir qu'une déclaration ultérieure puisse invalider.

## Alternatives envisagées

### Intégrer la condition à l'indice de cardinalité comme second membre

Chaque générateur porteur d'une borne dirait si celle-ci survit à un comparateur plus fin, et le compilateur les
y tiendrait tous. Cela rejoint l'argument déjà consigné pour avoir mis la borne et le test d'appartenance sur une
même interface : une paire tenue par le compilateur ne peut pas dériver.

Rejetée par proportion. La réponse est identique pour vingt-cinq des vingt-six générateurs concernés, et
vingt-cinq redites l'enseveliraient sous le bruit celui qui diffère — l'inverse de ce à quoi sert une
déclaration tenue par le compilateur. La dérive ainsi évitée est réelle mais étroite : il y faut un futur
générateur sur un type à égalité grossière, tirant une plage sur la dimension effacée, dont l'auteur néglige la
condition. Ce risque est accepté et nommé dans l'interface plutôt que payé par du bruit sur tous les autres
sites d'implémentation.

### Abandonner le contrôle anticipé dès qu'un comparateur personnalisé est porté

La collection ignorerait entièrement la borne dès qu'un comparateur est déclaré, pour tous les générateurs.

Rejetée comme trop large. Elle sacrifierait un refus anticipé correct dans tous les cas où le comparateur est
plus grossier, ou où la borne du générateur n'a jamais été en danger — c'est-à-dire presque tous : ce serait
échanger un refus faux étroit contre une perte étendue du diagnostic que le contrôle existe pour fournir.

### Compter des graphies plutôt que des valeurs

Le générateur concerné annoncerait le nombre de graphies distinctes que ses contraintes autorisent, de sorte
qu'un comparateur sensible à la graphie trouverait la borne saine.

Rejetée parce qu'elle déplace l'erreur au lieu de la supprimer. Cette borne est un sur-comptage sous le
comparateur par défaut et sous tout comparateur plus grossier, ce qui laisserait passer un compte impossible et
échouerait plus tard, pendant le tirage. Rien ne distingue les deux sens sans inspecter un comparateur que la
bibliothèque ne peut pas inspecter.

## Conséquences

### Positives

* Une spécification que le comparateur de l'appelant rend satisfiable n'est plus refusée à la déclaration.
* Le refus anticipé est préservé partout où il était correct, y compris sous un comparateur personnalisé, et y
  compris pour le générateur concerné quand ses valeurs viennent d'un pool fourni.
* Le raisonnement qui a failli est consigné là où il se lit — à l'interface — plutôt que de survivre en
  commentaire qu'un changement futur croirait de nouveau.
* La borne et l'égalité qui s'en sert sont désormais demandées au même moment, ce qui supprime une classe de
  défaut où une déclaration ultérieure invalide une réponse antérieure.

### Négatives

* Un générateur sur un type à égalité grossière tirant une plage sur la dimension effacée abandonne son refus
  anticipé sous comparateur personnalisé, et retombe sur le tirage-dédup borné. Ce repli signale une pénurie
  réelle pendant le tirage plutôt qu'à la déclaration : message plus tardif et moins précis.
* Deux interfaces décrivent maintenant la cardinalité là où une seule le faisait, et un lecteur doit savoir
  laquelle s'applique.

### Risques

* Un futur générateur remplissant la même condition pourrait porter une borne et omettre la déclaration,
  réintroduisant le refus faux pour son type. Rien de mécanique ne l'en empêche ; la condition est nommée à
  l'interface, et le property test associé couvre le seul générateur connu pour la remplir.
* Un comparateur qui n'est ni strictement plus fin ni strictement plus grossier que celui par défaut est traité
  comme plus fin, ce qui est sans danger pour les refus mais abandonne un contrôle anticipé qui aurait pu être
  sain.

## Actions de suivi

* Aucune. La décision est mise en œuvre pour le seul générateur qui remplit la condition, et aucune autre
  famille de la bibliothèque ne la remplit aujourd'hui.

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner ce qui est tenté, et
  refuser au bord plutôt que d'y paraître réussir.
* Pull request [#75](https://github.com/Reefact/just-dummies/pull/75) — l'implémentation dont cette décision est
  tirée.
