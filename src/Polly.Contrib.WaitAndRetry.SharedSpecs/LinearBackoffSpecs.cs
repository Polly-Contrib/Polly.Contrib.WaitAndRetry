using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class LinearBackoffSpecs
    {
        [Fact]
        public void Backoff_WithMinDelayLessThanZero_ThrowsException()
        {
            // Arrange
            var minDelay = new TimeSpan(-1);
            var maxDelay = new TimeSpan(0);
            int retryCount = 3;
            bool fastFirst = false;
            int seed = 1;

            // Act
            Action act = () => Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("minDelay");
        }

        [Fact]
        public void Backoff_WithMaxDelayLessThanMinDelay_ThrowsException()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(1);
            var maxDelay = TimeSpan.FromMilliseconds(0);
            int retryCount = 3;
            bool fastFirst = false;
            int seed = 1;

            // Act
            Action act = () => Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("maxDelay");
        }

        [Fact]
        public void Backoff_WithRetryCountLessThanZero_ThrowsException()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(1);
            var maxDelay = TimeSpan.FromMilliseconds(2);
            int retryCount = -1;
            bool fastFirst = false;
            int seed = 1;

            // Act
            Action act = () => Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("retryCount");
        }

        [Fact]
        public void Backoff_WithRetryEqualToZero_ResultIsEmpty()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(1);
            var maxDelay = TimeSpan.FromMilliseconds(2);
            int retryCount = 0;
            bool fastFirst = false;
            int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Backoff_WithFastFirstEqualToTrue_ResultIsZero()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(1);
            var maxDelay = TimeSpan.FromMilliseconds(2);
            int retryCount = 3;
            bool fastFirst = true;
            int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            bool first = true;
            foreach (TimeSpan timeSpan in result)
            {
                if (first)
                {
                    timeSpan.Should().Be(TimeSpan.FromMilliseconds(0));
                    first = false;
                }
                else
                {
                    timeSpan.Should().BeGreaterOrEqualTo(minDelay);
                    timeSpan.Should().BeLessOrEqualTo(maxDelay);
                }
            }
        }

        [Fact]
        public void Backoff_WithMinDelayEqualTo10AndMaxDelayEqualTo100_ResultIsInRange()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(10);
            var maxDelay = TimeSpan.FromMilliseconds(100);
            int retryCount = 3;
            bool fastFirst = false;
            int seed = 100;

            // Act
            IEnumerable<TimeSpan> result = Backoff.DecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, fastFirst, seed);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            foreach (TimeSpan timeSpan in result)
            {
                timeSpan.Should().BeGreaterOrEqualTo(minDelay);
                timeSpan.Should().BeLessOrEqualTo(maxDelay);
            }
        }
    }
}