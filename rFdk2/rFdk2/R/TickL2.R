#' Gets the ticks L2 as requested
#' @param symbol Symbol
#' @param from From
#' @param count Count
#'
#' @export
tt2.QS.GetTickL2List <- function(symbol, from, count) {
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2List',symbol, from, count)
  tt2.QS.GetTickL2Frame()
}

#' Get tick L2 table
tt2.QS.GetTickL2Frame<-function()
{
  BidPrice = tt2.QS.GetTickL2BidPrice()
  BidVolume = tt2.QS.GetTickL2BidVolume()
  AskPrice = tt2.QS.GetTickL2AskPrice()
  AskVolume = tt2.QS.GetTickL2AskVolume()
  Timestamp = tt2.QS.GetTickL2Timestamp()
  Level = tt2.QS.GetTickL2Level()
  data.table(BidPrice, BidVolume, AskPrice, AskVolume, Timestamp, Level)
}
#' Get tick L2 field
tt2.QS.GetTickL2BidVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2BidVolume')
}
#' Get tick L2 field
tt2.QS.GetTickL2AskVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2AskVolume')
}
#' Get tick L2 field
tt2.QS.GetTickL2BidPrice<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2BidPrice')
}
#' Get tick L2 field
tt2.QS.GetTickL2AskPrice<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2AskPrice')
}
#' Get tick L2 field
tt2.QS.GetTickL2Timestamp<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2Timestamp')
}
#' Get tick L2 field
tt2.QS.GetTickL2Level<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteStore', 'GetTickL2Level')
}
