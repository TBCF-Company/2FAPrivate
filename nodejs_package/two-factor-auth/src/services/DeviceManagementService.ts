import { v4 as uuidv4 } from 'uuid';
import { OtpManager } from './OtpManager';
import {
  DeviceInfo,
  ActivationRequest,
  ActivationResponse,
  DeviceActivationValidation,
  ActivationValidationResult,
} from '../models/types';

/**
 * Device Management Service for handling device activation and management
 */
export class DeviceManagementService {
  private readonly otpManager: OtpManager;
  
  // In-memory storage (use database in production)
  private devices: Map<string, DeviceInfo>;
  private pendingActivations: Map<string, { secret: string; expiresAt: Date }>; // deviceId -> { secret, expiresAt }
  
  // Activation requests expire after 5 minutes
  private readonly activationExpiryMinutes = 5;

  constructor(otpManager?: OtpManager) {
    this.otpManager = otpManager || new OtpManager();
    this.devices = new Map();
    this.pendingActivations = new Map();
  }

  /**
   * Request device activation and generate OTP for display
   */
  async requestDeviceActivation(request: ActivationRequest): Promise<ActivationResponse> {
    try {
      console.log(`Device activation requested for ${request.deviceId}`);

      // Generate secret for this device
      const secret = this.otpManager.generateSecret();

      // Generate current OTP code to display (use sync version for simplicity)
      const otpCode = this.otpManager.generateTotpSync(secret);

      // Store pending activation with expiration
      const expiresAt = new Date(Date.now() + this.activationExpiryMinutes * 60 * 1000);
      this.pendingActivations.set(request.deviceId, { secret, expiresAt });

      // Store device info as pending
      const deviceInfo: DeviceInfo = {
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
    } catch (error) {
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
  async validateDeviceActivation(
    validation: DeviceActivationValidation
  ): Promise<ActivationValidationResult> {
    try {
      console.log(`Validating device activation for ${validation.deviceId}`);

      // Check if we have a pending activation for this device
      const activationData = this.pendingActivations.get(validation.deviceId);
      if (!activationData) {
        console.warn(`No pending activation found for device ${validation.deviceId}`);
        return {
          success: false,
          message: 'No pending activation found for this device. Please request activation first.',
        };
      }

      // Check if activation has expired
      if (new Date() > activationData.expiresAt) {
        this.pendingActivations.delete(validation.deviceId);
        console.warn(`Activation request expired for device ${validation.deviceId}`);
        return {
          success: false,
          message: 'Activation request has expired. Please request activation again.',
        };
      }

      const secret = activationData.secret;

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
        deviceInfo.activationToken = uuidv4();

        // Remove from pending activations
        this.pendingActivations.delete(validation.deviceId);

        console.log(`Device ${validation.deviceId} activated successfully`);

        return {
          success: true,
          message: 'Device activated successfully!',
          activationToken: deviceInfo.activationToken,
          activatedAt: deviceInfo.activatedAt,
        };
      } else {
        console.error(`Device info not found for ${validation.deviceId}`);
        return {
          success: false,
          message: 'Device information not found.',
        };
      }
    } catch (error) {
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
  async isDeviceActivated(deviceId: string): Promise<boolean> {
    const deviceInfo = this.devices.get(deviceId);
    return deviceInfo?.isActivated || false;
  }

  /**
   * Get device information
   */
  async getDeviceInfo(deviceId: string): Promise<DeviceInfo | null> {
    return this.devices.get(deviceId) || null;
  }

  /**
   * Get all activated devices
   */
  async getActivatedDevices(): Promise<DeviceInfo[]> {
    return Array.from(this.devices.values()).filter((device) => device.isActivated);
  }

  /**
   * Deactivate a device
   */
  async deactivateDevice(deviceId: string): Promise<boolean> {
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
  async getAllDevices(): Promise<DeviceInfo[]> {
    return Array.from(this.devices.values());
  }

  /**
   * Clear all devices (for testing purposes)
   */
  async clearAllDevices(): Promise<void> {
    this.devices.clear();
    this.pendingActivations.clear();
  }
}
