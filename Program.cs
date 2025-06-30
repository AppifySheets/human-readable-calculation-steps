using HumanReadableCalculationSteps;
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing OriginalExample_FinalPriceWithPrecedence:");
        var basePrice = 100m.As("საბაზო ფასი");
        var taxRate = 0.18m.As("დღგ");
        var discount = 15m.As("ფასდაკლება");
        var multiplier = 120.0m.As("ასოცი");

        var tax = basePrice * taxRate;
        var discountedPrice = basePrice - discount;
        var finalPrice = discountedPrice + tax * multiplier;

        Console.WriteLine($"Value: {finalPrice.Value}");
        Console.WriteLine($"Current output: {finalPrice.FinalCalculationSteps}");
        Console.WriteLine();

        Console.WriteLine("Testing ComplexParentheses_AdditionOfProducts:");
        var a = 2m.As("a");
        var b = 3m.As("b");
        var c = 4m.As("c");
        var d = 5m.As("d");
        var e = 1m.As("e");
        var result = (a * b) + (c * d) - e;

        Console.WriteLine($"Value: {result.Value}");
        Console.WriteLine($"Current output: {result.FinalCalculationSteps}");
        Console.WriteLine();

        Console.WriteLine("Testing NestedPrecedence_DivisionWithAdditionSubtraction:");
        var a2 = 8m.As("a");
        var b2 = 2m.As("b");
        var c2 = 4m.As("c");
        var d2 = 3m.As("d");
        var e2 = 5m.As("e");
        var result2 = a2 / b2 - c2 + d2 * e2;

        Console.WriteLine($"Value: {result2.Value}");
        Console.WriteLine($"Current output: {result2.FinalCalculationSteps}");
        Console.WriteLine();

        Console.WriteLine("Testing NestedPrecedence_MixedOperations:");
        var a3 = 8m.As("a");
        var b3 = 2m.As("b");
        var c3 = 4m.As("c");
        var d3 = 3m.As("d");
        var e3 = 5m.As("e");
        var result3 = a3 * b3 + c3 * d3 - e3;

        Console.WriteLine($"Value: {result3.Value}");
        Console.WriteLine($"Current output: {result3.FinalCalculationSteps}");
        Console.WriteLine();

        Console.WriteLine("Testing NestedPrecedence_MultipleHighPrecedenceOperations:");
        var a4 = 8m.As("a");
        var b4 = 2m.As("b");
        var c4 = 4m.As("c");
        var d4 = 3m.As("d");
        var e4 = 5m.As("e");
        var result4 = a4 + b4 * c4 + d4 * e4;

        Console.WriteLine($"Value: {result4.Value}");
        Console.WriteLine($"Current output: {result4.FinalCalculationSteps}");
    }
}