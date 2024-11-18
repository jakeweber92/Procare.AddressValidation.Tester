//-----------------------------------------------------------------------
// <copyright file="AddressValidationRequest.cs" company="Procare Software, LLC">
//     Copyright © 2021-2024 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester;

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;

public record struct AddressValidationRequest(
    string? CompanyName,
    string? Line1,
    string? Line2,
    string? City,
    string? StateCode,
    string? Urbanization,
    string? ZipCodeLeading5,
    string? ZipCodeTrailing4)
: IEquatable<AddressValidationRequest>
{
    public string ToQueryString()
    {
        var result = new StringBuilder();

        foreach (var prop in this.GetType().GetProperties())
        {
            var value = (string?)prop.GetMethod!.Invoke(this, Array.Empty<object>());
            if (!string.IsNullOrEmpty(value))
            {
                result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}={2}", result.Length == 0 ? "?" : "&", WebUtility.UrlEncode(prop.Name), WebUtility.UrlEncode(value));
            }
        }

        return result.ToString();
    }

    public HttpRequestMessage ToHttpRequest(Uri baseUri)
    {
        return new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, this.ToQueryString()));
    }
}
