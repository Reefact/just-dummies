global using JustDummies;
global using JustDummies.Xunit;

global using Payroll.Domain;
global using Payroll.Tests.Expert.Generators;

global using Xunit;

// Every test draws its dummies from a seed that is reported when the test fails.
[assembly: Reproducible]
