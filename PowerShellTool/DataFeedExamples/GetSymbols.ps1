Import-Module ([xml](Get-Content config.xml)).root.modulePath;
<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$feed.Server.GetSymbols();

DisconnectFeed