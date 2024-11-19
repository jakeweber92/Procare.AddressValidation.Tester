using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Procare.AddressValidation.Tester;
/**
 * dont rewrite the wheel use polly
public class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;

        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
            }

            return response;
        }
    }**/

public class RetryHandler(HttpClientHandler handler) : DelegatingHandler(handler)
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {

        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3);

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromMilliseconds(750),
            TimeoutStrategy.Optimistic,
            (context, timeout, _, exception) =>
            {
                Console.WriteLine($"{"Timeout",-10}{timeout,-10:ss\\.fff}: {exception.GetType().Name}");
                return Task.CompletedTask;
            });


        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrInner<TimeoutRejectedException>()
            .WaitAndRetryAsync(delay, 
                (response, timeSpan) =>
                {
                    Console.WriteLine($"Something failed: Status Code: {(response?.Result?.StatusCode)}, Exception: {response?.Exception?.Message}, TimeSpan: {timeSpan}");
                });
            
        return Policy.WrapAsync(retryPolicy, timeoutPolicy).ExecuteAsync(async (ct) => await base.SendAsync(request, ct), CancellationToken.None);
    }

}

