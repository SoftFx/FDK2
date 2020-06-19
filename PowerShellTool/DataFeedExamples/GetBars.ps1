Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$feed.Server.GetBars("TTT_EURUSD", [TickTrader.FDK.Common.PriceType]::Ask, [TickTrader.FDK.Common.BarPeriod]::S1, [datetime]::new(2019, 1, 1), 10);
$feed.Server.GetBars("TTT_EURUSD", [TickTrader.FDK.Common.BarPeriod]::S1, [datetime]::new(2019, 1, 1), 10);

DisconnectFeed