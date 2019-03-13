using CommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UserCode;

namespace Auxiliaries
{
    public static class BacktestPriceSegmentation
    {
        public static List<TradeSignal> BacktestPriceSegmentProcessor(SignalBase signalBase, Dictionary<Selection, IEnumerable<Bar>> marketData,
                                                    Auxiliaries.ExecuteTradesParam tradeParams,
                                                    PriceConstants btBarSegment, Func<Dictionary<Selection, IEnumerable<Bar>>, List<TradeSignal>> detectSignals,
                                                    Dictionary<Selection, IEnumerable<Bar>> trigInstrData = null,
                                                    IEnumerable<Tick> queuedTicks = null)
        {
            var modMarketData = marketData.ToDictionary(k => k.Key, v => v.Value.Select(b => new Bar(b)).ToList().AsEnumerable());
            var origLast = marketData.Last().Value.Last();
            var modLast = modMarketData.Last().Value.Last();

            List<TradeSignal> trades = null;

            switch (btBarSegment)
            {
                case PriceConstants.OPEN:
                    modLast.HighAsk = origLast.OpenAsk;
                    modLast.HighBid = origLast.OpenBid;
                    modLast.LowAsk = origLast.OpenAsk;
                    modLast.LowBid = origLast.OpenBid;
                    modLast.CloseAsk = origLast.OpenAsk;
                    modLast.CloseBid = origLast.OpenBid;
                    modLast.HighAsk = origLast.OpenAsk;

                    trigInstrData = modMarketData;
                    trades = detectSignals(modMarketData);
                    break;

                case PriceConstants.HIGH:
                    modLast.LowAsk = origLast.HighAsk;
                    modLast.LowBid = origLast.HighBid;
                    modLast.CloseAsk = origLast.HighAsk;
                    modLast.CloseBid = origLast.HighBid;
                    trigInstrData = modMarketData;
                    trades = detectSignals(modMarketData);
                    break;

                case PriceConstants.LOW:
                    modLast.CloseAsk = origLast.LowAsk;
                    modLast.CloseBid = origLast.LowBid;
                    trigInstrData = modMarketData;
                    trades = detectSignals(modMarketData);
                    break;

                case PriceConstants.CLOSE:
                    trigInstrData = modMarketData;
                    trades = detectSignals(modMarketData);
                    break;

                case PriceConstants.OHLC:
                    signalBase.Alert($"origin openAsk: {origLast.OpenAsk}");
                    signalBase.Alert($"origin openBid: {origLast.OpenBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanOpen}");
                    signalBase.Alert($"origin highAsk: {origLast.HighAsk}");
                    signalBase.Alert($"origin highBid: {origLast.HighBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanHigh}");
                    signalBase.Alert($"origin lowAsk: {origLast.LowAsk}");
                    signalBase.Alert($"origin lowBid: {origLast.LowBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanLow}");
                    signalBase.Alert($"origin closeAsk: {origLast.CloseAsk}");
                    signalBase.Alert($"origin closeBid: {origLast.CloseBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanClose}");
                    modLast.HighAsk = origLast.OpenAsk;
                    modLast.HighBid = origLast.OpenBid;
                    modLast.LowAsk = origLast.OpenAsk;
                    modLast.LowBid = origLast.OpenBid;
                    modLast.CloseAsk = origLast.OpenAsk;
                    modLast.CloseBid = origLast.OpenBid;
                    trigInstrData = modMarketData;
                    signalBase.Alert($"mod openAsk: {modLast.OpenAsk}");
                    signalBase.Alert($"mod openBid: {modLast.OpenBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanOpen}");
                    signalBase.Alert($"mod highAsk: {modLast.HighAsk}");
                    signalBase.Alert($"mod highBid: {modLast.HighBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanHigh}");
                    signalBase.Alert($"mod lowAsk: {modLast.LowAsk}");
                    signalBase.Alert($"mod lowBid: {modLast.LowBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanLow}");
                    signalBase.Alert($"mod closeAsk: {modLast.CloseAsk}");
                    signalBase.Alert($"mod closeBid: {modLast.CloseBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanClose}");
                    var trades1 = detectSignals(modMarketData);
                    trades = trades1;

                    //signalBase.Alert($"origin openAsk: {origLast.OpenAsk}");
                    //signalBase.Alert($"origin openBid: {origLast.OpenBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanOpen}");
                    //signalBase.Alert($"origin highAsk: {origLast.HighAsk}");
                    //signalBase.Alert($"origin highBid: {origLast.HighBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanHigh}");
                    //signalBase.Alert($"origin lowAsk: {origLast.LowAsk}");
                    //signalBase.Alert($"origin lowBid: {origLast.LowBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanLow}");
                    //signalBase.Alert($"origin closeAsk: {origLast.CloseAsk}");
                    //signalBase.Alert($"origin closeBid: {origLast.CloseBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanClose}");
                    modLast.HighAsk = origLast.HighAsk;
                    modLast.HighBid = origLast.HighBid;
                    modLast.LowAsk = origLast.HighAsk;
                    modLast.LowBid = origLast.HighBid;
                    modLast.CloseAsk = origLast.HighAsk;
                    modLast.CloseBid = origLast.HighBid;
                    trigInstrData = modMarketData;
                    //signalBase.Alert($"mod openAsk: {modLast.OpenAsk}");
                    //signalBase.Alert($"mod openBid: {modLast.OpenBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanOpen}");
                    //signalBase.Alert($"mod highAsk: {modLast.HighAsk}");
                    //signalBase.Alert($"mod highBid: {modLast.HighBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanHigh}");
                    //signalBase.Alert($"mod lowAsk: {modLast.LowAsk}");
                    //signalBase.Alert($"mod lowBid: {modLast.LowBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanLow}");
                    //signalBase.Alert($"mod closeAsk: {modLast.CloseAsk}");
                    //signalBase.Alert($"mod closeBid: {modLast.CloseBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanClose}");
                    var trades2 = detectSignals(modMarketData);
                    trades.AddRange(trades2);

                    //signalBase.Alert($"origin openAsk: {origLast.OpenAsk}");
                    //signalBase.Alert($"origin openBid: {origLast.OpenBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanOpen}");
                    //signalBase.Alert($"origin highAsk: {origLast.HighAsk}");
                    //signalBase.Alert($"origin highBid: {origLast.HighBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanHigh}");
                    //signalBase.Alert($"origin lowAsk: {origLast.LowAsk}");
                    //signalBase.Alert($"origin lowBid: {origLast.LowBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanLow}");
                    //signalBase.Alert($"origin closeAsk: {origLast.CloseAsk}");
                    //signalBase.Alert($"origin closeBid: {origLast.CloseBid}");
                    //signalBase.Alert($"origin mean: {origLast.MeanClose}");
                    modLast.LowAsk = origLast.LowAsk;
                    modLast.LowBid = origLast.LowBid;
                    modLast.CloseAsk = origLast.LowAsk;
                    modLast.CloseBid = origLast.LowBid;
                    trigInstrData = modMarketData;
                    //signalBase.Alert($"mod openAsk: {modLast.OpenAsk}");
                    //signalBase.Alert($"mod openBid: {modLast.OpenBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanOpen}");
                    //signalBase.Alert($"mod highAsk: {modLast.HighAsk}");
                    //signalBase.Alert($"mod highBid: {modLast.HighBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanHigh}");
                    //signalBase.Alert($"mod lowAsk: {modLast.LowAsk}");
                    //signalBase.Alert($"mod lowBid: {modLast.LowBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanLow}");
                    //signalBase.Alert($"mod closeAsk: {modLast.CloseAsk}");
                    //signalBase.Alert($"mod closeBid: {modLast.CloseBid}");
                    //signalBase.Alert($"mod mean: {modLast.MeanClose}");
                    var trades3 = detectSignals(modMarketData);
                    trades.AddRange(trades3);

                    modLast.CloseAsk = origLast.CloseAsk;
                    modLast.CloseBid = origLast.CloseBid;
                    modMarketData = marketData;
                    trigInstrData = modMarketData;
                    signalBase.Alert($"origin openAsk: {origLast.OpenAsk}");
                    signalBase.Alert($"origin openBid: {origLast.OpenBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanOpen}");
                    signalBase.Alert($"origin highAsk: {origLast.HighAsk}");
                    signalBase.Alert($"origin highBid: {origLast.HighBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanHigh}");
                    signalBase.Alert($"origin lowAsk: {origLast.LowAsk}");
                    signalBase.Alert($"origin lowBid: {origLast.LowBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanLow}");
                    signalBase.Alert($"origin closeAsk: {origLast.CloseAsk}");
                    signalBase.Alert($"origin closeBid: {origLast.CloseBid}");
                    signalBase.Alert($"origin mean: {origLast.MeanClose}");

                    signalBase.Alert($"mod openAsk: {modLast.OpenAsk}");
                    signalBase.Alert($"mod openBid: {modLast.OpenBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanOpen}");
                    signalBase.Alert($"mod highAsk: {modLast.HighAsk}");
                    signalBase.Alert($"mod highBid: {modLast.HighBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanHigh}");
                    signalBase.Alert($"mod lowAsk: {modLast.LowAsk}");
                    signalBase.Alert($"mod lowBid: {modLast.LowBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanLow}");
                    signalBase.Alert($"mod closeAsk: {modLast.CloseAsk}");
                    signalBase.Alert($"mod closeBid: {modLast.CloseBid}");
                    signalBase.Alert($"mod mean: {modLast.MeanClose}");
                    var trades4 = detectSignals(modMarketData);

                    trades.AddRange(trades4);
                    break;

                case PriceConstants.OLHC:
                    modLast.HighAsk = origLast.OpenAsk;
                    modLast.HighBid = origLast.OpenBid;
                    modLast.LowAsk = origLast.OpenAsk;
                    modLast.LowBid = origLast.OpenBid;
                    modLast.CloseAsk = origLast.OpenAsk;
                    modLast.CloseBid = origLast.OpenBid;
                    trigInstrData = modMarketData;
                    trades1 = detectSignals(modMarketData);
                    trades = trades1;

                    modLast.HighAsk = origLast.LowAsk;
                    modLast.HighBid = origLast.LowBid;
                    modLast.CloseAsk = origLast.LowAsk;
                    modLast.CloseBid = origLast.LowBid;
                    trigInstrData = modMarketData;
                    trades3 = detectSignals(modMarketData);
                    trades.AddRange(trades3);

                    modLast.CloseAsk = origLast.HighAsk;
                    modLast.CloseBid = origLast.HighBid;
                    trigInstrData = modMarketData;
                    trades2 = detectSignals(modMarketData);
                    trades.AddRange(trades2);

                    trigInstrData = modMarketData;
                    trades4 = detectSignals(modMarketData);
                    trades.AddRange(trades4);
                    break;
            }

            return trades;
        }
    }
}
