using Asp.Versioning;
using Ecommerce.API.Extensions;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Application.Coupons;
using Ecommerce.Application.Coupons.AddCoupon;
using Ecommerce.Application.Coupons.DeleteCoupon;
using Ecommerce.Application.Coupons.EditCoupon;
using Ecommerce.Application.Coupons.GetCoupons;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers.Coupons;
[Route("api/v{v:apiVersion}/coupons")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
public class CouponsController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPost]
    public async Task<IActionResult> AddCoupon(AddCouponRequest request, ICommandHandler<AddCouponCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new AddCouponCommand(request.Code, request.PercentOff, request.StartsOn.UtcDateTime, request.ExpiresOn.UtcDateTime, request.MaxRedemptions);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, result.Value);
    }

    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetCoupons(IQueryHandler<GetCouponsQuery, IReadOnlyList<CouponResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new GetCouponsQuery();

        Result<IReadOnlyList<CouponResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [MapToApiVersion(1)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditCoupon(Guid id, EditCouponRequest request, ICommandHandler<EditCouponCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditCouponCommand(id, request.PercentOff, request.StartsOn.UtcDateTime, request.ExpiresOn.UtcDateTime, request.MaxRedemptions);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [MapToApiVersion(1)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id, ICommandHandler<DeleteCouponCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteCouponCommand(id);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
