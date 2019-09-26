using System;
using FluentAssertions;

namespace Polly.Contrib.WaitAndRetry.Specs.Utilities
{
    public static class TimeSpanExtensions
    {
        public static void ShouldBeBetweenOrEqualTo(this TimeSpan timeSpan, TimeSpan minDelay, TimeSpan maxDelay)
        {
            timeSpan.Should().BeGreaterOrEqualTo(minDelay);
            timeSpan.Should().BeLessOrEqualTo(maxDelay);
        }
    }
}