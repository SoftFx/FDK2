rem @echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR

set PACKAGE_DIR="Package\TickTrader FDK "%1""

rd /S /Q %PACKAGE_DIR%
mkdir %PACKAGE_DIR% || goto END
xcopy README.md %PACKAGE_DIR% || goto END
xcopy LICENSE %PACKAGE_DIR% || goto END

rem Bin

mkdir %PACKAGE_DIR%\Bin || goto END
xcopy Bin\ICSharpCode.SharpZipLib.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\NDesk.Options.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\SoftFX.Net.Core.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\SoftFX.Net.QuoteFeed.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\SoftFX.Net.OrderEntry.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\SoftFX.Net.QuoteStore.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\SoftFX.Net.TradeCapture.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.Common.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.QuoteFeed.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.OrderEntry.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.QuoteStore.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.TradeCapture.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.Extended.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.Calculator.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TickTrader.FDK.Standard.dll %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\QuoteFeedAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\QuoteFeedSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\OrderEntryAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\OrderEntrySample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\QuoteStoreAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\QuoteStoreSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TradeCaptureAsyncSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TradeCaptureSample.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\DataFeedExamples.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\DataTradeExamples.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\TradeFeedExamples.exe %PACKAGE_DIR%\Bin || goto END
xcopy Bin\Release\StandardExamples.exe %PACKAGE_DIR%\Bin || goto END

rem Doc

mkdir %PACKAGE_DIR%\Doc || goto END

rem Samples

mkdir %PACKAGE_DIR%\Samples || goto END
xcopy Samples\Samples.sln %PACKAGE_DIR%\Samples || goto END

mkdir %PACKAGE_DIR%\Samples\DataFeedExamples || goto END
xcopy Samples\DataFeedExamples\DataFeedExamples.csproj %PACKAGE_DIR%\Samples\DataFeedExamples || goto END
xcopy Samples\DataFeedExamples\*.cs %PACKAGE_DIR%\Samples\DataFeedExamples || goto END

mkdir %PACKAGE_DIR%\Samples\DataFeedExamples\Properties || goto END
xcopy Samples\DataFeedExamples\Properties\*.cs %PACKAGE_DIR%\Samples\DataFeedExamples\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\DataTradeExamples || goto END
xcopy Samples\DataTradeExamples\DataTradeExamples.csproj %PACKAGE_DIR%\Samples\DataTradeExamples || goto END
xcopy Samples\DataTradeExamples\*.cs %PACKAGE_DIR%\Samples\DataTradeExamples || goto END
mkdir %PACKAGE_DIR%\Samples\DataTradeExamples\Properties || goto END
xcopy Samples\DataTradeExamples\Properties\*.cs %PACKAGE_DIR%\Samples\DataTradeExamples\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END
xcopy Samples\OrderEntryAsyncSample\OrderEntryAsyncSample.csproj %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END
xcopy Samples\OrderEntryAsyncSample\*.cs %PACKAGE_DIR%\Samples\OrderEntryAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntryAsyncSample\Properties || goto END
xcopy Samples\OrderEntryAsyncSample\Properties\*.cs %PACKAGE_DIR%\Samples\OrderEntryAsyncSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntrySample || goto END
xcopy Samples\OrderEntrySample\OrderEntrySample.csproj %PACKAGE_DIR%\Samples\OrderEntrySample || goto END
xcopy Samples\OrderEntrySample\*.cs %PACKAGE_DIR%\Samples\OrderEntrySample || goto END

mkdir %PACKAGE_DIR%\Samples\OrderEntrySample\Properties || goto END
xcopy Samples\OrderEntrySample\Properties\*.cs %PACKAGE_DIR%\Samples\OrderEntrySample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END
xcopy Samples\QuoteFeedAsyncSample\QuoteFeedAsyncSample.csproj %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END
xcopy Samples\QuoteFeedAsyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample\Properties || goto END
xcopy Samples\QuoteFeedAsyncSample\Properties\*.cs %PACKAGE_DIR%\Samples\QuoteFeedAsyncSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedSample || goto END
xcopy Samples\QuoteFeedSample\QuoteFeedSample.csproj %PACKAGE_DIR%\Samples\QuoteFeedSample || goto END
xcopy Samples\QuoteFeedSample\*.cs %PACKAGE_DIR%\Samples\QuoteFeedSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteFeedSample\Properties || goto END
xcopy Samples\QuoteFeedSample\Properties\*.cs %PACKAGE_DIR%\Samples\QuoteFeedSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END
xcopy Samples\QuoteStoreAsyncSample\QuoteStoreAsyncSample.csproj %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END
xcopy Samples\QuoteStoreAsyncSample\*.cs %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample\Properties || goto END
xcopy Samples\QuoteStoreAsyncSample\Properties\*.cs %PACKAGE_DIR%\Samples\QuoteStoreAsyncSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreSample || goto END
xcopy Samples\QuoteStoreSample\QuoteStoreSample.csproj %PACKAGE_DIR%\Samples\QuoteStoreSample || goto END
xcopy Samples\QuoteStoreSample\*.cs %PACKAGE_DIR%\Samples\QuoteStoreSample || goto END

mkdir %PACKAGE_DIR%\Samples\QuoteStoreSample\Properties || goto END
xcopy Samples\QuoteStoreSample\Properties\*.cs %PACKAGE_DIR%\Samples\QuoteStoreSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\StandardExamples || goto END
xcopy Samples\StandardExamples\StandardExamples.csproj %PACKAGE_DIR%\Samples\StandardExamples || goto END
xcopy Samples\StandardExamples\*.cs %PACKAGE_DIR%\Samples\StandardExamples || goto END

mkdir %PACKAGE_DIR%\Samples\StandardExamples\Properties || goto END
xcopy Samples\StandardExamples\Properties\*.cs %PACKAGE_DIR%\Samples\StandardExamples\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END
xcopy Samples\TradeCaptureAsyncSample\TradeCaptureAsyncSample.csproj %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END
xcopy Samples\TradeCaptureAsyncSample\*.cs %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample\Properties || goto END
xcopy Samples\TradeCaptureAsyncSample\Properties\*.cs %PACKAGE_DIR%\Samples\TradeCaptureAsyncSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureSample || goto END
xcopy Samples\TradeCaptureSample\TradeCaptureSample.csproj %PACKAGE_DIR%\Samples\TradeCaptureSample || goto END
xcopy Samples\TradeCaptureSample\*.cs %PACKAGE_DIR%\Samples\TradeCaptureSample || goto END

mkdir %PACKAGE_DIR%\Samples\TradeCaptureSample\Properties || goto END
xcopy Samples\TradeCaptureSample\Properties\*.cs %PACKAGE_DIR%\Samples\TradeCaptureSample\Properties || goto END

mkdir %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END
xcopy Samples\TradeFeedExamples\TradeFeedExamples.csproj %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END
xcopy Samples\TradeFeedExamples\*.cs %PACKAGE_DIR%\Samples\TradeFeedExamples || goto END

mkdir %PACKAGE_DIR%\Samples\TradeFeedExamples\Properties || goto END
xcopy Samples\TradeFeedExamples\Properties\*.cs %PACKAGE_DIR%\Samples\TradeFeedExamples\Properties || goto END

rem Xml

mkdir %PACKAGE_DIR%\Xml || goto END
xcopy Xml\Core.xml %PACKAGE_DIR%\Xml || goto END
xcopy Xml\OrderEntry.xml %PACKAGE_DIR%\Xml || goto END
xcopy Xml\QuoteFeed.xml %PACKAGE_DIR%\Xml || goto END
xcopy Xml\QuoteStore.xml %PACKAGE_DIR%\Xml || goto END
xcopy Xml\TradeCapture.xml %PACKAGE_DIR%\Xml || goto END

rem Zip

cd Package || goto END
del "TickTrader FDK "%1".zip"
7z.exe a -tzip "TickTrader FDK "%1".zip" "TickTrader FDK "%1"" || goto END
cd ..

echo Build succeeded
goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Package version is not specified

:END

