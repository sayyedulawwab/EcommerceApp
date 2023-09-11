using Ecommerce.Models.APIModels;
using Ecommerce.Models.EntityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Services.Abstractions.Auth
{
    public interface IAuthService
    {
        bool Register(RegisterDTO model);
        User Login(LoginDTO model);

    }
}
