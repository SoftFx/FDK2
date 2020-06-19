Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$feed.Server.GetBarsHistory("TTT_EURUSD", [TickTrader.FDK.Common.PriceType]::Ask, [TickTrader.FDK.Common.BarPeriod]::M1, [datetime]::new(2019, 1, 1), [datetime]::UtcNow);
$feed.Server.GetBarsHistory("TTT_EURUSD", [TickTrader.FDK.Common.BarPeriod]::M1, [datetime]::new(2019, 1, 1), [datetime]::UtcNow);
DisconnectFeed