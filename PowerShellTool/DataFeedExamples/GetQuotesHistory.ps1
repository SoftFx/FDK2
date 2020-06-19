Import-Module ([xml](Get-Content config.xml)).root.modulePath;
<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$feed.Server.GetQuotesHistory("TTT_EURUSD", [datetime]::new(2019, 1, 1), [datetime]::UtcNow, 10);

DisconnectFeed