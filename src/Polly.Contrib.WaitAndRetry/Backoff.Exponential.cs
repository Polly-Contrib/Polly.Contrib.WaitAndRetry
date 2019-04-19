using System;
using System.Collections.Generic;

namespace Polly.Contrib.WaitAndRetry
{
    partial class Backoff // .Exponential
    {
        /// <summary>
        /// Generates sleep durations in an exponential manner.
        /// The formula used is: Duration = <paramref name="initialDelay"/> x 2^iteration.
        /// For example: 100ms, 200ms, 400ms, 800ms, ...
        /// </summary>
        /// <param name="initialDelay">The duration value for the wait before the first retry.</param>
        /// <param name="factor">The exponent to multiply each subsequent duration by.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        public static IEnumerable<TimeSpan> ExponentialBackoff(TimeSpan initialDelay, int retryCount, double factor = 2.0, bool fastFirst = false)
        {
            if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            if (factor < 1.0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 1.0");

            if (retryCount == 0)
                return Empty();

            return Enumerate(initialDelay, retryCount, fastFirst, factor);

            IEnumerable<TimeSpan> Enumerate(TimeSpan initial, int retry, bool fast, double f)
            {
                int i = 0;
                if (fast)
                {
                    i++;
                    yield return TimeSpan.Zero;
                }

                double ms = initial.TotalMilliseconds;
                for (; i < retry; i++, ms *= f)
                {
                    yield return TimeSpan.FromMilliseconds(ms);
                }
            }
        }
    }
}
