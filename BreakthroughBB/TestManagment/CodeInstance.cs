using System;
using System.Collections.Generic;
using CommonObjects;
using UserCode;

namespace CodeInstance
{
	public class CodeInstance : SignalBase
	{
    	public int param_1 { get; set; }

		public CodeInstance()
		{
			Name = "CodeInstance";
		}

		protected override bool InternalInit(IEnumerable<Selection> selections)
		{
			Selections.AddRange(selections);
			
			// Your code initialization

			return true;
		}

		protected override void InternalStart(Selection instrument = null, IEnumerable<Tick> ticks = null)
		{
			// Your code run logic

			throw new NotImplementedException();
		}

		protected override List<CodeParameterBase> InternalGetParameters()
		{
			return new List<CodeParameterBase>();
		}

		protected override bool InternalSetParameters(List<CodeParameterBase> parameterBases)
		{
			return true;
		}

        protected override List<TradeSignal> BacktestSlotItem(IEnumerable<Selection> instruments, IEnumerable<object> parameters)
        {
            // Your backtest logic

            throw new NotImplementedException();
        }

        protected override OrderParams AnalyzePreTrade(OrderParams order)
        {
            // Your pre-trade analysis logic

            return order;
        }

        protected override void AnalyzePostTrade(Order order)
        {
            // Your post-trade analysis logic

        }

        protected override void ProcessTradeFailure(Order order, string error)
        {
            // Your order failure handler

        }
    }
}