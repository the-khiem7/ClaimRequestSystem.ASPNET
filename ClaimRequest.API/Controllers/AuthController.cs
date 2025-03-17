﻿using System.Text.RegularExpressions;
using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests;
using ClaimRequest.DAL.Data.Requests.Auth;
using ClaimRequest.DAL.Data.Responses;
using ClaimRequest.DAL.Data.Responses.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class AuthController : BaseController<AuthController>
    {
        private readonly IAuthService _authService;
        public AuthController(ILogger<AuthController> logger, IAuthService authService) : base(logger)
        {
            _authService = authService;
        }

        [HttpPost(ApiEndPointConstant.Auth.LoginEndpoint)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var response = await _authService.Login(loginRequest);
            return Ok(ApiResponseBuilder.BuildResponse(StatusCodes.Status200OK, "Login successful", response));
        }

        [HttpPost(ApiEndPointConstant.Auth.ForgotPasswordEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "Please provide valid email, new password, and OTP."
                    )
                );
            }

            try
            {
                var result = await _authService.ForgotPassword(forgotPasswordRequest);

                return Ok(
                    ApiResponseBuilder.BuildResponse<object>(
                        StatusCodes.Status200OK,
                        "Password changed successfully.",
                        new { AttemptsLeft = result.AttemptsLeft }
                    )
                );
            }
            catch (OtpValidationException ex)
            {
                _logger.LogWarning(ex, "OTP validation error: {Message}", ex.Message);
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        new { AttemptsLeft = ex.AttemptsLeft },
                        StatusCodes.Status400BadRequest,
                        ex.Message,
                        ex.Message
                    )
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation error: {Message}", ex.Message);
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        ex.Message,
                        ex.Message
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {Message}", ex.Message);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status500InternalServerError,
                        "An error occurred.",
                        "An unexpected error occurred while changing the password."
                    )
                );
            }
        }

        [HttpPost(ApiEndPointConstant.Auth.ChangePasswordEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "Please provide valid email, old password, new password, and OTP."
                    )
                );
            }

            try
            {
                var result = await _authService.ChangePassword(changePasswordRequest);

                return Ok(
                    ApiResponseBuilder.BuildResponse<object>(
                        StatusCodes.Status200OK,
                        "Password changed successfully.",
                        new { AttemptsLeft = result.AttemptsLeft }
                    )
                );
            }
            catch (OtpValidationException ex)
            {
                _logger.LogWarning(ex, "OTP validation error: {Message}", ex.Message);
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        new { AttemptsLeft = ex.AttemptsLeft },
                        StatusCodes.Status400BadRequest,
                        ex.Message,
                        ex.Message
                    )
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation error: {Message}", ex.Message);
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        ex.Message,
                        ex.Message
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {Message}", ex.Message);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status500InternalServerError,
                        "An error occurred.",
                        "An unexpected error occurred while changing the password."
                    )
                );
            }
        }
    }
}
