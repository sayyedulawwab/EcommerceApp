﻿using Ecommerce.API.Extensions;
using Ecommerce.Application.Users.Login;
using Ecommerce.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Users.Login;
[Route("api/auth/login")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly ISender _sender;

    public LoginController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Login(
        LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var query = new LoginUserQuery(request.Email, request.Password);

        Result<AccessTokenResponse> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
