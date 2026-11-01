# ADR-0127 | Tirer la valeur vide sauf si une garde la refuse

🌍 🇬🇧 [English](0127-draw-the-empty-value-unless-a-guard-refuses-it.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

> Les références de section (§N) renvoient à la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

La table de base du §5.2 écrivait `Any.String().NonEmpty()` pour tout paramètre `string` et `Any.Guid().NonEmpty()`
pour tout `Guid`, quoi que dise le constructeur. La spécification le justifiait par une mesure : une chaîne sans
contrainte revenait vide environ une fois sur dix-sept quand le §17 l'a mesuré, une fois sur mille sous l'étendue
plus large fixée ensuite par l'ADR-0076, et une chaîne métier était tenue pour massivement requise non vide. Le
récapitulatif étiquetait la ligne `guard`, le mot qu'il donne à une contrainte lue dans une garde.

Depuis, une garde que le moteur ne sait pas lire marque son paramètre et bloque la compilation
([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)), et la garde déléguée à un helper
qui retourne la valeur, le principal chemin silencieux, est marquée aussi (#407). Un constructeur qui refuse la
valeur vide est donc lu — `IsNullOrEmpty`, une longueur nulle, `IsNullOrWhiteSpace` comme `NotBlank()`, leurs throw
helpers et les deux bibliothèques de gardes — ou marqué `to verify`.

Le second essai d'expérience développeur a rencontré le défaut dans le contexte 35, les variants d'ADN
([CLI-16](../../../../tools/ux-trial/REPORT-2.fr.md#cli-16)). Un allèle est la séquence de bases que porte un côté
d'un variant, et l'allèle vide est valide et courant : le côté de référence d'une insertion, le côté alternatif
d'une délétion. Le constructeur le disait dans sa documentation et ne refusait rien de tel. Le scaffold écrivait
pourtant `NonEmpty()`, une insertion ou une délétion n'était jamais tirée, et l'un des trois utilisateurs a cru que
le constructeur refusait l'allèle vide.

Le premier fondamental de la bibliothèque, dans `CLAUDE.md` : un dummy est une valeur dont le test ne se soucie
pas, et le tirage existe pour mettre cette indifférence à l'épreuve. Une valeur que le constructeur accepte est un
dummy valide ; un test qui échoue dessus dépendait du paramètre ou a trouvé un défaut, et la graine le rejoue dans
les deux cas. L'[ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) a appliqué ce
raisonnement aux valeurs à la limite d'un type et à `null`.

## Décision

Un paramètre `string` ou `Guid` n'est tiré `NonEmpty()` que là où une garde de son constructeur refuse la valeur
vide ; la table de base n'ajoute aucun refus de son cru.

## Justification

**Le défaut devinait une règle que le constructeur n'écrivait pas, et devinait mal là où ça comptait.** Sa
prémisse — qu'une chaîne métier est massivement requise non vide — était un pari statistique sur les
constructeurs en général. L'essai a produit celui sur lequel il perd : un type dont la valeur vide n'est pas un cas
limite mais la moitié de son sens. Une devinette juste la plupart du temps reste une règle que le développeur n'a
jamais déclarée, écrite dans un fichier qui prétend tirer ce que le constructeur accepte.

**La devinette a cessé de se justifier quand le lecteur de gardes a appris à dire ce qu'il ne savait pas lire.** Le
défaut protégeait d'un échec silencieux : un constructeur refusant la chaîne vide, non lu, la tirant et levant. Cet
échec n'arrive plus en silence. Le refus est lu dans la chaîne quand il est écrit dans une forme que le moteur
connaît, et le paramètre est marqué et la compilation bloquée quand il ne l'est pas (ADR-0083). Tout ce dont le
défaut protégeait est désormais honoré ou déclaré.

**Un dummy que le constructeur accepte est un dummy.** Tirer l'allèle vide n'est pas un risque pour la suite ; c'est
ce pour quoi on tire. Du code plus loin qui casse dessus avait une dépendance que le littéral aurait cachée, et la
graine rejoue le tirage. Présenter le tirage vide comme de l'instabilité est la mauvaise lecture que le second
fondamental interdit.

**L'étiquette `guard` doit vouloir dire ce qu'elle dit.** Une ligne du récapitulatif étiquetée `guard` pour une
contrainte qu'aucune garde n'a produite disait au développeur que le domaine affirmait quelque chose qu'il
n'affirmait pas. Dès que la ligne ne porte que ce que les gardes ont produit, l'étiquette redevient honnête, et un
simple `Any.String()` se lit comme le tirage neutre qu'il est — `JD030` nommant le remède de longueur au site
d'appel, comme il le fait déjà.

**Une règle pour une idée, encore.** L'ADR-0117 a décidé qu'une valeur que le type nomme — une limite, `null` — se
tire plutôt que de s'éviter en silence. La chaîne vide et `Guid.Empty` sont des valeurs du même genre : déclarées,
légales, et exactement là où une dépendance cachée se montre. Garder un défaut qui les évitait aurait laissé la
table de base plaider contre la décision que les tirages venaient d'adopter.

## Alternatives envisagées

### Garder le défaut

Envisagé parce qu'une mesure le soutenait et que la plupart des constructeurs refusent bel et bien la chaîne vide.

Rejeté : la mesure a été prise avant l'ADR-0083, quand un refus non lu était un échec silencieux. Avec le marquage
en place, le défaut ne protège plus rien qui ne soit déjà honoré ou déclaré, et il coûte le seul domaine où la
valeur vide est légale, sous une étiquette qui cache d'où vient la règle.

### Garder le défaut, et marquer le paramètre `to verify`

Envisagé comme un moyen de garder la protection tout en disant qu'il s'agissait d'une devinette.

Rejeté : le marquage est pour une garde que le moteur a vue et n'a pas su lire (ADR-0083), jamais pour une opinion
que le moteur s'est faite tout seul. Bloquer la compilation de chaque paramètre `string` pour demander au
développeur de confirmer une règle inventée par le moteur ferait du marquage un bruit sur le chemin ordinaire.

### Faire du défaut une option de projet

Envisagé parce que `any.json` porte déjà des défauts de projet
([ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md)).

Rejeté : un interrupteur sur une devinette reste une devinette, appliquée d'un coup à toutes les chaînes du projet ;
l'endroit où dire « cette chaîne n'est jamais vide » est le constructeur, que le moteur lit, ou la base valide du
paramètre, qui appartient au développeur.

## Conséquences

### Positives

* Une chaîne vide ou `Guid.Empty` est tirée partout où le constructeur l'accepte, si bien qu'une dépendance à un
  contenu que le test n'a jamais déclaré se montre, et la graine la rejoue.
* L'étiquette `guard` du récapitulatif ne couvre que ce que les gardes ont produit, et la table de base ne porte
  plus une opinion que le lecteur de gardes devait faire céder sous combinaison.

### Négatives

* Un paramètre `string` sans aucune garde tire vide environ une fois sur mille, si bien qu'une suite qui comptait
  en silence sur un contenu qu'elle n'avait jamais demandé verra un échec qu'elle ne voyait pas. Chacun se rejoue
  depuis sa graine et est soit un test qui dépend d'un paramètre, soit un défaut — jamais un coût du tirage.
* Un fichier scaffoldé avant cette décision garde son `NonEmpty()` jusqu'à être scaffoldé de nouveau (§1).

### Risques

* Un développeur qui veut l'ancien comportement sur un paramètre ajoute `NonEmpty()` à sa base valide ; un qui le
  veut sur toutes les chaînes d'un type n'a pas d'interrupteur et l'écrit une fois par paramètre. C'est voulu.
* La ligne `Uri` écrit toujours `.Web()`, une opinion du même genre sur le schéma qu'un domaine accepte. Elle est
  laissée en place : aucun retour n'a rencontré un constructeur acceptant une URI non web, et sous la symétrie de
  l'[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.fr.md) un retrait demande un retour aussi.

## Actions de suivi

* Le changelog `cli` et la note de version disent que les fichiers déjà scaffoldés gardent leur `NonEmpty()`.

## Références

* Issue [#409](https://github.com/Reefact/just-dummies/issues/409).
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — alimenter les générateurs depuis les gardes du constructeur.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — bloquer la compilation sur une garde dont le moteur ne peut pas répondre.
* [ADR-0117](0117-draw-a-limit-value-the-type-permits-one-draw-in-ten.fr.md) — tirer une valeur limite que le type permet une fois sur dix.
* [Spécification de `any`](../specifications/justdummies-tool.fr.md), §5.2 — la table de base.
