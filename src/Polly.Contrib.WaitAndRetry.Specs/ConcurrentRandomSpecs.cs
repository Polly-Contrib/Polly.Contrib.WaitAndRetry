using FluentAssertions;
using Xunit;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class ConcurrentRandomSpecs
    {
        [Fact]
        public void GetRandomNumber_ThatIsGreaterThan100AndLessThan1000_RandomNumberReturned()
        {
            // Arrange
            double low = 100;
            double high = 1000;
            var concurrentRandom = new ConcurrentRandom();

            // Act
            double result = concurrentRandom.Uniform(low, high);

            // Assert
            result.Should().BeInRange(low, high);
        }

        [Fact]
        public void GetRandomNumber_WhereLowAndHighAreEqual_LowHighValueReturned()
        {
            // Arrange
            double low = 100;
            double high = 100;
            var concurrentRandom = new ConcurrentRandom();

            // Act
            double result = concurrentRandom.Uniform(low, high);

            // Assert
            result.Should().BeInRange(low, high);
        }

        [Fact]
        public void GetRandomNumber_WithSeedOf2ThatIsGreaterThan100AndLessThan1000_RandomNumberReturned()
        {
            // Arrange
            double low = 100;
            double high = 1000;
            int seed = 2;
            var concurrentRandom = new ConcurrentRandom(seed);

            // Act
            double result = concurrentRandom.Uniform(low, high);

            // Assert
            result.Should().BeInRange(low, high);
        }
    }
}