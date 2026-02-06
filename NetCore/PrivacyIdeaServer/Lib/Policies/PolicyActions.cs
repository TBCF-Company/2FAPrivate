// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/lib/policies/actions.py
//
// This is the list of policy actions available in privacyIDEA.
// These constants define the various actions that can be controlled by policies.

namespace PrivacyIdeaServer.Lib.Policies
{
    /// <summary>
    /// Policy action constants - defines all available policy actions
    /// Equivalent to Python's PolicyAction class
    /// </summary>
    public static class PolicyAction
    {
        // Admin Dashboard
        public const string ADMIN_DASHBOARD = "admin_dashboard";
        
        // Token Management
        public const string ASSIGN = "assign";
        public const string UNASSIGN = "unassign";
        public const string ENABLE = "enable";
        public const string DISABLE = "disable";
        public const string DELETE = "delete";
        public const string SET = "set";
        public const string SETDESCRIPTION = "setdescription";
        public const string SETPIN = "setpin";
        public const string SETRANDOMPIN = "setrandompin";
        public const string SETREALM = "setrealm";
        public const string RESET = "reset";
        public const string RESYNC = "resync";
        public const string REVOKE = "revoke";
        public const string IMPORT = "importtokens";
        public const string COPYTOKENPIN = "copytokenpin";
        public const string COPYTOKENUSER = "copytokenuser";
        public const string LOSTTOKEN = "losttoken";
        public const string LOSTTOKENPWLEN = "losttoken_PW_length";
        public const string LOSTTOKENPWCONTENTS = "losttoken_PW_contents";
        public const string LOSTTOKENVALID = "losttoken_valid";
        public const string SETTOKENINFO = "settokeninfo";
        public const string TOKENINFO = "tokeninfo";
        public const string HIDE_TOKENINFO = "hide_tokeninfo";
        public const string TOKENLIST = "tokenlist";
        public const string TOKENPAGESIZE = "token_page_size";
        public const string TOKENREALMS = "tokenrealms";
        public const string TOKENTYPE = "tokentype";
        public const string DEFAULT_TOKENTYPE = "default_tokentype";
        public const string TOKENISSUER = "tokenissuer";
        public const string TOKENLABEL = "tokenlabel";
        public const string TOKENWIZARD = "tokenwizard";
        public const string TOKENWIZARD2ND = "tokenwizard_2nd_token";
        public const string TOKENROLLOVER = "token_rollover";
        public const string GETSERIAL = "getserial";
        public const string RESETALLTOKENS = "reset_all_user_tokens";
        public const string DISABLED_TOKEN_TYPES = "disabled_token_types";
        public const string FORCE_SERVER_GENERATE = "force_server_generate";
        
        // Application
        public const string APPIMAGEURL = "appimageurl";
        public const string APPLICATION_TOKENTYPE = "application_tokentype";
        
        // Audit
        public const string AUDIT = "auditlog";
        public const string AUDIT_AGE = "auditlog_age";
        public const string AUDIT_DOWNLOAD = "auditlog_download";
        public const string AUDITPAGESIZE = "audit_page_size";
        public const string HIDE_AUDIT_COLUMNS = "hide_audit_columns";
        
        // Authentication
        public const string AUTHITEMS = "fetch_authentication_items";
        public const string AUTHMAXSUCCESS = "auth_max_success";
        public const string AUTHMAXFAIL = "auth_max_fail";
        public const string AUTOASSIGN = "autoassignment";
        public const string LASTAUTH = "last_auth";
        public const string AUTH_CACHE = "auth_cache";
        public const string PASSNOTOKEN = "passOnNoToken";
        public const string PASSNOUSER = "passOnNoUser";
        public const string PASSTHRU = "passthru";
        public const string PASSTHRU_ASSIGN = "passthru_assign";
        
        // CA Connector
        public const string CACONNECTORREAD = "caconnectorread";
        public const string CACONNECTORWRITE = "caconnectorwrite";
        public const string CACONNECTORDELETE = "caconnectordelete";
        
        // Challenge Response
        public const string CHALLENGERESPONSE = "challenge_response";
        public const string CHALLENGETEXT = "challenge_text";
        public const string CHALLENGETEXT_HEADER = "challenge_text_header";
        public const string CHALLENGETEXT_FOOTER = "challenge_text_footer";
        public const string GETCHALLENGES = "getchallenges";
        public const string TRIGGERCHALLENGE = "triggerchallenge";
        public const string INCREASE_FAILCOUNTER_ON_CHALLENGE = "increase_failcounter_on_challenge";
        public const string FORCE_CHALLENGE_RESPONSE = "force_challenge_response";
        
        // Email
        public const string EMAILCONFIG = "smtpconfig";
        public const string EMAILVALIDATION = "email_validation";
        public const string REQUIREDEMAIL = "requiredemail";
        
        // PIN
        public const string ENCRYPTPIN = "encrypt_pin";
        public const string OTPPIN = "otppin";
        public const string OTPPINRANDOM = "otp_pin_random";
        public const string OTPPINSETRANDOM = "otp_pin_set_random";
        public const string OTPPINMAXLEN = "otp_pin_maxlength";
        public const string OTPPINMINLEN = "otp_pin_minlength";
        public const string OTPPINCONTENTS = "otp_pin_contents";
        public const string PINHANDLING = "pinhandling";
        public const string CHANGE_PIN_FIRST_USE = "change_pin_on_first_use";
        public const string CHANGE_PIN_EVERY = "change_pin_every";
        public const string CHANGE_PIN_VIA_VALIDATE = "change_pin_via_validate";
        public const string ENROLLPIN = "enrollpin";
        
        // App Configuration
        public const string FORCE_APP_PIN = "force_app_pin";
        public const string APP_FORCE_UNLOCK = "app_force_unlock";
        
        // Random
        public const string GETRANDOM = "getrandom";
        
        // Login/Logout
        public const string LOGINMODE = "login_mode";
        public const string LOGOUT_REDIRECT = "logout_redirect";
        public const string LOGOUTTIME = "logout_time";
        public const string JWTVALIDITY = "jwt_validity";
        public const string LOGIN_TEXT = "login_text";
        
        // Machine
        public const string MACHINERESOLVERWRITE = "mresolverwrite";
        public const string MACHINERESOLVERDELETE = "mresolverdelete";
        public const string MACHINERESOLVERREAD = "mresolverread";
        public const string MACHINELIST = "machinelist";
        public const string MACHINETOKENS = "manage_machine_tokens";
        
        // Mangle
        public const string MANGLE = "mangle";
        
        // Limits
        public const string MAXTOKENREALM = "max_token_per_realm";
        public const string MAXTOKENUSER = "max_token_per_user";
        public const string MAXACTIVETOKENUSER = "max_active_token_per_user";
        
        // Response Details
        public const string NODETAILSUCCESS = "no_detail_on_success";
        public const string ADDUSERINRESPONSE = "add_user_in_response";
        public const string ADDRESOLVERINRESPONSE = "add_resolver_in_response";
        public const string NODETAILFAIL = "no_detail_on_fail";
        public const string HIDE_SPECIFIC_ERROR_MESSAGE = "hide_specific_error_message";
        public const string HIDE_SPECIFIC_ERROR_MESSAGE_FOR_OFFLINE_REFILL = "hide_specific_error_message_for_offline_refill";
        public const string HIDE_SPECIFIC_ERROR_MESSAGE_FOR_TTYPE = "hide_specific_error_message_for_ttype";
        
        // Password
        public const string PASSWORDRESET = "password_reset";
        public const string PASSWORD_LENGTH = "pw.length";
        public const string PASSWORD_CONTENTS = "pw.contents";
        
        // Policy
        public const string POLICYDELETE = "policydelete";
        public const string POLICYWRITE = "policywrite";
        public const string POLICYREAD = "policyread";
        public const string POLICYTEMPLATEURL = "policy_template_url";
        
        // Realm
        public const string REALM = "realm";
        public const string REALMDROPDOWN = "realm_dropdown";
        public const string SET_REALM = "set_realm";
        
        // Registration
        public const string REGISTRATIONCODE_LENGTH = "registration.length";
        public const string REGISTRATIONCODE_CONTENTS = "registration.contents";
        public const string REGISTERBODY = "registration_body";
        
        // Remote User
        public const string REMOTE_USER = "remote_user";
        
        // Resolver
        public const string RESOLVERDELETE = "resolverdelete";
        public const string RESOLVERWRITE = "resolverwrite";
        public const string RESOLVERREAD = "resolverread";
        public const string RESOLVER = "resolver";
        public const string REQUIRE_AUTH_FOR_RESOLVER_DETAILS = "require_auth_for_resolver_details";
        
        // Serial
        public const string SERIAL = "serial";
        
        // System Configuration
        public const string SYSTEMDELETE = "configdelete";
        public const string SYSTEMWRITE = "configwrite";
        public const string SYSTEMREAD = "configread";
        public const string CONFIGDOCUMENTATION = "system_documentation";
        public const string SETHSM = "set_hsm_password";
        
        // User
        public const string USERLIST = "userlist";
        public const string USERPAGESIZE = "user_page_size";
        public const string ADDUSER = "adduser";
        public const string DELETEUSER = "deleteuser";
        public const string UPDATEUSER = "updateuser";
        public const string USERDETAILS = "user_details";
        public const string SET_USER_ATTRIBUTES = "set_custom_user_attributes";
        public const string DELETE_USER_ATTRIBUTES = "delete_custom_user_attributes";
        
        // API Key
        public const string APIKEY = "api_key_required";
        
        // SMTP Server
        public const string SMTPSERVERWRITE = "smtpserver_write";
        public const string SMTPSERVERREAD = "smtpserver_read";
        
        // RADIUS Server
        public const string RADIUSSERVERWRITE = "radiusserver_write";
        public const string RADIUSSERVERREAD = "radiusserver_read";
        
        // PrivacyIDEA Server
        public const string PRIVACYIDEASERVERWRITE = "privacyideaserver_write";
        public const string PRIVACYIDEASERVERREAD = "privacyideaserver_read";
        
        // Event Handling
        public const string EVENTHANDLINGWRITE = "eventhandling_write";
        public const string EVENTHANDLINGREAD = "eventhandling_read";
        
        // Periodic Task
        public const string PERIODICTASKWRITE = "periodictask_write";
        public const string PERIODICTASKREAD = "periodictask_read";
        
        // SMS Gateway
        public const string SMSGATEWAYWRITE = "smsgateway_write";
        public const string SMSGATEWAYREAD = "smsgateway_read";
        
        // Multichallenge
        public const string RESYNC_VIA_MULTICHALLENGE = "resync_via_multichallenge";
        public const string ENROLL_VIA_MULTICHALLENGE = "enroll_via_multichallenge";
        public const string ENROLL_VIA_MULTICHALLENGE_TEXT = "enroll_via_multichallenge_text";
        public const string ENROLL_VIA_MULTICHALLENGE_TEMPLATE = "enroll_via_multichallenge_template";
        public const string ENROLL_VIA_MULTICHALLENGE_OPTIONAL = "enroll_via_multichallenge_optional";
        
        // Client Type
        public const string CLIENTTYPE = "clienttype";
        public const string PREFERREDCLIENTMODE = "preferred_client_mode";
        public const string CLIENT_MODE_PER_USER = "client_mode_per_user";
        
        // Subscription
        public const string MANAGESUBSCRIPTION = "managesubscription";
        
        // UI Configuration
        public const string SEARCH_ON_ENTER = "search_on_enter";
        public const string TIMEOUT_ACTION = "timeout_action";
        public const string DELETION_CONFIRMATION = "deletion_confirmation";
        public const string HIDE_BUTTONS = "hide_buttons";
        public const string HIDE_WELCOME = "hide_welcome_info";
        public const string SHOW_SEED = "show_seed";
        public const string CUSTOM_MENU = "custom_menu";
        public const string CUSTOM_BASELINE = "custom_baseline";
        public const string GDPR_LINK = "privacy_statement_link";
        public const string DIALOG_NO_TOKEN = "dialog_no_token";
        public const string SHOW_ANDROID_AUTHENTICATOR = "show_android_privacyidea_authenticator";
        public const string SHOW_IOS_AUTHENTICATOR = "show_ios_privacyidea_authenticator";
        public const string SHOW_CUSTOM_AUTHENTICATOR = "show_custom_authenticator";
        public const string SHOW_NODE = "show_node";
        public const string REQUIRE_DESCRIPTION = "require_description";
        public const string REQUIRE_DESCRIPTION_ON_EDIT = "require_description_on_edit";
        
        // Statistics
        public const string STATISTICSREAD = "statistics_read";
        public const string STATISTICSDELETE = "statistics_delete";
        
        // Authorization
        public const string AUTHORIZED = "authorized";
        public const string VERIFY_ENROLLMENT = "verify_enrollment";
        
        // Token Groups
        public const string TOKENGROUPS = "tokengroups";
        public const string TOKENGROUP_LIST = "tokengroup_list";
        public const string TOKENGROUP_ADD = "tokengroup_add";
        public const string TOKENGROUP_DELETE = "tokengroup_delete";
        
        // Service ID
        public const string SERVICEID_LIST = "serviceid_list";
        public const string SERVICEID_ADD = "serviceid_add";
        public const string SERVICEID_DELETE = "serviceid_delete";
        
        // Container Management
        public const string CONTAINER_DESCRIPTION = "container_description";
        public const string CONTAINER_INFO = "container_info";
        public const string CONTAINER_STATE = "container_state";
        public const string CONTAINER_CREATE = "container_create";
        public const string CONTAINER_DELETE = "container_delete";
        public const string CONTAINER_ADD_TOKEN = "container_add_token";
        public const string CONTAINER_REMOVE_TOKEN = "container_remove_token";
        public const string CONTAINER_ASSIGN_USER = "container_assign_user";
        public const string CONTAINER_UNASSIGN_USER = "container_unassign_user";
        public const string CONTAINER_REALMS = "container_realms";
        public const string CONTAINER_LIST = "container_list";
        public const string CONTAINER_REGISTER = "container_register";
        public const string CONTAINER_UNREGISTER = "container_unregister";
        public const string CONTAINER_ROLLOVER = "container_rollover";
        public const string CONTAINER_SERVER_URL = "privacyIDEA_server_url";
        public const string CONTAINER_REGISTRATION_TTL = "container_registration_ttl";
        public const string CONTAINER_CHALLENGE_TTL = "container_challenge_ttl";
        public const string CONTAINER_SSL_VERIFY = "container_ssl_verify";
        public const string CONTAINER_TEMPLATE_CREATE = "container_template_create";
        public const string CONTAINER_TEMPLATE_DELETE = "container_template_delete";
        public const string CONTAINER_TEMPLATE_LIST = "container_template_list";
        public const string CONTAINER_CLIENT_ROLLOVER = "container_client_rollover";
        public const string INITIALLY_ADD_TOKENS_TO_CONTAINER = "initially_add_tokens_to_container";
        public const string DISABLE_CLIENT_TOKEN_DELETION = "disable_client_token_deletion";
        public const string DISABLE_CLIENT_CONTAINER_UNREGISTER = "disable_client_container_unregister";
        public const string DEFAULT_CONTAINER_TYPE = "default_container_type";
        public const string CONTAINER_WIZARD_TYPE = "container_wizard_type";
        public const string CONTAINER_WIZARD_TEMPLATE = "container_wizard_template";
        public const string CONTAINER_WIZARD_REGISTRATION = "container_wizard_registration";
        public const string HIDE_CONTAINER_INFO = "hide_container_info";
        
        // RSS Feeds
        public const string RSS_FEEDS = "rss_feeds";
        public const string RSS_AGE = "rss_age";
    }
}
