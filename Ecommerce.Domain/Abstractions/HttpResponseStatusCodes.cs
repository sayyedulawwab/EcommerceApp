using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Domain.Abstractions;
public enum HttpResponseStatusCodes
{
    BadRequest = 400,
    NotFound = 404,
    Conflict = 409,
    InternalServerError = 500,
}
