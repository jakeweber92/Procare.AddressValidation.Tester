//-----------------------------------------------------------------------
// <copyright file="AddressValidationService.cs" company="Procare Software, LLC">
//     Copyright © 2021-2024 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class AddressValidationService : BaseHttpService
{
    public AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl)
        : this(httpClientFactory, disposeFactory, baseUrl, null, false)
    {
    }

    protected AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl, HttpMessageHandler? httpMessageHandler, bool disposeHandler)
        : base(httpClientFactory, disposeFactory, baseUrl, httpMessageHandler, disposeHandler)
    {
    }

    public async Task<string> GetAddressesAsync(AddressValidationRequest request, CancellationToken token = default)
    {
        return await this.SendWithRetries(request, token).ConfigureAwait(false);
    }
}
