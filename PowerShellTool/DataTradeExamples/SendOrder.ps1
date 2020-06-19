Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectTrade
$trade.Server.SendOrder("TTT_EURUSD", [TickTrader.FDK.Common.OrderType]::Market, [TickTrader.FDK.Common.OrderSide]::Buy, 100, $null, 1, $null, $null, $null, $null, $null, "", "", $null, $false, $null)

DisconnectTrade