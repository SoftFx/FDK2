#' Gets the symbols as requested
#' @examples
#' tt2.QF.GetSymbolList()
#'
#' @export
tt2.QF.GetSymbolList <- function() {
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolList')
  tt2.QF.GetSymbolFrame()
}

#' Get Symbol table
tt2.QF.GetSymbolFrame<-function()
{
  Name = tt2.QF.GetSymbolName()
  Currency = tt2.QF.GetSymbolCurrency()
  SettlementCurrency = tt2.QF.GetSymbolSettlementCurrency()
  Description = tt2.QF.GetSymbolDescription()
  Precision = tt2.QF.GetSymbolPrecision()
  RoundLot = tt2.QF.GetSymbolRoundLot()
  MinTradeVolume = tt2.QF.GetSymbolMinTradeVolume()
  MaxTradeVolume = tt2.QF.GetSymbolMaxTradeVolume()
  TradeVolumeStep = tt2.QF.GetSymbolTradeVolumeStep()
  ProfitCalcMode = tt2.QF.GetSymbolProfitCalcMode()
  MarginCalcMode = tt2.QF.GetSymbolMarginCalcMode()
  MarginHedge = tt2.QF.GetSymbolMarginHedge()
  MarginFactor = tt2.QF.GetSymbolMarginFactor()
  MarginFactorFractional = tt2.QF.GetSymbolMarginFactorFractional()
  ContractMultiplier = tt2.QF.GetSymbolContractMultiplier()
  Color = tt2.QF.GetSymbolColor()
  CommissionType = tt2.QF.GetSymbolCommissionType()
  CommissionChargeType = tt2.QF.GetSymbolCommissionChargeType()
  CommissionChargeMethod = tt2.QF.GetSymbolCommissionChargeMethod()
  LimitsCommission = tt2.QF.GetSymbolLimitsCommission()
  Commission = tt2.QF.GetSymbolCommission()
  MinCommission = tt2.QF.GetSymbolMinCommission()
  MinCommissionCurrency = tt2.QF.GetSymbolMinCommissionCurrency()
  SwapType = tt2.QF.GetSymbolSwapType()
  TripleSwapDay = tt2.QF.GetSymbolTripleSwapDay()
  SwapSizeShort = tt2.QF.GetSymbolSwapSizeShort()
  SwapSizeLong = tt2.QF.GetSymbolSwapSizeLong()
  DefaultSlippage = tt2.QF.GetSymbolDefaultSlippage()
  IsTradeEnabled = tt2.QF.GetSymbolIsTradeEnabled()
  GroupSortOrder = tt2.QF.GetSymbolGroupSortOrder()
  SortOrder = tt2.QF.GetSymbolSortOrder()
  CurrencySortOrder = tt2.QF.GetSymbolCurrencySortOrder()
  SettlementCurrencySortOrder = tt2.QF.GetSymbolSettlementCurrencySortOrder()
  CurrencyPrecision = tt2.QF.GetSymbolCurrencyPrecision()
  SettlementCurrencyPrecision = tt2.QF.GetSymbolSettlementCurrencyPrecision()
  StatusGroupId = tt2.QF.GetSymbolStatusGroupId()
  SecurityName = tt2.QF.GetSymbolSecurityName()
  SecurityDescription = tt2.QF.GetSymbolSecurityDescription()
  StopOrderMarginReduction = tt2.QF.GetSymbolStopOrderMarginReduction()
  HiddenLimitOrderMarginReduction = tt2.QF.GetSymbolHiddenLimitOrderMarginReduction()
  data.table(Name, Currency, SettlementCurrency, Description, Precision, RoundLot, MinTradeVolume, MaxTradeVolume, TradeVolumeStep, ProfitCalcMode,
             MarginCalcMode, MarginHedge, MarginFactor, MarginFactorFractional, ContractMultiplier, Color, CommissionType, CommissionChargeType,
             CommissionChargeMethod, LimitsCommission, Commission, MinCommission, MinCommissionCurrency, SwapType, TripleSwapDay, SwapSizeShort,
             SwapSizeLong, DefaultSlippage, IsTradeEnabled, GroupSortOrder, SortOrder, CurrencySortOrder, SettlementCurrencySortOrder, CurrencyPrecision,
             SettlementCurrencyPrecision, StatusGroupId, SecurityName, SecurityDescription, StopOrderMarginReduction, HiddenLimitOrderMarginReduction)
}
#' Get Symbol field
tt2.QF.GetSymbolName<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolName')
}
#' Get Symbol field
tt2.QF.GetSymbolCurrency<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCurrency')
}
#' Get Symbol field
tt2.QF.GetSymbolSettlementCurrency<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSettlementCurrency')
}
#' Get Symbol field
tt2.QF.GetSymbolDescription<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolDescription')
}
#' Get Symbol field
tt2.QF.GetSymbolPrecision<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolPrecision')
}
#' Get Symbol field
tt2.QF.GetSymbolRoundLot<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolRoundLot')
}
#' Get Symbol field
tt2.QF.GetSymbolMinTradeVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMinTradeVolume')
}
#' Get Symbol field
tt2.QF.GetSymbolMaxTradeVolume<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMaxTradeVolume')
}
#' Get Symbol field
tt2.QF.GetSymbolTradeVolumeStep<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolTradeVolumeStep')
}
#' Get Symbol field
tt2.QF.GetSymbolProfitCalcMode<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolProfitCalcMode')
}
#' Get Symbol field
tt2.QF.GetSymbolMarginCalcMode<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMarginCalcMode')
}
#' Get Symbol field
tt2.QF.GetSymbolMarginHedge<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMarginHedge')
}
#' Get Symbol field
tt2.QF.GetSymbolMarginFactor<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMarginFactor')
}
#' Get Symbol field
tt2.QF.GetSymbolMarginFactorFractional<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMarginFactorFractional')
}
#' Get Symbol field
tt2.QF.GetSymbolContractMultiplier<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolContractMultiplier')
}
#' Get Symbol field
tt2.QF.GetSymbolColor<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolColor')
}
#' Get Symbol field
tt2.QF.GetSymbolCommissionType<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCommissionType')
}
#' Get Symbol field
tt2.QF.GetSymbolCommissionChargeType<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCommissionChargeType')
}
#' Get Symbol field
tt2.QF.GetSymbolCommissionChargeMethod<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCommissionChargeMethod')
}
#' Get Symbol field
tt2.QF.GetSymbolLimitsCommission<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolLimitsCommission')
}
#' Get Symbol field
tt2.QF.GetSymbolCommission<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCommission')
}
#' Get Symbol field
tt2.QF.GetSymbolMinCommission<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMinCommission')
}
#' Get Symbol field
tt2.QF.GetSymbolMinCommissionCurrency<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolMinCommissionCurrency')
}
#' Get Symbol field
tt2.QF.GetSymbolSwapType<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSwapType')
}
#' Get Symbol field
tt2.QF.GetSymbolTripleSwapDay<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolTripleSwapDay')
}
#' Get Symbol field
tt2.QF.GetSymbolSwapSizeShort<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSwapSizeShort')
}
#' Get Symbol field
tt2.QF.GetSymbolSwapSizeLong<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSwapSizeLong')
}
#' Get Symbol field
tt2.QF.GetSymbolDefaultSlippage<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolDefaultSlippage')
}
#' Get Symbol field
tt2.QF.GetSymbolIsTradeEnabled<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolIsTradeEnabled')
}
#' Get Symbol field
tt2.QF.GetSymbolGroupSortOrder<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolGroupSortOrder')
}
#' Get Symbol field
tt2.QF.GetSymbolSortOrder<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSortOrder')
}
#' Get Symbol field
tt2.QF.GetSymbolCurrencySortOrder<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCurrencySortOrder')
}
#' Get Symbol field
tt2.QF.GetSymbolSettlementCurrencySortOrder<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSettlementCurrencySortOrder')
}
#' Get Symbol field
tt2.QF.GetSymbolCurrencyPrecision<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolCurrencyPrecision')
}
#' Get Symbol field
tt2.QF.GetSymbolSettlementCurrencyPrecision<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSettlementCurrencyPrecision')
}
#' Get Symbol field
tt2.QF.GetSymbolStatusGroupId<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolStatusGroupId')
}
#' Get Symbol field
tt2.QF.GetSymbolSecurityName<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSecurityName')
}
#' Get Symbol field
tt2.QF.GetSymbolSecurityDescription<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolSecurityDescription')
}
#' Get Symbol field
tt2.QF.GetSymbolStopOrderMarginReduction<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolStopOrderMarginReduction')
}
#' Get Symbol field
tt2.QF.GetSymbolHiddenLimitOrderMarginReduction<-function(){
  rClr::clrCallStatic('FDK2toR.QuoteFeed', 'GetSymbolHiddenLimitOrderMarginReduction')
}
