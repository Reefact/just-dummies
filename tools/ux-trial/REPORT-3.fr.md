# Essai d'expérience développeur n° 3 — JustDummies et `any` : rapport

## 1. Synthèse

**Légende.** Chaque domaine a été testé par trois sessions indépendantes, une par profil de développeur : **D = découverte** (n'a jamais utilisé JustDummies ni `any`), **R = régulier** (les utilise depuis des mois), **E = expert** (artisan expert du test, exigeant sur ce qu'est un dummy). Un identifiant de constat comme `07R-3` se lit : domaine 07, profil R, troisième constat de cette session. « D / R / E » dans un tableau, ou « (D, R) » après un domaine, donne un décompte ou une liste par profil.

**Verdict.** La bibliothèque tient ; le temps se perd à la frontière du scaffolder.
- 50 domaines, trois profils chacun : 150 sessions, 150 suites, **43 867 cas de test, 0 en échec, 601 ignorés**. Chaque cas ignoré consigne un défaut de domaine. Ce décompte vient de l'exécution de chaque suite par le coordinateur depuis le dépôt, non des rapports des sessions.
- Toutes les suites sont restées vertes sur des exécutions répétées avec de nouvelles graines (de 11 à 222 exécutions par suite, selon le rapport de chaque session).
- 1 530 constats : 994 portent sur la bibliothèque, l'outil ou la documentation, regroupés en **151 causes racines**, et 536 sur les modèles de domaine de l'essai lui-même, regroupés en 264 grappes.
- Chacune des 151 causes racines a été vérifiée de façon contradictoire, par reproduction sur les packages empaquetés : **53 confirmées, 63 partielles, 33 par conception, 1 réfutée, 1 non reproduite**. Parmi les 116 éléments confirmés ou partiels, 15 sont de gravité moyenne et 101 de gravité basse ; aucun n'est de gravité haute.
- **Un seul bug du cœur** a été trouvé, et il touche la justesse : une paire ordonnée accepte ou refuse les mêmes deux épingles selon l'ordre des appels ([CORE-1](#core-1)). Il a été rencontré une fois en 150 sessions.

Ce qui a fonctionné :
- Le DSL, les paires `With…` d'épingle et d'affinage, les sentinelles `TODO_verify` qui bloquent la compilation et `[Reproducible]` ont fonctionné dans tous les domaines. Partout où le scaffolder ne pouvait pas se porter garant d'une garde, la compilation s'est arrêtée sur une sentinelle ; les lacunes sont les formes documentées comme silencieuses ([CLI-10](#cli-10)) et un cas d'instruction composée ([CLI-5](#cli-5)).
- Les valeurs tirées ont révélé de vrais défauts de domaine dans au moins 14 domaines, chacun rejoué depuis sa graine : une somme de covariances que le domaine refuse (28), une entrée de solveur hors plage (32), une somme de durées impossible à écrire (33), une population menée à l'extinction (21), et plusieurs dépassements de grandeurs laissées sans plafond (voir le [§9](#9-défauts-de-domaine-trouvés-par-les-sessions)).
- À six reprises, une valeur tirée a montré qu'une valeur que le test traitait comme un dummy n'en était pas un, et la graine l'a rejouée du premier coup ([§7](#7-ce-qui-a-fonctionné)).
- La plupart des éléments de REPORT-2 ont été traités et tiennent sur le terrain ([§1.2](#12-depuis-report-2)).

Ce qui a ralenti :
- **Les règles qui lient des paramètres entre eux.** 219 constats dans 49 des 50 domaines : un paramètre dérivé dont les épingles n'atteignent plus le tirage ([CLI-1](#cli-1)), des liens passant par un autre tirage que la documentation ne couvre que dans un autre guide ([DOCS-2](#docs-2), [DOCS-6](#docs-6)), un ordre assorti d'un écart maximal ([CORE-2](#core-2)), et l'absence d'une couture qui résiste à un nouveau scaffold (une décision, [§5](#5-candidats-à-une-décision)).
- **La mise en place du premier lancement, extérieure aux concepts de la bibliothèque** : l'opt-in à Microsoft.Testing.Platform sur le SDK .NET 10 ([DOCS-1](#docs-1) ; sa fréquence est un artefact du banc d'essai, voir le [§2](#2-limites-de-cet-essai)) et la restauration dont `any` a besoin ([CLI-2](#cli-2)).

### 1.1 À corriger d'abord

Classés selon ce que chaque élément coûte d'un domaine à l'autre, et pas seulement selon leur nombre. « Constats » est le nombre de constats de session que regroupe la cause racine ; D/R/E les compte par profil.

| # | Élément | Nature / gravité | Verdict | Constats | Domaines | D / R / E |
|---|---|---|---|---|---|---|
| 1 | [CORE-1](#core-1) Deux épingles sur une paire ordonnée sont acceptées ou refusées selon l'ordre des appels ; au travers d'un générateur scaffoldé, aucun ordre ne fonctionne | bug, moyenne | confirmé | 1 | 1 | 0 / 1 / 0 |
| 2 | [CLI-1](#cli-1) Un paramètre dérivé dans `Generate()` garde des épingles `With…` qui compilent et ne sont pas honorées ; une épingle contradictoire est réparée au lieu d'être refusée | friction, moyenne | confirmé | 83 | 41 | 26 / 29 / 28 |
| 3 | [CLI-4](#cli-4) Un helper de garde `void` du même type n'est jamais lu, alors que le même corps derrière un helper qui renvoie une valeur est lu en entier | friction, moyenne | confirmé | 23 | 13 | 7 / 6 / 10 |
| 4 | [CLI-5](#cli-5) Les `throw` d'une même instruction composée (un bloc `if`, une boucle) partagent un seul pointeur : les suivants ne sont ni nommés ni comptés, et les liens passant par une variable locale ne sont pas signalés | défaut, moyenne | partiel / confirmé | 18 | 11 | 5 / 6 / 7 |
| 5 | [XU-1](#xu-1) `[Reproducible]` rapporte une graine et un extrait de rejeu pour un test en échec qui n'a rien tiré | défaut, basse | confirmé | 19 | 16 | 5 / 2 / 12 |
| 6 | [DOCS-2](#docs-2) + [DOCS-6](#docs-6) Les liens passant par un autre tirage (un membre, une sorte, un nombre, les éléments d'une collection) ont des formes `.Then(...)` qui fonctionnent, mais la section de la page de la CLI sur les liens ne les montre ni ne les référence jamais | documentation, moyenne | partiel / confirmé | 60 | 34 | 28 / 11 / 21 |
| 7 | [DOCS-1](#docs-1) Le plancher `xunit.v3` 4.x de l'adaptateur demande l'opt-in à Microsoft.Testing.Platform sur le SDK .NET 10, et aucune page utilisateur ne le dit | documentation, moyenne | partiel | 148 | 50 | 50 / 50 / 48 |
| 8 | [DOCS-3](#docs-3) Un test de garde contre null au travers d'un `With…` scaffoldé demande un cast (CS0121) ; la page ne le montre que pour les paramètres nullables | documentation, basse | confirmé | 20 | 17 | 3 / 5 / 12 |
| 9 | [CORE-2](#core-2) Un ordre assorti d'un écart maximal est scaffoldé en un `Any.OrderedPair` que le constructeur refuse à chaque tirage, et aucune page ne montre la modification qui conserve les épingles | friction, moyenne | partiel | 19 | 8 | 8 / 6 / 5 |
| 10 | [CLI-3](#cli-3) + [DOCS-5](#docs-5) Une ligne `decimal`/`double` bornée d'un seul côté atteint la limite du type un tirage sur dix, le récapitulatif dit seulement `guard`, et la page de la CLI explique JD030 mais pas JD036 | friction / documentation, basse | partiel | 48 | 29 | 14 / 15 / 19 |
| 11 | [CLI-2](#cli-2) La page utilisateur ne dit jamais que le projet doit être restauré ; un lot non restauré est rapporté comme le refus du premier type | friction, basse | partiel | 46 | 34 | 23 / 16 / 7 |
| 12 | [CLI-7](#cli-7) Le récapitulatif et le rapport JSON ne disent jamais que deux paramètres ont été tirés ensemble au travers d'`Any.OrderedPair` | défaut, basse | confirmé | 11 | 8 | 5 / 4 / 2 |
| 13 | [CLI-9](#cli-9) L'indication de regex est perdue pour un champ `Regex` construit avec un `new("…")` à type cible, contre la propre phrase de la spec, et pour `Match(...).Success` | défaut, basse | partiel | 8 | 8 | 2 / 2 / 4 |

### 1.2 Depuis REPORT-2

Les vérificateurs ont confronté chaque élément antérieur qu'ils rencontraient à la version empaquetée. Ces éléments de REPORT-2 sont traités et ont tenu dans cet essai ; un reliquat, quand il en reste un, a sa propre entrée.

| REPORT-2 | Ce qui a changé | Reliquat |
|---|---|---|
| CLI-1 gardes d'intervalle `\|\|` non lues | un `\|\|` est découpé en gardes distinctes (ADR-0128) | [CLI-46](#cli-low) |
| CLI-3 seule la première garde non lue est nommée | « first unread guard …, and N more » et « ties a to b » (#373) | [CLI-5](#cli-5), [CLI-27](#cli-low) |
| CLI-8 paramètre dérivé, épingles perdues | traité pour un ordre direct : `Any.OrderedPair` (ADR-0126, #404) émis par le scaffolder (#405) | [CLI-1](#cli-1), [CORE-2](#core-2) |
| CLI-10 paramètre typé par une interface | provenance `abstract` avec un TODO qui nomme le générateur à écrire (#375) | [CLI-11](#cli-11) pour les types du framework |
| CLI-12 nom court ambigu refusé | se résout vers le type du projet et nomme l'alternative | [CLI-40](#cli-low) |
| CLI-13 le TODO de regex omet le motif | le TODO cite le motif et liste les endroits à retaper (#374) | [CLI-9](#cli-9), [CLI-15](#cli-15) |
| CLI-14 helper qui renvoie une valeur, silencieux | marqué (#407), puis lu un niveau plus bas (#416) | [CLI-4](#cli-4), [CLI-10](#cli-10) |
| CLI-15 lignes de gardes manquantes | bornes `TimeSpan` en ligne, `decimal.Round` → `WithScale`, `IndexOfAnyExcept`, `.Length` au travers d'un helper (#408) | [CLI-8](#cli-8), [CLI-22](#cli-22) |
| CLI-18 espace de noms non nommé | le récapitulatif affiche `in namespace X (--namespace to change).` (#378) | [DOCS-4](#docs-4) |
| CLI-19 épingle à null d'un nullable | `Without…()` là où le défaut tire null (#411) ; null un tirage sur dix (ADR-0117) | [DOCS-3](#docs-3) |
| CLI-20 générateur passé à `With…` | remarques générées et un paragraphe de page (#391) | [CLI-21](#cli-21) |
| CLI-22 constante copiée comme valeur | une constante nommée est référencée (#377) | [CLI-32](#cli-low) |
| CLI-24 commentaire de cinq lignes périmé | « then delete these two lines » (#412) | [CLI-34](#cli-low), [CLI-37](#cli-low) |
| CORE-2 `Except` sans `IEnumerable<T>` | chaque builder typé a les deux surcharges | — |
| CORE-3 pas de `DistinctBy` | ajouté et documenté sur la page des collections | — |
| CORE-4 pas de « B diffère de A » | `DifferentFrom` sur chaque `IAny<T>` (ADR-0121) | [DOCS-12](#docs-12), [DOCS-13](#docs-13), [CORE-7](#core-7) |
| CORE-5, CORE-12 tirage dépendant, choix | `.Then(...)` (ADR-0119), `Any.Boolean().Then(...)` comme choix documenté | [DOCS-2](#docs-2), [AN-1](#an-1) |
| CORE-6 pas de générateur de constante | `Any.Fixed` | — |
| DOCS-1 formes lues non listées | la page de la CLI a un tableau des formes lues et non lues | — |
| DOCS-3 étendue des dates non documentée | « An unconstrained draw spans the whole type » sur la page des dates | [DOCS-35](#docs-low) |
| DOCS-8 le guide de démarrage ne mentionne jamais `any` | lié depuis « Where to go next » | [DOCS-10](#docs-10) |
| XU-1 l'extrait collé demande un `using` | l'extrait est pleinement qualifié | — |
| AN-1 règles Info invisibles | le remède figure dans le readme du nupkg, la page de la CLI et les pages JD030/JD031 | [DOCS-9](#docs-9) |
| REPORT.md AN-C3 entrées scaffoldées invisibles pour JD007 | JD007 se déclenche sur les entrées `Any.X()` scaffoldées (#365) | — |

Restent ouvertes en tant que décisions : l'étalement en échelle logarithmique (REPORT-2 CORE-14), les bases privées `AnyValid…()` (CLI-17), la couture qui résiste à un nouveau scaffold (le cas général de CLI-8). Voir le [§5](#5-candidats-à-une-décision).

### 1.3 Fluidité et nombre de tests

Une cellule se lit « fluidité / cas de test (ignorés) ». Les cas et les cas ignorés sont ceux de l'exécution de chaque suite par le coordinateur depuis le dépôt ; la fluidité est la note que chaque session s'est donnée.

| Domaine | D | R | E |
|---|---|---|---|
| 01-payments | 4 / 185 (3) | 4 / 230 (5) | 4 / 306 (5) |
| 02-trading | 4 / 169 (3) | 4 / 286 (3) | 4 / 231 (5) |
| 03-insurance | 4 / 175 (2) | 4 / 206 (2) | 4 / 193 (3) |
| 04-manufacturing | 4 / 233 (5) | 4 / 200 (6) | 4 / 266 (5) |
| 05-cad-drawing | 4 / 643 (11) | 4 / 400 (7) | 4 / 521 (11) |
| 06-warehouse | 4 / 188 (7) | 4 / 224 (5) | 4 / 244 (7) |
| 07-freight | 4 / 158 (4) | 4 / 245 (6) | 4 / 248 (4) |
| 08-patient-record | 4 / 193 (5) | 4 / 182 (4) | 4 / 249 (4) |
| 09-laboratory | 4 / 205 (3) | 4 / 246 (4) | 4 / 213 (3) |
| 10-pharma-serialisation | 4 / 213 (4) | 4 / 234 (5) | 4 / 271 (8) |
| 11-telecom | 4 / 202 (4) | 4 / 198 (4) | 4 / 207 (5) |
| 12-smart-grid | 4 / 200 (5) | 4 / 232 (5) | 4 / 238 (6) |
| 13-payroll | 4 / 201 (1) | 4 / 190 (1) | 4 / 189 (2) |
| 14-ecommerce | 4 / 295 (9) | 4 / 300 (6) | 4 / 262 (7) |
| 15-hotel-booking | 4 / 213 (2) | 4 / 301 (6) | 4 / 290 (9) |
| 16-aviation | 4 / 287 (5) | 4 / 290 (5) | 4 / 323 (6) |
| 17-real-estate | 4 / 242 (5) | 4 / 244 (2) | 4 / 297 (3) |
| 18-education | 4 / 174 (4) | 4 / 182 (2) | 4 / 163 (4) |
| 19-iam | 4 / 187 (4) | 4 / 156 (2) | 4 / 171 (3) |
| 20-agritech-iot | 4 / 188 (5) | 4 / 180 (4) | 4 / 203 (5) |
| 21-orbital-microbiology | 4 / 334 (2) | 4 / 329 (3) | 4 / 380 (4) |
| 22-pulmonary-nodules | 4 / 186 (2) | 4 / 161 (2) | 4 / 223 (2) |
| 23-captive-breeding | 4 / 207 (2) | 4 / 252 (2) | 4 / 239 (5) |
| 24-radiocarbon-chronology | 4 / 211 (4) | 4 / 238 (3) | 4 / 290 (6) |
| 25-adaptive-video | 4 / 332 (4) | 4 / 240 (3) | 4 / 311 (5) |
| 26-robotic-milking | 4 / 302 (5) | 4 / 185 (3) | 4 / 222 (4) |
| 27-railway-interlocking | 4 / 186 (4) | 4 / 156 (3) | 4 / 173 (4) |
| 28-satellite-conjunction | 4 / 249 (6) | 4 / 214 (4) | 4 / 266 (6) |
| 29-parimutuel-betting | 4 / 207 (1) | 4 / 221 (1) | 4 / 262 (2) |
| 30-liquidity-gap | 4 / 263 (0) | 4 / 264 (1) | 4 / 259 (3) |
| 31-vessel-stowage | 4 / 324 (0) | 4 / 364 (1) | 4 / 390 (0) |
| 32-grid-load-flow | 4 / 261 (3) | 4 / 273 (1) | 4 / 304 (3) |
| 33-music-score | 4 / 219 (2) | 4 / 309 (1) | 4 / 306 (2) |
| 34-manuscript-reconstruction | 4 / 269 (1) | 4 / 293 (1) | 4 / 379 (3) |
| 35-dna-variants | 4 / 266 (4) | 4 / 199 (3) | 4 / 235 (4) |
| 36-knitting-patterns | 4 / 149 (0) | 4 / 164 (3) | 4 / 205 (1) |
| 37-batch-genealogy | 4 / 275 (5) | 4 / 286 (10) | 4 / 392 (6) |
| 38-carbon-ledger | 4 / 310 (8) | 4 / 222 (14) | 4 / 298 (10) |
| 39-geothermal-reservoir | 4 / 189 (2) | 4 / 237 (1) | 4 / 287 (1) |
| 40-catastrophe-losses | 4 / 225 (4) | 4 / 254 (6) | 4 / 299 (11) |
| 41-shift-planning-time-zones | 4 / 295 (4) | 4 / 366 (4) | 4 / 426 (3) |
| 42-token-ledger | 4 / 452 (5) | 4 / 429 (4) | 4 / 525 (4) |
| 43-passive-components | 4 / 527 (4) | 4 / 593 (5) | 4 / 694 (5) |
| 44-vaccine-cold-chain | 4 / 491 (3) | 4 / 401 (5) | 4 / 445 (10) |
| 45-healthcare-claims | 4 / 197 (2) | 4 / 213 (3) | 4 / 226 (3) |
| 46-network-acl | 4 / 641 (1) | 4 / 536 (1) | 4 / 649 (1) |
| 47-international-catalogue | 4 / 621 (5) | 4 / 600 (3) | 4 / 723 (5) |
| 48-tax-rules-engine | 4 / 475 (2) | 4 / 522 (2) | 4 / 497 (4) |
| 49-clinical-trial-crf | 4 / 418 (6) | 4 / 500 (6) | 4 / 489 (6) |
| 50-fx-settlement | 4 / 370 (2) | 4 / 322 (3) | 4 / 317 (3) |
| **Total** | **14 002 (184)** | **14 069 (186)** | **15 796 (231)** |

Cela fait 43 867 cas issus de 25 254 méthodes de test, répartis sur 150 suites, 0 en échec, 601 ignorés. Toutes les sessions ont noté la fluidité 4/5, donc la moyenne est de 4,00 — voir le [§2](#2-limites-de-cet-essai), point 3, avant d'en tirer quoi que ce soit. Pour 01 à 20, la suite `Tests` du premier essai est conservée à côté des trois nouvelles suites et n'est pas comptée ici.

---

## 2. Limites de cet essai

À lire avant les constats. Aucune ne remet en cause un verdict, puisque chaque verdict a été reproduit indépendamment ; elles changent ce que les décomptes signifient.

1. **Le banc d'essai a gonflé un constat.** Les espaces de travail n'avaient pas le `global.json` du dépôt, et la consigne prescrivait `dotnet test --project`. Ce commutateur n'existe qu'en mode Microsoft.Testing.Platform, si bien que presque toutes les sessions ont rencontré l'opt-in dès leur premier lancement. Le manque de documentation est réel ([DOCS-1](#docs-1)) ; sa fréquence, 148 constats dans 50 domaines, est un artefact. Le même banc d'essai faisait écrire à la main le `.csproj` des sessions à partir d'un modèle, ce qui explique que tant d'entre elles aient rencontré le prérequis de restauration ([CLI-2](#cli-2)) avant que le `dotnet add package` documenté ne l'aurait restauré pour elles.
2. **Les fichiers `MODEL.md` de 01 à 20 ont été écrits à partir du code pour cet essai**, et ils décrivent sans les juger des comportements surprenants — `$` qui accepte un saut de ligne final, par exemple. Cela a orienté les sessions vers ces défauts de domaine : la famille du saut de ligne final apparaît dans 19 de ces 20 domaines.
3. **Les 150 sessions ont toutes noté la fluidité 4/5.** Une note qui ne varie jamais ne mesure rien. Elle est rapportée, et aucune conclusion n'en est tirée.
4. **Les instructions de mainteneur du dépôt ont été chargées dans chaque session.** Les consignes demandaient aux sessions d'agir en consommatrices, mais une partie du vocabulaire du projet (numéros d'ADR, sections de la spec) a pu se glisser dans leur lecture des pages utilisateur.
5. **L'isolation a failli.** Plusieurs sessions ont écrit des fichiers temporaires hors de leur propre espace de travail, dans la racine de travail partagée. Rien n'indique qu'une session ait lu le travail d'une autre, mais on ne peut pas l'exclure.
6. **Le redémarrage d'un conteneur a interrompu 6 sessions.** Leurs espaces de travail ont été réinitialisés et elles sont reparties de zéro.
7. **Une suite a échoué quand le coordinateur l'a lancée depuis le dépôt.** Dans la suite de l'expert de 49, `A_death_cannot_be_reported_before_the_onset_of_an_event_already_logged` a échoué sur la graine −2085749253 : elle affirmait qu'aucun décès n'était consigné alors que l'événement journalisé avait été tiré au grade 5, qui est lui-même un décès. Le grade n'était pas un dummy pour cette assertion ; le tirage l'a montré, et la graine l'a rejoué. Le grade est désormais épinglé à 3, et [NOTES.expert.md](domains/49-clinical-trial-crf/NOTES.expert.md) le consigne. Les 12 exécutions répétées de la session n'avaient pas rencontré cette combinaison.
8. **Les défauts de domaine n'ont pas été vérifiés indépendamment** ([§9](#9-défauts-de-domaine-trouvés-par-les-sessions)). Chaque session rapporte les avoir démontrés par un test conservé en `Skip = "domain defect…"`.

---

## 3. Par profil

Sur les 994 constats portant sur la bibliothèque, l'outil et la documentation, 335 viennent des sessions de découverte, 305 des sessions d'habitué et 354 des sessions d'expert. Les trois profils ont rencontré les mêmes éléments de tête ([DOCS-1](#docs-1), [CLI-1](#cli-1), le retypage de la regex) ; ce qui diffère, c'est la queue.

### Découverte : ce qui fait trébucher un développeur qui découvre la bibliothèque

- **Démarrage.** La classe d'entrée `extension(Any)` du guide de démarrage n'a ni prose, ni note sur C# 14, ni renvoi à `any generate --entry-point any` (9 constats, tous de découverte : [DOCS-10](#docs-10)). Aucune page ne nomme l'espace de noms `JustDummies` avant la ligne 295 ([DOCS-11](#docs-11)). La restauration dont `any` a besoin n'est pas documentée ([CLI-2](#cli-2), 23 de ses 46 constats).
- **Liens.** Les nouveaux venus ont cherché un lien sur la page de la CLI et n'ont pas trouvé les formes `.Then(...)` qui vivent dans le guide de composition ([DOCS-2](#docs-2), 21 de ses 45 constats).
- **Analyseurs.** Les pages disent que les règles « start working on your next build » ; une règle Info n'affiche jamais rien dans une compilation en console, si bien qu'un nouveau venu ne peut pas distinguer « rien à signaler » de « non chargée » ([DOCS-9](#docs-9)).
- **Les liens morts** du bundle de documentation (10 constats) sont l'œuvre du banc d'essai et sont réfutés ([§6](#6-réfutés-et-non-reproduits)).

### Habitué : ce qui ralentit un utilisateur régulier

- **Chirurgie manuelle sur les générateurs liés.** Dériver un paramètre d'un autre dans `Generate()` laisse ses épingles compiler et ne rien faire ([CLI-1](#cli-1)) ; certains ont écrit à la main un test `ReferenceEquals(_x, _xRecipe)`, d'autres un champ « épinglé » nullable, pour distinguer une épingle d'un tirage.
- **Modifications mécaniques.** Le retypage de la regex en quatre ou cinq endroits par fichier (par conception, [§5](#5-candidats-à-une-décision)), les remarques périmées qu'il laisse ([CLI-15](#cli-15)), l'ajout d'un point d'entrée plus tard seulement au travers de `--force` ([CLI-17](#cli-17)).
- **Les transmissions au constructeur de base** (`: base(message)`) ont bloqué trois sessions d'habitué, sans aucune garde à lire ([CLI-25](#cli-25)).
- **Les exceptions d'un composeur** remontent enveloppées dans `AnyGenerationException`, délibérément et sans qu'une page utilisateur le dise ([DOCS-23](#docs-low)).

### Expert : ce qui limite un artisan du code

- **Tester des refus au travers d'un générateur.** Une valeur qui enfreint une règle est une épingle, et un lien que le générateur impose doit être contourné avec le constructeur ; aucune page ne le dit ([DOCS-8](#docs-8), 9 de ses 13 constats sont d'expert). L'épingle à null demande un cast ([DOCS-3](#docs-3), 12 de ses 20 constats sont d'expert).
- **La ligne de graine sur des tests qui n'ont rien tiré** ([XU-1](#xu-1), 12 de ses 19 constats sont d'expert) : les experts l'ont remarquée parce qu'ils lisent chaque sortie d'échec.
- **Passer toutes les règles JD en avertissement.** Le scaffold déclenche alors lui-même JD030/JD036, délibérément ([DOCS-5](#docs-5), 10 de ses 17 constats sont d'expert), et sur une paire ordonnée JD036 annonce un tirage de limite qui n'a jamais lieu ([CLI-20](#cli-20)).
- **Des pièces de composition manquantes** : une exclusion d'ensemble sur `IAny<T>` ([CORE-7](#core-7)), un tirage en échelle logarithmique, une paire ordonnée sur un value object comparable (les deux sont des décisions).

---

## 4. Par composant

Chaque élément donne sa nature et sa gravité (telles que réévaluées par le vérificateur), son verdict, l'endroit où il a été rencontré (domaine avec profils, puis les identifiants des constats de session, `<domaine><D|R|E>-<n>`), ce qui se passe, une reproduction minimale, sa relation aux rapports précédents et la recommandation. Les éléments de faible valeur sont réunis dans un tableau compact à la fin de chaque composant ; aucun n'est écarté.

### Core

#### CORE-1 — Deux épingles sur une paire ordonnée sont acceptées ou refusées selon l'ordre des appels
<a id="core-1"></a>
- **Nature / gravité :** bug, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 37 (R). Constat 37R-5.
- **Ce qui se passe :** `WithFirst(v)` sans seconde épingle vérifie `v` par rapport à la base du second côté ; avec une seconde épingle présente, il ne vérifie que l'ordre. `WithSecond` en est le miroir. La première épingle appliquée est donc vérifiée par rapport à une base que la seconde épingle remplace ensuite, et les deux mêmes épingles réussissent dans un ordre et échouent dans l'autre. Les deux sens échouent : des épingles au-dessus des deux bases échouent quand `WithFirst` vient en premier, des épingles en dessous des deux bases quand c'est `WithSecond`.
  ```
  WithFirst(2030-01-25).WithSecond(2030-06-01): ConflictingAnyConstraintException: Cannot apply WithFirst(01/25/2030) because no second value lies above 01/25/2030: Between(2020-01-01, 2029-12-31) already requires values less than or equal to 2029-12-31.
  WithSecond(2030-06-01).WithFirst(2030-01-25): OK -> (01/25/2030, 06/01/2030)
  int WithSecond(-10).WithFirst(-20): ConflictingAnyConstraintException … Between(0, 100) already requires values greater than or equal to 0.
  int WithFirst(-20).WithSecond(-10): OK -> (-20, -10)
  ```
  Le scaffolder aggrave les choses : la paire générée est reconstruite à chaque appel `With…`, toujours `WithFirst` avant `WithSecond`, si bien qu'au travers d'un générateur scaffoldé aucun ordre d'appel ne fonctionne. La session a contourné le générateur avec le constructeur.
- **Reproduction minimale :**
  ```csharp
  AnyOrderedPair<DateOnly> pair = Any.OrderedPair(
      Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31)),
      Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31)), strict: true);
  pair.WithFirst(new DateOnly(2030, 1, 25)).WithSecond(new DateOnly(2030, 6, 1));            // throws
  pair.WithSecond(new DateOnly(2030, 6, 1)).WithFirst(new DateOnly(2030, 1, 25)).Generate(); // (2030-01-25, 2030-06-01)
  ```
- **Pourquoi c'est un bug :** une fois les deux côtés épinglés, `Generate()` renvoie les deux littéraux et ne lit jamais aucune des bases, donc la déclaration est satisfiable. Un refus à cet endroit est un faux conflit, non une contradiction. Le propre test de la bibliothèque (`AnyOrderedPairTests`, lignes 99–100) affirme l'indépendance vis-à-vis de l'ordre, mais seulement pour des épingles à l'intérieur de la base. Aucune mauvaise valeur n'est jamais tirée : l'échec est bruyant, à la ligne d'arrange.
- **Antécédent :** nouveau. `Any.OrderedPair` est arrivé après REPORT-2 (ADR-0126).
- **Recommandation :** rendre le résultat indépendant de l'ordre des appels. Choisir lequel est une décision pour l'ADR-0126 : soit les épingles sont des littéraux et une épingle isolée n'est vérifiée par rapport à l'autre base que tant que ce côté reste non épinglé (en différant la vérification, ce qui correspond à « une valeur épinglée est un littéral »), soit chaque épingle est toujours vérifiée par rapport à l'autre base, ce qui fait que les ordres acceptés ci-dessus sont refusés aussi. Ajouter les cas hors base, dans les deux ordres, à côté du test existant, et mettre à jour la page de composition (EN/FR), qui ne décrit qu'une épingle isolée.

#### CORE-2 — Un ordre assorti d'un écart maximal est scaffoldé en une paire ordonnée qui refuse chaque tirage
<a id="core-2"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** partiel
- **Rencontré dans :** 03 (D, R), 04 (D, R, E), 07 (D, R, E), 11 (D, R, E), 15 (D, R, E), 19 (D, R, E), 37 (D), 44 (D). Constats 03D-2, 03R-2, 04D-8, 04R-7, 04E-2, 07D-4, 07R-2, 07E-4, 11D-4, 11R-4, 11E-1, 15D-5, 15R-7, 15E-2, 19D-4, 19R-3, 19E-2, 37D-6, 44D-2.
- **Ce qui se passe :** pour `start >= end` plus `end.DayNumber - start.DayNumber > 31`, le scaffolder émet `Any.OrderedPair(_startRecipe, _endRecipe, strict: true)` sur deux recettes `Any.DateOnly()` non bornées et marque les deux `to verify … ties end to start`. Une fois les sentinelles supprimées, le constructeur a refusé 1 000 graines sur 1 000, et épingler l'un ou l'autre côté n'a rien changé. `AnyOrderedPair<T>` n'expose que `WithFirst`, `WithSecond` et `Generate`, il ne peut donc pas porter un écart.
  ```
  start: unread guard at BillingPeriod.cs, line 7: if (end.DayNumber - start.DayNumber > 31) { ... — ties end to start
  ✓ AnyBillingPeriod.cs — 2 of 2 parameters inferred, 2 to verify.
  ```
- **Reproduction minimale :** `BillingPeriod(DateOnly start, DateOnly end)` avec les deux gardes ci-dessus ; `any generate BillingPeriod` ; supprimer les deux paires de TODO ; tirer 1 000 fois.
- **Pourquoi partiel :** la compilation s'arrête sur le lien par conception (spec §5.3 ; l'ADR-0126 restreint la paire à un ordre). Une modification qui conserve les épingles existe avec l'API livrée — une réécriture de 18 lignes du `StartAndEnd()` généré avec `.Then` et une borne `AfterOrEqualTo`/`BeforeOrEqualTo` par côté a tenu 1 000 tirages sur 1 000 en gardant les deux épingles — mais aucune page ne la montre.
- **Antécédent :** le reliquat de REPORT-2 CLI-8, traité pour les ordres seulement.
- **Recommandation :** la documentation d'abord : un exemple « ordre plus écart maximal » sous « When a guard ties two parameters » (EN/FR, compilé). Ensuite, petit changement dans le train cli : quand un paramètre déjà présent dans une paire tirée a une autre garde non lue qui lie les deux mêmes paramètres, le dire dans le récapitulatif et le TODO (« the order is drawn by the pair; this guard is not ») et renvoyer le TODO vers `StartAndEnd()`. Une paire qui porte un écart est la « relation autre qu'un ordre » que l'index des ADR nomme comme condition de reprise ; elle demande une décision.

#### CORE-3 — Les messages de conflit d'une paire ordonnée nomment `WithFirst`/`WithSecond` et affichent une date épinglée en `MM/dd/yyyy`
<a id="core-3"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 06 (R), 37 (E), 42 (E), 44 (R). Constats 06R-11, 37E-8, 42E-5, 44R-7.
- **Ce qui se passe :** un `WithMaxCelsius(-40m)` scaffoldé est refusé en `Cannot apply WithSecond(-40) because no first value lies below -40 …`, un appel que l'utilisateur n'a jamais écrit. Les valeurs épinglées passent par `AnyDerivation.Display` au format invariant « G », si bien qu'une même phrase mélange `03/10/2026` et une borne ISO `2024-12-31` ; les fractions de seconde sont perdues, donc deux épingles distinctes peuvent s'afficher de la même façon et le message paraît se contredire. C'est identique sous toutes les cultures.
- **Reproduction minimale :**
  ```csharp
  Any.OrderedPair(Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2024, 12, 31)),
                  Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2024, 12, 31)), strict: true)
     .WithFirst(new DateOnly(2026, 3, 10));
  // Cannot apply WithFirst(03/10/2026) because no second value lies above 03/10/2026: Between(2015-01-01, 2024-12-31) …
  ```
- **Antécédent :** nouveau.
- **Recommandation :** afficher les valeurs temporelles dans `AnyDerivation.Display` avec le format aller-retour « O » que les générateurs utilisent pour leurs bornes (un correctif simple ; `DateOnly`/`TimeOnly` derrière `NET8_0_OR_GREATER` ; `Display` sert aussi `Combine`, `DifferentFrom` et les collections, donc mettre à jour toute sonde qui fige ces messages). Nommer le membre scaffoldé à la place de `WithFirst` touche la surface publique — une surcharge d'`OrderedPair` portant les noms des côtés, ou un `With…` qui reformule le message — et relève d'une décision.

#### CORE-4 — Une épingle `Local` sur une paire `DateTime` ordonnée est refusée ou acceptée selon l'ordre des épingles
<a id="core-4"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** partiel
- **Rencontré dans :** 14 (D). Constat 14D-4.
- **Ce qui se passe :** `WithFirst(local)` tant que l'autre côté est tiré vérifie l'autre côté au travers de `After(local)`, que l'ADR-0114 refuse pour une sorte `Local` ; le refus nomme `instant`. Épinglée dans l'autre sens, la même valeur `Local` est acceptée. Refuser tant que l'autre côté est tiré est honnête (ADR-0114) ; dépendre de l'ordre ne l'est pas.
- **Antécédent :** nouveau ; ancêtre REPORT.md CORE-C7.
- **Recommandation :** décider d'une seule règle avec [CORE-1](#core-1) : refuser une épingle `Local` sur une paire `DateTime` quoi que contienne l'autre côté, ou la traiter comme une valeur fournie. Nommer le côté de la paire plutôt que `instant` dans le refus.

#### CORE-5 — `Any.StringMatching` lit un caractère astral comme deux unités UTF-16, en silence
<a id="core-5"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** partiel
- **Rencontré dans :** 47 (D, R). Constats 47D-1, 47R-2, 47R-3.
- **Ce qui se passe :** un quantificateur placé après un littéral astral ne répète que le substitut bas ; une classe qui en contient un tire des substituts isolés (10 tirages sur 12) ; une plage de classe astrale est refusée avec les substituts bruts affichés, qu'une console UTF-8 montre comme deux U+FFFD.
  ```
  \U00020000{3}: len=4 units=[D840 DC00 DC00 DC00] wellFormed=False realRegexMatches=True
  ```
  Chaque tirage a passé le vrai moteur .NET, c'est donc l'ADR-0027 qui fonctionne : sous la sémantique .NET, le langage du motif contient bien ces chaînes. Ce qui est fautif, c'est le silence. `WithChars` refuse un substitut (ADR-0075), et le message de `WithoutChars` affirme que « no draw ever yields half of an astral code point ».
- **Antécédent :** nouveau.
- **Recommandation :** ne pas traiter les paires comme des atomes (cela casse l'ADR-0027). Indiquer sur la page des chaînes (EN/FR) qu'un motif est lu en unités de code UTF-16 et qu'un caractère astral s'écrit comme un groupe, `(\U00020000){3}`. Afficher les substituts en `\uXXXX` dans le refus de plage. En option, étendre la famille JD014 à un motif constant dont l'analyse sépare une paire.

#### CORE-6 — `DifferentFrom` sur un générateur qui ne produit qu'une valeur donne un message hésitant
<a id="core-6"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 27 (D). Constat 27D-2.
- **Ce qui se passe :** `Any.Fixed(s).DifferentFrom(s)` épuise son budget de 1 000 tirages et rapporte « Could not draw a value different from …: 1000 draws in a row produced it », alors que l'indication de cardinalité sait déjà que le domaine est vide. La valeur est échappée comme l'exige l'ADR-0075 (le constat sur des caractères de contrôle bruts est réfuté). Le test portait sur le refus de signaux égaux, donc la valeur n'était pas un dummy ; la session est passée au constructeur.
- **Recommandation :** quand la cardinalité de l'opérande est 1 et qu'elle contient la valeur exclue, refuser avant la boucle avec un message net (« the generator yields only that value »).

#### Core, version compacte
<a id="core-low"></a>

| Id | Élément | Nature, verdict | Rencontré dans | Recommandation |
|---|---|---|---|---|
| CORE-7 <a id="core-7"></a> | `IAny<T>` n'a qu'un `DifferentFrom` à valeur unique ; exclure un ensemble revient à le chaîner | idée, partiel | 31 (E), 50 (E) : 31E-7, 50E-4 | Documenter la voie constructive au travers du composant (`WithValue(v => v.Except(a, b))`), vérifiée à la déclaration. Un `Except(IEnumerable<T>)` sur `IAny<T>` amende l'ADR-0121. Chaque builder typé a déjà `Except(IEnumerable<T>)`, ce qui clôt REPORT-2 CORE-2. |
| CORE-8 <a id="core-8"></a> | Pas d'`Any.BigInteger()`, et aucune page ne mentionne `BigInteger` | idée, partiel | 42 (R, E) : 42R-2, 42E-2 | Documenter la composition en une ligne `Any.ArrayOf(Any.Byte()).WithCount(32).As(b => new BigInteger(b, isUnsigned: true))` et une forme à plage fermée. Une fabrique de premier rang demande une valeur par défaut bornée pour un côté ouvert, donc un ADR d'abord (l'audit du 2026-07-20 la liste). Les builders d'environ 60 lignes des sessions venaient de la recette manquante, non d'une capacité manquante. |
| CORE-9 <a id="core-9"></a> | Aucun moyen documenté de tirer un membre d'énumération non défini pour un test de refus | idée, partiel | 17 (R) : 17R-9 | Un paragraphe sur la page enums-and-choices : `Any.Int32().Except(Enum.GetValues<T>().Select(…))`, qui reste non défini quand un membre est ajouté. Pas `AnyEnum<T>.Undefined()` : il contredit le contrat du type et soulève une question sur `[Flags]`. |

### CLI (`any`)

#### CLI-1 — Un paramètre dérivé dans `Generate()` garde des épingles qui compilent et ne sont pas honorées
<a id="cli-1"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 01 (R), 02 (R, E), 05 (D, R), 07 (D, E), 08 (R, E), 09 (D), 10 (R), 11 (D, R, E), 12 (D, R, E), 13 (D, E), 14 (D), 16 (E), 17 (E), 18 (D), 20 (D, R, E), 21 (R, E), 22 (D), 24 (D, E), 25 (D, R, E), 26 (E), 28 (D, R, E), 29 (R, E), 30 (D, R, E), 31 (R, E), 32 (R, E), 34 (D, R, E), 35 (D), 36 (D, R, E), 38 (D, R, E), 39 (D), 40 (D, R, E), 41 (R), 42 (D, E), 43 (R), 44 (R), 45 (D, R, E), 46 (R), 47 (R, E), 48 (D, R), 49 (D, R, E), 50 (D, R). Constats 01R-2, 02R-8, 02E-2, 05D-4, 05R-3, 07D-5, 07E-5, 08R-8, 08E-2, 09D-3, 10R-3, 11D-3, 11R-5, 11E-2, 12D-5, 12R-3, 12E-2, 13D-3, 13E-3, 14D-5, 16E-8, 17E-6, 18D-4, 20D-8, 20R-2, 20E-5, 21R-4, 21E-5, 22D-4, 24D-4, 24E-2, 25D-10, 25R-6, 25E-5, 26E-3, 28D-6, 28R-3, 28E-5, 29R-3, 29E-7, 29E-8, 30D-1, 30R-3, 30E-3, 31R-2, 31E-3, 31E-4, 32R-4, 32E-5, 34D-5, 34R-2, 34E-2, 35D-5, 36D-2, 36R-6, 36E-4, 38D-3, 38R-5, 38R-6, 38E-10, 39D-6, 40D-1, 40R-8, 40E-2, 41R-6, 42D-3, 42E-3, 43R-6, 44R-8, 45D-3, 45R-6, 45E-6, 46R-5, 47R-5, 47E-3, 48D-2, 48R-4, 49D-6, 49R-3, 49E-4, 50D-3, 50D-4, 50R-3.
- **Ce qui se passe :** le récapitulatif nomme un lien avec précision (`ties kind to quantity`) ; la page de la CLI conseille de dériver un côté dans `Generate()`. Après cette modification :
  ```
  WithKind(Sms).WithQuantity(5)                 -> Kind=Sms Quantity=1   (the pin is ignored)
  Assert.Throws(… WithKind(Sms).WithQuantity(5)) -> No exception was thrown (a contradictory pin is repaired, not refused)
  WithKind(Sms).WithQuantity(q => q.GreaterThan(100)) -> Quantity=1
  derived Generate(), WithQuantity(5): pin ignored on 102 of 300 seeds
  per-parameter Generate(), WithQuantity(5): constructor threw on 103 of 300 seeds
  ```
  La compilation est propre et aucune règle JD ne se déclenche. Aucune couture publique ne distingue un paramètre épinglé d'un paramètre tiré ; les sessions ont écrit à la main des drapeaux `_xPinned` ou testé `ReferenceEquals(_x, _xRecipe)`, un artefact non documenté de la disposition des champs. Pour un ordre direct, l'outil émet déjà un champ nullable `_{param}Pin` et le respecte ; pour tout autre lien, il n'émet que `_x = Any.Fixed(value)`.
- **Reproduction minimale :** `UsageRecord(UsageKind kind, long quantity, …)` avec `if (kind == UsageKind.Sms && quantity != 1) throw` ; `any generate UsageRecord` ; écrire `long quantity = kind == UsageKind.Sms ? 1L : _quantity.Generate();` dans `Generate()` ; puis `Any.UsageRecord().WithKind(UsageKind.Sms).WithQuantity(5).Generate()`.
- **Pourquoi c'est important :** une contrainte déclarée au point d'appel que le générateur n'honore pas en silence peut donner un faux vert, et une épingle contradictoire qui devrait atteindre le refus du constructeur est réparée à la place. L'avertissement de la page (lignes 746–756) énonce la conséquence, puis conseille « Pin both, or write the tie in `Generate()` » — c'est la seconde moitié qui déconnecte l'épingle. Sa formulation, « the test then depends on the seed », présente en outre le résultat comme celui du test ; ce qu'un échec montre là, c'est un générateur qui n'honore pas sa propre déclaration.
- **Antécédent :** REPORT-2 CLI-8, confirmé ; traité depuis pour les ordres seulement. Deuxième essai de suite ; la friction la plus récurrente de celui-ci.
- **Recommandation :** étendre l'enregistrement d'épingle de la paire ordonnée à chaque paramètre que le récapitulatif signale déjà « ties X to Y » : émettre le `_{param}Pin` nullable (positionné par la surcharge de valeur, effacé par la surcharge d'affinage, porté par le constructeur de copie), de sorte qu'un `Generate()` écrit à la main puisse lire `if (_quantityPin is { } q) …`. Cela n'exige aucun solveur et n'élargit aucune borne : l'outil connaît déjà le lien et émet déjà cette forme dans un cas. Faire nommer ce champ par le TODO du paramètre lié. Sur la page de la CLI (EN/FR), ajouter un exemple compilé discriminant-plus-charge utile qui honore une épingle présente et refuse une épingle contradictoire, et reformuler « depends on the seed ».

#### CLI-2 — La restauration dont `any` a besoin n'est pas documentée, et un lot non restauré est rapporté comme le refus de son premier type
<a id="cli-2"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 34 domaines — 01 (R), 02 (D, E), 03 (D), 04 (D), 07 (D, E), 08 (E), 09 (D), 11 (D, R, E), 12 (D), 13 (D, R), 14 (R), 15 (D), 16 (R), 17 (D, R, E), 18 (D), 19 (E), 20 (D, R), 21 (E), 22 (R), 23 (R), 28 (D, R), 29 (D), 30 (D), 32 (D, R), 33 (D), 34 (D), 36 (D, R), 37 (R), 39 (D, R), 40 (R), 42 (D), 43 (R), 46 (D), 47 (D). 46 constats, de 01R-3 à 47D-9.
- **Ce qui se passe :**
  ```
  any generate Alpha Beta Gamma --dry-run
  ✗ Alpha: this project has not been restored, so its package references cannot be read.
    Restore it, then run the command again: dotnet restore
  (exit 1; nothing for Beta or Gamma; no closing summary)
  ```
  Sous `--format json`, `results` contient une entrée pour `Alpha` avec `summary.failed: 1` ; les autres arguments manquent. L'arrêt anticipé est délibéré et testé (« said once ») ; étiqueter un fait sur le projet comme le refus d'un seul type ne l'est pas, et la page utilisateur dit que les types d'un lot sont « processed independently ». Les chemins où le projet ne se charge pas rapportent déjà un `refusal` au niveau de l'exécution.
- **Pourquoi partiel :** le message lui-même est clair (le correctif de REPORT.md CLI-B7 tient) ; « exit 2 » est réfuté (le code de sortie est 1, comme le dit la spec) ; une restauration automatique est refusée par conception (spec §7). La fréquence tient surtout au `.csproj` écrit à la main par le banc d'essai ([§2](#2-limites-de-cet-essai)).
- **Recommandation :** une phrase dans « Requires » de la page de la CLI (EN/FR) : le projet doit être restauré. Rapporter « not restored » (et « library not referenced ») comme un refus au niveau de l'exécution sur stderr et dans le `refusal` de premier niveau du JSON ; énoncer l'arrêt anticipé dans la spec §7 et à côté de « processed independently ».

#### CLI-3 — Une ligne à virgule flottante bornée d'un seul côté atteint la limite du type un tirage sur dix, et le récapitulatif dit seulement `guard`
<a id="cli-3"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 01 (E), 06 (D), 07 (D, R), 09 (R), 10 (D, E), 11 (D), 13 (R), 14 (R), 15 (D, R), 16 (D), 21 (D, R, E), 29 (R), 30 (D, E), 31 (R), 33 (E), 38 (D), 40 (D, R, E), 48 (R, E), 49 (D, E), 50 (R). Constats 01E-14, 06D-2, 07D-6, 07R-4, 09R-5, 10D-6, 10E-6, 11D-2, 13R-3, 14R-5, 15D-9, 15R-12, 16D-5, 21D-5, 21R-9, 21E-6, 21E-7, 29R-5, 30D-7, 30E-4, 31R-8, 33E-9, 38D-5, 40D-8, 40R-10, 40E-5, 48R-11, 48E-12, 49D-7, 49E-8, 50R-9.
- **Ce qui se passe :** `ThrowIfNegative(nightlyRate)` est scaffoldé en `Any.Decimal().GreaterThanOrEqualTo(0m)  guard`, avec `remark: null` dans le JSON. Selon l'ADR-0117, le côté ouvert atteint `decimal.MaxValue` un tirage sur dix, si bien que l'arithmétique du domaine sur un tirage neuf déborde dans environ 8 à 10 % des cas. Le seul signal est JD036, en Info. Une ligne de date non gardée a une dernière colonne vide.
- **Pourquoi partiel :** le tirage est délibéré et documenté (page des nombres ; la spec dit que le moteur n'inventera pas de plage pour faire taire JD036), et c'est le tirage qui fait son travail : plusieurs domaines ont ainsi appris qu'une grandeur n'avait pas de plafond ([§9](#9-défauts-de-domaine-trouvés-par-les-sessions)). Non tenu : les dates débordent environ au rapport du décalage sur l'étendue (`+90 days` jamais en 20 000 tirages) ; les plages monétaires couvrant tout le domaine (48, 50) et les lignes entières ont d'autres causes.
- **Recommandation :** laisser le tirage tel quel et le faire remonter par le canal `remark` existant (les remarques de taille de #361) : une phrase sous les lignes pour une ligne à virgule flottante dont un côté est ouvert (« one draw in ten reaches double.MaxValue (ADR-0117); declare its range in `AnyValid…()` if the domain computes with it — JD036 »). Formuler une éventuelle remarque sur les dates comme une portée atteinte, non comme un débordement fréquent. Va de pair avec [DOCS-5](#docs-5).

#### CLI-4 — Un helper de garde `void` du même type n'est jamais lu
<a id="cli-4"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 01 (E), 02 (R, E), 03 (E), 04 (D, R), 05 (D), 08 (R, E), 12 (D, R, E), 13 (D, E), 14 (E), 16 (E), 18 (D, R, E), 28 (D, R, E), 45 (D). Constats 01E-12, 02R-5, 02E-4, 03E-2, 04D-7, 04R-8, 05D-3, 08R-4, 08E-6, 12D-2, 12R-2, 12E-4, 13D-2, 13E-2, 14E-4, 16E-10, 18D-3, 18R-8, 18E-3, 28D-5, 28R-4, 28E-7, 45D-5.
- **Ce qui se passe :** changer uniquement le type de retour du helper inverse le résultat.
  ```
  void Validate(amount):    amount  decimal  Any.Decimal()  to verify, unread guards
                            amount: unread guard at Price.cs, line 7: Validate(amount);
  decimal Validate(amount): amount  decimal  Any.Decimal().Positive().LessThan(PriceReturning.Ceiling)  guard
  ```
  Un paramètre passé mais non gardé dans le helper est bloqué lui aussi ; une garde que la recette satisfait déjà (`!double.IsFinite`) ne coûte rien en ligne mais bloque au travers du helper ; le TODO cite l'appel, jamais une règle à l'intérieur.
- **Reproduction minimale :** `public Price(decimal amount) { Validate(amount); Amount = amount; }` avec `private static void Validate(decimal a) { if (a <= 0m) throw …; if (a >= Ceiling) throw …; }`, à comparer au même corps qui renvoie `a`.
- **Antécédent :** REPORT.md CLI-G4 (toujours ouvert) ; REPORT-2 CLI-14 corrigé pour les helpers qui renvoient une valeur (#407, #416), dont le texte notait déjà le cas `void`.
- **Recommandation :** cet essai est le retour de terrain de l'ADR-0085. Étendre la lecture sur un niveau de #416 à un helper `void` du même type, sous le même critère (méthode ordinaire, non virtuelle, à corps de bloc, dans le même arbre, sans `ref`/`out`), en la repliant comme un constructeur délégué. Le résultat étant écarté, il n'y a aucune normalisation à distinguer, donc cela supprime une asymétrie sans ajouter de mécanisme. Ce n'est pas un bug — la compilation s'arrête au lieu de tirer de mauvaises valeurs.

#### CLI-5 — Les `throw` d'une même instruction composée partagent un seul pointeur
<a id="cli-5"></a>
- **Nature / gravité :** défaut, moyenne · **Verdict :** partiel (la partie comptée est documentée), confirmé pour le cas de la boucle
- **Rencontré dans :** 07 (R), 10 (R), 16 (D), 17 (E), 24 (D, E), 26 (D, R, E), 30 (R), 36 (D, E), 39 (E), 45 (R, E), 46 (R). Constats 07R-3, 10R-4, 16D-3, 17E-4, 24D-1, 24E-1, 26D-2, 26D-4, 26R-1, 26E-1, 26E-2, 30R-4, 36D-3, 36E-2, 39E-3, 45R-5, 45E-8, 46R-6.
- **Ce qui se passe :** nommer la première garde non lue et compter les autres (« …, and 1 more », #373) est la réponse documentée à REPORT-2 CLI-3. Mais `MarkIfItRejects` marque par instruction de tête : quand un bloc `if` ou une boucle contient plusieurs `throw`, chaque paramètre qu'il touche reçoit un seul endroit, le premier `throw`. Ainsi :
  ```
  cow: unread guard at Session.cs, line 12: if (last > startedAt) ...
  settings: unread guard at Session.cs, line 12: if (last > startedAt) ...      (line 17, the only guard on settings, is never named)
  items: … line 7 (a for loop with two throwing ifs; JSON "further": 0)
  network: unread guard at Prefix.cs, line 11: if (hostBits != 0) { ... }     (no "ties": the tie goes through a local)
  ```
  Un paramètre que seul le second `throw` teste est renvoyé vers une ligne qui ne le mentionne pas, il n'y a pas de « and N more », et un lien passant par une variable locale (`hostBits`, `produces`, `last`) n'est jamais signalé. Après suppression des deux lignes TODO sur la garde nommée, un générateur `Cow` a été refusé 416 fois sur 2 000 tirages.
- **Antécédent :** le reliquat de REPORT-2 CLI-3 et proche de REPORT-2 CLI-6.
- **Recommandation :** marquer un endroit par `throw`, à l'instruction englobante la plus interne de ce `throw`, pour les paramètres que ses propres parties décisives atteignent (y compris au travers de copies), de sorte que `Further` compte chaque `throw`. Joindre la note « ties » chaque fois que les parties décisives d'un `throw` atteignent deux paramètres ou plus, comme le promet la spec §5.6. Ajouter des lignes de fichiers de référence (golden) ou de corpus pour les formes Session, Row, Prefix et Curve.

#### CLI-6 — Des sentinelles sur des gardes que la recette par défaut satisfait déjà
<a id="cli-6"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 02 (D), 03 (D, E), 06 (D), 11 (D, E), 20 (R, E), 26 (R), 44 (E), 45 (R), 46 (R, E), 49 (D). Constats 02D-7, 03D-4, 03E-3, 06D-6, 11D-5, 11E-3, 20R-4, 20E-6, 26R-4, 44E-12, 45R-4, 46R-7, 46E-6, 49D-3.
- **Ce qui se passe :** `at.Kind != DateTimeKind.Utc`, `at.Offset != TimeSpan.Zero`, `!Enum.IsDefined` dans une boucle ou derrière `HasValue &&`, et les gardes `&&`/`switch` d'énumération qui listent chaque membre reviennent toutes en `to verify, unread guards` ; avec les sentinelles supprimées, chacune a donné 0 refus sur 2 000 tirages. L'utilisateur n'apprend que la recette était la bonne que par une autre page.
- **Pourquoi partiel :** spécifié (l'ensemble fermé du §5.3 ; une garde « contains no `&&` » et n'est pas sous une boucle ; ADR-0083). Les cas de sous-ensemble (un `switch` qui couvre certains membres) refusent bien environ un tirage sur trois, donc ils doivent rester marqués.
- **Recommandation :** un retour de terrain pour l'ADR-0085 sur des lignes exactes : `Kind != Utc` → aucune (comme `!double.IsFinite`) ; `Offset != Zero` → `.WithOffset(TimeSpan.Zero)` ; un `switch` d'énumération ou une chaîne de `!=` qui couvre chaque membre déclaré → aucune. Laisser les formes dans une boucle et `&&` sur un nullable à la règle de placement existante.

#### CLI-7 — Le récapitulatif et le JSON ne disent jamais que deux paramètres ont été tirés ensemble
<a id="cli-7"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 06 (R, E), 22 (D), 23 (D), 24 (E), 25 (D), 29 (R), 32 (D, R), 34 (D, R). Constats 06R-5, 06E-3, 22D-2, 23D-3, 24E-5, 25D-8, 29R-4, 32D-6, 32R-6, 34D-4, 34R-4.
- **Ce qui se passe :**
  ```
  minCelsius  decimal  Any.Decimal().GreaterThanOrEqualTo(-40m)  guard
  maxCelsius  decimal  Any.Decimal().LessThanOrEqualTo(40m)      guard
  ✓ AnyTemperatureRange.cs — 2 of 2 parameters inferred.
  ```
  Le fichier les tire au travers d'`Any.OrderedPair(…, strict: true)`. Le moteur détient l'appariement (`ScaffoldPlan.Pairings`) mais seul l'émetteur le lit. Quand l'ordre est la seule garde, la ligne se lit `Any.Int32()  guard` : une étiquette `guard` sans aucun resserrement visible, au regard de la propre définition du mot par la page. La spec §6 appelle le récapitulatif « the mechanism that keeps the tool honest about what it inferred ».
- **Recommandation :** afficher les appariements : une ligne par paire sous les lignes (`minCelsius, maxCelsius: drawn together through Any.OrderedPair (minCelsius < maxCelsius, line 7)`), un champ `pairedWith` dans le JSON, le mot dans le tableau de la page de la CLI et dans la spec §6 (EN/FR), un test du récapitulatif à côté de `OrderedPairTests`, et toute sonde ux-trial qui fige un récapitulatif.

#### CLI-8 — Une borne tenue dans un champ `static readonly` n'est pas lue
<a id="cli-8"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 04 (E), 08 (D), 16 (R), 19 (D, R), 20 (D, R, E), 48 (E). Constats 04E-7, 08D-4, 16R-4, 19D-2, 19R-4, 20D-7, 20R-5, 20E-4, 48E-7.
- **Ce qui se passe :** `ThrowIfGreaterThan(maximumLifetime, HardMaximumLifetime)` avec `static readonly TimeSpan HardMaximumLifetime = TimeSpan.FromHours(24)` reste `to verify` ; la même garde écrite `TimeSpan.FromHours(24)` en ligne est lue. Même un `static readonly int` n'est pas lu. Le TODO n'en donne aucune raison, contrairement à celui de la regex.
- **Pourquoi partiel :** pour 5 des 9 membres (bornes `Half` et `DateOnly`), `static readonly` n'est pas la cause : aucune écriture d'une borne `Half` ou date n'est lue du tout.
- **Recommandation :** le dire sur la page de la CLI (EN/FR) : un champ `static readonly` n'est pas une constante ; les bornes `TimeSpan` ne sont lues qu'en ligne ; les bornes `Half` et date ne sont jamais lues. Suivre un `static readonly` dont l'unique écriture est un initialiseur que le lecteur évalue déjà exactement est une décision candidate pour l'ADR-0085.

#### CLI-9 — L'indication de regex est perdue pour un champ `new(...)` à type cible et pour `Match(...).Success`
<a id="cli-9"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** partiel
- **Rencontré dans :** 02 (D), 04 (R), 09 (D), 15 (R), 27 (E), 32 (E), 43 (E), 49 (E). Constats 02D-3, 04R-9, 09D-6, 15R-10, 27E-2, 32E-7, 43E-4, 49E-6.
- **Ce qui se passe :** le motif est cité, avec le retypage vers `AnyPattern`, pour `Regex.IsMatch(p, "…")`, `[GeneratedRegex]` et un champ construit avec `new Regex("…")`. Il est perdu pour un champ construit avec un `new("…")` à type cible (02D-3, 27E-2, 32E-7), parce que `Guards.PatternOf` ne reconnaît que `ObjectCreationExpressionSyntax` — un écart par rapport à la propre phrase de la spec §5 (« that of a `Regex` field built from a constant »), sans test sur le cas du champ. Il est aussi perdu pour `Match(...).Success`, la moitié de REPORT-2 CLI-13 que #374 n'a jamais close, et pour une conjonction ou un helper.
- **Reproduction minimale :** `private static readonly Regex Format = new("^[A-Z]{1,2}[0-9]{1,4}$");` et `if (!Format.IsMatch(value)) throw …` → le TODO n'a pas de motif ; `new Regex("…")` → il en a un.
- **Recommandation :** reconnaître `BaseObjectCreationExpressionSyntax` (sur le plancher Roslyn 4.8.0) et ajouter les lignes de champ à `GuardReadingTests` ; étendre `RegexPatternIn` à `Match(p).Success`, y compris une variable locale affectée depuis `Match`. 09D-6 relève de [CLI-4](#cli-4).

#### CLI-10 — Une garde derrière un appel que l'outil ne suit pas laisse la ligne non marquée
<a id="cli-10"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** partiel
- **Rencontré dans :** 05 (D, R, E), 33 (D), 48 (D, R, E), 50 (D). Constats 05D-2, 05R-2, 05E-2, 33D-4, 48D-1, 48R-1, 48E-1, 50D-2.
- **Ce qui se passe :** le récapitulatif ne montre aucune marque et dit « N of N parameters inferred » ; le fichier compile et le constructeur refuse la plupart des tirages. En 48, une classe `Guard` commune à tout le domaine a laissé `label` en `Any.String()` nu et `IncomeKind` en `Any.Enum<IncomeKind>()`, qui tire `None` environ une fois sur cinq. En 50, une date de transaction un week-end a été scaffoldée en un simple `Any.DateOnly()`.
- **Pourquoi partiel :** un validateur d'un autre type, une méthode de calcul d'un autre objet et un opérateur sur un agrégat sont documentés comme silencieux (« What the tool still cannot see stays silent » ; spec §9, ADR-0085). La forme incohérente est celle de 05 : un `Validate()` sans paramètre du même type, appelé sous la première affectation, est silencieux, alors que trois formes voisines sont marquées.
- **Recommandation :** pour 05, une décision : un appel à une méthode du même type avec source dont le corps lève une exception, placé sous la première affectation, marque chaque paramètre `to verify`, comme le fait l'ADR-0110 pour `if (TooBig()) throw` — cela n'ajoute que des refus. Les formes d'un autre type sont des non-objectifs du §9 ; lire un helper statique d'un autre type de la même compilation est une option à argumenter au titre de l'ADR-0085/0046, non un correctif.

#### CLI-11 — Un type du framework sans fabrique de la bibliothèque est composé en un `new AnyX()` à l'aveugle
<a id="cli-11"></a>
- **Nature / gravité :** défaut, moyenne · **Verdict :** partiel
- **Rencontré dans :** 32 (R, E), 42 (D, R, E), 47 (E). Constats 32R-3, 32E-8, 42D-1, 42D-2, 42R-1, 42E-1, 47E-5.
- **Ce qui se passe :**
  ```
  baseUnits  BigInteger   new AnyBigInteger()   AnyX, constraint unavailable
  fromEndMva Complex      new AnyComplex()      AnyX
  culture    CultureInfo  new AnyCultureInfo()  AnyX
  ```
  La compilation échoue alors sur CS0246, ce qui masque la sentinelle `TODO_verify` du fichier pour le `baseUnits < 0` non lu. L'étape suivante documentée, `any generate BigInteger`, ne fonctionne pas pour deux des trois types.
- **Pourquoi partiel :** l'appel à l'aveugle est spécifié (spec §5.2, §5.4 : « CS0246, or a clean build, is the only verdict ») ; « no TODO sentinel » est réfuté — elle est là, masquée. Mais la prémisse de l'ADR-0089 est « a domain type of the developer's own », et le « any other non-generic named type » de la spec est plus large que cela.
- **Recommandation :** une décision qui restreint l'ADR-0089 : traiter un type de métadonnées sans ligne dans la table de base comme un type abstrait — une sentinelle du §5.5, un mot dans le récapitulatif (`framework`), et un TODO qui nomme un `AnyBigInteger : IAny<BigInteger>` écrit à la main ou `Any.Int64().As(v => new BigInteger(v))` plutôt que `any generate`. Sinon, documenter le cas sur la page de la CLI et dans la spec §5.4.

#### CLI-12 — `ThrowIfLessThan(a, b)` entre deux paramètres n'est pas apparié, alors que `if (a < b) throw` l'est
<a id="cli-12"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 10 (D, R, E), 17 (D, R, E). Constats 10D-4, 10R-2, 10E-2, 17D-7, 17R-4, 17E-3.
- **Ce qui se passe :**
  ```
  issuedOn       DateOnly  Any.DateOnly()  to verify, unread guards
  effectiveFrom  DateOnly  Any.DateOnly()  to verify, unread guards
  issuedOn: unread guard at RecallNotice.cs, line 5: ArgumentOutOfRangeException.ThrowIfLessThan(effectiveFrom, issuedOn);
  ```
  Le second argument du helper doit être une constante de compilation, « the same as a comparison's other side » — une analogie devenue caduque quand la ligne de paire a fait d'un paramètre un autre côté légitime. L'outil lit une écriture et refuse son équivalent exact, ce que la spec elle-même appelle « the worst of both outcomes ». Un test fige le comportement actuel.
- **Recommandation :** lire les helpers à deux arguments comme des ordres quand les deux arguments sont des paramètres distincts d'un même type non nullable pour lequel la bibliothèque a une paire : `ThrowIfLessThan(a, b)` → `OrderedPair(b, a, strict: false)`, `…OrEqual` → `strict: true`, en miroir pour `GreaterThan`. Remplacer le test qui fige le comportement par une assertion d'appariement et ajouter une ligne de corpus.

#### CLI-13 — Un ordre nié entre deux paramètres n'est pas apparié
<a id="cli-13"></a>
- **Nature / gravité :** friction, basse · **Verdict :** confirmé
- **Rencontré dans :** 31 (D, R, E). Constats 31D-2, 31R-3, 31E-2.
- **Ce qui se passe :** `!(high > low)` reste `to verify`, même sur `int`, alors que le lecteur à un paramètre lit `!(p > N)` ; `high <= low || double.IsInfinity(high)` est apparié. `TryOrdering` ne déballe pas une négation logique.
- **Recommandation :** accepter une `LogicalNotExpression` autour d'une comparaison des deux paramètres dans `TryOrdering`, en faisant correspondre la sorte au moyen du `Negated(SyntaxKind)` existant.

#### CLI-14 — Un helper dont la borne est passée en argument n'est lu que pour la borne écrite à l'intérieur
<a id="cli-14"></a>
- **Nature / gravité :** friction, basse · **Verdict :** confirmé
- **Rencontré dans :** 21 (D, R, E), 22 (D, R, E). Constats 21D-4, 21R-3, 21E-2, 22D-3, 22R-3, 22E-2.
- **Ce qui se passe :** `Row = Guard(row, MaxPlane, nameof(row))` avec `if (double.IsNaN(value) || value < Min || value > max) throw` donne `Any.Double().GreaterThanOrEqualTo(Spacing.Min)  to verify, guard, unread guards` pour `row` comme pour `slice` ; chaque TODO cite la même ligne du helper, non l'appel qui tient sa propre borne. Aucune constante du point d'appel n'est substituée à un paramètre du helper. Sans danger — chaque paramètre est marqué et la compilation s'arrête. La variante `void` en 21 est documentée.
- **Recommandation :** pointer la marque vers le point d'appel, ou l'y ajouter (`value > max — max is the helper's parameter, given MaxPlane at line 10`) ; une phrase sur la page de la CLI et dans la spec §5.6 (EN/FR). Mettre à jour les fichiers de référence (golden) et toute sonde qui fige le message.

#### CLI-15 — Remplacer une base scaffoldée est un retypage à la main, et il laisse les remarques de `With…(Func)` nommer l'ancien type
<a id="cli-15"></a>
- **Nature / gravité :** friction / défaut, basse · **Verdict :** partiel (retypage), confirmé (remarques)
- **Rencontré dans :** 12 (E), 15 (R), 28 (D, E), 30 (D), 31 (R), 35 (R), 44 (R), 47 (R), 48 (D). Constats 12E-5, 15R-9, 28D-7, 28E-6, 30D-3, 31R-4, 35R-3, 44R-11, 47R-6, 48D-8.
- **Ce qui se passe :** passer à `AnyPattern` ou `IAny<T>` oblige à retaper la fabrique, le champ de recette, le constructeur de recette et le `Func` de la surcharge d'affinage ; chaque endroit manqué est une erreur de compilation (l'affirmation qu'un endroit manqué compile est réfutée). Le retypage que prescrivent le TODO et la page laisse derrière lui `/// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> …` ; le compilateur ne peut pas le détecter, et un rechercher-remplacer à l'aveugle transforme le texte échappé en XML invalide.
- **Recommandation :** écrire la remarque sans le type concret du builder (« a lambda of the type of `configure` »), pour qu'elle ne puisse pas se périmer ; mettre à jour les fichiers de référence (golden) et l'exemple de la spec. Élargir le paragraphe de la page sur `IAny<T>` au-delà d'`IAny<string>` avec une ligne sur les collections. Le retypage lui-même est voulu (voir la ligne regex du [§5](#5-candidats-à-une-décision)).

#### CLI-16 — Un prédicat de classe de caractères dans une lambda n'est pas lu, et son TODO ne donne aucune direction
<a id="cli-16"></a>
- **Nature / gravité :** friction, basse · **Verdict :** confirmé
- **Rencontré dans :** 30 (D, E), 35 (E), 37 (E), 40 (D). Constats 30D-2, 30E-2, 35E-10, 37E-12, 40D-7.
- **Ce qui se passe :** `!code.All(c => c is >= 'A' and <= 'Z')`, `!siteCode.All(char.IsAsciiLetterUpper)` et `annualLosses.Any(x => x < 0m)` ne sont pas lus, sans danger ; le TODO dit seulement « verify against … ». Une regex reçoit un motif cité et une indication ; ici, rien, alors que `code.AsSpan().IndexOfAnyExcept("AB…Z") >= 0` est aujourd'hui lu comme `.WithChars(...)`.
- **Recommandation :** une direction dans le TODO pour un prédicat de classe de caractères (`.WithChars("…")` ou `.Alpha().InUpperCase()` gardent le type `AnyString`), la réécriture `IndexOfAnyExcept` dans « A tip that works today » de la page, et ces lignes en retour de terrain pour l'ADR-0085.

#### CLI-17 — Ajouter un point d'entrée à un type scaffoldé demande `--force`, qui écrase le générateur modifié
<a id="cli-17"></a>
- **Nature / gravité :** friction, basse · **Verdict :** confirmé
- **Rencontré dans :** 08 (D), 18 (R), 30 (R), 35 (D), 37 (R). Constats 08D-5, 18R-2, 30R-6, 35D-2, 37R-7.
- **Ce qui se passe :** `any generate Accession --entry-point any` après une modification à la main : `✗ …/AnyAccession.cs already exists. Re-run with --force to overwrite it. Whatever you changed in it is lost.` (code de sortie 1, aucun `.Entry.cs` écrit). Le générateur qu'il aurait écrit est identique octet pour octet au premier scaffold. « Land together or not at all » (spec §7, ADR-0070) vise le cas du demi-scaffold ; ajouter plus tard le seul fichier d'entrée manquant complète un scaffold et ne touche jamais au fichier du développeur.
- **Recommandation :** documenter la voie non destructive (`--output <scratch>` en ne gardant que le fichier d'entrée ; ou décider dans `any.json` avant le premier scaffold) et la nommer dans le refus à côté de `--force`. N'écrire que le fichier d'entrée quand le générateur existe amende la spec §7 et l'ADR-0070 : une décision.

#### CLI-18 — Une garde sur un membre d'un paramètre n'est pas lue, et le TODO ne renvoie pas au correctif documenté
<a id="cli-18"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 09 (E), 11 (E), 23 (D, R), 42 (D). Constats 09E-4, 11E-4, 23D-4, 23R-5, 42D-5.
- **Ce qui se passe :** `if (maximumPairKinship.Value > 0.25) throw` → `new AnyKinshipCoefficient()  to verify, AnyX, unread guards`. Non lue par conception (spec §5.3 ; l'ADR-0046 refuse la propagation). « No reusable fix » est réfuté : « Where the valid base lives » montre exactement ce cas (`return new AnyRoute().WithDomestic(true);`), et un affinage nommé dans la classe partielle est la voie de réutilisation.
- **Recommandation :** étendre « A tip that works today » à une règle de membre, avec un lien vers « Where the valid base lives » ; en option, faire nommer par le TODO les méthodes `With…` du générateur composé (une indication, sans lecture).

#### CLI-19 — Un champ ajouté à côté d'un générateur scaffoldé est réinitialisé par le `With…` généré suivant
<a id="cli-19"></a>
- **Nature / gravité :** documentation, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 21 (D), 28 (R), 36 (D), 45 (R). Constats 21D-2, 28R-2, 36D-7, 45R-7.
- **Ce qui se passe :** le constructeur de copie généré ne copie que les champs scaffoldés, et chaque `With…` commence par `new AnyX(this)`. Un `private bool _expedited;` ajouté dans `AnyShipment.Custom.cs` avec une méthode `Expedited()` à la forme copier/écrire/renvoyer de la page est réinitialisé en silence par le `With…` généré suivant. La compilation est propre. L'en-tête dit « members you add in a neighbouring file survive » — vrai du code source, non de la valeur d'un champ.
- **Recommandation :** le dire dans « Adding the overload yourself » (EN/FR) avec un exemple compilé : exprimer la variante au travers d'un champ existant (une recette épinglée avec `Any.Fixed`), appliquer une étape de cycle de vie en dernier avec `.As(…)`, ou ajouter la ligne au constructeur de copie en sachant que `--force` la supprime. Envisager de nuancer la ligne de l'en-tête.

#### CLI-20 — Une paire ordonnée scaffoldée de recettes à virgule flottante bornées d'un seul côté déclenche JD036 alors que la paire borne les deux extrémités
<a id="cli-20"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 06 (D, E), 22 (E), 44 (E). Constats 06D-3, 06E-2, 22E-4, 44E-9.
- **Ce qui se passe :** `TemperatureRange` est scaffoldé en `Any.Decimal().GreaterThanOrEqualTo(-40m)` et `Any.Decimal().LessThanOrEqualTo(40m)` liés par `Any.OrderedPair`. Passée en avertissement, JD036 signale les deux lignes avec « one draw in ten spans the whole type », ce qui est faux dans ce contexte : la paire resserre chaque tirage par la borne de l'autre. JD036 raisonne sur une seule expression par conception, et la spec excuse JD036 dans le code émis ; la raison de l'excuse (« a range the domain never stated ») ne tient pas pour une paire.
- **Recommandation :** dire sur la page de la CLI, à côté du paragraphe sur JD030, que dans une paire la borne de l'autre côté plafonne déjà le tirage et que `Between(lo, hi)` sur chaque côté l'énonce. Émettre la borne opposée sur chaque membre est une décision (spec §5.3/§17).

#### CLI-21 — Passer un générateur préparé à un `With…` scaffoldé donne un CS1503 nu
<a id="cli-21"></a>
- **Nature / gravité :** friction, basse · **Verdict :** confirmé
- **Rencontré dans :** 13 (E), 17 (D, R), 42 (R). Constats 13E-6, 17D-10, 17R-8, 42R-6.
- **Ce qui se passe :** `WithRoute(domestic)` avec `domestic` un `AnyRoute` → `CS1503 cannot convert from AnyRoute to Route`, sans diagnostic JD. L'erreur de compilation est voulue (ADR-0109) ; les remarques et le paragraphe de page ajoutés depuis (#391) ont été lus par deux des quatre sessions, qui ont tout de même buté dessus. « Library `With…` » est réfuté : aucun `With…` de la bibliothèque n'a cette paire.
- **Recommandation :** une décision entre une règle JD sur la résolution de surcharge en échec (nommant les deux voies : affiner avec `x => x.…`, ou épingler `prepared.Generate()`) et une surcharge `[Obsolete(error: true)] With…(G generator)` émise.

#### CLI-22 — `p.Scale > N` sur un decimal est hors de l'ensemble du lecteur de gardes
<a id="cli-22"></a>
- **Nature / gravité :** idée, basse · **Verdict :** confirmé
- **Rencontré dans :** 02 (D, R), 12 (D, E). Constats 02D-5, 02R-6, 12D-3, 12E-3.
- **Ce qui se passe :** `if (kwh.Scale > 3) throw` garde `Any.Decimal().GreaterThanOrEqualTo(0m)  to verify, guard, unread guards` ; `decimal.Round(kwh, 3) != kwh` donne `.WithScale(3)  guard`. Sans l'échelle, 179 938 tirages sur 200 000 ont une Scale > 3. Une ligne `.Scale` reposerait sur le fait que les valeurs tirées ont une Scale ≤ N sous `WithScale(N)` — vrai aujourd'hui au travers de `Math.Round`, non promis par l'ADR-0015, et faux pour une valeur fournie (`OneOf(1.0000m).WithScale(3)` garde une Scale de 4).
- **Recommandation :** soit amender l'ADR-0015 (ou un nouvel ADR) pour promettre une Scale ≤ N pour les valeurs tirées et ajouter la ligne, figée par un test de propriété ; soit laisser la ligne de côté et faire suggérer `.WithScale(N)` par le TODO.

#### CLI-23 — `any generate` ne dit jamais que d'autres constructeurs ont été laissés de côté
<a id="cli-23"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** partiel
- **Rencontré dans :** 46 (D, R, E). Constats 46D-3, 46R-2, 46E-3.
- **Ce qui se passe :** `Prefix(V4 network, int length)` et `Prefix(V6 network, int length)` sont à égalité de largeur ; le premier dans l'ordre du source est retenu. Le récapitulatif affiche `constructor Prefix(V4, int)` et « 2 of 2 parameters inferred » ; rien ne mentionne `Prefix(V6, int)`, et le générateur ne peut jamais atteindre la moitié de l'espace de valeurs du type — un tirage qui ne pourra jamais exposer une dépendance à IPv6. Le plus large d'abord est spécifié et testé ; `--ctor` est différé (spec §16).
- **Recommandation :** rendre la limite bruyante : une ligne du récapitulatif nommant les constructeurs laissés de côté et pourquoi (`not scaffolded: Prefix(V6, int) — widest first, ties by source order`), la même liste dans le JSON. Savoir s'il faut refuser une égalité de largeur comme on refuse des fabriques à égalité (§5.1.2) est une décision.

#### CLI-24 — Un lien écrit dans `Generate()` en affinant une partie composée abandonne une épingle que le test avait posée à l'intérieur de cette partie
<a id="cli-24"></a>
- **Nature / gravité :** friction, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 08 (E), 22 (R). Constats 08E-3, 22R-2.
- **Ce qui se passe :** `Dosage dosage = _dosageRecipe.WithUnit(u => u.Except(route.Forbidden…)).Generate();` part de la recette du test mais remplace une unité que le test avait épinglée dans `Dosage`, en silence et seulement sur certaines graines. La règle (un affinage après une épingle la remplace) est spécifiée (ADR-0116, Proposed) ; le « Two consequences follow » de la section sur les liens ne mentionne jamais le cas composé, et l'auteur ne voit pas l'épingle imbriquée.
- **Recommandation :** ajouter la troisième conséquence sur la page de la CLI (EN/FR) et montrer les deux formes qui conservent une épingle imbriquée (tirer d'abord le côté que le test peut épingler, puis affiner l'autre).

#### CLI-25 — Un paramètre transmis à un constructeur de base du framework est marqué comme une garde non lue
<a id="cli-25"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 23 (R), 36 (R), 42 (R). Constats 23R-4, 36R-5, 42R-7.
- **Ce qui se passe :** chaque paramètre qui atteint `: base(message)` ou `: base($"… {a} … {b}")` sur un type d'exception reçoit `to verify, unread guards` et bloque le fichier, sans garde ni explication.
- **Recommandation :** une décision sur la voie de l'ADR-0085, la plus étroite d'abord : lire les constructeurs d'exception du framework (`Exception(string)` et leurs semblables) comme non rejetants.

#### CLI-26 — Le scaffolder écrit un type par son nom court même là où il est ambigu
<a id="cli-26"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 44 (R). Constat 44R-6.
- **Ce qui se passe :** `TypeNames.cs` écrit chaque type de paramètre par son nom court et ajoute un `using` pour son espace de noms, sans vérifier les autres espaces de noms que le fichier ouvre. Avec l'alias propre au domaine, le `Shipment` émis échoue avec CS0104, contrairement à la remarque selon laquelle le nom court et son `using` forment une seule décision.
- **Recommandation :** avant d'écrire, vérifier chaque nom court par rapport à tous les espaces de noms ouverts et qualifier en cas de collision.

#### CLI, version compacte
<a id="cli-low"></a>

| Id | Élément | Nature, verdict | Rencontré dans | Recommandation |
|---|---|---|---|---|
| CLI-27 | Un ordre à trois paramètres (min < opt < max) est marqué, non tiré (par conception, ADR-0126) ; le décompte ne dit pas que la garde supplémentaire lie des paramètres | friction, partiel | 21 (R, E) : 21R-2, 21E-4, 21E-16 | « , and 1 more, which ties a, b and c », ou nommer d'abord une garde qui lie. |
| CLI-28 | `Math.Round(double, N) != p` n'est pas lu, avec un TODO générique ; aucune page ne montre un double en centièmes | friction, partiel | 17 (D, R, E) : 17D-8, 17R-6, 17E-5 | Le rapporter comme `constraint unavailable`, avec un TODO nommant le remplacement (`Any.Int32().Between(…).As(k => k / 100d)`, ou un decimal avec `WithScale(2)` converti par `.As`) ; une phrase sur la page des nombres. `WithScale` sur `AnyDouble` est une décision. |
| CLI-29 | `p % N != 0` n'est pas lu, et l'astuce de réécriture ne renvoie pas à `MultipleOf`/`Even` | idée, partiel | 18 (D, R), 25 (E) : 18D-5, 18R-3, 25E-3 | Une phrase dans « A tip that works today » ; une ligne est une décision de l'ADR-0085. |
| CLI-30 | Un scaffold de lot bloque tout le projet tant que chaque fichier `to verify` n'est pas résolu | friction, partiel | 29 (E), 43 (E) : 29E-9, 43E-6, 43E-7 | Garder le blocage (ADR-0083) ; conseiller de scaffolder les types que les tests tirent, les agrégats d'abord. |
| CLI-31 | Une fabrique statique à plusieurs arguments est refusée sans nommer la fabrique proche ni la voie écrite à la main | friction, confirmé | 10 (D, R, E) : 10D-3, 10R-5, 10E-3 | Nommer `Of(int, int)` et la voie écrite à la main dans le refus ; une fabrique à plusieurs arguments traitée comme un constructeur est une décision candidate. |
| CLI-32 | Une constante publique niée est repliée en littéral : `Between(-999999999999999m, Money.MaxMagnitude)` | friction, confirmé | 50 (D, R) : 50D-10, 50R-8 | Voir à travers un moins unaire et émettre `-Money.MaxMagnitude`. |
| CLI-33 | Le récapitulatif d'un lot s'achève sans la liste des fichiers qui ne compileront pas encore | friction, confirmé | 39 (E), 42 (R) : 39E-6, 42R-8 | Étendre le résumé de clôture de #379 pour les nommer. |
| CLI-34 | Supprimer les deux lignes documentées laisse une ligne vide après l'accolade | friction, confirmé | 22 (R), 40 (D) : 22R-6, 40D-6 | Supprimer la ligne vide que l'émetteur écrit après la sentinelle ; mettre à jour les fichiers de référence (golden). |
| CLI-35 | Une garde citée qui se termine par `;` reçoit `;;` dans le TODO et `;,` dans le récapitulatif | défaut, confirmé | 21 (E), 30 (D) : 21E-3, 30D-4 | Faire dépendre le séparateur de la façon dont la ligne citée se termine. |
| CLI-36 | Les ensembles fermés de chaînes (`is not ("FR" or "BE")`) restent `Any.String()` sans rien qui renvoie à `OneOf` | idée, confirmé | 14 (D, E) : 14D-2, 14E-2 | Faire nommer `Any.String().OneOf(…)` par le TODO. |
| CLI-37 | Le bloc TODO(any) de l'en-tête reste une fois la dernière recette acceptée | friction, confirmé | 23 (E) : 23E-4 | Faire dire au bloc qu'il doit se supprimer lui-même quand il ne reste plus de TODO. |
| CLI-38 | Une énumération `[Flags]` est scaffoldée en un simple `Any.Enum<T>()`, sans renvoi à `AllowingCombinations()` | friction, confirmé | 46 (E), 48 (R) : 46E-5, 48R-6 | Garder le défaut (ADR-0020) ; ajouter une remarque dans le récapitulatif nommant l'opt-in. |
| CLI-39 | Le TODO de regex ne dit pas que `StringMatching` abandonne la garde de longueur que portait la base | documentation, confirmé | 46 (E) : 46E-7 | Nommer dans le TODO les contraintes abandonnées. |
| CLI-40 | « Also answers to that name » nomme des types du framework imbriqués ou statiques que `any` ne pourrait jamais scaffolder | défaut, confirmé | 19 (R, E) : 19R-5, 19E-4 | Exclure les classes statiques de `Nameable` ; trancher pour les types imbriqués au §3.2. |
| CLI-41 | Le CS0103 de la sentinelle nomme le paramètre, non la raison | idée, confirmé | 09 (D) : 09D-4 | Documenter comment relister les gardes ouvertes (`--format json`) ; un identifiant plus riche est une décision sur l'ADR-0060. |
| CLI-42 | Les fichiers scaffoldés sont longs (1 247 lignes pour 7 types, ~60 % de tuyauterie) | idée, confirmé | 17 (E) : 17E-8 | Une décision contre le « one shape for every parameter » de l'ADR-0109 (`--minimal`/`--with`). |
| CLI-43 | Sur un projet non restauré, un type atteint par une référence transitive est « no type by that name » | défaut, confirmé | 45 (R) : 45R-2 | Afficher le refus « not restored » quand `!restored` et que le type n'est pas trouvé. |
| CLI-44 | La ligne de relance de l'en-tête omet `--output`, `--namespace`, `--entry-point-namespace` | défaut, confirmé | 33 (E) : 33E-7 | Répéter chaque option qui a façonné le fichier, `--output` relatif au projet. |
| CLI-45 | Une garde sur une valeur calculée à partir d'un paramètre est marquée sans note | défaut, partiel | 35 (D, R) : 35D-3, 35R-2 | Garder le refus (propagation, ADR-0046) ; donner une note à la marque issue du suivi des copies. |
| CLI-46 | Une branche `StartsWith(const)` d'une garde `\|\|` reste non lue sans qu'aucun remède soit nommé | friction, partiel | 20 (E) : 20E-3 | Documenter `.StartingWith(…)` plus `.WithChars(…)` pour un préfixe plus une boucle de caractères ; pas d'indication `StringMatching`. |
| CLI-47 | Les remarques de taille citent « (ADR-0076) » comme un numéro nu | friction, partiel | 20 (D) : 20D-9 | Remplacer par un lien vers la page utilisateur, ou retirer le numéro. |
| CLI-48 | La sentinelle d'un paramètre nullable est dans `AnyValid…()`, mais son `null` vient de `.OrNull()` dans le constructeur privé | friction, partiel | 33 (E) : 33E-3 | Garder D10 ; pour une garde qui porte sur la nullité, faire dire par le TODO que cette fabrique ne tire jamais `null` et que le `null` du défaut vient de `.OrNull()` dans le constructeur. |
| CLI-49 | Idée : une copie épinglée en un appel d'une valeur existante (`AnyX.Like(x)`) | idée, partiel | 47 (R) : 47R-11 | Une recette documentée, non un scaffold (relire un paramètre depuis une propriété est une heuristique). |
| CLI-50 | La documentation des épingles porte la clause « one shared instance for a reference type » aussi pour les types valeur | documentation, partiel | 05 (E) : 05E-6 | Retirer la clause pour les primitives, les énumérations et `string`. |

### Analyseurs

#### AN-1 — JD012 signale un ensemble de générateurs que `Then(g => g)` aplatit, le remède que son propre message nomme
<a id="an-1"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** partiel
- **Rencontré dans :** 36 (R, E), 48 (D, R). Constats 36R-8, 36E-5, 48D-3, 48R-2, 48R-3.
- **Ce qui se passe :**
  ```
  warning JD012: Call Generate() on each pooled generator, or choose one with Then: Any.ElementOf inferred a pool of recipes, so drawing from it yields a recipe rather than a value
      <- Any.ElementOf(leaves).Then(leaf => leaf)
  ```
  À l'exécution, cette forme tire des valeurs, réparties à peu près également, et se rejoue depuis une seule graine. L'analyseur ne regarde jamais comment l'ensemble est utilisé ; la section « What it does not flag » de la page JD012 et ses tests sont muets sur ce cas. Les sous-affirmations sont réfutées : `Any.Boolean().Then(b => b ? x : y)` est le choix propre documenté, `Any.Int32().Between(0, n - 1).Then(i => generators[i])` est propre pour n branches, et `IAny<out T>` est covariant, donc aucun cast vers le haut n'est nécessaire.
- **Antécédent :** REPORT-2 CORE-12, traité par `.Then` (ADR-0119) ; le faux positif est nouveau.
- **Recommandation :** ne pas signaler un ensemble `OneOf`/`ElementOf` qui est le receveur de `Then` avec un résultat qui n'est pas un générateur ; ajouter le test dans les deux sens et le cas à la page JD012 (EN/FR). Si la règle reste syntaxique, reformuler le message pour que « choose one with Then » signifie tirer un index ou un drapeau.

#### AN-2 — Un `null` constant dans `Any.OneOf<T?>(…)` compile sans diagnostic et lève toujours une exception
<a id="an-2"></a>
- **Nature / gravité :** idée, basse · **Verdict :** confirmé
- **Rencontré dans :** 46 (D, R, E). Constats 46D-5, 46R-8, 46E-8.
- **Ce qui se passe :** `Any.OneOf<byte?>(null, (byte)6, (byte)17)` et `Any.OneOf("a", null)` (T inféré en `string?`) compilent proprement et lèvent `ArgumentException: The values must not contain a null element; use OrNull() …` — à la déclaration, non à `Generate()` comme les sessions le croyaient. JD014 confie `null` à l'analyse de nullabilité, qui ne peut pas voir un `T` nullable. `ArgumentGuardFacts.MayRefuse` reconnaît déjà le cas et ne le signale jamais.
- **Recommandation :** signaler un élément `null` constant en ligne dans la ligne `OneOf` de JD014, avec la formulation propre de la garde ; restreindre la ligne « left to nullable analysis » de la page JD014 (EN/FR) ; ajouter des tests unitaires.

#### AN-3 — Avec JD011 activée, JD005 et JD011 signalent toutes deux un générateur concaténé dans une chaîne
<a id="an-3"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 16 (E). Constat 16E-11.
- **Ce qui se passe :** `Any.String().Generate() + Any.Int32().Between(1, 5)` et `s += Any.Int32()…` déclenchent JD005 et JD011 sur le même intervalle, parce que `string + object` est une conversion implicite que JD011 signale.
- **Antécédent :** la même classe que REPORT.md AN-D1, corrigée alors pour JD014/JD017.
- **Recommandation :** laisser JD005 posséder les formes de rendu en texte (ADR-0078, « one mistake draws one diagnostic ») : JD011 retourne tôt quand le parent de la conversion est un `+` ou `+=` de chaîne.

#### AN-4 — JD036 dit « floating-point dummy » sur les chaînes `Any.Decimal()`
<a id="an-4"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 13 (E), 31 (E), 37 (D). Constats 13E-4, 31E-6, 37D-7.
- **Ce qui se passe :** `This floating-point dummy declares no range: one draw in ten spans the whole type` sur `Any.Decimal().Negative()`. .NET lui-même appelle `decimal` un type à virgule flottante, donc le texte n'est pas faux ; mais les propres pages de la bibliothèque distinguent `Decimal` de « the floating-point builders », et deux sessions s'y sont arrêtées.
- **Recommandation :** nommer le type sur lequel la règle s'est déclenchée (`This {0} dummy declares no range…`), en gardant alignés le titre du catalogue, son résumé, le message du test unitaire et le changelog. L'identifiant et la gravité ne changent pas.

#### AN-5 — JD009 ne suit pas un initialiseur statique dans un helper qui tire
<a id="an-5"></a>
- **Nature / gravité :** idée, basse · **Verdict :** confirmé
- **Rencontré dans :** 25 (R). Constat 25R-7.
- **Ce qui se passe :** `private static readonly int Inline = Any.Int32()….Generate();` déclenche JD009 ; `private static readonly int ViaHelper = Fixtures.Rate(1);`, dont le corps tire, non. La règle est lexicale par conception ; le danger est réel.
- **Recommandation :** une puce « What it does not flag » sur les pages JD009 et JD007 (EN/FR) : un tirage à l'intérieur d'une méthode que l'initialiseur appelle n'est pas suivi ; renvoyer plutôt le générateur depuis le helper.

### Adaptateur xUnit

#### XU-1 — `[Reproducible]` rapporte une graine pour un test en échec qui n'a rien tiré
<a id="xu-1"></a>
- **Nature / gravité :** défaut, basse · **Verdict :** confirmé
- **Rencontré dans :** 02 (D), 04 (E), 07 (D), 09 (E), 23 (E), 27 (E), 33 (E), 37 (R), 41 (D, E), 44 (D, E), 45 (E), 46 (R, E), 48 (E), 49 (D). Constats 02D-13, 04E-4, 07D-9, 09E-5, 23E-7, 27E-7, 33E-8, 37R-11, 41D-5, 41E-5, 44D-6, 44E-14, 45E-10, 46R-9, 46E-9, 48E-4, 49D-8. (17E-9 et 40E-7 avaient été regroupés ici mais décrivent des tests qui ont bien tiré ; la ligne y est juste et doit rester.)
- **Ce qui se passe :**
  ```
  failed SeedReportTests.Literal_only_failure (0ms)
    Assert.Equal() Failure: Values differ
    [JustDummies] These arbitrary values were seeded with 2028111925. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 2028111925)].
  ```
  Aucun appel `Any.*` n'a été exécuté. La propre règle de l'adaptateur est de ne jamais nommer une graine qu'aucun tirage n'a utilisée : un niveau extérieur « prints nothing, since replaying its seed would reproduce nothing », et une portée supplantée reste silencieuse pour la même raison. Le cas sans tirage relève de cette règle et n'est pas traité. Sous `[assembly: Reproducible]`, un corps enveloppé dans `Any.Reproducibly(...)` affiche deux lignes de graine, dont une nomme une graine que rien n'a utilisée. Des sessions qui consignaient un défaut de domaine trouvé avec un littéral ont été envoyées chercher une valeur tirée qui n'existait pas.
- **Reproduction minimale :** `[assembly: Reproducible]` et `[Fact] public void Literal_only_failure() { Assert.Equal(1, 2); }`.
- **Antécédent :** nouveau.
- **Recommandation :** ne rapporter que si la source de la portée a servi à tirer. `SeededRandom` compte déjà les tirages (ADR-0049), mais l'adaptateur ne peut pas lire ce compte : il faut un petit ajout public sur la poignée d'`Any.UseSeed`, ou une autorisation d'accès aux internes — un amendement de l'ADR-0017, donc à passer par une décision. Appliquer la même règle à `Any.Reproducibly`. D'ici là, une phrase sur la page de l'adaptateur (EN/FR).

#### XU, version compacte
<a id="xu-low"></a>

| Id | Élément | Nature, verdict | Rencontré dans | Recommandation |
|---|---|---|---|---|
| XU-2 <a id="xu-2"></a> | La ligne de rejeu ne dit pas à quel niveau placer l'attribut avec graine | idée, partiel | 29 (E) : 29E-11 | Chaque emplacement naturel rejoue (méthode, classe, l'attribut d'assembly modifié) ; un second attribut d'assembly échoue avec CS0579. Formulation optionnelle : « …by adding [JustDummies.Xunit.Reproducible(Seed = N)] to the failing test », avec le guide de reproductibilité (EN/FR) et toute sonde. |
| XU-3 <a id="xu-3"></a> | Aucune page ne montre comment garder exécutable à la demande une graine qui révèle un défaut | documentation, partiel | 28 (R) : 28R-10 | La forme `Skip = "…, seed N"` venait de la consigne de l'essai. Documenter `[Fact(Explicit = true), Reproducible(Seed = N)]` avec `dotnet test -- --explicit only` sur la page de l'adaptateur (EN/FR). |

### Documentation

#### DOCS-1 — Le plancher `xunit.v3` de l'adaptateur demande l'opt-in à Microsoft.Testing.Platform sur le SDK .NET 10, et aucune page utilisateur ne le dit
<a id="docs-1"></a>
- **Nature / gravité :** documentation, moyenne · **Verdict :** partiel
- **Rencontré dans :** les 50 domaines, chaque session de découverte et d'habitué, et chaque session d'expert sauf 48 et 49 — 148 constats (01D-1 à 50R-1). La fréquence est celle du banc d'essai ([§2](#2-limites-de-cet-essai), point 1).
- **Ce qui se passe :**
  ```
  dotnet test --project Tests -> MSBUILD : error MSB1001: Unknown switch. … Switch: --project
  dotnet test Tests           -> …/microsoft.testing.platform.msbuild/2.5.1/…: Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. …
  ```
  `{"test":{"runner":"Microsoft.Testing.Platform"}}` dans `global.json` règle le problème. Cela relève de JustDummies parce que la page de l'adaptateur fixe le plancher « `xunit.v3` 4.0.2 or later », et que `xunit.v3` 4.x ne tourne que sur Microsoft.Testing.Platform v2 ; une équipe sous `xunit.v3` 3.x qui monte de version pour adopter l'adaptateur trouve `dotnet test` cassé. Le même projet sous 3.2.2 tourne avec un simple `dotnet test`. La seule page qui le dit est un ADR de mainteneur (ADR-0103).
- **Pourquoi partiel :** `MSB1001` vient de la forme `--project` de la consigne ; un simple `dotnet test <proj>` reçoit une erreur explicite qui nomme l'opt-in. `global.json` n'est pas la seule voie (`dotnet run --project`, ou `-p:IsTestingPlatformApplication=false`). Le guide de démarrage n'installe que le package cœur, donc c'est la mauvaise page pour la note.
- **Recommandation :** une note « Running the tests » sur la page de l'adaptateur (EN/FR) et dans `JustDummies.Xunit/README.nuget.md`, à côté du paragraphe NU1608 existant puisque les deux découlent du même plancher : l'opt-in, les deux alternatives, et un lien vers la propre page du SDK plutôt qu'une reformulation.

#### DOCS-2 — La section de la page de la CLI sur les liens ne montre ni ne référence jamais les formes `.Then(...)` qui couvrent déjà les liens de membre, conditionnels, de produit et de nullabilité
<a id="docs-2"></a>
- **Nature / gravité :** documentation, moyenne · **Verdict :** partiel
- **Rencontré dans :** 05 (D), 08 (D), 09 (D, R), 13 (D), 15 (E), 16 (D), 17 (D, R), 18 (R), 21 (D), 25 (R), 26 (E), 27 (D), 31 (D), 32 (D, R, E), 33 (D, R, E), 34 (D), 35 (E), 37 (D, E), 39 (D, R, E), 40 (E), 41 (D, E), 43 (D, E), 44 (E), 45 (E), 46 (D, E), 47 (D), 49 (E), 50 (R, E). Constats 05D-5, 08D-3, 09D-2, 09R-2, 13D-4, 15E-3, 16D-4, 17D-9, 17R-5, 18R-5, 21D-3, 25R-5, 26E-7, 27D-3, 31D-3, 32D-3, 32D-5, 32R-7, 32E-9, 33D-5, 33R-4, 33E-4, 34D-6, 35E-8, 37D-8, 37E-10, 37E-11, 39D-9, 39R-2, 39E-4, 40E-3, 41D-4, 41D-11, 41E-4, 43D-3, 43D-10, 43E-3, 44E-8, 45E-7, 46D-4, 46E-4, 47D-4, 49E-3, 50R-2, 50E-1.
- **Ce qui se passe :** le récapitulatif nomme chaque lien (`ties measurement to range`, `ties cells, cellsX and cellsY`), le TODO arrête la compilation par conception, et l'utilisateur cherche sur la page de la CLI. Elle montre quatre formes scalaires. Le guide de composition a déjà une chaîne de trois au travers d'`OrderedPair` et `.Then`, un nombre issu d'un produit de dimensions tirées, un conditionnel au travers d'`Any.Boolean().Then(...)`, une borne prise sur un autre tirage, et `Any.Fixed`. Le vérificateur a écrit chaque forme contestée en une ligne à partir de ces blocs et des générateurs de parties scaffoldés ; chacune a tenu son lien à chaque tirage :
  ```csharp
  IAny<LabResult> labResults = new AnyReferenceRange()
      .Then(range => new AnyMeasurement().WithUnit(range.Unit).As(m => new LabResult(m, range)));
  ```
- **Pourquoi partiel :** « no scaffolded form » est par conception et consigné (le « Considered, not adopted » de l'index des ADR : une relation générale est un solveur). « No documented form » est réfuté pour la plupart des formes ; ce qui manque, c'est le lien.
- **Recommandation :** un paragraphe et un exemple compilé, « a tie through another draw — a member, a kind, a count, a null », sous « When a guard ties two parameters » (EN/FR), lié à la section `.Then` de la composition. Corriger le texte de lien périmé « shows the three forms » dans la composition (EN ligne 252, FR ligne 261).

#### DOCS-3 — Un test de garde contre null au travers d'un `With…` scaffoldé demande un cast, et la page ne le montre que pour les paramètres nullables
<a id="docs-3"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** confirmé
- **Rencontré dans :** 03 (E), 04 (D, E), 05 (E), 06 (D), 10 (R), 13 (E), 15 (D, E), 16 (E), 19 (R, E), 22 (E), 23 (R), 31 (R), 32 (E), 37 (E), 43 (R), 47 (E), 48 (E). Constats 03E-5, 04D-9, 04E-6, 05E-4, 06D-5, 10R-9, 13E-5, 15D-7, 15E-9, 16E-7, 19R-6, 19E-10, 22E-5, 23R-2, 31R-6, 32E-10, 37E-13, 43R-7, 47E-4, 48E-5.
- **Ce qui se passe :**
  ```csharp
  Any.PayslipLine().WithLabel(null!).Generate();          // CS0121: ambiguous between WithLabel(string) and WithLabel(Func<AnyString, AnyString>)
  Any.PayslipLine().WithLabel((string)null!).Generate();  // compiles; the guard throws ArgumentNullException
  Any.PayslipLine().WithLabel(value: null!).Generate();   // compiles too, no cast
  ```
  L'ambiguïté est connue et délibérée (ADR-0109 ; spec : pas de `Without…()` pour un paramètre dont la garde refuse null, #411). Utiliser le générateur pour un test de garde contre null est légitime : le `null` est le littéral significatif, les autres arguments sont des dummies. Le seul cast des documents utilisateur se trouve dans le paragraphe sur les paramètres nullables.
- **Recommandation :** une phrase et un exemple sur la page de la CLI (EN/FR), « Testing a constructor's guard » : le cast et `!`, ou l'argument nommé `value:`. Ne pas ajouter de `Without…()` pour les paramètres gardés.

#### DOCS-4 — Le récapitulatif du démarrage rapide omet la ligne de l'espace de noms, et « costs your tests no import » est énoncé sans sa condition
<a id="docs-4"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 01 (D), 02 (R), 04 (R, E), 07 (E), 14 (E), 16 (E), 19 (R), 20 (R), 24 (D, E), 26 (E), 28 (D), 34 (R), 40 (E), 44 (D), 45 (D), 46 (D), 47 (D, R). Constats 01D-8, 02R-10, 04R-10, 04E-3, 07E-7, 14E-12, 16E-12, 19R-9, 20R-7, 24D-6, 24E-9, 26E-5, 28D-8, 34R-7, 40E-15, 44D-10, 45D-2, 46D-7, 47D-10, 47R-7.
- **Ce qui se passe :** l'espace de noms par défaut est celui du domaine par décision (ADR-0062, ADR-0065), et le récapitulatif affiche désormais `in namespace Payments.Domain (--namespace to change).` (#378). Le récapitulatif du démarrage rapide de la page s'arrête avant cette ligne, et la page dit que les points d'entrée « cost your tests no import » sans condition ; un test dans un autre espace de noms obtient `CS0117: 'Any' does not contain a definition for 'Party'`, le texte du compilateur pour un membre d'extension statique.
- **Recommandation :** ajouter la ligne de l'espace de noms au récapitulatif de la page (EN/FR) et rendre conditionnelle la phrase « no import », en nommant CS0117 et les deux remèdes. Ne pas changer le défaut.

#### DOCS-5 — La page de la CLI explique pourquoi les chaînes scaffoldées déclenchent JD030, mais pas pourquoi les lignes à virgule flottante bornées d'un seul côté déclenchent JD036
<a id="docs-5"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 04 (E), 24 (E), 25 (E), 28 (D), 29 (D), 30 (E), 31 (R, E), 32 (E), 33 (R, E), 34 (E), 36 (E), 38 (E), 44 (D, R), 45 (R). Constats 04E-5, 24E-8, 25E-4, 28D-10, 29D-3, 30E-7, 31R-5, 31E-5, 32E-12, 33R-5, 33E-6, 34E-5, 36E-6, 38E-11, 44D-4, 44R-10, 45R-8.
- **Ce qui se passe :** avec JD030 et JD036 passées en avertissement, un scaffold neuf les signale sur `Any.String().NotBlank()`, `Any.Double().Positive()` et les bornes decimal d'un seul côté. C'est décidé (spec : « Informational rules are excused by name, never by severity » ; le test du code émis excuse exactement JD030 et JD036) et le tirage est fidèle — la sonde a atteint 1,79e308 et 7,9e28, et 28D-10 rapporte que ce tirage de limite a trouvé un vrai défaut de `Vector3.Normalized`. La page de la CLI a un paragraphe pour JD030 et aucun pour JD036.
- **Recommandation :** étendre ce paragraphe (EN/FR) aux lignes à virgule flottante, et ajouter une ligne « Scaffolded output » sur la page JD036 qui y renvoie. Va de pair avec [CLI-3](#cli-3) et [CLI-20](#cli-20).

#### DOCS-6 — Aucune recette pour une collection dont les éléments dépendent d'un frère tiré, d'une valeur partagée, de leur index ou de leur nombre
<a id="docs-6"></a>
- **Nature / gravité :** documentation, moyenne · **Verdict :** confirmé
- **Rencontré dans :** 02 (E), 03 (D, R, E), 12 (D), 14 (D, R, E), 18 (D, E), 29 (D), 33 (D, E), 36 (D, R). Constats 02E-6, 03D-3, 03R-3, 03E-4, 12D-4, 14D-3, 14R-3, 14E-6, 18D-10, 18E-4, 29D-4, 33D-8, 33E-5, 36D-4, 36R-7.
- **Ce qui se passe :** chaque exemple de page est scalaire. Chaque forme que décrivent les membres, hors somme déclarée ou contrainte de voisin, peut se construire à partir de primitives documentées — une valeur partagée (`Then` + `ListOf` avec un élément affiné), un nombre issu d'un autre tirage (`Then` + `WithCount`), l'élément i issu de i (un `As` qui transforme l'index), N générateurs avec N tiré (un repli `Then`) — et chacune a tenu 2 000 tirages sur 2 000 et s'est rejouée. La voie `.Then` oblige à retaper l'`AnyList<T>` scaffoldé (CS0266), ce qui est documenté ailleurs.
- **Recommandation :** une sous-section « When a rule ties a collection's elements » sur la page de la CLI (EN/FR), référencée depuis la composition et les collections, un exemple compilé par forme, et une déclaration claire qu'un plafond de somme ou une règle de voisin s'écrit à la main.

#### DOCS-7 — Un value object à un seul paramètre avec chiffre de contrôle n'a pas de section propre
<a id="docs-7"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 01 (D), 06 (D, R, E), 08 (D, R, E), 10 (D, E), 11 (E), 31 (D), 45 (D), 47 (E). Constats 01D-10, 06D-4, 06R-3, 06E-4, 08D-2, 08R-3, 08E-4, 10D-5, 10E-4, 11E-7, 31D-4, 45D-4, 47E-1, 47E-2.
- **Ce qui se passe :** pour une regex plus une clé de contrôle, le TODO n'offre que le retypage vers `AnyPattern` et compte la clé comme « ; and 1 more » ; suivi à la lettre, le fichier compile et le constructeur lève une exception dès le premier lancement. Le seul exemple de chiffre de contrôle (`Any.StringMatching("[0-9]{15}").As(body => body + LuhnDigit(body))`) se trouve sous « When a guard ties two parameters », et la voie documentée renonce à l'affinage (`IAny<T>` n'a pas de `WithValue`).
- **Recommandation :** une courte sous-section sous « Adjusting a parameter » (EN/FR) pour une valeur dont les derniers caractères sont calculés, montrant la forme qui conserve l'affinage (`_value = body.As(b => b + CheckDigit(b))`, la recette restant le builder du corps) à côté de la voie réservée aux épingles. En option, faire mentionner par le TODO de regex que d'autres gardes suivent.

#### DOCS-8 — Aucune page utilisateur ne montre comment tester le refus propre d'un constructeur au travers d'un générateur
<a id="docs-8"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 03 (E), 10 (D, R), 19 (D), 22 (E), 23 (E), 24 (E), 31 (E), 32 (E), 35 (E), 41 (R), 45 (E). Constats 03E-6, 10D-7, 10R-8, 19D-5, 22E-9, 23E-5, 23E-6, 24E-4, 31E-8, 32E-6, 35E-11, 41R-7, 45E-11.
- **Ce qui se passe :** `WithName(n => n.WithLength(41))` sur une base plafonnée à 40 est refusé (`ConflictingAnyConstraintException: Cannot apply WithLength(41) because WithMaxLength(40) already caps the length at 40.`) ; une paire ordonnée ou un `DifferentFrom` refuse une épingle qui rompt son lien. Chacun est délibéré (ADR-0109, ADR-0126, ADR-0121, ADR-0054) et s'accorde avec les fondamentaux : la valeur qui enfreint la règle est ce sur quoi porte l'assertion, donc c'est une épingle, et un générateur doit refuser une épingle qui contredit un lien qu'il déclare. La moitié « feuille » est sur la page, mais dans une section sur une surcharge supprimée, et la ligne 380 présente une épingle qui enfreint une règle comme une erreur.
- **Recommandation :** une sous-section « Testing your constructor's refusal » (EN/FR) : la valeur invalide est une épingle, jamais un affinage (`Assert.Throws<ArgumentOutOfRangeException>(() => Any.Individual().WithAge(-1).Generate())`) ; un lien que le générateur impose se teste avec le constructeur, les autres arguments étant tirés.

#### DOCS-9 — Les premières pages promettent que les analyseurs « start working on your next build », et aucune ne dit comment confirmer qu'ils sont chargés
<a id="docs-9"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 04 (D), 05 (D), 07 (E), 08 (E), 10 (E), 13 (D), 19 (E), 20 (D, E), 21 (R), 35 (D), 39 (E), 43 (D). Constats 04D-11, 05D-7, 07E-9, 08E-5, 10E-5, 13D-5, 19E-9, 20D-10, 20E-8, 21R-10, 35D-6, 39E-8, 43D-6.
- **Ce qui se passe :** `dotnet build` affiche « 0 Warning(s) » alors que JD030, JD033 et JD036 se déclenchent en Info ; même `-v:d` ne montre aucune ligne JD. Le remède figure désormais dans l'index des analyseurs et le readme du nupkg, et plusieurs membres le citent, donc on peut le trouver. Ce qui reste : le guide de démarrage (ligne 64), la FAQ (ligne 139) et le hub des utilisateurs (ligne 60) promettent que les règles fonctionnent sans rien configurer, et aucune page ne donne de commande pour confirmer qu'elles sont chargées.
- **Recommandation :** une phrase et un lien sur ces trois pages (EN/FR) ; une ligne « confirm the rules are loaded » (`dotnet build -p:ErrorLog=jd.sarif`) dans l'index. Garder les défauts en Info.

#### DOCS-10 — La classe d'entrée `extension(Any)` du guide de démarrage n'a ni prose, ni note sur C# 14, ni renvoi à `any`
<a id="docs-10"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 03 (D), 04 (D), 09 (D), 16 (D), 19 (D), 23 (D), 34 (D), 36 (D), 37 (D). Constats 03D-6, 04D-10, 09D-9, 16D-2, 19D-9, 23D-2, 34D-3, 36D-5, 37D-9 — sessions de découverte seulement.
- **Ce qui se passe :** le bloc `AnyEntry` de la page (lignes 197–202) n'a aucune explication. En dessous de C# 14, il ne s'analyse pas, avec de simples erreurs d'analyse (CS1001, CS1513, CS1022) qui ne mentionnent jamais la version du langage — et la bibliothèque prend en charge net8.0, netstandard2.0 et .NET Framework 4.7.2. `any generate X --entry-point any` écrit le même fichier ; la page ne le dit jamais.
- **Recommandation :** deux ou trois phrases après le bloc (EN/FR) : un membre d'extension statique C# 14, ce qu'il faut écrire en dessous de C# 14, et la commande qui l'écrit, avec un lien vers la page de la CLI. Garder le défaut (ADR-0070).

#### DOCS-11 — Aucune page utilisateur ne nomme l'espace de noms `JustDummies` avant la ligne 295 du guide de démarrage
<a id="docs-11"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 27 (D), 29 (E), 31 (D), 34 (R), 48 (D). Constats 27D-5, 29E-6, 31D-5, 34R-6, 48D-7.
- **Ce qui se passe :** un fichier de test avec seulement `using Xunit;` obtient `CS0103: The name 'Any' does not exist` ; un helper dans l'espace de noms des tests obtient CS1061 sur `DifferentFrom`, parce que le fichier scaffoldé porte son propre `using` et que le helper non. Le compilateur d'exemples de la documentation injecte `using JustDummies;` et `using Xunit;` avant chaque exemple, ce qui masque le manque (le « remove the prelude injection » de REPORT.md XU-D1 n'a été réalisé que pour `JustDummies.Xunit`). La moitié `using Xunit;` venait du banc d'essai.
- **Recommandation :** une phrase tôt dans le guide de démarrage et dans le README (EN/FR) — `using JustDummies;`, ou `<Using Include="JustDummies" />` une fois par projet — et une ligne sur la page de la CLI pour les helpers écrits à la main. Cesser d'injecter le prélude dans les exemples `jd:declarations`.

#### DOCS-12 — `DifferentFrom` sur `IAny<T>` prend une seule valeur, et aucune page ne relie « ces tirages doivent différer » aux outils qui le disent
<a id="docs-12"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** partiel
- **Rencontré dans :** 27 (D), 37 (R), 40 (R), 41 (D, E). Constats 27D-6, 37R-10, 40R-11, 41D-6, 41E-6.
- **Ce qui se passe :** deux tirages indépendants d'un même générateur peuvent être égaux — 17 collisions sur 20 000 graines pour un numéro de lot parmi 1 000 valeurs, comme attendu. Des tirages égaux sont un résultat valide de ce qui a été déclaré ; quand le code a besoin qu'ils diffèrent, c'est une contrainte à déclarer. `DifferentFrom` (chaîné pour une troisième valeur), `ListOf(g).WithCount(8).Distinct()` et `DistinctBy` ont chacun donné 0 collision. Passer une liste à `DifferentFrom` donne CS0411, qui pointe dans le mauvais sens. composition.en.md:295 (FR :305) dit qu'un `ListOf` « produces a whole list of distinct customers » sans `Distinct()`, à l'encontre de la page des collections.
- **Recommandation :** sous « `.DifferentFrom(value)` on any generator » (EN/FR) : une valeur, le chaînage pour une troisième, `Distinct`/`SetOf` pour N, `DistinctBy` pour des enfants par clé. Après « usually different » dans la FAQ et core-concepts, une phrase : quand le code a besoin qu'ils diffèrent, le déclarer. Corriger composition :295/:305.

#### DOCS-13 — Affiner une partie scaffoldée imbriquée avec `DifferentFrom` ne compile pas, et aucune page ne dit pourquoi
<a id="docs-13"></a>
- **Nature / gravité :** friction, basse · **Verdict :** partiel
- **Rencontré dans :** 09 (E), 27 (R), 41 (D), 44 (R). Constats 09E-7, 27R-8, 41D-2, 44R-9.
- **Ce qui se passe :** dans `WithId(Func<AnySiteId, AnySiteId>)`, `v.DifferentFrom("PAR-01")` donne CS0411 et `v.DifferentFrom(new SiteId("PAR-01"))` donne CS0266 + CS1662 : l'appel se lie à l'extension d'`IAny<T>`, qui renvoie `IAny<T>`. Le « are you missing a cast? » de CS0266 mène à un cast qui compile et lève `InvalidCastException` à chaque exécution. Les formes qui fonctionnent sont `WithId(i => i.WithValue(v => v.DifferentFrom(other.Id.Value)))` ou un `Any.SiteId().DifferentFrom(other.Id).Generate()` épinglé.
- **Recommandation :** une puce dans « What a refinement can do, and what it cannot » (EN/FR) avec les deux formes qui fonctionnent et une mise en garde contre l'indication de cast.

#### DOCS-14 — La référence des générateurs ne nomme jamais le type de builder que renvoie chaque fabrique
<a id="docs-14"></a>
- **Nature / gravité :** documentation, basse · **Verdict :** confirmé
- **Rencontré dans :** 14 (R), 24 (D), 26 (E), 50 (E). Constats 14R-10, 24D-5, 26E-6, 50E-3.
- **Ce qui se passe :** chaque tableau de référence se lit Factory | Draws | Page. `AnyDecimal`, `AnyDouble`, `AnySet<T>`, `AnySequence<T>`, `AnyArray<T>`, `AnyDictionary<K,V>`, `AnyEnum<T>`, `AnyOneOf<T>` et d'autres ne sont jamais nommés. Un champ, le type de retour d'un helper ou un paramètre de recette doivent nommer le builder. Exagéré : le compilateur nomme la cible dans CS0029, et le package livre sa documentation XML.
- **Recommandation :** une colonne « Builder » dans les tableaux de référence, la règle de nommage énoncée une fois, et une phrase dans « When the replacement is another kind of generator » (EN/FR).

#### Documentation, version compacte
<a id="docs-low"></a>

| Id | Élément | Nature, verdict | Rencontré dans | Recommandation |
|---|---|---|---|---|
| DOCS-15 | Un ordre `TimeOnly` entre deux paramètres n'est pas apparié, et ni la page ni le récapitulatif ne disent pourquoi | documentation, confirmé | 02 (D, R), 41 (E) : 02D-4, 02R-7, 41E-2 | Nuancer la ligne `a > b` de la page comme le fait la spec : chaque builder ordonné sauf `TimeOnly`, dont l'ordre est cyclique (ADR-0126). |
| DOCS-16 | `category-JustDummies.severity` ne relève rien en silence ; les quatre clés de catégorie ne sont jamais listées | documentation, confirmé | 42 (E), 45 (E), 50 (D) : 42E-7, 45E-12, 50D-7 | Lister les quatre clés de catégorie complètes à côté de l'exemple en bloc (EN/FR). |
| DOCS-17 | La page des chaînes ne dit jamais qu'un ensemble `WithChars` est limité au BMP, ni comment tirer du texte astral | documentation, confirmé | 47 (D, E) : 47D-2, 47E-6 | Un passage : un ensemble est fait d'unités UTF-16 dans le BMP ; le texte astral passe par `OneOf` (ADR-0075). |
| DOCS-18 | Le guide de reproductibilité couvre le rejeu d'une graine, jamais la façon d'en trouver une | documentation, confirmé | 13 (E), 31 (D) : 13E-12, 31D-6 | Une section « When the failure is rare » : des exécutions locales répétées avec de nouvelles graines, et un balayage de graines avec `Any.UseSeed`. |
| DOCS-19 | La composition dit que les scaffolds écrivent `.AsNullable()` pour chaque paramètre nullable de type valeur ; le défaut est `.OrNull()` depuis l'ADR-0117 | documentation, confirmé | 49 (D) : 49D-4 | Énoncer la règle réelle (EN/FR). |
| DOCS-20 | JD038 nomme deux correctifs, et aucun ne compile dans une fabrique qui doit renvoyer `AnyInt32` | documentation, confirmé | 15 (D) : 15D-8 | Une sous-section « Inside a generator's factory » sur la page JD038. |
| DOCS-21 | `Any.OrderedPair` est la seule fabrique `Any.*` publique absente de l'index de référence | documentation, confirmé | 17 (E) : 17E-7 | Une ligne dans le tableau Composition (EN/FR). |
| DOCS-22 | La page des collections ne dit jamais qu'une collection non contrainte tire de 0 à 8 éléments (de min à min + 8 avec un plancher) | documentation, confirmé | 40 (E) : 40E-6 | Une courte section « What an unconstrained collection looks like ». |
| DOCS-23 | Une fabrique `As`/`Combine` qui lève une exception remonte en `AnyGenerationException` (délibéré, `InnerException` conservée), et la composition laisse entendre que l'exception du domaine remonte | documentation, partiel | 27 (R), 46 (R), 48 (R) : 27R-2, 46R-4, 48R-8 | Garder l'enveloppe (elle porte les valeurs tirées et la graine). Le dire dans la composition et errors-and-conflicts, et tester le refus d'un constructeur sur le constructeur lui-même. |
| DOCS-24 | Aucune page n'applique les pièces de tirage dépendant à deux agrégats qui doivent partager une valeur tirée | documentation, partiel | 28 (E), 29 (R), 30 (E) : 28E-9, 29R-6, 30E-5 | Un exemple détaillé après `.Then` dans la composition : tirer le fait partagé une fois et l'épingler sur les deux. |
| DOCS-25 | La section sur les liens ne couvre pas une paire nullable « tous deux présents ou tous deux absents », que le défaut `OrNull` un tirage sur dix touche désormais (environ 18 % des tirages) | documentation, partiel | 23 (D, R, E) : 23D-6, 23R-3, 23E-2 | Une cinquième forme : tirer la paire comme une seule valeur au travers de `Then` sur le `OrNull()` d'un côté. |
| DOCS-26 | Aucune page ne dit qu'un générateur de type référence est déjà un `IAny<T?>`, si bien qu'on cherche `AsNullable()` et qu'on obtient CS0453 | documentation, partiel | 39 (D), 47 (D, R) : 39D-7, 47D-12, 47R-4 | Indiquer qu'`AsNullable()` est pour les types valeur et qu'`IAny<T>` est covariant. |
| DOCS-27 | Aucune page ne dit comment traiter un objet tiré qui n'atteint l'assertion que par son identité (`Assert.Same`) | documentation, partiel | 01 (D), 26 (D), 43 (E) : 01D-9, 26D-5, 43E-5 | Demande d'abord la position du mainteneur au regard du premier fondamental ([§5](#5-candidats-à-une-décision)) ; ensuite un paragraphe dans core-concepts qui sépare la poignée du contenu. |
| DOCS-28 | Le TODO d'un paramètre auto-référent demande un générateur à profondeur bornée, et aucune page n'en montre un | documentation, confirmé | 05 (R) : 05R-4 | Un court exemple détaillé sur la page de la CLI (spec §5.4, ADR-0046 : la profondeur est l'affaire du développeur). |
| DOCS-29 | La documentation dit que JustDummies et une bibliothèque de tests par propriétés « coexist happily » mais ne dit jamais comment répartir une suite | idée, confirmé | 46 (E) : 46E-10 | Étendre la réponse de la FAQ avec la règle : une assertion qui a un espace d'entrée est une propriété. |
| DOCS-30 | La page dit qu'un paramètre nullable reçoit `Without…()`, mais un paramètre laissé en TODO ne garde que son épingle | documentation, partiel | 48 (D, E) : 48D-9, 48E-6 | Nuancer « two `With…` methods per parameter » (lignes 41 et 331). |
| DOCS-31 | La section Characters ne dit pas qu'`Any.Char()` n'a pas de `Between`, ni qu'une plage hors d'une famille passe par `OneOf` | documentation, partiel | 05 (R), 44 (D) : 05R-5, 44D-5 | Une phrase ; la moitié `OneOf` est déjà montrée deux fois (réfutée). |
| DOCS-32 | La page des chaînes ne dit pas qu'une position libre dans un motif (`\d`, `.`, `[^…]`) ne tire que de l'ASCII imprimable | documentation, partiel | 12 (D), 22 (E) : 12D-6, 22E-6 | Une phrase sous « Supported constructs » ; la moitié sur les ancres est réfutée. |
| DOCS-33 | Rien n'avertit qu'une classe sans `Equals` rend `DifferentFrom` inerte | documentation, partiel | 16 (R) : 16R-5 | La phrase que la note sur `Distinct()` a déjà, sur la composition et la page de la CLI. |
| DOCS-34 | La règle de l'intervalle complet des entiers cite l'ADR-0117, dont le tirage de limite un sur dix ne s'applique pas aux entiers | documentation, partiel | 25 (E) : 25E-6 | Citer l'ADR-0031 pour les entiers et dire en une proposition où l'ADR-0117 s'applique. |
| DOCS-35 | La page des dates ne renvoie pas à `Any.OrderedPair` pour « a later day », si bien que les auteurs inventent une borne supérieure | documentation, partiel | 12 (E), 18 (E) : 12E-7, 18E-12 | Une phrase et un exemple dans « An unconstrained draw spans the whole type ». |
| DOCS-36 | Un générateur qui épingle les deux côtés d'un lien interne puis applique la lambda de l'appelant laisse celle-ci ré-épingler un côté | documentation, partiel | 39 (R) : 39R-7 | Un paragraphe pour les auteurs de générateurs : la lambda de l'appelant peut ré-épingler ce que le générateur a épinglé, donc n'épingler que la partie indépendante et laisser l'élément dériver la partie liée à partir de sa valeur finale. |
| DOCS-37 | Un `Generate()` écrit à la main qui lit une valeur épinglée avant que le constructeur n'intercepte un `null!` destiné à la garde | documentation, partiel | 05 (E) : 05E-7 | Une phrase dans « Derive one draw from another inside `Generate()` ». |
| DOCS-38 | Aucune page ne suggère de construire un générateur gardé par regex à partir du `Regex` propre au domaine, si bien que le motif est copié | documentation, partiel | 03 (E) : 03E-11 | `Any.StringMatching(PolicyNumber.Format)` quand le domaine l'expose. |
| DOCS-39 | Les consignes sur les prérequis de scénario ne vivent que dans core-concepts, sans lien depuis la règle principale | documentation, partiel | 39 (E) : 39E-7 | Une phrase et un lien dans le guide de démarrage et dans la FAQ. |
| DOCS-40 | Aucune page n'applique la règle du dummy à un système sous test qui prend sa propre graine | documentation, partiel | 40 (R) : 40R-12 | Une entrée de FAQ qui applique la règle existante : le tirer si l'assertion tient quelle qu'elle soit, l'épingler sinon. |
| DOCS-41 | `InlineData` ne peut pas porter un decimal (CS0182, une limite de C#) ; xUnit v3 convertit déjà un argument chaîne | idée, partiel | 03 (R), 08 (E) : 03R-9, 08E-12 | Phrase optionnelle sur la page de l'adaptateur. |
| DOCS-42 | Un substitut isolé dans un argument d'attribut arrive en U+FFFD — l'encodage des attributs de C#, ni xUnit ni JustDummies | idée, partiel | 47 (D, R) : 47D-13, 47R-8 | Pas un élément de la bibliothèque ; au plus une ligne à côté de DOCS-41. |
| DOCS-43 | Un tuple construit dans `As` à partir d'un élément non nul avertit CS8619 face à une cible nullable — comportement de C# | documentation, partiel | 49 (R) : 49R-6 | Pas un élément de la bibliothèque ; la reproduction de la grappe ne se reproduit pas. |

---

## 5. Candidats à une décision

Aucun de ces éléments n'est compté comme un bug. Chacun est soit décidé à dessein — et l'essai apporte des preuves de terrain qu'une décision pèserait — soit demanderait que le générateur tente plus qu'aujourd'hui. La ligne de périmètre (CLAUDE.md, ADR-0046) fait foi : un changement qui rend le générateur plus malin qu'un refus net, qui élargit une borne ou qui ajoute un solveur demande une décision, non un correctif.

### 5.1 Là où cet essai est le retour de terrain qu'attend une décision enregistrée

| Élément | Rencontré dans | Décidé dans | Ce que l'essai apporte |
|---|---|---|---|
| **Lire les gardes de regex** comme `AnyPattern`, au lieu d'`AnyString` plus un TODO et un retypage en quatre ou cinq endroits | 47 constats, 27 domaines, D15 R17 E15 | spec §5.3 et §16 (un élément de la v1.1), ADR-0063 | Troisième essai de suite, l'élément par conception le plus étendu. Le TODO cite désormais le motif et liste les endroits (#374), ce qui a rendu le retypage mécanique ; l'étape suivante est de le lire. |
| **Une couture qui résiste à un nouveau scaffold** pour un lien entre paramètres (un `Generate()` écrit à la main est effacé par `--force`) | 28 constats, 19 domaines, D7 R12 E9 | Index des ADR, « Considered, not adopted » : un point d'accroche `partial` qui survit à `--force` | La récurrence la plus forte de toutes les options refusées, trois essais de suite. Avec l'enregistrement d'épingle de [CLI-1](#cli-1), la modification garderait au moins ses épingles. |
| **Un lien d'inégalité scaffoldé en `DifferentFrom`** (`a.Equals(b)`, `a == b`) | 7 constats, 4 domaines : 24 (D, E), 27 (E), 32 (D, R, E), 35 (E) | ADR-0121, alternative rejetée « let `any` read an `a.Equals(b)` guard » | Le même argument que #405 pour `OrderedPair` : des modifications perdues avec `--force`. |
| **Un étalement en échelle logarithmique** pour les plages à virgule flottante sur de nombreuses décades | 6 constats, 3 domaines : 28 (R), 39 (R, E), 43 (D, R, E) | ADR-0117, « A log-uniform spread » rejeté ; page des nombres | Des plages de 11 à 25 décades dont les basses décades portent le comportement du domaine : environ 90 % des tirages de `Resistance` dans la décade supérieure. REPORT-2 CORE-14 à nouveau. |
| **Un ordre avec un côté nullable** (`until < from`, `until is T e && e < from`) | 5 constats, 3 domaines : 19 (D, R, E), 38 (D), 49 (E) | spec §5.3 : la ligne d'ordre apparie deux paramètres non nullables ; « a conjunction stays unread » | Une extension étroite et exacte de la ligne d'ordre (ADR-0082). |
| **Un accesseur public en opt-in** pour les bases privées `AnyValid…()` | 5 constats, 5 domaines : 05 (E), 13 (R), 20 (E), 27 (E), 30 (R) | spec §4.2 ; la page de la CLI montre l'enveloppe partielle en une ligne | REPORT-2 CLI-17 à nouveau ; un interrupteur dans `any.json` émettrait l'enveloppe. |
| **Une contrainte de jour de semaine ou de jour ouvré** | 5 constats, 2 domaines : 02 (D, R, E), 50 (D, R) | REPORT.md CORE-C3, refusé ; index des ADR | Garder le refus ; la page des dates (EN/FR) manque toujours de la recette (`Between(…).As(NextBusinessDay)`), qui sort le paramètre de son type de builder. |

### 5.2 Décisions soulevées par les éléments confirmés ou partiels

| Élément | La question |
|---|---|
| [CORE-1](#core-1) | Deux épingles sur une paire ordonnée : des littéraux vérifiés seulement l'un par rapport à l'autre, ou chaque épingle toujours vérifiée par rapport à l'autre base ? (ADR-0126) |
| [CORE-2](#core-2) | Une paire ordonnée qui porte un écart maximal — la « relation autre qu'un ordre » que l'index des ADR nomme comme condition de reprise. |
| [CORE-3](#core-3), [CORE-4](#core-4) | Les noms de côtés sur `AnyOrderedPair` (surface publique) ; une seule règle pour une épingle `Local` sur une paire `DateTime` (ADR-0114). |
| [CORE-7](#core-7), [CORE-8](#core-8) | `Except(IEnumerable<T>)` sur `IAny<T>` (amende l'ADR-0121) ; une valeur par défaut bornée pour un `Any.BigInteger()` (ADR-0046, ADR-0117). |
| [CLI-4](#cli-4), [CLI-6](#cli-6), [CLI-8](#cli-8), [CLI-16](#cli-16), [CLI-29](#cli-low) | De nouvelles lignes exactes pour le lecteur de gardes au titre de l'ADR-0085 : helpers `void` du même type, gardes que la recette satisfait déjà, bornes `static readonly`, prédicats de classe de caractères, modulo. |
| [CLI-10](#cli-10), [CLI-25](#cli-25) | Marquer un appel levant une exception du même type sous la première affectation (dans le sens de l'ADR-0110) ; cesser de marquer une transmission à un constructeur d'exception du framework. |
| [CLI-11](#cli-11) | Restreindre le `new AnyX()` à l'aveugle de l'ADR-0089 aux types propres du développeur ; donner une sentinelle aux types du framework. |
| [CLI-17](#cli-17), [CLI-23](#cli-23) | N'écrire que le fichier d'entrée manquant (spec §7, ADR-0070) ; refuser un lien entre constructeurs de même largeur (spec §5.1). |
| [CLI-20](#cli-20), [CLI-22](#cli-22) | Émettre la borne opposée sur chaque membre d'une paire ; promettre une Scale ≤ N pour les valeurs tirées (ADR-0015). |
| [CLI-21](#cli-21), [CLI-41](#cli-low), [CLI-42](#cli-low) | Une règle JD ou une surcharge `[Obsolete]` émise pour un générateur passé à `With…` ; une sentinelle plus riche (ADR-0060) ; un scaffold minimal (ADR-0109). |
| [XU-1](#xu-1) | Exposer si une portée de graine a servi à tirer (ADR-0017, ADR-0049). |
| [DOCS-27](#docs-low) | Un objet tiré qui n'atteint l'assertion que par son identité est-il un dummy ? Une position au regard du premier fondamental, avant qu'une page ne la dise. |

### 5.3 Par conception, consigné

| Élément | Rencontré dans | Pourquoi cela reste |
|---|---|---|
| Un grand plafond de garde est écrit tel qu'il est lu ; le conseil de restriction n'est que dans la console et le JSON | 20 (R, E), 35 (D, E) : 20R-3, 20E-2, 35D-4, 35E-7 | Spec §6, REPORT.md CLI-C1/#361. Documentation : le mentionner sur la page de la CLI. |
| `--entry-point any` refuse `Voice` quand `VoiceEntry` existe | 33 (D, R, E) : 33D-3, 33R-3, 33R-8, 33E-2 | La spec §4.5 emploie exactement ce cas ; le bug de REPORT-2 CLI-11 est désormais un refus avant que quoi que ce soit ne soit écrit. |
| Les gardes d'unicité de collection (`Distinct().Count() != Count`) ne sont pas lues | 27 (E), 38 (R), 40 (R) | Ensemble fermé (ADR-0085) ; preuves de terrain pour REPORT.md CLI-G8. |
| Un wrapper générique (`Observed<T>`) refusé comme cible, laissé à un TODO comme paramètre | 49 (D, R, E) | Spec §3.2, §5.4 ; un exemple de documentation pour un générateur générique écrit à la main aiderait. |
| « N of N parameters inferred » compte les lignes `to verify` sous une coche | 43 (D), 48 (E), 50 (R) | Spec §6, ADR-0083 ; la page ne montre jamais la ligne. |
| Une expression `switch` qui lève une exception, affectée à un état, est marquée et non lue | 09 (D, R, E) | Spec §5.3 (un `if` avant la première écriture) ; retour de terrain pour une ligne. |
| Une ligne de chaîne scaffoldée tire tout l'ASCII, caractères de contrôle compris | 41 (R) : 41R-8, 41R-9 | L'ADR-0075 a rejeté l'ASCII imprimable parce qu'il masque des défauts ; l'ADR-0127 refuse de deviner à partir d'un nom. |
| `!(p >= a && p <= b)` et `x = c ? x : throw` non lus | 14 (R, E) | L'ADR-0128 a rejeté un normalisateur général ; deux lignes étroites demanderaient un amendement. |
| Une lambda d'affinage sur un paramètre nullable ne tire jamais `null` | 15 (R), 26 (D) | Spec D10, ADR-0117 ; en option, le dire dans les remarques de la surcharge. |
| Une borne sous `is { } e` sur un nullable est marquée | 36 (E) | Spec §5.3, §9 (« validate what is present »). |
| Un affinage n'a rien à affiner sur un `IAny<string>` composé ou un `AnyPattern` | 01 (E) | ADR-0109 ; restreindre demande une intersection (ADR-0046). |
| Un paramètre abstrait reste un TODO après que son générateur écrit à la main existe | 48 (E) | #375 : l'outil ne le cherche jamais ; le dire sur la page. |
| Une capacité tirée sur `1..MaxCapacity` fait échouer un test qui ajoute trois règles | 46 (R) | La base est juste ; le test dépend de la capacité et doit la déclarer. |
| Un générateur scaffoldé n'expose que sa paire `With…`, non le vocabulaire des feuilles (`After`, `Before`) | 28 (D) | Spec §4.2, ADR-0109/0116. |
| Pas de point d'entrée sans `--entry-point` | 38 (R) | ADR-0070 (le plancher C# 14) ; renvoyer à l'`entryPoint` d'`any.json`. |
| Une lambda de liste ne peut pas atteindre la recette de son élément | 12 (E) | ADR-0109 ; une répétition de REPORT-2 CLI-26. |
| Pas d'`OrderedPair` sur un value object comparable | 34 (D, R, E), 48 (E) | L'ADR-0126 l'a rejeté (une épingle honorée seulement par rejet) ; documenter apparier-la-primitive-puis-`As`. |
| Une surcharge écrite à la main qui n'écrit que le champ de tirage est écrasée par un `With…(Func)` ultérieur | 37 (D) | ADR-0116 (Proposed) ; documentation : une variante nommée. |
| `AnyPattern` n'a ni `StartingWith`, ni plafond de longueur | 11 (R), 16 (E) | ADR-0008, ADR-0046 (pas d'intersection de langages). |
| Un second `OneOf` ne peut pas restreindre un ensemble de base | 05 (D) | ADR-0100 ; `Except` restreint, et le message le nomme. |
| Un decimal tiré sans `WithScale` porte 27 ou 28 chiffres fractionnaires | 44 (E) | ADR-0031, ADR-0117 ; le plafonner masquerait du code qui arrondit. |
| JD036 n'a pas de marqueur d'intention pour un tirage délibérément ouvert | 07 (R), 28 (E) | Un `[SuppressMessage]` justifié est le marqueur documenté. |
| Aucun analyseur n'avertit que deux tirages indépendants peuvent entrer en collision là où une garde les exige distincts | 09 (E) | L'exigence est une garde d'exécution ; une heuristique ferait du bruit (ADR-0046). |
| Sous Microsoft.Testing.Platform, la ligne de graine se trouve sous « Standard output » | 06 (E), 42 (R) | Canal de l'ADR-0018 ; documentation : montrer un échec tel que `dotnet test` l'affiche. |
| Une valeur tirée dont le test dépendait a fait échouer le test et la graine l'a rejoué | 25 (R), 34 (R, E), 44 (D), 47 (D), 48 (R) | Le premier fondamental à l'œuvre ; consigné sous le [§7](#7-ce-qui-a-fonctionné). Documentation : les pages vers lesquelles mène un test rouge (la boucle de reproductibilité, l'entrée de FAQ « A test failed once and passed on rerun », le « Replaying a failure » de l'adaptateur) ne nomment que « corriger le défaut » ; elles devraient nommer les deux issues, et la question de la FAQ porte elle-même le cadrage en termes de test instable. Un rapport des valeurs tirées (REPORT-2 XU-2) reste non adopté : chaque rejeu ici a fonctionné du premier coup. |

---

## 6. Réfutés et non reproduits

- **shipped-docs-dead-links** (10 constats, découverte seulement : 09D-8, 11D-11, 16D-10, 17D-6, 19D-10, 23D-10, 25D-11, 33D-6, 39D-8, 47D-8) — réfuté : les liens morts venaient de la façon dont le script d'espace de travail de cette exécution a reconstruit le dossier de documentation, non du dépôt ni des packages. Corriger le banc d'essai pour le prochain essai.
- **duplicate-todo-line** (29D-6) — non reproduit : aucune fabrique ne reçoit deux lignes TODO ; la même ligne apparaît une fois dans chacune de deux fabriques liées par une même garde.

Sous-affirmations réfutées à l'intérieur d'éléments confirmés ou partiels, pour mémoire : le refus de restauration sort avec le code 1, non 2 ([CLI-2](#cli-2)) ; une ligne de type du framework porte bien sa sentinelle, masquée par CS0246 ([CLI-11](#cli-11)) ; un endroit de retypage manqué ne compile jamais ([CLI-15](#cli-15)) ; le message de `DifferentFrom` échappe les caractères de contrôle ([CORE-6](#core-6)) ; le refus de `OneOf` se déclenche à la déclaration, non à `Generate()` ([AN-2](#an-2)) ; aucun `With…` de la bibliothèque n'a de paire valeur/lambda ([CLI-21](#cli-21)).

---

## 7. Ce qui a fonctionné

- **Les valeurs tirées ont révélé de vrais défauts de domaine — la raison d'être de la bibliothèque.** Selon les sessions, chacun s'est rejoué depuis sa graine :
  - 32 (D) : `Solve` sur un réseau valide tiré a levé `ArgumentOutOfRangeException: Reactive power must be finite and within ±10000 Mvar. Actual value was 17599.82` depuis `Bus.NetInjection`, sur la graine −2128493443 ; rejouer la graine a redonné la même valeur. La production d'un bus valide moins sa charge peut sortir de la plage du `PowerInjection` qu'il construit. Voir [NOTES.discovery.md](domains/32-grid-load-flow/NOTES.discovery.md).
  - 28 (D, R, E) : une conjonction tirée a montré que `PositionCovariance.Combine` refuse la somme de deux covariances valides (`Actual value was 1308982.4889122401`), si bien que `Conjunction.Assess` lève une exception pour des trajectoires valides ; le tirage de limite un sur dix a aussi trouvé que `Vector3.Normalized` fait un dépassement de capacité par le bas. « The draw found a domain defect before any of my tests did » ([NOTES.discovery.md](domains/28-satellite-conjunction/NOTES.discovery.md)).
  - 33 (D, R) : la somme exacte de deux durées valides peut être non représentable — « a real domain issue … that no literal I would have written would have found ».
  - 21 (E) : un facilitateur de très haut rendement mène l'espèce aidée à l'extinction, sur la graine −1087769823 ; conservé comme test ignoré avec un frère qui épingle un rendement comparable.
  - 37 (R) : `Any.Quantity().Generate()` a atteint le plafond de 1e9 dans `ConvertTo` à la troisième exécution ; une transformation tirée avec des unités mixtes a laissé `BatchGenealogy.Record` à moitié appliqué environ une exécution sur six.
  - Des grandeurs laissées sans plafond ont débordé sous le tirage en 06, 13, 14, 15, 29, 36 et 40 — le salaire net dans 64 fiches de paie sur 3 000 (13), une largeur de jauge de 6,86e28 (36), 114 portefeuilles sur 20 000 (40) — et un échéancier de dates a dépassé `DateOnly.MaxValue` une exécution sur six (30). Chaque session a conservé le défaut sous forme de test ignoré plutôt que de plafonner le générateur pour le masquer.
  - 50 (E) : l'unique tirage rouge a mené la session à une `InvalidOperationException` brute qui s'échappe de `SettlementBatch.Book`.
- **Une valeur tirée a montré qu'une valeur traitée comme un dummy n'en était pas un, et la graine l'a rejouée du premier coup.** Six constats dans cinq domaines (25R-8, 34R-10, 34E-4, 44D-11, 47D-11, 48R-7), plus 49 (R) et l'exécution du coordinateur sur 49 (E) :
  - 48 (R) : la date de naissance d'un contribuable, tirée dans un test sur une condition d'âge. `[JustDummies] These arbitrary values were seeded with 481195150. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 481195150)].` La session a épinglé la date dans le helper partagé.
  - 44 (D) : une exécution rouge sur 87, graine 22259052 : le test tirait un produit dont la tolérance d'excursion décide de l'intervalle du logger, et construisait les palettes séparément.
  - 49 (R) : un événement indésirable « de remplissage » pouvait lui-même être mortel ; la session a ajouté `AnyAdverseEvent.NonFatal()`, et épingler la graine a rejoué l'échec aussitôt.
  - Aucun de ces cas n'est de l'instabilité. Comme l'a dit une session : « The sentence 'a test that fails on a drawn value is never flaky' matched my experience: it was my set-up. »
- **Le rejeu depuis la graine a tenu partout où il a été essayé.** Une exécution verte sous `[assembly: Reproducible]` n'affiche rien ; une exécution rouge affiche une ligne prête à coller, désormais pleinement qualifiée, donc REPORT-2 XU-1 est corrigé (23D-11, 32D-9). La coller a redonné la même valeur tirée au bit près. Les 150 suites sont restées vertes sur 11 à 222 exécutions chacune avec de nouvelles graines ; 47 (E) seul a lancé 723 cas au vert 33 fois dans la session et 41 fois de plus en vérification.
- **Les sentinelles du scaffolder ont tenu leur promesse.** Partout où l'outil ne pouvait pas se porter garant d'une garde, la compilation s'est arrêtée à la fabrique exacte avec la garde citée (05E-14, 08D-11, 26E-12, 31D-9) :
  ```
  Container resolved to VesselStowage.Domain.Container; System.ComponentModel.Container also answers to that name.
  // TODO(any): verify against Probe.cs, line 5: if (!Pattern().IsMatch(value)) { ... } — a regex, "^CH(0[1-9]|1[0-2])-C[0-9]{4}$": Any.StringMatching(pattern) replaces the base and returns an AnyPattern, so retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithValue — then delete these two lines.
  amount       decimal  Any.Decimal().Positive().LessThanOrEqualTo(10000m).WithScale(3)  guard
  timesPerDay  int      Any.Int32().Positive().LessThanOrEqualTo(6)                    guard
  ```
  La garde `amount <= 0 || amount > 10000m` que REPORT-2 CLI-1 trouvait non lue est désormais lue, et `decimal.Round(amount, 3) != amount` aussi. Le nom court ambigu se résout au lieu de faire échouer le lot (REPORT-2 CLI-12), et le TODO de regex cite le motif et liste la modification (REPORT-2 CLI-13). Les exceptions sont les formes silencieuses documentées ([CLI-10](#cli-10)) et un cas d'instruction composée ([CLI-5](#cli-5)).
- **Le scaffolder refuse bruyamment à sa frontière.** Un paramètre abstrait, une cible générique, un conflit de nom de point d'entrée, une fabrique à plusieurs arguments, un projet non restauré : chacun est un refus qui nomme ce qu'il ne peut pas honorer, jamais un générateur deviné (ADR-0046).
- **Les analyseurs se sont déclenchés avec justesse et sont restés muets sur le code propre.** JD007 se déclenche désormais sur une entrée `Any.X()` scaffoldée dans un initialiseur de champ et dans un constructeur de classe de test (REPORT.md AN-C3, #365), et JD013 nomme `Any.ElementOf` comme remplacement (08R-9, 12E-14, 30E-6, 42E-10). Plusieurs sessions d'expert ont passé toutes les règles JD en avertissement sur des suites de 500 à 700 cas et n'ont obtenu aucun diagnostic sur leur propre code (42E, 46E, 47E, 48D) ; une sonde délibérée `$"{Any.Int32()}"` a déclenché JD005 (50D).
- **Le DSL du cœur n'a demandé aucune explication.** Les épingles, les affinages, `OrderedPair`, `.Then`, `DifferentFrom`, `DistinctBy` et `Any.Fixed`, pour la plupart des réponses à REPORT-2, ont été utilisés comme documenté sur les cinquante domaines ; la friction se trouve autour d'eux, dans le scaffolder et la documentation, non en eux.

---

## 8. Ce qu'ont appris les nouveaux domaines 41 à 50

- **41 — fuseaux horaires et heure d'été** ([D](domains/41-shift-planning-time-zones/NOTES.discovery.md), [R](domains/41-shift-planning-time-zones/NOTES.regular.md), [E](domains/41-shift-planning-time-zones/NOTES.expert.md)). La bibliothèque n'avait rien à voir avec les fuseaux : les cas d'heure d'été, de repos et de limite hebdomadaire ont employé des dates littérales de Paris, New York, Tokyo, Sydney et Troll, et les identifiants, noms, périodes et participants ont été tirés. C'est le partage que demandent les fondamentaux. Les coûts sont venus des liens entre heures de la journée : un ordre `TimeOnly` n'est pas apparié, par décision, et rien ne dit pourquoi ([DOCS-15](#docs-low)) ; une fenêtre qui s'ouvre avant de se fermer, ou un début plus une durée en heure murale, a dû être écrit dans `Generate()` ([CLI-1](#cli-1)). Une demande tirée peut tomber dans un trou d'heure d'été et être refusée, sans moyen de déclarer « résoluble dans ce fuseau » (41D-11). Les défauts de domaine — `IsoWeek(9999, 52)` et l'arithmétique de roulement à `DateOnly.MaxValue` — ont tous été trouvés avec des littéraux ; les sessions de découverte et d'expert ne rapportent aucun échec sur une valeur tirée en 28 et 45 exécutions.
- **42 — des nombres au-delà de `decimal`** ([D](domains/42-token-ledger/NOTES.discovery.md), [R](domains/42-token-ledger/NOTES.regular.md), [E](domains/42-token-ledger/NOTES.expert.md)). `UInt128` est une fabrique comme une autre ; `BigInteger` non. Le scaffolder écrit `new AnyBigInteger()` comme s'il existait et la compilation s'arrête sur CS0246 ([CLI-11](#cli-11)) ; aucune page ne dit comment en composer un, alors qu'une valeur uniforme de 256 bits tient en une ligne ([CORE-8](#core-8)). Les sessions ont écrit des classes de builder à la main. Les liens arithmétiques (un plafond multiple de 10^decimals, une offre bornée par le plafond) sont passés dans `Generate()` avec des drapeaux d'épingle tenus à la main.
- **43 — très petites et très grandes grandeurs** ([D](domains/43-passive-components/NOTES.discovery.md), [R](domains/43-passive-components/NOTES.regular.md), [E](domains/43-passive-components/NOTES.expert.md)). Une plage de 25 décades est tirée uniformément en valeur, si bien qu'environ 90 % des tirages de `Resistance` tombent dans la décade supérieure : la preuve de terrain la plus nette à ce jour en faveur de l'étalement en échelle logarithmique différé ([§5.1](#51-là-où-cet-essai-est-le-retour-de-terrain-quattend-une-décision-enregistrée)). Les tirages eux-mêmes étaient corrects à toutes les grandeurs. Les quatre exécutions rouges d'un expert sur des valeurs tirées étaient chacune une valeur qui n'était pas un dummy, ou une erreur d'arithmétique.
- **44 — transversal à 06, 07, 10 et 20** ([D](domains/44-vaccine-cold-chain/NOTES.discovery.md), [R](domains/44-vaccine-cold-chain/NOTES.regular.md), [E](domains/44-vaccine-cold-chain/NOTES.expert.md)). Une seule commande a scaffoldé 28 types sur quatre domaines référencés, et le graphe composé a tenu ; la compilation s'est arrêtée sur 21 d'entre eux, parce que ce modèle valide surtout au travers de regex et de helpers. Composer à travers des domaines a fait apparaître ce qu'un domaine seul ne fait jamais : un nom de type court qui entre en collision avec l'alias d'un autre domaine (CS0104, [CLI-26](#cli-26)), et de nombreux espaces de noms à importer ([DOCS-4](#docs-4)). L'unique tirage rouge en 87 exécutions était une règle inter-domaines (l'intervalle du logger face à la tolérance d'excursion) que le test avait répartie sur deux tirages indépendants.
- **45 — transversal à 03, 08 et 01** ([D](domains/45-healthcare-claims/NOTES.discovery.md), [R](domains/45-healthcare-claims/NOTES.regular.md), [E](domains/45-healthcare-claims/NOTES.expert.md)). De 21 à 23 types par session scaffoldés en une exécution (14 issus des domaines référencés dans celle de l'expert). Un type atteint par une référence de projet transitive avant une restauration est rapporté comme « no type by that name » ([CLI-43](#cli-low)). Les agrégats à cinq liens avec leur prescription ont été réécrits à la main ; les générateurs de feuilles ont demandé peu. Aucune valeur tirée n'a échoué en 37 à 72 exécutions.
- **46 — types au niveau du bit et non signés** ([D](domains/46-network-acl/NOTES.discovery.md), [R](domains/46-network-acl/NOTES.regular.md), [E](domains/46-network-acl/NOTES.expert.md)). Les adresses IPv6 `UInt128` et les plages non signées ont été tirées sans friction ; l'exécution différentielle d'un expert sur 300 000 adresses aléatoires face à `System.Net.IPAddress` n'a trouvé que le rendu propre du framework pour les adresses basses. Ce qui a fait mal, c'est la forme, non les bits : un `AddressPrefix` avec un constructeur IPv4 et un constructeur IPv6 n'a scaffoldé que le premier, en silence ([CLI-23](#cli-23)) ; une énumération `[Flags]` est tirée un membre à la fois sans rien qui renvoie à `AllowingCombinations()` ([CLI-38](#cli-low)) ; un `null` constant dans un ensemble `OneOf<byte?>` compile et lève une exception ([AN-2](#an-2)). La moitié des scaffolds a été remplacée parce que chaque paramètre d'un paquet dépend des autres.
- **47 — Unicode au-delà d'ASCII** ([D](domains/47-international-catalogue/NOTES.discovery.md), [R](domains/47-international-catalogue/NOTES.regular.md), [E](domains/47-international-catalogue/NOTES.expert.md)). Les refus propres au domaine (plages CJK sans U+3005, l'okina, les formes pleine chasse) ont été trouvés avec des données littérales réalistes, comme il se doit. Côté bibliothèque, `Any.StringMatching` avec un caractère astral tire en silence de l'UTF-16 mal formé alors que `WithChars` refuse la même entrée ([CORE-5](#core-5)), et aucune page ne dit comment tirer du texte hors BMP ([DOCS-17](#docs-low)). Un substitut isolé ne peut pas traverser `[InlineData]` — une limite d'encodage de C#, non de la bibliothèque ([DOCS-42](#docs-low)).
- **48 — polymorphisme et arbres récursifs** ([D](domains/48-tax-rules-engine/NOTES.discovery.md), [R](domains/48-tax-rules-engine/NOTES.regular.md), [E](domains/48-tax-rules-engine/NOTES.expert.md)). Les paramètres abstraits sont laissés à un TODO par conception, et l'outil ne reprend jamais un `AnyICondition` écrit à la main une fois qu'il existe ([§5.3](#53-par-conception-consigné)). Choisir parmi des générateurs fonctionne avec `Then`, mais JD012 signale la forme `ElementOf(...).Then(g => g)` que son propre message recommande ([AN-1](#an-1)). Le manque le plus coûteux a été une classe `Guard` commune à tout le domaine, d'un autre type : silencieuse, si bien qu'`IncomeKind` a tiré `None` une fois sur cinq alors que le récapitulatif disait toujours « inferred » ([CLI-10](#cli-10)). Une date de naissance tirée sous un test de condition d'âge a été le non-dummy le plus net de l'essai.
- **49 — valeurs absentes** ([D](domains/49-clinical-trial-crf/NOTES.discovery.md), [R](domains/49-clinical-trial-crf/NOTES.regular.md), [E](domains/49-clinical-trial-crf/NOTES.expert.md)). Le wrapper `Observed<T>` du domaine type cinq des six paramètres les plus difficiles, et un type générique ne peut pas être scaffoldé ([§5.3](#53-par-conception-consigné)). `null` un tirage sur dix par défaut est désormais la règle, et la page de composition dit encore autre chose ([DOCS-19](#docs-low)) ; un ordre avec un côté nullable n'est pas apparié ([§5.1](#51-là-où-cet-essai-est-le-retour-de-terrain-quattend-une-décision-enregistrée)). Le domaine a besoin de « cela a-t-il été épinglé, éventuellement à null ? », ce que l'expert a reconstruit avec une struct privée `Pin<T>` ([CLI-1](#cli-1)). C'est aussi le domaine dont le grade tiré a montré, lors de l'exécution du coordinateur, qu'un événement « de remplissage » n'était pas un dummy ([§2](#2-limites-de-cet-essai), point 7).
- **50 — échelle décimale dépendant de la devise** ([D](domains/50-fx-settlement/NOTES.discovery.md), [R](domains/50-fx-settlement/NOTES.regular.md), [E](domains/50-fx-settlement/NOTES.expert.md)). Dix scaffolds sur onze se sont arrêtés sur un lien — code vers unités mineures, montant vers devise, décimales du taux à apparier — exactement les liens autour desquels ce domaine a été construit ; contraindre l'échelle d'un paramètre scaffoldé à partir de la valeur tirée d'un autre n'a aucun idiome pris en charge ([CLI-1](#cli-1)). Les cas d'arrondi et de point milieu sont restés des littéraux, qui « read as specifications » ; les tirages rouges en 50 (R) et 50 (E) étaient des plages de taux de feuille trop larges pour le scénario, dont les croisements tombent sous ce qu'une cotation à trois décimales peut porter et s'arrondissent à 0 — le domaine avait raison de refuser, et en 50 (R) un côté de transaction laissé tiré dans un test sur un achat n'était pas non plus un dummy. Une borne constante niée est copiée comme une valeur ([CLI-32](#cli-low)), les dates de jour de semaine n'ont aucune contrainte (refusé, [§5.1](#51-là-où-cet-essai-est-le-retour-de-terrain-quattend-une-décision-enregistrée)), et la référence des générateurs ne nomme jamais les types de builder ([DOCS-14](#docs-14)).

---

## 9. Défauts de domaine trouvés par les sessions

**Rapportés, non vérifiés indépendamment.** Les 536 constats de domaine ci-dessous, regroupés en 264 grappes, viennent des sessions. Chaque session rapporte avoir démontré ses défauts par un test conservé en `Skip = "domain defect…"` et l'avoir confirmé rouge en le réactivant une fois ; la passe de vérification ne les a pas reproduits. Ce sont les modèles inventés de l'essai lui-même, donc aucun n'est un défaut de JustDummies.

Familles récurrentes, par titre de grappe (une grappe peut tomber dans deux) :
- une regex qui se termine par `$`, que .NET fait aussi correspondre avant un `\n` final : 43 domaines (dans 01–20, le `MODEL.md` y a orienté les sessions, [§2](#2-limites-de-cet-essai) point 2) ;
- une arithmétique au-delà d'un plafond manquant, ou une date au-delà de `MaxValue` : environ 22 domaines — la famille que les valeurs tirées ont trouvée le plus souvent ;
- un stockage interne exposé derrière `IReadOnlyList`, ou la liste de l'appelant conservée : environ 24 domaines ;
- `\d` qui accepte des chiffres non ASCII : 18 ; `NullReferenceException` au lieu d'`ArgumentNullException` : 16 ; `ToString` dépendant de la culture : 12 ; NaN laissé passer : 5 ; un membre d'énumération non défini accepté : 5.

« Trouvé par » dit comment la session rapporte les avoir trouvés : **draw** quand une valeur tirée a révélé un défaut (la preuve pour laquelle la bibliothèque existe), **literal** quand la session dit avoir utilisé des cas limites choisis, et **cases** quand les notes ne le disent pas.

| Domaine | Notes | Constats (grappes) | Défauts les plus parlants | Trouvé par |
|---|---|---|---|---|
| 01-payments | [D](domains/01-payments/NOTES.discovery.md) [R](domains/01-payments/NOTES.regular.md) [E](domains/01-payments/NOTES.expert.md) | 16 (7) | `Iban` passe sa regex avec un `\n` final, puis lève `FormatException` ; `PaymentBatch` garde la liste de l'appelant, si bien que ses invariants se brisent après la construction ; chiffres de contrôle d'IBAN 00, 01 et 99 acceptés | literal |
| 02-trading | [D](domains/02-trading/NOTES.discovery.md) [R](domains/02-trading/NOTES.regular.md) [E](domains/02-trading/NOTES.expert.md) | 11 (6) | un ordre stop-limit de vente exige le stop sous la limite, à l'envers ; un ordre nul dans un lot lève `NullReferenceException` | literal |
| 03-insurance | [D](domains/03-insurance/NOTES.discovery.md) [R](domains/03-insurance/NOTES.regular.md) [E](domains/03-insurance/NOTES.expert.md) | 13 (8) | une police valide de 367 jours ne peut jamais être renouvelée ; un sinistre déclaré deux fois compte deux fois au renouvellement ; `BonusMalus.AfterYear` perd un centime en virgule flottante binaire | cases |
| 04-manufacturing | [D](domains/04-manufacturing/NOTES.discovery.md) [R](domains/04-manufacturing/NOTES.regular.md) [E](domains/04-manufacturing/NOTES.expert.md) | 11 (4) | `Cadence.TimeToProduce` déborde `TimeSpan` avec une exception sans rapport ; `$` et `\d` dans deux regex | literal |
| 05-cad-drawing | [D](domains/05-cad-drawing/NOTES.discovery.md) [R](domains/05-cad-drawing/NOTES.regular.md) [E](domains/05-cad-drawing/NOTES.expert.md) | 21 (9) | `FitKind.Interference` ne peut jamais être produit ; `StandardTolerance` laisse passer NaN et indexe hors limites ; les totaux de nomenclature débordent `int` en silence | literal |
| 06-warehouse | [D](domains/06-warehouse/NOTES.discovery.md) [R](domains/06-warehouse/NOTES.regular.md) [E](domains/06-warehouse/NOTES.expert.md) | 16 (5) | un poids unitaire non borné déborde `WeightKg` et `InspectionReasons` (graine −113536279) ; `TemperatureRange` signale un maximum excessif sur `minCelsius` | draw |
| 07-freight | [D](domains/07-freight/NOTES.discovery.md) [R](domains/07-freight/NOTES.regular.md) [E](domains/07-freight/NOTES.expert.md) | 7 (3) | `ContainerCode.Create` avec un propriétaire trop court lève `IndexOutOfRangeException` au lieu de `FormatException` | literal |
| 08-patient-record | [D](domains/08-patient-record/NOTES.discovery.md) [R](domains/08-patient-record/NOTES.regular.md) [E](domains/08-patient-record/NOTES.expert.md) | 12 (6) | `AgeOn` est négatif avant la naissance et une naissance un 29 février vieillit le 28 février ; les dates de dispensation débordent près de `DateOnly.MaxValue` | literal |
| 09-laboratory | [D](domains/09-laboratory/NOTES.discovery.md) [R](domains/09-laboratory/NOTES.regular.md) [E](domains/09-laboratory/NOTES.expert.md) | 11 (5) | `ReferenceRange` accepte des limites critiques NaN et `Classify(NaN)` renvoie Normal ; `LabReport.Results` modifiable par un cast | literal |
| 10-pharma-serialisation | [D](domains/10-pharma-serialisation/NOTES.discovery.md) [R](domains/10-pharma-serialisation/NOTES.regular.md) [E](domains/10-pharma-serialisation/NOTES.expert.md) | 7 (4) | `Gtin(string)` accepte des chiffres non ASCII que `Gtin.FromBody` refuse ; `$` dans quatre regex | literal |
| 11-telecom | [D](domains/11-telecom/NOTES.discovery.md) [R](domains/11-telecom/NOTES.regular.md) [E](domains/11-telecom/NOTES.expert.md) | 15 (6) | `BillFor` et `OverageFor` débordent sur des entrées acceptées ; un forfait « priced in cents » accepte des fractions de centime | cases |
| 12-smart-grid | [D](domains/12-smart-grid/NOTES.discovery.md) [R](domains/12-smart-grid/NOTES.regular.md) [E](domains/12-smart-grid/NOTES.expert.md) | 16 (8) | `Meter.Record` remplace la dernière lecture avant de la valider, corrompant le compteur en cas de refus (haute) ; `DailyLoadCurve` garde la liste de l'appelant | literal, au travers de compteurs construits par générateur |
| 13-payroll | [D](domains/13-payroll/NOTES.discovery.md) [R](domains/13-payroll/NOTES.regular.md) [E](domains/13-payroll/NOTES.expert.md) | 12 (6) | aucun plafond sur une ligne de fiche de paie, si bien que le salaire net déborde `decimal` (64 fiches tirées sur 3 000) ; lignes nulles acceptées ; un contrat embauché après 2100 ne peut jamais être payé | draw |
| 14-ecommerce | [D](domains/14-ecommerce/NOTES.discovery.md) [R](domains/14-ecommerce/NOTES.regular.md) [E](domains/14-ecommerce/NOTES.expert.md) | 14 (7) | `GiftCard.Redeem` accepte un montant négatif et augmente le solde ; `Money` n'a pas de maximum, si bien que les totaux débordent | draw (débordement), literal |
| 15-hotel-booking | [D](domains/15-hotel-booking/NOTES.discovery.md) [R](domains/15-hotel-booking/NOTES.regular.md) [E](domains/15-hotel-booking/NOTES.expert.md) | 11 (6) | `Reservation.Cancel` positionne le statut avant de calculer la pénalité ; débordement de `Money` dans `Cancel` (graine 350311511) ; le motif d'e-mail refuse `user@example.co.uk` | draw |
| 16-aviation | [D](domains/16-aviation/NOTES.discovery.md) [R](domains/16-aviation/NOTES.regular.md) [E](domains/16-aviation/NOTES.expert.md) | 15 (6) | `AircraftRegistration("")` lève `IndexOutOfRangeException` ; la branche à 8 caractères du motif du numéro N est morte ; un créneau accepte des ticks inférieurs à la milliseconde | cases |
| 17-real-estate | [D](domains/17-real-estate/NOTES.discovery.md) [R](domains/17-real-estate/NOTES.regular.md) [E](domains/17-real-estate/NOTES.expert.md) | 15 (8) | un `\n` final contourne la règle du numéro de plan 0000 ; pas de plafond de loyer, si bien que les dépôts débordent ; `%2F` décodé en séparateur de chemin | cases |
| 18-education | [D](domains/18-education/NOTES.discovery.md) [R](domains/18-education/NOTES.regular.md) [E](domains/18-education/NOTES.expert.md) | 11 (6) | un numéro d'étudiant avec des chiffres non ASCII fait lever `FormatException` à `IntakeYear` ; rien ne lie la date de naissance au semestre | cases |
| 19-iam | [D](domains/19-iam/NOTES.discovery.md) [R](domains/19-iam/NOTES.regular.md) [E](domains/19-iam/NOTES.expert.md) | 8 (3) | l'action d'un scope devient `"read\n"` ; `AllowedScopes` et `RedirectUris` sont des ensembles vivants, si bien qu'une liste d'autorisation s'élargit par un cast | literal |
| 20-agritech-iot | [D](domains/20-agritech-iot/NOTES.discovery.md) [R](domains/20-agritech-iot/NOTES.regular.md) [E](domains/20-agritech-iot/NOTES.expert.md) | 10 (4) | une humidité NaN empoisonne la moyenne et désactive l'irrigation ; `IrrigationPolicy` ne refuse NaN que par son bit de signe | literal (depuis `MODEL.md`) |
| 21-orbital-microbiology | [D](domains/21-orbital-microbiology/NOTES.discovery.md) [R](domains/21-orbital-microbiology/NOTES.regular.md) [E](domains/21-orbital-microbiology/NOTES.expert.md) | 12 (8) | un facilitateur très productif mène l'espèce aidée à l'extinction (graine −1087769823) ; `Advance` dépend de la façon dont l'appelant découpe le temps | draw |
| 22-pulmonary-nodules | [D](domains/22-pulmonary-nodules/NOTES.discovery.md) [R](domains/22-pulmonary-nodules/NOTES.regular.md) [E](domains/22-pulmonary-nodules/NOTES.expert.md) | 7 (4) | `Track` laisse fuir `ArgumentOutOfRangeException` quand le centroïde projeté sort du champ ; un repère localisé deux fois compte pour deux | literal |
| 23-captive-breeding | [D](domains/23-captive-breeding/NOTES.discovery.md) [R](domains/23-captive-breeding/NOTES.regular.md) [E](domains/23-captive-breeding/NOTES.expert.md) | 10 (4) | `RecordDeath` peut rompre « dam not dead before the birth » après l'enregistrement ; `RankSiresFor` liste des mâles que `Propose` refuse | literal |
| 24-radiocarbon-chronology | [D](domains/24-radiocarbon-chronology/NOTES.discovery.md) [R](domains/24-radiocarbon-chronology/NOTES.regular.md) [E](domains/24-radiocarbon-chronology/NOTES.expert.md) | 10 (5) | un coquillage marin est accepté, puis `CorrectedAge` lève une exception ; un âge au-delà de la courbe n'est refusé que si la vraisemblance fait un dépassement par le bas | cases |
| 25-adaptive-video | [D](domains/25-adaptive-video/NOTES.discovery.md) [R](domains/25-adaptive-video/NOTES.regular.md) [E](domains/25-adaptive-video/NOTES.expert.md) | 16 (7) | NaN passe les gardes `<= 0` ; un tampon presque vide monte tout de même d'un échelon ; les compteurs débordent à `int.MaxValue` en un refus trompeur | cases |
| 26-robotic-milking | [D](domains/26-robotic-milking/NOTES.discovery.md) [R](domains/26-robotic-milking/NOTES.regular.md) [E](domains/26-robotic-milking/NOTES.expert.md) | 13 (7) | un démarrage refusé reste dans les anomalies ; la phase régresse de Milking à Attaching ; un quartier sous le seuil de détachement ne se termine jamais | cases |
| 27-railway-interlocking | [D](domains/27-railway-interlocking/NOTES.discovery.md) [R](domains/27-railway-interlocking/NOTES.regular.md) [E](domains/27-railway-interlocking/NOTES.expert.md) | 14 (6) | `RequestRoute` n'est pas atomique quand le temps de verrouillage est celui par défaut (haute) ; trois motifs de refus sont inatteignables | cases |
| 28-satellite-conjunction | [D](domains/28-satellite-conjunction/NOTES.discovery.md) [R](domains/28-satellite-conjunction/NOTES.regular.md) [E](domains/28-satellite-conjunction/NOTES.expert.md) | 9 (4) | `PositionCovariance.Combine` refuse la somme de deux covariances valides, si bien qu'`Assess` lève une exception pour des trajectoires valides (haute) ; `Vector3` fait des dépassements par le bas et par le haut ; l'interpolation peut passer sous la surface de la Terre | draw (les deux premiers) |
| 29-parimutuel-betting | [D](domains/29-parimutuel-betting/NOTES.discovery.md) [R](domains/29-parimutuel-betting/NOTES.regular.md) [E](domains/29-parimutuel-betting/NOTES.expert.md) | 7 (4) | aucun plafond sur un dividende, si bien que `Payout` déborde ; `Settle` ne vérifie la course que par le code | draw |
| 30-liquidity-gap | [D](domains/30-liquidity-gap/NOTES.discovery.md) [R](domains/30-liquidity-gap/NOTES.regular.md) [E](domains/30-liquidity-gap/NOTES.expert.md) | 8 (4) | un échéancier près de `DateOnly.MaxValue` échoue avec une exception brute du framework (1 exécution sur 6) ; des flux contractuels arrondis de deux façons différentes | draw |
| 31-vessel-stowage | [D](domains/31-vessel-stowage/NOTES.discovery.md) [R](domains/31-vessel-stowage/NOTES.regular.md) [E](domains/31-vessel-stowage/NOTES.expert.md) | 3 (3) | un nombre pair de rangées donne plus de rangées à tribord qu'à bâbord ; `Rotation.Ports` contourne les gardes du constructeur | cases |
| 32-grid-load-flow | [D](domains/32-grid-load-flow/NOTES.discovery.md) [R](domains/32-grid-load-flow/NOTES.regular.md) [E](domains/32-grid-load-flow/NOTES.expert.md) | 6 (4) | `Bus.NetInjection` lève une exception pour un bus valide (graine −2128493443) ; `BusId` accepte `"ABC\n"` | draw, literal |
| 33-music-score | [D](domains/33-music-score/NOTES.discovery.md) [R](domains/33-music-score/NOTES.regular.md) [E](domains/33-music-score/NOTES.expert.md) | 6 (4) | la somme de deux durées valides peut être non représentable ; liaisons enharmoniques refusées | draw |
| 34-manuscript-reconstruction | [D](domains/34-manuscript-reconstruction/NOTES.discovery.md) [R](domains/34-manuscript-reconstruction/NOTES.regular.md) [E](domains/34-manuscript-reconstruction/NOTES.expert.md) | 6 (3) | la longueur d'une variante est vérifiée avant la normalisation ; `WitnessSiglum("A\n")` accepté | literal |
| 35-dna-variants | [D](domains/35-dna-variants/NOTES.discovery.md) [R](domains/35-dna-variants/NOTES.regular.md) [E](domains/35-dna-variants/NOTES.expert.md) | 14 (7) | `ApplyAll` dépend de l'ordre d'enregistrement pour une insertion et un changement à une même position (haute) ; une insertion avant la première base se normalise en `g.0_1ins` | cases |
| 36-knitting-patterns | [D](domains/36-knitting-patterns/NOTES.discovery.md) [R](domains/36-knitting-patterns/NOTES.regular.md) [E](domains/36-knitting-patterns/NOTES.expert.md) | 10 (5) | une largeur tirée de 6,86e28 déborde `Gauge` ; un patron dont `Expand()` ne peut jamais fonctionner est accepté | draw |
| 37-batch-genealogy | [D](domains/37-batch-genealogy/NOTES.discovery.md) [R](domains/37-batch-genealogy/NOTES.regular.md) [E](domains/37-batch-genealogy/NOTES.expert.md) | 12 (8) | `Record` n'est pas tout-ou-rien (haute) ; le plafond de quantité de 1e9 casse les conversions de quantités valides ; les parts décimales ne totalisent pas 1 | draw |
| 38-carbon-ledger | [D](domains/38-carbon-ledger/NOTES.discovery.md) [R](domains/38-carbon-ledger/NOTES.regular.md) [E](domains/38-carbon-ledger/NOTES.expert.md) | 13 (7) | les opérations refusées lèvent `ArgumentException`, non la `LedgerRuleViolationException` du modèle ; un journal « append-only » expose son stockage | literal |
| 39-geothermal-reservoir | [D](domains/39-geothermal-reservoir/NOTES.discovery.md) [R](domains/39-geothermal-reservoir/NOTES.regular.md) [E](domains/39-geothermal-reservoir/NOTES.expert.md) | 5 (3) | `IsFaceNeighbourOf` déborde `int` et rapporte des cellules éloignées comme voisines ; un pas au pas stable maximal est toujours refusé pour un injecteur | cases |
| 40-catastrophe-losses | [D](domains/40-catastrophe-losses/NOTES.discovery.md) [R](domains/40-catastrophe-losses/NOTES.regular.md) [E](domains/40-catastrophe-losses/NOTES.expert.md) | 13 (8) | aucun plafond sur les valeurs assurées, si bien que les sommes débordent (114 portefeuilles tirés sur 20 000) ; `DistanceKmTo` est NaN pour certaines paires antipodales | draw (débordement), literal |
| 41-shift-planning-time-zones | [D](domains/41-shift-planning-time-zones/NOTES.discovery.md) [R](domains/41-shift-planning-time-zones/NOTES.regular.md) [E](domains/41-shift-planning-time-zones/NOTES.expert.md) | 11 (4) | `IsoWeek(9999, 52)` lève une exception ; un roulement accepte un employé dont le site d'attache ne partage qu'un identifiant | literal |
| 42-token-ledger | [D](domains/42-token-ledger/NOTES.discovery.md) [R](domains/42-token-ledger/NOTES.regular.md) [E](domains/42-token-ledger/NOTES.expert.md) | 9 (4) | `$` en quatre endroits, dont `ExactDecimal.Parse` ; `NonceExhausted` inatteignable | literal |
| 43-passive-components | [D](domains/43-passive-components/NOTES.discovery.md) [R](domains/43-passive-components/NOTES.regular.md) [E](domains/43-passive-components/NOTES.expert.md) | 11 (6) | `MODEL.md` donne la mauvaise valeur pour le `104` d'une inductance ; `PreferredValue.CompareTo` vaut 0 alors qu'`Equals` est faux ; un coefficient de température de −0,0 refusé | literal |
| 44-vaccine-cold-chain | [D](domains/44-vaccine-cold-chain/NOTES.discovery.md) [R](domains/44-vaccine-cold-chain/NOTES.regular.md) [E](domains/44-vaccine-cold-chain/NOTES.expert.md) | 14 (6) | des zéros non ASCII et un `\n` final contournent la règle de la séquence de zéros ; `HistoryOf` expose un historique dont `Record` contourne les règles de l'expédition | literal |
| 45-healthcare-claims | [D](domains/45-healthcare-claims/NOTES.discovery.md) [R](domains/45-healthcare-claims/NOTES.regular.md) [E](domains/45-healthcare-claims/NOTES.expert.md) | 10 (6) | des zéros arabes-indiens contournent le refus de la séquence de zéros ; `Rates` expose son dictionnaire | literal |
| 46-network-acl | [D](domains/46-network-acl/NOTES.discovery.md) [R](domains/46-network-acl/NOTES.regular.md) [E](domains/46-network-acl/NOTES.expert.md) | 3 (1) | `RuleId("a\n")` accepté | literal |
| 47-international-catalogue | [D](domains/47-international-catalogue/NOTES.discovery.md) [R](domains/47-international-catalogue/NOTES.regular.md) [E](domains/47-international-catalogue/NOTES.expert.md) | 10 (5) | les tables d'écritures omettent 々, l'okina, le latin pleine chasse et le katakana demi-chasse, si bien que des noms ordinaires sont refusés ; `Merge` laisse deux enregistrements qu'`Add` refuserait | literal (données réalistes) |
| 48-tax-rules-engine | [D](domains/48-tax-rules-engine/NOTES.discovery.md) [R](domains/48-tax-rules-engine/NOTES.regular.md) [E](domains/48-tax-rules-engine/NOTES.expert.md) | 7 (3) | une somme de revenus au-delà de `Money.MaxAmount` lève une `ArgumentOutOfRangeException` brute | cases |
| 49-clinical-trial-crf | [D](domains/49-clinical-trial-crf/NOTES.discovery.md) [R](domains/49-clinical-trial-crf/NOTES.regular.md) [E](domains/49-clinical-trial-crf/NOTES.expert.md) | 7 (3) | `VitalSigns.IsAnyObserved` ignore la pression diastolique ; cinq motifs acceptent un `\n` final | literal (par lecture du code) |
| 50-fx-settlement | [D](domains/50-fx-settlement/NOTES.discovery.md) [R](domains/50-fx-settlement/NOTES.regular.md) [E](domains/50-fx-settlement/NOTES.expert.md) | 8 (4) | `Book` laisse échapper une `InvalidOperationException` brute pour une référence non cotable ; les positions cumulées ne sont pas bornées | literal, un amené par un tirage |
| **Total** | | **536 (264)** | | |

Les cas ignorés (601) et les constats de domaine (536) sont des décomptes différents : un défaut peut être conservé sous forme de plusieurs cas ignorés, et une session peut trouver un défaut qu'une autre rapporte à nouveau.

---

## 10. Méthode

- **Domaines.** 01–40 existaient. 01–20 ont reçu pour cet essai un `MODEL.md` écrit à partir de leur code ; 21–40 ont gardé le leur. 41–50 sont nouveaux, chacun construit autour d'une spécificité : 41 fuseaux horaires et heure d'été, 42 nombres au-delà de `decimal` (`BigInteger`, `UInt128`), 43 très petites et très grandes grandeurs (pF à TΩ), 44 transversal à 06, 07, 10 et 20, 45 transversal à 03, 08 et 01, 46 types au niveau du bit et non signés (IPv6 en `UInt128`, `[Flags]`), 47 Unicode au-delà d'ASCII (NFC, graphèmes, bidi), 48 polymorphisme et arbres récursifs, 49 valeurs absentes (nullable, motifs de données manquantes), 50 une échelle décimale qui dépend de la devise, avec arrondi explicite.
- **Sessions.** Trois profils indépendants par domaine — découverte, habitué, expert — soit 150 sessions. Chacune s'est déroulée dans un espace de travail isolé hors du dépôt, ne contenant que le domaine, son `MODEL.md`, la documentation utilisateur, le `any` empaqueté et les packages `1.0.0-uxtrial.1` empaquetés depuis `main` à l'époque. Aucune session n'a vu le travail d'une autre, les suites ou rapports précédents, ni les sources de la bibliothèque. Chaque session a tourné sur le même modèle et au même niveau d'effort ; la vérification a tourné sur une configuration plus puissante. Chaque session a scaffoldé ses générateurs avec `any`, écrit des tests xUnit sur des cas réels, lancé sa suite à plusieurs reprises avec de nouvelles graines, conservé chaque défaut de domaine sous forme de test ignoré, consigné son travail dans `NOTES.<profil>.md` et noté sa fluidité.
- **Ce qui est conservé.** Chaque domaine contient ses trois suites de profil (`Tests.Discovery`, `Tests.Regular`, `Tests.Expert`) et leurs notes. Pour 01–20, la suite `Tests` du premier essai, avec ses sondes, est conservée à côté ; pour 21–40, les suites et notes du deuxième essai ont été remplacées (l'historique git les garde).
- **Constats.** 1 530 au total, chacun avec une nature, un composant, une gravité, des preuves et une reproduction. Les identifiants de constat se lisent `<domaine><D|R|E>-<n>`.
- **Vérification.** Les 994 constats portant sur la bibliothèque, l'outil et la documentation ont été regroupés en 151 causes racines. Chaque cause racine a été vérifiée de façon contradictoire : un vérificateur a tenté de la réfuter par reproduction sur les packages empaquetés et le `any` de l'essai, puis a rendu un verdict (confirmé, partiel, par conception, réfuté, non reproduit), une nature et une gravité réévaluées, sa relation à REPORT.md et REPORT-2.md, et une recommandation. Ce rapport emploie les gravités réévaluées. Les espaces de travail de vérification vivaient hors du dépôt et ne sont pas commités ; la reproduction de chaque élément suffit à les reconstruire.
- **Défauts de domaine.** Les 536 constats de domaine ont été regroupés en 264 grappes et non vérifiés ([§9](#9-défauts-de-domaine-trouvés-par-les-sessions)).
- **Exécution du coordinateur.** Le coordinateur a lancé les 150 suites depuis le dépôt : 43 867 cas, 0 en échec, 601 ignorés, après le seul correctif décrit au [§2](#2-limites-de-cet-essai), point 7.
- **Identifiants.** `CORE-n`, `CLI-n`, `AN-n`, `XU-n` et `DOCS-n` sont stables à l'intérieur de ce rapport et sans rapport avec les identifiants d'apparence semblable de REPORT.md et REPORT-2.md, toujours cités avec le nom de leur rapport.
