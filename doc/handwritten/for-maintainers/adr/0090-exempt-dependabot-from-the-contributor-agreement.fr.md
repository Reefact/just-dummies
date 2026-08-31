# ADR-0090 | Exempter Dependabot de l'accord de contribution, uniquement sur son propre commit signé

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0090-exempt-dependabot-from-the-contributor-agreement.md)

**Status:** Accepted
**Proposed:** 2026-08-31
**Accepted:** 2026-08-31
**Decision Makers:** Reefact

## Contexte

JustDummies appartient à Sylvain Aurat, agissant à titre personnel. Toute personne autre que le
Project Owner doit accepter le [Contributor Agreement](../../../../CONTRIBUTOR_AGREEMENT.md) (en
anglais) avant qu'une contribution soit acceptée ; une pull request soumise directement par le
Project Owner n'a pas besoin de l'acquiescement, et une pull request produite par le workflow Claude
Code du Project Owner le conserve et le vérifie tout de même. Un contrôle de CI applique cette règle
en lisant une case d'acquiescement dans le corps de la pull request.

L'accord définit une Contribution comme un matériel que le contributeur **soumet intentionnellement**.
Il demande à ce contributeur de déclarer qu'il est juridiquement fondé à la soumettre et qu'il détient
les droits nécessaires pour conclure l'accord, et il emporte cession des droits patrimoniaux
transmissibles sur ce qu'il a soumis.

Dependabot est une application GitHub, pas une personne juridique. Il ouvre une pull request parce que
la configuration de dépendances propre à ce dépôt lui indique quels écosystèmes surveiller, et ce
qu'il soumet est un numéro de version répondant à une publication amont.

Dependabot ne remplit pas le modèle de pull request de ce dépôt : la case d'acquiescement n'est donc
jamais présente dans le corps qu'il écrit. Le contrôle échoue sur toute pull request Dependabot. Les
deux pull requests ouvertes depuis l'arrivée du modèle le montrent : sur la première, l'échec a été
levé en éditant le corps à la main ; la seconde est ouverte et n'échoue sur rien d'autre.

Les mises à jour Dependabot de type patch et mineur sont fusionnées par un workflow dès que les
contrôles requis passent, sans intervention humaine.

Toute personne disposant d'un accès en écriture peut ajouter des commits sur une branche Dependabot.
Ouvrir une pull request ne fige pas ce que cette pull request portera ensuite. Les deux autres
workflows Dependabot du dépôt tranchent déjà l'identité sur ce point : un contrôle de l'auteur **et**
la signature de GitHub sur la tête de branche avant d'armer une action, un signal plus faible pour en
retirer une.

Le workflow d'autofix répare une pull request Dependabot en l'amendant ou en la rebasant, ce qui
conserve Dependabot comme auteur du commit et retire la signature de GitHub de la tête.

## Décision

Une pull request ouverte par Dependabot n'exige aucun acquiescement au Contributor Agreement tant que
sa tête est le propre commit de Dependabot signé par GitHub, et elle en exige un comme n'importe
quelle autre pull request sur toute autre tête.

## Justification

Le contrôle recueille une cession de droits patrimoniaux et un ensemble de déclarations sur l'œuvre.
Dependabot ne peut donner ni l'une ni les autres : il n'est pas une personne juridique, et le numéro
de version qu'il a écrit sur instruction de ce dépôt ne porte aucun droit de tiers qu'il pourrait
céder. Exiger la case sur ses pull requests réclame un consentement que personne n'est en position de
donner, et ne l'obtient que parce qu'un humain la coche à la place du bot — une signature sans
signataire derrière elle. Nommer qui le contrôle n'a jamais pu engager n'ôte rien à ce qu'il recueille
auprès de ceux qu'il engage.

Le coût du statu quo pèse sur le contrôle, pas sur Dependabot. Chaque pull request de dépendance
arrive avec un contrôle de gouvernance en échec qu'un humain lève en éditant le corps. Un contrôle
dont la procédure normale est un contournement manuel cesse d'être lu comme un contrôle ; le
contournement de routine est le dommage ici, pas la croix rouge. Il casse en outre la voie qu'il
traverse, puisque les mises à jour patch et mineures sont censées atterrir sans humain et qu'un
contrôle qu'elles ne peuvent jamais passer en replace un devant chacune d'elles.

Renoncer à un acquiescement est la direction où une erreur coûte quelque chose : elle prend donc la
preuve forte plutôt que la commode. Une exemption fondée sur qui a ouvert la pull request reposerait
sur un fait qui ne dit rien de ce que la branche porte maintenant : ajouter des commits sur une
branche Dependabot est ouvert à quiconque a l'accès en écriture, et ce qui est ajouté est une
Contribution au sens même de l'accord. Exiger que la tête soit encore le propre commit signé de
Dependabot lie l'exemption à l'œuvre plutôt qu'à l'étiquette posée dessus — un nom d'auteur de commit
est un réglage local et se falsifie librement, la signature de GitHub non.

Cette même condition répond au cas Claude Code sans seconde règle. Une pull request Dependabot réparée
porte un changement écrit par le workflow modèle du Project Owner, et le modèle tient déjà qu'un tel
changement conserve l'acquiescement. La réparation retire la signature : l'exigence revient donc par
construction, et non par une exception que quelqu'un doit penser à écrire.

## Alternatives envisagées

### Continuer à lever le contrôle à la main

Cela ne demande aucun changement et laisse le contrôle exactement tel qu'il est écrit.

Rejeté parce que cela fait du contournement manuel d'un contrôle de gouvernance la façon ordinaire
dont les mises à jour de dépendances atterrissent, ce qui érode le contrôle plus vite que n'importe
quelle exemption écrite, et parce que cela poste un humain dans la seule voie dont toute la valeur est
qu'aucun n'y soit nécessaire.

### Exempter tout auteur robot

Lire le type du compte auteur ne demande aucune liste à tenir à jour, et couvre d'un coup toute
automatisation future.

Rejeté parce que cela remet la dispense à toute GitHub App jamais installée sur le dépôt, présente ou
future, sur la foi d'un indicateur de type — une concession bien plus large que le cas qui l'a
motivée, et qui s'élargirait de nouveau en silence à chaque application ajoutée.

### Exempter sur le seul auteur, sans la tête signée

C'est plus simple, cela ne demande aucune seconde lecture, et cela survit à une réparation par le
workflow d'autofix.

Rejeté parce que cela renonce à l'acquiescement pour ce que la branche porte plutôt que pour ce que
Dependabot a écrit. C'est exactement le contrôle que les deux autres workflows Dependabot du dépôt ont
déjà jugé insuffisant, pour exactement la même raison.

### Rendre le contrôle consultatif sur les pull requests de robots

Le dépôt a déjà choisi de rendre sa porte de mutation consultative plutôt que de la retirer
([ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md)) lorsqu'elle s'est mise à
produire des échecs qui ne disaient rien de la pull request.

Rejeté parce que les deux portes font un travail différent. La porte de mutation mesure, et une mesure
vaut encore d'être rapportée quand elle ne peut plus bloquer. Celle-ci recueille un consentement, et
un consentement consultatif ne recueille rien : un contrôle qui annonce « non accepté » et laisse la
fusion se faire est pire que pas de contrôle du tout, parce qu'il a l'air d'une protection.

## Conséquences

### Positives

* Une pull request de dépendance passe au vert d'elle-même, et la voie patch et mineure se referme
  sans qu'un humain y soit posté.
* L'exemption n'ouvre aucun passage à une contribution ajoutée derrière le nom de Dependabot : le
  contrôle de la tête la retire dès que la branche cesse d'être l'œuvre propre de Dependabot.
* L'exemption est énoncée dans les termes de l'accord lui-même — qui est en position de faire ses
  déclarations — et non comme une commodité accordée à la CI.

### Négatives

* Une pull request Dependabot réparée par le workflow d'autofix redemande l'acquiescement, y compris
  après une réparation triviale dont ce workflow conserve délibérément l'auto-merge. Le Project Owner
  coche alors la case ou fait atterrir la pull request à la main.
* Une lecture d'API supplémentaire par événement sur une pull request Dependabot.

### Risques

* Le verdict décrit la tête que l'événement a rapportée. Une poussée ultérieure lève son propre
  événement et est contrôlée à son tour, la fenêtre est donc l'ordinaire, mais le contrôle répond de
  ce qu'il a lu.
* Si GitHub cessait de signer les commits de Dependabot, toute pull request Dependabot redemanderait
  la case. Bruyant, jamais dangereux : l'échec tombe du côté qui réclame le consentement.

## Actions de suivi

* Si l'acquiescement sur une pull request Dependabot trivialement réparée devient une friction de
  routine, décider si le workflow d'autofix doit préserver une signature vérifiable, ou si la classe
  triviale de réparation mérite sa propre exemption — comme une décision, pas comme un correctif.

## Références

* `.github/workflows/contributor-agreement.yml` — la porte et cette exemption.
* `.github/workflows/dependabot-automerge.yml` et `.github/workflows/dependabot-autofix.yml` — les
  contrôles d'identité que cette exemption reprend.
* [`CONTRIBUTOR_AGREEMENT.md`](../../../../CONTRIBUTOR_AGREEMENT.md) (en anglais) — §1 « Contribution »
  et §2 « Ownership and authority ».
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md) — la porte consultative que
  celle-ci n'est délibérément pas.
