"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.OtpManager = void 0;
const otplib_1 = require("otplib");
/**
 * OTP Manager for generating and validating OTP codes
 */
class OtpManager {
    constructor(digits = 6, period = 30) {
        this.digits = digits;
        this.period = period;
        // Create TOTP instance with options
        this.totp = new otplib_1.TOTP();
    }
    /**
     * Generate a random secret key
     */
    generateSecret() {
        return (0, otplib_1.generateSecret)();
    }
    /**
     * Generate TOTP code
     */
    async generateTotp(secret) {
        try {
            const code = await this.totp.generate({ secret, digits: this.digits, period: this.period });
            return code;
        }
        catch (error) {
            console.error('Error generating TOTP:', error);
            return '000000';
        }
    }
    /**
     * Generate TOTP code synchronously (fallback to manual implementation)
     */
    generateTotpSync(secret) {
        try {
            // Simple TOTP implementation for synchronous use
            const crypto = require('crypto');
            const base32 = require('base32.js');
            // Decode base32 secret
            const decoder = new base32.Decoder();
            const keyBytes = decoder.write(secret).finalize();
            // Calculate time step
            const epoch = Math.floor(Date.now() / 1000);
            const timeStep = Math.floor(epoch / this.period);
            // Convert time step to bytes
            const timeBuffer = Buffer.alloc(8);
            timeBuffer.writeBigUInt64BE(BigInt(timeStep));
            // Generate HMAC
            const hmac = crypto.createHmac('sha1', Buffer.from(keyBytes));
            hmac.update(timeBuffer);
            const hash = hmac.digest();
            // Dynamic truncation
            const offset = hash[hash.length - 1] & 0x0f;
            const binary = ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);
            const otp = (binary % Math.pow(10, this.digits)).toString().padStart(this.digits, '0');
            return otp;
        }
        catch (error) {
            console.error('Error generating TOTP synchronously:', error);
            return '000000';
        }
    }
    /**
     * Validate TOTP code
     */
    async validateTotp(secret, code, window = 1) {
        try {
            const result = await this.totp.verify(code, { secret, digits: this.digits, period: this.period });
            return result.valid;
        }
        catch (error) {
            console.error('Error validating TOTP:', error);
            return false;
        }
    }
    /**
     * Generate provisioning URI for QR code
     */
    generateProvisioningUri(params) {
        return `otpauth://totp/${encodeURIComponent(params.issuer)}:${encodeURIComponent(params.account)}?secret=${params.secret}&issuer=${encodeURIComponent(params.issuer)}&algorithm=SHA1&digits=${this.digits}&period=${this.period}`;
    }
    /**
     * Get remaining time for current TOTP code (in seconds)
     */
    getRemainingTime() {
        const currentTime = Math.floor(Date.now() / 1000);
        return this.period - (currentTime % this.period);
    }
    /**
     * Get current time step
     */
    getCurrentTimeStep() {
        return Math.floor(Date.now() / 1000 / this.period);
    }
}
exports.OtpManager = OtpManager;
