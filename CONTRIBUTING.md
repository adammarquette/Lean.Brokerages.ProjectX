# Contributing to Lean.Brokerages.ProjectX

Thank you for your interest in contributing! This document explains the project conventions, development workflow, and testing requirements.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Git | any recent |
| LEAN Engine | cloned at `../Lean` |
| MarqSpec.Client.ProjectX | cloned at `../MarqSpec.Client.ProjectX` |

---

## Repository Layout

```
Lean.Brokerages.ProjectX/
├── QuantConnect.ProjectXBrokerage/          # Production code
│   ├── ProjectXBrokerage.cs                 # IBrokerage + IDataQueueHandler (core)
│   ├── ProjectXBrokerage.DataQueueHandler.cs
│   ├── ProjectXBrokerage.DataQueueUniverseProvider.cs
│   ├── ProjectXBrokerageFactory.cs
│   ├── ProjectXBrokerageModel.cs            # IBrokerageModel
│   ├── ProjectXFeeModel.cs                  # IFeeModel (57-product RT table)
│   └── ProjectXSymbolMapper.cs              # ISymbolMapper
├── QuantConnect.ProjectXBrokerage.Tests/    # Test suite
│   ├── ProjectXBrokerageTests.cs            # BrokerageTests base class integration
│   ├── ProjectXBrokerage*Tests.cs           # Per-feature test fixtures
│   └── ProjectXBrokerageTestsHelper.cs      # Shared test utilities
├── QuantConnect.ProjectXBrokerage.ToolBox/  # Bulk-download CLI
├── PRD.md                                   # Requirements & roadmap
├── ARCHITECTURE.md                          # Design decisions
├── CONFIGURATION.md                         # Config reference
├── TROUBLESHOOTING.md                       # Common problems
└── CONTRIBUTING.md                          ← you are here
```

---

## Branching Strategy

```
master       ← production-ready, tagged releases
  └─ develop ← integration branch
       └─ feature/<name>   ← new features
       └─ fix/<name>       ← bug fixes
       └─ test/<name>      ← test-only changes
       └─ docs/<name>      ← documentation-only changes
```

1. Branch from `develop`, never from `master`.
2. Open a PR against `develop`.
3. `develop` → `master` is gated: all unit tests must pass, coverage must not regress.

---

## Development Workflow

```bash
# 1. Fork and clone
git clone https://github.com/<your-fork>/Lean.Brokerages.ProjectX.git
cd Lean.Brokerages.ProjectX

# 2. Ensure dependencies are cloned side-by-side
ls ../Lean                          # LEAN Engine
ls ../MarqSpec.Client.ProjectX      # ProjectX API client

# 3. Create your branch
git checkout -b feature/my-feature develop

# 4. Build
dotnet build

# 5. Run unit tests (no credentials required)
dotnet test --filter "Category!=Integration&Category!=Performance&Category!=RequiresApiCredentials"

# 6. Run integration tests (requires sandbox credentials)
export QC_BROKERAGE_PROJECT_X_API_KEY="..."
dotnet test --filter "Category=Integration"
```

---

## Test Categories

| Category | Credentials | Runs in CI | Command |
|----------|-------------|------------|---------|
| *(default, no category)* | No | Yes | `dotnet test --filter "Category!=Integration&Category!=Performance"` |
| `Integration` | Yes (sandbox) | No | `dotnet test --filter "Category=Integration"` |
| `RequiresApiCredentials` | Yes | No | `dotnet test --filter "Category=RequiresApiCredentials"` |
| `Performance` | No | No | `dotnet test --filter "Category=Performance"` |

**CI policy:** Only tests without credentials run in the CI pipeline. Integration tests are expected to be run locally before merging a PR that touches trading logic.

---

## Code Style

- Follow the existing code style (see `.editorconfig`).
- Use `PascalCase` for public methods and properties; `camelCase` with `_` prefix for private fields.
- Write XML doc comments only for `public` API members.
- Do not add comments for obvious code — prefer clear names.
- All LEAN interface method implementations must match the base signature exactly.

---

## Adding a New Futures Symbol

1. Open `ProjectXSymbolMapper.cs`.
2. Add the root → market mapping in `_rootToMarket`.
3. If the exchange is new, add it to `_marketToExchange`.
4. Add a corresponding test case in `ProjectXBrokerageSymbolMapperTests.cs`.

---

## Adding a New Order Type

1. Map the LEAN `OrderType` in `ProjectXBrokerage.cs` → `ConvertOrderType()`.
2. Map the order direction in `ConvertOrderDirection()` if needed.
3. Add at least one unit test in `ProjectXBrokerageOrderValidationTests.cs` for the new type.
4. Update `ProjectXBrokerageModel.cs` to reflect whether the order type is supported.

---

## Updating the Fee Table

1. Edit `ProjectXFeeModel.cs` → `_roundTurnFees` dictionary.
2. Update `ProjectXFeeModel.GetOrderFee()` if the fee calculation changes.
3. Add or update test cases in `ProjectXBrokerageModelTests.cs`.

---

## Code Coverage

Run coverage locally and review the report before submitting a PR that reduces coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/ \
    --filter "Category!=Integration&Category!=Performance"
```

Target: **≥ 80% line coverage** on the production assembly.

---

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add trailing stop order support
fix: correct NQ symbol mapping for March expiry
test: add universe provider unit tests
docs: update TROUBLESHOOTING with reconnect error
refactor: extract GetThirdFriday into helper class
```

---

## Pull Request Checklist

- [ ] Tests pass: `dotnet test --filter "Category!=Integration&Category!=Performance"`
- [ ] No new compiler warnings
- [ ] Coverage not regressed (if applicable)
- [ ] `TROUBLESHOOTING.md` updated for any new error conditions
- [ ] `CONFIGURATION.md` updated for any new config keys
- [ ] PR description explains *why*, not just *what*
