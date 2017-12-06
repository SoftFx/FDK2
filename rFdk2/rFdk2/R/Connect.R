#' Initialize the CLR runtime and loads FDK2 host assembly
#'
ttmInit <- function() {
  require(rClr)
  if(!require(stringi))
  {
    install.packages("stringi")
    require(stringi)
  }
  if(!require(properties))
  {
    install.packages("properties")
    require(properties)
  }
  if(!require(data.table))
  {
    install.packages("data.table")
    require(data.table)
  }
  fileName <-system.file("data", "FDK2toR.dll", package="rFdk2")
  clrLoadAssembly(fileName)
}
#' Connects to a TT server with QuoteFeed client
#'
#' @param address Url of the server
#' @param login account you login
#' @param password password of the account
#'
#' @export
tt2.QF.Connect <- function(address = "",login = "",password = "") {
  ttmInit()
  hResult = rClr::clrCallStatic('FDK2toR.QuoteFeed', 'Connect', address, login,password)
  if(hResult != 0) stop('Unable to connect')
}


#' Disconnect from a TT server with QuoteFeed client
#' @examples
#' tt2.QF.Disconnect()
#'
#' @export
tt2.QF.Disconnect <- function() {
  hResult = rClr::clrCallStatic('FDK2toR.QuoteFeed', 'Disconnect')
}

#' Connects to a TT server with QuoteStore client
#'
#' @param address Url of the server
#' @param login account you login
#' @param password password of the account
#'
#' @export
tt2.QS.Connect <- function(address = "",login = "",password = "") {
  ttmInit()
  hResult = rClr::clrCallStatic('FDK2toR.QuoteStore', 'Connect', address, login,password)
  if(hResult != 0) stop('Unable to connect')
}


#' Disconnect from a TT server with QuoteStore client
#' @examples
#' tt2.QS.Disconnect()
#'
#' @export
tt2.QS.Disconnect <- function() {
  hResult = rClr::clrCallStatic('FDK2toR.QuoteStore', 'Disconnect')
}
