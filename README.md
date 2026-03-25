![lean-brokerage-template](https://user-images.githubusercontent.com/18473240/131904120-f67dab9c-cc6f-4c08-83e9-5d3ffafdb85d.png)

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
Test and local configuration should live in the test project config file. Example config keys (do NOT commit credentials):

```
{
  "brokerage-api-url": "https://api.projectx.example",
  "brokerage-app-key": "<app-key>",
  "brokerage-secret": "<secret>",
  "brokerage-refresh-token": "",
  "brokerage-redirect-url": "https://127.0.0.1"
}
```

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
