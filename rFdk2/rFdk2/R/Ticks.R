#' Gets the ticks as requested
#' @param symbol Symbol
#' @param from From
#' @param count Count
#'
#' @export
tt2.QS.GetTickList <- function(symbol, from, count) {
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickList',symbol, from, count)
  tt2.QS.GetTickFrame()
}

#' Gets the ticks as requested
#' @param symbol Symbol
#' @param from From
#' @param to to
#'
#' @export
tt2.QS.DownloadTicks <- function(symbol, from, to) {
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'DownloadTicks',symbol, from, to)
  tt2.QS.GetTickFrame()
}

#' Get tick table
tt2.QS.GetTickFrame<-function()
{
  BidPrice = tt2.QS.GetTickBidPrice()
  BidVolume = tt2.QS.GetTickBidVolume()
  AskPrice = tt2.QS.GetTickAskPrice()
  AskVolume = tt2.QS.GetTickAskVolume()
  Timestamp = tt2.QS.GetTickTimestamp()
  data.table(BidPrice, BidVolume, AskPrice, AskVolume, Timestamp)
}
#' Get tick field
tt2.QS.GetTickBidPrice<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickBidPrice')
}
#' Get tick field
tt2.QS.GetTickBidVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickBidVolume')
}
#' Get tick field
tt2.QS.GetTickAskPrice<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickAskPrice')
}
#' Get tick field
tt2.QS.GetTickAskVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickAskVolume')
}
#' Get tick field
tt2.QS.GetTickTimestamp<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickTimestamp')
}
