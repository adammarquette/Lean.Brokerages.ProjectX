Here is the updated README.md content reflecting the current state of the project and PRD.md:

---

# ProjectX Lean Brokerage

[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading.

**Status:** Phases 1–6 Complete | API Refinements (PR #28 "Fix Gaps")  
**Last updated:** March 30, 2026  
**Project lead:** Marquette Speculations  
**Repository:** https://github.com/adammarquette/Lean.Brokerages.ProjectX

**Important:** This brokerage is designed for systematic and algorithmic trading strategies. It is **not optimized for high-frequency trading (HFT)** requiring sub-10ms latency or >100 orders/sec throughput.

---

## Project Scope & Roadmap

This project provides a LEAN-compatible brokerage implementation that enables backtesting and live trading of **futures contracts** on ProjectX. The architecture is designed for future expansion to additional asset classes.

### Current State
- **Phases 1–6 Complete:**
  - Core trading, account sync, symbol mapping, brokerage model, live data, and historical data are implemented and tested.
- **In Progress:**
  - Phase 7: Bulk data downloads (ToolBox)
  - Phase 8: Fee modeling
  - Phase 9: Comprehensive testing & documentation
  - Phase 10: Production release & LEAN integration

### Supported
- Futures contracts (CME, CBOT, NYMEX, ICE)
- Market, Limit, Stop Market, Stop Limit order types
- Real-time and historical data
- Thread-safe, bidirectional order ID mapping

### Not Supported (yet)
- Market On Open/Close, Option Exercise, Bracket/OCO, Trailing Stop
- Asset classes other than futures (planned for future phases)

### Constraints
- ProjectX API rate limits may impact data/order throughput
- Trading restricted to ProjectX-supported futures market hours
- Requires .NET 10 and LEAN Engine

---

## User Stories & Success Criteria

**User Stories:**
- As a futures day trader, I want to use my LEAN algorithm with ProjectX for low-latency execution (<100ms order latency).
- As a researcher, I want to backtest strategies using ProjectX historical data (5+ years, all major futures contracts).

**Success Criteria:**
- [x] All LEAN interface contracts fully implemented (Phases 2–6)
- [ ] >90% code coverage in integration tests
- [ ] 30+ days paper trading without critical errors
- [ ] Live trading validation with real account
- [ ] Performance: <100ms order latency, <1s data lag

---

## Quick Start

### Prerequisites
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **LEAN Engine** repository cloned at Lean relative to this repository

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
- **PRD.md** - Project requirements, architecture, and roadmap
- **CONFIGURATION.md** - Configuration guide
- **DIAGRAMS.md** - Architecture diagrams
- **CICD.md** - CI/CD documentation

---

## Configuration

Configuration is managed through LEAN's standard JSON configuration files. Copy config.template.json to create your own configuration file, then fill in your credentials.

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
- config-schema.json - JSON Schema for validation
- config.template.json - Template with all options documented
- See PRD.md for detailed configuration examples

---

## Features Implemented

### Core Trading & Account Sync (Phases 1–2)
- Order placement, cancellation, and open order retrieval
- Account holdings and cash balance sync
- Real-time account update events
- Thread-safe order ID mapping

### Symbol Mapping (Phase 3)
- Full futures symbol support (standard, continuous, edge cases)
- Market/exchange mapping (CME, CBOT, NYMEX, ICE)

### Brokerage Model (Phase 4)
- Order type and time-in-force validation
- Margin, leverage, and buying power calculations
- Market hours and fee model integration

### Live Data Streaming (Phase 5)
- Real-time tick and quote data via WebSocket
- Data aggregation and subscription management

### Historical Data (Phase 6)
- Backtesting and warm-up data retrieval
- Multiple resolutions (second, minute, hour, daily)

### In Progress
- Bulk data downloads (ToolBox)
- Fee modeling (commission, exchange, regulatory, slippage)
- Comprehensive test suite and documentation

---

## Known Limitations

- **Not optimized for HFT:** Not suitable for strategies requiring <10ms latency or >100 orders/sec
- **Futures only:** Initial release supports only futures contracts; other asset classes planned for future phases
- **Order types:** Bracket, OCO, trailing stop, and advanced order types not yet supported
- **API rate limits:** Default 10 orders/sec, 50 data requests/sec (configurable)
- **Market hours:** Trading restricted to ProjectX-supported futures market hours
- **Data:** No Level II (market depth) data in initial phases

---

## Development & Contribution

Please open issues and PRs against this repository. Follow the branching workflow:

**Branching Strategy:** `master` ← `develop` ← `feature/*`
- `master`: Production-ready code
- `develop`: Active development
- `feature/*`: Feature branches off `develop`

**Workflow:**
1. Create feature branch from `develop`: `git checkout -b feature/my-feature develop`
2. Make changes and commit
3. Open PR against `develop`
4. After review, merge to `develop`
5. Periodic releases merge `develop` → `master`

**Important:** Do not commit any secret or credential material.

---

## References & Resources

- [LEAN engine](https://github.com/QuantConnect/Lean)
- PRD.md - Requirements, architecture, and roadmap
- [MarqSpec ProjectX client](https://github.com/adammarquette/MarqSpec.Client.ProjectX)

---

## License

See the repository `LICENSE` file.