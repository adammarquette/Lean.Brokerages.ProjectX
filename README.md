# ProjectX Lean Brokerage

[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading (Phase 1). This repository implements the scaffolding and roadmap for connecting LEAN to the ProjectX trading platform.

**Status**: Phases 1–6 Complete | API Refinements (PR #28 "Fix Gaps")  
**Last updated**: March 29, 2026  
**Project lead**: Marquette Speculations  
**Repository**: https://github.com/adammarquette/Lean.Brokerages.ProjectX

**Important:** This brokerage is designed for systematic and algorithmic trading strategies. It is **not optimized for high-frequency trading (HFT)** requiring sub-10ms latency or greater than 100 orders per second throughput.

Overview
--------
This project provides a LEAN-compatible brokerage implementation that enables backtesting and live trading of futures contracts on ProjectX. Phase 1 delivers the project structure, basic factory and brokerage stubs, and documentation. Subsequent phases will add order execution, symbol mapping, real-time market data, historical data, and full production readiness.

Goals
- Implement LEAN brokerage interfaces (IBrokerageFactory, IBrokerage, ISymbolMapper, IDataQueueHandler, IHistoryProvider, etc.)
- Support futures trading (symbol mapping, contract specs, margin rules)
- Provide historical and real-time data support
- Deliver production-ready code with tests and CI

Quick Start
-----------

### Prerequisites
- **.NET 10 SDK (Preview)** or later ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **LEAN Engine** repository cloned at `../Lean` relative to this repository

### Setup
```bash
# Clone ProjectX Brokerage
git clone https://github.com/adammarquette/Lean.Brokerages.ProjectX.git
cd Lean.Brokerages.ProjectX

# Clone LEAN Engine (required dependency)
cd ..
git clone https://github.com/adammarquette/Lean.git
cd Lean.Brokerages.ProjectX

# Restore and build
dotnet restore
dotnet build

# Run tests (excludes tests requiring API credentials)
dotnet test --filter "Category!=RequiresApiCredentials&Category!=Integration"
```

### Documentation
- **[PRD.md](PRD.md)** - Complete project requirements, architecture, and roadmap
- **[CONFIGURATION.md](CONFIGURATION.md)** - Detailed configuration guide
- **[DIAGRAMS.md](DIAGRAMS.md)** - Architecture diagrams and documentation
- **[CICD.md](CICD.md)** - Build system and CI/CD documentation

Configuration
-------------
Configuration is managed through LEAN's standard JSON configuration files. Copy `config.template.json` to create your own configuration file, then fill in your credentials.

**⚠️ NEVER commit credentials to source control!**

### Required Configuration
```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "data-queue-handler": "ProjectXBrokerage"
}
```

### Configuration Options

  - Connection validation
  - Ready for API integration
- Bidirectional thread-safe mapping between LEAN and ProjectX order IDs

# ProjectX Lean Brokerage

[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading.

**Status:** Phase 2 - Core Trading Implementation In Progress  
**Last updated:** March 30, 2026  
**Project lead:** Marquette Speculations  
**Repository:** https://github.com/adammarquette/Lean.Brokerages.ProjectX

**Important:** This brokerage is designed for systematic and algorithmic trading strategies. It is **not optimized for high-frequency trading (HFT)** requiring sub-10ms latency or >100 orders/sec throughput.

---

## Project Scope & Roadmap

This project provides a LEAN-compatible brokerage implementation that enables backtesting and live trading of **futures contracts** on ProjectX. The architecture is designed for future expansion to additional asset classes.

**Current Phase:**
- Phase 2: Core Trading (Order Management, Account Synchronization)

**Planned Phases:**
- Phase 3: Market Data (real-time & historical)
- Phase 4: Production Readiness & Expanded Asset Support

**Supported:**
- Futures contracts only (initial release)
- Market, Limit, Stop Market, Stop Limit order types

**Not Supported (yet):**
- Market On Open/Close, Option Exercise, Trailing Stop
- Asset classes other than futures

**Constraints:**
- ProjectX API rate limits may impact data/order throughput
- Trading restricted to ProjectX-supported futures market hours
- Requires .NET 10 and LEAN Engine

---

## User Stories & Success Criteria

**User Stories:**
- As a futures day trader, I want to use my LEAN algorithm with ProjectX for low-latency execution (<100ms order latency).
- As a researcher, I want to backtest strategies using ProjectX historical data (5+ years, all major futures contracts).

**Success Criteria:**
- [ ] All LEAN interface contracts fully implemented
- [ ] >90% code coverage in integration tests
- [ ] 30+ days paper trading without critical errors
- [ ] Live trading validation with real account
- [ ] Performance: <100ms order latency, <1s data lag

---

## Quick Start

### Prerequisites
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **LEAN Engine** repository cloned at `../Lean` relative to this repository

### Setup
```bash
# Clone ProjectX Brokerage
git clone https://github.com/adammarquette/Lean.Brokerages.ProjectX.git
cd Lean.Brokerages.ProjectX

# Clone LEAN Engine (required dependency)
cd ..
git clone https://github.com/adammarquette/Lean.git
cd Lean.Brokerages.ProjectX

# Restore and build
dotnet restore
dotnet build

# Run tests (excludes tests requiring API credentials)
dotnet test --filter "Category!=RequiresApiCredentials&Category!=Integration"
```

### Documentation
- **[PRD.md](PRD.md)** - Project requirements, architecture, and roadmap
- **[CONFIGURATION.md](CONFIGURATION.md)** - Configuration guide
- **[DIAGRAMS.md](DIAGRAMS.md)** - Architecture diagrams
- **[CICD.md](CICD.md)** - CI/CD documentation

---

## Configuration

Configuration is managed through LEAN's standard JSON configuration files. Copy `config.template.json` to create your own configuration file, then fill in your credentials.

**⚠️ NEVER commit credentials to source control!**

### Required Configuration
```json
{
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "data-queue-handler": "ProjectXBrokerage"
}
```

### Environment Variables
All configuration keys can be set via environment variables using the `QC_` prefix:
```bash
export QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
export QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
export QC_BROKERAGE_PROJECT_X_ENVIRONMENT="sandbox"
```

### Configuration Files
- `config-schema.json` - JSON Schema for validation
- `config.template.json` - Template with all options documented
- See `PRD.md` for detailed configuration examples

---

## Features Implemented

### Order Management (Phase 2)
- **PlaceOrder()**: Market, Limit, Stop Market, Stop Limit
- **CancelOrder()**: Cancel pending orders
- **GetOpenOrders()**: Retrieve open orders
- **Order ID Mapping**: Thread-safe, bidirectional
- **Order Validation**: Symbol, quantity, type, price, connection state

### Account Synchronization (Phase 2)
- **GetAccountHoldings()**: Retrieve positions
- **GetCashBalance()**: Retrieve cash balances (multi-currency)
- **ReconcilePositions()**: Sync LEAN state with ProjectX
- **SubscribeToAccountUpdates()**: WebSocket account events
- **HandleAccountUpdate()**: Real-time balance/position updates

### Test Coverage
- 29 unit tests (5 executable, 24 pending ProjectX client integration)
- Tests for holdings, cash, account updates, reconciliation

---

## Technical Architecture

Implements LEAN brokerage interfaces:
- `IBrokerageFactory`: Instantiates/configures brokerage
- `IBrokerage`: Order placement, account sync, connection, events
- `ISymbolMapper`: Futures symbol translation (LEAN ↔ ProjectX)
- `IBrokerageModel`: Order types, margin, contract specs, market hours

**Pending:** MarqSpec.Client.ProjectX integration for live API connectivity

---

## Development Guidance

- Start from provided stubs; follow LEAN brokerage examples (TradeStation, Bybit, Binance)
- Implement `GetHistory()` early to validate symbol mapping
- Keep authentication isolated (token refresh, OAuth)
- Use `BaseWebsocketsBrokerage` for streaming
- Add unit tests for every feature

---

## Resources

- LEAN engine: https://github.com/QuantConnect/Lean
- PRD and architecture: `PRD.md`
- MarqSpec ProjectX client (internal): https://github.com/adammarquette/MarqSpec.Client.ProjectX

---

## Contributing

Please open issues and PRs against this repository. Follow the branching workflow:

**Branching Strategy:** `master` ← `develop` ← `feature-branch`
- `master`: Release branch (production-ready)
- `develop`: Active development
- Feature branches: Branch off from `develop`, merge back via PR

**Workflow:**
1. Create feature branch from `develop`: `git checkout -b feature/my-feature develop`
2. Make changes and commit
3. Open PR against `develop`
4. After review, merge to `develop`
5. Periodic releases merge `develop` → `master`

**Important:** Do not commit any secret or credential material.

---

## License

See the repository `LICENSE` file.
