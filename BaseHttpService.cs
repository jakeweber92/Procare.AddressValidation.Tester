//-----------------------------------------------------------------------
// <copyright file="BaseHttpService.cs" company="Procare Software, LLC">
//     Copyright © 2021-2024 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class BaseHttpService : IDisposable
{
    private readonly bool disposeFactory;
    private readonly bool disposeHandler;
    private IHttpClientFactory? httpClientFactory;
    private HttpMessageHandler? httpMessageHandler;

    protected BaseHttpService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl, HttpMessageHandler? httpMessageHandler, bool disposeHandler)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.disposeFactory = disposeFactory;
        this.BaseUrl = baseUrl;
        this.httpMessageHandler = httpMessageHandler;
        this.disposeHandler = disposeHandler;
    }

    ~BaseHttpService()
    {
        this.Dispose(false);
    }

    public Uri BaseUrl { get; }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected HttpClient CreateClient()
    {
        ObjectDisposedException.ThrowIf(this.httpClientFactory == null, this);

        return this.httpClientFactory.CreateClient(this.httpMessageHandler, this.disposeHandler);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (this.disposeFactory)
            {
                this.httpClientFactory?.Dispose();
            }
        }

        this.httpMessageHandler = null;
        this.httpClientFactory = null;
    }

    public async Task<string> SendWithRetries(IRequest request, CancellationToken cancellationToken)
    {
        //After implementing my "SendWithRetries" method, I began researching what you meant by backoff and jitter.
        //It makes sense we would want it, but suddenly I realize I am re-inventing the wheel here. I will continue with this solution,
        //I think in real life I would definitly go for the polly library.
        //https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly


        var retryLimit = 3;
        for (int retryCount = 1; retryCount <= retryLimit; retryCount++)
        {
            var start = Stopwatch.GetTimestamp(); // todo: is stopwatch the most accurate way to detect time?
            try
            {
                Console.WriteLine($"Try #{retryCount} starting at {start}");

                var response = await this.SendAsync(request, 750, cancellationToken).ConfigureAwait(false);
                Console.WriteLine("Success!");
                return response.ToString();
            }
            catch (InternalErrorException e)
            {
                Console.WriteLine($"Internal Exception: {e.Message}. Retrying {retryCount} of {retryLimit}");
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine($"Timeout Exception: {e.Message}. Retrying {retryCount} of {retryLimit}");
            }
            catch
            {
                throw; 
            }
            await JitterWait(retryCount, cancellationToken).ConfigureAwait(false);
        }
        throw new HttpRequestException("Exceeded retry count. See console for more info.");
    }

    private async Task JitterWait(int retryCount, CancellationToken cancellationToken)
    {
        var expBackoff = (int)Math.Pow(2, retryCount);
        var maxJitter = (int)Math.Ceiling(expBackoff * 0.2);
        var finalBackoff = expBackoff + Random.Shared.Next(maxJitter);
        await Task.Delay(finalBackoff);
    }



    public async Task<string> SendAsync(IRequest request, int timeoutMS, CancellationToken cancellationToken)
    {
        using var httpRequest = request.ToHttpRequest(this.BaseUrl);

        using var cancelTimeout = new CancellationTokenSource();
        cancelTimeout.CancelAfter(750);

        using var response = await this.CreateClient().SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancelTimeout.Token)
            .ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;

        //todo: I have a hunch we can avoid magic numbers
        if (statusCode is >= 500 and < 600)
        {
            throw new InternalErrorException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Failed for some other reason.");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    }
}
