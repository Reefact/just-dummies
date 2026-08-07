# ADR-0050 | Nommer une règle supprimée par une constante de catalogue, pas par une chaîne littérale

🌍 🇬🇧 [English](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-02
**Decision Makers:** Reefact

## Contexte

Ce dépôt porte **83 attributs `[SuppressMessage]`**, et aucun `#pragma warning disable` : l'attribut est
la façon dont une règle est silenciée ici. Chacun nomme sa règle par deux chaînes littérales — une
catégorie et un identifiant — que rien ne vérifie.

Ces littéraux ne sont pas fragiles de la façon évidente. Un identifiant mal orthographié laisse la
règle active, le diagnostic se déclenche, et le cliquet des warnings de la CI en fait une erreur : cet
échec est bruyant. Ce qui est silencieux, c'est le cas inverse. Quand un éditeur renomme ou retire une
règle, l'attribut continue de compiler, ne silence plus rien, et aucun build ne le dit. Les suppressions
mortes s'accumulent, et le code prétend silencier ce qu'il ne silence plus.

L'exposition est concentrée plutôt que diffuse : **13 règles distinctes couvrent 67 des 83**
suppressions — `S3267` à elle seule apparaît 14 fois, `S107` 9, `S2436`, `S2325` et `CA1822` 7 chacune.
Un seul renommage côté éditeur touche donc plusieurs fichiers d'un coup, dans des projets dont les
auteurs n'ont aucune raison de regarder.

`DiagnosticCatalog` publie les règles d'un package d'analyseurs sous forme de membres `const string`
générés depuis les descripteurs de l'analyseur lui-même, de sorte que `[SuppressMessage]` peut prendre
des références que le compilateur résout. Son catalogue `DiagnosticCatalog.Sonar` reflète
**SonarAnalyzer.CSharp 10.31.0.145097** — la version exacte que ce dépôt épingle — et
`DiagnosticCatalog.NetAnalyzers` reflète les règles `CA` du SDK. À eux deux, ils décrivent **toutes** les
règles `S` et `CA` supprimées ici.

## Décision

Une suppression nomme sa règle par une constante de catalogue. `DiagnosticCatalog.Sonar` et
`DiagnosticCatalog.NetAnalyzers` sont référencés pour tous les projets, en actifs de compilation
uniquement, et les analyseurs `DCAT` qu'ils embarquent maintiennent la règle appliquée à leur sévérité
par défaut.

Les diagnostics `JD` du produit restent en littéraux : aucun catalogue ne les décrit encore.

## Justification

**Cela transforme une panne silencieuse en échec de build.** Une règle retirée laisse aujourd'hui un
attribut qui se lit comme une suppression et n'en est pas une. Une constante ne survit pas à la
disparition de sa règle du catalogue : la montée de catalogue suivante la signale. C'est là toute la
valeur ; la protection contre la faute de frappe n'en est pas une, puisqu'une faute de frappe est déjà
bruyante ici.

**L'exposition justifie la dépendance.** 67 des 83 suppressions se concentrent sur 13 règles, c'est-à-dire
le cas où un renommage éditeur est un événement multi-fichiers et non une correction d'une ligne.
Mesuré, pas supposé.

**Le catalogue est épinglé sur l'analyseur que ce dépôt exécute réellement.** Le catalogue Sonar reflète
le même `10.31.0.145097` épinglé dans `Directory.Packages.props` : les constantes décrivent donc les
règles que ce build rapporte, et non celles d'une version voisine.

**Central, et non par projet, parce qu'une lacune ne se voit pas.** Tous les projets ici exécutent ces
analyseurs, donc tous peuvent en supprimer une règle. Un projet laissé sans catalogue est un projet où un
nouveau littéral atterrit sans contrôle, et rien ne le distingue d'un projet converti à la lecture.

**Cela ne coûte rien à l'artefact publié, et c'est vérifié plutôt qu'argumenté.** Les références sont
de compilation uniquement ; le `.nuspec` packagé ne déclare aucune dépendance et le package ne porte
aucun fichier de catalogue. L'assembly émis est inchangé : `SuppressMessageAttribute` est conditionné
par `CODE_ANALYSIS` et n'est jamais émis, et une comparaison octet à octet de la bibliothèque construite
avant et après la conversion ne diffère que par l'identité du module (MVID), les deux horodatages
déterministes, et la signature et le checksum du PDB — 72 octets, tous dérivés du texte source, aucun
n'étant du code.

**Aucune rampe d'adoption n'a été nécessaire, ce qui est en soi l'argument pour le faire en un seul
commit.** Les règles `DCAT` sont livrées en erreurs, et le guide destiné aux bases existantes prévoit un
abaissement temporaire via `.editorconfig`. Aucun des deux diagnostics n'en a eu besoin ici : les 83
suppressions portaient déjà un `Justification`, et la conversion a atterri d'un bloc. Adopter à la
sévérité par défaut est ce qui rend un nouveau littéral non mergeable dès le premier commit.

## Alternatives considérées

### Garder les littéraux

Rejeté : cela conserve le cas silencieux. Rien ne signale une suppression qui a cessé de supprimer, et
avec 13 règles réparties sur 67 attributs, le jour où un renommage arrive est le jour où plusieurs
fichiers cessent discrètement de vouloir dire ce qu'ils disent.

### Ne référencer les catalogues que dans les projets qui suppriment aujourd'hui

La lecture minimale de « pas de dépendance sans raison ». Rejeté : la lacune laissée est invisible. Un
projet sans catalogue accepte un nouveau littéral en silence, et un lecteur ne peut pas savoir quels
projets sont couverts sans ouvrir chaque `.csproj`.

### Convertir progressivement derrière un abaissement `.editorconfig`

Le chemin que décrit le guide d'adoption du catalogue lui-même, pour les bases qui ne peuvent pas
convertir d'un coup. Rejeté comme inutile ici plutôt que comme mauvais : cette base satisfaisait les deux
sévérités par défaut dès l'arrivée, donc un abaissement n'aurait acheté qu'une migration plus lente et
une fenêtre où de nouveaux littéraux pouvaient atterrir.

### Écrire nos propres constantes

Un fichier de `const string` par règle, sans dépendance. Rejeté : c'est la même maintenance que
l'éditeur fait déjà, faite moins bien — une constante écrite à la main ne peut pas remarquer que sa
règle a été retirée, ce qui est précisément la panne dont parle cet enregistrement.

## Conséquences

### Positives

* Une règle qui disparaît lors d'une montée d'analyseur est signalée au lieu de laisser une suppression
  morte.
* « Où cette règle est-elle supprimée, et pourquoi ? » devient un *Find All References*, pas une
  recherche textuelle.
* Un nouveau littéral de suppression ne peut plus être mergé, dans aucun projet, dès le premier commit.

### Négatives

* Deux dépendances de compilation là où il n'y en avait aucune, sur des packages du même auteur que ce
  dépôt. « Dogfooding » est le mot honnête, et il coupe dans les deux sens : un défaut du catalogue est
  un défaut que ce dépôt rencontre en premier.
* Une montée d'analyseur veut désormais que son catalogue suive, sans quoi les constantes décrivent une
  version que le build n'exécute plus. L'appariement des versions est écrit là où les deux sont déclarés.

### Risques

* Les catalogues sont générés depuis les descripteurs d'un éditeur et ne sont pas officiels. Une règle
  que le générateur manque est une règle non référençable ; la forme littérale compile toujours, donc le
  repli est le statu quo et non un mur.
* `DCAT0006` et `DCAT0014` sont des erreurs. Une future montée d'analyseur introduisant des suppressions
  plus vite que le catalogue ne les couvre bloquerait un build ; l'abaissement `.editorconfig` que décrit
  le guide d'adoption est la soupape, et il a délibérément été inutile pour arriver ici.

## Actions de suivi

* Les 7 suppressions `JD` restent des littéraux. Publier un catalogue pour les diagnostics `JD` propres à
  ce produit fermerait cette lacune **et** donnerait aux consommateurs des suppressions vérifiées des
  règles que JustDummies livre — une question de produit, à peser contre
  [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) après la 1.0.

## Références

* [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) — l'exigence d'autonomie que cet
  enregistrement devait satisfaire, et que `tools/packaging/pack.sh` vérifie sur l'artefact produit.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la règle contre laquelle
  cet enregistrement a été pesé, et celle à laquelle l'action de suivi devra répondre.
* `Directory.Build.props` — où les références et les usings globaux sont déclarés, et pourquoi là.
