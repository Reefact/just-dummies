# ADR-0087 | Vérifier un décompte documenté contre sa source, non contre sa traduction

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0087-check-a-documented-count-against-its-source-not-its-translation.md)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

## Contexte

La documentation énonce en prose combien de règles d'analyzer le paquet embarque, sur des pages qu'un
lecteur atteint avant d'installer quoi que ce soit. Ce nombre est un fait que le code possède : c'est
le décompte des identifiants `JDxxx` distincts que l'assembly publié lève.

**Mesuré sur l'arbre, sept énoncés répartis sur cinq pages nommaient un nombre que le paquet ne
portait plus depuis un moment** — 28, 29, 31 ou 32, contre les 33 qu'il embarque :

* le `README` racine, première page que voient le visiteur et la fiche du paquet ;
* `packages/justdummies.en.md`, qui énonçait le décompte trois fois dans un même fichier avec trois
  nombres différents, seul celui du milieu étant juste ;
* `guides/getting-started.en.md`, la page que suit un nouveau venu ;
* l'inventaire des analyzers de la spécification `dum`, dans les deux langues, à qui manquait aussi la
  ligne de la trente-troisième règle.

Deux d'entre eux étaient des désaccords entre une page et son jumeau français, et la paire divergeait
dans les **deux** sens : 31 et 28 en anglais contre 33 et 29 en français. La spécification a dérivé
autrement — ses deux moitiés énonçaient le même couple de nombres faux.

L'[ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md) a établi une suite
de tests sur ce corpus : les échantillons compilent contre les paquets publiés, les analyzers publiés
s'exécutent dessus, chaque page est tenue à la parité structurelle avec son jumeau, et les liens
résolvent. **Aucun de ces contrats ne lit un nombre.** La suite référence déjà l'assembly des
analyzers en bibliothèque simple et réfléchit dessus pour exécuter les règles : le décompte est donc
disponible là où les pages sont lues.

Une comparaison des séquences de chiffres entre une page et son jumeau était le mécanisme que
proposait le rapport de défaut. Mesurée sur ce corpus, elle signale 18 paires pour en révéler 2
vraies. Le bruit a trois sources : le séparateur de milliers français, qui écrit `1 000 000` là où
l'anglais dit « a million » ; un token orthographié en code d'un côté et en texte de l'autre, qui
survit à un nettoyage et pas à l'autre ; et une référence écrite sous deux formes au sein d'une paire.

Le dépôt amont portait un `tools/analyzer-count-check` gardant ce fait. L'extraction ne l'a
délibérément pas porté, consignant que `README.nuget.md` ne faisait pas cette promesse — ce qui était
vrai de ce fichier et faux du `README` racine.

La base de décisions, les notes de version et le journal de migration énoncent tous des décomptes qui
étaient corrects le jour où ils ont été écrits. Les pages sont coupées à cent colonnes, et tout
décompte du corpus fait au moins deux mots.

## Décision

Un décompte que la documentation énonce au sujet du produit publié est tenu par le build à la valeur
lue dans l'assembly publié plutôt qu'à celle qu'énonce sa traduction, sur toute page hormis celles qui
consignent ce qui était vrai lorsqu'elles ont été écrites.

## Justification

**Un jumeau n'est pas une source.** Deux énoncés en prose peuvent diverger sans qu'aucun ne fasse
autorité, et ici les deux moitiés étaient fausses, et fausses différemment — si bien qu'un échec de
parité, même déclenché, aurait nommé un désaccord sans nommer la vérité, laissant un mainteneur aller
chercher le vrai nombre de toute façon. Là où les deux moitiés dérivent de concert, comme l'a fait la
spécification, une comparaison entre elles ne rapporte rien du tout. L'assembly n'a pas ce mode de
défaillance : il est ce dont la phrase parle.

**La source est déjà dans la pièce.** L'ADR-0055 a mis les analyzers publiés entre les mains de cette
suite pour exécuter les règles sur les échantillons ; demander à ces mêmes objets combien
d'identifiants ils lèvent n'ajoute ni dépendance, ni outillage, ni second endroit à tenir en phase.
Les mécanismes alternatifs réclament tous quelque chose de neuf.

**La couverture par défaut est ce que plaide le défaut.** Le décompte a dérivé parce que rien ne
surveillait la prose, et une liste d'inclusion étendrait cette condition à toute page écrite après le
garde-fou. Tenir le corpus entier et nommer les exemptions une à une — comme cette base le fait déjà
pour les pages de règles antériorisées — place une page écrite demain sous le contrat le jour où elle
existe.

**Un relevé réécrit pour s'accorder au code d'aujourd'hui a cessé de relever.** L'ADR-0055 dit que le
produit embarque 28 règles et avait raison en août ; le journal de migration explique un portage fait
quand un autre nombre avait cours. Leur valeur tient à ce qu'ils disent ce qu'on croyait alors : ils
sont donc exemptés par le raisonnement même qui met les pages vivantes dans le périmètre.

Le coût est que le contrat doit reconnaître un décompte écrit en prose, dans deux langues, ce qui
relève de l'inférence là où la comparaison à l'assembly est exacte. Ce coût achète une vérification
qui survit au fait d'être faux dans les deux langues à la fois, c'est-à-dire la défaillance que le
corpus a réellement produite.

## Alternatives envisagées

### Comparer les séquences de chiffres d'une page et de son jumeau

Envisagée parce qu'elle ne demande de connaître le sens d'aucun nombre, prolonge un contrat qui existe
déjà, et attraperait la dérive de n'importe quel fait plutôt que de celui-ci seul.

Rejetée sur la mesure et sur le principe. À 18 paires signalées pour en trouver 2, c'est une
vérification qu'on suspend puis qu'on supprime, et les trois sources de bruit sont toutes de la
traduction légitime — un garde-fou qui qualifie de défaut du français correct apprend au mainteneur à
ne plus le lire. Sur le principe, elle est aveugle exactement là où ce corpus a échoué : les moitiés
de la spécification s'accordaient entre elles et désaccordaient d'avec le code.

### Vérifier qu'une page ne se contredit pas elle-même

Envisagée parce qu'une page énonçait le décompte trois fois avec trois valeurs, ce qui n'a besoin
d'aucune traduction pour être visiblement faux, et parce qu'elle aurait attrapé deux des sept énoncés
par leurs propres mérites.

Rejetée comme subsumée. Comparer chaque énoncé à l'assembly vérifie les trois indépendamment et
attrape en plus le cas que celle-ci manque — une page parfaitement cohérente avec elle-même et fausse
de bout en bout, ce qu'étaient précisément le `README` racine et la spécification.

### Restaurer la vérification shell d'amont

Envisagée parce qu'elle existait, gardait exactement ce fait, et que son absence est consignée comme
délibérée.

Rejetée parce qu'elle gardait un fichier contre une seule formulation, quand la dérive a touché cinq
pages en deux langues ; et parce qu'un script hors solution lit la sortie empaquetée plutôt que
l'assembly, si bien qu'il faudrait lui dire le nombre au lieu de le lui demander.

### Cesser d'énoncer le décompte en prose

Envisagée parce qu'un fait jamais retapé ne peut pas dériver, et que l'index des règles est à un lien
de distance sur toute page qui l'énonce.

Rejetée parce que le nombre est ce qui rend la phrase utile : un lecteur qui décide s'il installe veut
l'ordre de grandeur de ce qu'il embarque, pas une invitation à aller compter. Protéger la
documentation en la rendant moins informative échange le défaut contre une page pire.

## Conséquences

### Positives

* Le décompte ne peut plus dériver inaperçu, dans aucune des deux langues, sur aucune page du
  périmètre.
* Une règle ajoutée sans page de documentation, ou une page laissée derrière par une règle qui cesse
  d'être publiée, fait échouer le build — la plage d'identifiants est tenue comme un ensemble, pas
  seulement comme un total.
* Une page écrite après cette décision est sous le contrat dès le jour où elle existe.
* L'échec nomme le fichier, la ligne, les deux nombres et le texte fautif : la correction ne demande
  aucune investigation.

### Négatives

* Le contrat doit reconnaître les formes sous lesquelles un décompte s'écrit, dans deux langues. Une
  formulation que personne n'a encore employée lui échappe tant que les motifs ne l'ont pas apprise,
  ce qui fait de la détection une inférence là où la comparaison, elle, est exacte.
* Quatre pages sont nommées comme exemptes plutôt que dérivées : un cinquième genre de relevé devrait
  donc être ajouté à la main.

### Risques

* Un décompte peut se cacher des motifs. C'est déjà arrivé une fois : une coupure de ligne a placé le
  nombre en fin de ligne et son nom au début de la suivante, et un balayage ligne à ligne l'a manqué.
  Lire des paragraphes plutôt que des lignes répond à ce cas, non au risque général.
* La liste d'exemptions est un endroit où une page vivante pourrait être garée pour faire taire un
  échec. Elle est courte, et chaque entrée porte sa raison, ce qui est la seule défense d'une liste de
  ce genre.

## Actions de suivi

* Examiner si d'autres faits que le code possède et que la prose répète — le plancher de framework
  supporté, le nombre de paquets publiés — méritent le même traitement.
* Le journal de migration consigne `tools/analyzer-count-check` comme valant d'être réintroduit si le
  README venait à annoncer le décompte. Il l'annonce, et l'invariant est de retour sous une autre
  forme ; le journal reste tel qu'écrit.

## Références

* [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.fr.md) — la suite que ce contrat rejoint.
* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) — où un test de ce genre a sa place.
* [Issue #120](https://github.com/Reefact/just-dummies/issues/120) — le rapport de défaut, et le mécanisme de parité qu'il proposait.
