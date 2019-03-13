using CommonObjects;
using UserCode;
using System;
using System.Collections.Generic;

namespace Auxiliaries
{
    static class ExecuteTrades
    {
        public static void Execute(List<TradeSignal> signals, SignalBase signalBase, ExecuteTradesParam execTradeParam)
        {
            decimal actualPrice = 0;
            decimal priceOffset = 0;
            var tradeSignals = signals;
            var stopLoss = execTradeParam.SL;
            var takeProfit = execTradeParam.TP;

            foreach (var _account in signalBase.BrokerAccounts)
            {
                //check if the account can trade this symbol...
                if (execTradeParam.DataFeed.AvailableDataFeeds.Contains(_account.DataFeedName))
                {
                    foreach (var item in tradeSignals)
                    {   // trade only if the symbol has correct trade slot and it was not the first iteration (first iteration can)
                        if (execTradeParam.TradeableSymbols.Contains(item.Instrument.Symbol) && execTradeParam.EvalCount > 1)
                        {
                            if (execTradeParam.OrderType == TradeType.Limit || execTradeParam.OrderType == TradeType.Stop)
                            {
                                Tick tick = null;
                                try
                                {
                                    tick = execTradeParam.DataFeed.GetLastTick(_account.DataFeedName, item.Instrument.Symbol);
                                }
                                catch (Exception e)
                                {
                                    signalBase.Alert($"Failed to retrieve tick for {item.Instrument}: {e.Message}");
                                }

                                if (tick != null)
                                {
                                    if (item.Side == Side.Buy && tick.Ask > 0)
                                    {
                                        actualPrice = tick.Ask;
                                        if (execTradeParam.OrderType == TradeType.Limit)
                                            priceOffset = execTradeParam.BuyPriceOffset / -100000;
                                        if (execTradeParam.OrderType == TradeType.Stop)
                                            priceOffset = execTradeParam.BuyPriceOffset /  100000;
                                    }

                                    else if (item.Side == Side.Sell && tick.Bid > 0)
                                    {
                                        actualPrice = tick.Bid;
                                        if (execTradeParam.OrderType == TradeType.Limit)
                                            priceOffset = execTradeParam.SellPriceOffset / 100000;
                                        if (execTradeParam.OrderType == TradeType.Stop)
                                            priceOffset = execTradeParam.SellPriceOffset / -100000;
                                    }
                                }
                            }

                            if (execTradeParam.HideSL == true)
                                stopLoss = null;
                            if (execTradeParam.HideTP == true)
                                takeProfit = null;
                                                                               
                            signalBase.PlaceOrder(new OrderParams(DateTime.UtcNow.Ticks.ToString(), item.Instrument.Symbol)
                            {
                                TimeInForce = execTradeParam.TIF,
                                SLOffset = stopLoss,
                                TPOffset = takeProfit,
                                Quantity = execTradeParam.OrderQuantity,
                                OrderSide = item.Side,
                                OrderType = execTradeParam.OrderType,
                                DataServerSide = execTradeParam.HideOrder,
                                Price = actualPrice + priceOffset,
                            }, _account);
                            signalBase.Alert($"Trade: {execTradeParam.OrderType}, {item.Instrument.Symbol}, {item.Side}, {execTradeParam.OrderQuantity}, {execTradeParam.TP}, {execTradeParam.SL}");                      
                        }
                    }
                }
            }
        }
    }
}
