using Xunit;

namespace HumanReadableCalculationSteps.Tests
{
    public class MultilineFormattingTests
    {
        [Fact]
        public void LongExpression_ShouldSplitIntoMultipleLines()
        {
            // Arrange - Create a long expression similar to your example
            var v1 = 0m.As("მორიგეობის ხელფასი");
            var v2 = 2500m.As("ფიქსირებული ხელფასი");
            var v3 = 0m.As("ნორმალიზებული საშვებულებო ანაზღაურება");
            var v4 = 0m.As("ნორმალიზებული ბიულეტენის ანაზღაურება");
            var v5 = 0m.As("0");
            var v6 = 0m.As("კომპანიის დაზღვევის წილი");
            var v7 = 0m.As("ბრუტო 0.000000");
            var v8 = 0m.As("ბრუტო 0.000000");
            var v9 = 0m.As("ბონუსი ფილიალიდან");
            var v10 = 0m.As("ბონუსი: საოპერაციო მოგება");
            var v11 = 0m.As("ერთჯერადი ბონუსი");
            var v12 = 0m.As("ROCC ბონუსი");
            var v13 = 0m.As("რამე არა");
            var v14 = 0m.As("პრემია");
            var pensionRate = 0.02m.As("პენსიის %");

            // Act - Create the long expression
            var result = (v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8 + v9 + v10 + v11 + v12 + v13 + v14) * pensionRate;

            // Assert - Check that the FinalCalculationSteps splits into multiple lines
            var actualOutput = result.FinalCalculationSteps;

            // The output should be split into multiple lines for better readability
            var expectedOutput =
                """
                  (  მორიგეობის ხელფასი[0]
                   + ფიქსირებული ხელფასი[2,500]
                   + ნორმალიზებული საშვებულებო ანაზღაურება[0]
                   + ნორმალიზებული ბიულეტენის ანაზღაურება[0]
                   + 0[0]
                   + კომპანიის დაზღვევის წილი[0]
                   + ბრუტო 0.000000[0]
                   + ბრუტო 0.000000[0]
                   + ბონუსი ფილიალიდან[0]
                   + ბონუსი: საოპერაციო მოგება[0]
                   + ერთჯერადი ბონუსი[0]
                   + ROCC ბონუსი[0]
                   + რამე არა[0]
                   + პრემია[0]
                  )
                × პენსიის %[0.02]
                = 50
                """;

            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}