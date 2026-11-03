global using JustDummies;
global using JustDummies.Xunit;

global using KnittingPatterns.Domain;
global using KnittingPatterns.Tests.Expert.Generators;
global using KnittingPatterns.Tests.Expert.Support;

global using Xunit;

// Every test draws from a pinned seed that is reported when it fails; replay with [Reproducible(Seed = n)].
[assembly: Reproducible]
