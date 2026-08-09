# `JustDummies.DiagnosticCatalog`

🌍 **Langues :**  
🇬🇧 [English](./justdummies-diagnosticcatalog.en.md) | 🇫🇷 Français (ce fichier)

Les règles `JD001`–`JD028`, publiées sous forme de constantes qu'un `[SuppressMessage]` peut nommer.
Ce paquet ne vous est utile que si vous supprimez un diagnostic JustDummies et souhaitez que le
compilateur vérifie que la règle nommée existe.

## Le problème qu'il résout

Une suppression nomme normalement sa règle par deux littéraux de chaîne :

<!-- jd:declarations -->
```csharp
internal static class LegacyArrangements {

    [SuppressMessage("JustDummies.Usage", "JD006:DiscardedGeneratorResult", Justification = "Mesuré : la contrainte est appliquée en amont.")]
    internal static void Legacy() {
        // ...
    }

}
```

Rien ne vérifie ces chaînes. Faites une faute dans la catégorie et la suppression cesse
silencieusement d'agir. Retirez ou renumérotez la règle et l'attribut subsiste, ne supprimant plus
rien, sans le moindre avertissement — le compilateur n'a jamais résolu ces chaînes, il ne peut donc
pas remarquer qu'elles sont périmées.

## La correction

Référencez le catalogue et nommez la règle via des constantes que le compilateur résout :

<!-- jd:declarations -->
```csharp
internal static class LegacyArrangements {

    [SuppressMessage(JustDummiesRule.JD006.Category, JustDummiesRule.JD006.Id, Justification = "Mesuré : la contrainte est appliquée en amont.")]
    internal static void Legacy() {
        // ...
    }

}
```

Désormais, une catégorie renommée ou une règle retirée devient une **erreur de compilation** sur
chaque site de suppression, c'est-à-dire précisément là où la décision de supprimer a été prise
([ADR-0050](../../for-maintainers/adr/0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md)).

## Installation

```bash
dotnet add package JustDummies.DiagnosticCatalog
```

Il ne porte ni générateur ni analyzer propre — seulement les identifiants. Il est `netstandard2.0`,
comme tout le reste ici.

Ajoutez l'espace de noms une fois, dans votre fichier projet, et chaque site de suppression voit les
constantes sans `using` supplémentaire :

```xml
<ItemGroup>
  <Using Include="JustDummies.Diagnostics" />
</ItemGroup>
```

## Ce que porte chaque constante de règle

```csharp
string id       = JustDummiesRule.JD006.Id;          // « JD006 »
string category = JustDummiesRule.JD006.Category;    // « JustDummies.Usage »
string title    = JustDummiesRule.JD006.Title;
string helpLink = JustDummiesRule.JD006.HelpLinkUri; // la page de documentation de la règle
```

`Title` et `HelpLinkUri` sont là pour l'outillage qui rapporte des règles — un résumé de build, un
tableau de bord, un rapporteur maison — afin que la description et le lien proviennent du même
endroit que ce que lit l'analyzer, plutôt que d'une seconde liste qui dérive.

## Les quatre catégories

`JustDummiesCategory` publie les chaînes de catégorie seules, pour du code qui regroupe des règles
au lieu d'en nommer une :

| Constante | Valeur | Regroupe |
| --- | --- | --- |
| `JustDummiesCategory.Reproducibility` | `JustDummies.Reproducibility` | graines, portées, corps asynchrones |
| `JustDummiesCategory.Usage` | `JustDummies.Usage` | la frontière recette/valeur |
| `JustDummiesCategory.Constraints` | `JustDummies.Constraints` | jeux de contraintes décidables à la compilation |
| `JustDummiesCategory.Composition` | `JustDummies.Composition` | opérandes de `Combine`, contrats d'élément |

## En ai-je besoin ?

**Non**, si vous ne supprimez jamais de règle JustDummies — ce qui est le cas courant. Les analyzers
sont embarqués dans `JustDummies` lui-même et fonctionnent sans ce paquet.

**Oui**, si des suppressions apparaissent dans votre base de code et que vous préférez qu'elles
soient vérifiées plutôt que crues. Le catalogue publié est ce qui fait d'une suppression une
affirmation vérifiée par le compilateur, et non un commentaire qui se trouve être un attribut
([ADR-0052](../../for-maintainers/adr/0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md)).

Pour les règles elles-mêmes, voir l'[index des règles](../analyzers/README.fr.md).

---

[← Paquets](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
