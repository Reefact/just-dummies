# Identifiants et URI

🌍 **Langues :**  
🇬🇧 [English](./guids-and-uris.en.md) | 🇫🇷 Français (ce fichier)

Deux générateurs pour les deux sortes d'identifiants qui apparaissent dans presque tous les tests :
l'opaque, et le structuré.

## Guid

```csharp
Guid anyId       = Any.Guid().Generate();
Guid anyNonEmpty = Any.Guid().NonEmpty().Generate();
Guid anyEmpty    = Any.Guid().Empty().Generate();        // toujours Guid.Empty
Guid anyNotThis  = Any.Guid().DifferentFrom(Guid.Empty).Generate();
Guid anyOneOf    = Any.Guid().OneOf(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                                 Guid.Parse("22222222-2222-2222-2222-222222222222")).Generate();
```

`Empty()` mérite sa place parce que `Guid.Empty` est un cas distinct dans la plupart des domaines :
l'identifiant pas encore attribué. Un test couvrant « que se passe-t-il quand l'identifiant
manque ? » se lit mieux en `Any.Guid().Empty()` qu'en littéral, car il reste dans le même vocabulaire
que ses voisins.

`NonEmpty()` en est le miroir, et c'est celui à saisir par défaut : une entité qui existe a un
identifiant, et laisser le tirage errer jusqu'à `Guid.Empty` testerait occasionnellement un état que
votre domaine ne possède pas.

## URI

`Any.Uri()` est le point d'entrée et, non contraint, il couvre tout l'espace d'URI sûr — une URI web,
WebSocket, FTP ou mailto absolue, ou une référence relative :

```csharp
Uri anyUri = Any.Uri().Generate();
```

Se restreindre à une **famille** renvoie un constructeur n'exposant que les composants que cette
famille possède réellement. C'est tout l'intérêt de cette conception : une combinaison impossible —
un port sur un `mailto:`, un fragment sur une URI WebSocket — ne peut même pas s'écrire.

```mermaid
flowchart TD
    accTitle: Les sortes d'URI que Any.Uri() sait tirer
    accDescr: Any.Uri() se ramifie vers Web pour http et https, WebSocket pour ws et wss, Ftp pour ftp, Mailto pour mailto, et Relative pour un chemin tel que /a/b/c.
    U["Any.Uri()"] --> W["Web()<br/><i>http, https</i>"]
    U --> S["WebSocket()<br/><i>ws, wss</i>"]
    U --> F["Ftp()<br/><i>ftp</i>"]
    U --> M["Mailto()<br/><i>mailto</i>"]
    U --> R["Relative()<br/><i>/a/b/c</i>"]
    style U fill:#e8eaf6,stroke:#3f51b5,color:#1a237e
```

Chaque composant est tiré parmi les caractères ASCII non réservés et l'URI est assemblée directement :
une valeur est donc valide par construction. Les hôtes internationalisés (IDN) et le schéma `file`
sont volontairement hors du tirage non contraint : ni l'un ni l'autre ne fait l'aller-retour à
l'identique selon la cible, ce qui casserait le contrat de déterminisme.

Un hôte que vous épinglez s'écrit comme l'URI le portera. Passez la forme punycode d'un nom
internationalisé, une adresse IPv4 complète en quatre octets (`1.2.3.4`, jamais `1.2`), et une
adresse IPv6 entre crochets (`[::1]`). Toute autre écriture est refusée dès la déclaration, et le
message indique la forme à passer.

### URI web

```csharp
Uri anyPage     = Any.Uri().Web().Generate();
Uri anySecure   = Any.Uri().Web().UsingHttps().WithHost("api.example.com").Generate();
Uri anyInsecure = Any.Uri().Web().UsingHttp().Generate();
Uri anyDeep     = Any.Uri().Web().WithPathSegments(3).Generate();          // /a/b/c
Uri anyBare     = Any.Uri().Web().WithoutPath().Generate();
Uri anyOnAPort  = Any.Uri().Web().WithPort(8080).Generate();
Uri anyPort     = Any.Uri().Web().WithPort().Generate();                   // un port, non précisé
Uri anyQueried  = Any.Uri().Web().WithQuery().WithFragment().Generate();
Uri anyWithAuth = Any.Uri().Web().WithUserInfo("alice", "secret").Generate();
```

`WithPort()` sans argument demande qu'*un* port soit présent sans dire lequel — la contrainte qu'il
vous faut quand le code testé doit gérer un port explicite mais que le numéro n'a aucune importance.
`WithUserInfo` a trois formes : sans argument, avec un utilisateur, ou avec un utilisateur et un mot
de passe.

### URI WebSocket

```csharp
Uri anySocket = Any.Uri().WebSocket().Generate();                    // ws:// ou wss://
Uri anySecure = Any.Uri().WebSocket().UsingWss().WithHost("live.example.com").Generate();
Uri anyPlain  = Any.Uri().WebSocket().UsingWs().WithPathSegments(2).WithQuery().Generate();
```

Une URI WebSocket n'a pas de fragment : il n'y a donc pas de `WithFragment` à appeler.

### URI FTP

```csharp
Uri anyArchive = Any.Uri().Ftp().Generate();
Uri anyHosted  = Any.Uri().Ftp().WithHost("files.example.com").WithPathSegments(2).Generate();
Uri anyAccount = Any.Uri().Ftp().WithUserInfo("alice").WithPort(2121).Generate();
Uri anyRoot    = Any.Uri().Ftp().WithoutPath().Generate();
```

### URI mailto

```csharp
Uri anyMail    = Any.Uri().Mailto().Generate();
Uri anyToAlice = Any.Uri().Mailto().WithLocalPart("alice").WithDomain("example.com").Generate();
Uri anyWithCc  = Any.Uri().Mailto().WithHeaders().Generate();
```

Un `mailto:` n'a ni hôte, ni port, ni chemin — il a une partie locale, un domaine et des en-têtes
optionnels — et le constructeur expose exactement cela.

### Références relatives

```csharp
Uri anyRelative = Any.Uri().Relative().Generate();
Uri anyRooted   = Any.Uri().Relative().Rooted().Generate();                    // /a/b/c
Uri anyDeep     = Any.Uri().Relative().WithPathSegments(3).Generate();
Uri anyQueried  = Any.Uri().Relative().WithPathSegments(1).WithQuery().WithFragment().Generate();
```

Une combinaison mérite d'être connue : une référence relative sans segment de chemin, sans requête,
sans fragment et sans racine est la **référence vide**. Elle est licite, et n'est presque jamais ce
qu'un test voulait dire — d'où son propre diagnostic, [JD026](../analyzers/JD026.fr.md). C'est la
seule chaîne d'URI dont l'échec atterrit au moment de l'action plutôt qu'à la ligne d'arrangement, et
c'est la raison d'être de l'analyzer.

## Composer un identifiant dans votre propre type

Les deux générateurs alimentent `.As(...)` comme n'importe quel autre :

```csharp
IAny<Customer> customerGenerator = Any.Combine(
    Any.Guid().NonEmpty(),
    Any.String().Alpha().WithLengthBetween(3, 20),
    Any.Uri().Mailto().WithDomain("example.test"),
    (id, name, mail) => new Customer(id, name, mail.ToString()));

Customer anyCustomer = customerGenerator.Generate();
```

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
