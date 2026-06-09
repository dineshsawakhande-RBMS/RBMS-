using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.Dashboard;

namespace RBMS.Api.Controllers;

public class DashboardController : ApiControllerBase
{
    [HttpGet("summary")]
    [HasPermission(Permissions.DashboardView)]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
        => Ok(await Mediator.Send(new GetDashboardSummaryQuery(), ct));
}
