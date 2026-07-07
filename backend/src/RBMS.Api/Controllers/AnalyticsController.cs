using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Analytics;
using RBMS.Application.Features.Analytics.Queries;

namespace RBMS.Api.Controllers;

public class AnalyticsController : ApiControllerBase
{
    [HttpGet("dead-stock")]
    [HasPermission(Permissions.ReportView)]
    [ProducesResponseType(typeof(DeadStockReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeadStockReportDto>> DeadStock(
        [FromQuery] GetDeadStockQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpGet("customer-retention")]
    [HasPermission(Permissions.ReportView)]
    [ProducesResponseType(typeof(CustomerRetentionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerRetentionDto>> CustomerRetention(
        [FromQuery] GetCustomerRetentionQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));
}
