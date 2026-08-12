using System;
using System.Linq;
using System.Reflection;

using NFluent;

using Spectre.Console;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The command line of §3, read off the type that declares it.
/// </summary>
/// <remarks>
///     §3 ends with "That is the entire surface", and this is what holds the code to that sentence: an option
///     added here — a `--check`, an `--all`, a config file switch — fails the test that says the set is closed,
///     and has to be argued for in the specification first. Deferred options (§16) are refused by the same check
///     while they are deferred.
///     <para>
///         Read through <see cref="CustomAttributeData" /> rather than through Spectre's attribute properties, so
///         the assertion is on the template the source declares and survives a change to the framework's own API.
///     </para>
/// </remarks>
public sealed class GenerateSettingsTests {

    [Fact(DisplayName = "generate declares the eight options of §3, and no others.")]
    public void GenerateDeclaresTheOptionsOfTheSpecification() {
        string[] expected = [
            "--project <PATH>", "--output <DIR>", "--namespace <NAMESPACE>", "--force", "--dry-run",
            "--entry-point <VALUE>", "--entry-point-namespace <NAMESPACE>", "--format <FORMAT>"
        ];

        Check.That(TemplatesOf("Spectre.Console.Cli.CommandOptionAttribute")).IsEquivalentTo(expected);
    }

    [Fact(DisplayName = "generate takes its types as the one positional argument.")]
    public void GenerateTakesItsTypesAsTheOnePositionalArgument() {
        Check.That(TemplatesOf("Spectre.Console.Cli.CommandArgumentAttribute")).IsEquivalentTo("<TYPE>");

        PropertyInfo types = typeof(GenerateSettings).GetProperty(nameof(GenerateSettings.Types))!;

        // Several types are processed independently, and the exit code is the worst of them (§7). That only
        // reads as one invocation if the argument is a collection.
        Check.That(types.PropertyType).IsEqualTo(typeof(string[]));
    }

    // Every option carries a [Description]: it is what `dum generate --help` prints, and the defaults of §3 are
    // only discoverable there.
    [Fact(DisplayName = "Every part of the command line describes itself in the help.")]
    public void EveryPartOfTheCommandLineDescribesItself() {
        string[] undescribed = DeclaredParts()
                              .Where(property => property.GetCustomAttributesData()
                                                         .All(attribute => attribute.AttributeType.FullName
                                                                        != "System.ComponentModel.DescriptionAttribute"))
                              .Select(property => property.Name)
                              .ToArray();

        Check.That(undescribed).IsEmpty();
    }

    /// <summary>
    ///     An option given as an empty string is refused where it is read, not where it is used.
    /// </summary>
    /// <remarks>
    ///     <c>--namespace ""</c> is not "no override": it is an override to nothing, and the three options
    ///     would each answer it differently and late — a path routine throwing, a namespace declared empty.
    /// </remarks>
    [Theory(DisplayName = "An option given as an empty value is refused, naming which one.")]
    [InlineData("--project")]
    [InlineData("--output")]
    [InlineData("--namespace")]
    public void AnEmptyOptionValueIsRefused(string option) {
        GenerateSettings settings = new() { Types = ["Order"] };

        switch (option) {
            case "--project": settings.Project = "   "; break;
            case "--output":  settings.Output = "   "; break;
            default:          settings.Namespace = "   "; break;
        }

        ValidationResult refused = settings.Validate();

        Check.That(refused.Successful).IsFalse();
        Check.That(refused.Message).Contains(option);
    }

    [Fact(DisplayName = "A command line with nothing left blank validates.")]
    public void ACompleteCommandLineValidates() {
        Check.That(new GenerateSettings { Types = ["Order"] }.Validate().Successful).IsTrue();
    }

    private static string[] TemplatesOf(string attributeName) {
        return DeclaredParts()
              .SelectMany(property => property.GetCustomAttributesData())
              .Where(attribute => attribute.AttributeType.FullName == attributeName)
              // The FIRST string argument in both attributes: an option is (template, isHidden), an argument is
              // (position, template). Neither is "the last one" — that read empty, and IsOnlyMadeOf passed on
              // the empty set, which is why the assertion below is an equivalence.
              .Select(attribute => attribute.ConstructorArguments
                                            .FirstOrDefault(argument => argument.Value is string)
                                            .Value as string)
              .Where(template => template is not null)
              .Select(template => template!)
              .ToArray();
    }

    private static PropertyInfo[] DeclaredParts() {
        PropertyInfo[] properties = typeof(GenerateSettings)
           .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // A settings type read by reflection is one rename away from testing nothing at all.
        Check.That(properties).Not.IsEmpty();

        return properties;
    }

}
