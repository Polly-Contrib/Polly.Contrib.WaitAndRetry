using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class DecorrelatedJitterBackoffV2Specs
    {
        private readonly ITestOutputHelper testOutputHelper;

        public DecorrelatedJitterBackoffV2Specs(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Backoff_WithMeanFirstDelayLessThanZero_ThrowsException()
        {
            // Arrange
            var medianFirstDelay = new TimeSpan(-1);
            const int retryCount = 3;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            Action act = () => Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay, retryCount, seed, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("medianFirstRetryDelay");
        }

        [Fact]
        public void Backoff_WithRetryCountLessThanZero_ThrowsException()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(1);
            const int retryCount = -1;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            Action act = () => Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay, retryCount, seed, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("retryCount");
        }

        [Fact]
        public void Backoff_WithRetryEqualToZero_ResultIsEmpty()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(2);
            const int retryCount = 0;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay, retryCount, seed, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Backoff_WithFastFirstEqualToTrue_ResultIsZero()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(2);
            const int retryCount = 10;
            const bool fastFirst = true;
            const int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay, retryCount, seed, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result = result.ToList();
            result.Should().HaveCount(retryCount);

            bool first = true;
            int t = 0;
            foreach (TimeSpan timeSpan in result)
            {
                if (first)
                {
                    timeSpan.Should().Be(TimeSpan.FromMilliseconds(0));
                    first = false;
                }
                else
                {
                    t++;
                    AssertOnRetryDelayForTry(t, timeSpan, medianFirstDelay);
                }
            }
        }
        
        public static IEnumerable<object[]> SeedRange => Enumerable.Range(0, 1000).Select(o => new object[] {o}).ToArray();

        [Theory]
        [MemberData(nameof(SeedRange))]
        public void Backoff_ResultIsInRange(int seed)
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(3);
            const int retryCount = 6;
            const bool fastFirst = false;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay, retryCount, seed, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result = result.ToList();
            result.Should().HaveCount(retryCount);

            int t = 0;
            foreach (TimeSpan timeSpan in result)
            {
                t++;
                AssertOnRetryDelayForTry(t, timeSpan, medianFirstDelay);
            }
        }

        private void AssertOnRetryDelayForTry(int t, TimeSpan calculatedDelay, TimeSpan medianFirstDelay)
        {
            /*testOutputHelper.WriteLine($"Try {t}, delay: {calculatedDelay.TotalSeconds} seconds; given median first delay {medianFirstDelay.TotalSeconds} seconds.");*/

            calculatedDelay.Should().BeGreaterOrEqualTo(TimeSpan.Zero);

            int upperLimitFactor = t < 2 ? (int)Math.Pow(2, t + 1) : (int)(Math.Pow(2, t + 1) - Math.Pow(2, t - 1));

            calculatedDelay.Should().BeLessOrEqualTo(TimeSpan.FromTicks(medianFirstDelay.Ticks * upperLimitFactor));
        }
    }
}