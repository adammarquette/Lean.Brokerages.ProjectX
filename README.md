# ProjectX Lean Brokerage

[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading.

**Status:** Phases 1–9 Complete | Production Release (Phase 10) In Progress  
**Last updated:** March 30, 2026  
**Project lead:** Marquette Speculations  
**Repository:** https://github.com/adammarquette/Lean.Brokerages.ProjectX

**Important:** This brokerage is designed for systematic and algorithmic trading strategies. It is **not optimized for high-frequency trading (HFT)** requiring sub-10ms latency or >100 orders/sec throughput.

---

## Project Scope & Roadmap

This project provides a LEAN-compatible brokerage implementation that enables backtesting and live trading of **futures contracts** on ProjectX. The architecture is designed for future expansion to additional asset classes.

### Current State
- **Phases 1–9 Complete:**
  - Core trading, account sync, symbol mapping, brokerage model, live data, historical data, ToolBox bulk download, fee modeling, and comprehensive test suite are all implemented.
- **In Progress:**
  - Phase 10: Production release & LEAN integration

### Supported
- Futures contracts (CME, CBOT, NYMEX, ICE)
- Market, Limit, Stop Market, Stop Limit, Trailing Stop order types
- Real-time and historical data
- Thread-safe, bidirectional order ID mapping
- Round-turn fee table for 57 futures products

### Not Supported (yet)
- Market On Open/Close, Option Exercise, Bracket/OCO
- Asset classes other than futures (planned for future phases)
- Level II (market depth) data

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
- **ARCHITECTURE.md** - Component design and data flow
- **TROUBLESHOOTING.md** - Common problems and fixes
- **CONTRIBUTING.md** - Development workflow and PR checklist
- **DIAGRAMS.md** - Architecture diagrams
- **CICD.md** - CI/CD documentation

---

## Algorithm Example

The following minimal LEAN algorithm trades ES front-month futures on ProjectX:

```csharp
using System;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

public class ESMomentumAlgorithm : QCAlgorithm
{
    private Symbol _es;
    private SimpleMovingAverage _fast;
    private SimpleMovingAverage _slow;

    public override void Initialize()
    {
        SetStartDate(2024, 1, 1);
        SetEndDate(2025, 1, 1);
        SetCash(50_000);

        // Subscribe to front-month E-mini S&P 500 futures
        _es = AddFuture("ES", Resolution.Minute,
            dataNormalizationMode: DataNormalizationMode.BackwardsRatio).Symbol;

        _fast = SMA(_es, 20, Resolution.Minute);
        _slow = SMA(_es, 60, Resolution.Minute);
    }

    public override void OnData(Slice data)
    {
        if (!_fast.IsReady || !_slow.IsReady) return;

        if (!Portfolio.Invested && _fast > _slow)
            MarketOrder(_es, 1);          // long 1 contract

        if (Portfolio.Invested && _fast < _slow)
            Liquidate(_es);
    }
}
```

Point the algorithm at the ProjectX brokerage by setting `"brokerage": "ProjectXBrokerage"` in `config.json`.

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

### Bulk Data Downloads (Phase 7)
- ToolBox CLI for bulk historical data download
- Exchange info downloader

### Fee Modeling (Phase 8)
- Round-turn commission table for 57 futures products (CME, CBOT, NYMEX, ICE)
- Per-side fee calculation integrated into `ProjectXBrokerageModel`

### Comprehensive Testing & Documentation (Phase 9)
- LEAN `BrokerageTests` base-class integration (ES futures, 5 order types)
- Moq-based unit tests for universe provider, order management, fee model
- Thread-safety and concurrent-order performance regression tests
- Coverage tooling via Coverlet
- ARCHITECTURE.md, TROUBLESHOOTING.md, CONTRIBUTING.md

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

See the [LICENSE](LICENSE) file.
