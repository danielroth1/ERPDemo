# Environment Variables Guide

## How Docker Compose Environment Variables Work

Docker Compose supports multiple ways to set environment variables, with the following priority (highest to lowest):

### 1. **docker-compose.override.yml** (Highest Priority - Local Development)
This file provides **local development overrides** and is automatically loaded by Docker Compose.
- **Already configured** with dummy SMTP values for local development
- **Not tracked in production** - typically in `.gitignore` for local customization
- Use this for local development values that differ from production

### 2. **.env file** (Production/Staging)
Create a `.env` file in the same directory as `docker-compose.yml`:

```bash
# Example .env file
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM_EMAIL=noreply@erp-system.com
```

**On the target machine:**
1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```
2. Edit `.env` with your actual values
3. Run `docker-compose up -d`

**Security:** Add `.env` to `.gitignore` to prevent committing secrets!

### 3. **System Environment Variables**
Set on the target machine before running docker-compose:

```bash
# Linux/macOS
export SMTP_HOST=smtp.gmail.com
export SMTP_PORT=587

# Windows PowerShell
$env:SMTP_HOST="smtp.gmail.com"
$env:SMTP_PORT="587"
```

Then run:
```bash
docker-compose up -d
```

### 4. **Inline with docker-compose command**
Pass variables directly when running docker-compose:

```bash
SMTP_HOST=smtp.gmail.com SMTP_PORT=587 docker-compose up -d
```

## Current Setup

### Local Development (Default)
✅ **Already configured** in `docker-compose.override.yml`:
- Uses dummy SMTP values
- No additional setup required
- Just run: `docker-compose up -d`

### Production/Staging Deployment
1. Create `.env` file on the target machine:
   ```bash
   cp .env.example .env
   nano .env  # or vim, code, etc.
   ```

2. Update with real credentials:
   ```env
   SMTP_HOST=smtp.gmail.com
   SMTP_PORT=587
   SMTP_USERNAME=actual-email@gmail.com
   SMTP_PASSWORD=actual-app-password
   SMTP_FROM_EMAIL=noreply@yourcompany.com
   ```

3. Deploy:
   ```bash
   docker-compose up -d
   ```

## Environment Variable Resolution

When Docker Compose runs, it resolves variables in this order:
1. `docker-compose.override.yml` values (if file exists)
2. `.env` file values
3. System environment variables
4. Default values in `docker-compose.yml`

## Files

- **`.env.example`**: Template with all required variables (committed to git)
- **`.env`**: Your actual values (DO NOT commit - in `.gitignore`)
- **`docker-compose.yml`**: Uses `${VARIABLE}` syntax to reference env vars
- **`docker-compose.override.yml`**: Local development overrides (optional, auto-loaded)

## Best Practices

1. ✅ Keep `.env.example` up-to-date with all required variables
2. ✅ Add `.env` to `.gitignore`
3. ✅ Use `docker-compose.override.yml` for local dev customization
4. ✅ Document all environment variables in `.env.example`
5. ❌ Never commit real credentials to git
6. ❌ Never hardcode secrets in `docker-compose.yml`

## Warnings

The warnings you see:
```
WARN[0000] The "SMTP_HOST" variable is not set. Defaulting to a blank string.
```

These are **normal and harmless** because:
- In local development, `docker-compose.override.yml` provides the values
- The warnings appear because the variables aren't in `.env` or system environment
- The override file values are used anyway (highest priority)

To eliminate warnings, create a `.env` file (optional for local dev).
