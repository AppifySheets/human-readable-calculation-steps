using HumanReadableCalculationSteps;

var x = 6m.As("x");
var y = 4m.As("y");
var z = 2m.As("z");

var difference = (x - y).As("Diff");
var result = difference * z;

Console.WriteLine("=== Difference ===");
Console.WriteLine($"difference.Value: {difference.Value}");
Console.WriteLine($"difference.Precedence: {difference.Precedence}");
Console.WriteLine($"difference._caption: {difference._caption}");
Console.WriteLine($"difference.CalculationSteps count: {difference.CalculationSteps.Count}");
foreach (var step in difference.CalculationSteps)
{
    Console.WriteLine($"  - {step}");
}
Console.WriteLine($"difference.FinalCalculationSteps:");
Console.WriteLine($"\"{difference.FinalCalculationSteps}\"");

Console.WriteLine();
Console.WriteLine("=== Result ===");
Console.WriteLine($"result.Value: {result.Value}");
Console.WriteLine($"result.Precedence: {result.Precedence}");
Console.WriteLine($"result._caption: {result._caption}");
Console.WriteLine($"result.CalculationSteps count: {result.CalculationSteps.Count}");
foreach (var step in result.CalculationSteps)
{
    Console.WriteLine($"  - {step}");
}
Console.WriteLine($"result.FinalCalculationSteps:");
Console.WriteLine($"\"{result.FinalCalculationSteps}\"");