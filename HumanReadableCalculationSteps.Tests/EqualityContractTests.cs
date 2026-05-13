using Xunit;

namespace HumanReadableCalculationSteps.Tests
{
    // Pins down the equality / hash / comparison contract.
    // Contract: equality is by Value only. Caption is display metadata, not identity.
    // operator ==, Equals(object), GetHashCode, and CompareTo must all agree on this.
    public class EqualityContractTests
    {
        [Fact]
        public void Equals_WithSameValueDifferentCaption_ReturnsTrue()
        {
            var a = 10m.As("apples");
            var b = 10m.As("oranges");

            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
        }

        [Fact]
        public void Equals_WithDifferentValueSameCaption_ReturnsFalse()
        {
            var a = 10m.As("x");
            var b = 11m.As("x");

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            var a = 10m.As("x");

            Assert.False(a.Equals(null));
            Assert.False(a.Equals((object?)null));
        }

        [Fact]
        public void Equals_WithUnrelatedType_ReturnsFalse()
        {
            var a = 10m.As("x");

            Assert.False(a.Equals("10"));
            Assert.False(a.Equals(10m));
            Assert.False(a.Equals(new object()));
        }

        [Fact]
        public void OperatorEquals_AndEqualsMethod_Agree()
        {
            // Without this, hash-based collections (Dictionary, HashSet) would
            // diverge from operator == in surprising ways.
            var a = 10m.As("apples");
            var b = 10m.As("oranges");

            Assert.Equal(a == b, a.Equals(b));
            Assert.Equal(a != b, !a.Equals(b));
        }

        [Fact]
        public void GetHashCode_AgreesWithEquals()
        {
            // Contract: a.Equals(b) => a.GetHashCode() == b.GetHashCode()
            var a = 10m.As("apples");
            var b = 10m.As("oranges");

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_IsStableAcrossCalls()
        {
            var a = 42.5m.As("x");

            Assert.Equal(a.GetHashCode(), a.GetHashCode());
        }

        [Fact]
        public void CompareTo_AgreesWithEquals_AtZero()
        {
            // Contract: a.CompareTo(b) == 0  <=>  a.Equals(b)
            var a = 10m.As("apples");
            var b = 10m.As("oranges");

            Assert.Equal(0, a.CompareTo(b));
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void HashSet_TreatsSameValueDifferentCaptionAsSame()
        {
            // Smoke test for the hash-based collection contract.
            var set = new HashSet<ValueWithCaption> { 10m.As("apples"), 10m.As("oranges") };

            Assert.Single(set);
        }
    }
}
