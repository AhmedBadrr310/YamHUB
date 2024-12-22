using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;

namespace YamHUB.NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController(IConfiguration _configuration, ILogger<EmailController> _logger, Neo4jService _neo4J) : ControllerBase
    {
        [Authorize]
        [HttpPost("SendVerificationEmail")]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var errorMsg = new
            {
                error = "Invalid token"
            };
            #region ExtractDataFromToken
            string authHeader = Request.Headers["Authorization"]!;
            string token = authHeader.Substring("Bearer ".Length).Trim();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")!.Value;
            var verificationCode = jwtToken.Claims.FirstOrDefault(c => c.Type == "verificationCode")!.Value;
            if (string.IsNullOrEmpty(verificationCode) || string.IsNullOrEmpty(email))
                return BadRequest(errorMsg); 
            #endregion

            try
            {
                #region SendingTheMail
                var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {

                    Credentials = new NetworkCredential("medo.pss201115@gmail.com", "pclm ocve tybo xtzm"),
                    EnableSsl = true, // This ensures a secure connection
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("medo.pss201115@gmail.com"),
                    Subject = "Email Verification",
                    Body = $"Your verification code is {verificationCode}.",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                #endregion
            }
            catch (Exception e)
            {
                errorMsg = new
                {
                    error = e.Message
                };
                return BadRequest(errorMsg);
            }
            return Ok();
        }
    }
}
