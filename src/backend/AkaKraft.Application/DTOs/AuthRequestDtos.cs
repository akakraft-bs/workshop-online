namespace AkaKraft.Application.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName
);

public record LoginRequest(
    string Email,
    string Password
);

public record ConfirmEmailRequest(
    string Token
);

public record ResendConfirmationRequest(
    string Email
);

public record PasswordResetRequest(
    string Email
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);
