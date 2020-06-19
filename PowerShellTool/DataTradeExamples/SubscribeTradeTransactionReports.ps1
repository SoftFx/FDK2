Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectTrade
$trade.Server.SubscribeTradeTransactionReports([datetime]::new(2019, 1, 1), $false);
DisconnectTrade