using Xunit;

namespace HumanReadableCalculationSteps.Tests
{
    public class ComparableTests
    {
        [Fact]
        public void CompareTo_WithOtherValueWithCaption_ShouldCompareByValue()
        {
            var value1 = 10m.As("value1");
            var value2 = 20m.As("value2");
            var value3 = 10m.As("value3");
            
            Assert.True(value1.CompareTo(value2) < 0); // 10 < 20
            Assert.True(value2.CompareTo(value1) > 0); // 20 > 10
            Assert.Equal(0, value1.CompareTo(value3)); // 10 == 10
        }
        
        [Fact]
        public void CompareTo_WithNull_ShouldReturnPositive()
        {
            var value = 10m.As("value");
            
            Assert.True(value.CompareTo(null) > 0);
            Assert.True(value.CompareTo((ValueWithCaption?)null) > 0);
        }
        
        [Fact]
        public void CompareTo_WithObject_ShouldCompareCorrectly()
        {
            var value1 = 10m.As("value1");
            var value2 = 20m.As("value2");
            object objValue2 = value2;
            
            Assert.True(value1.CompareTo(objValue2) < 0);
        }
        
        [Fact]
        public void CompareTo_WithIncompatibleType_ShouldThrowArgumentException()
        {
            var value = 10m.As("value");
            var incompatibleObject = "string";
            
            Assert.Throws<ArgumentException>(() => value.CompareTo(incompatibleObject));
        }
        
        [Fact]
        public void Sort_ListOfValueWithCaption_ShouldSortByValue()
        {
            var values = new List<ValueWithCaption>
            {
                30m.As("thirty"),
                10m.As("ten"),
                20m.As("twenty"),
                5m.As("five")
            };
            
            values.Sort();
            
            Assert.Equal(5m, values[0].Value);
            Assert.Equal(10m, values[1].Value);
            Assert.Equal(20m, values[2].Value);
            Assert.Equal(30m, values[3].Value);
        }
        
        [Fact]
        public void Sort_WithNulls_ShouldPlaceNullsFirst()
        {
            var values = new List<ValueWithCaption?>
            {
                20m.As("twenty"),
                null,
                10m.As("ten"),
                null,
                30m.As("thirty")
            };
            
            values.Sort();
            
            Assert.Null(values[0]);
            Assert.Null(values[1]);
            Assert.Equal(10m, values[2]?.Value);
            Assert.Equal(20m, values[3]?.Value);
            Assert.Equal(30m, values[4]?.Value);
        }
        
        [Fact]
        public void OrderBy_UsingLinq_ShouldWork()
        {
            var values = new[]
            {
                30m.As("thirty"),
                10m.As("ten"),
                20m.As("twenty"),
                5m.As("five")
            };
            
            var sorted = values.OrderBy(v => v).ToArray();
            
            Assert.Equal(5m, sorted[0].Value);
            Assert.Equal(10m, sorted[1].Value);
            Assert.Equal(20m, sorted[2].Value);
            Assert.Equal(30m, sorted[3].Value);
        }
        
        [Fact]
        public void OrderByDescending_UsingLinq_ShouldWork()
        {
            var values = new[]
            {
                30m.As("thirty"),
                10m.As("ten"),
                20m.As("twenty"),
                5m.As("five")
            };
            
            var sorted = values.OrderByDescending(v => v).ToArray();
            
            Assert.Equal(30m, sorted[0].Value);
            Assert.Equal(20m, sorted[1].Value);
            Assert.Equal(10m, sorted[2].Value);
            Assert.Equal(5m, sorted[3].Value);
        }
        
        [Fact]
        public void CompareTo_WithComputedValues_ShouldCompareByResultValue()
        {
            var a = 10m.As("a");
            var b = 5m.As("b");
            var sum = (a + b).As("sum"); // 15
            var product = (a * b).As("product"); // 50
            
            Assert.True(sum.CompareTo(product) < 0); // 15 < 50
            Assert.True(product.CompareTo(sum) > 0); // 50 > 15
        }
        
        [Fact]
        public void CompareTo_WithNegativeValues_ShouldCompareCorrectly()
        {
            var positive = 10m.As("positive");
            var negative = (-5m).As("negative");
            var zero = 0m.As("zero");
            
            Assert.True(negative.CompareTo(positive) < 0); // -5 < 10
            Assert.True(positive.CompareTo(negative) > 0); // 10 > -5
            Assert.True(negative.CompareTo(zero) < 0); // -5 < 0
            Assert.True(zero.CompareTo(negative) > 0); // 0 > -5
        }
        
        [Fact]
        public void CompareTo_WithEqualValuesButDifferentCaptions_ShouldReturnZero()
        {
            var value1 = 10m.As("first");
            var value2 = 10m.As("second");
            
            // CompareTo should only compare values, not captions
            Assert.Equal(0, value1.CompareTo(value2));
        }
        
        [Fact]
        public void Array_Sort_ShouldWork()
        {
            var values = new[]
            {
                30m.As("thirty"),
                10m.As("ten"),
                20m.As("twenty"),
                5m.As("five")
            };
            
            Array.Sort(values);
            
            Assert.Equal(5m, values[0].Value);
            Assert.Equal(10m, values[1].Value);
            Assert.Equal(20m, values[2].Value);
            Assert.Equal(30m, values[3].Value);
        }
        
        [Fact]
        public void Min_Max_Operations_ShouldWork()
        {
            var values = new[]
            {
                30m.As("thirty"),
                10m.As("ten"),
                20m.As("twenty"),
                5m.As("five")
            };
            
            var min = values.Min();
            var max = values.Max();
            
            Assert.Equal(5m, min?.Value);
            Assert.Equal(30m, max?.Value);
        }
    }
}