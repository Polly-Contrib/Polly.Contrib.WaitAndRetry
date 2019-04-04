using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Polly.Contrib.WaitAndRetry.Specs
{
    public sealed class ConstantBackoffSpecs
    {
        [Fact]
        public void Backoff_WithDelayLessThanZero_ThrowsException()
        {
            // Arrange
            var delay = new TimeSpan(-1);
            const int retryCount = 3;
            const bool fastFirst = false;

            // Act
            Action act = () => Backoff.ConstantBackoff(delay, retryCount, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("delay");
        }

        [Fact]
        public void Backoff_WithRetryCountLessThanZero_ThrowsException()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(1);
            const int retryCount = -1;
            const bool fastFirst = false;

            // Act
            Action act = () => Backoff.ConstantBackoff(delay, retryCount, fastFirst);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("retryCount");
        }

        [Fact]
        public void Backoff_WithRetryEqualToZero_ResultIsEmpty()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(1);
            const int retryCount = 0;
            const bool fastFirst = false;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ConstantBackoff(delay, retryCount, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Backoff_WithFastFirstEqualToTrue_ResultIsZero()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(1);
            const int retryCount = 3;
            const bool fastFirst = true;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ConstantBackoff(delay, retryCount, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(retryCount);

            bool first = true;
            foreach (TimeSpan timeSpan in result)
            {
                if (first)
                {
                    timeSpan.Should().Be(TimeSpan.Zero);
                    first = false;
                }
                else
                {
                    timeSpan.Should().Be(delay);
                }
            }
        }

        [Fact]
        public void Backoff_ResultIsConstant()
        {
            // Arrange
            var delay = TimeSpan.FromMilliseconds(10);
            const int retryCount = 3;
            const bool fastFirst = false;

            // Act
            IEnumerable<TimeSpan> result = Backoff.ConstantBackoff(delay, retryCount, fastFirst);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(retryCount);

            foreach (TimeSpan timeSpan in result)
            {
                timeSpan.Should().Be(delay);
            }
        }
    }
}