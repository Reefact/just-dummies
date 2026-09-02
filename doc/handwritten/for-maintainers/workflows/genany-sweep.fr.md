# Workflow `genany-sweep`

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](genany-sweep.en.md)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/genany-sweep.yml`](../../../../.github/workflows/genany-sweep.yml)
**Suite :** [`JustDummies.GenAny.UnitTests/GenerativeSweepTests.cs`](../../../../JustDummies.GenAny.UnitTests/GenerativeSweepTests.cs)
**Banc :** [`JustDummies.GenAny.UnitTests/Sweep/`](../../../../JustDummies.GenAny.UnitTests/Sweep/)

## À quoi il sert

Il prend le produit d'un ensemble d'axes déclarés — type de collection × type d'élément × garde de
taille × famille — et fait passer chaque domaine qui en sort par tout le scaffolder : le scaffolder,
compiler ce qui en est sorti, passer les analyzers de la bibliothèque dessus, et tirer. Environ
3 600 formes.

**C'est l'instrument qui trouve des choses**, et son premier passage complet a trouvé deux défauts que
personne n'avait vus. Cette affirmation est mesurée, pas supposée. Sur la campagne de lecture des
gardes d'août 2026, le test de mutation et le corpus nommé de
[`GuardCorpus.cs`](../../../../JustDummies.GenAny.UnitTests/GuardCorpus.cs) réunis ont produit **aucun
défaut du moteur** sur vingt-six formes écrites à la main ; un balayage génératif ad hoc en a produit
**vingt**. Les deux ne cherchent pas la même chose, et jusqu'à ce workflow celui qui trouve des défauts
**n'était pas dans le dépôt** : ce balayage était du code scratch. Rien ne pouvait le rejouer contre
`main`, et personne ne pouvait dire si ses trouvailles étaient fermées.

Les trois bancs se répartissent ainsi, et aucun ne remplace un autre :

| Banc | Demande |
|---|---|
| `GuardCorpus` + `GuardedScaffoldsHoldTests` | *le moteur traite-t-il **ce** domaine correctement ?* — une personne a choisi chacun, et chacun est une question |
| Les jambes de mutation de [`justdummies-mutation`](justdummies-mutation.fr.md) | *y a-t-il du code que rien n'affirme ?* — des cellules qu'aucun test n'a visitées |
| Ce balayage | *quelque chose sort-il faux dans un produit large et uniforme ?* — personne n'en a choisi aucun |

## Les sept règles

Le balayage ne prédit rien. Un banc qui calculerait le verdict attendu à partir des axes encoderait le
comportement d'aujourd'hui et deviendrait un détecteur de changement déguisé en détecteur de défauts ;
un banc qui classerait sur le texte d'un message du compilateur lirait de la prose. Ce à quoi il tient
les formes, ce sont des affirmations vraies quoi que fasse le moteur — et la première porte sur le banc
lui-même.

| # | Règle | Une violation est |
|---|---|---|
| 0 | le domaine généré compile **seul**, avant qu'on demande quoi que ce soit au moteur | **un bug du balayage** — jamais une trouvaille |
| 1 | le moteur scaffolde la cible, et chaque générateur que le fichier de la cible nomme | une trouvaille |
| 2 | ce qui ne compile pas, ne compile pas **sur une ligne sentinelle** | une trouvaille |
| 3 | la ligne `TODO_verify_*` supprimée, comme §5.6 le dit au développeur, ça compile | une trouvaille |
| 4 | aucune règle de la bibliothèque au-dessus d'`Info`, aucun `Info` hors `JD030` | une trouvaille |
| 5 | un tirage produit une valeur, **ou** est refusé par `AnyGenerationException` | une trouvaille |
| 6 | un refus de distinction arrive exactement quand la source dit qu'il le doit — **dans les deux sens** | une trouvaille |

### La règle 0 est celle qui a été apprise à la dure

Le balayage d'août a imprimé 4 394 lignes. Les trier après coup en a montré 208 dont le fichier émis
échouait sur `CS0019: Operator '<' cannot be applied to operands of type 'method group' and 'int'` — le
balayage avait gardé des tableaux avec `.Count` au lieu de `.Length`, donc **les domaines qu'il générait
ne compilaient pas**, et il lisait son propre C# cassé comme des défauts du moteur. Rien en lui ne
pouvait distinguer les deux.

Alors le balayage compile d'abord le domaine seul, et un échec là est rapporté dans des mots qu'on ne
peut pas confondre avec une trouvaille. C'est aussi pourquoi `SweepAxes.Collections` porte le membre de
comptage par collection au lieu de supposer `Count` : l'axe sait à quoi répond un tableau.

### La règle 5 est la plus tranchante, et elle est gratuite

Chacun des 352 échecs de tirage du balayage d'août était une `AnyGenerationException` — la bibliothèque
déclinant un domaine qu'elle ne peut pas honorer (ADR-0046). Pas un seul n'était le constructeur du
domaine rejetant une valeur produite par le moteur. Ça donne une ligne exacte : un refus de première
classe est un résultat auquel la bibliothèque a droit, et **tout le reste est une valeur qui n'aurait
jamais dû être tirée**.

### La règle 6 est celle qu'août ne pouvait pas énoncer

`SweepAxes.Elements` déclare une cardinalité par élément — combien de valeurs distinctes le type contient
— et seulement là où la source générée le tranche elle-même, c'est-à-dire en pratique les énums. Alors un
ensemble demandant cinq `Slot` distincts là où `Slot` déclare trois membres **doit** être refusé, et un
ensemble demandant deux `Wide` là où `Wide` en déclare trente-deux **ne doit pas** l'être. Entre les deux
la réponse dépend de la façon dont la bibliothèque tire, la bibliothèque borne ses retirages et échoue au
lieu de boucler (ADR-0004, ADR-0012, ADR-0027), et le balayage ne dit rien plutôt que de deviner.

## Les verdicts

Réussite et échec seraient faux ici : trois d'entre eux sont des résultats auxquels le moteur a **droit**,
et les fondre dans « échoué » rapporterait l'honnêteté de la bibliothèque comme un défaut — le miroir de
l'erreur que l'[ADR-0093](../adr/0093-publish-mutation-statuses-not-a-score.fr.md) consigne sur
l'instrument de mutation, où un timeout était fondu dans « tué ».

| Verdict | Ce qui s'est passé |
|---|---|
| `Held` | a compilé, n'a rien levé, a tiré des valeurs que son propre domaine accepte |
| `RefusedByDesign` | le tirage a rencontré un refus de première classe (ADR-0046) |
| `BlockedForVerification` | une sentinelle `TODO_verify_*` bloque la compilation au-dessus d'une base réelle (§5.6, ADR-0083) |
| `Unresolved` | une sentinelle `TODO_supply_a_generator_for_*` : un paramètre ouvert (§5.5) |
| `KnownResidue` | le générateur a tiré une valeur que le domaine rejette, **et §9 dit qu'il le ferait** |
| `KnownDefect` | une règle cassée, qu'une entrée de `SweepDefects` consigne déjà |
| `Finding` | une règle que le moteur doit tenir, cassée |
| `SweepBug` | un domaine généré ne compile pas seul — le nôtre, pas celui du moteur |

`KnownResidue` est celui qui mérite deux lectures. §9 déclare, comme non-objectif, qu'une garde atteinte
par un niveau d'indirection que l'outil ne suit pas — *une copie locale du paramètre* avant tout — est
une garde que l'outil ne distingue pas d'une absence de garde : il ne marque rien, ne bloque rien, et
tire librement. Les seize formes `delegate-computed-*` sont là exprès, chacune portant la phrase qui
l'excuse. **C'est le seul instrument du dépôt qui met un nombre sur la largeur de ce résidu**, et une
forme qui cesse d'y atterrir déplace les comptes versionnés — donc le résidu qui rétrécit s'annonce aussi.

## Ce que le premier passage a trouvé

Le produit entier tourne en **deux minutes** sur quatre cœurs. Le 2026-09-02 il est revenu avec 3 627
formes jugées, aucun bug du balayage, et **103 trouvailles en deux classes** — ouvertes toutes les deux,
consignées toutes les deux dans
[`SweepDefects.cs`](../../../../JustDummies.GenAny.UnitTests/Sweep/SweepDefects.cs), corrigées ni l'une
ni l'autre dans le changement qui a posé le banc.

**`cardinality-hint-lost-through-as` (55 formes).** `Any.SetOf(Any.Boolean())` plafonne l'ensemble à deux
éléments, parce qu'`AnyBoolean` porte `ICardinalityHint<bool>` et qu'`AnySet` le lit (ADR-0004).
`Any.SetOf(Any.Boolean().As(value => (bool?)value))` non : `AnyExtensions.As` rend un
`DerivedAny<TResult>` qui porte la source aléatoire et la reproductibilité de ce qu'il enveloppe, et rien
d'autre. L'ensemble n'a alors plus de plafond, choisit une taille que le vivier d'éléments ne peut pas
remplir, et meurt sur le retirage borné — sur un domaine qui demande **un** élément. Faire suivre
l'indication à travers une projection est sain en général : une projection peut confondre des valeurs
distinctes, jamais en créer. Ça touche tout ensemble ou dictionnaire scaffoldé clé par une énum ou un
booléen **nullable**, puisque cette conversion est exactement ce que le moteur écrit pour un élément
nullable.

**`nested-collection-loses-its-declared-interface` (48 formes).** `Any.SetOf(…)` est typé
`IAny<HashSet<T>>` et `Any.ListOf(…)` `IAny<List<T>>`, donc une collection *de* l'un de ceux-là porte le
type concret là où le paramètre déclare l'interface. Les types externes covariants se lient encore — c'est
pourquoi `nested-rolist-*` et `nested-array-*` compilent — et les invariants ne le peuvent pas :
`List<HashSet<Slot>>` n'est pas un `List<ISet<Slot>>`. Le fichier émis échoue alors sur un simple `CS0029`
sans sentinelle au-dessus, ce qui est la seule chose qu'ADR-0083 dit qui ne doit pas arriver.
`List<IReadOnlyList<string>>` est un domaine ordinaire.

Deux autres nombres de ce passage méritent lecture. La famille `element` est revenue **78 bloquées,
0 tenue** : une garde de distinction sur les éléments et un test de nullité dans un `foreach` sont tous
deux hors de l'ensemble fermé de §5.3, donc chacune rencontre une sentinelle — ce qui est la réponse
prévue, qui marche. Et les seize formes `delegate-computed-*` sont la totalité de `KnownResidue` : le
résidu du §9, mesuré.

## Quand il tourne

* **Chaque semaine**, le lundi à 07h07 UTC.
* **À la demande**, par `workflow_dispatch`.

Jamais sur une pull request, pour la raison que l'[ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)
a donnée à la jambe de mutation du générateur : le coût suit la taille du produit, pas celle du diff.

**Ce qui tourne à chaque build à la place**, c'est la tranche couvrante — le plus petit sous-ensemble
glouton par préfixe qui touche encore chaque valeur d'axe, environ quatre-vingt-quinze formes — comme une
théorie ordinaire dans `JustDummies.GenAny.UnitTests`. Elle ne peut pas trouver ce que le produit trouve.
Elle existe pour que l'appareil ne puisse pas cesser de fonctionner en silence entre deux lundis, ce qui
est précisément comme les autres bancs de ce dépôt se sont cassés.

## Comment il tourne

Le workflow ajoute exactement une chose à un run de test ordinaire : la variable `JUSTDUMMIES_SWEEP`.
Sans elle le balayage complet est skippé et la tranche tourne quand même, donc
`dotnet test JustDummies.sln` reste rapide et la jambe de mutation du générateur ne paie rien pour
l'existence du balayage.

```
dotnet test JustDummies.sln                                     # la tranche seule
JUSTDUMMIES_SWEEP=1 dotnet test JustDummies.GenAny.UnitTests    # le produit entier
```

Les formes tournent **séquentiellement**, et pas faute de cœurs : le tirage tourne sous une graine
ambiante (ADR-0061) que deux formes tirant en même temps partageraient. Un banc dont les valeurs
dépendent du nombre de formes qui tournaient à côté est le défaut exact que l'ADR-0093 consigne sur
l'autre instrument.

## Ce qu'il publie

Des comptes par verdict, par famille — jamais un score, pour la raison que donne l'ADR-0093. Un run où
chaque forme reviendrait `Unresolved` obtiendrait un score parfait contre n'importe quel ratio qu'on
voudrait définir, et voudrait dire que le moteur a cessé de résoudre quoi que ce soit.

* `artifacts/sweep/generative-sweep.tsv` — une ligne par forme, dans les sept colonnes qu'imprimait le
  balayage d'août (`name`, `family`, `status`, `provenance`, `compiles`, `rules`, `draw`) plus le verdict
  et sa raison, pour que les deux relevés se joignent ligne à ligne.
* `artifacts/sweep/summary.md` — les comptes, que le job ajoute au résumé du run.
* `JustDummies.GenAny.UnitTests/Sweep/sweep-baseline.tsv` — **versionné**, et vérifié par le balayage
  complet.

## À manier avec précaution

* **La référence est un fichier golden, et elle bouge délibérément.** Elle porte une ligne par famille et
  par verdict — grossier exprès. Une ligne par forme serait acceptée au lieu d'être relue ; une table de
  cette taille montre une régression de couverture comme un nombre qui a bougé, et trois cents formes
  passant d'une garde lue à une sentinelle ne violent aucune règle de l'oracle et passeraient sinon en
  silence. Sur un désaccord, le run écrit `sweep-baseline.received.tsv` à côté. Ne déplacez le second sur
  le premier **qu'**une fois que vous pouvez dire pourquoi ça a bougé.
* **Ne fondez pas un verdict dans un autre pour rendre un run vert.** Chacun des six répond à une question
  différente, et toute la conception repose sur leur séparation.
* **Un `KnownResidue` a besoin de sa phrase.** L'argument `residue:` est une affirmation sur la
  **spécification**, pas une prédiction sur le moteur : il dit qu'un lecteur peut trouver la phrase qui
  excuse cette forme. En ajouter un sans cette phrase transforme le banc en détecteur de changement.
* **Une entrée de `SweepDefects` sort avec le correctif, pas avec le test.** Une entrée que plus aucune
  forme ne reproduit fait échouer le run : un défaut que rien ne reproduit est un défaut corrigé, et son
  entrée est alors la seule chose qui dise encore le contraire.
* **Ajouter une valeur d'axe multiplie.** L'axe des éléments est dépensé large sur les collections
  distinctes, où la cardinalité tranche, et étroit sur le reste, où une garde de taille n'interagit avec
  l'élément par rien du tout. Gardez cette asymétrie en l'étendant.
* **Le balayage ne réclame aucune ligne des tables d'idiomes fermées** (§5.3). C'est le travail du corpus,
  et `RecognisedIdiomCoverageTests` en est le juge. Les deux bancs ne se recouvrent pas et ne doivent pas
  commencer à le faire.

## Voir aussi

* [`justdummies-mutation`](justdummies-mutation.fr.md) — l'autre instrument, et ce qu'il mesure à la
  place.
* [ADR-0083](../adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — pourquoi une
  garde invérifiable bloque la compilation au lieu d'être livrée en silence.
* [ADR-0085](../adr/0085-change-the-guard-reader-only-against-a-field-report.fr.md) — le corpus nommé, et
  l'oracle de tirage que les deux bancs partagent.
* [ADR-0093](../adr/0093-publish-mutation-statuses-not-a-score.fr.md) — des statuts plutôt qu'un score, et
  pourquoi un statut qui veut dire « pas de verdict » ne doit jamais être fondu dans un qui veut dire
  « attrapé ».
* [`justdummies-tool.fr.md`](../specifications/justdummies-tool.fr.md) — §5.3 l'ensemble fermé d'idiomes, §5.5 et
  §5.6 les deux sentinelles, §9 le résidu.
