/**
 * Device information interface
 */
export interface DeviceInfo {
    deviceId: string;
    deviceName: string;
    platform: string;
    osVersion: string;
    model: string;
    registeredAt: Date;
    isActivated: boolean;
    activatedAt?: Date;
    activationToken?: string;
}
/**
 * Device activation request interface
 */
export interface ActivationRequest {
    deviceId: string;
    deviceName?: string;
    platform?: string;
    osVersion?: string;
    model?: string;
    username: string;
    issuer: string;
}
/**
 * Device activation response interface
 */
export interface ActivationResponse {
    success: boolean;
    message: string;
    secret?: string;
    otpCode?: string;
    deviceId?: string;
}
/**
 * Device activation validation interface
 */
export interface DeviceActivationValidation {
    deviceId: string;
    otpCode: string;
    username?: string;
    issuer?: string;
}
/**
 * Activation validation result interface
 */
export interface ActivationValidationResult {
    success: boolean;
    message: string;
    activationToken?: string;
    activatedAt?: Date;
}
