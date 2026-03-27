# ProjectX Brokerage Configuration Guide

This guide provides detailed information about configuring the ProjectX brokerage integration for LEAN.

## Quick Start

1. Copy `config.template.json` to create your configuration file
2. Fill in your ProjectX API credentials
3. Choose your environment (sandbox or production)
4. Optionally adjust advanced settings

**⚠️ Security Warning: NEVER commit credentials to source control!**

## Configuration Methods

### Method 1: JSON Configuration File

Create a configuration file (e.g., `config.json`):

```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "brokerage-project-x-environment": "sandbox",
  "data-queue-handler": "ProjectXBrokerage"
}
```

### Method 2: Environment Variables

Set configuration via environment variables with the `QC_` prefix:

```bash
# Linux/macOS
export QC_BROKERAGE="ProjectXBrokerage"
export QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
export QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
export QC_BROKERAGE_PROJECT_X_ENVIRONMENT="sandbox"

# Windows PowerShell
$env:QC_BROKERAGE = "ProjectXBrokerage"
$env:QC_BROKERAGE_PROJECT_X_API_KEY = "your-api-key"
$env:QC_BROKERAGE_PROJECT_X_API_SECRET = "your-api-secret"
$env:QC_BROKERAGE_PROJECT_X_ENVIRONMENT = "sandbox"
```

### Method 3: Docker Environment

```dockerfile
ENV QC_BROKERAGE="ProjectXBrokerage"
ENV QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
ENV QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
ENV QC_BROKERAGE_PROJECT_X_ENVIRONMENT="production"
```

## Configuration Reference

### Required Settings

| Key | Description | Example |
|-----|-------------|---------|
| `brokerage` | Brokerage identifier | `"ProjectXBrokerage"` |
| `brokerage-project-x-api-key` | Your ProjectX API key | `"abc123..."` |
| `brokerage-project-x-api-secret` | Your ProjectX API secret | `"xyz789..."` |

### Environment Settings

| Key | Options | Default | Description |
|-----|---------|---------|-------------|
| `brokerage-project-x-environment` | `"sandbox"`, `"production"` | `"production"` | Trading environment |

**Sandbox Environment:**
- Use for development and testing
- No real money at risk
- Separate API credentials
- Limited market data availability

**Production Environment:**
- Live trading with real funds
- Full market data access
- Requires verified account

### Data Provider Settings

| Key | Value | Description |
|-----|-------|-------------|
| `data-queue-handler` | `"ProjectXBrokerage"` | Use ProjectX for real-time data |
| `history-provider` | `"ProjectXBrokerage"` | Use ProjectX for historical data |

### Rate Limiting Settings

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `brokerage-project-x-order-rate-limit` | integer | `10` | 1-100 | Max orders/second |
| `brokerage-project-x-data-rate-limit` | integer | `50` | 1-1000 | Max API requests/second |
| `brokerage-project-x-max-subscriptions` | integer | `100` | 1-500 | Max concurrent subscriptions |

**Rate Limiting Strategy:**
- Token bucket algorithm prevents API limit violations
- Exceeded limits trigger automatic request queuing
- Adjust based on your API tier and trading strategy

**Recommendations by Trading Style:**
- **Day Trading:** order-rate-limit: 10-15, data-rate-limit: 50-100
- **Systematic Trading:** order-rate-limit: 5-10, data-rate-limit: 30-50
- **Swing Trading:** order-rate-limit: 3-5, data-rate-limit: 20-30

**Important:** This brokerage is not designed for high-frequency trading (HFT). The architecture prioritizes reliability and data integrity over ultra-low latency execution.

### Connection Settings

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `brokerage-project-x-reconnect-attempts` | integer | `5` | 1-20 | Reconnection attempts |
| `brokerage-project-x-reconnect-delay` | integer | `1000` | 100-60000 | Initial delay (ms) |
| `brokerage-project-x-request-timeout` | integer | `30000` | 1000-300000 | HTTP timeout (ms) |
| `brokerage-project-x-websocket-timeout` | integer | `5000` | 1000-60000 | WebSocket timeout (ms) |

**Reconnection Strategy:**
- Exponential backoff: delay doubles after each attempt
- WebSocket automatically reconnects on disconnect
- Connection state events emitted for monitoring

### Logging Settings

| Key | Type | Default | Options | Description |
|-----|------|---------|---------|-------------|
| `brokerage-project-x-enable-logging` | boolean | `true` | true/false | Enable logging |
| `brokerage-project-x-log-level` | string | `"Information"` | See below | Log verbosity |

**Log Levels:**
- `"Trace"`: Every API call, WebSocket message (verbose)
- `"Debug"`: Connection events, subscriptions, orders
- `"Information"`: Successful operations, state changes
- `"Warning"`: Rate limits, retry attempts
- `"Error"`: Failed operations, exceptions
- `"Critical"`: Fatal errors

**Logging Best Practices:**
- Use `"Debug"` or `"Trace"` during development
- Use `"Information"` in production
- Enable `"Debug"` when troubleshooting issues
- Logs never contain API keys or secrets

### Custom Endpoint Settings

| Key | Type | Description |
|-----|------|-------------|
| `brokerage-project-x-api-url` | string (URL) | Custom REST API base URL |
| `brokerage-project-x-websocket-url` | string (URL) | Custom WebSocket URL |

**When to Use:**
- Testing against custom API endpoints
- Using API proxy or gateway
- Regional API endpoints (if available)

**Default Endpoints:**

Production:
- REST API: `https://api.projectx.com/v1`
- WebSocket: `wss://stream.projectx.com/v1`

Sandbox:
- REST API: `https://api-sandbox.projectx.com/v1`
- WebSocket: `wss://stream-sandbox.projectx.com/v1`

## Configuration Examples

### Example 1: Basic Live Trading

```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "pk_live_abc123",
  "brokerage-project-x-api-secret": "sk_live_xyz789",
  "data-queue-handler": "ProjectXBrokerage"
}
```

### Example 2: Development with Sandbox

```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "pk_test_abc123",
  "brokerage-project-x-api-secret": "sk_test_xyz789",
  "brokerage-project-x-environment": "sandbox",
  "brokerage-project-x-enable-logging": true,
  "brokerage-project-x-log-level": "Debug",
  "data-queue-handler": "ProjectXBrokerage"
}
```

### Example 3: Backtesting Only

```json
{
  "brokerage-project-x-api-key": "pk_live_abc123",
  "brokerage-project-x-api-secret": "sk_live_xyz789",
  "history-provider": "ProjectXBrokerage"
}
```

### Example 4: Multi-Data Source

```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "pk_live_abc123",
  "brokerage-project-x-api-secret": "sk_live_xyz789",
  "data-queue-handler": "ProjectXBrokerage",
  "history-provider": "QuantConnect"
}
```

## Security Best Practices

### 1. Credential Management

**DO:**
- ✅ Use environment variables for credentials
- ✅ Store credentials in secure vault (AWS Secrets Manager, Azure Key Vault)
- ✅ Use separate credentials for dev, test, and production
- ✅ Rotate API keys regularly (quarterly)
- ✅ Restrict API key permissions to minimum required

**DON'T:**
- ❌ Commit credentials to Git
- ❌ Share credentials in chat/email
- ❌ Use production credentials for testing
- ❌ Store credentials in plain text files
- ❌ Use overly permissive API keys

### 2. API Key Permissions

Configure ProjectX API keys with minimum required permissions:
- Trading keys: `orders:write`, `positions:read`
- Data keys: `market_data:read`
- Separate keys for trading vs data access

### 3. Configuration File Security

```bash
# Set restrictive file permissions
chmod 600 config.json

# Add to .gitignore
echo "config.json" >> .gitignore
echo "config.*.json" >> .gitignore
```

### 4. Environment-Specific Credentials

Maintain separate credentials for each environment:

```
config.sandbox.json    # Development
config.staging.json    # Testing
config.production.json # Live trading
```

Add all to `.gitignore`:
```gitignore
config.*.json
!config.template.json
```

## Troubleshooting

### Issue: "Invalid API Key"

**Symptoms:** Authentication failures, 401 errors

**Solutions:**
1. Verify API key is correct (no extra spaces/newlines)
2. Check environment matches key (sandbox vs production)
3. Ensure API key has not expired
4. Verify key has required permissions

### Issue: "Rate Limit Exceeded"

**Symptoms:** 429 errors, delayed order execution

**Solutions:**
1. Reduce `order-rate-limit` setting
2. Implement order batching in algorithm
3. Contact ProjectX to increase limits
4. Check for infinite loops in algorithm

### Issue: "Connection Timeout"

**Symptoms:** WebSocket disconnections, failed API calls

**Solutions:**
1. Check network connectivity
2. Increase `request-timeout` setting
3. Verify firewall allows WebSocket connections
4. Check ProjectX API status page

### Issue: "Too Many Subscriptions"

**Symptoms:** Subscription failures, missing data

**Solutions:**
1. Reduce number of symbols subscribed
2. Increase `max-subscriptions` setting
3. Unsubscribe from unused symbols
4. Consolidate similar instruments

## Validation

### Schema Validation

Validate your configuration against the JSON schema:

```bash
# Using ajv-cli
npm install -g ajv-cli
ajv validate -s config-schema.json -d config.json

# Using jsonschema (Python)
pip install jsonschema
jsonschema -i config.json config-schema.json
```

### Configuration Testing

Test configuration before live trading:

```csharp
// In your algorithm
public override void Initialize()
{
    // Verify brokerage is ProjectX
    if (Brokerage.Name != "ProjectXBrokerage")
    {
        throw new Exception("Expected ProjectXBrokerage");
    }
    
    // Log configuration (non-sensitive)
    Log($"Environment: {BrokerageModel.AccountType}");
    Log($"Connected: {Brokerage.IsConnected}");
}
```

## Additional Resources

- [PRD.md](./PRD.md) - Complete project documentation
- [config-schema.json](./config-schema.json) - JSON Schema for validation
- [config.template.json](./config.template.json) - Configuration template
- [LEAN Configuration Documentation](https://www.quantconnect.com/docs/v2/lean-cli/configuration)
- [ProjectX API Documentation](#) - Official API docs

## Support

For configuration issues:
1. Check this guide first
2. Review LEAN logs for error messages
3. Verify credentials and API status
4. Open GitHub issue with sanitized logs (no credentials!)

---

**Last Updated:** March 2026  
**Version:** 1.0
