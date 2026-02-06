/**
 * Example: Basic usage of the Two Factor Auth package
 */

import {
  DeviceManagementService,
  OtpManager,
  ActivationRequest,
  DeviceActivationValidation,
} from '../src/index';

async function main() {
  console.log('=== Two Factor Auth - Device Management Example ===\n');

  // Create service instances
  const otpManager = new OtpManager(6, 30);
  const deviceService = new DeviceManagementService(otpManager);

  // Example 1: Request device activation
  console.log('1. Requesting device activation...');
  const activationRequest: ActivationRequest = {
    deviceId: 'device-12345',
    deviceName: 'iPhone 13 Pro',
    platform: 'iOS',
    osVersion: '17.0',
    model: 'iPhone13,3',
    username: 'user@example.com',
    issuer: 'MyApp',
  };

  const activationResponse = await deviceService.requestDeviceActivation(activationRequest);
  
  if (activationResponse.success) {
    console.log('✓ Activation OTP generated successfully');
    console.log('  Device ID:', activationResponse.deviceId);
    console.log('  OTP Code:', activationResponse.otpCode);
    console.log('  Secret:', activationResponse.secret);
    console.log('  Message:', activationResponse.message);
  } else {
    console.log('✗ Activation request failed:', activationResponse.message);
    return;
  }

  console.log('\n2. Waiting for user to enter OTP code...');
  console.log('(In a real scenario, the user would enter the OTP code shown on the web interface)');

  // Simulate user entering the OTP code
  const userEnteredCode = activationResponse.otpCode!;

  // Example 2: Validate device activation
  console.log('\n3. Validating device activation...');
  const validation: DeviceActivationValidation = {
    deviceId: activationRequest.deviceId,
    otpCode: userEnteredCode,
    username: activationRequest.username,
    issuer: activationRequest.issuer,
  };

  const validationResult = await deviceService.validateDeviceActivation(validation);

  if (validationResult.success) {
    console.log('✓ Device activated successfully!');
    console.log('  Message:', validationResult.message);
    console.log('  Activation Token:', validationResult.activationToken);
    console.log('  Activated At:', validationResult.activatedAt);
  } else {
    console.log('✗ Activation validation failed:', validationResult.message);
    return;
  }

  // Example 3: Check device status
  console.log('\n4. Checking device status...');
  const isActivated = await deviceService.isDeviceActivated(activationRequest.deviceId);
  console.log('  Is Activated:', isActivated);

  // Example 4: Get device info
  console.log('\n5. Getting device information...');
  const deviceInfo = await deviceService.getDeviceInfo(activationRequest.deviceId);
  if (deviceInfo) {
    console.log('  Device ID:', deviceInfo.deviceId);
    console.log('  Device Name:', deviceInfo.deviceName);
    console.log('  Platform:', deviceInfo.platform);
    console.log('  OS Version:', deviceInfo.osVersion);
    console.log('  Model:', deviceInfo.model);
    console.log('  Registered At:', deviceInfo.registeredAt);
    console.log('  Is Activated:', deviceInfo.isActivated);
    console.log('  Activated At:', deviceInfo.activatedAt);
  }

  // Example 5: Get all activated devices
  console.log('\n6. Getting all activated devices...');
  const activatedDevices = await deviceService.getActivatedDevices();
  console.log(`  Found ${activatedDevices.length} activated device(s)`);
  activatedDevices.forEach((device, index) => {
    console.log(`  ${index + 1}. ${device.deviceName} (${device.deviceId})`);
  });

  // Example 6: OTP Manager usage
  console.log('\n7. OTP Manager examples...');
  const secret = otpManager.generateSecret();
  console.log('  Generated Secret:', secret);

  const totpCode = otpManager.generateTotp(secret);
  console.log('  TOTP Code:', totpCode);

  const isValid = otpManager.validateTotp(secret, totpCode);
  console.log('  Is Valid:', isValid);

  const remainingTime = otpManager.getRemainingTime();
  console.log('  Remaining Time:', remainingTime, 'seconds');

  const provisioningUri = otpManager.generateProvisioningUri({
    secret,
    issuer: 'MyApp',
    account: 'user@example.com',
  });
  console.log('  Provisioning URI:', provisioningUri);

  // Example 7: Deactivate device
  console.log('\n8. Deactivating device...');
  const deactivated = await deviceService.deactivateDevice(activationRequest.deviceId);
  if (deactivated) {
    console.log('✓ Device deactivated successfully');
  } else {
    console.log('✗ Failed to deactivate device');
  }

  // Verify deactivation
  const isStillActivated = await deviceService.isDeviceActivated(activationRequest.deviceId);
  console.log('  Is Still Activated:', isStillActivated);

  console.log('\n=== Example completed ===');
}

// Run the example
main().catch(console.error);
