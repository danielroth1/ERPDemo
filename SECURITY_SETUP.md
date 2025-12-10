# 🔐 Security Setup Guide

## Overview

This project follows security best practices by **NOT storing secrets in git**. Configuration files are structured as follows:

| File | Purpose | Contains Secrets? | Git Tracked? |
|------|---------|------------------|--------------|
| `appsettings.json` | Base configuration structure | ❌ No | ✅ Yes |
| `appsettings.Development.json` | Local development values | ✅ Yes (local only) | ✅ Yes (safe defaults) |
| `appsettings.Production.json` | Production overrides | ✅ Yes | ❌ No (gitignored) |
| `.env` | Environment variables | ✅ Yes | ❌ No (gitignored) |
| `.env.example` | Template for .env | ❌ No | ✅ Yes |

## 🚀 First-Time Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd ERPDemo
```

### 2. Create Your `.env` File

```bash
# Copy the example file
cp .env.example .env

# Edit with your actual values
# For local development, you can use the defaults:
# - JWT_SECRET: Use the example or generate a new one
# - MONGODB credentials: admin/admin123 is fine locally
# - SMTP: Leave blank if not testing emails
```

**For Local Development** - use these safe defaults in `.env`:
```bash
JWT_SECRET=local-development-secret-key-min-32-characters-long-not-for-production
MONGODB_ROOT_USERNAME=admin
MONGODB_ROOT_PASSWORD=admin123
```

**For Production** - MUST generate secure secrets:
```bash
# Generate JWT secret
JWT_SECRET=$(openssl rand -base64 32)

# Generate MongoDB password
MONGODB_ROOT_PASSWORD=$(openssl rand -base64 16)
```

### 3. Verify Configuration

The services will load configuration in this order:
1. `appsettings.json` (structure, no secrets)
2. `appsettings.Development.json` (local dev values)
3. Environment variables (override everything)

## 🔑 Configuration Hierarchy

### Local Development (ASPNETCORE_ENVIRONMENT=Development)

```
appsettings.json
  ↓ (merged with)
appsettings.Development.json  ← Has working local credentials
  ↓ (overridden by)
Environment Variables from .env
```

**Result**: Works immediately after `git clone` + `cp .env.example .env`

### Production Deployment

```
appsettings.json
  ↓ (merged with)
appsettings.Production.json  ← You create this (not in git!)
  ↓ (overridden by)
Environment Variables (from docker-compose, K8s, etc.)
```

## 📁 File Structure

### ✅ Safe to Commit (Base Structure)

**`services/*/appsettings.json`**:
```json
{
  "Logging": { ... },
  "MongoDB": {
    "ConnectionString": "",  // ← Empty! Must come from env
    "DatabaseName": "erp_users"
  },
  "Jwt": {
    "Secret": "",  // ← Empty! Must come from env
    "Issuer": "erp-system",
    "Audience": "erp-clients"
  }
}
```

### ✅ Safe to Commit (Local Development)

**`services/*/appsettings.Development.json`**:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017"
  },
  "Jwt": {
    "Secret": "local-development-secret-key-min-32-characters-long-not-for-production"
  }
}
```

These contain **localhost** credentials that are:
- ✅ Only valid on your local machine
- ✅ Protected by MongoDB running locally
- ✅ Never used in production
- ✅ Clearly marked as "not-for-production"

### ❌ NEVER Commit (Production)

**`services/*/appsettings.Production.json`** (gitignored):
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://realuser:STRONG_PASSWORD@prod-server:27017"
  },
  "Jwt": {
    "Secret": "PRODUCTION_SECRET_FROM_SECURE_VAULT"
  }
}
```

## 🔧 Developer Workflow

### Fresh Checkout
```bash
# 1. Clone repo
git clone <repo>

# 2. Copy environment template
cp .env.example .env

# 3. Start infrastructure
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# 4. Run services
dotnet run --project services/gateway/ApiGateway

# ✅ Everything works! Secrets loaded from appsettings.Development.json
```

### Production Deployment
```bash
# 1. Create production .env file
cat > .env << EOF
JWT_SECRET=$(openssl rand -base64 32)
MONGODB_ROOT_PASSWORD=$(openssl rand -base64 16)
# ... other production values
EOF

# 2. Deploy with docker-compose
docker-compose up -d

# ✅ Secrets loaded from .env file, NOT from config files
```

## 🛡️ Security Checklist

### Before Committing Code

- [ ] **NEVER** commit `.env` files
- [ ] **NEVER** commit `appsettings.Production.json`
- [ ] **VERIFY** `.gitignore` includes:
  ```gitignore
  .env
  appsettings.Production.json
  appsettings.Staging.json
  ```
- [ ] **CHECK** `appsettings.json` has empty strings for secrets:
  ```json
  "Secret": "",
  "ConnectionString": "",
  ```

### Before Production Deployment

- [ ] **ROTATE** all secrets from development
- [ ] **GENERATE** strong JWT secret: `openssl rand -base64 32`
- [ ] **GENERATE** strong MongoDB password: `openssl rand -base64 16`
- [ ] **VERIFY** `.env` file has production values
- [ ] **SECURE** `.env` file permissions: `chmod 600 .env`
- [ ] **BACKUP** `.env` file to secure location (password manager, vault)

### After Security Incident

If secrets are accidentally committed:

```bash
# 1. IMMEDIATELY rotate all exposed secrets
openssl rand -base64 32  # New JWT secret
openssl rand -base64 16  # New MongoDB password

# 2. Update production .env file
# 3. Restart all services
docker-compose restart

# 4. Remove from git history (if public repo)
# Use BFG Repo-Cleaner or git-filter-repo
git filter-repo --invert-paths --path '.env'

# 5. Force push (coordinate with team!)
git push --force
```

## 🎯 Why This Approach?

### ✅ Benefits

1. **Developer Experience**: Clone → copy .env.example → works!
2. **Security**: Production secrets never in git
3. **Flexibility**: Easy to override any value
4. **12-Factor App**: Configuration via environment
5. **Team Collaboration**: Everyone has same structure

### ❌ Alternative Approaches (Why Not?)

| Approach | Problem |
|----------|---------|
| Secrets in `appsettings.json` | ❌ Committed to git, exposed |
| Only use `.env` | ❌ No structure, hard to onboard |
| Delete `appsettings.json` | ❌ Developers must create files manually |
| Use User Secrets only | ❌ Doesn't work in Docker/K8s |

## 📚 Additional Resources

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [User Secrets for Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [12-Factor App Config](https://12factor.net/config)
- [OWASP Secrets Management](https://owasp.org/www-community/vulnerabilities/Use_of_hard-coded_password)

## 🆘 Troubleshooting

### "Configuration value not found"

**Cause**: Secret is empty in `appsettings.json` and not provided via environment

**Fix**: 
```bash
# Check if .env file exists
ls -la .env

# Verify environment variable is set
echo $JWT_SECRET

# For local dev, ensure using Development environment
export ASPNETCORE_ENVIRONMENT=Development
```

### "Authentication failed for user 'admin'"

**Cause**: MongoDB credentials mismatch

**Fix**:
```bash
# 1. Check .env file has correct password
cat .env | grep MONGODB

# 2. Restart MongoDB with new credentials
cd infrastructure
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up -d
```

### "JWT token validation failed"

**Cause**: Services using different JWT secrets

**Fix**:
```bash
# All services must use same JWT_SECRET
# Check each appsettings.Development.json has same value

# OR use environment variable for all:
export JWT_SECRET="your-secret-here"
```

## 🎓 Learning Resources

### Understanding Configuration Priority

Test configuration loading order:

```bash
# See what configuration is being used
dotnet run --project services/gateway/ApiGateway

# Override with environment variable
JWT_SECRET="test" dotnet run --project services/gateway/ApiGateway

# Check configuration in code
// Add to Startup.cs or Program.cs
var jwtSecret = Configuration["Jwt:Secret"];
Console.WriteLine($"Using JWT Secret: {jwtSecret?.Substring(0, 5)}...");
```

---

**Remember**: When in doubt, secrets should come from **environment variables** or **secure vaults**, NEVER from files in git!
