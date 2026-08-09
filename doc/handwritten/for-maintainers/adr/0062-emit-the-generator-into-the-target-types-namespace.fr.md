# ADR-0062 | Émettre le generator dans le namespace du type cible

🌍 🇬🇧 [English](0062-emit-the-generator-into-the-target-types-namespace.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

Le fichier scaffoldé est écrit dans le projet de test du développeur, mais le type qu'il génère vit
dans le projet de production.

Un test qui utilise `Order` importe déjà le namespace d'`Order`.

C# résout un nom de type simple dans le **namespace englobant avant toute directive `using`**, donc
un type déclaré dans un namespace l'emporte sur un type importé de même nom et de même arité.

La bibliothèque déclare 32 noms de types publics `Any*` non génériques (§14.2) ; un generator
scaffoldé dont le nom correspond à l'un d'eux, dans un namespace où la bibliothèque est importée, le
masque.

Le tool offre `--namespace` comme surcharge par invocation (§3), et le motif de nommage de la v1.1
(§16) change le nom du type émis mais pas son namespace.

Le moteur détient une `Compilation` et aucune connaissance MSBuild : il ignore le namespace racine du
projet et sa convention dossier-vers-namespace ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md)).

## Décision

Le generator émis est déclaré dans le namespace du type qu'il génère, sauf indication contraire de
`--namespace`.

## Justification

C'est le seul choix qui ne coûte rien au site d'appel. Un test important déjà le namespace métier
écrit `new AnyOrder()` et s'arrête là ; tout autre namespace ajoute un import à chaque fichier de
test qui touche au generator. C'est une friction payée à chaque usage, et la règle de conception 2 la
tarife cher — un outil trop pénible à chaque appel ne vaut pas d'être adopté.

C'est aussi le seul choix que le moteur peut faire avec ce qu'il détient. Le namespace qu'un IDE
inférerait — celui qu'implique le dossier de sortie — exige le namespace racine du projet et sa
convention de dossiers, c'est-à-dire exactement la connaissance MSBuild que [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) tient hors du moteur.

Le coût est réel et assumé les yeux ouverts : **cette décision, et elle seule, crée le risque de
masquage du §7.** Un generator dans un namespace dédié ne pourrait jamais masquer un type de la
bibliothèque, parce que le `using` du développeur concourrait alors à armes égales au lieu de perdre
d'office contre une déclaration englobante. Le risque est borné — 32 noms, un contrôle conscient de
l'arité, un avertissement nommant les deux types — et rare. Échanger une collision rare et signalée
contre une friction à chaque usage est le bon sens de l'échange.

## Alternatives considérées

##### Un namespace dédié aux helpers générés

Considérée parce qu'elle supprime entièrement le risque de masquage et garde les helpers de test
visiblement à part du code métier, ce que certains codebases exigent au titre du découpage en
couches.

Écartée parce qu'elle facture un import à chaque fichier de test, définitivement, pour éviter un
risque qui touche une poignée de noms de types et s'annonce quand il survient. `--namespace` donne
cette disposition à qui la veut, par invocation, sans l'imposer à tout le monde.

##### Le namespace impliqué par le dossier de sortie

Considérée parce que c'est ce que fait un IDE quand on ajoute un fichier, donc ce à quoi un
développeur s'attend.

Écartée parce que le dériver exige le namespace racine du projet et la convention
dossier-vers-namespace. Le moteur ne les porte pas ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md)), donc la CLI devrait les découvrir et les
transmettre, élargissant le contrat du §10.3 pour aboutir à un résultat moins bon que le namespace
propre du type cible.

## Conséquences

**Positives.** Aucune friction au site d'appel. Le moteur n'a besoin d'aucune connaissance du projet.
La déclaration de namespace émise est copiée sur le fichier du type cible, donc le fichier scaffoldé
ressemble à ses voisins dans la forme comme dans le nom (§4.4).

**Négatives.** Un helper de test est déclaré dans un namespace de production, ce que certains
codebases jugeront discutable au nom du découpage ; `--namespace` est la réponse, et il faut le
donner à chaque invocation. Et cette décision est la cause unique du risque du §7.

**Risques.** Un développeur scaffoldant un type portant l'un des 32 noms non génériques de la
bibliothèque obtient un masquage silencieux s'il ignore l'avertissement. Atténué par l'avertissement
qui nomme les deux types, et par le motif de nommage de la v1.1 qui offre un renommage sans exiger
de changer de namespace.

## Actions de suivi

* Le contrôle de masquage doit être conscient de l'arité (§7). Avertir sur les huit noms génériques,
  qui ne peuvent pas entrer en collision, entraînerait les développeurs à ignorer le seul
  avertissement qui compte.

## Références

* §3, §4.4, §7, §14.2, §16 de cette spécification ; [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md) de cette section.

---
