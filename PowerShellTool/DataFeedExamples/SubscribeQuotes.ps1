Import-Module ([xml](Get-Content config.xml)).root.modulePath;

<#
    You should set up credentials in config.xml file
#>

ConnectFeed

$quotes = [Collections.Generic.List[String]]::new();
$quotes.Add("TTT_EURUSD");
$quotes.Add("TTT_BTCUSD");

$feed.Server.SubscribeToQuotes($quotes, 3);
$feed.Server.UnsubscribeQuotes($quotes);

DisconnectFeed