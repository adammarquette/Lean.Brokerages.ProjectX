# ProjectX Lean Brokerage

ProjectX integration for the QuantConnect LEAN Engine — a brokerage plugin focused on futures trading (Phase 1). This repository implements the scaffolding and roadmap for connecting LEAN to the ProjectX trading platform.

Status: Phase 1 - Foundation Complete
Last updated: March 25, 2026
Project lead: Marquette Speculations
Repository: https://github.com/adammarquette/Lean.Brokerages.ProjectX

Overview
--------
This project provides a LEAN-compatible brokerage implementation that enables backtesting and live trading of futures contracts on ProjectX. Phase 1 delivers the project structure, basic factory and brokerage stubs, and documentation. Subsequent phases will add order execution, symbol mapping, real-time market data, historical data, and full production readiness.

Goals
- Implement LEAN brokerage interfaces (IBrokerageFactory, IBrokerage, ISymbolMapper, IDataQueueHandler, IHistoryProvider, etc.)
- Support futures trading (symbol mapping, contract specs, margin rules)
- Provide historical and real-time data support
- Deliver production-ready code with tests and CI

Quick start
-----------
1. Clone the repository and open the solution targeting .NET 10.
2. Review `PRD.md` for project scope, architecture, and phased roadmap.
3. Build the solution: `dotnet build`
4. Run unit tests (when available): `dotnet test`

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
Please open issues and PRs against this repository. Follow standard GitHub workflows and do not commit any secret or credential material.

License
-------
See the repository `LICENSE` file.
