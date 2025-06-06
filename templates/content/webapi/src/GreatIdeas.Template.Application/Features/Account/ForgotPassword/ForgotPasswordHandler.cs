﻿namespace GreatIdeas.Template.Application.Features.Account.ForgotPassword;

public interface IForgotPasswordHandler : IApplicationHandler
{
    ValueTask<ErrorOr<ApiResponse>> ForgotPassword(ForgotPasswordRequest request,
        CancellationToken cancellationToken);
}

internal sealed class ForgotPasswordHandler(
    IUserRepository userRepository,
    IPublishEndpoint publishEndpoint,
    ILogger<ForgotPasswordHandler> logger) : IForgotPasswordHandler
{
    private static readonly ActivitySource ActivitySource = new(nameof(ForgotPasswordHandler));

    public async ValueTask<ErrorOr<ApiResponse>> ForgotPassword(ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        // Start activity
        using var getUserActivity = ActivitySource.CreateActivity(nameof(ForgotPassword), ActivityKind.Server);
        getUserActivity?.Start();

        // Get user
        try
        {
            var loginResponse = await userRepository.ForgotPassword(request, cancellationToken);
            if (loginResponse.IsError)
            {
                // Add event
                return loginResponse.Errors;
            }

            // Add event
            await publishEndpoint.Publish(new ForgottenPasswordEvent(loginResponse.Value));

            return new ApiResponse("A temporal password has been sent to your email");
        }
        catch (Exception exception)
        {
            // Add event
            return exception.LogCriticalUser(logger,
                getUserActivity,
                request.Email,
                "Forgot password request failed");
        }
    }
}