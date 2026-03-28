# Phase 2: Core Trading Implementation - Task Checklist

**Status:** Ready to Start  
**Depends On:** Phase 1 ✅ Complete  
**Next Phase:** Phase 3 - Symbol Mapping

---

## 📋 Phase 2 Overview

Implement the core `IBrokerage` interface for order execution and account management, establishing the foundation for all trading operations.

**Estimated Duration:** 3-4 weeks  
**Priority:** HIGH - Critical path item

---

## 🔌 Task 1: Connection Management (Week 1)

### 1.1 Basic Connection Infrastructure
- [ ] **Implement `Connect()` method**
  - [ ] Initialize MarqSpec.Client.ProjectX client instance
  - [ ] Parse credentials from configuration
  - [ ] Authenticate with ProjectX API
  - [ ] Initialize WebSocket connections
  - [ ] Set `IsConnected` property to true on success
  - [ ] Log connection success/failure
  - [ ] Handle authentication errors
  - [ ] Unit test: Successful connection
  - [ ] Unit test: Failed authentication
  - [ ] Unit test: Network timeout

- [ ] **Implement `Disconnect()` method**
  - [ ] Close WebSocket connections gracefully
  - [ ] Dispose MarqSpec.Client resources
  - [ ] Set `IsConnected` property to false
  - [ ] Log disconnection event
  - [ ] Handle cleanup errors
  - [ ] Unit test: Clean disconnect
  - [ ] Unit test: Disconnect while orders pending
  - [ ] Unit test: Disconnect on already disconnected state

- [ ] **Implement `IsConnected` property**
  - [ ] Add private backing field `_isConnected`
  - [ ] Thread-safe property access
  - [ ] Real-time state tracking
  - [ ] WebSocket connection state monitoring
  - [ ] Unit test: Property reflects actual state

### 1.2 Connection Resilience
- [ ] **Retry Logic**
  - [ ] Implement exponential backoff algorithm
  - [ ] Configure max retry attempts (e.g., 5)
  - [ ] Configure initial retry delay (e.g., 1 second)
  - [ ] Configure max retry delay (e.g., 60 seconds)
  - [ ] Log each retry attempt
  - [ ] Unit test: Successful retry after transient failure
  - [ ] Unit test: Failure after max retries exceeded

- [ ] **WebSocket Reconnection**
  - [ ] Detect WebSocket disconnection events
  - [ ] Automatic reconnection on connection loss
  - [ ] Resubscribe to data feeds after reconnection
  - [ ] Resync account state after reconnection
  - [ ] Log reconnection events
  - [ ] Unit test: Successful reconnection
  - [ ] Unit test: Reconnection with pending orders
  - [ ] Integration test: Reconnection with live WebSocket

- [ ] **Connection Health Monitoring**
  - [ ] Implement periodic heartbeat/ping mechanism
  - [ ] Detect stale connections
  - [ ] Trigger reconnection on stale connection
  - [ ] Log health check events
  - [ ] Unit test: Heartbeat success
  - [ ] Unit test: Reconnection on heartbeat failure

### 1.3 Configuration & Credentials
- [ ] **Parse Brokerage Configuration**
  - [ ] Read ProjectX API credentials from config
  - [ ] Read WebSocket endpoints from config
  - [ ] Read timeout settings from config
  - [ ] Validate configuration completeness
  - [ ] Handle missing configuration gracefully
  - [ ] Unit test: Valid configuration parsing
  - [ ] Unit test: Missing required fields

- [ ] **Secure Credential Handling**
  - [ ] Never log credentials in plain text
  - [ ] Clear credentials from memory after use
  - [ ] Support environment variable credentials
  - [ ] Unit test: Credential sanitization in logs

---

## 📤 Task 2: Order Management (Week 2)

### 2.1 Place Order
- [ ] **Implement `PlaceOrder()` method**
  - [ ] Validate order before submission
  - [ ] Convert LEAN Order to ProjectX order format
  - [ ] Submit order via MarqSpec.Client
  - [ ] Store order ID mapping (LEAN ↔ ProjectX)
  - [ ] Handle order submission success
  - [ ] Handle order submission failure/rejection
  - [ ] Fire `OrderStatusChanged` event
  - [ ] Log order submission
  - [ ] Return true on success, false on failure
  - [ ] Unit test: Market order placement
  - [ ] Unit test: Limit order placement
  - [ ] Unit test: Stop order placement
  - [ ] Unit test: Order rejection handling
  - [ ] Integration test: Real order submission (sandbox)

### 2.2 Update Order
- [ ] **Implement `UpdateOrder()` method**
  - [ ] Check if ProjectX supports order modification
  - [ ] Validate updated order parameters
  - [ ] Convert LEAN Order to ProjectX update format
  - [ ] Submit order modification via MarqSpec.Client
  - [ ] Handle modification success
  - [ ] Handle modification failure/rejection
  - [ ] Fire `OrderStatusChanged` event
  - [ ] Log order modification
  - [ ] Return true on success, false on failure
  - [ ] Unit test: Successful order update
  - [ ] Unit test: Update rejection
  - [ ] Unit test: Update non-existent order
  - [ ] Integration test: Real order modification (sandbox)

### 2.3 Cancel Order
- [ ] **Implement `CancelOrder()` method**
  - [ ] Retrieve ProjectX order ID from mapping
  - [ ] Submit cancellation request via MarqSpec.Client
  - [ ] Handle cancellation success
  - [ ] Handle cancellation failure (order already filled)
  - [ ] Fire `OrderStatusChanged` event with Canceled status
  - [ ] Log order cancellation
  - [ ] Return true on success, false on failure
  - [ ] Unit test: Successful cancellation
  - [ ] Unit test: Cancel already filled order
  - [ ] Unit test: Cancel non-existent order
  - [ ] Integration test: Real order cancellation (sandbox)

### 2.4 Get Open Orders
- [ ] **Implement `GetOpenOrders()` method**
  - [ ] Query open orders from ProjectX API
  - [ ] Convert ProjectX orders to LEAN Order objects
  - [ ] Handle pagination if applicable
  - [ ] Filter out filled/canceled orders
  - [ ] Log open orders retrieval
  - [ ] Return List<Order>
  - [ ] Unit test: Parse open orders response
  - [ ] Unit test: Empty open orders list
  - [ ] Integration test: Retrieve real open orders (sandbox)

### 2.5 Order Validation
- [ ] **Pre-submission Validation**
  - [ ] Check connection state before submission
  - [ ] Validate order quantity > 0
  - [ ] Validate order symbol is supported
  - [ ] Check for duplicate order submissions
  - [ ] Unit test: Reject order when disconnected
  - [ ] Unit test: Reject invalid quantity
  - [ ] Unit test: Reject unsupported symbol

### 2.6 Order ID Mapping
- [ ] **Implement Order ID Tracking**
  - [ ] Create `ConcurrentDictionary<int, string>` for LEAN ID → ProjectX ID
  - [ ] Create `ConcurrentDictionary<string, int>` for ProjectX ID → LEAN ID
  - [ ] Add mapping on order placement
  - [ ] Remove mapping on order completion
  - [ ] Thread-safe access
  - [ ] Unit test: Bidirectional lookup
  - [ ] Unit test: Thread-safe concurrent access

---

## 💰 Task 3: Account Synchronization (Week 2-3)

### 3.1 Get Account Holdings
- [ ] **Implement `GetAccountHoldings()` method**
  - [ ] Query positions from ProjectX API
  - [ ] Convert ProjectX positions to LEAN Holding objects
  - [ ] Include symbol, quantity, average price
  - [ ] Handle empty positions gracefully
  - [ ] Log holdings retrieval
  - [ ] Return List<Holding>
  - [ ] Unit test: Parse holdings response
  - [ ] Unit test: Empty holdings
  - [ ] Unit test: Multiple positions
  - [ ] Integration test: Retrieve real holdings (sandbox)

### 3.2 Get Cash Balance
- [ ] **Implement `GetCashBalance()` method**
  - [ ] Query account balance from ProjectX API
  - [ ] Convert ProjectX balance to LEAN CashAmount objects
  - [ ] Support multiple currencies if applicable
  - [ ] Include available cash and total equity
  - [ ] Log balance retrieval
  - [ ] Return List<CashAmount>
  - [ ] Unit test: Parse balance response
  - [ ] Unit test: Multiple currencies
  - [ ] Integration test: Retrieve real balance (sandbox)

### 3.3 Account Update Events
- [ ] **Subscribe to Account Updates**
  - [ ] Register for ProjectX account update WebSocket events
  - [ ] Parse account update messages
  - [ ] Update internal account state
  - [ ] Fire LEAN `AccountChanged` event
  - [ ] Log account updates
  - [ ] Unit test: Parse account update message
  - [ ] Integration test: Receive real account update (sandbox)

### 3.4 Position Reconciliation
- [ ] **Reconciliation on Connect**
  - [ ] Query positions on connection establishment
  - [ ] Reconcile with LEAN's cached positions
  - [ ] Report discrepancies via log warning
  - [ ] Update LEAN with correct positions
  - [ ] Unit test: Positions match
  - [ ] Unit test: Position discrepancy detected

---

## 🔔 Task 4: Event Handling (Week 3)

### 4.1 Order Status Events
- [ ] **Subscribe to Order Updates**
  - [ ] Register for ProjectX order status WebSocket events
  - [ ] Parse order status update messages
  - [ ] Map ProjectX status to LEAN OrderStatus enum
  - [ ] Fire `OrderStatusChanged` event
  - [ ] Include status reason/message
  - [ ] Log status changes
  - [ ] Unit test: Parse status update (New)
  - [ ] Unit test: Parse status update (PartiallyFilled)
  - [ ] Unit test: Parse status update (Filled)
  - [ ] Unit test: Parse status update (Canceled)
  - [ ] Unit test: Parse status update (Rejected)
  - [ ] Integration test: Receive real status updates (sandbox)

### 4.2 Fill Notifications
- [ ] **Handle Order Fills**
  - [ ] Parse fill messages from ProjectX
  - [ ] Extract fill price, quantity, timestamp
  - [ ] Create LEAN OrderEvent with Fill status
  - [ ] Fire `OrderStatusChanged` event
  - [ ] Update order's fill quantity
  - [ ] Handle partial fills
  - [ ] Log fill events
  - [ ] Unit test: Parse full fill
  - [ ] Unit test: Parse partial fill
  - [ ] Integration test: Receive real fills (sandbox)

### 4.3 Error & Rejection Events
- [ ] **Handle Order Rejections**
  - [ ] Parse rejection messages from ProjectX
  - [ ] Extract rejection reason
  - [ ] Create LEAN OrderEvent with Invalid status
  - [ ] Fire `OrderStatusChanged` event
  - [ ] Log rejection with reason
  - [ ] Unit test: Parse rejection (Insufficient margin)
  - [ ] Unit test: Parse rejection (Invalid symbol)
  - [ ] Integration test: Trigger rejection (sandbox)

- [ ] **Handle Connection Errors**
  - [ ] Detect connection loss events
  - [ ] Fire `Message` event with error
  - [ ] Trigger reconnection logic
  - [ ] Log connection errors
  - [ ] Unit test: Connection error handling

### 4.4 Message Event
- [ ] **Implement Informational Messages**
  - [ ] Fire `Message` event for important brokerage messages
  - [ ] Include message type (Info, Warning, Error)
  - [ ] Log all messages
  - [ ] Unit test: Message event fired

---

## 🛡️ Task 5: Error Handling & Logging (Week 3-4)

### 5.1 Exception Handling
- [ ] **Wrap MarqSpec.Client Calls**
  - [ ] Try-catch around all API calls
  - [ ] Convert API exceptions to LEAN-friendly exceptions
  - [ ] Log exception details (without sensitive data)
  - [ ] Return false/null for recoverable errors
  - [ ] Throw for unrecoverable errors
  - [ ] Unit test: API exception converted

### 5.2 Error Code Translation
- [ ] **Map ProjectX Errors to LEAN**
  - [ ] Create error code mapping table
  - [ ] Translate common error codes:
    - [ ] Insufficient margin → `OrderResponseErrorCode.InsufficientMargin`
    - [ ] Invalid symbol → `OrderResponseErrorCode.InvalidSymbol`
    - [ ] Order not found → `OrderResponseErrorCode.OrderNotFound`
  - [ ] Log original and translated error codes
  - [ ] Unit test: Error code translation

### 5.3 Structured Logging
- [ ] **Enhance Logging**
  - [ ] Use appropriate log levels (Trace, Debug, Info, Warning, Error)
  - [ ] Include correlation IDs for order tracking
  - [ ] Log request/response timing for performance
  - [ ] Sanitize sensitive data in logs
  - [ ] Unit test: Verify no credentials in logs

### 5.4 Timeout Handling
- [ ] **Configure Timeouts**
  - [ ] Set connection timeout (e.g., 30 seconds)
  - [ ] Set request timeout (e.g., 10 seconds)
  - [ ] Handle timeout exceptions gracefully
  - [ ] Retry on timeout (using retry logic)
  - [ ] Log timeout events
  - [ ] Unit test: Timeout triggers retry

---

## ✅ Task 6: Testing (Week 4)

### 6.1 Unit Tests
- [ ] **Create Test Project Structure**
  - [ ] Add `ProjectXBrokerageTests.cs`
  - [ ] Add `ProjectXOrderManagementTests.cs`
  - [ ] Add `ProjectXAccountTests.cs`
  - [ ] Add `ProjectXConnectionTests.cs`

- [ ] **Mock MarqSpec.Client**
  - [ ] Create mock ProjectX API client
  - [ ] Implement mock responses for all operations
  - [ ] Simulate success scenarios
  - [ ] Simulate error scenarios
  - [ ] Unit test: Mock client behaves correctly

- [ ] **Achieve >90% Code Coverage**
  - [ ] Run code coverage analysis
  - [ ] Write tests for uncovered branches
  - [ ] Document coverage report

### 6.2 Integration Tests
- [ ] **ProjectX Sandbox Testing**
  - [ ] Obtain ProjectX sandbox credentials
  - [ ] Configure test environment
  - [ ] Test connection establishment
  - [ ] Test order placement → fill cycle
  - [ ] Test order cancellation
  - [ ] Test account synchronization
  - [ ] Test reconnection after disconnect
  - [ ] Document test results

### 6.3 Thread Safety Tests
- [ ] **Concurrent Operations**
  - [ ] Test concurrent order submissions
  - [ ] Test concurrent subscriptions
  - [ ] Test thread-safe property access
  - [ ] Use stress testing tools
  - [ ] Document thread safety guarantees

---

## 📦 Deliverables Checklist

- [ ] **Code**
  - [ ] `ProjectXBrokerage.cs` - All IBrokerage methods implemented
  - [ ] Connection management complete
  - [ ] Order management complete
  - [ ] Account synchronization complete
  - [ ] Event handling complete
  - [ ] Error handling complete
  - [ ] All code compiles without warnings
  - [ ] Code follows LEAN coding standards

- [ ] **Tests**
  - [ ] Unit test suite with >90% coverage
  - [ ] Integration tests against sandbox
  - [ ] Thread safety tests
  - [ ] All tests passing

- [ ] **Documentation**
  - [ ] Code XML documentation for all public methods
  - [ ] Update PRD with Phase 2 completion
  - [ ] Configuration guide for ProjectX credentials
  - [ ] Error code reference documentation

- [ ] **Build & CI**
  - [ ] Solution builds successfully
  - [ ] All unit tests pass in CI
  - [ ] Integration tests documented (may not run in CI)

---

## 🚀 Ready for Phase 3

Once Phase 2 is complete, you'll be ready to start:
- **Phase 3: Symbol Mapping** - Translate between ProjectX and LEAN symbol formats
- Focus on futures ticker formats (ESH25, NQM25, etc.)
- Essential for all data and order operations

---

## 📝 Notes

- **Development Order:** Follow tasks in sequence for logical dependencies
- **Testing:** Write tests alongside implementation (TDD recommended)
- **Code Reviews:** Review each sub-phase before moving to next
- **Sandbox First:** Always test in sandbox before any live testing
- **Documentation:** Update docs as you code, not after

---

## 🔗 References

- [LEAN Contribution Guidelines](https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md)
- [LEAN Brokerage Interface Documentation](https://www.quantconnect.com/docs/v2/lean-engine/contributions/brokerages)
- MarqSpec.Client.ProjectX API Documentation
- ProjectX API Reference

---

**Created:** Post Phase 1 Completion  
**Last Updated:** March 2026  
**Maintained By:** Development Team
