# ADR-0047 | Déclarer la dépendance de l'adaptateur à la bibliothèque indépendamment de la version packagée

🌍 🇬🇧 [English](0047-declare-the-adapters-library-dependency-independently.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Contexte

`JustDummies.Xunit` est publié sur son propre train de release (`xunit-v*`), séparément de
`JustDummies` (`lib-v*`). L'objet affiché de ce découpage est qu'un changement du binding xUnit ne doit
pas forcer une version de la bibliothèque, ni l'inverse
([ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md), `tools/trains.sh`).

L'adaptateur prend une `ProjectReference` sur la bibliothèque, ce qui lui permet d'être développé,
testé et analysé contre les sources voisines. Quand `dotnet pack` transforme cette référence en
dépendance NuGet, il déclare la version avec laquelle le projet référencé a été **construit**. Les
scripts de release passent la version du tag en propriété MSBuild globale, et une propriété globale
atteint tous les projets du graphe — donc packager l'adaptateur en `0.2.0` construisait aussi la
bibliothèque en `0.2.0`, et l'adaptateur déclarait une dépendance sur `JustDummies 0.2.0`.

Cette version n'existe pas nécessairement. Un garde-fou dans `tools/packaging/pack.sh` refusait donc de
packager un adaptateur dont la dépendance déclarée ne correspondait à aucun tag `lib-v*`, parce que le
publier donnerait aux consommateurs une dépendance non résoluble (`NU1102`) sur un artefact qu'on ne
peut jamais amender.

Le garde-fou avait raison, la situation qu'il produisait non : un correctif propre à l'adaptateur ne
pouvait pas sortir en `xunit-v0.1.1` tant qu'une `lib-v0.1.1` n'avait pas été publiée, ce qui revenait
à sortir une release de bibliothèque sans contenu uniquement pour libérer un numéro de version. Les
trains étaient indépendants de nom et verrouillés de fait.

## Décision

La version de `JustDummies` que `JustDummies.Xunit` déclare en dépendance est choisie au moment du
packaging comme la plus récente version de la bibliothèque que ce dépôt a publiée, indépendamment de
la version à laquelle l'adaptateur est packagé.

## Justification

**Cela restaure la propriété pour laquelle le découpage existait.** Des trains indépendants qui doivent
bouger ensemble ne sont pas indépendants. La dépendance déclarée étant choisie plutôt qu'héritée, un
correctif propre à l'adaptateur sort seul, et une release de bibliothèque ne traîne pas l'adaptateur
derrière elle.

**Cela retire un couplage sans retirer un garde-fou.** La lecture évidente de « supprimer le verrou »
serait d'effacer la vérification dans `pack.sh`, ce qui ne supprimerait pas le verrou du tout — cela
laisserait un adaptateur partir en réclamant une version de bibliothèque que personne n'a publiée,
c'est-à-dire précisément la panne que la vérification prévient, rendue silencieuse. Le verrou venait de
*la façon dont la version était dérivée* : c'est donc cela qui change. Le garde-fou reste et vérifie
désormais une décision au lieu d'attraper un accident ; il se déclenche toujours si la décision est
mauvaise.

**Les tags publiés sont la source honnête.** Les tags `lib-v*` sont exactement les versions de la
bibliothèque que ce dépôt a publiées. En lire la plus récente ne demande aucun appel réseau, fonctionne
hors ligne et en répétition, et ne peut pas dériver comme le ferait une constante tenue à la main.

**Cela ne coûte rien au développement.** Le mécanisme réécrit ce que le package *déclare* ; il ne
touche ni à ce qui est construit ni à ce qui est référencé. Les builds locaux, les tests, les
analyseurs et l'IDE continuent de compiler l'adaptateur contre les sources voisines exactement comme
avant, et un `dotnet pack` nu, sans propriété, se comporte comme il l'a toujours fait.

**Dépendre de la plus récente bibliothèque publiée est le bon défaut, pas seulement le plus commode.**
L'adaptateur est un binding mince sur une surface qui ne fait que croître sous la 1.0 ; la dernière
release est celle contre laquelle ses sources ont été construites et testées. Un plancher délibérément
plus ancien serait une décision distincte, et le mécanisme lui laisse la place — la propriété peut être
posée à la main.

## Alternatives considérées

### Supprimer le garde-fou de `pack.sh`

La lecture littérale de « il ne doit pas y avoir de verrouillage ». Rejeté : le garde-fou n'est pas le
verrou. Le retirer laisse l'adaptateur publier une dépendance sur une version jamais sortie — `NU1102`
pour le consommateur, sur un artefact immuable — ce qui est pire que le couplage qu'on soulagerait, et
silencieux.

### Remplacer la `ProjectReference` par une `PackageReference` sur la bibliothèque publiée

Cela découplerait les versions par construction, et c'est ce que fait un vrai consommateur. Rejeté :
l'adaptateur se construirait alors contre le dernier package publié plutôt que contre les sources à
côté de lui, si bien qu'un changement traversant les deux exigerait une publication avant de pouvoir
être compilé, et que les analyseurs comme les tests cesseraient d'exercer le code réellement modifié.

### Garder les trains verrouillés et versionner l'adaptateur avec la bibliothèque

Honnête, et plus simple que n'importe quel mécanisme. Rejeté parce que cela jette la raison du
découpage : `JustDummies.Xunit` existe pour porter la seule dépendance que la bibliothèque ne doit pas
prendre ([ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md)), et sa cadence de release n'a
aucune raison de suivre celle de la bibliothèque.

## Conséquences

### Positives

* Un correctif propre à l'adaptateur sort seul, sans release de bibliothèque sans contenu pour
  débloquer un numéro.
* La dépendance déclarée pointe désormais vers une version qui existe de façon prouvée, par
  construction plutôt que par chance.
* La répétition à blanc exerce le train de l'adaptateur dès la première pull request, puisque la
  dépendance déclarée ne dépend plus de la version jetable de la répétition.

### Négatives

* Le plancher de dépendance de l'adaptateur bouge tout seul dès qu'une version de bibliothèque sort,
  sans commit sur l'adaptateur pour le dire. C'est consigné dans l'artefact packagé et dans le log du
  pack, pas dans les sources.
* Un mécanisme MSBuild de plus à comprendre. Il est documenté là où il s'applique, et c'est le point
  d'extension de NuGet lui-même, pas une invention locale.

### Risques

* Une future version de NuGet pourrait changer le point d'ancrage utilisé. Atténuation : le garde-fou
  de `pack.sh` lit le `.nuspec` produit, donc un mécanisme qui cesserait de fonctionner ferait échouer
  le packaging au lieu de publier une mauvaise dépendance.
* Dépendre toujours de la bibliothèque la plus récente sera faux le jour où l'adaptateur devra
  supporter un plancher plus ancien. Ce sera une supersession de cet enregistrement, et la propriété
  sur laquelle il repose est déjà l'endroit où l'exprimer.

## Actions de suivi

* Aucune. `tools/trains.sh` et `JustDummies.Xunit/CHANGELOG.md` ne décrivent plus le couplage comme un
  coût assumé.

## Références

* [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.fr.md) — pourquoi l'adaptateur
  est un package séparé.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) — pourquoi la bibliothèque ne peut pas
  porter elle-même la dépendance xUnit.
* `tools/trains.sh` — les trains de release que cet enregistrement rend réellement indépendants.
