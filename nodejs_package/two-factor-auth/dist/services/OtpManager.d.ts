/**
 * OTP Manager for generating and validating OTP codes
 */
export declare class OtpManager {
    private readonly digits;
    private readonly period;
    private readonly totp;
    constructor(digits?: number, period?: number);
    /**
     * Generate a random secret key
     */
    generateSecret(): string;
    /**
     * Generate TOTP code
     */
    generateTotp(secret: string): Promise<string>;
    /**
     * Generate TOTP code synchronously (fallback to manual implementation)
     */
    generateTotpSync(secret: string): string;
    /**
     * Validate TOTP code
     */
    validateTotp(secret: string, code: string, window?: number): Promise<boolean>;
    /**
     * Generate provisioning URI for QR code
     */
    generateProvisioningUri(params: {
        secret: string;
        issuer: string;
        account: string;
    }): string;
    /**
     * Get remaining time for current TOTP code (in seconds)
     */
    getRemainingTime(): number;
    /**
     * Get current time step
     */
    getCurrentTimeStep(): number;
}
