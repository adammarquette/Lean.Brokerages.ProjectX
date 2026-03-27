# ProjectX Brokerage - Architecture Diagrams

This document provides an overview of the architecture diagrams included in the PRD.md file. These diagrams illustrate the structure, relationships, and workflows of the ProjectX LEAN brokerage integration.

## Overview

The project includes three main Mermaid diagrams that visualize different aspects of the architecture:

1. **Architecture Diagram** - System component structure and data flow
2. **Class Diagram** - Object-oriented design and relationships
3. **Sequence Diagram** - Runtime behavior and interaction patterns

---

## 1. Architecture Diagram (Component View)

**Type:** Flowchart (graph TB - Top to Bottom)  
**Purpose:** Shows the layered architecture and component interactions

### Layers

#### User Layer (Blue - #e1f5ff)
- **Trading Algorithm** - User's LEAN algorithm
- **Configuration** - API keys and settings

#### LEAN Engine Core (Orange - #fff3e0)
- **Algorithm Framework** - Core LEAN algorithm execution
- **Data Manager** - Manages market data feeds
- **Order Manager** - Handles order lifecycle
- **Portfolio Manager** - Tracks positions and account state

#### ProjectX Brokerage Integration (Purple - #f3e5f5)
- **Factory** - Creates and configures brokerage instances
- **Core Trading Components**
  - Brokerage - Main integration class
  - Symbol Mapper - Translates symbols between formats
  - Brokerage Model - Defines trading rules
- **Data Components**
  - Data Queue Handler - Real-time data streaming
  - History Provider - Historical data retrieval
  - Data Downloader - Bulk data downloads
- **Fee Model** - Commission and fee calculations

#### API Client Layer (Green - #e8f5e9)
- **MarqSpec.Client.ProjectX** - External API client library
- **Client Modules**
  - Authentication
  - Order Management
  - Market Data
  - Account & Positions

#### ProjectX Platform (Pink - #fce4ec)
- **REST API** - HTTP-based operations
- **WebSocket** - Real-time data streaming
- **Trading Infrastructure** - Exchange connectivity

### Key Relationships

- Factory instantiates Brokerage and configures data providers
- Brokerage uses Symbol Mapper for all symbol translations
- All integration components use MarqSpec.Client.ProjectX for API access
- Data flows from Platform → Client → Brokerage → LEAN → Algorithm

---

## 2. Class Diagram (Object-Oriented Design)

**Type:** Class Diagram  
**Purpose:** Shows interfaces, implementations, and object relationships

### Interface Hierarchy

#### LEAN Core Interfaces (Implemented by Brokerage)
- **IBrokerageFactory** - Factory pattern for brokerage creation
- **IBrokerage** - Core trading operations
- **ISymbolMapper** - Symbol translation
- **IBrokerageModel** - Trading rules and constraints
- **IDataQueueHandler** - Real-time data subscription
- **IHistoryProvider** - Historical data queries
- **IDataDownloader** - Bulk data downloads
- **IFeeModel** - Fee calculations

### Implementation Classes

#### ProjectX Implementations
- **ProjectXBrokerageFactory**
  - Properties: BrokerageData
  - Methods: CreateBrokerage(), GetBrokerageModel(), Dispose()

- **ProjectXBrokerage**
  - Private Fields: _apiClient, _symbolMapper, _aggregator, _webSocket
  - Methods: PlaceOrder(), CancelOrder(), GetAccountHoldings(), Connect(), Disconnect()

- **ProjectXSymbolMapper**
  - Private Fields: _futuresMap
  - Methods: GetLeanSymbol(), GetBrokerageSymbol(), ParseFuturesTicker()

- **ProjectXBrokerageModel**
  - Properties: DefaultMarkets, MarketMap
  - Methods: CanSubmitOrder(), GetFeeModel()

- **ProjectXDataQueueHandler**
  - Private Fields: _subscriptionManager, _webSocket
  - Methods: Subscribe(), Unsubscribe()

- **ProjectXHistoryProvider**
  - Private Fields: _apiClient
  - Methods: GetHistory(), Initialize()

- **ProjectXBrokerageDownloader**
  - Private Fields: _apiClient
  - Methods: Get()

- **ProjectXFeeModel**
  - Methods: GetOrderFee(), CalculateExchangeFees()

#### External Dependency
- **MarqSpecClient** (External Library)
  - Properties: OrderManagement, MarketData, AccountInfo, Authentication

### Relationships

**Inheritance (implements):**
- All ProjectX classes implement their corresponding LEAN interfaces

**Composition (uses):**
- Factory creates Brokerage and BrokerageModel
- Brokerage uses SymbolMapper, MarqSpecClient, and BrokerageModel
- All data components use MarqSpecClient and SymbolMapper

---

## 3. Sequence Diagram (Runtime Behavior)

**Type:** Sequence Diagram  
**Purpose:** Shows the temporal flow of operations during live trading

### Phases

#### Phase 1: Initialization (Blue Background)
1. User initializes algorithm
2. LEAN creates brokerage via Factory
3. Factory parses configuration
4. Brokerage initializes API client
5. Client authenticates with ProjectX
6. Connection established

#### Phase 2: Market Data Subscription (Green Background)
1. User subscribes to symbols (e.g., ES, NQ futures)
2. LEAN calls Brokerage.Subscribe()
3. SymbolMapper converts LEAN Symbol to broker ticker (ES → "ESH25")
4. Client subscribes to WebSocket feed
5. ProjectX confirms subscription

#### Phase 3: Real-time Data Stream (Orange Background)
**Continuous Loop:**
1. ProjectX sends market data updates
2. Client receives raw tick data
3. SymbolMapper converts broker ticker to LEAN Symbol
4. Brokerage converts to LEAN Tick format
5. LEAN aggregates ticks into Slice
6. Algorithm receives OnData(Slice) callback

#### Phase 4: Order Execution (Purple Background)
1. User places order (e.g., Buy 1 ES contract)
2. LEAN calls Brokerage.PlaceOrder()
3. Brokerage validates order
4. SymbolMapper converts Symbol to broker ticker
5. Client submits order via REST API
6. ProjectX accepts order (returns Order ID)
7. Brokerage emits OrderEvent(Submitted)
8. Algorithm receives OnOrderEvent callback

#### Phase 5: Order Fill (Pink Background)
1. ProjectX sends fill notification
2. Client receives fill event
3. Brokerage updates internal holdings
4. Brokerage emits OrderEvent(Filled)
5. LEAN updates portfolio
6. Algorithm receives OnOrderEvent(Filled) callback

#### Phase 6: Account Synchronization (Blue Background)
1. LEAN requests account holdings
2. Brokerage queries positions via Client
3. Client requests account data from ProjectX
4. ProjectX returns holdings data
5. SymbolMapper converts symbols to LEAN format
6. LEAN reconciles portfolio state

### Participants

- **Trading Algorithm** - User's algorithm code
- **LEAN Engine** - QuantConnect LEAN framework
- **ProjectXBrokerageFactory** - Brokerage factory
- **ProjectXBrokerage** - Main brokerage implementation
- **ProjectXSymbolMapper** - Symbol translator
- **MarqSpec.Client.ProjectX** - API client library
- **ProjectX Platform** - Trading platform backend

---

## Diagram Rendering

### GitHub/GitLab
Mermaid diagrams render natively in GitHub and GitLab markdown viewers.

### Local Viewing
Use one of these tools to view diagrams locally:

1. **VS Code Extensions**
   - Markdown Preview Mermaid Support
   - Mermaid Markdown Syntax Highlighting

2. **Mermaid Live Editor**
   - https://mermaid.live/
   - Paste diagram code to preview

3. **Command Line**
   ```bash
   npm install -g @mermaid-js/mermaid-cli
   mmdc -i PRD.md -o diagrams.pdf
   ```

### Export Formats
- PNG, SVG, PDF via Mermaid CLI
- Copy/paste into documentation tools (Confluence, Notion)
- Embed in presentations (PowerPoint, Google Slides)

---

## Diagram Maintenance

### When to Update Diagrams

**Architecture Diagram:**
- Adding new components or modules
- Changing layer boundaries
- Modifying data flow paths

**Class Diagram:**
- Adding/removing interfaces
- Changing inheritance hierarchy
- Adding significant methods or properties

**Sequence Diagram:**
- Changing initialization flow
- Adding new interaction patterns
- Modifying error handling paths

### Best Practices

1. **Keep Diagrams Synchronized** - Update all related diagrams together
2. **Use Consistent Naming** - Match class/component names exactly
3. **Color Coding** - Maintain consistent colors for layers
4. **Annotations** - Add notes for complex interactions
5. **Versioning** - Document diagram changes in commit messages

### Validation

Before committing diagram changes:
1. ✅ Verify syntax with Mermaid Live Editor
2. ✅ Check all node/edge connections are valid
3. ✅ Ensure colors render correctly
4. ✅ Test on GitHub preview
5. ✅ Update this documentation if needed

---

## Additional Diagrams (Future)

### Planned Additions

1. **Error Handling Flow**
   - WebSocket disconnection recovery
   - Order rejection handling
   - Rate limit backoff strategy

2. **State Machine Diagram**
   - Connection states (Disconnected → Connecting → Connected)
   - Order states (Submitted → PartiallyFilled → Filled)

3. **Deployment Diagram**
   - Cloud vs local deployment
   - Container architecture
   - Network topology

4. **Data Flow Diagram**
   - Historical data retrieval
   - Data normalization pipeline
   - Cache layers

---

## References

- [Mermaid Documentation](https://mermaid.js.org/)
- [PRD.md](./PRD.md) - Full project documentation
- [LEAN Architecture](https://www.quantconnect.com/docs/v2/lean-engine/architecture)

---

**Last Updated:** March 2026  
**Version:** 1.0
