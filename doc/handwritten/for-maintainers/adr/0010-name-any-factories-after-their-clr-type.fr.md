# ADR-0010 | Nommer les fabriques scalaires de Any d'après leur type CLR

🌍 🇬🇧 [English](0010-name-any-factories-after-their-clr-type.md) · 🇫🇷 Français (ce fichier)

**Statut :** Accepté
**Proposé :** 2026-07-21
**Accepté :** 2026-07-21
**Décideurs :** Reefact
**Enregistré à l'origine dans `Reefact/first-class-errors` sous le numéro ADR-0040.**

## Contexte

* `JustDummies` expose un point d'entrée statique `Dummy` dont les fabriques sans paramètre démarrent
  chacune un générateur pour un type simple .NET — `Dummy.Int32()`, `Dummy.SByte()`,
  `Dummy.Single()`, `Dummy.UInt64()`, `Dummy.String()`, `Dummy.Guid()`, `Dummy.DateTime()`, … — et
  retournent chacune un type builder nommé `Dummy{Nom}` (`Dummy.Int32()` retourne `DummyInt32`).
  `DummyContext` reflète chacune de ces fabriques scalaires pour la surface à contexte graine, si
  bien que chaque fabrique existe en deux endroits qui doivent concorder.
* Sur toute cette surface scalaire, le nom de la fabrique et le nom du builder sont le **nom de
  type CLR** — la valeur que retourne `System.Type.Name` — et non le mot-clé C# : `Int32` et non
  `Int`, `Single` et non `Float`, `Int64` et non `Long`, `Byte`/`SByte`, `Decimal`, `Char`. Les
  formes mots-clés C# ne peuvent pas toutes servir d'identifiants (`Dummy.int()` est une erreur de
  syntaxe), et les noms CLR se lisent uniformément avec les noms de types builder.
* Cela reflète les familles de méthodes par primitive de la bibliothèque de base .NET, qui
  nomment chaque méthode d'après le type CLR : `Convert.ToInt32` / `ToSingle` / `ToBoolean`,
  `BitConverter.ToInt32` / `ToBoolean` — jamais `ToBool`, `ToFloat` ou `ToInt`.
* Une fabrique déviait : `Dummy.Bool()`, retournant `AnyBool`, produisait un `System.Boolean`,
  dont le nom CLR est `Boolean`. C'était le seul membre de la surface scalaire non nommé d'après
  son type CLR. L'audit d'architecture et de conception de JustDummies du 2026-07-20 l'a mis en
  évidence (§8.2, §8.4) et a recommandé que le choix soit tranché délibérément et consigné avant
  la publication.
* `JustDummies` est en pré-publication : aucun tag `dum-v*`, aucun consommateur NuGet externe, une
  section *Unreleased* de changelog vide. Renommer une fabrique publique et un type builder
  public est un changement cassant dès que des consommateurs en dépendent, et ne coûte rien avant
  la première publication.
* Le dépôt consigne les décisions de nommage de cette classe sous forme d'ADR — [ADR-0005 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0005-reserve-the-plain-factory-name-for-the-outcome-returning-variant.fr.md) réserve
  le nom de fabrique nu à la variante retournant un `Outcome`, [ADR-0007 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0007-name-the-binder-terminals-new-and-create.fr.md) nomme les terminaux du
  binder `New` et `Create`.

## Décision

Toute fabrique scalaire sans paramètre de `Dummy` et de `DummyContext`, ainsi que le type builder
qu'elle retourne, est nommée d'après le nom CLR du type qu'elle produit, sans exception — en
renommant `Dummy.Bool()` / `DummyContext.Bool()` / `AnyBool` en `Dummy.Boolean()` /
`DummyContext.Boolean()` / `DummyBoolean`.

## Justification

* La convention était déjà « noms de types CLR » pour toutes les fabriques scalaires sauf une.
  Garder `Bool` ne laisse énoncer la règle qu'avec une réserve — « noms CLR, sauf celui qu'on a
  raccourci » — et ne la rend vérifiable qu'avec une entrée de liste d'exception portant cette
  réserve. Nommer `Boolean` rend la règle sans exception : une phrase la décrit et un seul garde
  la fait respecter.
* L'argument ergonomique pour le raccourci `Bool` est exactement celui déjà écarté pour `Int`
  (`int`), `Long` (`long`), `Short` (`short`) et `Float` (`float`) — toutes des graphies qu'un
  développeur écrit bien plus souvent que `Boolean`. Honorer la forme courte pour le seul `bool`
  privilégierait un mot-clé sans principe le distinguant des largeurs et réels que la surface
  écrit déjà en toutes lettres.
* Le choix aligne la surface sur les familles par primitive `Convert` / `BitConverter` de la BCL
  citées au Contexte, l'analogue existant le plus proche de « une méthode par type simple, nommée
  d'après le type » ; un consommateur qui cherche `Convert.ToBoolean` trouve `Dummy.Boolean()` là où
  il l'attend.
* Faire concorder nom de fabrique, nom de builder et nom CLR garde un seul modèle mental —
  `Dummy.X()` → `AnyX` → `System.X` — qu'un unique garde par réflexion peut vérifier sur toute la
  surface, si bien qu'un type ajouté plus tard ne peut réintroduire la déviation en silence.
* Trancher en pré-publication ne coûte rien et, selon l'audit, est le moment le moins cher pour
  décider ; différer au-delà de la 1.0 transforme un renommage gratuit en changement cassant dans
  un sens comme dans l'autre.

## Alternatives envisagées

### Garder `Bool()` / `AnyBool` et consigner la forme courte comme exception délibérée

Envisagée parce que `bool` est la graphie quasi universelle en C# moderne tandis que `Boolean`
est rarement écrit à la main, si bien que `Dummy.Bool()` est marginalement plus familier au point
d'appel, et qu'une justification d'une ligne dans le README pourrait faire lire la déviation comme
choisie plutôt qu'accidentelle.

Rejetée parce que le même argument de familiarité s'applique mot pour mot à
`Int`/`Long`/`Short`/`Float`, que la surface écarte déjà ; consigner l'exception préserve une
règle qu'il faut alors énoncer avec une réserve et garder avec une entrée de liste d'exception,
échangeant un renommage unique en pré-publication contre une asymétrie permanente dans la surface
dont toute la valeur est l'uniformité.

### Ne renommer qu'un côté — le builder en `DummyBoolean` en gardant `Bool()`, ou l'inverse

Envisagée parce qu'elle toucherait moins de points d'appel.

Rejetée parce qu'elle casse la correspondance nom-de-fabrique-égale-nom-de-builder qui tient
partout ailleurs (`Dummy.Int32()` → `DummyInt32`), remplaçant une déviation par une autre et perdant
le bénéfice du modèle mental unique qui motive le changement.

### Offrir à la fois `Bool()` et `Boolean()`, l'un déléguant à l'autre

Envisagée parce qu'elle garderait le nom familier tout en ajoutant le nom conventionnel.

Rejetée parce que deux noms pour un même générateur doublent la surface découvrable, invitent à
des points d'appel incohérents, et laissent tout de même un membre public hors convention à
documenter et à garder ; la fenêtre de pré-publication rend un nom unique et propre disponible
sans coût.

## Conséquences

### Positives

* La surface de fabriques scalaires suit une règle unique sans exception (`Dummy.X()` → `AnyX` →
  CLR `X`), énonçable en une phrase et exécutable par un seul garde par réflexion.
* La surface correspond au nommage par primitive `Convert` / `BitConverter` de la BCL, réduisant
  la surprise pour les consommateurs.
* Le nommage est tranché avant la première publication, si bien qu'aucun consommateur ne migre
  jamais.

### Négatives

* `Dummy.Boolean()` est plus verbeux que la graphie quasi universelle du mot-clé `bool` ; un
  consommateur attendant `Bool()` doit se tourner vers `Boolean()`.
* Le renommage touche d'un coup le type builder, les deux points d'entrée, la vérification de
  l'artefact empaqueté et les tests — un coût mécanique, en pré-publication.

### Risques

* Sans mise en application, un type scalaire futur pourrait réintroduire un nom court de mot-clé
  (un second `Bool`) ; atténué par le garde de parité de nommage ajouté avec cette décision, qui
  échoue lorsqu'une fabrique, son builder ou son miroir `DummyContext` s'écarte du nom CLR.

## Actions de suivi

* Renommer `Dummy.Bool()` / `DummyContext.Bool()` / `AnyBool` en `Boolean` / `DummyBoolean`, et mettre
  à jour les tests, la sonde d'artefact empaqueté `justdummies-check` et le README du package *(fait
  dans ce changement)*.
* Ajouter le garde de parité de nommage à `JustDummies.UnitTests` *(fait dans ce changement)*.

## Références

* [ADR-0005 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0005-reserve-the-plain-factory-name-for-the-outcome-returning-variant.fr.md) — réserver le nom de fabrique nu à la variante retournant un Outcome ; précédent de
  décision de nommage.
* [ADR-0007 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0007-name-the-binder-terminals-new-and-create.fr.md) — nommer les terminaux du binder New et Create ; précédent de décision de nommage.
* ADR-0006 — matérialiser les dummies uniquement via Generate() ; partage le cadrage pré-1.0 du
  « moment le moins cher pour décider ».
* Audit d'architecture et de conception de JustDummies du 2026-07-20, §8.2 et §8.4 — a mis en
  évidence la déviation.
* Issue #222.
