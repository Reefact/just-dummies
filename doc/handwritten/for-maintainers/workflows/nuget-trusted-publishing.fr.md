# Publication de confiance sur nuget.org

`release.yml` publie via la **publication de confiance** (*trusted publishing*) : le job échange son
jeton OIDC GitHub contre une clé d'API NuGet à durée de vie courte et à usage unique. Aucune clé
durable n'est stockée où que ce soit. L'échange ne fonctionne que tant que nuget.org détient une
politique nommant ce dépôt et ce workflow.

La mise en place ne peut pas être automatisée : créer la politique exige une session authentifiée sur
nuget.org en tant que propriétaire du paquet.

## Ce que cette page ne vous dit pas

**Quels paquets et quelles versions ont été publiés.** nuget.org et les tags `<train>-v*` de ce dépôt
y répondent déjà, de façon autoritaire et sans que personne ait à penser à les mettre à jour. Une
liste ici serait une troisième copie, fausse dès le lendemain d'une release que personne n'a songé à
documenter — ce qui est précisément arrivé à la version de cette page qui s'y était essayée.

Pour le savoir, lisez la source :

```
curl -s https://api.nuget.org/v3-flatcontainer/justdummies/index.json
git tag --list 'lib-v*' 'xunit-v*' 'catalog-v*'
```

## ⚠️ Pousser un tag de train publie

`release.yml` se déclenche sur `lib-v*`, `xunit-v*`, `catalog-v*` et `cli-v*`. Pousser un tel tag
empaquette le commit tagué et le pousse sur nuget.org, et **une version publiée est immuable** — elle
peut être délistée et dépréciée, jamais retirée ni remplacée.

Ce n'est pas hypothétique : un tag `lib-v0.0.0-rulesetcheck` a un jour été poussé pour tester un
réglage de protection de tags, et il a publié. Pour éprouver quoi que ce soit sur les tags de release
sans publier, utilisez une ref que le déclencheur ne reconnaît pas, ou le galop d'essai ci-dessous.

## Ce qu'il faut configurer sur nuget.org

Connectez-vous avec le compte propriétaire des paquets, puis créez une politique sous
*Account settings → Trusted Publishing*.

| Champ | Valeur |
| --- | --- |
| Package owner | `Reefact` |
| Repository owner | `Reefact` |
| Repository | `just-dummies` |
| Workflow file | `release.yml` |
| Environment | *(laisser vide — `release.yml` ne déclare aucun environnement)* |

**La politique porte sur le dépôt, non sur un identifiant de paquet.** Une seule politique couvre
tout paquet publié par ce dépôt : un nouvel identifiant — un nouveau train de release — n'en demande
donc aucune nouvelle.

Un identifiant que personne ne possède encore est réservé au compte lors du premier push réussi : un
paquet peut donc être publié avant d'exister sur nuget.org. Vérifiez que l'identifiant n'est pas déjà
pris par quelqu'un d'autre avant de compter dessus.

## Ce qu'il faut configurer sur GitHub

Une seule **variable** de dépôt — *Settings → Secrets and variables → Actions → Variables* :

| Variable | Valeur |
| --- | --- |
| `NUGET_USER` | le **nom d'utilisateur** du compte nuget.org (le nom de profil, pas l'adresse e-mail) |

Une variable et non un secret, délibérément : le nom d'utilisateur est public sur le profil nuget.org
et c'est un identifiant, pas un justificatif. Le seul justificatif de ce chemin est la clé à durée de
vie courte que frappe l'échange OIDC, et elle ne quitte jamais le runner. Stocker le nom
d'utilisateur en secret le masquerait dans les logs et rendrait un échec de connexion plus difficile
à diagnostiquer, sans rien protéger.

`release.yml` le lit dans `vars.NUGET_USER` et le passe à `NuGet/login`. Rien d'autre, sur le chemin
de release, n'a besoin d'un secret : le jeton OIDC est frappé par GitHub, et `GITHUB_TOKEN` couvre la
GitHub Release. Le définir comme *secret* laisse `vars.NUGET_USER` vide et la connexion échoue sur
`Input required and not supplied: user`.

Aucune protection de branche, aucun environnement ni aucune approbation n'est requis. Si l'un d'eux
est ajouté plus tard, son nom devra être déclaré à la fois dans `release.yml` (`environment:`) et
dans la politique nuget.org — l'échange s'appuie dessus.

## Vérifier sans publier

```
gh workflow run release.yml -f component=lib -f version=0.0.0-dry.1 -f dry_run=true
```

Une exécution verte prouve tout le pipeline de bout en bout — restore, build, test, pack, SBOM,
attestation et échange OIDC — sans rien publier : le push et la GitHub Release sont les seules étapes
qu'un galop d'essai saute. Tant qu'aucune politique n'existe, toute exécution y compris celle-ci
échoue sur `NuGet login (OIDC)`, et c'est voulu : la répétition existe pour qu'une erreur de
configuration se révèle avant une vraie release plutôt que pendant.
