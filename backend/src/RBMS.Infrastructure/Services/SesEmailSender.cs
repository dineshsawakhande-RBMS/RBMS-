using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string FromAddress { get; set; } = "no-reply@example.com";
}

/// <summary>Transactional email via Amazon SES v2.</summary>
public class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly EmailOptions _options;

    public SesEmailSender(IAmazonSimpleEmailServiceV2 ses, IOptions<EmailOptions> options)
    {
        _ses = ses;
        _options = options.Value;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => _ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = _options.FromAddress,
            Destination = new Destination { ToAddresses = new List<string> { to } },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body { Html = new Content { Data = htmlBody } }
                }
            }
        }, ct);
}
