#!/usr/bin/env node

/**
 * Cross-platform Kiota API Client Generator
 * Generates TypeScript clients from OpenAPI specs for all microservices
 * Works on Windows, macOS, and Linux
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import http from 'http';

const execAsync = promisify(exec);

// Parse command line arguments
const args = process.argv.slice(2);
const checkServices = args.includes('--check-services') || args.includes('-c');
const serviceIndex = args.findIndex(arg => arg === '--service' || arg === '-s');
const specificService = serviceIndex !== -1 ? args[serviceIndex + 1] : null;

// Service configurations
const services = {
  'dashboard': {
    url: 'http://localhost:5005/swagger/v1/swagger.json',
    output: './src/generated/clients/dashboard',
    className: 'DashboardClient'
  },
  'user-management': {
    url: 'http://localhost:5001/swagger/v1/swagger.json',
    output: './src/generated/clients/user-management',
    className: 'UserManagementClient'
  },
  'inventory': {
    url: 'http://localhost:5002/swagger/v1/swagger.json',
    output: './src/generated/clients/inventory',
    className: 'InventoryClient'
  },
  'sales': {
    url: 'http://localhost:5003/swagger/v1/swagger.json',
    output: './src/generated/clients/sales',
    className: 'SalesClient'
  },
  'financial': {
    url: 'http://localhost:5004/swagger/v1/swagger.json',
    output: './src/generated/clients/financial',
    className: 'FinancialClient'
  }
};

// Colors for terminal output
const colors = {
  reset: '\x1b[0m',
  cyan: '\x1b[36m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  red: '\x1b[31m'
};

function log(message, color = colors.reset) {
  console.log(`${color}${message}${colors.reset}`);
}

// Check if a service is running and get detailed error info
async function testServiceRunning(url) {
  return new Promise((resolve) => {
    const urlObj = new URL(url);
    const options = {
      hostname: urlObj.hostname,
      port: urlObj.port,
      path: urlObj.pathname,
      method: 'GET',
      timeout: 2000
    };

    let responseData = '';

    const req = http.request(options, (res) => {
      res.on('data', (chunk) => {
        responseData += chunk.toString();
      });

      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve({ available: true, error: null });
        } else {
          // Service is running but returning an error
          const errorMessage = responseData.substring(0, 500); // First 500 chars
          resolve({ available: false, error: `HTTP ${res.statusCode}: ${errorMessage}` });
        }
      });
    });

    req.on('error', (err) => {
      resolve({ available: false, error: `Connection failed: ${err.message}` });
    });

    req.on('timeout', () => {
      req.destroy();
      resolve({ available: false, error: 'Request timeout' });
    });

    req.end();
  });
}

// Check services availability
async function checkServicesAvailability() {
  log('\n🔍 Checking service availability...', colors.yellow);
  console.log('');

  for (const [serviceName, config] of Object.entries(services)) {
    const result = await testServiceRunning(config.url);
    if (result.available) {
      log(`  ✅ ${serviceName} - Running`, colors.green);
    } else {
      log(`  ❌ ${serviceName} - ${result.error}`, colors.red);
    }
  }
  console.log('');
}

// Generate client for a service
async function generateClient(serviceName, config) {
  log(`Processing ${serviceName}...`, colors.cyan);

  // Check if service is available
  const serviceCheck = await testServiceRunning(config.url);

  if (!serviceCheck.available) {
    log(`  ⚠️  Service error: ${serviceCheck.error}`, colors.yellow);
    console.log('');
    return { status: 'skipped', error: serviceCheck.error };
  }

  try {
    log('Generating TypeScript client...');

    const command = `dotnet kiota generate \
--language typescript \
--openapi ${config.url} \
--output ${config.output} \
--class-name ${config.className} \
--clean-output \
--backing-store \
--additional-data`;

    log (`running command: ${command}`);
    // Execute command and capture output
    const { stdout, stderr } = await execAsync(command);

    // Check if there are error messages in stdout or stderr
    const combinedOutput = (stdout + stderr).toLowerCase();
    if (combinedOutput.includes('fail:') || combinedOutput.includes('error:') || combinedOutput.includes('openapi error')) {
      log(`  ❌ Failed to generate client for ${serviceName}`, colors.red);
      log('\n  Kiota Output:', colors.yellow);
      if (stderr) {
        console.error(stderr);
      }
      if (stdout) {
        console.log(stdout);
      }
      console.log('');
      return { status: 'failed', error: stderr || stdout };
    }

    log(`  ✅ Successfully generated client for ${serviceName}`, colors.green);
    console.log('');
    return { status: 'success' };
  } catch (error) {
    log(`  ❌ Failed to generate client for ${serviceName}`, colors.red);
    log('\n  Error Details:', colors.yellow);
    
    if (error.stderr) {
      console.error('  STDERR:', error.stderr);
    }
    if (error.stdout) {
      console.log('  STDOUT:', error.stdout);
    }
    if (error.message) {
      console.error('  Message:', error.message);
    }
    
    console.log('');
    const errorMsg = error.stderr || error.stdout || error.message;
    return { status: 'failed', error: errorMsg };
  }
}

// Main execution
async function main() {
  log('🚀 Kiota API Client Generator', colors.cyan);
  log('================================', colors.cyan);
  console.log('');

  // Check services only
  if (checkServices) {
    await checkServicesAvailability();
    process.exit(0);
  }

  // Determine which services to generate
  let servicesToGenerate = services;

  if (specificService) {
    if (services[specificService]) {
      servicesToGenerate = { [specificService]: services[specificService] };
      log(`📦 Generating client for: ${specificService}`, colors.yellow);
    } else {
      log(`❌ Service '${specificService}' not found. Available services: ${Object.keys(services).join(', ')}`, colors.red);
      process.exit(1);
    }
  } else {
    log('📦 Generating clients for all services...', colors.yellow);
  }

  console.log('');

  // Generate clients
  let successCount = 0;
  let failCount = 0;
  let skippedCount = 0;

  for (const [serviceName, config] of Object.entries(servicesToGenerate)) {
    const result = await generateClient(serviceName, config);

    if (result.status === 'success') successCount++;
    else if (result.status === 'failed') failCount++;
    else if (result.status === 'skipped') skippedCount++;
  }

  // Summary
  log('================================', colors.cyan);
  log('📊 Generation Summary:', colors.cyan);
  log(`  ✅ Success: ${successCount}`, colors.green);
  log(`  ❌ Failed: ${failCount}`, colors.red);
  log(`  ⚠️  Skipped: ${skippedCount}`, colors.yellow);
  console.log('');

  if (failCount > 0) {
    log('⚠️  Some clients failed to generate. Check that all services are running.', colors.yellow);
    process.exit(1);
  } else if (skippedCount > 0) {
    log('ℹ️  Some services were not running. Start them with \'watch-all-services\' task.', colors.yellow);
  } else {
    log('🎉 All clients generated successfully!', colors.green);
  }
}

// Run main function
main().catch((error) => {
  console.error('Unexpected error:', error);
  process.exit(1);
});
