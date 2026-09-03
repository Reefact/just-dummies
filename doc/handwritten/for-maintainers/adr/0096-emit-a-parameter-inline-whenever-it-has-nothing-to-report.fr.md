# ADR-0096 | Émettre un paramètre en ligne dès qu'il n'a rien à rapporter

🌍 🇬🇧 [English](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-03
**Accepted:** 2026-09-03
**Decision Makers:** Reefact

> Les références de section (§N) pointent dans la [spécification `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le constructeur émis du §4.2 nomme le generator de chaque paramètre par un appel. Jusqu'ici, cet
appel s'écrivait de deux façons : en ligne, directement dans l'initialiseur, pour un paramètre
composé — un seul appel au generator que son type possède, et rien d'autre à dire (ADR-0089) — et
partout ailleurs, une méthode statique privée que l'initialiseur appelle par son nom, que le corps
de cette méthode ait ou non quoi que ce soit à ajouter au tirage propre de la table de base.

La factory d'un paramètre primitif sans guard est vide en tout ce qui compte : `private static
IDummy<OrderStatus> AnyValidStatus() { return Any.Enum<OrderStatus>(); }` retourne exactement l'appel
propre de la table de base, resserré par rien, ne bloquant rien. La justification même d'ADR-0089
pour le cas composé — « une méthode l'enveloppant ne dirait rien que l'appel ne dit déjà » —
décrit ce paramètre mot pour mot, mais la règle dans laquelle elle a été écrite ne posait la
question que pour les paramètres composés.

L'écart devient net dès qu'un paramètre composé peut lui aussi porter un guard qui s'avère
n'ajouter rien (ADR-0095) : avant ce correctif, un paramètre composé gardé seulement par un
null-check était routé vers une factory uniquement pour y loger un marqueur de vérification qui,
une fois résolu, laissait `private static IDummy<OrderReference> AnyValidReference() { return new
AnyOrderReference(); }` — une méthode enveloppant un seul appel, indiscernable dans sa forme de la
factory du paramètre primitif sans guard juste à côté, toutes deux ne disant rien que leur propre
appel ne dit déjà.

## Décision

Un paramètre s'écrit en ligne — sans méthode de factory — dès qu'il est résolu, ne nécessite
aucune vérification, et qu'aucun guard n'a été combiné dans sa chaîne, que son unique appel
compose via le generator propre de son type ou lise directement la table de base.

## Justification

Une méthode de factory ne mérite sa place que s'il y a quelque chose pour elle à porter : un
guard que le constructeur a déclaré et que le lecteur a resserré dans la chaîne, ou l'un des deux
marqueurs qui bloquent la compilation (§5.5, §5.6). Un paramètre ne portant ni l'un ni l'autre est
un seul appel et rien de plus, et une méthode enveloppant un seul appel qui ne dit rien que
l'appel lui-même ne dit déjà est une décoration — le même argument qu'ADR-0089 avait déjà fait
pour le cas composé, généralisé à la question qu'elle n'avait jamais posée pour un primitif.

Cette généralisation retire une asymétrie plutôt qu'elle n'introduit une nouvelle distinction :
avant elle, deux paramètres de forme identique — résolus, sans guard, l'un composé et l'autre
non — s'émettaient différemment pour une raison qui avait cessé de porter sur quoi que ce soit que
l'un ou l'autre portait. La règle répond désormais à une seule question, « ce paramètre a-t-il
quelque chose à dire », plutôt qu'à deux, « ce paramètre est-il composé » puis, séparément,
« a-t-il quelque chose à dire ».

Rien ne change au §5.5 ni au §5.6 : un paramètre reçoit toujours une factory dès que l'un des deux
le bloque, composé ou non, et les mots du récapitulatif eux-mêmes (`guard`, `unread guards`,
`constraint unavailable`, …) rapportent exactement ce qu'ils rapportaient — cette décision porte
sur l'endroit où une recette résolue et non bloquée s'écrit, jamais sur ce que l'outil lit ou
rapporte.

## Alternatives envisagées

### Garder la règle limitée aux paramètres composés, et traiter la factory enveloppant du rien comme un bruit acceptable

Envisagée parce qu'elle change le moins de code, et qu'une factory retournant son propre appel de
base est légale, compile, et ne lève aucune règle propre à la bibliothèque.

Rejetée parce que « bruit acceptable » n'est pas un coût fixe : ADR-0095 était sur le point de le
rendre visible — le null-check d'un paramètre composé, une fois reconnu comme satisfait, aurait
sinon continué à passer par une factory sans qu'aucun lecteur n'en trouve la raison, juste à côté
d'un primitif réellement sans guard portant la même factory silencieuse. Deux défauts de même
forme, corrigés une fois en généralisant plutôt que deux fois en traitant chaque cas à part.

### Garder chaque primitif en factory, et donner un second chemin en ligne seulement au paramètre composé-et-propre

Envisagée parce que c'est le changement le plus petit, le plus local — une condition de plus sur
la règle existante réservée aux composés plutôt qu'une suppression de sa frontière.

Rejetée parce qu'elle ne répond pas à la question de savoir pourquoi la frontière est
composé-contre-primitif plutôt que a-quelque-chose-à-dire-contre-n'en-a-pas : les deux paramètres
que l'exemple même de cet enregistrement met en regard — `status: Dummy.Enum<OrderStatus>()` et un
`customerId: new AnyCustomerId()` composé et propre — portent la forme identique et le rien
identique à rapporter, et une règle qui les distinguerait par leur seul type aurait tracé une
ligne qu'aucun lecteur du fichier émis n'a de raison de voir.

## Conséquences

### Positives

* Le generator d'un paramètre primitif sans guard est un simple appel dans l'initialiseur du
  constructeur, sans méthode de factory pour l'envelopper — le fichier émis est plus court d'une
  méthode par paramètre de ce genre, le même bénéfice qu'ADR-0089 revendiquait déjà pour les
  composés.
* Deux paramètres de forme identique — résolus, sans guard — s'émettent de façon identique, que
  l'un des deux soit composé ou non, refermant l'asymétrie qu'ADR-0095 aurait sinon rendue
  visible.
* Une méthode de factory, là où elle est encore émise, porte toujours quelque chose : une chaîne
  resserrée, ou l'un des deux marqueurs bloquants. Sa présence est désormais informative en
  elle-même.

### Négatives

* Chaque fichier golden existant et chaque fixture du corpus nommé dont les paramètres étaient des
  primitifs sans guard change de forme — une mise à jour mécanique, ponctuelle, pas un coût
  récurrent.

### Risques

* Un futur ajout de lecture de guard qui resserre une chaîne sans positionner le drapeau `Guard`
  mettrait silencieusement en ligne un paramètre qui aurait dû garder sa factory. Le drapeau est le
  seul signal que cette décision lit, donc un lecteur ajoutant un nouveau chemin de resserrement
  doit le positionner, la même discipline que les chemins existants respectent déjà.

## Actions de suivi

* Aucune.

## Références

* [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md) — la règle
  que cet enregistrement généralise ; sa phrase de décision reste inchangée, seule la question de
  la forme d'émission que ses conséquences positives réservaient aux paramètres composés change.
* [ADR-0095](0095-read-the-assigned-null-check-as-a-guard-idiom-too.fr.md) — le changement qui a
  rendu visible, sur un paramètre composé et pas seulement primitif, l'asymétrie que cet
  enregistrement retire.
