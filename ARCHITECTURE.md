# Architecture

This document describes the design of the ProjectX LEAN brokerage adapter: component boundaries, data flows, key design decisions, and extension points.

---

## Overview

The adapter bridges two APIs:

| Side | Technology |
|------|-----------|
| **LEAN Engine** | C# interfaces (`IBrokerage`, `IDataQueueHandler`, `IHistoryProvider`, `IDataQueueUniverseProvider`) |
| **ProjectX** | REST API (order management, historical data) + WebSocket (live data, order updates) |

```
┌─────────────────────────────────────────────────────┐
│                  LEAN Algorithm                      │
│  PlaceOrder / Subscribe / GetHistory / LookupSymbols │
└───────────────────┬─────────────────────────────────┘
                    │  LEAN interfaces
┌───────────────────▼─────────────────────────────────┐
│              ProjectXBrokerage (partial)              │
│  ┌──────────────┐  ┌──────────────────────────────┐  │
│  │ IBrokerage   │  │ IDataQueueHandler            │  │
│  │ IHistoryProv │  │ IDataQueueUniverseProvider   │  │
│  └──────┬───────┘  └─────────────┬────────────────┘  │
│         │                        │                    │
│  ┌──────▼────────────────────────▼────────────────┐  │
│  │          ProjectXSymbolMapper                   │  │
│  │     LEAN Symbol ↔ "ESH25" contract ID           │  │
│  └─────────────────────────────────────────────────┘  │
└───────────────────┬─────────────────────────────────┘
                    │  MarqSpec.Client.ProjectX SDK
          ┌─────────┴──────────┐
          │                    │
  ┌───────▼──────┐    ┌────────▼───────┐
  │  REST API    │    │   WebSocket    │
  │  (orders,    │    │   (live ticks, │
  │   history)   │    │  order events) │
  └──────────────┘    └────────────────┘
```

---

## Partial Class Structure

`ProjectXBrokerage` is split across four files to keep concerns separated:

| File | Interface implemented |
|------|-----------------------|
| `ProjectXBrokerage.cs` | `IBrokerage`, `IHistoryProvider` |
| `ProjectXBrokerage.DataQueueHandler.cs` | `IDataQueueHandler` |
| `ProjectXBrokerage.DataQueueUniverseProvider.cs` | `IDataQueueUniverseProvider` |
| `ProjectXBrokerageFactory.cs` | `IBrokerageFactory` |

---

## Key Classes

### `ProjectXBrokerage`

Responsibilities:
- Connect / disconnect / reconnect lifecycle
- Place, cancel, update orders via REST
- Fire `OrdersStatusChanged`, `AccountChanged`, `Message` events
- Retrieve history via `GetHistory()`

State fields:
- `_isConnected` — `volatile bool`, read by multiple threads
- `_apiClient` — `IProjectXApiClient`, injected
- `_wsClient` — `IProjectXWebSocketClient`, injected
- `_symbolMapper` — `ProjectXSymbolMapper`
- `_orderIdMap` — `ConcurrentDictionary<int, string>` LEAN order ID → brokerage order ID

### `ProjectXSymbolMapper`

Converts between:
- LEAN `Symbol` (e.g., `ES YMAD5U5BZGV1` with `ID.Symbol = "ESH25"`) 
- ProjectX contract ID string (e.g., `"ESH25"`)

Format: `<ROOT> <MONTH_CODE> <2-DIGIT-YEAR>`, using CME month codes (H = March, M = June, U = September, Z = December, etc.).

### `ProjectXBrokerageModel`

Configures LEAN's strategy layer:
- Supported order types: Market, Limit, StopMarket, StopLimit, TrailingStop
- Time in force: Day, GTC
- `GetFeeModel()` → `ProjectXFeeModel`

### `ProjectXFeeModel`

Round-turn commission table keyed by futures root ticker. Falls back to `$5.00` RT for unknown roots. Uses half the RT fee per side (buy or sell), so two fills on the same futures equal the listed RT rate.

---

## Order Lifecycle

```
Algorithm                  ProjectXBrokerage          ProjectX REST/WS
    │                            │                          │
    │──PlaceOrder(Order)─────────►│                          │
    │                             │──PlaceOrderAsync()──────►│
    │                             │◄─────────────────────────│ PlaceOrderResponse
    │                             │  maps brokerage order ID │
    │                             │◄─────────────────────────│ WebSocket OrderUpdate
    │                             │  fires OrdersStatusChanged│
    │◄────────OrderEvent──────────│                          │
```

- `PlaceOrder` returns `true` if the REST call succeeded; the subsequent fill/cancel events arrive via WebSocket.
- `CancelOrder` sends a REST cancellation request; the `Canceled` event arrives via WebSocket.

---

## Data Flow: Live Streaming

```
Subscribe(config)                     WebSocket
    │                                    │
    │──Subscribe(symbol)────────────────►│
    │◄───────────────────────────────────│ tick/quote stream
    │  DataAggregator.Update()           │
    │◄──BaseData enumerator──────────────│
```

Subscriptions are thread-safe. Multiple symbols can be subscribed simultaneously. `Unsubscribe()` removes only the specified config without affecting others.

---

## Data Flow: Historical Data

```
GetHistory(HistoryRequest)
    │
    ├── Convert LEAN Resolution → AggregateBarUnit
    ├── GET /history?contractId=ESH25&start=...&end=...
    └── Convert Bar[] → TradeBar[] / QuoteBar[] / Tick[]
```

Tick resolution is not natively supported by the REST API; the adapter returns minute bars when tick is requested and logs a warning.

---

## Thread Safety

| Operation | Thread model |
|-----------|-------------|
| `IsConnected` | `volatile bool` — safe for concurrent reads |
| `PlaceOrder` / `CancelOrder` | Sequential per LEAN constraint; no additional locking needed |
| `Subscribe` / `Unsubscribe` | `_subscriptions` guarded by `lock (_subscriptionsLock)` |
| `_orderIdMap` | `ConcurrentDictionary` |
| WebSocket callbacks | Fire on thread pool; events marshalled to caller via `OrdersStatusChanged` |

---

## Configuration

All runtime configuration is read from LEAN's `Config` singleton at connection time, not at construction time. This allows `TestSetup.ReloadConfiguration()` to inject test values before `Connect()`.

Key config keys:

| Key | Default | Description |
|-----|---------|-------------|
| `brokerage-project-x-api-key` | *(required)* | REST API key |
| `brokerage-project-x-api-secret` | *(required)* | REST API secret |
| `brokerage-project-x-environment` | `sandbox` | `sandbox` or `production` |
| `brokerage-project-x-account-id` | *(required)* | Account ID |
| `brokerage-project-x-reconnect-attempts` | `3` | Retry count [1–20] |
| `brokerage-project-x-reconnect-delay` | `2000` | Milliseconds between retries |
| `brokerage-project-x-connection-timeout` | `30000` | Connection timeout (ms) |

---

## Dependency Injection (Testing)

`ProjectXBrokerage` exposes an internal constructor for unit tests that accepts pre-built mock objects:

```csharp
new ProjectXBrokerage(
    IProjectXApiClient,
    IProjectXWebSocketClient,
    ProjectXSymbolMapper,
    IOrderProvider,
    ISecurityProvider,
    IDataAggregator)
```

This avoids live API calls in unit tests and allows Moq-based injection of all external dependencies.

---

## Extension Points

| To add… | Change here |
|---------|------------|
| New futures root | `ProjectXSymbolMapper._rootToMarket` |
| New order type support | `ProjectXBrokerage.ConvertOrderType()` + `ProjectXBrokerageModel` |
| New fee schedule | `ProjectXFeeModel._roundTurnFees` |
| New resolution support | `ProjectXBrokerage.ConvertResolution()` in history provider |
| New asset class | New partial file + `ProjectXBrokerageModel` |
