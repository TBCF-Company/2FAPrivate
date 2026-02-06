// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// OTP Controller for 2FA operations

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Services;
using System;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIdeaServer.Controllers
{
    /// <summary>
    /// Request model for OTP enrollment
    /// </summary>
    public class EnrollOtpRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        public string Issuer { get; set; } = "2FA App";
        
        public bool IsTotp { get; set; } = true;
        
        public int Digits { get; set; } = 6;
        
        public int Step { get; set; } = 30;
    }

    /// <summary>
    /// Request model for OTP validation
    /// </summary>
    public class ValidateOtpRequest
    {
        [Required]
        public string Secret { get; set; } = string.Empty;
        
        [Required]
        public string Code { get; set; } = string.Empty;
        
        public bool IsTotp { get; set; } = true;
        
        public long Counter { get; set; } = 0;
        
        public int Digits { get; set; } = 6;
        
        public int Step { get; set; } = 30;
    }

    /// <summary>
    /// Response model for OTP enrollment
    /// </summary>
    public class EnrollOtpResponse
    {
        public string Secret { get; set; } = string.Empty;
        public string ProvisioningUri { get; set; } = string.Empty;
        public string QrCodeDataUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for OTP validation
    /// </summary>
    public class ValidateOtpResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// OTP Controller for managing one-time passwords
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly IOtpTokenService _otpService;
        private readonly ILogger<OtpController> _logger;

        public OtpController(IOtpTokenService otpService, ILogger<OtpController> logger)
        {
            _otpService = otpService ?? throw new ArgumentNullException(nameof(otpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Enroll a new OTP token for a user
        /// </summary>
        [HttpPost("enroll")]
        public ActionResult<EnrollOtpResponse> Enroll([FromBody] EnrollOtpRequest request)
        {
            try
            {
                _logger.LogInformation("Enrolling OTP token for user {Username}", request.Username);
                
                // Generate a new secret
                var secret = _otpService.GenerateSecret();
                
                // Generate provisioning URI for QR code
                var provisioningUri = _otpService.GenerateProvisioningUri(
                    secret, 
                    request.Issuer, 
                    request.Username,
                    request.IsTotp,
                    request.Digits,
                    request.Step
                );
                
                // Generate QR code (for now, just return the URI - QR code generation will be done client-side)
                var response = new EnrollOtpResponse
                {
                    Secret = secret,
                    ProvisioningUri = provisioningUri,
                    QrCodeDataUrl = provisioningUri // Client will use this to generate QR code
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling OTP token");
                return StatusCode(500, new { error = "Failed to enroll OTP token", detail = ex.Message });
            }
        }

        /// <summary>
        /// Validate an OTP code
        /// </summary>
        [HttpPost("validate")]
        public ActionResult<ValidateOtpResponse> Validate([FromBody] ValidateOtpRequest request)
        {
            try
            {
                _logger.LogInformation("Validating OTP code");
                
                bool isValid;
                
                if (request.IsTotp)
                {
                    isValid = _otpService.ValidateTotp(
                        request.Secret,
                        request.Code,
                        request.Digits,
                        request.Step
                    );
                }
                else
                {
                    isValid = _otpService.ValidateHotp(
                        request.Secret,
                        request.Code,
                        request.Counter,
                        request.Digits
                    );
                }
                
                var response = new ValidateOtpResponse
                {
                    IsValid = isValid,
                    Message = isValid ? "OTP code is valid" : "OTP code is invalid"
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP code");
                return StatusCode(500, new { error = "Failed to validate OTP code", detail = ex.Message });
            }
        }

        /// <summary>
        /// Generate a current OTP code (for testing/demo purposes)
        /// </summary>
        [HttpPost("generate")]
        public ActionResult<object> Generate([FromBody] GenerateOtpRequest request)
        {
            try
            {
                _logger.LogInformation("Generating OTP code");
                
                string code;
                
                if (request.IsTotp)
                {
                    code = _otpService.GenerateTotp(request.Secret, request.Digits, request.Step);
                }
                else
                {
                    code = _otpService.GenerateHotp(request.Secret, request.Counter, request.Digits);
                }
                
                return Ok(new { code, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP code");
                return StatusCode(500, new { error = "Failed to generate OTP code", detail = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for OTP generation
    /// </summary>
    public class GenerateOtpRequest
    {
        [Required]
        public string Secret { get; set; } = string.Empty;
        
        public bool IsTotp { get; set; } = true;
        
        public long Counter { get; set; } = 0;
        
        public int Digits { get; set; } = 6;
        
        public int Step { get; set; } = 30;
    }
}
