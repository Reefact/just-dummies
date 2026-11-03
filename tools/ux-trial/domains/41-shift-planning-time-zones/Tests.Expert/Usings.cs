global using System.Globalization;

global using JustDummies;
global using JustDummies.Xunit;

global using ShiftPlanning.Domain;
global using ShiftPlanning.Domain.Tests.Expert.Generators;

global using Xunit;

// Every test case runs on its own seed, reported when (and only when) the case fails.
[assembly: Reproducible]
