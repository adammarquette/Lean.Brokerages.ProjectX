# ProjectX LEAN Brokerage Integration

**Status:** Phase 1 - Foundation Complete | Phase 2 - Core Trading (Planning)  
**Last Updated:** March 2026
**Project Lead:** Marquette Speculations  
**Repository:** https://github.com/adammarquette/Lean.Brokerages.ProjectX

## Executive Summary

The ProjectX Lean Brokerage is a comprehensive integration solution that enables 
algorithmic trading of futures contracts with the ProjectX platform through the 
QuantConnect LEAN Engine. This integration provides seamless connectivity 
between LEAN's backtesting and live trading capabilities with ProjectX's trading 
infrastructure, focusing initially on futures markets with the architecture 
designed for future expansion to additional asset classes.

## Project Scope

### Motivation
The LEAN engine currently lacks native support for the ProjectX trading platform, 
preventing traders at several proprietary trading firms from leveraging the 
LEAN engine as an algorithmic trading platform. This integration bridges that 
gap, allowing quantitative traders to execute their LEAN-developed strategies 
directly on ProjectX while maintaining full compatibility with LEAN's ecosystem.

### Primary Objectives
1. **Full Brokerage Integration** - Implement all required LEAN interfaces following [contribution guidelines](https://www.quantconnect.com/docs/v2/lean-engine/contributions/brokerages)
2. **Futures Trading Support** - Enable comprehensive futures contract trading with accurate symbol mapping and contract specifications
3. **Data Pipeline** - Provide real-time market data streaming and historical data access for backtesting
4. **Production Ready** - Deliver enterprise-grade code with comprehensive testing, error handling, and logging
5. **Documentation** - Create complete user and developer documentation for adoption and maintenance

### Initial Scope
**Phase 1-4 Focus:** Futures contracts only
- Symbol mapping for futures tickers
- Market data for futures (real-time and historical)
- Order execution for futures contracts
- Margin calculations specific to futures

### Target Users
- **Quantitative Traders** - Professionals developing and deploying algorithmic futures strategies
- **Systematic Traders** - Traders operating rule-based systems requiring automated execution
- **Research Teams** - Quantitative researchers needing access to ProjectX futures data

### Key Assumptions & Constraints

#### Assumptions
1. **API Stability** - MarqSpec.Client.ProjectX API maintains backward compatibility during Phases 2-8
2. **Market Access** - Users have valid ProjectX accounts with futures trading permissions
3. **Network Reliability** - Stable internet connectivity for WebSocket and REST API communication
4. **Data Availability** - ProjectX provides sufficient historical data depth for LEAN's requirements

#### Constraints
1. **Rate Limits** - ProjectX API rate limiting may impact data download speeds and order submission frequency
2. **Asset Support** - Initial release limited to futures contracts supported by ProjectX
3. **Market Hours** - Trading restricted to ProjectX-supported futures market hours
4. **.NET Version** - Requires .NET 10 compatibility with LEAN Engine

### Success Criteria
- [ ] All LEAN interface contracts fully implemented
- [ ] Integration test suite with >90% code coverage
- [ ] Successful paper trading for minimum 30 days without critical errors
- [ ] Live trading validation with real account
- [ ] Documentation approved by code review
- [ ] Performance benchmarks meeting LEAN standards (< 100ms order latency, < 1s data lag)

## User Stories
### Story 1: Futures Day Trader
As a futures day trader, I want to use my LEAN algorithm with the ProjectX platform so that I can execute my strategies with low latency.

**Acceptance Criteria:**
- Order latency <100ms
- Real-time data with <1s lag
- Easy configuration setup

### Story 2: Researcher
As a quantitative researcher, I want to backtest strategies using ProjectX historical data so that I can validate my models.

**Acceptance Criteria:**
- Access to 5+ years of historical data
- Support for all major futures contracts
- Fast data downloads

## Technical Architecture

### Core Components

Following LEAN's brokerage architecture, this integration implements:

#### 1. IBrokerageFactory
**Purpose:** Initialize and configure the brokerage instance
- Parse configuration from job packets
- Instantiate brokerage with proper credentials
- Manage brokerage lifecycle

#### 2. IBrokerage
**Purpose:** Core trading functionality
- Order placement, modification, and cancellation
- Account and holdings synchronization
- Connection state management
- Real-time order and fill event handling
- WebSocket management for market data and account updates

#### 3. ISymbolMapper
**Purpose:** Symbol translation between LEAN and ProjectX formats
- Convert ProjectX ticker format to LEAN Symbol objects (e.g., "ESH25" → Future Symbol)
- Handle reverse mapping for order submission (LEAN Symbol → ProjectX ticker)
- **Initial Focus:** Futures contracts with standard and non-standard symbols
- Parse contract specifications (expiration, strike, underlying)
- Support market identification and routing

#### 4. IBrokerageModel
**Purpose:** Define brokerage capabilities and constraints
- **Order Types:** Market, Limit, Stop Market, Stop Limit (futures-specific)
- **Time-in-Force:** Day, GTC, IOC, FOK (as supported by ProjectX)
- **Margin Requirements:** Initial and maintenance margin for futures
- **Contract Specifications:** Tick size, multiplier, settlement rules
- **Market Hours:** Futures trading sessions (regular, extended, electronic)
- **Buying Power:** Real-time cash calculation for position sizing

#### 5. IDataQueueHandler
**Purpose:** Real-time market data streaming
- Subscribe/unsubscribe to market data feeds
- Stream tick data (Trade, Quote, OpenInterest)
- Handle data aggregation for various resolutions

#### 6. IHistoryProvider
**Purpose:** Historical data retrieval
- Fetch historical data for backtesting and warm-up
- Support multiple resolutions (Tick, Second, Minute, Hour, Daily)
- Handle date range queries

#### 7. IDataDownloader (ToolBox)
**Purpose:** Bulk data downloads
- Download historical data to local storage
- Convert to LEAN format
- Support data universe generation

#### 8. IFeeModel
**Purpose:** Accurate fee calculations for futures trading
- **Commission Structure:** Per-contract commission modeling
- **Exchange Fees:** Pass-through fees (CME, CBOT, NYMEX, etc.)
- **Regulatory Fees:** NFA, clearing fees
- **Slippage Modeling:** Market impact estimation
- **Round-Turn vs Per-Side:** Support both commission models

## External Dependencies

### LEAN Engine
The core algorithmic trading engine that provides:
- Strategy backtesting framework
- Live trading infrastructure  
- Data management and normalization
- Risk management and portfolio construction tools

**Reference:** https://github.com/adammarquette/Lean
**Version Compatibility:** .NET 10  
**Key Integration Points:**
- `Lean/Brokerages/` - Brokerage implementations
- `Lean/ToolBox/` - Data download utilities
- `Lean/Common/` - Core interfaces and models

### MarqSpec.Client.ProjectX
A C# client library providing programmatic access to ProjectX's trading API, serving as the communication layer between LEAN and ProjectX.

**Reference:** https://github.com/adammarquette/MarqSpec.Client.ProjectX  
**Language:** C# (.NET compatible)  
**License:** [Verify from repository]

**Repository Structure:**
- `MarqSpec.Client.ProjectX/` - Core API client library
- `MarqSpec.Client.ProjectX.Samples/` - Usage examples and patterns
- `MarqSpec.Client.ProjectX.Tests/` - Integration and unit tests
- `MarqSpec.Client.ProjectX.Diagnostics/` - API health checks and debugging tools

**Key Capabilities:**
1. **Authentication & Session Management**
   - OAuth/API key authentication
   - Token refresh and session persistence
   - Connection state monitoring

2. **Market Data Services**
   - Real-time tick data (WebSocket)
   - Historical data queries (REST API)
   - Contract specifications and expiration calendars
   - Market depth (Level II data, if available)

3. **Order Management**
   - Order placement (all supported types)
   - Order modification and cancellation
   - Order status tracking and fill notifications
   - Bulk order operations

4. **Account & Position Management**
   - Real-time account balance updates
   - Position tracking (open positions, realized/unrealized P&L)
   - Margin availability calculations
   - Trade history and reporting

5. **Contract Discovery**
   - Symbol search and lookup
   - Contract specifications (expiration, multiplier, tick size)
   - Available instruments and markets

**Integration Pattern:**
```
LEAN Brokerage (ProjectXBrokerage)
    ↓
MarqSpec.Client.ProjectX (API Client)
    ↓
ProjectX REST API / WebSocket
```

The brokerage wraps MarqSpec.Client.ProjectX to:
- Translate LEAN data models to/from ProjectX formats
- Handle LEAN-specific lifecycle (Connect/Disconnect)
- Aggregate and normalize market data for LEAN consumption
- Map LEAN order types to ProjectX equivalents
- Provide thread-safe access for concurrent operations

**Version Compatibility:**
- Target latest stable version of MarqSpec.Client.ProjectX
- Monitor for breaking changes during development
- Pin to specific version for production deployments

## Implementation Phases

### Phase 1: Foundation Setup ✅ COMPLETE
- [x] Repository initialization and structure
- [x] Project naming conventions (ProjectXBrokerage)
- [x] Basic factory stub (IBrokerageFactory)
- [x] Basic brokerage stub (IBrokerage)
- [x] Project references and dependencies
- [x] Build system configuration (.NET 10, GitHub Actions CI/CD with preview support)
- [x] Initial documentation (README, PRD)
- [x] Configuration schema definition (config.json)
- [x] Logging infrastructure setup

**Deliverables:**
- [x] Compilable solution with proper project structure
- [x] Factory creates brokerage instance (stub)
- [x] PRD and architecture documentation
- [x] Configuration file schema

### Phase 2: Core Trading Implementation
**Objective:** Implement core IBrokerage interface for order execution and account management

**Tasks:**
- [ ] **Connection Management**
  - [ ] Implement `Connect()` - Initialize MarqSpec.Client connection
  - [ ] Implement `Disconnect()` - Graceful shutdown
  - [ ] Implement `IsConnected` property - Real-time connection state
  - [ ] Connection retry logic with exponential backoff
  - [ ] WebSocket reconnection handling

- [ ] **Order Management**
  - [ ] `PlaceOrder()` - Submit orders to ProjectX
  - [ ] `UpdateOrder()` - Modify existing orders (if supported)
  - [ ] `CancelOrder()` - Cancel pending orders
  - [ ] `GetOpenOrders()` - Query active orders
  - [ ] Order validation before submission
  - [ ] Order ID mapping (LEAN ↔ ProjectX)

- [ ] **Account Synchronization**
  - [ ] `GetAccountHoldings()` - Current positions
  - [ ] `GetCashBalance()` - Available funds
  - [ ] Account update event handling
  - [ ] Position reconciliation on connect

- [ ] **Event Handling**
  - [ ] Order status change events
  - [ ] Fill notifications
  - [ ] Error/rejection events
  - [ ] Account update events

- [ ] **Error Handling & Logging**
  - [ ] Comprehensive exception handling
  - [ ] Structured logging (Serilog/NLog)
  - [ ] Error code translation (ProjectX → LEAN)

**Deliverables:**
- [ ] Fully functional order execution
- [ ] Real-time account state synchronization
- [ ] Unit tests for all IBrokerage methods
- [ ] Integration tests with MarqSpec.Client.ProjectX test environment

### Phase 3: Symbol Mapping 🔜 NEXT
**Objective:** Translate between ProjectX and LEAN symbol formats

**Tasks:**
- [ ] **ISymbolMapper Implementation**
  - [ ] `GetLeanSymbol()` - ProjectX ticker → LEAN Symbol
  - [ ] `GetBrokerageSymbol()` - LEAN Symbol → ProjectX ticker
  - [ ] Parse futures ticker formats (ES, NQ, CL, etc.)
  - [ ] Handle expiration date encoding
  - [ ] Support market/exchange identification

- [ ] **Futures Symbol Support**
  - [ ] Standard futures contracts (ESH25, NQM25)
  - [ ] Continuous contracts (if applicable)
  - [ ] Handle month codes (F=Jan, G=Feb, etc.)
  - [ ] Parse contract year (2025 → 25)

- [ ] **Market Mapping**
  - [ ] CME futures
  - [ ] CBOT futures
  - [ ] NYMEX/COMEX futures
  - [ ] ICE futures (if supported)

- [ ] **Testing & Validation**
  - [ ] Unit tests for all symbol formats
  - [ ] Round-trip conversion tests
  - [ ] Edge case handling (invalid symbols, expired contracts)

**Deliverables:**
- [ ] Production-ready ISymbolMapper
- [ ] Symbol conversion test suite
- [ ] Documentation of supported symbol formats

### Phase 4: Brokerage Model
**Objective:** Define ProjectX-specific trading rules and constraints

**Tasks:**
- [ ] **IBrokerageModel Implementation**
  - [ ] `CanSubmitOrder()` - Validate order before submission
  - [ ] `CanUpdateOrder()` - Check if modification allowed
  - [ ] `CanExecuteOrder()` - Validate execution constraints

- [ ] **Order Type Support**
  - [ ] Market orders
  - [ ] Limit orders
  - [ ] Stop Market orders
  - [ ] Stop Limit orders
  - [ ] Define unsupported order types

- [ ] **Time-in-Force Support**
  - [ ] Day orders
  - [ ] GTC (Good-Till-Cancelled)
  - [ ] IOC (Immediate-or-Cancel)
  - [ ] FOK (Fill-or-Kill)

- [ ] **Margin & Leverage**
  - [ ] Initial margin requirements by contract
  - [ ] Maintenance margin rules
  - [ ] Intraday vs overnight margin
  - [ ] Buying power calculations

- [ ] **Market Hours**
  - [ ] Regular trading hours by exchange
  - [ ] Extended hours (if supported)
  - [ ] Electronic trading sessions
  - [ ] Settlement times

- [ ] **Fee Model Integration**
  - [ ] Link to IFeeModel implementation
  - [ ] Default fee structure

**Deliverables:**
- [ ] Complete IBrokerageModel implementation
- [ ] Comprehensive order validation rules
- [ ] Market hours database
- [ ] Documentation of brokerage constraints

### Phase 5: Live Data Streaming
**Objective:** Real-time market data for live trading

**Tasks:**
- [ ] **IDataQueueHandler Implementation**
  - [ ] `Subscribe()` - Start receiving data for symbol
  - [ ] `Unsubscribe()` - Stop data feed
  - [ ] `SetJob()` - Initialize with algorithm job

- [ ] **WebSocket Integration**
  - [ ] Connect to ProjectX WebSocket feed
  - [ ] Handle subscription management
  - [ ] Process real-time tick data
  - [ ] Reconnection logic

- [ ] **Data Streaming**
  - [ ] Trade ticks (price, volume, timestamp)
  - [ ] Quote ticks (bid, ask, spread)
  - [ ] Open Interest updates (if available)

- [ ] **Data Aggregation**
  - [ ] Integrate with LEAN's IDataAggregator
  - [ ] Support multiple resolutions (Tick, Second, Minute)
  - [ ] Handle data consolidation

- [ ] **Subscription Management**
  - [ ] EventBasedDataQueueHandlerSubscriptionManager
  - [ ] Thread-safe subscription tracking
  - [ ] Concurrent symbol handling

**Deliverables:**
- [ ] Working real-time data feed
- [ ] Subscription test suite
- [ ] Performance benchmarks (latency, throughput)
- [ ] Data quality validation

### Phase 6: Historical Data
**Objective:** Provide historical data for backtesting and warm-up

**Tasks:**
- [ ] **IHistoryProvider Implementation**
  - [ ] `GetHistory()` - Fetch historical data
  - [ ] `Initialize()` - Setup history provider

- [ ] **Data Retrieval**
  - [ ] Query ProjectX historical data API
  - [ ] Handle pagination for large datasets
  - [ ] Support multiple resolutions
  - [ ] Date range validation

- [ ] **Resolution Support**
  - [ ] Tick data (if available)
  - [ ] Second bars
  - [ ] Minute bars
  - [ ] Hour bars
  - [ ] Daily bars

- [ ] **Data Caching**
  - [ ] Cache frequently requested data
  - [ ] Implement cache invalidation strategy
  - [ ] Optimize for backtesting performance

- [ ] **Error Handling**
  - [ ] Handle missing data gaps
  - [ ] Validate data completeness
  - [ ] Retry logic for failed requests

**Deliverables:**
- [ ] Complete IHistoryProvider
- [ ] Historical data test suite
- [ ] Caching strategy documentation
- [ ] Performance benchmarks

### Phase 7: Data Downloads (ToolBox)
**Objective:** Enable bulk historical data downloads

**Tasks:**
- [ ] **IDataDownloader Implementation**
  - [ ] `Get()` - Download data for date range
  - [ ] Support DataDownloaderGetParameters

- [ ] **Bulk Download Utilities**
  - [ ] Command-line tool integration
  - [ ] Batch download for multiple symbols
  - [ ] Progress tracking and resumption

- [ ] **Data Format Conversion**
  - [ ] Convert ProjectX format to LEAN format
  - [ ] Write to LEAN directory structure
  - [ ] Compress data files (zip)

- [ ] **Universe Generation**
  - [ ] Generate futures universe files
  - [ ] Contract expiration calendars
  - [ ] Symbol mapping files

**Deliverables:**
- [ ] Functional data downloader
- [ ] ToolBox command-line interface
- [ ] Download scripts and documentation
- [ ] Sample universe files

### Phase 8: Fee Modeling
**Objective:** Accurate commission and fee calculations

**Tasks:**
- [ ] **IFeeModel Implementation**
  - [ ] `GetOrderFee()` - Calculate total fees for order

- [ ] **Commission Structure**
  - [ ] Per-contract commission rates
  - [ ] Volume-based tiering (if applicable)
  - [ ] Round-turn vs per-side modeling

- [ ] **Exchange Fees**
  - [ ] CME fees by product
  - [ ] CBOT fees
  - [ ] NYMEX/COMEX fees
  - [ ] Pass-through fee structure

- [ ] **Regulatory Fees**
  - [ ] NFA fees
  - [ ] Clearing fees
  - [ ] Section 31 fees (if applicable)

- [ ] **Slippage Modeling**
  - [ ] Market impact estimation
  - [ ] Bid-ask spread costs
  - [ ] Configurable slippage model

**Deliverables:**
- [ ] Complete IFeeModel
- [ ] Fee calculation test suite
- [ ] Documentation of fee structure
- [ ] Fee configuration examples

### Phase 9: Testing & Documentation
**Objective:** Comprehensive testing and documentation

**Tasks:**
- [ ] **Unit Testing**
  - [ ] Unit tests for all components (target >90% coverage)
  - [ ] Mock MarqSpec.Client for isolated testing
  - [ ] Edge case and error condition tests
  - [ ] Thread safety tests

- [ ] **Integration Testing**
  - [ ] Tests against ProjectX sandbox/test environment
  - [ ] End-to-end order execution tests
  - [ ] Data streaming validation tests
  - [ ] Account synchronization tests
  - [ ] Historical data accuracy tests

- [ ] **Regression Testing**
  - [ ] LEAN standard brokerage test suite
  - [ ] `BrokerageTests` base class implementation
  - [ ] Order type test scenarios
  - [ ] Symbol mapping regression tests

- [ ] **Performance Testing**
  - [ ] Order latency benchmarks
  - [ ] Data streaming throughput tests
  - [ ] Memory usage profiling
  - [ ] Concurrent operation stress tests

- [ ] **User Documentation**
  - [ ] Setup and configuration guide
  - [ ] API reference documentation
  - [ ] Code examples and samples
  - [ ] Troubleshooting guide
  - [ ] FAQ

- [ ] **Developer Documentation**
  - [ ] Architecture overview
  - [ ] Code organization and patterns
  - [ ] Extension points
  - [ ] Contributing guidelines
  - [ ] Release notes

**Deliverables:**
- [ ] Test suite with >90% coverage
- [ ] All LEAN brokerage tests passing
- [ ] Complete user documentation
- [ ] Developer documentation
- [ ] Performance benchmark report

### Phase 10: Deployment & Release
**Objective:** Production readiness and release

**Tasks:**
- [ ] **Code Review & QA**
  - [ ] Internal code review
  - [ ] Security audit
  - [ ] Performance review
  - [ ] Documentation review
  - [ ] LEAN team code review

- [ ] **Production Readiness**
  - [ ] Paper trading validation (30+ days)
  - [ ] Live account validation
  - [ ] Failover and recovery testing
  - [ ] Monitoring and alerting setup

- [ ] **Repository Preparation**
  - [ ] Clean commit history
  - [ ] Remove sensitive data
  - [ ] Finalize README
  - [ ] License verification
  - [ ] Changelog preparation

- [ ] **LEAN Integration**
  - [ ] Pull request to LEAN repository
  - [ ] Address review feedback
  - [ ] Merge to LEAN master branch
  - [ ] LEAN release coordination

- [ ] **QuantConnect Cloud** (Optional)
  - [ ] Cloud integration assessment
  - [ ] Cloud-specific configuration
  - [ ] Cloud deployment testing
  - [ ] Cloud documentation

- [ ] **Release & Versioning**
  - [ ] Version tagging (v1.0.0)
  - [ ] GitHub release notes
  - [ ] NuGet package (if applicable)
  - [ ] Announcement and promotion

**Deliverables:**
- [ ] Merged to LEAN master
- [ ] Public release (GitHub)
- [ ] Complete documentation
- [ ] Community support plan

## Development Guidelines

### Code Style & Standards
- **LEAN Coding Standards:** Follow [LEAN contribution guidelines](https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md#code-style-and-testing)
- **Naming Conventions:** PascalCase for public members, _camelCase for private fields
- **XML Documentation:** Required for all public APIs and interfaces
- **Code Comments:** Explain complex logic, avoid obvious comments
- **Async/Await:** Use async patterns for I/O operations
- **Null Handling:** Proper null checks and nullable reference types
- **Exception Handling:** Specific exceptions, avoid catching generic Exception
- **Logging:** Structured logging with appropriate severity levels

### Testing Standards
- **Unit Tests:** 
  - Test coverage >90% for all production code
  - Use NUnit testing framework
  - Mock external dependencies (MarqSpec.Client.ProjectX)
  - Test both success and failure scenarios
  - Include edge cases and boundary conditions

- **Integration Tests:**
  - Test against ProjectX sandbox/test environment
  - Validate end-to-end workflows
  - Test data accuracy and consistency
  - Performance benchmarks

- **Regression Tests:**
  - Inherit from LEAN's `BrokerageTests` base class
  - Implement all required test methods
  - Test all supported order types
  - Validate symbol mapping round-trips

- **Test Organization:**
  ```
  QuantConnect.ProjectXBrokerage.Tests/
  ├── ProjectXBrokerageTests.cs (regression)
  ├── ProjectXSymbolMapperTests.cs (unit)
  ├── ProjectXBrokerageModelTests.cs (unit)
  ├── ProjectXDataQueueHandlerTests.cs (integration)
  └── ProjectXHistoryProviderTests.cs (integration)
  ```

### Security Best Practices
- **Credential Management:**
  - Never commit API keys or secrets to repository
  - Use environment variables or secure configuration
  - Support credential encryption in config files
  - Clear sensitive data from memory after use

- **API Communication:**
  - Always use HTTPS/WSS
  - Validate SSL certificates
  - Implement request signing if required
  - Rate limit handling and backoff

- **Error Messages:**
  - Never expose sensitive data in logs
  - Sanitize error messages before logging
  - Use secure exception handling

- **Dependency Security:**
  - Regular security audits of NuGet packages
  - Keep dependencies up to date
  - Monitor for CVEs

### Performance Considerations
- **Connection Management:**
  - Reuse connections where possible
  - Implement connection pooling
  - Handle reconnection gracefully
  - Monitor connection health

- **Threading:**
  - Thread-safe implementations required
  - Use concurrent collections where appropriate
  - Avoid blocking operations on hot paths
  - Proper disposal of resources

- **Memory Management:**
  - Dispose IDisposable objects properly
  - Avoid memory leaks in event handlers
  - Monitor memory usage under load
  - Implement object pooling for high-frequency operations

- **Data Streaming:**
  - Efficient data structure selection
  - Minimize allocations in hot paths
  - Buffer management for WebSocket data
  - Back-pressure handling

### Logging Strategy
- **Log Levels:**
  - `Trace`: Detailed diagnostic information (dev only)
  - `Debug`: Debugging information (dev/staging)
  - `Information`: General informational messages
  - `Warning`: Unexpected behavior that doesn't prevent operation
  - `Error`: Errors that prevent operation but allow recovery
  - `Critical`: Failures requiring immediate attention

- **What to Log:**
  - Connection state changes
  - Order submissions and status changes
  - Data subscription events
  - Error conditions and exceptions
  - Performance metrics
  - Configuration changes

- **What NOT to Log:**
  - API keys or credentials
  - Full order details with sensitive info
  - Personal account information
  - Raw API responses with sensitive data

### Version Control
- **Branching Strategy:**
  - `master`: Production-ready code
  - `develop`: Integration branch
  - `feature/*`: Feature development
  - `bugfix/*`: Bug fixes
  - `release/*`: Release preparation

- **Commit Messages:**
  - Clear, descriptive commit messages
  - Reference issue numbers where applicable
  - Follow conventional commits format

- **Pull Requests:**
  - Descriptive PR title and description
  - Link to related issues
  - Include test results
  - Request reviews from maintainers

## Known Limitations

This section documents intentional limitations and design decisions that define the scope of the ProjectX brokerage integration.

### Trading Style Constraints

**Not Optimized for High-Frequency Trading (HFT)**

This brokerage integration is **not designed or optimized** for high-frequency trading strategies. The architecture prioritizes:
- **Reliability** - Robust error handling and connection management
- **Data Integrity** - Accurate order tracking and position reconciliation  
- **Maintainability** - Clean code and comprehensive testing

HFT strategies requiring sub-millisecond latency and ultra-high order throughput should consider:
- Native exchange APIs with co-location
- Specialized HFT infrastructure
- Direct market access (DMA) solutions

**Supported Trading Styles:**
- ✅ **Systematic Trading** - Rule-based strategies with moderate order frequency (1-10 orders/minute)
- ✅ **Algorithmic Day Trading** - Intraday strategies with reasonable execution requirements
- ✅ **Swing Trading** - Multi-day position holding strategies
- ✅ **Statistical Arbitrage** - Mean reversion and pairs trading (non-HFT timeframes)
- ✅ **Trend Following** - Momentum-based strategies on various timeframes

**Not Supported:**
- ❌ **High-Frequency Trading** - Strategies requiring <10ms latency or >100 orders/second
- ❌ **Market Making** - Continuous quote updates and sub-second order placement
- ❌ **Latency Arbitrage** - Strategies exploiting microsecond price discrepancies

### Asset Class Limitations (Phase 1-4)

**Futures Only**
- Initial releases (Phases 1-4) support futures contracts exclusively
- Equities, options, forex, and crypto support planned for future phases
- Symbol mapper focused on futures ticker formats
- Margin calculations specific to futures contracts

**Future Expansion:**
- Phase 5+: Equities support
- Phase 6+: Options on futures
- Phase 7+: Forex and crypto futures

### Technical Constraints

**Rate Limits**
- Default order rate limit: 10 orders/second (configurable to max 100/second)
- Default data rate limit: 50 requests/second (configurable to max 1000/second)
- Maximum concurrent subscriptions: 100 symbols (configurable to 500)
- Exceeded limits trigger automatic queuing, not rejection

**Latency Expectations**
- Target order latency: <100ms (p95)
- Market data lag: <1 second (p99)
- WebSocket reconnection: <5 seconds
- Not suitable for strategies requiring <10ms execution

**Data Availability**
- Historical data depth subject to ProjectX API limitations
- Tick data availability varies by contract and exchange
- No Level II (market depth) data in Phase 1-4
- Real-time data limited to Trade, Quote, and Open Interest ticks

### Operational Constraints

**Market Hours**
- Trading restricted to exchange-defined market hours
- No after-hours execution for contracts without extended sessions
- Settlement times respected per exchange rules

**Order Types (Phase 1-4)**
- Supported: Market, Limit, Stop Market, Stop Limit
- Not Supported: Bracket orders, OCO (One-Cancels-Other), trailing stops
- Advanced order types may be added in future phases

**Account Types**
- Live trading requires funded ProjectX account
- Paper trading uses ProjectX sandbox environment
- No simulated brokerage mode (must have ProjectX account)

## Risk Management & Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **MarqSpec.Client.ProjectX Breaking Changes** | High | Medium | Pin to stable version; monitor repository; maintain version compatibility matrix |
| **ProjectX API Changes** | High | Medium | Regular communication with ProjectX; version API calls; maintain backward compatibility |
| **Rate Limiting** | Medium | High | Implement request throttling; queue management; user documentation on limits |
| **WebSocket Disconnections** | High | Medium | Automatic reconnection; connection monitoring; graceful degradation |
| **Data Quality Issues** | Medium | Medium | Validation layer; data quality monitoring; alert on anomalies |
| **Performance Degradation** | Medium | Low | Performance testing; monitoring; optimization hot paths |
| **Memory Leaks** | Medium | Low | Profiling; code review; dispose pattern; automated testing |

### Business Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **Limited Adoption** | Medium | Medium | Comprehensive documentation; community engagement; support channels |
| **ProjectX Platform Changes** | High | Low | Diversify development; maintain flexibility; community communication |
| **Support Burden** | Medium | Medium | Clear documentation; FAQ; community support; issue templates |
| **Regulatory Changes** | Low | Low | Monitor regulatory landscape; design for compliance flexibility |

### Operational Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **Incomplete Testing** | High | Medium | Comprehensive test plan; code coverage requirements; paper trading validation |
| **Security Vulnerabilities** | High | Low | Security audits; dependency scanning; secure coding practices |
| **Documentation Gaps** | Medium | High | Documentation requirements in DoD; peer review; user feedback |
| **Maintenance Costs** | Medium | Medium | Clean code; good architecture; automated testing; community contributions |

## Risk Management & Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **MarqSpec.Client.ProjectX Breaking Changes** | High | Medium | Pin to stable version; monitor repository; maintain version compatibility matrix |
| **ProjectX API Changes** | High | Medium | Regular communication with ProjectX; version API calls; maintain backward compatibility |
| **Rate Limiting** | Medium | High | Implement request throttling; queue management; user documentation on limits |
| **WebSocket Disconnections** | High | Medium | Automatic reconnection; connection monitoring; graceful degradation |
| **Data Quality Issues** | Medium | Medium | Validation layer; data quality monitoring; alert on anomalies |
| **Performance Degradation** | Medium | Low | Performance testing; monitoring; optimization hot paths |
| **Memory Leaks** | Medium | Low | Profiling; code review; dispose pattern; automated testing |

### Business Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **Limited Adoption** | Medium | Medium | Comprehensive documentation; community engagement; support channels |
| **ProjectX Platform Changes** | High | Low | Diversify development; maintain flexibility; community communication |
| **Support Burden** | Medium | Medium | Clear documentation; FAQ; community support; issue templates |
| **Regulatory Changes** | Low | Low | Monitor regulatory landscape; design for compliance flexibility |

### Operational Risks

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **Incomplete Testing** | High | Medium | Comprehensive test plan; code coverage requirements; paper trading validation |
| **Security Vulnerabilities** | High | Low | Security audits; dependency scanning; secure coding practices |
| **Documentation Gaps** | Medium | High | Documentation requirements in DoD; peer review; user feedback |
| **Maintenance Costs** | Medium | Medium | Clean code; good architecture; automated testing; community contributions |

## Success Metrics & KPIs

### Technical Metrics
- [ ] **Code Quality**
  - [ ] Unit test coverage >90%
  - [ ] Integration test coverage >80%
  - [ ] Zero critical security vulnerabilities
  - [ ] Code review approval from LEAN maintainers

- [ ] **Functionality**
  - [ ] All LEAN IBrokerage interface methods implemented
  - [ ] All required LEAN brokerage tests passing
  - [ ] Support for minimum 10 popular futures contracts
  - [ ] All major order types working (Market, Limit, Stop, StopLimit)

- [ ] **Performance**
  - [ ] Order submission latency <100ms (p95)
  - [ ] Market data lag <1 second (p99)
  - [ ] Historical data query response <5 seconds for 1 year daily data
  - [ ] Memory usage stable under sustained load
  - [ ] Zero memory leaks detected

- [ ] **Reliability**
  - [ ] 30+ days paper trading without critical errors
  - [ ] Successful reconnection after network disruption
  - [ ] Graceful handling of API rate limits
  - [ ] 99.9% uptime during trading hours (after deployment)

- [ ] **Performance Benchmarks**
  - [ ] Order Latency (p50) <50ms
  - [ ] Order Latency (p95) <100ms
  - [ ] Order Latency (p99) <200ms
  - [ ] Market Data Lag (p99) <1s
  - [ ] Historical Query (1yr daily) <5s
  - [ ] Memory Usage (8hr session) <500MB
  - [ ] WebSocket Reconnect Time <5s

### Business Metrics
- [ ] **Adoption**
  - [ ] Successful live trading with real account (validation)
  - [ ] 10+ community users testing in first month
  - [ ] Positive feedback from beta testers
  - [ ] Merged to LEAN master branch

- [ ] **Documentation**
  - [ ] Complete user setup guide
  - [ ] API documentation for all public interfaces
  - [ ] Minimum 5 example algorithms
  - [ ] Troubleshooting guide with common issues
  - [ ] Documentation completeness score >90%

- [ ] **Support**
  - [ ] Average issue response time <48 hours
  - [ ] 90% of issues resolved within 2 weeks
  - [ ] Active community discussions
  - [ ] Regular updates and maintenance

### Acceptance Criteria
To consider the project complete and ready for release:
1. ✅ All Phase 1-9 tasks completed
2. ⬜ Code review approved by LEAN maintainers
3. ⬜ All automated tests passing (unit, integration, regression)
4. ⬜ 30-day paper trading validation successful
5. ⬜ Live trading validation completed
6. ⬜ Security audit passed
7. ⬜ Performance benchmarks met
8. ⬜ Documentation peer-reviewed and approved
9. ⬜ No critical or high-priority bugs outstanding
10. ⬜ Community beta testing feedback addressed

## References

- [LEAN Brokerage Contribution Guide](https://www.quantconnect.com/docs/v2/lean-engine/contributions/brokerages)
- [LEAN Repository](https://github.com/QuantConnect/Lean)
- [LEAN Brokerage Template](https://github.com/QuantConnect/Lean.Brokerages.Template/blob/master/README.md)
- [MarqSpec.Client.ProjectX Repository](https://github.com/adammarquette/MarqSpec.Client.ProjectX)
- [QuantConnect Documentation](https://www.quantconnect.com/docs/v2/)

### Project Documentation Files
- [CONFIGURATION.md](./CONFIGURATION.md) - Complete configuration guide
- [DIAGRAMS.md](./DIAGRAMS.md) - Architecture diagram documentation
- [config-schema.json](./config-schema.json) - JSON Schema for validation
- [config.template.json](./config.template.json) - Configuration template

## Appendix

### Configuration Schema

The ProjectX brokerage integration uses LEAN's standard configuration mechanism, where settings are provided through a JSON configuration file or environment variables. Configuration keys follow LEAN's naming convention using lowercase with hyphens.

#### Configuration Keys

| Key | Type | Required | Default | Description |
|-----|------|----------|---------|-------------|
| `brokerage` | string | Yes | - | Must be set to `"ProjectXBrokerage"` |
| `brokerage-project-x-api-key` | string | Yes | - | ProjectX API key for authentication |
| `brokerage-project-x-api-secret` | string | Yes | - | ProjectX API secret for authentication |
| `brokerage-project-x-environment` | string | No | `"production"` | Trading environment: `"production"` or `"sandbox"` |
| `brokerage-project-x-api-url` | string | No | Auto | Custom API base URL (overrides environment default) |
| `brokerage-project-x-websocket-url` | string | No | Auto | Custom WebSocket URL (overrides environment default) |
| `brokerage-project-x-order-rate-limit` | integer | No | `10` | Maximum orders per second (burst limit) |
| `brokerage-project-x-data-rate-limit` | integer | No | `50` | Maximum data requests per second |
| `brokerage-project-x-max-subscriptions` | integer | No | `100` | Maximum concurrent symbol subscriptions |
| `brokerage-project-x-reconnect-attempts` | integer | No | `5` | Number of reconnection attempts before failure |
| `brokerage-project-x-reconnect-delay` | integer | No | `1000` | Initial reconnection delay in milliseconds |
| `brokerage-project-x-request-timeout` | integer | No | `30000` | HTTP request timeout in milliseconds |
| `brokerage-project-x-websocket-timeout` | integer | No | `5000` | WebSocket ping/pong timeout in milliseconds |
| `brokerage-project-x-enable-logging` | boolean | No | `true` | Enable detailed brokerage logging |
| `brokerage-project-x-log-level` | string | No | `"Information"` | Log level: `"Trace"`, `"Debug"`, `"Information"`, `"Warning"`, `"Error"`, `"Critical"` |
| `data-queue-handler` | string | No | - | Set to `"ProjectXBrokerage"` to use ProjectX for live data |
| `history-provider` | string | No | - | Set to `"ProjectXBrokerage"` to use ProjectX for historical data |

#### Environment-Specific URLs

The brokerage automatically selects API endpoints based on the `brokerage-project-x-environment` setting:

**Production Environment:**
- REST API: `https://api.projectx.com/v1`
- WebSocket: `wss://stream.projectx.com/v1`

**Sandbox Environment:**
- REST API: `https://api-sandbox.projectx.com/v1`
- WebSocket: `wss://stream-sandbox.projectx.com/v1`

#### Environment Variable Support

All configuration keys can be set via environment variables using the `QC_` prefix with underscores instead of hyphens:

```bash
# Example: Setting API key via environment variable
export QC_BROKERAGE_PROJECT_X_API_KEY="your-api-key"
export QC_BROKERAGE_PROJECT_X_API_SECRET="your-api-secret"
export QC_BROKERAGE_PROJECT_X_ENVIRONMENT="sandbox"
```

#### Security Considerations

**⚠️ NEVER commit credentials to source control!**

- Store credentials in environment variables or secure configuration management systems
- Use separate API keys for development, testing, and production
- Rotate API keys regularly
- Restrict API key permissions to minimum required scope
- Use sandbox environment for development and testing

#### Configuration Validation

The brokerage validates configuration on initialization and will throw detailed exceptions for:
- Missing required parameters
- Invalid environment values
- Malformed URLs
- Out-of-range numeric values
- Invalid log level strings

#### Advanced Configuration

##### Rate Limiting Strategy

The brokerage implements token bucket rate limiting:
- `brokerage-project-x-order-rate-limit`: Controls burst order submissions (default: 10/second)
- `brokerage-project-x-data-rate-limit`: Controls API request frequency (default: 50/second)
- Exceeded limits trigger automatic request queuing

**Recommended Settings by Trading Style:**
- **Day Trading:** order-rate-limit: 10-15, data-rate-limit: 50-100
- **Systematic Trading:** order-rate-limit: 5-10, data-rate-limit: 30-50  
- **Swing Trading:** order-rate-limit: 3-5, data-rate-limit: 20-30

**Important:** This brokerage is not designed for high-frequency trading (HFT). The architecture prioritizes reliability, data integrity, and proper order handling over ultra-low latency execution. For HFT strategies requiring sub-millisecond latency, consider using native exchange APIs with co-location.

##### Reconnection Strategy

WebSocket reconnection uses exponential backoff:
- Initial delay: `brokerage-project-x-reconnect-delay` ms
- Backoff multiplier: 2x per attempt
- Maximum attempts: `brokerage-project-x-reconnect-attempts`
- Connection state events emitted for monitoring

##### Logging Configuration

Enable verbose logging for troubleshooting:
```json
{
  "brokerage-project-x-enable-logging": true,
  "brokerage-project-x-log-level": "Debug"
}
```

**Log Levels:**
- `Trace`: All API requests/responses, WebSocket messages
- `Debug`: Connection events, order submissions, data subscriptions
- `Information`: Successful operations, state changes (default)
- `Warning`: Rate limit warnings, retry attempts
- `Error`: Failed operations, exceptions
- `Critical`: Fatal errors requiring immediate attention

### Configuration Example

#### Minimal Configuration (Live Trading)
```json
{
  "job-project-id": "0",
  "environment": "live",
  "algorithm-type-name": "BasicTemplateAlgorithm",
  "algorithm-language": "CSharp",
  "parameters": {},
  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "data-queue-handler": "ProjectXBrokerage"
}
```

#### Complete Configuration (All Options)
```json
{
  "job-project-id": "0",
  "environment": "live",
  "algorithm-type-name": "FuturesTradingAlgorithm",
  "algorithm-language": "CSharp",
  "parameters": {},

  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "brokerage-project-x-environment": "production",
  "brokerage-project-x-order-rate-limit": 10,
  "brokerage-project-x-data-rate-limit": 50,
  "brokerage-project-x-max-subscriptions": 100,
  "brokerage-project-x-reconnect-attempts": 5,
  "brokerage-project-x-reconnect-delay": 1000,
  "brokerage-project-x-request-timeout": 30000,
  "brokerage-project-x-websocket-timeout": 5000,
  "brokerage-project-x-enable-logging": true,
  "brokerage-project-x-log-level": "Information",

  "data-queue-handler": "ProjectXBrokerage",
  "history-provider": "ProjectXBrokerage"
}
```

#### Development/Sandbox Configuration
```json
{
  "job-project-id": "0",
  "environment": "live",
  "algorithm-type-name": "TestFuturesAlgorithm",
  "algorithm-language": "CSharp",
  "parameters": {},

  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "sandbox-api-key",
  "brokerage-project-x-api-secret": "sandbox-api-secret",
  "brokerage-project-x-environment": "sandbox",
  "brokerage-project-x-enable-logging": true,
  "brokerage-project-x-log-level": "Debug",

  "data-queue-handler": "ProjectXBrokerage",
  "history-provider": "ProjectXBrokerage"
}
```

#### Backtesting Configuration (Historical Data Only)
```json
{
  "job-project-id": "0",
  "environment": "backtesting",
  "algorithm-type-name": "FuturesBacktestAlgorithm",
  "algorithm-language": "CSharp",
  "parameters": {},

  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "brokerage-project-x-environment": "production",

  "history-provider": "ProjectXBrokerage"
}
```

#### Custom API Endpoints Configuration
```json
{
  "job-project-id": "0",
  "environment": "live",
  "algorithm-type-name": "BasicTemplateAlgorithm",
  "algorithm-language": "CSharp",
  "parameters": {},

  "brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "brokerage-project-x-api-url": "https://custom-api.projectx.com/v1",
  "brokerage-project-x-websocket-url": "wss://custom-stream.projectx.com/v1",

  "data-queue-handler": "ProjectXBrokerage"
}
```

#### Configuration for Local LEAN Deployment
```json
{
  "environment": "live-paper",
  "live-mode": true,

  "live-mode-brokerage": "ProjectXBrokerage",
  "brokerage-project-x-api-key": "your-api-key",
  "brokerage-project-x-api-secret": "your-api-secret",
  "brokerage-project-x-environment": "sandbox",

  "data-queue-handler": ["ProjectXBrokerage"],
  "data-folder": "./Data",

  "live-holdings": [],
  "live-cash-balance": {
    "USD": 100000
  }
}
```

#### Test/Integration Configuration (config.json)
```json
{
  "debug-mode": true,
  "data-folder": "../../../../Lean/Data/",

  "brokerage-project-x-api-key": "",
  "brokerage-project-x-api-secret": "",
  "brokerage-project-x-environment": "sandbox",
  "brokerage-project-x-enable-logging": true,
  "brokerage-project-x-log-level": "Trace"
}
```

#### Schema Validation Example (JSON Schema)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ProjectX Brokerage Configuration",
  "type": "object",
  "required": ["brokerage", "brokerage-project-x-api-key", "brokerage-project-x-api-secret"],
  "properties": {
    "brokerage": {
      "type": "string",
      "const": "ProjectXBrokerage"
    },
    "brokerage-project-x-api-key": {
      "type": "string",
      "minLength": 1,
      "description": "ProjectX API Key"
    },
    "brokerage-project-x-api-secret": {
      "type": "string",
      "minLength": 1,
      "description": "ProjectX API Secret"
    },
    "brokerage-project-x-environment": {
      "type": "string",
      "enum": ["sandbox", "production"],
      "default": "production"
    },
    "brokerage-project-x-api-url": {
      "type": "string",
      "format": "uri",
      "pattern": "^https://"
    },
    "brokerage-project-x-websocket-url": {
      "type": "string",
      "format": "uri",
      "pattern": "^wss://"
    },
    "brokerage-project-x-order-rate-limit": {
      "type": "integer",
      "minimum": 1,
      "maximum": 100,
      "default": 10
    },
    "brokerage-project-x-data-rate-limit": {
      "type": "integer",
      "minimum": 1,
      "maximum": 1000,
      "default": 50
    },
    "brokerage-project-x-max-subscriptions": {
      "type": "integer",
      "minimum": 1,
      "maximum": 500,
      "default": 100
    },
    "brokerage-project-x-reconnect-attempts": {
      "type": "integer",
      "minimum": 1,
      "maximum": 20,
      "default": 5
    },
    "brokerage-project-x-reconnect-delay": {
      "type": "integer",
      "minimum": 100,
      "maximum": 60000,
      "default": 1000
    },
    "brokerage-project-x-request-timeout": {
      "type": "integer",
      "minimum": 1000,
      "maximum": 300000,
      "default": 30000
    },
    "brokerage-project-x-websocket-timeout": {
      "type": "integer",
      "minimum": 1000,
      "maximum": 60000,
      "default": 5000
    },
    "brokerage-project-x-enable-logging": {
      "type": "boolean",
      "default": true
    },
    "brokerage-project-x-log-level": {
      "type": "string",
      "enum": ["Trace", "Debug", "Information", "Warning", "Error", "Critical"],
      "default": "Information"
    },
    "data-queue-handler": {
      "type": "string"
    },
    "history-provider": {
      "type": "string"
    }
  }
}
```

### Architecture Diagram

```mermaid
graph TB
    subgraph User_Layer["User Layer"]
        User[Trading Algorithm]
        Config[Configuration<br/>API Keys, Settings]
    end

    subgraph LEAN_Core["LEAN Engine Core"]
        AlgoFramework[Algorithm Framework]
        DataManager[Data Manager]
        OrderManager[Order Manager]
        PortfolioManager[Portfolio Manager]
    end

    subgraph ProjectX_Integration["ProjectX Brokerage Integration"]
        Factory[ProjectXBrokerageFactory<br/>IBrokerageFactory]

        subgraph Core_Components["Core Trading Components"]
            Brokerage[ProjectXBrokerage<br/>IBrokerage]
            SymbolMapper[ProjectXSymbolMapper<br/>ISymbolMapper]
            BrokerageModel[ProjectXBrokerageModel<br/>IBrokerageModel]
        end

        subgraph Data_Components["Data Components"]
            DataQueue[ProjectXDataQueueHandler<br/>IDataQueueHandler]
            HistoryProvider[ProjectXHistoryProvider<br/>IHistoryProvider]
            DataDownloader[ProjectXDataDownloader<br/>IDataDownloader]
        end

        subgraph Fee_Component["Fee Model"]
            FeeModel[ProjectXFeeModel<br/>IFeeModel]
        end
    end

    subgraph Client_Layer["API Client Layer"]
        APIClient[MarqSpec.Client.ProjectX<br/>API Client Library]

        subgraph Client_Modules["Client Modules"]
            AuthModule[Authentication]
            OrderModule[Order Management]
            MarketDataModule[Market Data]
            AccountModule[Account & Positions]
        end
    end

    subgraph Platform_Layer["ProjectX Platform"]
        API[ProjectX REST API]
        WS[ProjectX WebSocket]
        Platform[Trading Infrastructure]
    end

    %% User to LEAN connections
    User -->|Creates & Runs| AlgoFramework
    Config -->|Loads| Factory

    %% LEAN to Integration connections
    AlgoFramework -->|Requests Data| DataManager
    AlgoFramework -->|Places Orders| OrderManager
    AlgoFramework -->|Queries Portfolio| PortfolioManager

    DataManager -->|Subscribe/Stream| DataQueue
    DataManager -->|Historical Requests| HistoryProvider
    OrderManager -->|Execute Orders| Brokerage
    PortfolioManager -->|Account Sync| Brokerage

    %% Factory connections
    Factory -->|Instantiates| Brokerage
    Factory -->|Configures| DataQueue
    Factory -->|Configures| HistoryProvider

    %% Integration internal connections
    Brokerage -->|Translates Symbols| SymbolMapper
    Brokerage -->|Validates Orders| BrokerageModel
    Brokerage -->|Calculates Fees| FeeModel
    DataQueue -->|Maps Symbols| SymbolMapper
    HistoryProvider -->|Maps Symbols| SymbolMapper

    %% Integration to Client connections
    Brokerage -->|Order Operations| APIClient
    Brokerage -->|Account Sync| APIClient
    DataQueue -->|Real-time Data| APIClient
    HistoryProvider -->|Historical Data| APIClient
    DataDownloader -->|Bulk Downloads| APIClient

    %% Client internal connections
    APIClient -->|Uses| AuthModule
    APIClient -->|Uses| OrderModule
    APIClient -->|Uses| MarketDataModule
    APIClient -->|Uses| AccountModule

    %% Client to Platform connections
    AuthModule -->|Authenticates| API
    OrderModule -->|REST Calls| API
    MarketDataModule -->|Streams| WS
    MarketDataModule -->|Queries| API
    AccountModule -->|Queries| API

    API -->|Connected to| Platform
    WS -->|Connected to| Platform

    %% Styling
    classDef userLayer fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef leanLayer fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef integrationLayer fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef clientLayer fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef platformLayer fill:#fce4ec,stroke:#880e4f,stroke-width:2px

    class User,Config userLayer
    class AlgoFramework,DataManager,OrderManager,PortfolioManager leanLayer
    class Factory,Brokerage,SymbolMapper,BrokerageModel,DataQueue,HistoryProvider,DataDownloader,FeeModel integrationLayer
    class APIClient,AuthModule,OrderModule,MarketDataModule,AccountModule clientLayer
    class API,WS,Platform platformLayer
```

### Class Diagram

```mermaid
classDiagram
    %% LEAN Core Interfaces
    class IBrokerageFactory {
        <<interface>>
        +CreateBrokerage() IBrokerage
        +GetBrokerageModel(IOrderProvider) IBrokerageModel
        +Dispose() void
    }

    class IBrokerage {
        <<interface>>
        +PlaceOrder(Order) bool
        +UpdateOrder(Order) bool
        +CancelOrder(Order) bool
        +GetAccountHoldings() List~Holding~
        +GetCashBalance() List~CashAmount~
        +IsConnected bool
        +Connect() void
        +Disconnect() void
    }

    class ISymbolMapper {
        <<interface>>
        +GetLeanSymbol(string) Symbol
        +GetBrokerageSymbol(Symbol) string
    }

    class IBrokerageModel {
        <<interface>>
        +CanSubmitOrder(Security, Order) bool
        +CanUpdateOrder(Security, Order) bool
        +CanExecuteOrder(Security, Order) bool
        +GetFeeModel(Security) IFeeModel
        +GetLeverage(Security) decimal
        +GetMarginRemaining(Portfolio) decimal
    }

    class IDataQueueHandler {
        <<interface>>
        +Subscribe(SubscriptionDataConfig, EventHandler) void
        +Unsubscribe(SubscriptionDataConfig) void
        +SetJob(LiveNodePacket) void
    }

    class IHistoryProvider {
        <<interface>>
        +GetHistory(HistoryRequest) IEnumerable~BaseData~
        +Initialize(HistoryProviderInitializeParameters) void
    }

    class IDataDownloader {
        <<interface>>
        +Get(DataDownloaderGetParameters) IEnumerable~BaseData~
    }

    class IFeeModel {
        <<interface>>
        +GetOrderFee(OrderFeeParameters) OrderFee
    }

    %% ProjectX Implementations
    class ProjectXBrokerageFactory {
        +BrokerageData Dictionary~string, string~
        +CreateBrokerage(LiveNodePacket, IAlgorithm) IBrokerage
        +GetBrokerageModel(IOrderProvider) IBrokerageModel
        +Dispose() void
    }

    class ProjectXBrokerage {
        -_apiClient MarqSpecClient
        -_symbolMapper ProjectXSymbolMapper
        -_aggregator IDataAggregator
        -_webSocket WebSocketClient
        +PlaceOrder(Order) bool
        +CancelOrder(Order) bool
        +GetAccountHoldings() List~Holding~
        +Connect() void
        +Disconnect() void
    }

    class ProjectXSymbolMapper {
        -_futuresMap Dictionary~string, Symbol~
        +GetLeanSymbol(string) Symbol
        +GetBrokerageSymbol(Symbol) string
        -ParseFuturesTicker(string) Symbol
    }

    class ProjectXBrokerageModel {
        +DefaultMarkets Dictionary~SecurityType, string~
        +MarketMap Dictionary~SecurityType, string~
        +CanSubmitOrder(Security, Order) bool
        +GetFeeModel(Security) IFeeModel
    }

    class ProjectXDataQueueHandler {
        -_subscriptionManager SubscriptionManager
        -_webSocket WebSocketClient
        +Subscribe(SubscriptionDataConfig, EventHandler) void
        +Unsubscribe(SubscriptionDataConfig) void
    }

    class ProjectXHistoryProvider {
        -_apiClient MarqSpecClient
        +GetHistory(HistoryRequest) IEnumerable~BaseData~
        +Initialize(HistoryProviderInitializeParameters) void
    }

    class ProjectXBrokerageDownloader {
        -_apiClient MarqSpecClient
        +Get(DataDownloaderGetParameters) IEnumerable~BaseData~
    }

    class ProjectXFeeModel {
        +GetOrderFee(OrderFeeParameters) OrderFee
        -CalculateExchangeFees(Order) decimal
    }

    %% External Dependency
    class MarqSpecClient {
        <<external>>
        +OrderManagement OrderApi
        +MarketData DataApi
        +AccountInfo AccountApi
        +Authentication AuthApi
    }

    %% Inheritance Relationships
    IBrokerageFactory <|.. ProjectXBrokerageFactory : implements
    IBrokerage <|.. ProjectXBrokerage : implements
    ISymbolMapper <|.. ProjectXSymbolMapper : implements
    IBrokerageModel <|.. ProjectXBrokerageModel : implements
    IDataQueueHandler <|.. ProjectXDataQueueHandler : implements
    IHistoryProvider <|.. ProjectXHistoryProvider : implements
    IDataDownloader <|.. ProjectXBrokerageDownloader : implements
    IFeeModel <|.. ProjectXFeeModel : implements

    %% Composition Relationships
    ProjectXBrokerageFactory --> ProjectXBrokerage : creates
    ProjectXBrokerageFactory --> ProjectXBrokerageModel : creates
    ProjectXBrokerage --> ProjectXSymbolMapper : uses
    ProjectXBrokerage --> MarqSpecClient : uses
    ProjectXBrokerage --> ProjectXBrokerageModel : validates with
    ProjectXDataQueueHandler --> MarqSpecClient : uses
    ProjectXDataQueueHandler --> ProjectXSymbolMapper : uses
    ProjectXHistoryProvider --> MarqSpecClient : uses
    ProjectXHistoryProvider --> ProjectXSymbolMapper : uses
    ProjectXBrokerageDownloader --> MarqSpecClient : uses
```

### Sequence Diagram

```mermaid
sequenceDiagram
    participant User as Trading Algorithm
    participant LEAN as LEAN Engine
    participant Factory as ProjectXBrokerageFactory
    participant Brokerage as ProjectXBrokerage
    participant Mapper as ProjectXSymbolMapper
    participant Client as MarqSpec.Client.ProjectX
    participant ProjectX as ProjectX Platform

    %% Initialization Phase
    rect rgb(230, 245, 255)
    Note over User,ProjectX: Initialization Phase
    User->>LEAN: Initialize Algorithm
    LEAN->>Factory: Create Brokerage Instance
    Factory->>Factory: Parse Configuration<br/>(API keys, settings)
    Factory->>Brokerage: Instantiate with credentials
    Brokerage->>Client: Initialize API Client
    Client->>ProjectX: Authenticate
    ProjectX-->>Client: Session Token
    Client-->>Brokerage: Connection Established
    Brokerage-->>LEAN: Brokerage Ready
    LEAN-->>User: OnInitialize()
    end

    %% Market Data Subscription
    rect rgb(232, 245, 233)
    Note over User,ProjectX: Market Data Subscription
    User->>LEAN: Subscribe to Market Data<br/>(e.g., ES, NQ futures)
    LEAN->>Brokerage: Subscribe(Symbol)
    Brokerage->>Mapper: GetBrokerageSymbol(Symbol)
    Mapper-->>Brokerage: "ESH25"
    Brokerage->>Client: Subscribe to WebSocket<br/>(ticker: "ESH25")
    Client->>ProjectX: WebSocket Subscribe Request
    ProjectX-->>Client: Subscription Confirmed
    end

    %% Real-time Data Stream
    rect rgb(255, 243, 224)
    Note over User,ProjectX: Real-time Data Stream
    loop Continuous Market Data
        ProjectX-->>Client: Market Data Updates<br/>(tick, quote, trade)
        Client-->>Brokerage: Raw Tick Data
        Brokerage->>Mapper: GetLeanSymbol("ESH25")
        Mapper-->>Brokerage: Symbol(ES, Future)
        Brokerage->>Brokerage: Convert to LEAN Tick format
        Brokerage-->>LEAN: LEAN Tick Data
        LEAN->>LEAN: Aggregate to Slice
        LEAN-->>User: OnData(Slice)
    end
    end

    %% Order Placement
    rect rgb(243, 229, 245)
    Note over User,ProjectX: Order Execution
    User->>LEAN: Place Market Order<br/>(Buy 1 ES contract)
    LEAN->>Brokerage: PlaceOrder(Order)
    Brokerage->>Brokerage: Validate Order
    Brokerage->>Mapper: GetBrokerageSymbol(Symbol)
    Mapper-->>Brokerage: "ESH25"
    Brokerage->>Client: Submit Order<br/>(ticker: "ESH25", qty: 1)
    Client->>ProjectX: REST API Order Request
    ProjectX-->>Client: Order Accepted<br/>(Order ID: 12345)
    Client-->>Brokerage: Order Confirmation Event
    Brokerage-->>LEAN: OrderEvent(Submitted)
    LEAN-->>User: OnOrderEvent(Submitted)
    end

    %% Order Fill
    rect rgb(252, 228, 236)
    Note over User,ProjectX: Order Fill
    ProjectX-->>Client: Fill Notification<br/>(Order ID: 12345, Filled)
    Client-->>Brokerage: Fill Event<br/>(Price, Quantity, Time)
    Brokerage->>Brokerage: Update Holdings
    Brokerage-->>LEAN: OrderEvent(Filled)
    LEAN->>LEAN: Update Portfolio
    LEAN-->>User: OnOrderEvent(Filled)
    end

    %% Position Sync
    rect rgb(230, 245, 255)
    Note over User,ProjectX: Account Synchronization
    LEAN->>Brokerage: GetAccountHoldings()
    Brokerage->>Client: Query Account Positions
    Client->>ProjectX: REST API Account Request
    ProjectX-->>Client: Account Holdings Data
    Client-->>Brokerage: Position List
    Brokerage->>Mapper: Map Symbols to LEAN format
    Mapper-->>Brokerage: LEAN Symbols
    Brokerage-->>LEAN: Holdings List
    LEAN->>LEAN: Reconcile Portfolio
    end
```

### Glossary

| Term | Definition |
|------|------------|
| **LEAN** | QuantConnect's open-source algorithmic trading engine |
| **ProjectX** | Trading platform providing futures market access |
| **MarqSpec.Client.ProjectX** | C# API client library for ProjectX |
| **IBrokerage** | LEAN interface for brokerage operations |
| **ISymbolMapper** | LEAN interface for symbol translation |
| **IDataQueueHandler** | LEAN interface for real-time data |
| **IHistoryProvider** | LEAN interface for historical data |
| **Futures Contract** | Derivative contract to buy/sell asset at future date |
| **Tick Data** | Individual trade or quote data points |
| **WebSocket** | Protocol for real-time bi-directional communication |
| **REST API** | HTTP-based API for request/response operations |
| **Market Data** | Price, volume, and other trading information |
| **Order Book** | Collection of buy/sell orders at various prices |

### Change Log

#### v0.1.0 - Initial Setup (March 2026)
- Repository structure initialized
- Basic factory and brokerage stubs
- Project references configured
- PRD and architecture documentation

#### v0.2.0 - Core Trading (Planned)
- IBrokerage implementation
- Order management
- Account synchronization
- Connection handling

*See GitHub Releases for detailed changelogs*

---

**Document Version:** 2.0  
**Last Updated:** March 2026  
**Next Review:** After Phase 2 Completion