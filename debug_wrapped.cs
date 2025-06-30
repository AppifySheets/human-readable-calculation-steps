using System;

// Simple test to understand multiline formatting
class DebugTest 
{
    static void Main() 
    {
        Console.WriteLine("Testing the multiline logic...");
        
        // Test if "a[8] + b[2] × c[4] + d[3] × e[5]" (4 operators) should be multiline
        var expression = "a[8] + b[2] × c[4] + d[3] × e[5]";
        var operatorCount = CountOperators(expression);
        var shouldBeMultiline = operatorCount > 3 || expression.Length > 150;
        
        Console.WriteLine($"Expression: {expression}");
        Console.WriteLine($"Operator count: {operatorCount}");
        Console.WriteLine($"Expression length: {expression.Length}");
        Console.WriteLine($"Should be multiline: {shouldBeMultiline}");
    }
    
    static int CountOperators(string expression)
    {
        int count = 0;
        count += expression.Split(" + ").Length - 1;
        count += expression.Split(" - ").Length - 1;
        count += expression.Split(" × ").Length - 1;
        count += expression.Split(" ÷ ").Length - 1;
        return count;
    }
}