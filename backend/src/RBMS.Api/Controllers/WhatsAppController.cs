using Microsoft.AspNetCore.Mvc;
using RBMS.Api.Authorization;
using RBMS.Application.Common.Models;
using RBMS.Application.Common.Security;
using RBMS.Application.Features.WhatsApp;
using RBMS.Application.Features.WhatsApp.Commands;
using RBMS.Application.Features.WhatsApp.Queries;

namespace RBMS.Api.Controllers;

public class WhatsAppController : ApiControllerBase
{
    [HttpGet("messages")]
    [HasPermission(Permissions.WhatsAppSend)]
    [ProducesResponseType(typeof(PagedResult<WhatsAppMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WhatsAppMessageDto>>> GetMessages(
        [FromQuery] GetWhatsAppMessagesQuery query, CancellationToken ct)
        => Ok(await Mediator.Send(query, ct));

    [HttpPost("messages")]
    [HasPermission(Permissions.WhatsAppSend)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Send([FromBody] SendWhatsAppMessageCommand command, CancellationToken ct)
        => Ok(await Mediator.Send(command, ct));

    [HttpPost("sales/{saleId:guid}/invoice")]
    [HasPermission(Permissions.WhatsAppSend)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> SendInvoice(Guid saleId, CancellationToken ct)
        => Ok(await Mediator.Send(new SendInvoiceWhatsAppCommand(saleId), ct));
}
