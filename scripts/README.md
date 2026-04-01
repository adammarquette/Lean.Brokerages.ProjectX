# Developer Utility Scripts

This directory contains utility scripts used during the development of the ProjectX brokerage integration. These scripts are intended for developers to apply specific code transformations and should not be part of the regular build process.

**⚠️ Warning:** These scripts directly modify the source code. Ensure you have a clean git status before running them.

## Scripts

### `fix_gethistory.ps1`

**Purpose:** Implements the `GetHistory()` method in `ProjectXBrokerage.cs`.

This script replaces the `NotImplementedException` in the `GetHistory` method with a full implementation that handles historical data requests for different resolutions (Second, Minute, Hour, Daily).

**What it does:**
- Checks if the `ProjectXBrokerage.cs` file contains the placeholder exception.
- Replaces the placeholder with a `try-catch` block that:
  - Validates the connection status.
  - Maps the LEAN symbol to the brokerage's contract ID.
  - Determines the appropriate bar unit and limit based on the request resolution.
  - Calls the API client to get historical bars.
  - Converts the bars to the LEAN `TradeBar` format.
  - Handles exceptions and logs errors.
- Adds a `ConvertHistoricalBars` helper method.

**How to run:**
```powershell
./scripts/fix_gethistory.ps1
```

### `fix_handlers.ps1`

**Purpose:** Adds and wires up real-time data handlers in `ProjectXBrokerage.cs`.

This script performs three main tasks to integrate real-time price and trade data handling:

1.  **Adds Event Handlers:** Inserts the `OnPriceUpdateReceived` and `OnTradeUpdateReceived` methods to process incoming WebSocket messages for quote and trade ticks.
2.  **Updates Cleanup Logic:** Modifies the `CleanupClients` method to unwire the new event handlers on disconnect.
3.  **Implements Resubscription:** Replaces a `TODO` comment in the `HandleReconnection` method with logic to automatically resubscribe to market data feeds after a WebSocket reconnection.

**How to run:**
```powershell
./scripts/fix_handlers.ps1
```

## Usage

These scripts were created to automate repetitive coding tasks during specific development phases. They serve as a record of the transformations applied but are not intended for general use unless you are re-applying these specific changes.

For the latest code, always refer to the main source files in the `QuantConnect.ProjectXBrokerage` directory.
