using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UnsubscribeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnsubscribeController> _logger;

        public UnsubscribeController(IConfiguration configuration, ILogger<UnsubscribeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// One-click unsubscribe endpoint. Shows acknowledgment page.
        /// Supports both GET (link click) and POST (RFC 8058 one-click).
        /// </summary>
        [HttpGet]
        [HttpPost]
        public IActionResult Unsubscribe([FromQuery] string? email = null)
        {
            _logger.LogInformation("Unsubscribe request received for email: {Email}", email ?? "not provided");

            var frontendUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5173";

            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Unsubscribe - BCFR</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', 'Roboto', sans-serif;
            line-height: 1.6;
            color: #212529;
            margin: 0;
            padding: 0;
            background-color: #fdf8f1;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
        }}
        .container {{
            max-width: 500px;
            margin: 20px;
            background-color: #ffffff;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
        }}
        .header {{
            background: linear-gradient(135deg, #6B3AA0 0%, #4263EB 100%);
            color: white;
            padding: 40px 32px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 700;
            letter-spacing: -0.02em;
        }}
        .content {{
            padding: 40px 32px;
            text-align: center;
        }}
        .icon {{
            font-size: 64px;
            margin-bottom: 20px;
        }}
        .message {{
            font-size: 18px;
            color: #495057;
            margin: 0 0 24px 0;
        }}
        .sub-message {{
            font-size: 15px;
            color: #6C757D;
            margin: 0 0 32px 0;
        }}
        .button {{
            display: inline-block;
            padding: 14px 32px;
            background: #4263EB;
            color: white;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
        }}
        .button:hover {{
            background: #3B5BDB;
        }}
        .footer {{
            background-color: #F5F2ED;
            padding: 24px;
            text-align: center;
        }}
        .footer p {{
            color: #6C757D;
            font-size: 13px;
            margin: 0 0 4px 0;
        }}
        .footer a {{
            color: #4263EB;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Birmingham Committee on Foreign Relations</h1>
        </div>
        <div class='content'>
            <div class='icon'>✓</div>
            <p class='message'>Unsubscribe Request Received</p>
            <p class='sub-message'>
                Your request has been noted. If you continue to receive unwanted emails,
                please contact us at <a href='mailto:admin@birminghamforeignrelations.org'>admin@birminghamforeignrelations.org</a>.
            </p>
            <a href='{frontendUrl}' class='button'>Visit Our Website</a>
        </div>
        <div class='footer'>
            <p><strong>Birmingham Committee on Foreign Relations</strong></p>
            <p>2001 Park Pl, Suite 450</p>
            <p>Birmingham, Alabama 35203, US</p>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}
