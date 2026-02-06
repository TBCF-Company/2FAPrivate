"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DeviceManagementService = void 0;
const uuid_1 = require("uuid");
const OtpManager_1 = require("./OtpManager");
/**
 * Device Management Service for handling device activation and management
 */
class DeviceManagementService {
    constructor(otpManager) {
        this.otpManager = otpManager || new OtpManager_1.OtpManager();
        this.devices = new Map();
        this.pendingActivations = new Map();
    }
    /**
     * Request device activation and generate OTP for display
     */
    async requestDeviceActivation(request) {
        try {
            console.log(`Device activation requested for ${request.deviceId}`);
            // Generate secret for this device
            const secret = this.otpManager.generateSecret();
            // Generate current OTP code to display (use sync version for simplicity)
            const otpCode = this.otpManager.generateTotpSync(secret);
            // Store pending activation
            this.pendingActivations.set(request.deviceId, secret);
            // Store device info as pending
            const deviceInfo = {
                deviceId: request.deviceId,
                deviceName: request.deviceName || 'Unknown Device',
                platform: request.platform || 'Unknown',
                osVersion: request.osVersion || 'Unknown',
                model: request.model || 'Unknown',
                registeredAt: new Date(),
                isActivated: false,
            };
            this.devices.set(request.deviceId, deviceInfo);
            console.log(`Generated activation OTP for device ${request.deviceId}: ${otpCode}`);
            return {
                success: true,
                message: 'Activation OTP generated. Please enter this code on your device.',
                secret,
                otpCode,
                deviceId: request.deviceId,
            };
        }
        catch (error) {
            console.error('Error requesting device activation:', error);
            return {
                success: false,
                message: `Error requesting activation: ${error instanceof Error ? error.message : 'Unknown error'}`,
            };
        }
    }
    /**
     * Validate device activation with OTP code
     */
    async validateDeviceActivation(validation) {
        try {
            console.log(`Validating device activation for ${validation.deviceId}`);
            // Check if we have a pending activation for this device
            const secret = this.pendingActivations.get(validation.deviceId);
            if (!secret) {
                console.warn(`No pending activation found for device ${validation.deviceId}`);
                return {
                    success: false,
                    message: 'No pending activation found for this device. Please request activation first.',
                };
            }
            // Validate OTP code
            const isValid = await this.otpManager.validateTotp(secret, validation.otpCode);
            if (!isValid) {
                console.warn(`Invalid OTP code for device ${validation.deviceId}`);
                return {
                    success: false,
                    message: 'Invalid OTP code. Please try again.',
                };
            }
            // Mark device as activated
            const deviceInfo = this.devices.get(validation.deviceId);
            if (deviceInfo) {
                deviceInfo.isActivated = true;
                deviceInfo.activatedAt = new Date();
                deviceInfo.activationToken = (0, uuid_1.v4)();
                // Remove from pending activations
                this.pendingActivations.delete(validation.deviceId);
                console.log(`Device ${validation.deviceId} activated successfully`);
                return {
                    success: true,
                    message: 'Device activated successfully!',
                    activationToken: deviceInfo.activationToken,
                    activatedAt: deviceInfo.activatedAt,
                };
            }
            else {
                console.error(`Device info not found for ${validation.deviceId}`);
                return {
                    success: false,
                    message: 'Device information not found.',
                };
            }
        }
        catch (error) {
            console.error('Error validating device activation:', error);
            return {
                success: false,
                message: `Error validating activation: ${error instanceof Error ? error.message : 'Unknown error'}`,
            };
        }
    }
    /**
     * Check if device is activated
     */
    async isDeviceActivated(deviceId) {
        const deviceInfo = this.devices.get(deviceId);
        return deviceInfo?.isActivated || false;
    }
    /**
     * Get device information
     */
    async getDeviceInfo(deviceId) {
        return this.devices.get(deviceId) || null;
    }
    /**
     * Get all activated devices
     */
    async getActivatedDevices() {
        return Array.from(this.devices.values()).filter((device) => device.isActivated);
    }
    /**
     * Deactivate a device
     */
    async deactivateDevice(deviceId) {
        const deviceInfo = this.devices.get(deviceId);
        if (deviceInfo) {
            deviceInfo.isActivated = false;
            deviceInfo.activatedAt = undefined;
            deviceInfo.activationToken = undefined;
            console.log(`Device ${deviceId} deactivated`);
            return true;
        }
        console.warn(`Device ${deviceId} not found for deactivation`);
        return false;
    }
    /**
     * Get all devices (for admin purposes)
     */
    async getAllDevices() {
        return Array.from(this.devices.values());
    }
    /**
     * Clear all devices (for testing purposes)
     */
    async clearAllDevices() {
        this.devices.clear();
        this.pendingActivations.clear();
    }
}
exports.DeviceManagementService = DeviceManagementService;
