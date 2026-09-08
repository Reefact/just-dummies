# ADR-0099 | Offrir `OneOf` partout où le générateur construirait sinon la valeur

🌍 🇬🇧 [English](0099-offer-oneof-wherever-the-generator-would-otherwise-build.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-09-08
**Decision Makers:** Reefact

## Contexte

L'[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) a réglé ce qu'un
générateur peut exposer : une contrainte est **constructive** — elle décrit une valeur que le
générateur doit bâtir, et n'est offerte que là où il peut en bâtir une qui la satisfait — ou
**rejective** — elle retire des valeurs d'un domaine, et est offerte partout. Elle a appliqué ce test
à `AnyPattern` et y a admis la paire d'exclusion, tout en maintenant le refus des contraintes de
forme : construire dans l'intersection de deux langages réguliers est une machinerie que la
bibliothèque n'a pas.

`OneOf` figurait dans le tableau mesuré de cet enregistrement, comme l'un des deux cas dont la
décision restaurait la composabilité. Il n'a jamais été appliqué *à* `AnyPattern`, parce
qu'`AnyPattern` n'avait aucun `OneOf` à rendre composable. Les conséquences de l'enregistrement citent
`Any.StringMatching(p).DifferentFrom(existing)` et s'arrêtent là.

Il en résulte le seul trio partiel de la surface : `AnyPattern` porte `Except` et `DifferentFrom`, et
pas de `OneOf`. C'est un manque, non un refus — aucun enregistrement ne l'énonce, et la documentation
du type se décrivait comme portant « la paire d'exclusion », ce qui se lit comme une réponse complète.

Autres faits qui encadrent le choix :

* L'argument même de l'ADR-0033 pour admettre un ensemble de valeurs fourni par l'appelant est qu'il
  s'agit d'*un domaine, non d'une mise en page* : dès que les valeurs sont fournies il n'y a plus rien
  à bâtir, chaque autre contrainte devient un test que chaque valeur passe ou échoue, le domaine est
  l'ensemble des valeurs qui passent, et la satisfiabilité est l'unique question de savoir s'il en
  reste.
* Un motif est exactement un tel test, et le moteur qui l'exécute existe déjà. Depuis
  l'[ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md), chaque valeur
  bâtie est vérifiée contre le vrai moteur .NET avant d'être renvoyée. Juger les valeurs fournies
  demande ce vérificateur et rien d'autre.
* L'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) exige qu'un constructeur typé
  n'accepte que des valeurs valides du domaine qu'il représente — `Any.Enum<T>().OneOf(...)` rejette un
  membre que le type ne définit pas. Le domaine d'un motif est son langage : filtrer un ensemble
  fourni par le motif, c'est cette même règle, pas une nouvelle.
* Sur un motif sans vivier, une exclusion est satisfaite par un **retirage borné**, et une exclusion
  qui vide le langage se manifeste à la génération comme un budget dépensé — jamais comme une preuve
  d'impossibilité, puisque la bibliothèque n'énumère pas un langage régulier. Un ensemble fourni est
  fini et énumérable : la même question devient décidable à la déclaration.
* Deux générateurs portent légitimement moins que le trio complet, et les deux se dérivent :
  `AnyOneOf<T>`, dont le vivier *est* le `OneOf` — un second ne pourrait qu'intersecter deux viviers —
  et `AnyBoolean`, dont le domaine à deux valeurs est nommé membre par membre par `True()`/`False()`.
* `AnyPattern` diffère la compilation de son vérificateur jusqu'à ce qu'un tirage en ait besoin, et
  c'est délibéré : un motif dont le plafond de génération refuse la génération (un quantificateur non
  borné dont le minimum se compte en milliards) est rejeté par le constructeur avant que le
  vérificateur ne soit bâti, si bien qu'une `Regex` dont on a observé qu'elle épuisait la mémoire sur
  au moins une implémentation du moteur .NET n'est jamais compilée pour lui.

## Décision

`OneOf` est offert par tout générateur qui construirait sinon la valeur qu'il tire, `AnyPattern`
compris, et n'est retenu que là où le domaine est déjà l'ensemble explicite de l'appelant ou est nommé
membre par membre.

## Justification

**Le test de l'ADR-0033, appliqué honnêtement, l'admet.** Le refus sur un motif est un énoncé sur les
contraintes *constructives* — elles ne peuvent être bâties dans l'intersection de deux langages
réguliers. Un ensemble de valeurs ne construit rien : il fournit le domaine et rétrograde le motif au
rang de prédicat. Le refuser demanderait un fondement que l'ADR-0033 n'a pas, et le seul candidat — « le
motif ne fait alors que valider » — est tout aussi vrai de `WithLength(3)` à côté de
`Any.String().OneOf(...)`, que cet enregistrement a délibérément admis.

**Le manque enseigne une règle que la bibliothèque ne tient pas.** Un lecteur qui trouve `Except` et
`DifferentFrom` sur un motif et pas de `OneOf` ne peut pas distinguer un refus d'un oubli, et le type
disait « la paire d'exclusion » comme si la question était close. Énoncer la règle positive — `OneOf`
est offert partout où le générateur construirait sinon — répond pour `AnyPattern`, et explique aussi
les deux générateurs qui portent moins, lesquels ressemblaient jusqu'ici à deux exceptions de plus.

**Le gain n'est pas seulement une symétrie : c'est un meilleur diagnostic.** Le domaine étant fourni,
une exclusion qui le vide devient un conflit signalé dès la déclaration, avec un message nommant les
deux contraintes en cause. Sur un motif sans vivier, la même exclusion ne pouvait qu'épuiser son
budget de retirage et signaler ce budget épuisé. Quant à une valeur que le motif ne reconnaît pas,
l'inspection de vivier la rapporte en désignant le motif comme la contrainte qui l'a écartée. C'est
précisément la question à laquelle `IPoolInspection<T>` doit répondre : faut-il élargir l'invariant,
ou corriger le catalogue ?

**La composition qu'il permet est celle pour laquelle la bibliothèque existe.** Un helper partagé
définit le format (`Any.StringMatching(SkuPattern)`), et le site d'appel limite ensuite le générateur
aux références réellement disponibles dans la fixture. Sans `OneOf`, l'appelant doit renoncer au
helper et écrire `Any.ElementOf(pool)` : il perd alors le format que le helper servait à énoncer, et
avec lui la vérification que le vivier lui reste conforme.

**Compiler le vérificateur plus tôt est un compromis qu'il faut énoncer.** Valider les valeurs de
l'appelant dès la déclaration est ce qui rend le conflit immédiat, mais cela compile le vérificateur
pour un motif qu'un tirage aurait pu refuser avant même d'avoir bâti une valeur. Le cas exposé exige
à la fois un motif à borne de quantificateur démesurée *et* un ensemble de valeurs, combinaison qui
n'a aucune raison de se présenter ; en face, le conflit devient immédiat dans tous les cas
ordinaires.

## Alternatives envisagées

### Refuser `OneOf` sur un motif, et consigner le refus

Envisagée sérieusement, et c'est la réponse la moins chère : une phrase sur le type, une ligne au
registre *Considered, not adopted* avec sa condition de réouverture, et rien de la machinerie de
vivier. Elle s'accorde aussi avec l'habitude de la base : borner la surface et refuser bruyamment au
bord.

Rejetée parce que le refus n'a aucun fondement qui survive à l'ADR-0033. Chaque argument en sa faveur
— le motif cesse de générer, il ne fait que valider, `Any.OneOf(...)` tire déjà d'un vivier —
s'applique mot pour mot à `Any.String().OneOf(...)` à côté d'une contrainte de forme, que l'ADR-0033 a
admis après avoir pesé exactement ceux-là. Un refus qui contredit un enregistrement accepté est pire
que le manque : il rend la base inutilisable comme argument.

### Offrir `OneOf` mais laisser les exclusions différées, comme aujourd'hui

Envisagée parce que c'est le plus petit changement : tirer dans l'ensemble fourni, garder la boucle de
retirage pour les exclusions, n'ajouter aucune validation immédiate.

Rejetée parce qu'elle conserverait une recherche bornée là où la réponse est désormais décidable, et
rapporterait un budget dépensé pour un domaine que la bibliothèque sait compter. Elle contredit aussi
la promesse que l'ADR-0033 a obtenue — un générateur qui existe sait générer — pour la seule forme où
l'honorer est trivial.

### Juger les valeurs fournies paresseusement, au premier tirage

Envisagée comme le moyen de garder le vérificateur différé : filtrer le vivier au premier `Generate`,
pour qu'un motif que le plafond de génération refuse ne compile jamais de `Regex`.

Rejetée parce qu'elle déplace l'échec de la déclaration qui l'a causé vers un tirage qui n'y est pour
rien, ce qui est précisément le diagnostic que cette bibliothèque existe pour protéger. Un *arrange*
impossible rapporté à la génération se lit comme un défaut du générateur plutôt que comme le défaut de
test qu'il est.

## Conséquences

### Positives

* Le trio agnostique du type est complet partout où il a du sens, et les deux générateurs qui en
  portent moins sont expliqués par la même phrase au lieu de rester des exceptions.
* Sous un ensemble de valeurs, un domaine vidé est un conflit à la déclaration nommant les deux côtés,
  au lieu d'un budget de retirage épuisé à la génération.
* Un motif à vivier rapporte ses survivants et ses rejets, et répond à une collection distincte avec
  la taille de l'ensemble qui a survécu.

### Négatives

* `AnyPattern` passe d'un petit constructeur autonome à un constructeur portant un vivier, sa
  provenance et deux interfaces de plus — davantage d'état à garder cohérent au fil des dérivations.
* Le vérificateur n'est plus compilé uniquement sur un chemin qui a déjà validé que le motif est
  générable : déclarer un ensemble de valeurs le compile, délibérément.
* Un troisième générateur se comporte désormais différemment selon qu'un ensemble fourni par
  l'appelant est déclaré ou non, ce que la documentation doit énoncer : refus immédiat avec un
  ensemble, retirage borné sans.

### Risques

* **La distinction entre refus immédiat et refus différé est une couture.** La même exclusion produit
  deux échecs différents selon qu'un ensemble de valeurs a été déclaré ou non, et un lecteur qui
  rencontre l'une des deux formes en premier risque de lire l'autre comme une régression. Atténué en
  énonçant les deux cas sur le type et sur la page utilisateur, au niveau de la contrainte, et pas
  seulement ici.
* **La compilation du vérificateur à la déclaration n'a été mesurée que sur un moteur.** Un motif à
  borne de quantificateur énorme se compile en quelques millisecondes sur le moteur .NET moderne ; la
  précaution que portait le code nomme une autre implémentation, et le plancher .NET Framework 4.7.2
  n'a pas été mesuré. L'exposition exige ce motif *et* un ensemble de valeurs pour être atteinte.

## Actions de suivi

* Aucune. La contrainte, ses conflits, l'inspection de vivier, la garde de surface et la documentation
  utilisateur atterrissent ensemble.

## Références

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — le test
  constructif/rejectif que cet enregistrement applique au seul endroit qu'il n'avait pas atteint.
* [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md) — le vérificateur
  par lequel un ensemble de valeurs est jugé.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — pourquoi une valeur fournie
  hors du domaine est refusée plutôt que tirée.
* [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.fr.md) et
  [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.fr.md) —
  l'inspection qu'un nouveau vivier doit porter.
* [ADR-0098](0098-offer-every-oneof-in-both-shapes.fr.md) — la décision jumelle sur la forme de
  `OneOf`, issue du même élément d'audit.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — l'élément d'audit qui a signalé
  le trio partiel.
