Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectFeed
$feed.Server.GetQuotes(([string[]]$symbols = "TTT_EURUSD", "TTT_BTCUSD"), 10);
$feed.Server.GetQuotes("TTT_EURUSD", [datetime]::new(2019, 1, 1), 10, 10);

DisconnectFeed