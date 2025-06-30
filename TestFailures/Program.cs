using HumanReadableCalculationSteps;

// Testing expressions that might be failing:

Console.WriteLine("Testing expressions that might be failing:");
Console.WriteLine();

// Test 1: NestedPrecedence_MultipleHighPrecedenceOperations
var a = 8m.As("a");
var b = 2m.As("b");
var c = 4m.As("c");
var d = 3m.As("d");
var e = 5m.As("e");
var result1 = a + b * c + d * e;
Console.WriteLine($"Expression: a + b * c + d * e");
Console.WriteLine($"Operators: 4, Length: {result1.ToString().Length}");
Console.WriteLine($"FinalSteps: {result1.FinalCalculationSteps}");
Console.WriteLine();

// Test 2: ComplexParentheses_AdditionOfProducts
var a2 = 2m.As("a");
var b2 = 3m.As("b");
var c2 = 4m.As("c");
var d2 = 5m.As("d");
var e2 = 1m.As("e");
var result2 = a2 * b2 + c2 * d2 - e2;
Console.WriteLine($"Expression: a * b + c * d - e");
Console.WriteLine($"Operators: 4, Length: {result2.ToString().Length}");
Console.WriteLine($"FinalSteps: {result2.FinalCalculationSteps}");
Console.WriteLine();

// Test 3: NestedPrecedence_DivisionWithAdditionSubtraction
var a3 = 8m.As("a");
var b3 = 2m.As("b");
var c3 = 4m.As("c");
var d3 = 3m.As("d");
var e3 = 5m.As("e");
var result3 = a3 / b3 - c3 + d3 * e3;
Console.WriteLine($"Expression: a / b - c + d * e");
Console.WriteLine($"Operators: 4, Length: {result3.ToString().Length}");
Console.WriteLine($"FinalSteps: {result3.FinalCalculationSteps}");