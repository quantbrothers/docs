using System;
using System.Collections.Generic;
using System.Linq;
using QuickFix.Fields;
using QuickFix.FIX44;


namespace QuickFIXExchangeExample
{
    class Program
    {
        static void Main(string[] args)
        {
            TestFixApp testFixApp = new TestFixApp(args.FirstOrDefault());
            testFixApp.Start();

            Account account = new Account("54b97c9e-202b-11b2-be90-529049f1f1bb");
            ClOrdID clOrdId = new ClOrdID(Guid.NewGuid().ToString());
            Symbol symbol = new Symbol("BTC/USD");
            Side side = new Side(Side.BUY);

            #region SecurityListRequest
            SecurityListRequest securityListRequest = new SecurityListRequest(
                new SecurityReqID(Guid.NewGuid().ToString()),
                new SecurityListRequestType(SecurityListRequestType.SYMBOL)
            )
            {
                Symbol = symbol
            };
            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(securityListRequest);
            testFixApp.WaitResponces(1);
            #endregion SecurityListRequest

            #region MarketDataRequest
            MarketDataRequest marketDataRequest = new MarketDataRequest(
                new MDReqID(Guid.NewGuid().ToString()),
                new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT),
                new MarketDepth(5))
            {
                MDUpdateType = new MDUpdateType(MDUpdateType.FULL_REFRESH),
            };

            var bid = new MarketDataRequest.NoMDEntryTypesGroup()
            {
                MDEntryType = new MDEntryType(MDEntryType.BID)
            };
            var ask = new MarketDataRequest.NoMDEntryTypesGroup()
            {
                MDEntryType = new MDEntryType(MDEntryType.OFFER)
            };

            var symGroup = new MarketDataRequest.NoRelatedSymGroup
            {
                Symbol = symbol
            };

            marketDataRequest.AddGroup(bid);
            marketDataRequest.AddGroup(ask);
            marketDataRequest.AddGroup(symGroup);


            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(marketDataRequest);
            testFixApp.WaitResponces(1);
            #endregion MarketDataRequest

            #region NewOrderSingle
            NewOrderSingle newOrderSingle = new NewOrderSingle(
                clOrdId,
                symbol,
                side,
                new TransactTime(DateTime.Now),
                new OrdType(OrdType.LIMIT))
            {
                OrderQty = new OrderQty(0.1m),
                Price = new Price(1m),
                Account = account,
                AcctIDSource = new AcctIDSource(AcctIDSource.OTHER)
            };

            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(newOrderSingle);
            testFixApp.WaitResponces(2);
            #endregion NewOrderSingle

            #region OrderCancelReplaceRequest
            OrderCancelReplaceRequest orderCancelReplaceRequest = new OrderCancelReplaceRequest(
                new OrigClOrdID(clOrdId.ToString()),
                clOrdId = new ClOrdID(Guid.NewGuid().ToString()),
                symbol,
                side,
                new TransactTime(DateTime.Now),
                new OrdType(OrdType.LIMIT))
            {
                Price = new Price(2m),
                OrderQty = new OrderQty(0.2m)
            };

            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(orderCancelReplaceRequest);
            testFixApp.WaitResponces(2);
            #endregion OrderCancelReplaceRequest

            #region OrderStatusRequest
            OrderStatusRequest orderStatusRequest = new OrderStatusRequest(
                clOrdId,
                symbol,
                side
                );
            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(orderStatusRequest);
            testFixApp.WaitResponces(1);
            #endregion OrderStatusRequest

            #region OrderCancelRequest
            OrderCancelRequest orderCancelRequest = new OrderCancelRequest(
                    new OrigClOrdID(clOrdId.ToString()),
                    new ClOrdID(Guid.NewGuid().ToString()),
                    symbol,
                    side,
                    new TransactTime(DateTime.Now)
                )
            { OrderQty = new OrderQty(0.1m) };

            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(orderCancelRequest);
            testFixApp.WaitResponces(3);
            #endregion region OrderCancelRequest

            #region OrderMassStatusRequest
            OrderMassStatusRequest orderMassStatusRequest = new OrderMassStatusRequest(
                new MassStatusReqID(Guid.NewGuid().ToString()),
                new MassStatusReqType(MassStatusReqType.STATUS_FOR_ALL_ORDERS))
            {
                Side = side,
                Symbol = symbol
            };
            Console.WriteLine("Press enter to next comand");
            Console.ReadLine();
            testFixApp.Send(orderMassStatusRequest);
            #endregion OrderMassStatusRequest

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            testFixApp.Stop();
        }
    }
}
