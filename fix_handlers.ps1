$f = "C:\Users\Adam\source\repos\adammarquette\Lean.Brokerages.ProjectX\QuantConnect.ProjectXBrokerage\ProjectXBrokerage.cs"
$c = [IO.File]::ReadAllText($f)
$d = [char]36
$nl = "`r`n"

# ── 1. Add OnPriceUpdateReceived + OnTradeUpdateReceived before OnConnectionStatusChanged ──────

$old = '        /// <summary>' + $nl +
       '        /// Handles WebSocket connection status changes; triggers reconnection on failure.' + $nl +
       '        /// </summary>' + $nl +
       '        private void OnConnectionStatusChanged(object sender, ConnectionStatusChange e)'

$new  = '        /// <summary>' + $nl
$new += '        /// Handles real-time best bid/offer price updates from the ProjectX WebSocket.' + $nl
$new += '        /// </summary>' + $nl
$new += '        private void OnPriceUpdateReceived(object sender, PxPriceUpdate e)' + $nl
$new += '        {' + $nl
$new += '            try' + $nl
$new += '            {' + $nl
$new += '                if (!_subscribedContractIds.TryGetValue(e.ContractId, out var symbol))' + $nl
$new += '                {' + $nl
$new += '                    Log.Debug(' + $d + '"ProjectXBrokerage.OnPriceUpdateReceived(): Received price update for untracked contract {e.ContractId}");' + $nl
$new += '                    return;' + $nl
$new += '                }' + $nl + $nl
$new += '                var tick = new Tick' + $nl
$new += '                {' + $nl
$new += '                    Symbol = symbol,' + $nl
$new += '                    Time = e.Timestamp,' + $nl
$new += '                    TickType = TickType.Quote,' + $nl
$new += '                    BidPrice = e.BidPrice,' + $nl
$new += '                    AskPrice = e.AskPrice,' + $nl
$new += '                    BidSize = (decimal)e.BidSize,' + $nl
$new += '                    AskSize = (decimal)e.AskSize,' + $nl
$new += '                    Value = (e.BidPrice + e.AskPrice) / 2m' + $nl
$new += '                };' + $nl + $nl
$new += '                _aggregator.Update(tick);' + $nl
$new += '            }' + $nl
$new += '            catch (Exception ex)' + $nl
$new += '            {' + $nl
$new += '                Log.Error(ex, ' + $d + '"ProjectXBrokerage.OnPriceUpdateReceived(): Error processing price update for contract {e.ContractId}");' + $nl
$new += '            }' + $nl
$new += '        }' + $nl + $nl
$new += '        /// <summary>' + $nl
$new += '        /// Handles real-time trade print updates from the ProjectX WebSocket.' + $nl
$new += '        /// </summary>' + $nl
$new += '        private void OnTradeUpdateReceived(object sender, PxTradeUpdate e)' + $nl
$new += '        {' + $nl
$new += '            try' + $nl
$new += '            {' + $nl
$new += '                if (!_subscribedContractIds.TryGetValue(e.ContractId, out var symbol))' + $nl
$new += '                {' + $nl
$new += '                    Log.Debug(' + $d + '"ProjectXBrokerage.OnTradeUpdateReceived(): Received trade update for untracked contract {e.ContractId}");' + $nl
$new += '                    return;' + $nl
$new += '                }' + $nl + $nl
$new += '                var tick = new Tick' + $nl
$new += '                {' + $nl
$new += '                    Symbol = symbol,' + $nl
$new += '                    Time = e.Timestamp,' + $nl
$new += '                    TickType = TickType.Trade,' + $nl
$new += '                    Value = e.Price,' + $nl
$new += '                    Quantity = (decimal)e.Quantity' + $nl
$new += '                };' + $nl + $nl
$new += '                _aggregator.Update(tick);' + $nl
$new += '            }' + $nl
$new += '            catch (Exception ex)' + $nl
$new += '            {' + $nl
$new += '                Log.Error(ex, ' + $d + '"ProjectXBrokerage.OnTradeUpdateReceived(): Error processing trade update for contract {e.ContractId}");' + $nl
$new += '            }' + $nl
$new += '        }' + $nl + $nl
$new += '        /// <summary>' + $nl
$new += '        /// Handles WebSocket connection status changes; triggers reconnection on failure.' + $nl
$new += '        /// </summary>' + $nl
$new += '        private void OnConnectionStatusChanged(object sender, ConnectionStatusChange e)'

if ($c.Contains($old)) {
    $c = $c.Replace($old, $new)
    Write-Host "STEP 1: handlers added"
} else {
    Write-Host "STEP 1: OLD STRING NOT FOUND"
    exit 1
}

# ── 2. CleanupClients: unwire Price + Trade events ─────────────────────────────────────────────

$old2 = '                _wsClient.ConnectionStatusChanged -= OnConnectionStatusChanged;' + $nl +
        '                _wsClient.OrderUpdateReceived -= OnOrderUpdateReceived;' + $nl +
        '                }'

$new2 = '                _wsClient.ConnectionStatusChanged -= OnConnectionStatusChanged;' + $nl +
        '                _wsClient.OrderUpdateReceived -= OnOrderUpdateReceived;' + $nl +
        '                _wsClient.PriceUpdateReceived -= OnPriceUpdateReceived;' + $nl +
        '                _wsClient.TradeUpdateReceived -= OnTradeUpdateReceived;' + $nl +
        '                }'

if ($c.Contains($old2)) {
    $c = $c.Replace($old2, $new2)
    Write-Host "STEP 2: CleanupClients unwires added"
} else {
    Write-Host "STEP 2: OLD STRING NOT FOUND"
    exit 1
}

# ── 3. HandleReconnection: replace TODO comment with resubscription loop ───────────────────────

$old3 = '                    // TODO: Resubscribe to data feeds'

$new3  = '                    // Resubscribe to all active market data feeds' + $nl
$new3 += '                    var contractsToResubscribe = _subscribedContractIds.Keys.ToList();' + $nl
$new3 += '                    Log.Debug(' + $d + '"ProjectXBrokerage.HandleReconnection(): Resubscribing to {contractsToResubscribe.Count} market data feed(s)");' + $nl
$new3 += '                    foreach (var contractId in contractsToResubscribe)' + $nl
$new3 += '                    {' + $nl
$new3 += '                        try' + $nl
$new3 += '                        {' + $nl
$new3 += '                            _wsClient.SubscribeToPriceUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();' + $nl
$new3 += '                            _wsClient.SubscribeToTradeUpdatesAsync(contractId, _connectionCts.Token).GetAwaiter().GetResult();' + $nl
$new3 += '                            Log.Trace(' + $d + '"ProjectXBrokerage.HandleReconnection(): Resubscribed to {contractId}");' + $nl
$new3 += '                        }' + $nl
$new3 += '                        catch (Exception resubEx)' + $nl
$new3 += '                        {' + $nl
$new3 += '                            Log.Error(resubEx, ' + $d + '"ProjectXBrokerage.HandleReconnection(): Failed to resubscribe to {contractId}");' + $nl
$new3 += '                        }' + $nl
$new3 += '                    }'

if ($c.Contains($old3)) {
    $c = $c.Replace($old3, $new3)
    Write-Host "STEP 3: HandleReconnection TODO replaced"
} else {
    Write-Host "STEP 3: OLD STRING NOT FOUND"
    exit 1
}

[IO.File]::WriteAllText($f, $c)
Write-Host "ALL DONE — file written"
