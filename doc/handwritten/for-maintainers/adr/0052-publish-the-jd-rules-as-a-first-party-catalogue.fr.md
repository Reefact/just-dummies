# ADR-0052 | Publier les règles JD comme catalogue first-party, et y lire les descripteurs

🌍 🇬🇧 [English](0052-publish-the-jd-rules-as-a-first-party-catalogue.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-02
**Accepted:** 2026-08-02
**Decision Makers:** Reefact

## Contexte

L'[ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) a converti toutes les
suppressions de ce dépôt en constantes de catalogue — sauf sept. Celles-ci nomment des règles `JD`,
qu'aucun catalogue ne décrivait, et sont restées des littéraux. Qui consomme JustDummies est dans la
même situation, en pire : il supprime des règles **que ce produit publie**, avec des chaînes que rien
ne vérifie, et il n'a aucun moyen de faire autrement.

L'exposition n'est pas hypothétique. `JD001`–`JD028` sont un contrat public — le dépôt traite déjà le
renommage d'un identifiant de diagnostic comme un changement cassant — et chacune est atteignable
depuis le `[SuppressMessage]` d'un consommateur.

À l'intérieur du produit, les mêmes chaînes sont déjà transcrites deux fois. `DiagnosticIds` porte
l'identifiant, `DiagnosticCategories` la catégorie, et `Descriptors` les assemble dans le
`DiagnosticDescriptor` avec lequel l'analyseur rapporte. La suppression d'un consommateur est une
troisième transcription, dans son assembly, et rien sur la plateforme ne compare les trois.

`DiagnosticCatalog` prend exactement ce cas en charge, et le distingue du fait de refléter l'analyseur
d'un tiers : quand le même projet possède l'analyseur **et** le catalogue, le descripteur peut lire les
constantes du catalogue, et les deux cessent d'être des copies indépendantes d'une même chaîne.

## Décision

Les règles `JD` sont publiées sous le nom `JustDummies.DiagnosticCatalog`, sur son propre train de
release, et `JustDummies.Analyzers` lit dans ce catalogue l'identifiant, la catégorie, le titre et le
lien d'aide de ses descripteurs.

## Justification

**La suppression d'un consommateur est le cas que le produit ne peut vérifier autrement.** Les sept
littéraux d'ici auraient pu rester tels quels. Ceux du code d'un consommateur non : ils silencient des
règles que JustDummies livre, et quand une règle est retirée ou recatégorisée, leur attribut continue
de compiler et ne silence plus rien. Publier le catalogue est le seul moyen de rendre cette panne
visible à celui à qui elle arrive.

**La boucle est la raison d'être d'un catalogue first-party.** Le descripteur lisant le catalogue, la
règle que l'analyseur *rapporte* et la règle qu'un consommateur *silencie* sont la même valeur par
construction. La catégorie surtout : c'est une chaîne que seul ce produit publie, que rien ne vérifie,
et « par diligence » est précisément ce qui échoue.

**Le sens est décidé par l'artefact écrit à la main.** Ces déclarations sont écrites à la main, donc le
descripteur est alimenté depuis elles. L'inverse — générer le catalogue depuis les descripteurs — est
ce que doit faire un dépôt qui génère ses catalogues, et il lui faut alors une vérification de
régénération pour remplacer la boucle. Ici l'alimentation est disponible, et elle est plus forte : rien
à exécuter, rien à vérifier, le compilateur l'impose.

**Cela ne coûte rien à l'analyseur livré.** Les membres sont `const`, donc le compilateur substitue
leurs valeurs et l'analyseur construit ne porte aucune référence à résoudre au chargement — ce qui
importe, puisqu'il est chargé depuis le package de la bibliothèque par le compilateur de chaque
consommateur, sur un plancher Roslyn épinglé (ADR-0001).

**Son propre train, parce qu'il versionne sur autre chose.** Le catalogue bouge quand l'ensemble de
règles bouge — une règle ajoutée, retirée, recatégorisée. Ce n'est pas quand la bibliothèque bouge, ni
quand l'adaptateur bouge. Le lier à l'un ou l'autre obligerait à sortir une release sans contenu de
l'un pour publier l'autre, c'est-à-dire le couplage que
l'[ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) a supprimé.

## Alternatives considérées

### Laisser les suppressions `JD` en littéraux

Le statu quo, et le plus petit changement. Rejeté : cela accepte, pour les règles propres à ce produit,
exactement la panne silencieuse que l'ADR-0050 a refusée pour celles de tout le monde, et cela laisse
les consommateurs sans aucune option.

### Publier le catalogue depuis `Reefact/diagnostic-catalog`, à côté des treize autres

Il hériterait du générateur et de la chaîne de publication de ce dépôt. Rejeté : il devrait lire les
descripteurs d'un `JustDummies.Analyzers` **publié**, donc le catalogue ne pourrait jamais être généré
avant la release qu'il décrit et traînerait toujours d'une version — un catalogue décrivant des règles
que ses utilisateurs n'ont peut-être pas, c'est-à-dire le défaut que cet enregistrement existe pour
supprimer.

### Générer le catalogue depuis les descripteurs, dans ce dépôt

Le sens qu'utilise le dépôt de la fondation lui-même. Rejeté ici parce que la boucle est disponible :
les déclarations sont écrites à la main, donc le descripteur peut les lire et aucune vérification de
régénération n'est nécessaire. Générer mettrait aussi le texte des règles derrière un outil, pour un
ensemble qui change quelques fois par an.

### Ne livrer que `Id` et `Category`

Suffisant pour une suppression. Rejeté : `Title` est ce que l'IDE d'un consommateur affiche au survol de
la constante — là où va la prose une fois que la suppression cesse de la porter — et `HelpLinkUri` se
compose depuis l'identifiant à la compilation, pour rien. `MessageFormat` et `Description` ne sont
délibérément **pas** publiés : les paramètres du format de message sont couplés aux sites d'appel de
l'analyseur, et ni l'un ni l'autre n'est ce qu'une suppression nomme.

## Conséquences

### Positives

* La suppression d'une règle `JD` par un consommateur est vérifiée à la compilation, et les analyseurs
  DCAT signalent la forme littérale et proposent le correctif.
* L'identifiant, la catégorie et le titre existent une seule fois pour tout le produit, si bien que
  l'analyseur et une suppression ne peuvent pas diverger.
* Les sept dernières suppressions littérales de ce dépôt sont converties : il n'en reste aucune.

### Négatives

* Un quatrième package à publier, avec son changelog, son train et sa politique nuget.org.
* Le premier package d'ici à déclarer une **vraie** dépendance. `JustDummies` et `JustDummies.Xunit`
  gardent `PrivateAssets="all"` sur les catalogues qu'ils consomment ; celui-ci ne le doit pas, car un
  package qui *publie* un catalogue doit laisser la fondation atteindre ses consommateurs — elle porte
  les marqueurs que les déclarations arborent et les analyseurs qui vérifient leurs suppressions.
  L'ADR-0003 n'est pas touché : il porte sur la bibliothèque, dont le `.nuspec` ne déclare toujours
  rien.
* `JustDummies.DiagnosticCatalog` est le seul projet d'ici qui ne peut pas se faire analyser par les
  analyseurs JustDummies : ils lisent leurs descripteurs dedans, donc les référencer en retour est un
  cycle de build. Il déclare des constantes et aucune instruction exécutable, donc ces règles n'ont rien
  sur quoi se déclencher.

### Risques

* Une règle ajoutée à l'analyseur et pas au catalogue est invisible pour un compilateur. C'est pourquoi
  `CatalogueCoverageTests` compare les deux par réflexion, dans les deux sens, depuis les artefacts
  livrés plutôt que depuis une liste — une liste serait une quatrième transcription réclamant le même
  garde-fou.
* Les règles de stabilité d'un catalogue sont plus strictes que celles d'un package ordinaire : un
  membre est `const`, donc inliné dans l'assembly d'un consommateur à *sa* compilation, et en supprimer
  un casse son build avec un message ne nommant rien de ce qu'il a écrit. Une règle retirée du produit
  est reportée en `[Obsolete]`, jamais supprimée. Rien ne l'impose ; le changelog et cet enregistrement
  sont l'endroit où c'est écrit.

## Actions de suivi

* Trancher l'icône du package. La convention de la fondation badge l'icône d'un catalogue avec le
  **préfixe des règles qu'il contient** (`JD`), posé sur la marque de la famille DiagnosticCatalog —
  qui est l'identité de ce projet-là, pas celle de ce produit. Le package porte pour l'instant la marque
  JustDummies, qui dit de quel produit il relève et non quelles règles il contient. Laissé ouvert
  délibérément.
  * 2026-08-02 — tranché, et pas dans les termes de cet enregistrement. Les trois packages portent UNE
    seule marque, un mannequin de crash-test, sans aucun badge par package : une liste nuget.org rend
    l'icône à 128 px, taille à laquelle un badge fait quelques pixels que personne ne lit, et c'est la
    répétition d'une marque unique qui fait lire les trois comme un seul produit. La convention de badge de
    la fondation répond à une question que son dépôt se pose et que celui-ci ne se pose pas — là-bas tous
    les packages sont des catalogues et le badge est le seul discriminant ; ici le nom du package le porte.
    L'icône est de Magnific, via Flaticon, créditée dans le README de chaque package.
* Créer la politique de trusted publishing nuget.org pour le nouvel identifiant de package avant sa
  première release.

## Références

* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) — la décision que
  celle-ci complète, et les sept suppressions qu'elle n'a pas pu convertir.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) — l'exigence d'autonomie, et pourquoi
  une dépendance déclarée ici n'y touche pas.
* [ADR-0047](0047-declare-the-adapters-library-dependency-independently.fr.md) — pourquoi un package
  qui versionne sur autre chose a son propre train.
* `JustDummies.DiagnosticCatalog/DiagnosticCatalogOptIn.props` — le fichier sans lequel le catalogue ne
  vérifie personne, et ne le dit pas.
