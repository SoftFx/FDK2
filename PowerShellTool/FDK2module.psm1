$config = ([xml](Get-Content config.xml)).root;
Add-Type -Path ($config.libPath + "TickTrader.FDK.Extended.dll");
Add-Type -Path ($config.libPath + "TickTrader.FDK.Common.dll");

$feed = [TickTrader.FDK.Extended.DataFeed]::new();

$trade = [TickTrader.FDK.Extended.DataTrade]::new();

function ConnectFeed()
{
    $connectionString = [TickTrader.FDK.Extended.ConnectionStringBuilder]::new();
    $connectionString.Address = $config.address;
    $connectionString.Password = $config.password;
    $connectionString.Username = $config.username;
    $connectionString.LogMessages = $true;
    $connectionString.LogEvents = $true;
    $connectionString.LogStates = $true;
    $connectionString.OperationTimeout = 30000;
    $connectionString.QuoteFeedPort = 5041;
    $connectionString.QuoteStorePort = 5042;
    $feed.Initialize($connectionString.ToString());
    $feed.Start();
    $feed.WaitForLogon();   
}

function ConnectTrade()
{
    $connectionString = [TickTrader.FDK.Extended.ConnectionStringBuilder]::new();
    $connectionString.Address = $config.address;
    $connectionString.Password = $config.password;
    $connectionString.Username = $config.username;
    $connectionString.LogMessages = $true;
    $connectionString.LogEvents = $true;
    $connectionString.LogStates = $true;
    $connectionString.OperationTimeout = 30000;
    $connectionString.OrderEntryPort = 5043;
    $connectionString.TradeCapturePort = 5044;
    $trade.Initialize($connectionString.ToString());
    $trade.Start();
    $trade.WaitForLogon();   
}


function DisconnectFeed()
{
    $feed.Stop();   
}

function DisconnectTrade()
{
    $trade.Stop();
}

Export-ModuleMember -Variable feed;
Export-ModuleMember -Variable trade;
Export-ModuleMember -Function ConnectFeed;
Export-ModuleMember -Function ConnectTrade;
Export-ModuleMember -Function DisconnectFeed;
Export-ModuleMember -Function DisconnectTrade;