$f = "C:\Users\Adam\source\repos\adammarquette\Lean.Brokerages.ProjectX\QuantConnect.ProjectXBrokerage\ProjectXBrokerage.cs"
$c = [IO.File]::ReadAllText($f)

$sp  = '            '
$sp2 = '                '
$sp3 = '                    '
$sp4 = '                        '
$nl  = "`r`n"
$d   = [char]36  # dollar sign

$old = $sp + 'throw new NotImplementedException("ProjectXBrokerage.GetHistory(): Implementation pending Phase 6");' + $nl + '        }'

$new  = $sp + 'if (request.Resolution == Resolution.Tick)' + $nl
$new += $sp + '{' + $nl
$new += $sp2 + 'Log.Trace(' + $d + '"ProjectXBrokerage.GetHistory(): Tick resolution is not supported by the ProjectX API. Symbol: {request.Symbol}");' + $nl
$new += $sp2 + 'return null;' + $nl
$new += $sp + '}' + $nl + $nl
$new += $sp + 'try' + $nl
$new += $sp + '{' + $nl
$new += $sp2 + 'if (!IsConnected)' + $nl
$new += $sp2 + '{' + $nl
$new += $sp3 + 'Log.Error("ProjectXBrokerage.GetHistory(): Not connected to ProjectX");' + $nl
$new += $sp3 + 'return null;' + $nl
$new += $sp2 + '}' + $nl + $nl
$new += $sp2 + 'var contractId = _symbolMapper.GetBrokerageSymbol(request.Symbol);' + $nl + $nl
$new += $sp2 + 'PxAggregateBarUnit unit;' + $nl
$new += $sp2 + 'int limit;' + $nl
$new += $sp2 + 'switch (request.Resolution)' + $nl
$new += $sp2 + '{' + $nl
$new += $sp3 + 'case Resolution.Second:' + $nl
$new += $sp4 + 'unit = PxAggregateBarUnit.Second;' + $nl
$new += $sp4 + 'limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalSeconds) + 1;' + $nl
$new += $sp4 + 'break;' + $nl
$new += $sp3 + 'case Resolution.Minute:' + $nl
$new += $sp4 + 'unit = PxAggregateBarUnit.Minute;' + $nl
$new += $sp4 + 'limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalMinutes) + 1;' + $nl
$new += $sp4 + 'break;' + $nl
$new += $sp3 + 'case Resolution.Hour:' + $nl
$new += $sp4 + 'unit = PxAggregateBarUnit.Hour;' + $nl
$new += $sp4 + 'limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalHours) + 1;' + $nl
$new += $sp4 + 'break;' + $nl
$new += $sp3 + 'case Resolution.Daily:' + $nl
$new += $sp4 + 'unit = PxAggregateBarUnit.Day;' + $nl
$new += $sp4 + 'limit = (int)Math.Ceiling((request.EndTimeUtc - request.StartTimeUtc).TotalDays) + 1;' + $nl
$new += $sp4 + 'break;' + $nl
$new += $sp3 + 'default:' + $nl
$new += $sp4 + 'Log.Trace(' + $d + '"ProjectXBrokerage.GetHistory(): Resolution {request.Resolution} is not supported");' + $nl
$new += $sp4 + 'return null;' + $nl
$new += $sp2 + '}' + $nl + $nl
$new += $sp2 + '// Cap at a reasonable maximum to avoid excessive API calls' + $nl
$new += $sp2 + 'limit = Math.Min(limit, 10000);' + $nl + $nl
$new += $sp2 + 'var bars = _apiClient.GetHistoricalBarsAsync(' + $nl
$new += $sp3 + 'contractId,' + $nl
$new += $sp3 + 'request.StartTimeUtc,' + $nl
$new += $sp3 + 'request.EndTimeUtc,' + $nl
$new += $sp3 + 'unit,' + $nl
$new += $sp3 + '1,' + $nl
$new += $sp3 + 'limit,' + $nl
$new += $sp3 + 'false,' + $nl
$new += $sp3 + 'CancellationToken.None' + $nl
$new += $sp2 + ').GetAwaiter().GetResult();' + $nl + $nl
$new += $sp2 + 'return ConvertHistoricalBars(bars, request.Symbol, request.Resolution);' + $nl
$new += $sp + '}' + $nl
$new += $sp + 'catch (Exception ex)' + $nl
$new += $sp + '{' + $nl
$new += $sp2 + 'Log.Error(ex, ' + $d + '"ProjectXBrokerage.GetHistory(): Error retrieving history for {request.Symbol}");' + $nl
$new += $sp2 + 'OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "HISTORY_ERROR",' + $nl
$new += $sp3 + $d + '"Error retrieving history for {request.Symbol}: {ex.Message}"));' + $nl
$new += $sp2 + 'return null;' + $nl
$new += $sp + '}' + $nl
$new += '        }' + $nl + $nl
$new += '        private IEnumerable<BaseData> ConvertHistoricalBars(IEnumerable<PxAggregateBar> bars, Symbol symbol, Resolution resolution)' + $nl
$new += '        {' + $nl
$new += $sp + 'var period = resolution.ToTimeSpan();' + $nl
$new += $sp + 'foreach (var bar in bars)' + $nl
$new += $sp + '{' + $nl
$new += $sp2 + 'yield return new TradeBar(bar.Timestamp, symbol, bar.Open, bar.High, bar.Low, bar.Close, (decimal)bar.Volume, period);' + $nl
$new += $sp + '}' + $nl
$new += '        }'

if ($c.Contains($old)) {
    [IO.File]::WriteAllText($f, $c.Replace($old, $new))
    Write-Host "SUCCESS"
} else {
    Write-Host "OLD STRING NOT FOUND"
}
