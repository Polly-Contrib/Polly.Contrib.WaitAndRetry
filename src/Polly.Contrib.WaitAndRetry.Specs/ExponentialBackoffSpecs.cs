using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class ExponentialBackoffSpecs
    {
        [Fact]
        public void Backoff_WithInitialDelayLessThanZero_ThrowsException()
        {
            // Arrange
            var initialDelay = new TimeSpan(-1);
            const int retryCount = 3;
            const double factor = 2;
            const bool fastFirst = false;

            // Act
            Action act = () => Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("initialDelay");
        }

        [Fact]
        public void Backoff_WithRetryCountLessThanZero_ThrowsException()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(1);
            const int retryCount = -1;
            const double factor = 2;
            const bool fastFirst = false;

            // Act
            Action act = () => Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("retryCount");
        }

        [Fact]
        public void Backoff_WithRetryEqualToZero_ResultIsEmpty()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(1);
            const int retryCount = 0;
            const double factor = 2;
            const bool fastFirst = false;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Backoff_WithFactorLessThanOne_ThrowsException()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(1);
            const int retryCount = 3;
            const double factor = 0.99;
            const bool fastFirst = false;

            // Act
            Action act = () => Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("factor");
        }

        [Fact]
        public void Backoff_WithFastFirstEqualToTrue_ResultIsZero()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(1);
            const int retryCount = 3;
            const double factor = 2;
            const bool fastFirst = true;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            result.Should().NotBeNull();
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
                    timeSpan.Should().BeGreaterOrEqualTo(initialDelay);
                }
            }
        }

        [Fact]
        public void Backoff_ResultIsCorrect()
        {
            // Arrange
            var initialDelay = TimeSpan.FromMilliseconds(10);
            const int retryCount = 5;
            const double factor = 2;
            const bool fastFirst = false;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ExponentialBackoff(initialDelay, retryCount, factor, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(retryCount);

            result.Select(t => t.TotalMilliseconds).Should().BeEquivalentTo(new double[] { 10, 20, 40, 80, 160 });
        }
    }
}