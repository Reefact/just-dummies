# ADR-0130 | Livrer des correctifs de code à côté des analyzers, pour l'unique réécriture qui garde l'intention

🌍 🇬🇧 [English](0130-ship-code-fixes-beside-the-analyzers.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-02
**Accepted:** 2026-11-02
**Decision Makers:** Reefact

## Contexte

Aucune règle JustDummies ne livrait de correctif de code : il n'y avait aucun `CodeFixProvider` dans le dépôt, et
le suivi que l'ADR-0077 désignait (issue #95) n'a jamais été construit. Les décisions prises lors du premier essai
UX développeur en demandent (issue #355) : plusieurs règles signalent une erreur dont l'écriture correcte est
entièrement déterminée par ce que la règle a trouvé — une contrainte qui ne restreint rien, une paire de bornes que
l'auteur a transposée, deux bornes inclusives que le générateur nomme comme une plage en un seul appel.

Un correctif de code s'exécute dans un IDE ou dans `dotnet format`, jamais dans le compilateur. Il repose sur la
couche workspace de Roslyn, que les analyzers ne référencent pas, et il est chargé depuis le même dossier
`analyzers/dotnet/cs` que les règles (ADR-0023). Les analyzers sont compilés contre un plancher Roslyn épinglé une
fois, pour que l'hôte supporté le plus ancien les charge (ADR-0001) ; le job `floor` le prouve sur le paquet
empaqueté.

Un correctif qui devine est pire que pas de correctif : un développeur qui l'accepte par lot voit son code changé en
quelque chose qu'il n'a pas écrit. D'autres règles de cette bibliothèque signalent déjà des erreurs à plusieurs
issues — une contradiction dont chaque côté peut être celui visé, une chaîne réduite à une valeur où un littéral
comme une plage plus large peuvent être visés — et les nomment au lieu de choisir.

## Décision

Les correctifs de code sont livrés dans un assemblage frère, `JustDummies.Analyzers.CodeFixes`, empaqueté à côté
des analyzers sous `analyzers/dotnet/cs`, construit sur la couche workspace au même plancher Roslyn que les règles,
et une règle ne reçoit un correctif que là où exactement une réécriture garde l'intention du développeur.

* Une règle qui admet un correctif lui transmet ce qu'elle a établi par les propriétés du diagnostic — quelle paire
  d'arguments est transposée, quels deux appels font la plage et comment elle se nomme — pour que le correctif
  réécrive ce que la règle a trouvé et ne relise rien.
* Un correctif se teste comme une règle : dans `JustDummies.Analyzers.UnitTests`, par la source qu'il produit, en
  entier.
* Le premier lot fixe le modèle, un de chaque forme : JD024 retire la contrainte qui ne restreint rien, JD014
  échange une paire transposée, JD031 écrit la paire sous sa forme de plage. Les autres règles que l'issue liste
  suivent le même critère ; les règles qui signalent une contradiction, une chaîne réduite à une valeur, ou une
  erreur de cycle de vie n'en reçoivent aucun.

## Justification

**Un assemblage frère, parce que le compilateur doit rester intouché.** Les règles ne portent aucune référence à la
couche workspace, ce qui garde l'analyzer petit et son contrat de chargement celui du compilateur. Un correctif a
besoin de cette couche. Empaqueter les correctifs dans leur propre assemblage à côté des règles garde les deux : le
compilateur lit les règles, un IDE lit les deux, et aucun des assemblages ne porte ce dont l'autre a besoin.

**Le même plancher, pour la même raison.** Un hôte assez ancien pour charger les règles doit charger leurs
correctifs aussi, sans quoi un développeur sur cet hôte voit une règle sans ampoule ni explication. L'épingle qui
tient les règles tient les correctifs.

**La règle dit au correctif ce qu'elle a trouvé.** Un correctif qui redériverait la paire ou la plage depuis la
syntaxe serait une seconde lecture de la même chaîne, qui peut contredire la première. Le diagnostic porte déjà la
lecture de la règle ; la transmettre par des propriétés rend le correctif exactement aussi juste que la règle.

**Seulement la réécriture unique.** Les règles de la bibliothèque nomment des issues plutôt que de choisir entre
elles ; un correctif est là où choisir est inévitable, il n'existe donc que là où il n'y a rien à choisir. Les trois
du premier lot sont équivalents par construction : une contrainte qui ne restreint rien, retirée, laisse le même
domaine ; une paire transposée, échangée, est la paire que la garde accepte ; et la méthode de plage est les deux
bornes qu'elle remplace.

**Testé par la source produite.** Un IDE applique un correctif à un document et montre le résultat ; le test fait de
même et compare le texte entier, trivia compris, pour qu'un correctif qui abîme la ligne voisine de celle qu'il
visait échoue.

## Alternatives envisagées

### Les correctifs dans l'assemblage de l'analyzer

Envisagé parce qu'une seule DLL est plus simple à empaqueter et à prouver. Rejeté : l'analyzer référencerait alors
la couche workspace, que le compilateur ne charge pas, et son contrat de chargement ne serait plus celui du seul
compilateur.

### Un correctif pour chaque règle, choisissant la réécriture la plus probable

Envisagé parce qu'une ampoule sur chaque diagnostic se lit comme un produit complet. Rejeté : un correctif qui devine
quel côté d'une contradiction garder réécrit un test en un autre que le développeur ne visait pas, et une
application par lot le multiplie.

### Un correctif qui relit la chaîne

Envisagé parce que la syntaxe est sous la main. Rejeté : deux lectures d'une même chaîne peuvent diverger, et celle
de la règle est celle sur laquelle le diagnostic a été signalé.

## Conséquences

### Positives

* Une erreur signalée à une seule écriture correcte est corrigée là où elle est signalée, une par une ou par lot.
* Un nouveau correctif suit un modèle écrit : les propriétés que la règle attache, le harnais qui l'applique.

### Négatives

* Le paquet porte un second assemblage sous `analyzers/dotnet/cs`, et le script d'empaquetage vérifie les deux.

### Risques

* Un hôte qui charge l'assemblage des correctifs sans la couche workspace journalise un échec de chargement là où
  les règles s'exécutent toujours ; le compilateur n'instancie jamais un correctif, une compilation n'est donc pas
  affectée.

## Actions de suivi

* Les règles que l'issue liste avec un seul correctif préservant l'intention — JD025, JD032, JD035, JD013, JD005,
  JD011, JD002, JD001, JD027 — et les deux à choix proposés côte à côte, JD010 et JD004, suivent le modèle fixé ici.

## Références

* [ADR-0001](0001-lock-the-analyzer-roslyn-floor.fr.md) — le plancher Roslyn.
* [ADR-0023](0023-ship-justdummies-analyzers.fr.md) — les règles voyagent dans le paquet.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — le suivi qui a nommé un correctif le premier.
* Issues #355 et #95.
