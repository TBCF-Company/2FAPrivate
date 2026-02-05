// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/error.py

using System;

namespace PrivacyIdeaServer.Lib
{
    /// <summary>
    /// Error codes used throughout the application
    /// Equivalent to Python's ERROR class
    /// </summary>
    public static class ErrorCodes
    {
        public const int SUBSCRIPTION = 101;
        public const int TOKENADMIN = 301;
        public const int CONFIGADMIN = 302;
        public const int POLICY = 303;
        public const int IMPORTADMIN = 304;
        public const int VALIDATE = 401;
        public const int REGISTRATION = 402;
        public const int AUTHENTICATE = 403;
        public const int AUTHENTICATE_WRONG_CREDENTIALS = 4031;
        public const int AUTHENTICATE_MISSING_USERNAME = 4032;
        public const int AUTHENTICATE_AUTH_HEADER = 4033;
        public const int AUTHENTICATE_DECODING_ERROR = 4304;
        public const int AUTHENTICATE_TOKEN_EXPIRED = 4305;
        public const int AUTHENTICATE_MISSING_RIGHT = 4306;
        public const int AUTHENTICATE_ILLEGAL_METHOD = 4307;
        public const int ENROLLMENT = 404;
        public const int CA = 503;
        public const int CA_CSR_INVALID = 504;
        public const int CA_CSR_PENDING = 505;
        public const int RESOURCE_NOT_FOUND = 601;
        public const int HSM = 707;
        public const int SELFSERVICE = 807;
        public const int DATABASE = 902;
        public const int SERVER = 903;
        public const int USER = 904;
        public const int PARAMETER = 905;
        public const int RESOLVER = 907;
        public const int PARAMETER_USER_MISSING = 9051;
        public const int CONTAINER = 3000;
        public const int CONTAINER_NOT_REGISTERED = 3001;
        public const int CONTAINER_INVALID_CHALLENGE = 3002;
        public const int CONTAINER_ROLLOVER = 3003;
    }

    /// <summary>
    /// Base exception for all privacyIDEA errors
    /// Equivalent to Python's privacyIDEAError
    /// </summary>
    public class PrivacyIDEAError : Exception
    {
        public int Id { get; set; }

        public PrivacyIDEAError(string description = "privacyIDEAError!", int id = 10) 
            : base(description)
        {
            Id = id;
        }

        public int GetId() => Id;

        public string GetDescription() => Message;

        public override string ToString()
        {
            return $"ERR{Id}: {Message}";
        }
    }

    /// <summary>
    /// Subscription-related errors
    /// </summary>
    public class SubscriptionError : PrivacyIDEAError
    {
        public string? Application { get; set; }

        public SubscriptionError(string? description = null, string? application = null, 
            int id = ErrorCodes.SUBSCRIPTION)
            : base(description ?? "Subscription error!", id)
        {
            Application = application;
        }

        public override string ToString()
        {
            return $"{GetType().Name}('{Message}', application={Application})";
        }
    }

    /// <summary>
    /// Token import errors
    /// </summary>
    public class TokenImportException : PrivacyIDEAError
    {
        public TokenImportException(string description, int id = ErrorCodes.IMPORTADMIN)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Authentication errors
    /// </summary>
    public class AuthError : PrivacyIDEAError
    {
        public object? Details { get; set; }

        public AuthError(string description, int id = ErrorCodes.AUTHENTICATE, object? details = null)
            : base(description, id)
        {
            Details = details;
        }
    }

    /// <summary>
    /// Resource not found errors
    /// </summary>
    public class ResourceNotFoundError : PrivacyIDEAError
    {
        public ResourceNotFoundError(string description, int id = ErrorCodes.RESOURCE_NOT_FOUND)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Policy-related errors
    /// </summary>
    public class PolicyError : PrivacyIDEAError
    {
        public PolicyError(string description, int id = ErrorCodes.POLICY)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Validation errors
    /// </summary>
    public class ValidateError : PrivacyIDEAError
    {
        public ValidateError(string description = "validation error!", int id = ErrorCodes.VALIDATE)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Registration errors
    /// </summary>
    public class RegistrationError : PrivacyIDEAError
    {
        public RegistrationError(string description = "registration error!", int id = ErrorCodes.REGISTRATION)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Enrollment errors
    /// </summary>
    public class EnrollmentError : PrivacyIDEAError
    {
        public EnrollmentError(string description = "enrollment error!", int id = ErrorCodes.ENROLLMENT)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Token administration errors
    /// </summary>
    public class TokenAdminError : PrivacyIDEAError
    {
        public TokenAdminError(string description = "token admin error!", int id = ErrorCodes.TOKENADMIN)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Configuration administration errors
    /// </summary>
    public class ConfigAdminError : PrivacyIDEAError
    {
        public ConfigAdminError(string description = "config admin error!", int id = ErrorCodes.CONFIGADMIN)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Alias for ConfigAdminError for backward compatibility
    /// </summary>
    public class ConfigAdminException : ConfigAdminError
    {
        public ConfigAdminException(string description = "config admin error!", int id = ErrorCodes.CONFIGADMIN)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Certificate Authority errors
    /// </summary>
    public class CAError : PrivacyIDEAError
    {
        public CAError(string description = "CA error!", int id = ErrorCodes.CA)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Certificate Signing Request errors
    /// </summary>
    public class CSRError : CAError
    {
        public CSRError(string description = "CSR invalid", int id = ErrorCodes.CA_CSR_INVALID)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// CSR pending errors
    /// </summary>
    public class CSRPending : CAError
    {
        public string? RequestId { get; set; }

        public CSRPending(string description = "CSR pending", int id = ErrorCodes.CA_CSR_PENDING, 
            string? requestId = null)
            : base(description, id)
        {
            RequestId = requestId;
        }
    }

    /// <summary>
    /// User-related errors
    /// </summary>
    public class UserError : PrivacyIDEAError
    {
        public UserError(string description = "user error!", int id = ErrorCodes.USER)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Server errors
    /// </summary>
    public class ServerError : PrivacyIDEAError
    {
        public ServerError(string description = "server error!", int id = ErrorCodes.SERVER)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Hardware Security Module errors
    /// </summary>
    public class HSMException : PrivacyIDEAError
    {
        public HSMException(string description = "hsm error!", int id = ErrorCodes.HSM)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Self-service portal errors
    /// </summary>
    public class SelfserviceException : PrivacyIDEAError
    {
        public SelfserviceException(string description = "selfservice error!", int id = ErrorCodes.SELFSERVICE)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Parameter validation errors
    /// </summary>
    public class ParameterError : PrivacyIDEAError
    {
        public ParameterError(string description = "unspecified parameter error!", int id = ErrorCodes.PARAMETER)
            : base(description, id)
        {
        }
    }

    /// <summary>
    /// Database errors
    /// </summary>
    public class DatabaseError : PrivacyIDEAError
    {
        public DatabaseError(string description = "database error!", int eid = ErrorCodes.DATABASE)
            : base(description, eid)
        {
        }
    }

    /// <summary>
    /// Resolver errors
    /// </summary>
    public class ResolverError : PrivacyIDEAError
    {
        public ResolverError(string description = "resolver error!", int eid = ErrorCodes.RESOLVER)
            : base(description, eid)
        {
        }
    }

    /// <summary>
    /// Container errors
    /// </summary>
    public class ContainerError : PrivacyIDEAError
    {
        public ContainerError(string description = "container error!", int eid = ErrorCodes.CONTAINER)
            : base(description, eid)
        {
        }
    }

    /// <summary>
    /// Container not registered errors
    /// </summary>
    public class ContainerNotRegistered : ContainerError
    {
        public ContainerNotRegistered(string description = "container is not registered error!", 
            int eid = ErrorCodes.CONTAINER_NOT_REGISTERED)
            : base(description, eid)
        {
        }
    }

    /// <summary>
    /// Container invalid challenge errors
    /// </summary>
    public class ContainerInvalidChallenge : ContainerError
    {
        public ContainerInvalidChallenge(string description = "container challenge error!", 
            int eid = ErrorCodes.CONTAINER_INVALID_CHALLENGE)
            : base(description, eid)
        {
        }
    }

    /// <summary>
    /// Container rollover errors
    /// </summary>
    public class ContainerRollover : ContainerError
    {
        public ContainerRollover(string description = "container rollover error", 
            int eid = ErrorCodes.CONTAINER_ROLLOVER)
            : base(description, eid)
        {
        }
    }
}
