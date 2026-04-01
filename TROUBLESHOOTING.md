# Troubleshooting Guide

Common issues and their solutions for the ProjectX LEAN brokerage adapter.

---

## Connection Issues

### `ArgumentException: API key is required`
**Cause:** `brokerage-project-x-api-key` is empty or not set.  
**Fix:** Set the value in `config.json` or use the environment variable:
```bash
export QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
```

### `ArgumentException: API secret is required`
**Cause:** `brokerage-project-x-api-secret` is empty or not set.  
**Fix:**
```bash
export QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
```

### `ArgumentException: Invalid environment`
**Cause:** `brokerage-project-x-environment` must be exactly `"sandbox"` or `"production"` (case-sensitive).  
**Fix:** Check the spelling. The current implementation is case-sensitive.

### `ArgumentException: Invalid reconnect attempts`
**Cause:** `brokerage-project-x-reconnect-attempts` is outside the valid range [1, 20].  
**Fix:** Use a value between 1 and 20 (default: 3).

### Brokerage connects but immediately disconnects
**Possible causes:**
- Invalid or expired API credentials
- Wrong environment (`sandbox` vs `production`)
- Account ID mismatch — verify `brokerage-project-x-account-id`

---

## Order Issues

### `PlaceOrder` returns `false` for a valid order
**Possible causes:**
1. **Not connected** — call `Connect()` before placing orders, or check `IsConnected`.
2. **Zero quantity** — quantity must be non-zero.
3. **Invalid symbol** — only `SecurityType.Future` is supported; equity/crypto/option symbols are rejected.
4. **Canonical symbol** — pass a dated futures symbol (e.g., `ESH25`), not a canonical root (e.g., `ES XXXXX`).
5. **Expired contract** — contracts with expiry in the past are rejected.

### Limit order with price `0` is rejected
Limit and stop prices must be > 0. A limit price of 0 is treated as invalid.

### `OrderStatus.Invalid` received after `PlaceOrder` returns `true`
The order was accepted locally but rejected by the exchange. Check:
- Order size vs. account buying power
- Price tick increments (ES tick = $12.50, 0.25 pts)
- Market hours (futures have specific session windows)

### Order filled at unexpected price
Review the `OrderFillPrice` in the `OrderEvent`. For market orders this is expected. For limit orders, verify the limit price was achievable at the time.

---

## Data / Streaming Issues

### No data received after `Subscribe()`
**Possible causes:**
- Brokerage is not connected — `Subscribe` returns an empty enumerator when disconnected.
- Market is closed — futures have session-specific trading hours.
- Symbol not found on ProjectX — call `LookupSymbols()` to verify availability.

### History returns `null` for valid parameters
`null` is returned when the symbol's `SecurityType` is not supported (only `Future` is supported). Verify you are not passing an equity or crypto symbol.

### History bars are not in chronological order
The `BrokerageHistoryProvider` does not sort results. If you require sorted data, sort the returned `Slice[]` by `.Time` yourself.

---

## Testing Issues

### All Integration / BrokerageTests tests are skipped with "Skipping: brokerage-project-x-api-key not configured"
This is expected behavior in CI environments without credentials. Set the environment variable to run these tests locally:
```bash
export QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
export QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
export QC_BROKERAGE_PROJECT_X_ENVIRONMENT="sandbox"
export QC_BROKERAGE_PROJECT_X_ACCOUNT_ID="12345"
```
Then run:
```bash
dotnet test --filter "Category=Integration"
```

### Performance tests are not running
Performance tests use `[Explicit]` and must be run explicitly:
```bash
dotnet test --filter "Category=Performance"
```

### `coverlet.msbuild` not collecting coverage
Run with the correct MSBuild property:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/
```
To exclude integration tests from the coverage run:
```bash
dotnet test /p:CollectCoverage=true --filter "Category!=Integration&Category!=Performance"
```

---

## Symbol Mapping Issues

### `KeyNotFoundException` from `GetBrokerageSymbol`
The symbol's root ticker is not in `_rootToMarket` in `ProjectXSymbolMapper`. Add the mapping for the new futures root.

### `FormatException` from `GetLeanSymbol`
The brokerage contract ID does not match the expected format `<ROOT><MONTH_CODE><2-DIGIT-YEAR>` (e.g., `ESH25`). Verify the API is returning the expected format.

---

## Build Issues

### `error CS0246: The type or namespace name 'MarqSpec' could not be found`
The `MarqSpec.Client.ProjectX` library is not restored. Ensure the dependency is cloned and referenced:
```bash
cd ..
git clone https://github.com/adammarquette/MarqSpec.Client.ProjectX.git
cd Lean.Brokerages.ProjectX
dotnet restore
```

### `error CS0246: The type or namespace name 'QuantConnect' could not be found`
The LEAN engine is not cloned alongside this repository. Clone it:
```bash
cd ..
git clone https://github.com/adammarquette/Lean.git
```

---

## Logging

Enable verbose logging to diagnose issues:
```json
{
  "log-handler": "QuantConnect.Logging.CompositeLogHandler",
  "debug-mode": true
}
```
All brokerage operations are logged with the `ProjectXBrokerage.*` prefix.
