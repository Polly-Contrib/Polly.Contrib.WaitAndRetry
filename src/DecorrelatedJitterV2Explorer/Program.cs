using System;
using System.Collections.Generic;
using System.Linq;
using Polly.Contrib.WaitAndRetry;

namespace DecorrelatedJitterV2Explorer
{
    class Program
    {
        static void Main(string[] args)
        {
            // This code is adapted from https://gist.github.com/reisenberger/2b696bec50d4bf059831c451f0a0cea9, originally at https://gist.github.com/george-polevoy/c0c36c3c22c9c1fe67821b1d8255413a .
            // The original author/credit for that code is @george-polevoy . Jitter formula used with permission as described at https://github.com/App-vNext/Polly/issues/530#issuecomment-526555979 

            // This code verifies and demonstrates that the coefficients chosen in the Polly.Contrib.WaitAndRetry Backoff.DecorrelatedJitterBackoffV2(...) implementation
            // generate a series of retry times whose 50th percentiles (medians) fall broadly in exponential backoff by powers of 2.
            // Any single individual sequence obtained from the iterator will (intentionally) show variation from this - that is the jitter.
            // This code demonstrates (by taking a large enough sample to average out the jitter again) that the broad behaviour is exponential backoff.

            // The code also outputs (as csv) the distribution of retry times (over the sample set), at each try; and combined over all tries.
            // Visualizing these (for example in PowerBI or Excel) shows the smooth distribution of the formula, as discussed in Polly issue 530.

            // Visualizations can also be seen at: https://github.com/App-vNext/Polly/issues/530#issuecomment-441740575 ; https://github.com/App-vNext/Polly/issues/530#issuecomment-450553899
            // (Those visualizations are based on const double pFactor = 3.0 but the characteristics are closely similar.
            // For the final implementation, const double pFactor = 4.0 was chosen, to tip the median progression closer to 1, 2, 4, 8, 16.)


            // Max retries to iterate over.
            var maxRetries = 6;

            // Test scale factor
            TimeSpan testMedianFirstRetryDelay = TimeSpan.FromSeconds(1); // Set this to any other value to explore that the results scale properly for user choice of medianFirstRetryDelay.

            Console.WriteLine($"Computing median values (50th percentile) of retry delays, with Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds({testMedianFirstRetryDelay.TotalSeconds}, ...))." +
                              $"{Environment.NewLine}{nameof(maxRetries)}:{maxRetries}");

            // Size of the sample used in the experiment.
            const int totalSamples = 100_000;

            // How finely we bucket the values (retry delays) emerging from running the experiment.
            const int slotsPerSec = 10;

            double formulaCeilingSeconds = Math.Pow(2, maxRetries + 1); // By induction, from reading the formula.

            int totalSlots = Convert.ToInt32(Math.Ceiling(slotsPerSec * formulaCeilingSeconds));

            double[,] samplesPerTry = new double[maxRetries, totalSlots];

            var formula = Backoff.DecorrelatedJitterBackoffV2(testMedianFirstRetryDelay, maxRetries);

            // Determine what proportion of samples fall in each slot - for each try.
            for (var i = 0; i < totalSamples; i++)
            {
                var time = 0.0; // A double in seconds at which the try would take place (accumulated on previous retry delays; and assuming zero time-to-failure of the underlying operation - see discussion of assumptions).
                using (var ie = formula.GetEnumerator())
                {
                    for (var r = 0; r < maxRetries && ie.MoveNext(); r++)
                    {
                        time += (double)ie.Current.Ticks / testMedianFirstRetryDelay.Ticks; // Normalize for the purposes of array-handling - testScaleFactor re-introduced later.
                        var slotToCredit = (int)(time * slotsPerSec);

                        if (slotToCredit > totalSlots)
                        {
                            throw new InvalidOperationException();
                        }

                        samplesPerTry[r, slotToCredit] += 1.0 / totalSamples;
                    }
                }
            }

            // Record a label of the decimal midpoint per slot.
            string[] midpointPerSlot = new string[totalSlots];
            double midpointIncrement = (1d / slotsPerSec) / 2;
            for (int s = 0; s < totalSlots; s++)
            {
                midpointPerSlot[s] = ((1d / (double)slotsPerSec) * s + midpointIncrement).ToString("0.00");
            }

            bool doNotOutputSlotsWithZeroSamples = false; // true for more readability; false for consistent data ranges to copy into graphing tools.

            // Output the distribution per try.
            const char separator = ',';
            for (int r = 0; r < maxRetries; r++)
            {
                Console.WriteLine();
                Console.WriteLine($"Distribution, try {r}");
                for (int s = 0; s < totalSlots; s++)
                {
                    double value = samplesPerTry[r, s];
                    if (value == 0 && doNotOutputSlotsWithZeroSamples) continue;
                    Console.WriteLine($"{midpointPerSlot[s]}{separator}{value}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Combined distribution.");
            for (int s = 0; s < totalSlots; s++)
            {
                double value = 0;
                for (int r = 0; r < maxRetries; r++)
                {
                    value += samplesPerTry[r, s];
                }

                if (value == 0 && doNotOutputSlotsWithZeroSamples) continue;
                Console.WriteLine($"{midpointPerSlot[s]}{separator}{value}");
            }

            // Find 50th percentile, per try.
            Console.WriteLine();
            for (int r = 0; r < maxRetries; r++)
            {
                double sum = 0.0;
                IEnumerable<double> slice = SliceForTry(samplesPerTry, r);
                var reified = slice.ToArray(); // Could have used .Current and .MoveNext(), but clearer to reason exactly which slot we have numerically taken, if reify into an array.
                for (int slot = 0; slot < totalSlots; slot++)
                {
                    double prevSum = sum;

                    double distribThisSlot = reified[slot];
                    sum += distribThisSlot;
                    if (sum >= 0.5) // When we pass the 50th percentile.
                    {
                        if (slot == 0)
                        {
                            throw new InvalidOperationException();
                        }

                        double midpointSlotBelow = Convert.ToDouble(midpointPerSlot[slot - 1]);
                        double midpointSlotAbove = Convert.ToDouble(midpointPerSlot[slot]);

                        double proportionThroughSlot = (0.5 - prevSum) / (sum - prevSum);

                        double fiftiethPercentile = midpointSlotBelow + (midpointSlotAbove - midpointSlotBelow) * proportionThroughSlot;

                        Console.WriteLine($"Try {r}, fiftiethPercentile of retry times approximates {fiftiethPercentile * testMedianFirstRetryDelay.TotalSeconds} seconds.");
                        break;
                    }
                }
            }

            IEnumerable<double> SliceForTry(double[,] original, int r)
            {
                for (int s = 0; s < totalSlots; s++)
                {
                    yield return original[r, s];
                }
            }
        }

    }
}
