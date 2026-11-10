using FCG.Payments.Api.Extensions;
using FCG.Payments.Application.DTOs;
using FCG.Payments.Application.Services.Interfaces;
using FCG.Payments.Domain.Commons.Result;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FCG.Payments.Api.Controllers;

[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : BaseController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("orders/{orderId:long}")]
    [SwaggerOperation(Summary = "Get the payment result of an order (owner or administrator)")]
    [ProducesResponseType(typeof(SuccessResult<ReadPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrderIdAsync([FromRoute] long orderId, CancellationToken cancellationToken)
    {
        return (await _paymentService.GetByOrderIdAsync(orderId, cancellationToken)).ToActionResult();
    }
}
