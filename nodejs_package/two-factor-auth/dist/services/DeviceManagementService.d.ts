import { OtpManager } from './OtpManager';
import { DeviceInfo, ActivationRequest, ActivationResponse, DeviceActivationValidation, ActivationValidationResult } from '../models/types';
/**
 * Device Management Service for handling device activation and management
 */
export declare class DeviceManagementService {
    private readonly otpManager;
    private devices;
    private pendingActivations;
    constructor(otpManager?: OtpManager);
    /**
     * Request device activation and generate OTP for display
     */
    requestDeviceActivation(request: ActivationRequest): Promise<ActivationResponse>;
    /**
     * Validate device activation with OTP code
     */
    validateDeviceActivation(validation: DeviceActivationValidation): Promise<ActivationValidationResult>;
    /**
     * Check if device is activated
     */
    isDeviceActivated(deviceId: string): Promise<boolean>;
    /**
     * Get device information
     */
    getDeviceInfo(deviceId: string): Promise<DeviceInfo | null>;
    /**
     * Get all activated devices
     */
    getActivatedDevices(): Promise<DeviceInfo[]>;
    /**
     * Deactivate a device
     */
    deactivateDevice(deviceId: string): Promise<boolean>;
    /**
     * Get all devices (for admin purposes)
     */
    getAllDevices(): Promise<DeviceInfo[]>;
    /**
     * Clear all devices (for testing purposes)
     */
    clearAllDevices(): Promise<void>;
}
