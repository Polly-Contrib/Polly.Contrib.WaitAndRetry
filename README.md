# Polly.Contrib.WaitAndRetry

Polly.Contrib.WaitAndRetry contains several helper methods for defining backoff strategies when using [Polly](https://github.com/App-vNext/Polly/)'s wait-and-retry fault handling ability.

[![NuGet version](https://badge.fury.io/nu/Polly.Contrib.WaitAndRetry.svg)](https://badge.fury.io/nu/Polly.Contrib.WaitAndRetry) [![Build status](https://ci.appveyor.com/api/projects/status/5v3bpgjkw4snv3no?svg=true)](https://ci.appveyor.com/project/Polly-Contrib/polly-contrib-waitandretry) [![Slack Status](http://www.pollytalk.org/badge.svg)](http://www.pollytalk.org)

# Installing via NuGet

    Install-Package Polly.Contrib.WaitAndRetry

# Usage

One common approach when calling occasionally unreliable services is to wrap those calls in a retry policy. For example, if we're calling a remote service, we might choose to retry several times with a slight pause in between to account for infrastructure issues.

We can define a policy to do this in [Polly](https://github.com/App-vNext/Polly/).

While the core Polly package contains [core logic and gives examples for a variety of retry strategies](https://github.com/App-vNext/Polly#wait-and-retry), this Contrib packages up a variety of strategies in easy-to-use helper methods.

## Wait and Retry with Constant Back-off

The following defines a policy that will retry five times and pause 200ms between each call.

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(retryCount: 5, retryNumber => TimeSpan.FromMilliseconds(200));

We can simplify this by using the `ConstantBackoff` helper in Polly.Contrib.WaitAndRetry

    var delay = Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), retryCount: 5);

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(delay);

Note that `retryCount` must be greater than or equal to zero.

### Retry first failure fast

Additionally, when using the `ConstantBackoff` helper, or any other WaitAndRetry helper, we can signal that the first failure should retry immediately rather than waiting the indicated time. To do this, ensure the `fastFirst` parameter is `true`.

    var delay = Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), retryCount: 5, fastFirst: true);

This will still retry five times but the first retry will happen immediately.

## Wait and Retry with Linear Back-off

It can be desirable to wait increasingly long times between retries. For example, in cases where a remote service is being impacted by a high workload. In this scenario, we want to give the service some time to stabilize before trying again.

The first tool at our disposal is the `LinearBackoff` helper.

    var delay = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5);

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(delay);

This will create a linearly increasing retry delay of 100, 200, 300, 400, 500ms. 

The default linear factor is 1.0. However, we can provide our own.

    var delay = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5, factor: 2);

This will create an increasing retry delay of 100, 300, 500, 700, 900ms.

Note, the linear factor must be greater than or equal to zero. A factor of zero will return equivalent retry delays to the `ConstantBackoff` helper. 

## Wait and Retry with Exponential Back-off

We can also specify an exponential back-off where the delay duration is `initialDelay x 2^iteration`. Because of the exponential nature, this is best used with a low starting delay or in out-of-band communication, such as a service worker polling for information from a remote endpoint. Due to the potential for rapidly increasing times, care should be taken if an exponential retry is used in the code path for servicing a user request.

    var delay = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5);

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(delay);

This will create an exponentially increasing retry delay of 100, 200, 400, 800, 1600ms.

The default exponential growth factor is 2.0. However, can can provide our own.

    var delay = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5, factor: 4);

The upper for this retry with a growth factor of four is 25,600ms. Care and a calculator should be used when changing the factor.

Note, the growth factor must be greater than or equal to one. A factor of one will return equivalent retry delays to the `ConstantBackoff` helper. 

If the overall amount of time that an exponential-backoff retry policy could take is a concern, consider [placing a TimeoutPolicy outside the wait-and-retry policy](https://github.com/App-vNext/Polly/wiki/Timeout#combining-timeout-with-retries) using [PolicyWrap](https://github.com/App-vNext/Polly/wiki/PolicyWrap).  A timeout policy used in this way will limit the _overall_ execution time for all tries and waits-between-tries.  For instance, you could configure the exponential backoff for your wait-and-retry strategy to be 1, 2, 4, 8 seconds; and also impose an overall timeout, however many tries are invoked, at 45 seconds.  

    var retryWithBackoff = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(Backoff.ExponentialBackoff(TimeSpan.FromSeconds(1), retryCount: 5));
    var timeout = Policy.Timeout(TimeSpan.FromSeconds(45));
    var retryWithBackoffAndOverallTimeout = timeout.Wrap(retryWithBackoff);

When the combined time taken to make tries and wait between them exceeds 45 seconds, the TimeoutPolicy will be invoked and cause the current try and further retries to be abandoned.

## Wait and Retry with Jittered Back-off

In a high-throughput scenario, processing many requests at once, a fixed-progression wait-and-retry strategy can have disadvantages.

Incoming tries may naturally arrive correlated. Sudden issues affecting performance, combined with a fixed-progression wait-and-retry, can lead to subsequent retries being highly correlated.  For example, if there are 50 concurrent requests, and all 50 requests enter a wait-and-retry for 10ms, then all 50 requests will hit the service again in 10ms; potentially overwhelming the service again.

One way to address this is to add some randomness to the wait delay. This will cause each request to vary slightly on retry, which decorrelates the retries from each other.

### New jitter recommendation

Following exploration by Polly community members, we now recommend a jitter formula characterised by very smooth and even distribution of retry intervals, a well-controlled median initial retry delay, and a broadly exponential backoff.

    var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstDelay: TimeSpan.FromSeconds(1), retryCount: 5);

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(delay);

#### Characteristics of the recommended jitter formulae

**Median initial retry delay:** The median (50th percentile) of the first retry delay generated by the formula will be this value.

**Broadly exponential characteristic:** _Averaged over a suitably large sample_, the _medians_ of subsequent retries will be found to fall _close to_ a pattern of 2x, 4x, 8x etc the median of the initial retry delay.  

The smoothness and even distribution of the jitter generated by the formula is illustrated [third-from-bottom here](https://github.com/App-vNext/Polly/issues/530#issuecomment-441740575), compared against other approaches.
 
Credit for this work goes to [@george-polevoy](https://github.com/george-polevoy); included here [with permission](https://github.com/App-vNext/Polly/issues/530#issuecomment-526555979).

### Earlier jitter recommendations

The Polly team previously recommended the widely-referenced jitter strategy [described here](https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/). 

For completeness or for those who want continuity with previous implementations, this is still available in Polly.Contrib.WaitAndRetry:

    var delay = Backoff.AwsDecorrelatedJitterBackoff(minDelay: TimeSpan.FromMilliseconds(10), maxDelay: TimeSpan.FromMilliseconds(100), retryCount: 5);

    var retryPolicy = Policy
        .Handle<FooException>()
        .WaitAndRetryAsync(delay);

This will set up a policy that will retry five times. Each retry will delay for a random amount of time between the minimum of 10ms and the maximum of 100ms. 

### Characteristics of both jitter formulae

With both jitter formulae, note that unlike the linear and exponent wait-and-retry helpers, each subsequent retry delay in the jitter providers does not have a deterministic relationship to the previous. Due to the intentional jitter, the fourth retry delay may be significantly shorter (or longer), than the first or third, say.  

However, both jitter formulae do tend to increasing backoff.  That is, _averaged over a large sample_, the timing of later retries will be found to tend to increase compared to earlier retries.

### Ensuring optimum randomness

The Polly team [reviewed the literature on Random on .Net](https://github.com/App-vNext/Polly/issues/530#issuecomment-439680613), including how to ensure optimum randomness and safety in multi-threaded usage.

Internally, both jitter formulae uses a shared `Random` to better ensure a random distribution across all calls. You may, optionally, provide your own seed value.

    var delay = Backoff.AwsDecorrelatedJitterBackoff(
        minDelay: TimeSpan.FromMilliseconds(10), 
        maxDelay: TimeSpan.FromMilliseconds(100), 
        retryCount: 5, 
        seed: 100);

The shared `Random` will still be used internally in this case, but it will be seeded with your value versus the default used by .NET in a call to `new Random()`

## Sync and async compatible

Examples in this readme show asynchronous Polly policies, but all backoff helpers in `Polly.Contrib.WaitAndRetry` also work with synchronous `.WaitAndRetry()`.

## Retry first failure fast

All helper methods in Polly.Contrib.WaitAndRetry include an option to retry the first failure immediately. You can trigger this by passing in `fastFirst: true` to any of the helper methods.

    var delay = Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), retryCount: 5, fastFirst: true);

Note, the first retry will happen immediately and it will count against your retry count. That is, this will still retry five times but the first retry will happen immediately.

The logic behind a fast first retry is that a failure may just have been a transient blip rather than reflecting a deeper underlying issue. In that case, trying again with no delay can be the fastest route to success.  In this view, it is worth starting to back off (introduce delays) after you have had _two_ failures, indicating a more serious underlying problem.

## Credits

+ [@grant-d](https://github.com/grant-d): Original author of Polly.Contrib.WaitAndRetry.
+ [@george-polevoy](https://github.com/george-polevoy): Contributed the new jitter policy via [Polly/530](https://github.com/App-vNext/Polly/issues/530).
+ [@hyrmn](https://github.com/hyrmn): Added documentation.
+ [@reisenberger](https://github.com/reisenberger): Pulled the new jitter formula into the repo.
+ [@reisenberger](https://github.com/reisenberger): Extra documentation for the new jitter formula.

## Further Resources

Be sure to read through the material linked from the [Polly readme](https://github.com/App-vNext/Polly/).