Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectTrade

$trade.Server.GetTradeTransactionReportsHistory([TickTrader.FDK.Common.TimeDirection]::Backward, [datetime]::new(2019, 1, 1), [datetime]::UtcNow, $false);

DisconnectTrade