Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectTrade
$trade.Server.SendOrder("TTT_EURUSD", [TickTrader.FDK.Common.OrderType]::Market, [TickTrader.FDK.Common.OrderSide]::Buy, 1000, $null, 2, $null, $null, $null, $null, $null, "", "", $null, $false, $null);
$reports = $trade.Server.GetTradeRecords();
$trade.Server.ClosePosition($reports[$reports.Count - 1].OrderId);
DisconnectTrade