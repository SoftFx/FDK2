#' Gets the bars as requested
#' @param symbol Symbol
#' @param priceType PriceType
#' @param periodicity Periodicity
#' @param from From
#' @param count Count
#'
#' @export
tt2.QS.GetBarList <- function(symbol, priceType, periodicity, from, count) {
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarList',symbol, priceType, periodicity, from, count)
  tt2.QS.GetBarFrame()
}

#' Gets the bars as requested
#' @param symbol Symbol
#' @param priceType PriceType
#' @param periodicity Periodicity
#' @param from From
#' @param to To
#'
#' @export
tt2.QS.DownloadBars <- function(symbol, priceType, periodicity, from, to) {
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'DownloadBars',symbol, priceType, periodicity, from, to)
  tt2.QS.GetBarFrame()
}

#' Get bar table
tt2.QS.GetBarFrame<-function()
{
  From = tt2.QS.GetBarFrom()
  To = tt2.QS.GetBarTo()
  Open = tt2.QS.GetBarOpen()
  Close = tt2.QS.GetBarClose()
  High = tt2.QS.GetBarHigh()
  Low = tt2.QS.GetBarLow()
  Volume = tt2.QS.GetBarVolume()
  data.table(From, To, Open, Close, High, Low, Volume)
}
#' Get bar field
tt2.QS.GetBarFrom<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarFrom')
}
#' Get bar field
tt2.QS.GetBarTo<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarTo')
}
#' Get bar field
tt2.QS.GetBarOpen<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarOpen')
}
#' Get bar field
tt2.QS.GetBarClose<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarClose')
}
#' Get bar field
tt2.QS.GetBarHigh<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarHigh')
}
#' Get bar field
tt2.QS.GetBarLow<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarLow')
}
#' Get bar field
tt2.QS.GetBarVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetBarVolume')
}
