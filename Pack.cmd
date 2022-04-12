rem @echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR
if [%2]==[] goto INVALID_COMMAND_LINE_ERROR2

call Build.cmd %2

set PACKAGE_DIR="Pack\TickTrader FDK "%1""

rd /S /Q %PACKAGE_DIR%
mkdir %PACKAGE_DIR% || goto END
xcopy README.md %PACKAGE_DIR% || goto END
xcopy LICENSE %PACKAGE_DIR% || goto END

rem Bin

mkdir %PACKAGE_DIR%\Bin || goto END
xcopy Lib\ICSharpCode.SharpZipLib.dll %PACKAGE_DIR%\Bin || goto END
xcopy Lib\NDesk.Options.dll %PACKAGE_DIR%\Bin || goto END
xcopy Lib\Snappy.NET.dll %PACKAGE_DIR%\Bin || goto END
xcopy Lib\Snappy.NET.xml %PACKAGE_DIR%\Bin || goto END
xcopy Lib\Crc32C.NET.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\TickTrader.FDK.Common.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\TickTrader.FDK.Client.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\TickTrader.FDK.Extended.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\TickTrader.FDK.Calculator.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\TickTrader.FDK.Standard.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\SoftFX.Net.Core.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\SoftFX.Net.OrderEntry.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\SoftFX.Net.QuoteFeed.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\SoftFX.Net.QuoteStore.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\net46\SoftFX.Net.TradeCapture.dll %PACKAGE_DIR%\Bin || goto END

rem xcopy Bin\Release\QuoteFeedAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\QuoteFeedSyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\OrderEntryAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\OrderEntrySyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\QuoteStoreAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\QuoteStoreSyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\TradeCaptureAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\TradeCaptureSyncSample.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\DataFeedExamples.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\DataTradeExamples.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\TradeFeedExamples.exe %PACKAGE_DIR%\Bin || goto END
rem xcopy Bin\Release\StandardExamples.exe %PACKAGE_DIR%\Bin || goto END

rem Doc

mkdir %PACKAGE_DIR%\Doc || goto END

rem Samples

mkdir %PACKAGE_DIR%\Samples || goto END
xcopy Samples\Samples.sln %PACKAGE_DIR%\Samples || goto END

mkdir %PACKAGE_DIR%\Samples\DataFeedExamples || goto END
xcopy Samples\DataFeedExamples\DataFeedExamples.csproj %PACKAGE_DIR%\Samples\DataFeedExamples || goto END
xcopy Samples\DataFeedExamples\*.cs %PACKAGE_DIR%\Samples\DataFeedExamples || goto END

mkdir %PACKAGE_DIR%\Samples\DataTradeExamples || goto END
xcopy Samples\DataTradeExamples\DataTradeExamples.csproj %PACKAGE_DIR%\Samples\DataTradeExamples || goto END
xcopy Samples\DataTradeExamples\*.cs %PACKAGE_DIR%\Samples\DataTradeExamples || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END
xcopy Samples\OrderEntryAsyncSample\OrderEntryAsyncSample.csproj %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END
xcopy Samples\OrderEntryAsyncSample\*.cs %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntrySyncSample || goto END
xcopy Samples\OrderEntrySyncSample\OrderEntrySyncSample.csproj %PACKAGE_DIR%\Samples\OrderEntrySyncSample || goto END
xcopy Samples\OrderEntrySyncSample\*.cs %PACKAGE_DIR%\Samples\OrderEntrySyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END
xcopy Samples\QuoteFeedAsyncSample\QuoteFeedAsyncSample.csproj %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END
xcopy Samples\QuoteFeedAsyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedSyncSample || goto END
xcopy Samples\QuoteFeedSyncSample\QuoteFeedSyncSample.csproj %PACKAGE_DIR%\Samples\QuoteFeedSyncSample || goto END
xcopy Samples\QuoteFeedSyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteFeedSyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END
xcopy Samples\QuoteStoreAsyncSample\QuoteStoreAsyncSample.csproj %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END
xcopy Samples\QuoteStoreAsyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreSyncSample || goto END
xcopy Samples\QuoteStoreSyncSample\QuoteStoreSyncSample.csproj %PACKAGE_DIR%\Samples\QuoteStoreSyncSample || goto END
xcopy Samples\QuoteStoreSyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteStoreSyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\StandardExamples || goto END
xcopy Samples\StandardExamples\StandardExamples.csproj %PACKAGE_DIR%\Samples\StandardExamples || goto END
xcopy Samples\StandardExamples\*.cs %PACKAGE_DIR%\Samples\StandardExamples || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END
xcopy Samples\TradeCaptureAsyncSample\TradeCaptureAsyncSample.csproj %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END
xcopy Samples\TradeCaptureAsyncSample\*.cs %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureSyncSample || goto END
xcopy Samples\TradeCaptureSyncSample\TradeCaptureSyncSample.csproj %PACKAGE_DIR%\Samples\TradeCaptureSyncSample || goto END
xcopy Samples\TradeCaptureSyncSample\*.cs %PACKAGE_DIR%\Samples\TradeCaptureSyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END
xcopy Samples\TradeFeedExamples\TradeFeedExamples.csproj %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END
xcopy Samples\TradeFeedExamples\*.cs %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END

rem Zip

cd Pack || goto END
del "TickTrader FDK *.zip"
7z.exe a -tzip "TickTrader FDK "%1".zip" "TickTrader FDK "%1"" || goto END
cd ..

echo Succeeded
goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Package version is not specified
exit 42

:INVALID_COMMAND_LINE_ERROR2
echo Error: Build version is not specified
exit 42

:END
exit %ERRORLEVEL%
