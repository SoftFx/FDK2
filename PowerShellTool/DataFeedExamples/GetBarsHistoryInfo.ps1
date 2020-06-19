Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$feed.Server.GetBarsHistoryInfo("TTT_EURUSD", [TickTrader.FDK.Common.PriceType]::Ask, [TickTrader.FDK.Common.BarPeriod]::M1);

DisconnectFeed