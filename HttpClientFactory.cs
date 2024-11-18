//-----------------------------------------------------------------------
// <copyright file="HttpClientFactory.cs" company="Procare Software, LLC">
//     Copyright © 2021-2024 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester;

using System;
using System.Collections.Generic;
using System.Net.Http;

public class HttpClientFactory : IHttpClientFactory
{
    private HttpClient? defaultClient;
    private Dictionary<HttpMessageHandler, HttpClient>? specificClients = new();

    ~HttpClientFactory()
    {
        this.Dispose(false);
    }

    public HttpClient CreateClient()
    {
        return this.CreateClient(default, default);
    }

    public HttpClient CreateClient(HttpMessageHandler? handler, bool disposeHandler)
    {
        ObjectDisposedException.ThrowIf(this.specificClients == null, this);

        HttpClient? client;

        if (handler == null)
        {
            client = this.defaultClient ??= new HttpClient();
        }
        else if (!this.specificClients.TryGetValue(handler, out client))
        {
            client = new HttpClient(handler, disposeHandler);
            this.specificClients.Add(handler, client);
        }

        return client;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.defaultClient?.Dispose();

            if (this.specificClients != null)
            {
                foreach (var client in this.specificClients.Values)
                {
                    client?.Dispose();
                }
            }
        }

        this.defaultClient = null;
        this.specificClients = null;
    }
}
