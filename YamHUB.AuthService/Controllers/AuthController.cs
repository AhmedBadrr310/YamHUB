using CommonClasses.Services;
using LearningApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System;
using System.Security.Claims;
using YamHUB.AuthService.Model;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace YamHUB.AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(CheckCreditnials _check, IConfiguration _configuration, Neo4jService _neo4jDriver, ILogger<AuthController> _logger, JWT _jwt) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterationViewMdl mdl)
        {
            try
            {
                var erorrMsg = new
                {
                    error = "Duplicated Email"
                };
                _logger.LogInformation($"Registeration: Starting Registeration for {mdl.Username}");
                var registrationMdl = (RegistrationMdl)mdl;
                string verificationCode = Guid.NewGuid().ToString("N").Substring(0, 6);
                var parameters = new Dictionary<string, object> //To prevent sql injection (still dont know how it will do it)
                {
                    { "username", registrationMdl.Username },
                    { "name", registrationMdl.Name },
                    { "email", registrationMdl.Email },
                    { "gender", registrationMdl.Gender },
                    { "age", registrationMdl.Age},
                    { "verified", false },
                    { "password", registrationMdl.HashPassword }
                };

                #region CheckForEmailRedundancy
                _logger.LogInformation($"Registeration: Checking if the email is duplicated for {mdl.Username}");
                if (await _check.CheckForQuery("MATCH (u:User) WHERE u.email = $email RETURN u", parameters))
                    return Conflict(erorrMsg); 
                #endregion

                erorrMsg = new
                {
                    error = "Duplicated Username"
                };

                #region CheckForUsernameRedundancy
                _logger.LogInformation($"Registeration: Checking if the username is duplicated for {mdl.Username}");
                if (await _check.CheckForQuery("MATCH (u:User) WHERE u.username = $username RETURN u", parameters))
                    return Conflict(erorrMsg); 
                #endregion

                erorrMsg = new
                {
                    error = "Doesn't meet the age requirment"
                };

                #region CheckForAgeValidity
                _logger.LogInformation($"Registeration: Checking if the age meets the requirment for {mdl.Username}");
                if (registrationMdl.Age < 16)
                    return BadRequest(erorrMsg); 
                #endregion

                _logger.LogInformation($"Registeration: Saving the user to the database for {mdl.Username}");
                _neo4jDriver.WriteQuery("CREATE (u:User {username:$username, name:$name, email:$email, password:$password, age:$age, gender:$gender, verified:$verified})",parameters);
                _logger.LogInformation($"Registeration: Data Saved for {mdl.Username}");

                #region CreatingToken
                var claims = new Claim[]
                {
                    new(ClaimTypes.NameIdentifier,registrationMdl.Username),
                    new(ClaimTypes.Name,registrationMdl.Name),
                    new(ClaimTypes.Gender,registrationMdl.Gender),
                    new(ClaimTypes.Email,registrationMdl.Email),
                    new Claim("age",registrationMdl.Age.ToString()),
                    new Claim("photo", registrationMdl.Photo!.ToString()!),
                    new Claim("bio", registrationMdl.Bio!),
                    new Claim("verificationCode", verificationCode),
                    new Claim("verified", "false")
                };

                _logger.LogInformation($"Creating token for {mdl.Username}");
                string token = _jwt.CreateToken(claims);
                _logger.LogInformation($"Token created for {mdl.Username}");
                #endregion

                #region SendingVerificationEmail
                _logger.LogInformation($"Fetching Email Service for {mdl.Username}");
                string apiUrl = "http://notificationyamhub.runasp.net/api/Email/SendVerificationEmail";
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        HttpResponseMessage response = await client.PostAsync(apiUrl, null);
                        if (response.IsSuccessStatusCode)
                        {
                            var _response = new
                            {
                                AccessToken = token,
                                Expiration = DateTime.UtcNow.AddMinutes(5)
                            };
                            _logger.LogInformation($"Registeration Completed for {mdl.Username}");
                            return Ok(_response);
                        }
                        
                        _logger.LogError($"Sending email failed for {mdl.Username}");
                        return BadRequest();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"exception happened while fetching email service for {mdl.Username} with exception: {e.Message}");
                    return BadRequest(e.Message);
                } 
                #endregion

            }
            catch(Exception e)
            {
                _logger.LogError($"Exception happened for {mdl.Username} with exception: {e.Message}");
                return BadRequest(e.Message);
            }

        }

        [Authorize]
        [HttpPost("VerifyUser")]
        public IActionResult VerifyUser([FromQuery] [RegularExpression(@"[A-Za-z0-9]{6,6}")] string VerificationCode)
        {

            var errorMsg = new
            {
                error = "Invalid Token"
            };
            #region ExtractingDataFromToken
            string authHeader = Request.Headers["Authorization"]!;
            string token = authHeader.Substring("Bearer ".Length).Trim();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")!.Value;
            var verificationCode = jwtToken.Claims.FirstOrDefault(c => c.Type == "verificationCode")!.Value;
            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")!.Value;
            if (string.IsNullOrEmpty(verificationCode) || string.IsNullOrEmpty(email))
                return BadRequest(errorMsg);
            #endregion
            try
            {
                
                _logger.LogInformation($"Data Extracted from header for user: {username}");
                if (verificationCode != VerificationCode)
                {
                    errorMsg = new
                    {
                        error = "Wrong Verification Code"
                    };
                    _logger.LogInformation($"Wrong Verification Code for user: {username}");
                    return BadRequest(errorMsg);
                }

                var parameters = new Dictionary<string, object>()
            {
                {"email", email }
            };
                _logger.LogInformation($"Updating verified statud in database for user: {username}");
                _neo4jDriver.WriteQuery("MATCH (u:User) WHERE u.email = $email SET u.verified = TRUE", parameters);
                _logger.LogInformation($"Database Updated for user: {username}");
                #region GenerateNewToken
                // Create updated claims
                var updatedClaims = jwtToken.Claims
                    .Where(c => c.Type != "verified") // Remove the existing 'verified' claim
                    .Append(new Claim("verified", "true")) // Add the updated 'verified' claim
                    .ToArray();

                // Generate a new token with updated claims
                string newToken = _jwt.CreateToken(updatedClaims);
                _logger.LogInformation($"New token created for verified user {username}");
                #endregion

                var response = new
                {
                    AccessToken = newToken,
                    Expiration = DateTime.UtcNow.AddMinutes(10)
                };
                _logger.LogInformation($"Verification completed for user: {username}");
                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Exception happend while verifying user: {username} with exception: {e.Message}");
                return BadRequest(e.Message);
                
            }

        }
    }
}
