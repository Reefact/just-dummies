# ADR-0075 | Tirer les caractères dans tout l'ASCII, et ne rétrécir que par une famille nommée

🌍 🇬🇧 [English](0075-draw-characters-from-the-whole-of-ascii.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-18
**Accepted:** 2026-08-18
**Decision Makers:** Reefact

## Contexte

`Any.String()` et `Any.Char()` portent chacun une **famille de caractères** : une contrainte déclarée une
seule fois par générateur, qui nomme les caractères qu'un tirage peut utiliser. Les familles sont `Alpha`,
`Numeric` et `AlphaNumeric`, à côté de deux casses, plus `WithChars` sur la chaîne et `OneOf` sur le
caractère. Non contraints, les deux générateurs tirent parmi les lettres et les chiffres ASCII — 62
caractères — et `CharacterPools` détient cette définition une seule fois pour que les deux ne puissent pas
diverger. La documentation d'`AnyChar` énonce que ses familles reflètent celles de la chaîne, et un garde
par réflexion dans `SurfaceParityTests` tient chaque builder à l'ensemble de contraintes que sa famille
déclare.

Aucune famille nommée n'atteint un caractère qui ne soit ni une lettre ni un chiffre. Atteindre `:` suppose
de fournir les caractères soi-même — `Any.Char().OneOf(':')`, ou `Any.String().WithChars(...)` — ce qui
répond à *« exactement ces caractères »* et non à *« un caractère qui n'est pas alphanumérique »*. C'est le
signalement qui a ouvert la question.

La bibliothèque tire pourtant déjà au-delà des lettres et des chiffres, ailleurs. Le générateur
d'expressions régulières résout toute position qu'un motif laisse **libre** — le point, un raccourci, une
classe niée — dans l'ASCII imprimable (0x20–0x7E), et `RegexAlphabet` en consigne la raison : restreindre
les positions libres garde les dummies générés lisibles au lieu de disperser de l'Unicode arbitraire. Ainsi
`Any.StringMatching(".")` produit `:` alors qu'aucune famille de caractères ne le peut, et deux portes du
même produit répondent différemment à la même question.

Quatre autres faits pèsent sur le choix.

**La valeur d'un dummy est ce qu'il expose.** Une valeur sur laquelle personne n'assertit est tout de même
passée au code testé, et elle certifie tout ce à quoi elle survit. Un générateur qui ne tire jamais que des
lettres et des chiffres ne certifie rien d'un retour chariot, d'un NUL ou d'un caractère d'échappement —
les caractères les plus susceptibles de casser l'analyse, le stockage et la journalisation. Une référence
de commande qui ne doit pas contenir `\r\n` porte cet invariant, et aujourd'hui rien dans un test ne le
déclare, parce que rien ne peut en produire un contre-exemple.

**Changer un tirage non contraint est une version majeure.** L'[ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md)
promet qu'une graine se rejoue d'une version corrective ou mineure à l'autre, garantie par un golden master
qui épingle à la fois les valeurs produites et les tirages consommés. Élargir le défaut déplace toutes les
valeurs que rejoue toute graine committée.

**Au-delà de l'ASCII, c'est la reproductibilité elle-même qui est en jeu.** Les catégories Unicode bougent
avec la version du runtime, donc une famille définie par elles pourrait tirer différemment sur deux
frameworks cibles — la garantie que `tools/justdummies-check` compare octet par octet. Un substitut est une
moitié de caractère : `WithChars` en refuse déjà un pour cette raison.

**Deux analyzers reflètent la correspondance famille-vers-alphabet.** JD015 et JD029 raisonnent chacun sur
ce qu'une famille déclarée admet, et un analyzer ne référence aucun assembly JustDummies et ne peut pas
l'appeler. Une famille qu'ils ne nomment pas n'est pas mal rapportée : elle n'est simplement pas lue.

Enfin, `char.IsPunctuation` de .NET est plus étroit que le bloc imprimable non alphanumérique : il classe
`+`, `<`, `=` et `$` parmi les symboles. POSIX `[:punct:]` désigne tout ce bloc moins l'espace, les 32
caractères.

## Décision

Un `Any.Char()` non contraint, et le remplissage non contraint d'`Any.String()`, tirent dans tout l'ASCII
(0x00–0x7F), et toute famille de caractères — `Printable`, `NonPrintable`, `Whitespaces`, `Alpha`,
`Numeric`, `AlphaNumeric`, `Punctuation`, `Hexadecimal`, et la paire soustractive `WithoutAlpha` /
`WithoutNumeric` — ne fait jamais que rétrécir cet ensemble.

## Justification

**Un défaut qui ne tire que lettres et chiffres ne certifie rien.** L'intérêt d'une valeur arbitraire est
que le code testé n'a pas eu son mot à dire. La restreindre d'avance aux caractères qui ne posent jamais
problème retire précisément la preuve que le tirage existe pour produire : le test passe, et il n'a rien
établi sur les valeurs que le code rencontrera réellement. Élargir le défaut n'est donc pas un confort,
c'est ce qui donne un sens à un tirage non contraint.

**L'invariant appartient au site d'appel, et il peut désormais s'y écrire.** Une référence de commande qui
ne doit pas contenir de saut de ligne porte un invariant réel ; une colonne qui tient au plus 50 caractères
aussi. Sous cette décision, un test l'énonce — et un test qui ne l'énonce pas reçoit une valeur qui ira
voir. C'est le contrat que le reste de la bibliothèque applique déjà : les contraintes expriment ce que le
code environnant exige, et le générateur fournit le reste arbitrairement.

**L'ASCII est là où la borne doit tomber, et la localisation en est la raison.** Le pas au-delà de l'ASCII
n'est pas un cran d'ambition de plus, c'est un autre problème : le vivier dépendrait de la version
d'Unicode du runtime, ce qui met en péril la garantie de graine entre frameworks cibles, et les substituts
font qu'un `char` cesse d'être un caractère. S'arrêter à 128 garde tout tirage explicable, reproductible sur
chaque jambe, et exempt des questions de marques combinantes et de normalisation qu'aucune bibliothèque de
support de test ne devrait trancher. Au-delà, c'est un alphabet fourni par l'appelant — `WithChars`,
`OneOf` — qui est la forme honnête pour le texte qu'un domaine emploie vraiment.

**Faire du plus large ensemble le défaut est ce qui permet à toute contrainte de rétrécir.** La version
précédente de ce record gardait lettres et chiffres comme défaut et ajoutait des familles plus larges
par-dessus ; cela faisait de `Printable()` une contrainte qui *agrandissait* le tirage, ce qu'une
contrainte n'est pas, et forçait `Whitespaces()` et `NonPrintable()` à devenir des exceptions documentées à
la règle. Partir de tout l'ASCII referme le modèle : le défaut est le sommet du treillis, toute famille en
est un sous-ensemble, et il n'y a aucune exception à expliquer. `Printable()` devient une vraie contrainte
au lieu d'un no-op qui nomme le défaut.

**La symétrie entre les deux générateurs est déjà un engagement.** `CharacterPools` existe pour que le
remplissage de la chaîne et le vivier du caractère ne puissent pas se contredire, `AnyChar` documente ses
familles comme reflétant celles de la chaîne, et le garde de parité fait échouer un builder dont l'ensemble
dérive. Chaque famille atterrit sur les deux.

**La liste des membres est bornée par la forme de l'ASCII, pas par le goût.** L'univers se découpe en blocs
— contrôles, espace, chiffres, majuscules, minuscules, ponctuation — et les familles en sont les unions
utiles. `Hexadecimal` est le seul membre qui les traverse ; il mérite sa place parce qu'une norme publiée le
définit (RFC 4648, « Base 16 »), et ce critère est ce qui empêche la porte de devenir une file d'attente :
un alphabet qu'une norme définit peut être nommé, un alphabet qu'un projet invente s'écrit `WithChars`. La
paire soustractive s'accumule, donc `WithoutAlpha().WithoutNumeric()` fournit la troisième combinaison utile
sans troisième membre.

## Alternatives envisagées

### Garder lettres et chiffres comme défaut, et ajouter des familles plus larges par-dessus

La version précédente de ce record, et le plus petit changement : aucune graine existante ne bouge, et
`Punctuation()` répond au signalement d'origine.

Rejetée dès qu'on examine le modèle plutôt que le symptôme. Elle laisse le défaut ne rien certifier, et
elle fait de `Printable()` une contrainte qui élargit le tirage — réintroduisant, sous forme d'un membre
nommé, exactement l'incohérence qu'un lecteur remarque en premier. `Whitespaces()` et `NonPrintable()`
doivent alors être documentés comme exceptions à la règle de rétrécissement, et une règle dont la liste
d'exceptions s'allonge n'est pas une règle.

### Prendre l'ASCII imprimable (0x20–0x7E) comme défaut

Sérieusement envisagée, et adoptée dans une version intermédiaire de ce record : c'est l'univers que le
générateur d'expressions régulières emploie déjà, tout dummy reste visible dans un message d'échec, et
aucun tirage ne peut corrompre un terminal.

Rejetée parce qu'elle cache la classe de défaut que cette décision existe pour exposer. Un retour chariot,
un NUL et un caractère d'échappement sont les caractères les plus susceptibles de casser le stockage,
l'analyse et le traitement des journaux, et un défaut qui les exclut fait qu'aucun test non contraint n'en
rencontre jamais. Elle force aussi `Whitespaces()` — la tabulation est à 0x09 — et `NonPrintable()` à
sortir du défaut, donc la règle garde ses exceptions et seul leur nombre change.

### Tirer dans tout le plan multilingue de base, ou dans Unicode par catégorie

La lecture littérale de « n'importe quel char », et la forme générale de l'idée de famille : `Letter()`,
`Symbol()` et le reste, définis comme la BCL les définit.

Rejetée sur la reproductibilité et sur le périmètre. Le vivier suivrait la version d'Unicode du runtime,
donc une même graine pourrait tirer des valeurs différentes sur deux frameworks cibles, contre une garantie
que ce dépôt vérifie octet par octet ; un substitut est une moitié de caractère et ne peut pas être tiré
comme un caractère ; et la normalisation, les marques combinantes et la casse dépendante de la locale sont
un problème qu'une bibliothèque de dummies n'a pas à posséder. L'ASCII est le plus grand ensemble total,
stable et explicable.

### Exprimer la famille soustractive par un enum de drapeaux

Un seul membre, `Without(Characters.Alpha | Characters.Numeric)`, au lieu d'un par bloc — tout exprimable,
rien à étendre plus tard.

Rejetée sur le style plutôt que sur la capacité. Cela introduit un enum public là où toute la surface est
faite de méthodes fluides nommées, et le site d'appel se lit comme de la configuration plutôt que comme une
phrase. Deux membres couvrent les cas utiles et se composent pour donner le troisième.

### Aligner `Punctuation()` sur `char.IsPunctuation` plutôt que sur POSIX `[:punct:]`

Envisagée parce qu'un appelant .NET peut raisonnablement s'appuyer sur le prédicat de la BCL, et sera
surpris qu'une famille nommée `Punctuation` puisse tirer `+`.

Rejetée parce qu'elle couperait le bloc imprimable en deux et laisserait `+ < = > | $ ^ ~` atteignable par
aucune famille nommée. La divergence est documentée sur le membre à la place, là où un appelant la
rencontre. L'espace reste hors de la famille pour une autre raison : c'est le seul caractère qui disparaît
en silence sous un `Trim()`, et une famille dont le rôle est « un séparateur fiable » ne doit pas en tirer.
L'espace demeure nommable par `Whitespaces()`.

## Conséquences

### Positives

* Un tirage non contraint certifie quelque chose. Un test qui passe avec un dummy contenant un caractère de
  contrôle a établi que le code en tolère un ; aujourd'hui il n'établit rien.
* Toute famille de caractères rétrécit le défaut, **sans aucune exception** — le modèle se referme, et
  `Printable()` est une vraie contrainte au lieu d'un nom pour le défaut.
* Un seul univers sur toute la surface caractère de la bibliothèque, tenu par `CharacterPools` et vérifié
  par le garde de parité, au lieu d'un par générateur.
* Un fragment ancré portant de la ponctuation est légal sous `Printable()`, ce qu'aucune famille nommée
  n'admettait, et JD015 rend à la compilation le même verdict que l'exécution à la déclaration.
* « Un dummy peut-il être `:` » se répond par le défaut lui-même, et la réponse ne dépend plus de la porte
  par laquelle l'appelant est entré.

### Négatives

* Une **version majeure**. Toute graine rejoue d'autres valeurs, le golden master bouge, et tout test
  existant utilisant une chaîne ou un caractère non contraint tire autre chose.
* Un tirage par défaut peut contenir `\0`, `\r`, `\n` et `\x1b`. Le dernier ouvre une séquence
  d'échappement ANSI, donc un test en échec peut corrompre le terminal qui le rapporte — un dommage au
  canal de restitution, non au code testé. La bibliothèque échappe les caractères de contrôle dans sa
  propre sortie de diagnostic ; la façon dont un framework de test rend une valeur lui échappe.
* Neuf membres sur deux builders là où il y en avait trois, chacun portant une entrée de baseline, deux
  branches d'analyzer, un index de famille en property test et une jumelle de documentation.
* `Punctuation()` est délibérément en désaccord avec `char.IsPunctuation`, et la documentation est la seule
  défense.

### Risques

* Un consommateur peut lire le défaut élargi comme la bibliothèque devenue un fuzzer, et tout contraindre
  par précaution — ce qui coûterait exactement la preuve que le changement achète. Atténué en documentant
  le cadrage « l'invariant au site d'appel » plutôt qu'en présentant le défaut comme un test de résistance.
* Le générateur d'expressions régulières résout toujours ses positions libres dans l'ASCII imprimable, donc
  les deux portes redivergent, en sens inverse de celui qui a ouvert cette question. Signalé ci-dessous
  plutôt que tranché ici.
* La copie de la correspondance détenue par les analyzers est la seule hors de la bibliothèque, et rien ne
  vérifie que les deux s'accordent.

## Actions de suivi

* Décider si `RegexAlphabet` suit cette décision — si les positions libres d'un motif tirent dans l'ASCII
  ou restent imprimables. L'argument coupe des deux côtés : le motif est lui-même une contrainte explicite,
  donc son défaut peut légitimement différer ; mais deux univers sont ce que ce record entendait supprimer.
* S'assurer que la bibliothèque échappe les caractères de contrôle partout où elle rend une valeur tirée —
  messages de conflit, inspections de pool, golden master de graine, qui porte déjà un `Escape` pour son
  propre format de fichier.
* Consigner l'axe taille séparément : la même question « défaut le plus large, rétrécissement explicite »
  s'applique à la longueur et au compte, où un maximum déclaré ne pilote pas le tirage aujourd'hui
  (ADR-0029).

## Références

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la règle qui demande si un
  refus est la réponse honnête, et la raison pour laquelle ce record énonce où l'univers s'arrête.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) — pourquoi c'est une version
  majeure, et la garantie entre frameworks cibles qui écarte Unicode.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — le refus de laisser
  une décision de domaine au filtrage de l'appelant, que ce record suit.
* [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md) — le générateur dont les
  positions libres tirent dans l'ASCII imprimable, et la divergence laissée ouverte ci-dessus.
* `JustDummies/CharacterPools.cs` et `JustDummies/RegexAlphabet.cs` — les définitions que cette décision
  unifie.
