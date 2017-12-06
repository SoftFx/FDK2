#' Gets the currencies as requested
#' @examples
#' tt2.QF.GetCurrencyList()
#'
#' @export
tt2.QF.GetCurrencyList <- function() {
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetCurrencyList')
  tt2.QF.GetCurrencyFrame()
}

#' Get currency table
tt2.QF.GetCurrencyFrame<-function()
{
  Name = tt2.QF.GetCurrencyName()
  Description = tt2.QF.GetCurrencyDescription()
  SortOrder = tt2.QF.GetCurrencySortOrder()
  Precision = tt2.QF.GetCurrencyPrecision()
  data.table(Name, Description, SortOrder, Precision)
}
#' Get currency field
tt2.QF.GetCurrencyName<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetCurrencyName')
}
#' Get currency field
tt2.QF.GetCurrencyDescription<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetCurrencyDescription')
}
#' Get currency field
tt2.QF.GetCurrencySortOrder<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetCurrencySortOrder')
}
#' Get currency field
tt2.QF.GetCurrencyPrecision<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetCurrencyPrecision')
}
