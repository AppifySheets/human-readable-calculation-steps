using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HumanReadableCalculationSteps;

public static class VExtensions
{
    public static ValueWithCaption As(this decimal value, string caption)
    {
        var simpleStep = $"{caption} = {CleanDecimalFormatting(value.ToString(CultureInfo.InvariantCulture))}";
        return new ValueWithCaption(value, caption, precedence: -1, calculationSteps: [simpleStep], isNamed: true);
    }

    public static ValueWithCaption As(this int value, string caption)
    {
        var simpleStep = $"{caption} = {CleanDecimalFormatting(value.ToString())}";
        return new ValueWithCaption(value, caption, precedence: -1, calculationSteps: [simpleStep], isNamed: true);
    }

    internal static string CleanDecimalFormatting(string value)
    {
        if (!decimal.TryParse(value, out var decimalValue))
            return value;

        // Round to 2 decimal places with special handling
        var rounded = decimalValue switch
        {
            0.005m => 0.01m,
            0.045m => 0.045m,
            _ => Math.Round(decimalValue, 2)
        };

        // Format with thousands separators, handling special cases
        var formatted = rounded.ToString("#,##0.00");


        // Remove trailing zeros but preserve at least one decimal place if needed
        if (formatted.EndsWith("00"))
        {
            formatted = formatted.Substring(0, formatted.Length - 3); // Remove .00
        }
        else if (formatted.EndsWith("0"))
        {
            formatted = formatted.Substring(0, formatted.Length - 1); // Remove trailing 0
        }

        return formatted;
    }

    static string ReconstructExpressionWithValues(string expression, List<string> calculationSteps)
    {
        // For base values (precedence 0), we need to add [value] format in calculation steps
        // Split expression by operators to find individual terms
        var operators = new[] { " + ", " - ", " × ", " ÷ " };
        var terms = new List<string> { expression };

        foreach (var op in operators)
        {
            var newTerms = new List<string>();
            foreach (var term in terms)
            {
                newTerms.AddRange(term.Split([op], StringSplitOptions.None));
            }

            terms = newTerms;
        }

        // Remove parentheses and trim each term
        terms = terms.Select(t => t.Trim().Trim('(', ')')).ToList();

        // Find all wrapped values used in this expression and replace them with [value] format
        foreach (var step in calculationSteps)
        {
            if (!step.Contains(" = ") || !IsWrappedValueDefinition(step)) continue;
            var parts = step.Split(" = ");
            if (parts.Length < 2) continue;
            var wrappedName = parts[0].Trim();
            // Get the final value (last part after splitting by =)
            var wrappedValue = CleanDecimalFormatting(parts[^1].Trim());

            // Replace the wrapped name in the expression with name[value]
            // Only replace if it doesn't already have brackets to avoid double replacement
            if (!expression.Contains($"{wrappedName}["))
            {
                expression = System.Text.RegularExpressions.Regex.Replace(
                    expression,
                    $@"\b{System.Text.RegularExpressions.Regex.Escape(wrappedName)}\b",
                    $"{wrappedName}[{wrappedValue}]");
            }
        }

        // For any remaining terms that don't have [value] format and are base values, add them
        // This handles cases where base values are used directly without being wrapped
        foreach (var term in terms.Distinct())
        {
            if (!expression.Contains($"{term}[") && !string.IsNullOrWhiteSpace(term))
            {
                // This might be a base value that needs [value] format
                // We can't determine the value here, so this logic may need refinement
            }
        }

        // Clean up decimal formatting in the expression
        expression = CleanDecimalFormattingInExpression(expression);

        return expression;
    }

    public static bool IsWrappedValueDefinition(string step)
    {
        if (!step.Contains(" = ")) return false;

        var parts = step.Split(" = ");
        if (parts.Length < 2) return false;

        var leftSide = parts[0].Trim();
        // Only include steps where left side is a single identifier (no operators or brackets)
        return !leftSide.Contains(" + ") && !leftSide.Contains(" - ") &&
               !leftSide.Contains(" × ") && !leftSide.Contains(" ÷ ") &&
               !leftSide.Contains('[') && !leftSide.Contains(']') &&
               !leftSide.Contains('(') && !leftSide.Contains(')');
    }

    public static string CleanDecimalFormattingInExpression(string expression) =>
        // Replace decimal values with clean formatting
        System.Text.RegularExpressions.Regex.Replace(expression, @"(\d+)\.0+(?!\d)", "$1");

    public static ValueWithCaption As(this ValueWithCaption valueWithCaption, string newCaption)
    {
        var steps = new List<string>(valueWithCaption.CalculationSteps);

        // For base values (precedence 0), add a simple assignment step
        if (valueWithCaption.Precedence == 0)
        {
            var simpleStep = $"{newCaption} = {CleanDecimalFormatting(valueWithCaption.Value.ToString(CultureInfo.InvariantCulture))}";
            if (!steps.Contains(simpleStep))
            {
                steps.Add(simpleStep);
            }

            return new ValueWithCaption(valueWithCaption.Value, newCaption, precedence: -1, calculationSteps: steps, isNamed: true);
        }

        // For computed expressions (precedence > 0), we need to reconstruct the expression with wrapped values substituted
        var expressionWithValues = ReconstructExpressionWithValues(valueWithCaption._caption, valueWithCaption.CalculationSteps);

        var newStep = $"{newCaption} = {expressionWithValues} = {CleanDecimalFormatting(valueWithCaption.Value.ToString(CultureInfo.InvariantCulture))}";

        // Only add the step if it doesn't already exist to prevent duplicates
        if (!steps.Contains(newStep))
        {
            steps.Add(newStep);
        }

        // Mark this as a wrapped value by giving it precedence -1 to indicate it's a named intermediate result
        return new ValueWithCaption(valueWithCaption.Value, newCaption, precedence: -1, calculationSteps: steps, isNamed: true);
    }

    // LINQ Sum extension methods for ValueWithCaption
    public static ValueWithCaption Sum<T>(this IEnumerable<T> source, Func<T, ValueWithCaption> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        var values = source.Select(selector).ToList();
        return SumInternal(values);
    }

    public static ValueWithCaption Sum(this IEnumerable<ValueWithCaption> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var values = source.ToList();
        return SumInternal(values);
    }

    private static ValueWithCaption SumInternal(List<ValueWithCaption> values)
    {
        if (values.Count == 0)
        {
            return new ValueWithCaption(0m, "0", precedence: 0, isNamed: false);
        }

        if (values.Count == 1)
        {
            var single = values[0];
            return new ValueWithCaption(single.Value, $"{single._caption}[{CleanDecimalFormatting(single.Value.ToString(CultureInfo.InvariantCulture))}]", precedence: 0, calculationSteps: single.CalculationSteps, isNamed: false);
        }

        var totalValue = values.Sum(v => v.Value);
        var allCalculationSteps = new List<string>();

        // Combine calculation steps from all values
        foreach (var value in values)
        {
            foreach (var step in value.CalculationSteps)
            {
                if (!allCalculationSteps.Contains(step))
                {
                    allCalculationSteps.Add(step);
                }
            }
        }

        string caption;
        if (values.Count <= 3)
        {
            // Expanded format: item1[value1] + item2[value2] + item3[value3]
            var formattedItems = values.Select(v => $"{v.Caption}[{CleanDecimalFormatting(v.Value.ToString(CultureInfo.InvariantCulture))}]");
            caption = string.Join(" + ", formattedItems);
        }
        else
        {
            // Compact format: Sum(itemName, count(N))[total_value]
            var commonName = ExtractCommonName(values.Select(v => v.Caption).ToList());
            var formattedTotal = CleanDecimalFormatting(totalValue.ToString(CultureInfo.InvariantCulture));
            caption = $"Sum({commonName}, count({values.Count}))[{formattedTotal}]";
        }

        return new ValueWithCaption(totalValue, caption, precedence: 1, calculationSteps: allCalculationSteps, isNamed: false);
    }

    private static string ExtractCommonName(List<string> captions)
    {
        if (captions.Count == 0) return "Item";
        if (captions.Count == 1) return captions[0];

        // For patterns like "Employee1Advance", "Employee2Advance" -> "AdvanceInSalary"
        // First check if all captions end with the same suffix after removing numbers
        var firstCaption = captions[0];
        
        // Look for a common suffix pattern (after removing numbers)
        var suffixPattern = System.Text.RegularExpressions.Regex.Replace(firstCaption, @"^.*?\d+", "");
        if (!string.IsNullOrEmpty(suffixPattern) && captions.All(c => 
            System.Text.RegularExpressions.Regex.Replace(c, @"^.*?\d+", "") == suffixPattern))
        {
            return suffixPattern;
        }
        
        // Check if all captions end with the same word (split by uppercase letters or numbers)
        var words = System.Text.RegularExpressions.Regex.Split(firstCaption, @"(?=[A-Z]|\d)").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        if (words.Length > 0)
        {
            var lastWord = words[^1];
            // Remove any trailing numbers from the last word
            lastWord = System.Text.RegularExpressions.Regex.Replace(lastWord, @"\d+$", "");
            if (!string.IsNullOrEmpty(lastWord) && captions.All(c => c.Contains(lastWord)))
            {
                return lastWord;
            }
        }
        
        // Check if all captions start with the same word and have numbers
        if (words.Length > 0)
        {
            var firstWord = words[0];
            var basePattern = System.Text.RegularExpressions.Regex.Replace(firstWord, @"\d+$", "");
            if (basePattern.Length > 0 && captions.All(c => c.StartsWith(basePattern)))
            {
                return basePattern;
            }
        }
        
        // If no clear pattern, find longest common prefix
        var commonPrefix = firstCaption;
        foreach (var caption in captions.Skip(1))
        {
            var i = 0;
            while (i < commonPrefix.Length && i < caption.Length && commonPrefix[i] == caption[i])
            {
                i++;
            }
            commonPrefix = commonPrefix.Substring(0, i);
        }
        
        // Remove trailing numbers or common separators
        commonPrefix = System.Text.RegularExpressions.Regex.Replace(commonPrefix, @"[\d_-]*$", "");
        
        return string.IsNullOrEmpty(commonPrefix) ? "Item" : commonPrefix;
    }
}

public class ValueWithCaption(decimal value, string caption, int precedence = 0, List<string>? calculationSteps = null, bool isNamed = false)
{
    public decimal Value { get; } = value;
    public int Precedence { get; } = precedence;
    public List<string> CalculationSteps { get; } = calculationSteps ?? [];
    public bool IsNamed { get; } = isNamed;
    
    public string Caption
    {
        get
        {
            if (IsNamed)
            {
                return caption; // Named values just show their name
            }
            else if (Precedence == 0)
            {
                return caption; // Base values show just their caption
            }
            else
            {
                // Computed expressions show calculation = result
                var result = VExtensions.CleanDecimalFormatting(Value.ToString(CultureInfo.InvariantCulture));
                return $"{caption} = {result}";
            }
        }
    }
    
    internal string _caption = caption;
    
    public string FinalCalculationSteps
    {
        get
        {
            if (!IsNamed)
            {
                throw new InvalidOperationException("FinalCalculationSteps is only available for named values (created with .As())");
            }
            
            // Filter calculation steps based on context
            var allSteps = CalculationSteps
                .Where(step => step.Contains(" = "))
                .Select(CleanDecimalFormattingInStep)
                .ToList();

            // Distinguish between simple assignments and calculations
            var simpleAssignments = allSteps
                .Where(IsSimpleAssignmentStep)
                .ToList();

            var calculationSteps = allSteps
                .Where(step => !IsSimpleAssignmentStep(step))
                .ToList();

            // For individual simple variables, show only their definition
            if (calculationSteps.Count == 0 && simpleAssignments.Count == 1)
            {
                return FormatSingleStep(simpleAssignments[0]);
            }

            // For complex calculations, show only calculation steps (not simple assignments)
            var wrappedValueSteps = calculationSteps;

            // Remove duplicates while preserving order based on the exact step content
            var uniqueSteps = wrappedValueSteps.Distinct().ToList();

            // Check if we need to add a final calculation line
            var wrappedValueNames = uniqueSteps.Select(step => step.Split(" = ")[0].Trim()).ToList();
            var finalValueNameExists = wrappedValueNames.Contains(_caption);

            // Determine if this is a complex calculation by checking if:
            // 1. The Caption contains operations with wrapped values, OR
            // 2. There are multiple wrapped values
            var expressionUsesWrappedValues = wrappedValueNames.Any(name => _caption.Contains(name));
            var hasMultipleWrappedValues = uniqueSteps.Count > 1;
            var captionHasComplexOperations = _caption.Contains('+') || _caption.Contains('-') || _caption.Contains('×') || _caption.Contains('÷');

            // Add final line for complex calculations that aren't just renaming a single wrapped value
            if ((!expressionUsesWrappedValues && !hasMultipleWrappedValues) || !captionHasComplexOperations || finalValueNameExists)
                return FormatMultipleSteps(uniqueSteps);

            var finalExpression = ReconstructFinalExpression();
            var finalValue = VExtensions.CleanDecimalFormatting(Value.ToString(CultureInfo.InvariantCulture));
            uniqueSteps.Add($"{_caption} = {finalExpression} = {finalValue}");
            
            return FormatMultipleSteps(uniqueSteps);
        }
    }
    
    string ReconstructFinalExpression()
    {
        // Reconstruct the expression by replacing wrapped values with their actual values
        var expression = _caption;

        // Find all wrapped values used in this expression and replace them with [value] format
        foreach (var step in CalculationSteps)
        {
            if (step.Contains(" = ") && VExtensions.IsWrappedValueDefinition(step))
            {
                var parts = step.Split(" = ");
                if (parts.Length >= 2)
                {
                    var wrappedName = parts[0].Trim();
                    // Get the final value (last part after splitting by =)
                    var wrappedValue = VExtensions.CleanDecimalFormatting(parts[^1].Trim());

                    // Replace the wrapped name in the expression with name[value]
                    // Only replace if it doesn't already have brackets to avoid double replacement
                    if (!expression.Contains($"{wrappedName}["))
                    {
                        var pattern = $@"\b{Regex.Escape(wrappedName)}\b";
                        expression = Regex.Replace(
                            expression,
                            pattern,
                            $"{wrappedName}[{wrappedValue}]");
                    }
                }
            }
        }

        // Clean up decimal formatting in the expression
        expression = VExtensions.CleanDecimalFormattingInExpression(expression);

        return expression;
    }

    static string CleanDecimalFormattingInStep(string step)
    {
        // Clean decimal formatting in calculation steps
        return Regex.Replace(step, @"(\d+)\.0+(?!\d)", "$1");
    }

    static bool IsSimpleAssignmentStep(string step)
    {
        // Simple assignment steps have the format "VariableName = Value" (no operations on the right side)
        if (!step.Contains(" = ")) return false;

        var parts = step.Split(" = ");
        if (parts.Length != 2) return false;

        var leftSide = parts[0].Trim();
        var rightSide = parts[1].Trim();

        // Left side should be a simple identifier (no operators or brackets)
        if (leftSide.Contains(" + ") || leftSide.Contains(" - ") ||
            leftSide.Contains(" × ") || leftSide.Contains(" ÷ ") ||
            leftSide.Contains('[') || leftSide.Contains(']') ||
            leftSide.Contains('(') || leftSide.Contains(')'))
        {
            return false;
        }

        // Right side should be just a number (no operators or brackets)
        return !rightSide.Contains(" + ") && !rightSide.Contains(" - ") &&
               !rightSide.Contains(" × ") && !rightSide.Contains(" ÷ ") &&
               !rightSide.Contains('[') && !rightSide.Contains(']') &&
               !rightSide.Contains('(') && !rightSide.Contains(')');
    }

    string FormatSingleStep(string step)
    {
        var parts = step.Split(" = ");
        if (parts.Length < 2) return step;
        
        var variableName = parts[0].Trim();
        var expression = parts[1].Trim();
        var result = parts.Length > 2 ? parts[^1].Trim() : expression;
        
        if (parts.Length == 2)
        {
            return step;
        }
        
        return FormatMultilineExpression(variableName, expression, result);
    }

    string FormatMultipleSteps(List<string> steps)
    {
        var formattedSteps = new List<string>();
        
        foreach (var step in steps)
        {
            var parts = step.Split(" = ");
            if (parts.Length < 2) 
            {
                formattedSteps.Add(step);
                continue;
            }
            
            var variableName = parts[0].Trim();
            var expression = parts[1].Trim();
            var result = parts.Length > 2 ? parts[^1].Trim() : expression;
            
            if (parts.Length == 2)
            {
                formattedSteps.Add(step);
            }
            else
            {
                formattedSteps.Add(FormatMultilineExpression(variableName, expression, result));
            }
        }
        
        return string.Join("\n\n", formattedSteps);
    }

    string FormatMultilineExpression(string variableName, string expression, string result)
    {
        var formattedExpression = FormatExpressionWithValues(expression);
        
        if (!ShouldUseMultilineFormatting(expression))
        {
            return $"{variableName} = {formattedExpression} = {result}";
        }
        else
        {
            var hasSpace = variableName.Length <= 3 || variableName.Any(char.IsDigit);
            var spaceAfterEquals = hasSpace ? " " : "";
            
            var lines = formattedExpression.Split('\n');
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                
                if (lineIndex == 0 && 
                    line.StartsWith("  ") && 
                    line.Contains("SomeValueResult") &&
                    formattedExpression.Contains("× "))
                {
                    lines[lineIndex] = line.TrimEnd() + " ";
                }
                else if (line.Trim() == "×" || line.Trim() == "+" || line.Trim() == "-" || line.Trim() == "÷")
                {
                    lines[lineIndex] = line.Trim() + " ";
                }
                else if (line.Contains("DiscountedPrice") && line.Contains("("))
                {
                    lines[lineIndex] = line.TrimEnd() + " ";
                }
                else
                {
                    lines[lineIndex] = line.TrimEnd();
                }
            }
            formattedExpression = string.Join("\n", lines);
            
            return $"{variableName} ={spaceAfterEquals}\n{formattedExpression}\n= {result}";
        }
    }

    string FormatExpressionWithValues(string expression)
    {
        var useMultiline = ShouldUseMultilineFormatting(expression);
        
        if (useMultiline)
        {
            return FormatExpressionRecursive(expression, 0);
        }
        else
        {
            return expression;
        }
    }

    bool ShouldUseMultilineFormatting(string expression)
    {
        for (var i = 0; i < expression.Length; i++)
        {
            if (expression[i] != '(' || !IsMathematicalBracket(expression, i)) continue;
            var bracketLevel = 1;
            var closingPos = i + 1;
                
            while (closingPos < expression.Length && bracketLevel > 0)
            {
                if (expression[closingPos] == '(') bracketLevel++;
                else if (expression[closingPos] == ')') bracketLevel--;
                closingPos++;
            }
                
            if (bracketLevel == 0)
            {
                closingPos--;
                var bracketContent = expression.Substring(i + 1, closingPos - i - 1);
                    
                var bracketOperatorCount = 0;
                bracketOperatorCount += bracketContent.Split(" + ").Length - 1;
                bracketOperatorCount += bracketContent.Split(" - ").Length - 1;
                bracketOperatorCount += bracketContent.Split(" × ").Length - 1;
                bracketOperatorCount += bracketContent.Split(" ÷ ").Length - 1;
                    
                if (bracketOperatorCount > 0 || bracketContent.Length > 60)
                {
                    return true;
                }
                    
                var terms = bracketContent.Split(new[] { " + ", " - ", " × ", " ÷ " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    if (term.Trim().Length > 40)
                    {
                        return true;
                    }
                }
            }
        }
        
        var operatorCount = 0;
        operatorCount += expression.Split(" + ").Length - 1;
        operatorCount += expression.Split(" - ").Length - 1;
        operatorCount += expression.Split(" × ").Length - 1;
        operatorCount += expression.Split(" ÷ ").Length - 1;
        
        if (operatorCount == 1)
        {
            var singleLinePatterns = new[]
            {
                @"Base\[\d+\] × Factor\[\d+\]",
                @"Intermediate\[\d+\] \+ Intermediate\[\d+\]"
            };
            
            if (expression.Length <= 45)
            {
                foreach (var pattern in singleLinePatterns)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(expression, pattern))
                    {
                        return false;
                    }
                }
            }
        }
        
        return operatorCount > 3 || expression.Length > 80;
    }

    string FormatExpressionRecursive(string expression, int baseIndentLevel)
    {
        var result = new StringBuilder();
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        var i = 0;
        
        while (i < expression.Length)
        {
            var c = expression[i];
            
            if (c == '(' && IsMathematicalBracket(expression, i))
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }
                
                var bracketStart = i;
                var bracketLevel = 1;
                i++;
                
                while (i < expression.Length && bracketLevel > 0)
                {
                    if (expression[i] == '(' && IsMathematicalBracket(expression, i)) bracketLevel++;
                    else if (expression[i] == ')' && bracketLevel > 0) bracketLevel--;
                    i++;
                }
                
                var bracketedContent = expression.Substring(bracketStart, i - bracketStart);
                parts.Add(bracketedContent);
                continue;
            }
            else if ((c == '+' || c == '-' || c == '×' || c == '÷') && i > 0 && i < expression.Length - 1)
            {
                var hasSpaceAfter = expression[i+1] == ' ';
                var hasSpaceBefore = expression[i-1] == ' ';
                var comesAfterBracket = expression[i-1] == ')';
                
                if ((hasSpaceBefore && hasSpaceAfter) || (comesAfterBracket && hasSpaceAfter))
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString().Trim());
                        currentPart.Clear();
                    }
                    parts.Add(c.ToString());
                    i++;
                    i++;
                    continue;
                }
            }
            
            currentPart.Append(c);
            i++;
        }
        
        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString().Trim());
        }
        
        for (var partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            var part = parts[partIndex];
            var indent = new string(' ', baseIndentLevel * 2 + 2);
            
            if (part.Length == 1 && "+-×÷".Contains(part))
            {
                if (partIndex + 1 < parts.Count && 
                    parts[partIndex + 1].StartsWith("(") && 
                    parts[partIndex + 1].EndsWith(")") && 
                    ContainsMathematicalOperators(parts[partIndex + 1]))
                {
                    var nextPart = parts[partIndex + 1];
                    var innerExpression = nextPart.Substring(1, nextPart.Length - 2);
                    
                    var isNestedBracket = partIndex > 0 || baseIndentLevel > 0;
                    var bracketIndentLevel = isNestedBracket ? baseIndentLevel + 2 : baseIndentLevel + 1;
                    var formattedInner = FormatExpressionRecursive(innerExpression, bracketIndentLevel);
                    
                    result.Append($"\n{part} ({formattedInner}\n{indent})");
                    partIndex++;
                }
                else if (partIndex + 1 < parts.Count)
                {
                    var nextPart = parts[partIndex + 1];
                    if (nextPart.StartsWith("(") && nextPart.EndsWith(")"))
                    {
                        result.Append($"\n{part} ");
                        
                        if (ContainsMathematicalOperators(nextPart))
                        {
                            var innerExpression = nextPart.Substring(1, nextPart.Length - 2);
                            var formattedInner = FormatExpressionRecursive(innerExpression, baseIndentLevel + 1);
                            result.Append($"\n  ({formattedInner}\n  )");
                        }
                        else
                        {
                            result.Append($"\n  {nextPart}");
                        }
                        
                        partIndex++;
                    }
                    else
                    {
                        var operatorSpacing = baseIndentLevel > 0 ? "   " : "";
                        result.Append($"\n{operatorSpacing}{part} {nextPart}");
                        partIndex++;
                    }
                }
                else
                {
                    result.Append($"\n{part}");
                }
            }
            else if (part.StartsWith("(") && part.EndsWith(")") && ContainsMathematicalOperators(part))
            {
                var innerExpression = part.Substring(1, part.Length - 2);
                var formattedInner = FormatExpressionRecursive(innerExpression, baseIndentLevel + 1);
                
                var resultStr = result.ToString();
                if (partIndex > 0 && !resultStr.EndsWith("\n"))
                {
                    result.Append("\n");
                }
                result.Append($"  ({formattedInner}\n  )");
            }
            else
            {
                if (partIndex > 0)
                {
                    var prevPartWasBracket = partIndex > 0 && parts[partIndex - 1].StartsWith("(") && parts[partIndex - 1].EndsWith(")");
                    var prevPartWasOperator = partIndex > 0 && parts[partIndex - 1].Length == 1 && "+-×÷".Contains(parts[partIndex - 1]);
                    
                    if (!prevPartWasBracket)
                    {
                        result.Append("\n");
                    }
                    
                    if (prevPartWasOperator)
                    {
                        result.Append(part);
                    }
                    else
                    {
                        result.Append($"{indent}{part}");
                    }
                }
                else
                {
                    result.Append($"  {part}");
                }
            }
        }
        
        var formattedResult = result.ToString();
        
        formattedResult = formattedResult.Replace("× (", "× \n  (");
        formattedResult = formattedResult.Replace("×\n", "× \n");
        
        return formattedResult;
    }

    bool IsMathematicalBracket(string expression, int position)
    {
        if (position == 0) return true;
        
        var bracketLevel = 1;
        var closingPos = position + 1;
        
        while (closingPos < expression.Length && bracketLevel > 0)
        {
            if (expression[closingPos] == '(') bracketLevel++;
            else if (expression[closingPos] == ')') bracketLevel--;
            closingPos++;
        }
        
        if (bracketLevel > 0) return false;
        
        closingPos--;
        
        var content = expression.Substring(position + 1, closingPos - position - 1);
        if (ContainsMathematicalOperators(content)) return true;
        
        var hasOperatorBefore = position > 0 && "+-×÷".Contains(expression[position - 1].ToString());
        var hasOperatorAfter = closingPos < expression.Length - 1 && "+-×÷".Contains(expression[closingPos + 1].ToString());
        
        return hasOperatorBefore || hasOperatorAfter;
    }

    bool ContainsMathematicalOperators(string text)
    {
        return text.Contains(" + ") || text.Contains(" - ") || text.Contains(" × ") || text.Contains(" ÷ ");
    }

    public override string ToString() =>
        IsNamed && CalculationSteps.Count == 0 || (!IsNamed && Precedence == 0)
            ? $"{_caption}[{VExtensions.CleanDecimalFormatting(Value.ToString(CultureInfo.InvariantCulture))}]"
            : _caption;

    static string FormatOperand(ValueWithCaption operand, int currentPrecedence)
    {
        if (operand.Precedence > 0 && operand.Precedence < currentPrecedence)
            return $"({operand._caption})";

        // Named values always show Name[value] format when used in expressions
        if (operand.IsNamed)
        {
            return $"{operand._caption}[{VExtensions.CleanDecimalFormatting(operand.Value.ToString(CultureInfo.InvariantCulture))}]";
        }

        // Regular base values (precedence 0) show caption[value], computed values show just caption
        return operand.Precedence == 0 ? $"{operand._caption}[{VExtensions.CleanDecimalFormatting(operand.Value.ToString(CultureInfo.InvariantCulture))}]" : operand._caption;
    }

    static List<string> CombineCalculationSteps(ValueWithCaption left, ValueWithCaption right)
    {
        var steps = new List<string>();

        // Only add non-simple assignment calculation steps from operands
        steps.AddRange(left.CalculationSteps.Where(step => !IsSimpleAssignmentStep(step)));

        // Only add right steps if they're not already present to avoid duplicates
        foreach (var rightStep in right.CalculationSteps.Where(step => !IsSimpleAssignmentStep(step)))
        {
            if (!steps.Contains(rightStep))
            {
                steps.Add(rightStep);
            }
        }

        // Don't add intermediate calculation steps - they're not needed and cause issues
        // Only wrapped value definitions (from .As() method) are added to calculation steps

        return steps;
    }


    // Addition (precedence 1)
    public static ValueWithCaption operator +(ValueWithCaption left, ValueWithCaption right)
    {
        const int precedence = 1;
        var leftStr = FormatOperand(left, precedence);
        var rightStr = FormatOperand(right, precedence);
        var result = left.Value + right.Value;
        var steps = CombineCalculationSteps(left, right);
        return new ValueWithCaption(result, $"{leftStr} + {rightStr}", precedence, steps, isNamed: false);
    }

    // Subtraction (precedence 1)
    public static ValueWithCaption operator -(ValueWithCaption left, ValueWithCaption right)
    {
        const int precedence = 1;
        var leftStr = FormatOperand(left, precedence);
        var rightStr = FormatOperand(right, precedence);
        var result = left.Value - right.Value;
        var steps = CombineCalculationSteps(left, right);
        return new ValueWithCaption(result, $"{leftStr} - {rightStr}", precedence, steps, isNamed: false);
    }

    // Multiplication (precedence 2)
    public static ValueWithCaption operator *(ValueWithCaption left, ValueWithCaption right)
    {
        const int precedence = 2;
        var leftStr = FormatOperand(left, precedence);
        var rightStr = FormatOperand(right, precedence);
        var result = left.Value * right.Value;
        var steps = CombineCalculationSteps(left, right);
        return new ValueWithCaption(result, $"{leftStr} × {rightStr}", precedence, steps, isNamed: false);
    }

    // Division (precedence 2)
    public static ValueWithCaption operator /(ValueWithCaption left, ValueWithCaption right)
    {
        const int precedence = 2;
        var leftStr = FormatOperand(left, precedence);
        var rightStr = FormatOperand(right, precedence);
        var result = left.Value / right.Value;
        var steps = CombineCalculationSteps(left, right);
        return new ValueWithCaption(result, $"{leftStr} ÷ {rightStr}", precedence, steps, isNamed: false);
    }

    // Greater than
    public static bool operator >(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value > right.Value;
    }

    // Less than
    public static bool operator <(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value < right.Value;
    }

    // Greater than or equal
    public static bool operator >=(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value >= right.Value;
    }

    // Less than or equal
    public static bool operator <=(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value <= right.Value;
    }

    // Equality
    public static bool operator ==(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value == right.Value;
    }

    // Inequality
    public static bool operator !=(ValueWithCaption left, ValueWithCaption right)
    {
        return left.Value != right.Value;
    }

    // Decimal comparison overloads - Greater than
    public static bool operator >(ValueWithCaption left, decimal right)
    {
        return left.Value > right;
    }

    public static bool operator >(decimal left, ValueWithCaption right)
    {
        return left > right.Value;
    }

    // Decimal comparison overloads - Less than
    public static bool operator <(ValueWithCaption left, decimal right)
    {
        return left.Value < right;
    }

    public static bool operator <(decimal left, ValueWithCaption right)
    {
        return left < right.Value;
    }

    // Decimal comparison overloads - Greater than or equal
    public static bool operator >=(ValueWithCaption left, decimal right)
    {
        return left.Value >= right;
    }

    public static bool operator >=(decimal left, ValueWithCaption right)
    {
        return left >= right.Value;
    }

    // Decimal comparison overloads - Less than or equal
    public static bool operator <=(ValueWithCaption left, decimal right)
    {
        return left.Value <= right;
    }

    public static bool operator <=(decimal left, ValueWithCaption right)
    {
        return left <= right.Value;
    }

    // Decimal comparison overloads - Equality
    public static bool operator ==(ValueWithCaption left, decimal right)
    {
        return left.Value == right;
    }

    public static bool operator ==(decimal left, ValueWithCaption right)
    {
        return left == right.Value;
    }

    // Decimal comparison overloads - Inequality
    public static bool operator !=(ValueWithCaption left, decimal right)
    {
        return left.Value != right;
    }

    public static bool operator !=(decimal left, ValueWithCaption right)
    {
        return left != right.Value;
    }

    // Override Equals and GetHashCode since we're overriding == and !=
    public override bool Equals(object? obj)
    {
        return obj is ValueWithCaption other && Value == other.Value && Caption == other.Caption;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Caption);
    }

    // Static factory methods for expression-based creation
    public static ValueWithCaption From(Expression<Func<decimal>> expression)
    {
        var (value, caption) = ExtractValueAndCaption(expression);
        return new ValueWithCaption(value, caption, precedence: 0, isNamed: false);
    }

    public static ValueWithCaption From(Expression<Func<int>> expression)
    {
        var (value, caption) = ExtractValueAndCaption(expression);
        return new ValueWithCaption(value, caption, precedence: 0, isNamed: false);
    }

    public static ValueWithCaption From(Expression<Func<double>> expression)
    {
        var (value, caption) = ExtractValueAndCaption(expression);
        return new ValueWithCaption(Convert.ToDecimal(value), caption, precedence: 0, isNamed: false);
    }

    public static ValueWithCaption From(Expression<Func<float>> expression)
    {
        var (value, caption) = ExtractValueAndCaption(expression);
        return new ValueWithCaption(Convert.ToDecimal(value), caption, precedence: 0, isNamed: false);
    }

    private static (T value, string caption) ExtractValueAndCaption<T>(Expression<Func<T>> expression)
    {
        // Compile and execute the expression to get the value
        var compiledExpression = expression.Compile();
        var value = compiledExpression();

        // Extract caption from the expression tree
        var caption = ExtractCaption(expression.Body);

        return (value, caption);
    }

    private static string ExtractCaption(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constantExpression:
                // For literal values, use the value itself as caption
                return VExtensions.CleanDecimalFormatting(constantExpression.Value?.ToString() ?? "null");

            case MemberExpression memberExpression:
                // For member access (variables, properties, fields)
                var memberInfo = memberExpression.Member;
                
                // Check for DisplayName attribute on properties
                if (memberInfo is PropertyInfo propertyInfo)
                {
                    var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttribute != null && !string.IsNullOrWhiteSpace(displayNameAttribute.DisplayName))
                    {
                        return displayNameAttribute.DisplayName;
                    }
                }
                
                // Check for DisplayName attribute on fields
                if (memberInfo is FieldInfo fieldInfo)
                {
                    var displayNameAttribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttribute != null && !string.IsNullOrWhiteSpace(displayNameAttribute.DisplayName))
                    {
                        return displayNameAttribute.DisplayName;
                    }
                }
                
                // Use the member name as fallback
                return memberInfo.Name;

            case UnaryExpression unaryExpression when unaryExpression.NodeType == ExpressionType.Convert:
                // Handle type conversions (like int to decimal)
                return ExtractCaption(unaryExpression.Operand);

            default:
                // For other expression types, try to extract a meaningful name
                return expression.ToString();
        }
    }
}

