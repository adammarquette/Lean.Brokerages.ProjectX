# ProjectX Lean Brokerage

[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading (Phase 1). This repository implements the scaffolding and roadmap for connecting LEAN to the ProjectX trading platform.

**Status**: Phase 1 - Foundation Complete  
**Last updated**: March 25, 2026  
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

See the complete configuration documentation in `PRD.md` > Appendix > Configuration Schema for:
- All available configuration keys and their descriptions
- Environment-specific settings (sandbox vs production)
- Rate limiting and performance tuning
- Logging configuration
- Security best practices

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

Development guidance
--------------------
- Start from the factory and brokerage stubs provided. Follow LEAN brokerage examples (TradeStation, Bybit, Binance) for reference.
- Implement `GetHistory()` early to validate symbol mapping and history provider behavior.
- Keep authentication isolated in a dedicated class (token refresh, OAuth flows).
- Use `BaseWebsocketsBrokerage` and `BrokerageMultiWebSocketSubscriptionManager` when implementing streaming.
- Add unit tests for every implemented feature and aim for high coverage before live-trading components are introduced.

Resources
---------
- LEAN engine: https://github.com/QuantConnect/Lean
- PRD and architecture: `PRD.md`
- MarqSpec ProjectX client (internal): https://github.com/adammarquette/MarqSpec.Client.ProjectX

Contributing
------------
Please open issues and PRs against this repository. Follow the branching workflow:

**Branching Strategy**: `master` ← `develop` ← `feature-branch`

- **`master`**: Release branch (production-ready code)
- **`develop`**: Active development branch
- **Feature branches**: Branch off from `develop`, merge back to `develop` via PR

**Workflow**:
1. Create feature branch from `develop`: `git checkout -b feature/my-feature develop`
2. Make changes and commit
3. Open PR against `develop` branch
4. After review and approval, feature merges to `develop`
5. Periodic releases merge `develop` → `master`

**Important**: Do not commit any secret or credential material.

License
-------
See the repository `LICENSE` file.
