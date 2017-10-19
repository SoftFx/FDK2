@echo off

if [%1]==[] goto INVALID_COMMAND_LINE_ERROR

set BUILD_DIR="TickTrader FDK "%1""

rd /S /Q %BUILD_DIR%
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%
if ERRORLEVEL 1 goto END

xcopy Pack.cmd %BUILD_DIR%
if ERRORLEVEL 1 goto END

xcopy LICENSE %BUILD_DIR%
if ERRORLEVEL 1 goto END

xcopy README.md %BUILD_DIR%
if ERRORLEVEL 1 goto END

rem Bin

mkdir %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\SoftFX.Net.Core.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\SoftFX.Net.QuoteFeed.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\SoftFX.Net.OrderEntry.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\SoftFX.Net.QuoteStore.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\SoftFX.Net.TradeCapture.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\TickTrader.Common.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\TickTrader.BusinessObjects.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\TickTrader.BusinessLogic.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\ICSharpCode.SharpZipLib.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\NDesk.Options.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.Common.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.QuoteFeed.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.OrderEntry.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.QuoteStore.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.TradeCapture.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.Extended.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.Calculator.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TickTrader.FDK.Standard.dll %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\QuoteFeedAsyncSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\QuoteFeedSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\OrderEntryAsyncSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\OrderEntrySample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\QuoteStoreAsyncSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\QuoteStoreSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TradeCaptureAsyncSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TradeCaptureSample.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\DataFeedExamples.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\DataTradeExamples.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\TradeFeedExamples.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

xcopy Bin\Release\StandardExamples.exe %BUILD_DIR%\Bin
if ERRORLEVEL 1 goto END

rem Doc

mkdir %BUILD_DIR%\Doc
if ERRORLEVEL 1 goto END

rem Samples

mkdir %BUILD_DIR%\Samples
if ERRORLEVEL 1 goto END

xcopy Samples\Samples.sln %BUILD_DIR%\Samples
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\DataFeedExamples
if ERRORLEVEL 1 goto END

xcopy Samples\DataFeedExamples\DataFeedExamples.csproj %BUILD_DIR%\Samples\DataFeedExamples
if ERRORLEVEL 1 goto END

xcopy Samples\DataFeedExamples\*.cs %BUILD_DIR%\Samples\DataFeedExamples
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\DataFeedExamples\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\DataFeedExamples\Properties\*.cs %BUILD_DIR%\Samples\DataFeedExamples\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\DataTradeExamples
if ERRORLEVEL 1 goto END

xcopy Samples\DataTradeExamples\DataTradeExamples.csproj %BUILD_DIR%\Samples\DataTradeExamples
if ERRORLEVEL 1 goto END

xcopy Samples\DataTradeExamples\*.cs %BUILD_DIR%\Samples\DataTradeExamples
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\DataTradeExamples\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\DataTradeExamples\Properties\*.cs %BUILD_DIR%\Samples\DataTradeExamples\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\OrderEntryAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntryAsyncSample\OrderEntryAsyncSample.csproj %BUILD_DIR%\Samples\OrderEntryAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntryAsyncSample\*.cs %BUILD_DIR%\Samples\OrderEntryAsyncSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\OrderEntryAsyncSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntryAsyncSample\Properties\*.cs %BUILD_DIR%\Samples\OrderEntryAsyncSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\OrderEntrySample
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntrySample\OrderEntrySample.csproj %BUILD_DIR%\Samples\OrderEntrySample
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntrySample\*.cs %BUILD_DIR%\Samples\OrderEntrySample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\OrderEntrySample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\OrderEntrySample\Properties\*.cs %BUILD_DIR%\Samples\OrderEntrySample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteFeedAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedAsyncSample\QuoteFeedAsyncSample.csproj %BUILD_DIR%\Samples\QuoteFeedAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedAsyncSample\*.cs %BUILD_DIR%\Samples\QuoteFeedAsyncSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteFeedAsyncSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedAsyncSample\Properties\*.cs %BUILD_DIR%\Samples\QuoteFeedAsyncSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteFeedSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedSample\QuoteFeedSample.csproj %BUILD_DIR%\Samples\QuoteFeedSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedSample\*.cs %BUILD_DIR%\Samples\QuoteFeedSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteFeedSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteFeedSample\Properties\*.cs %BUILD_DIR%\Samples\QuoteFeedSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteStoreAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreAsyncSample\QuoteStoreAsyncSample.csproj %BUILD_DIR%\Samples\QuoteStoreAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreAsyncSample\*.cs %BUILD_DIR%\Samples\QuoteStoreAsyncSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteStoreAsyncSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreAsyncSample\Properties\*.cs %BUILD_DIR%\Samples\QuoteStoreAsyncSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteStoreSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreSample\QuoteStoreSample.csproj %BUILD_DIR%\Samples\QuoteStoreSample
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreSample\*.cs %BUILD_DIR%\Samples\QuoteStoreSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\QuoteStoreSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\QuoteStoreSample\Properties\*.cs %BUILD_DIR%\Samples\QuoteStoreSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\StandardExamples
if ERRORLEVEL 1 goto END

xcopy Samples\StandardExamples\StandardExamples.csproj %BUILD_DIR%\Samples\StandardExamples
if ERRORLEVEL 1 goto END

xcopy Samples\StandardExamples\*.cs %BUILD_DIR%\Samples\StandardExamples
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\StandardExamples\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\StandardExamples\Properties\*.cs %BUILD_DIR%\Samples\StandardExamples\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeCaptureAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureAsyncSample\TradeCaptureAsyncSample.csproj %BUILD_DIR%\Samples\TradeCaptureAsyncSample
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureAsyncSample\*.cs %BUILD_DIR%\Samples\TradeCaptureAsyncSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeCaptureAsyncSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureAsyncSample\Properties\*.cs %BUILD_DIR%\Samples\TradeCaptureAsyncSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeCaptureSample
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureSample\TradeCaptureSample.csproj %BUILD_DIR%\Samples\TradeCaptureSample
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureSample\*.cs %BUILD_DIR%\Samples\TradeCaptureSample
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeCaptureSample\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\TradeCaptureSample\Properties\*.cs %BUILD_DIR%\Samples\TradeCaptureSample\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeFeedExamples
if ERRORLEVEL 1 goto END

xcopy Samples\TradeFeedExamples\TradeFeedExamples.csproj %BUILD_DIR%\Samples\TradeFeedExamples
if ERRORLEVEL 1 goto END

xcopy Samples\TradeFeedExamples\*.cs %BUILD_DIR%\Samples\TradeFeedExamples
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Samples\TradeFeedExamples\Properties
if ERRORLEVEL 1 goto END

xcopy Samples\TradeFeedExamples\Properties\*.cs %BUILD_DIR%\Samples\TradeFeedExamples\Properties
if ERRORLEVEL 1 goto END

rem Src

mkdir %BUILD_DIR%\Src
if ERRORLEVEL 1 goto END

xcopy Src\FDK.sln %BUILD_DIR%\Src
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Calculator
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\Calculator.csproj %BUILD_DIR%\Src\Calculator
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\*.cs %BUILD_DIR%\Src\Calculator
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Calculator\Adapter
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\Adapter\*.cs %BUILD_DIR%\Src\Calculator\Adapter
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Calculator\Properties
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\Properties\*.cs %BUILD_DIR%\Src\Calculator\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Calculator\Rounding
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\Rounding\*.cs %BUILD_DIR%\Src\Calculator\Rounding
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Calculator\Serialization
if ERRORLEVEL 1 goto END

xcopy Src\Calculator\Serialization\*.cs %BUILD_DIR%\Src\Calculator\Serialization
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Common
if ERRORLEVEL 1 goto END

xcopy Src\Common\Common.csproj %BUILD_DIR%\Src\Common
if ERRORLEVEL 1 goto END

xcopy Src\Common\*.cs %BUILD_DIR%\Src\Common
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Common\Properties
if ERRORLEVEL 1 goto END

xcopy Src\Common\Properties\*.cs %BUILD_DIR%\Src\Common\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Extended
if ERRORLEVEL 1 goto END

xcopy Src\Extended\Extended.csproj %BUILD_DIR%\Src\Extended
if ERRORLEVEL 1 goto END

xcopy Src\Extended\*.cs %BUILD_DIR%\Src\Extended
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Extended\Properties
if ERRORLEVEL 1 goto END

xcopy Src\Extended\Properties\*.cs %BUILD_DIR%\Src\Extended\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\OrderEntry
if ERRORLEVEL 1 goto END

xcopy Src\OrderEntry\OrderEntry.csproj %BUILD_DIR%\Src\OrderEntry
if ERRORLEVEL 1 goto END

xcopy Src\OrderEntry\*.cs %BUILD_DIR%\Src\OrderEntry
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\OrderEntry\Properties
if ERRORLEVEL 1 goto END

xcopy Src\OrderEntry\Properties\*.cs %BUILD_DIR%\Src\OrderEntry\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\QuoteFeed
if ERRORLEVEL 1 goto END

xcopy Src\QuoteFeed\QuoteFeed.csproj %BUILD_DIR%\Src\QuoteFeed
if ERRORLEVEL 1 goto END

xcopy Src\QuoteFeed\*.cs %BUILD_DIR%\Src\QuoteFeed
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\QuoteFeed\Properties
if ERRORLEVEL 1 goto END

xcopy Src\QuoteFeed\Properties\*.cs %BUILD_DIR%\Src\QuoteFeed\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\QuoteStore
if ERRORLEVEL 1 goto END

xcopy Src\QuoteStore\QuoteStore.csproj %BUILD_DIR%\Src\QuoteStore
if ERRORLEVEL 1 goto END

xcopy Src\QuoteStore\*.cs %BUILD_DIR%\Src\QuoteStore
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\QuoteStore\Properties
if ERRORLEVEL 1 goto END

xcopy Src\QuoteStore\Properties\*.cs %BUILD_DIR%\Src\QuoteStore\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\QuoteStore\Serialization
if ERRORLEVEL 1 goto END

xcopy Src\QuoteStore\Serialization\*.cs %BUILD_DIR%\Src\QuoteStore\Serialization
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\TradeCapture
if ERRORLEVEL 1 goto END

xcopy Src\TradeCapture\TradeCapture.csproj %BUILD_DIR%\Src\TradeCapture
if ERRORLEVEL 1 goto END

xcopy Src\TradeCapture\*.cs %BUILD_DIR%\Src\TradeCapture
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\TradeCapture\Properties
if ERRORLEVEL 1 goto END

xcopy Src\TradeCapture\Properties\*.cs %BUILD_DIR%\Src\TradeCapture\Properties
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Standard
if ERRORLEVEL 1 goto END

xcopy Src\Standard\Standard.csproj %BUILD_DIR%\Src\Standard
if ERRORLEVEL 1 goto END

xcopy Src\Standard\*.cs %BUILD_DIR%\Src\Standard
if ERRORLEVEL 1 goto END

mkdir %BUILD_DIR%\Src\Standard\Properties
if ERRORLEVEL 1 goto END

xcopy Src\Standard\Properties\*.cs %BUILD_DIR%\Src\Standard\Properties
if ERRORLEVEL 1 goto END

rem Xml

mkdir %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

xcopy Xml\Core.xml %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

xcopy Xml\OrderEntry.xml %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

xcopy Xml\QuoteFeed.xml %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

xcopy Xml\QuoteStore.xml %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

xcopy Xml\TradeCapture.xml %BUILD_DIR%\Xml
if ERRORLEVEL 1 goto END

echo Build succeeded
goto END

:INVALID_COMMAND_LINE_ERROR
echo Error: Package version is not specified

:END

