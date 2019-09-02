using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class AwsDecorrelatedJitterBackoffSpecs
    {
        [Fact]
        public void Backoff_WithMinDelayLessThanZero_ThrowsException()
        {
            // Arrange
            var minDelay = new TimeSpan(-1);
            var maxDelay = new TimeSpan(0);
            const int retryCount = 3;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            Action act = () => Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

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
            const int retryCount = 3;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            Action act = () => Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

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
            const int retryCount = -1;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            Action act = () => Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

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
            const int retryCount = 0;
            const bool fastFirst = false;
            const int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

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
            const int retryCount = 3;
            const bool fastFirst = true;
            const int seed = 1;

            // Act
            IEnumerable<TimeSpan> result = Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result = result.ToList();
            result.Should().HaveCount(retryCount);

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
        public void Backoff_ResultIsInRange()
        {
            // Arrange
            var minDelay = TimeSpan.FromMilliseconds(10);
            var maxDelay = TimeSpan.FromMilliseconds(100);
            const int retryCount = 3;
            const bool fastFirst = false;
            const int seed = 100;

            // Act
            IEnumerable<TimeSpan> result = Backoff.AwsDecorrelatedJitterBackoff(minDelay, maxDelay, retryCount, seed, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result = result.ToList();
            result.Should().HaveCount(retryCount);

            foreach (TimeSpan timeSpan in result)
            {
                timeSpan.Should().BeGreaterOrEqualTo(minDelay);
                timeSpan.Should().BeLessOrEqualTo(maxDelay);
            }
        }
    }
}