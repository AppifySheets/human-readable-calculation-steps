using System.ComponentModel;
using Xunit;

namespace HumanReadableCalculationSteps.Tests
{
    public class ExpressionExtensionTests
    {
        [Fact]
        public void LiteralValue_ShouldCreateValueWithLiteralCaption()
        {
            // Act
            var price = ValueWithCaption.From(() => 100m);
            
            // Assert
            Assert.Equal(100m, price.Value);
            Assert.Equal("100", price.FinalCalculationSteps);
        }
        
        [Fact]
        public void LiteralValue_WithDecimals_ShouldCreateValueWithLiteralCaption()
        {
            // Act
            var price = ValueWithCaption.From(() => 123.45m);
            
            // Assert
            Assert.Equal(123.45m, price.Value);
            Assert.Equal("123.45", price.FinalCalculationSteps);  
        }
        
        [Fact]
        public void LiteralValue_Integer_ShouldCreateValueWithLiteralCaption()
        {
            // Act
            var count = ValueWithCaption.From(() => 5);
            
            // Assert
            Assert.Equal(5m, count.Value);
            Assert.Equal("5", count.FinalCalculationSteps);
        }
        
        [Fact]
        public void LiteralValue_Double_ShouldCreateValueWithLiteralCaption()
        {
            // Act
            var rate = ValueWithCaption.From(() => 0.18);
            
            // Assert
            Assert.Equal(0.18m, rate.Value);
            Assert.Equal("0.18", rate.FinalCalculationSteps);
        }
        
        [Fact]
        public void LiteralValue_Float_ShouldCreateValueWithLiteralCaption()
        {
            // Act
            var factor = ValueWithCaption.From(() => 2.5f);
            
            // Assert
            Assert.Equal(2.5m, factor.Value);
            Assert.Equal("2.5", factor.FinalCalculationSteps);
        }
        
        [Fact]
        public void VariableReference_ShouldCreateValueWithVariableName()
        {
            // Arrange
            var basePrice = 100m;

            // Act
            var price = ValueWithCaption.From(() => basePrice);

            // Assert
            // FinalCalculationSteps must include the value alongside the
            // caption — From(...) is symmetric with .As() in this respect.
            Assert.Equal(100m, price.Value);
            Assert.Equal("basePrice = 100", price.FinalCalculationSteps);
        }

        [Fact]
        public void VariableReference_DifferentTypes_ShouldCreateValueWithVariableName()
        {
            // Arrange
            var intValue = 42;
            var doubleValue = 3.14;
            var floatValue = 1.5f;

            // Act
            var intResult = ValueWithCaption.From(() => intValue);
            var doubleResult = ValueWithCaption.From(() => doubleValue);
            var floatResult = ValueWithCaption.From(() => floatValue);

            // Assert
            // Previously these worked by accident because the variable
            // names ended with "Value" (a suffix-based hack that's now
            // removed). Behavior is now driven by source kind: any
            // MemberExpression renders as "caption = value".
            Assert.Equal(42m, intResult.Value);
            Assert.Equal("intValue = 42", intResult.FinalCalculationSteps);

            Assert.Equal(3.14m, doubleResult.Value);
            Assert.Equal("doubleValue = 3.14", doubleResult.FinalCalculationSteps);

            Assert.Equal(1.5m, floatResult.Value);
            Assert.Equal("floatValue = 1.5", floatResult.FinalCalculationSteps);
        }

        [Fact]
        public void PropertyReference_WithoutDisplayName_ShouldUsePropertyName()
        {
            // Arrange
            var product = new TestProduct { Price = 99.99m };

            // Act
            var price = ValueWithCaption.From(() => product.Price);

            // Assert
            Assert.Equal(99.99m, price.Value);
            Assert.Equal("Price = 99.99", price.FinalCalculationSteps);
        }

        [Fact]
        public void PropertyReference_WithDisplayName_ShouldUseDisplayName()
        {
            // Arrange
            var product = new TestProductWithDisplayName { Cost = 150.00m };

            // Act
            var cost = ValueWithCaption.From(() => product.Cost);

            // Assert
            Assert.Equal(150.00m, cost.Value);
            Assert.Equal("Product Cost = 150", cost.FinalCalculationSteps);
        }

        [Fact]
        public void PropertyReference_WithEmptyDisplayName_ShouldUsePropertyName()
        {
            // Arrange
            var product = new TestProductWithEmptyDisplayName { Price = 75.50m };

            // Act
            var price = ValueWithCaption.From(() => product.Price);

            // Assert
            Assert.Equal(75.50m, price.Value);
            Assert.Equal("Price = 75.5", price.FinalCalculationSteps);
        }
        
        [Fact]
        public void ExpressionBasedValues_ShouldWorkWithArithmeticOperations()
        {
            // Arrange
            var basePrice = ValueWithCaption.From(() => 100m);
            var taxRate = ValueWithCaption.From(() => 0.18m);
            
            // Act
            var tax = basePrice * taxRate;
            
            // Assert
            Assert.Equal(18m, tax.Value);
            Assert.Equal("100[100] × 0.18[0.18] = 18", tax.FinalCalculationSteps);
        }
        
        [Fact]
        public void ExpressionBasedValues_WithVariables_ShouldWorkWithArithmeticOperations()
        {
            // Arrange
            var price = 100m;
            var rate = 0.18m;
            
            var basePrice = ValueWithCaption.From(() => price);
            var taxRate = ValueWithCaption.From(() => rate);
            
            // Act
            var tax = basePrice * taxRate;
            
            // Assert
            Assert.Equal(18m, tax.Value);
            Assert.Equal("price[100] × rate[0.18] = 18", tax.FinalCalculationSteps);
        }
        
        [Fact]
        public void ExpressionBasedValues_ShouldWorkWithComplexCalculations()
        {
            // Arrange
            var base1 = 50m;
            var base2 = 30m;
            var multiplier = 2m;
            
            var price1 = ValueWithCaption.From(() => base1);
            var price2 = ValueWithCaption.From(() => base2);
            var factor = ValueWithCaption.From(() => multiplier);
            
            // Act
            var result = (price1 + price2) * factor;
            
            // Assert
            Assert.Equal(160m, result.Value);
            Assert.Equal("(base1[50] + base2[30]) × multiplier[2] = 160", result.FinalCalculationSteps);
        }
        
        [Fact]
        public void ExpressionBasedValues_ShouldWorkWithWrapping()
        {
            // Arrange
            var originalPrice = 100m;
            var discount = 15m;
            
            var basePrice = ValueWithCaption.From(() => originalPrice);
            var discountAmount = ValueWithCaption.From(() => discount);
            
            // Act
            var discountedPrice = (basePrice - discountAmount).As("DiscountedPrice");
            var taxRate = ValueWithCaption.From(() => 0.18m);
            var finalPrice = discountedPrice + discountedPrice * taxRate;
            
            // Assert
            Assert.Equal(85m, discountedPrice.Value);
            Assert.Equal("DiscountedPrice = originalPrice[100] - discount[15] = 85", discountedPrice.FinalCalculationSteps);
            
            Assert.Equal(100.3m, finalPrice.Value);
            Assert.Equal("DiscountedPrice[85] + DiscountedPrice[85] × 0.18[0.18] = 100.3", finalPrice.FinalCalculationSteps);
        }
        
        [Fact]
        public void ExpressionBasedValues_ShouldHaveCorrectToStringBehavior()
        {
            // Arrange
            var price = 100m;
            
            // Act
            var basePrice = ValueWithCaption.From(() => price);
            
            // Assert
            Assert.Equal("price[100]", basePrice.ToString());
        }
        
        [Fact]
        public void ExpressionBasedValues_WithNamedValues_ShouldShowCorrectFormat()
        {
            // Arrange
            var price = 100m;
            
            var basePrice = ValueWithCaption.From(() => price);
            var namedPrice = basePrice.As("BasePrice");
            
            // Act & Assert
            Assert.Equal("price[100]", basePrice.ToString());
            Assert.Equal("BasePrice", namedPrice.ToString());
        }
    }
    
    // Test helper classes
    public class TestProduct
    {
        public decimal Price { get; set; }
    }
    
    public class TestProductWithDisplayName
    {
        [DisplayName("Product Cost")]
        public decimal Cost { get; set; }
    }
    
    public class TestProductWithEmptyDisplayName
    {
        [DisplayName("")]
        public decimal Price { get; set; }
    }
}