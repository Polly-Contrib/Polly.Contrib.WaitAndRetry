using System;
using System.Collections.Generic;

namespace Polly.Contrib.WaitAndRetry
{
    /// <summary>
    /// Helper methods for creating backoff strategies.
    /// </summary>
    public static partial class Backoff
    {
        private static IEnumerable<TimeSpan> Empty()
        {
            yield break;
        }
    }
}
