using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Procare.AddressValidation.Tester;

public interface IRequest
{
    public HttpRequestMessage ToHttpRequest(Uri baseUri);
}
