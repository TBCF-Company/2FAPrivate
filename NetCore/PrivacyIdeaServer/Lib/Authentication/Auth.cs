// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/auth.py
// Authentication library for admin and user authentication

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIdeaServer.Lib.Crypto;
using PrivacyIdeaServer.Models;
using PrivacyIdeaServer.Models.Database;

namespace PrivacyIdeaServer.Lib.Authentication
{
    /// <summary>
    /// User role constants
    /// Equivalent to Python's ROLE class
    /// </summary>
    public static class Role
    {
        public const string Admin = "admin";
        public const string User = "user";
        public const string Validate = "validate";
    }

    /// <summary>
    /// Result of web UI user authentication check
    /// </summary>
    public class WebUiAuthResult
    {
        public bool Authenticated { get; set; }
        public string Role { get; set; }
        public Dictionary<string, object>? Details { get; set; }

        public WebUiAuthResult(bool authenticated, string role, Dictionary<string, object>? details = null)
        {
            Authenticated = authenticated;
            Role = role;
            Details = details;
        }

        public WebUiAuthResult()
        {
            Authenticated = false;
            Role = Authentication.Role.User;
            Details = null;
        }
    }

    /// <summary>
    /// Authentication functions for admin and user verification
    /// Equivalent to Python's auth.py module functions
    /// </summary>
    public class AuthService
    {
        private readonly PrivacyIDEAContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(PrivacyIDEAContext context, ILogger<AuthService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify username and password against the database Admin table
        /// Equivalent to Python's verify_db_admin function
        /// </summary>
        /// <param name="username">The administrator username</param>
        /// <param name="password">The password</param>
        /// <returns>True if password is correct for the admin</returns>
        public async Task<bool> VerifyDbAdminAsync(string username, string password)
        {
            try
            {
                var admin = await _context.Admins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Username == username);

                if (admin == null || string.IsNullOrEmpty(admin.Password))
                {
                    return false;
                }

                return PasswordHasher.VerifyWithPepper(admin.Password, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying admin {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Check if a local admin exists in the database
        /// Equivalent to Python's db_admin_exists function
        /// </summary>
        /// <param name="username">The username of the admin</param>
        /// <returns>True if exists</returns>
        public async Task<bool> DbAdminExistsAsync(string username)
        {
            var admin = await GetDbAdminAsync(username);
            return admin != null;
        }

        /// <summary>
        /// Get a specific database admin
        /// Equivalent to Python's get_db_admin function
        /// </summary>
        /// <param name="username">The username of the admin</param>
        /// <returns>Admin object or null</returns>
        public async Task<Admin?> GetDbAdminAsync(string username)
        {
            return await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username == username);
        }

        /// <summary>
        /// Get all database admins
        /// Equivalent to Python's get_all_db_admins function
        /// </summary>
        /// <returns>List of all admins</returns>
        public async Task<List<Admin>> GetAllDbAdminsAsync()
        {
            return await _context.Admins
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Create or update a database admin
        /// Equivalent to Python's create_db_admin function
        /// </summary>
        /// <param name="username">Admin username</param>
        /// <param name="email">Admin email (optional)</param>
        /// <param name="password">Admin password (optional)</param>
        public async Task CreateDbAdminAsync(string username, string? email = null, string? password = null)
        {
            string? pwHash = null;
            if (!string.IsNullOrEmpty(password))
            {
                pwHash = PasswordHasher.HashWithPepper(password);
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);

            if (admin != null)
            {
                // Update existing admin
                if (!string.IsNullOrEmpty(email))
                {
                    admin.Email = email;
                }
                if (!string.IsNullOrEmpty(pwHash))
                {
                    admin.Password = pwHash;
                }
            }
            else
            {
                // Create new admin
                admin = new Admin(username, pwHash, email);
                _context.Admins.Add(admin);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Delete a database admin
        /// Equivalent to Python's delete_db_admin function
        /// </summary>
        /// <param name="username">Admin username to delete</param>
        public async Task DeleteDbAdminAsync(string username)
        {
            _logger.LogInformation("Deleting admin {Username}", username);
            
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
            if (admin == null)
            {
                throw new InvalidOperationException($"Admin {username} not found");
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Authenticate a user for the web UI
        /// Equivalent to Python's check_webui_user function
        /// This function checks against the userstore or against OTP/privacyIDEA
        /// </summary>
        /// <param name="user">The user object who tries to authenticate</param>
        /// <param name="password">Password, static and/or OTP</param>
        /// <param name="options">Additional options like g and clientip</param>
        /// <param name="superuserRealms">List of realms that contain admins</param>
        /// <param name="checkOtp">If true, authenticate against privacyIDEA instead of userstore</param>
        /// <returns>Tuple of (authenticated, role, details)</returns>
        public async Task<WebUiAuthResult> CheckWebUiUserAsync(
            object user,
            string password,
            Dictionary<string, object>? options = null,
            List<string>? superuserRealms = null,
            bool checkOtp = false)
        {
            options ??= new Dictionary<string, object>();
            superuserRealms ??= new List<string>();
            bool userAuth = false;
            string role = Role.User;
            Dictionary<string, object>? details = null;

            // TODO: Implement user object and check_user_pass function
            // This requires the user library and token library which should be converted separately
            
            if (checkOtp)
            {
                // Check if the given password matches an OTP token
                try
                {
                    // TODO: Implement check_user_pass
                    // var (check, checkDetails) = await CheckUserPassAsync(user, password, options);
                    // details = checkDetails;
                    // details["loginmode"] = "privacyIDEA";
                    // if (check)
                    // {
                    //     userAuth = true;
                    //     if (details.ContainsKey("serial"))
                    //     {
                    //         try
                    //         {
                    //             // TODO: Implement container support
                    //             // var container = await FindContainerForTokenAsync(details["serial"]);
                    //             // if (container != null)
                    //             // {
                    //             //     await container.UpdateLastAuthenticationAsync();
                    //             // }
                    //         }
                    //         catch (Exception e)
                    //         {
                    //             _logger.LogDebug(e, "Could not find container for token");
                    //         }
                    //     }
                    // }
                    _logger.LogWarning("OTP authentication not yet implemented");
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "Error authenticating user against privacyIDEA");
                }
            }
            else
            {
                // Check the password of the user against the userstore
                // TODO: Implement user.check_password
                // if (user.CheckPassword(password))
                // {
                //     userAuth = true;
                // }
                _logger.LogWarning("Userstore authentication not yet implemented");
            }

            // If the realm is in the superuser realms, elevate role to admin
            // TODO: Get user realm from user object
            // if (superuserRealms.Contains(user.Realm))
            // {
            //     role = Role.Admin;
            // }

            return new WebUiAuthResult(userAuth, role, details);
        }
    }
}
