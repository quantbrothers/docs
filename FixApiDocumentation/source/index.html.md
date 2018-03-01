---
title: API Reference

language_tabs: # must be one of https://git.io/vQNgJ


toc_footers:
  - <a href='http://www.QuantBrothers.com'>QuantBrothers</a>
  - <a href='https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample'>Fix Example</a>
  

includes:

search: false
---

# FIX API
message which represents 


## Workflow

After connecting and logging on, the client can either request a security list or subscribe to market data.


## Custom fields

Tag|Name|Type
---|----|----
20101|QReqID|String
20102|Amount|Qty
20103|Address|String
20104|PaymentID|String
20105|TransactionCompleted|Boolean
20106|TransactionID|String
20107|VenueTransactionID|String
20108|TransactionFee|Qty
20109|TransactionsListType|Int


**Security list**

Optional: Server offers a security list that supplies the definitive list of symbols traded on the exchange.

1. Client sends a `Security List Request <x>`
2. Server responds with a `Security List <y>`


**Market data subscription**

After connecting to the the market data channel, the client subscribes to market data by sending a `Market Data Request <V>` message.

When a client disconnects for any reason, the market data subscription is terminated. Upon reconnecting, the client needs to send another `Market Data Request <V>` message.

1. Client sends a `Market Data Request <V>`
2. Server responds by sending one Market Data - `Snapshot/Full Refresh <W>`
3. As bids, offers, trades and/or actions happen on the exchange, Server sends Market Data - `Incremental Refresh <X>` messages


## Logon < A> message

The `Logon <A>` message must be the first message sent by the application requesting to initiate a FIX session. The `Logon <A>` message authenticates an institution establishing a connection to Server.

Tag|Name|Req|Description
---|----|---|-----------
98|EncryptMethod|Y|Server does not support encryption. Valid values: 0 = None.
108|HeartBtInt|Y|`Heartbeat <0>` message interval (seconds).
141|ResetSeqNumFlag|N|Indicates that the both sides of the FIX session should reset sequence numbers. Valid values: Y = Yes, reset sequence numbers N = No.


## Logout < 5> message

The `Logout <5>` message initiates or confirms the termination of a FIX session. Disconnection without the exchange of `Logout <5>` messages should be interpreted as an abnormal condition.

**Workflow**
After connecting, logging on, and synchronizing sequence numbers, the client can submit orders. All identifiers must be unique like GUID or UUID.

## New Order Single < D> message

To submit a new order to server, send a `New Order Single <D>` message. Server will respond to a `New Order Single <D>` message with an `Execution Report <8>`.

>For lib Quickfix
>C#

```csharp
 NewOrderSingle newOrderSingle = new NewOrderSingle(
                new ClOrdId("00000000-0000-0000-0000-000000000001"),
                new Symbol("BTC/USD"),
                new Side(Side.BUY),
                new TransactTime(DateTime.Now),
                new OrdType(OrdType.LIMIT))
            {
                OrderQty = new OrderQty(0.1m),
                Price = new Price(1m),
                Account = account,
                AcctIDSource = new AcctIDSource(AcctIDSource.OTHER)
            };

testFixApp.Send(newOrderSingle);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)


Tag|Name|Req|Description
---|----|---|-----------
11 | ClOrdID | Y | Unique identifier of the order as assigned by the client. Must be unique like GUID or UUID.
1 | Account | N | Market account id
660 | AcctIDSource | Y * (required if use `Account <1>`) | Type of account. Should be 99 = Other (custom or proprietary) This field is required if 1 = Account contains account identifier.
40 | OrdType| Y | Order type. Valid values:   1 = Market   2 = Limit
44 | Price | Y * (required if `OrdType <40>` = 2  Limit) | Price per unit of quantity
38 | OrderQty | Y | Quantity ordered. Needed > 0
54 | Side |Y | Side of the order. Valid values:   1 = Buy    2 = Sell
55 | Symbol | Y | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
59 | TimeInForce | N | Specifies how long the order remains in effect. Valid values: 1 = Good Till Cancel (GTC) 3 = Immediate or Cancel (IOC) 4 = Fill or Kill (FOK)
60 | TransactTime | Y | Time of request creation


**Submitting an order**

1. Client sends server → `New Order Single <D>` message
2. Does server accept the order?
  * Yes, order is accepted for initial processing
    * server sends client ← an `Execution Report <8>` for a new order with:
      * `ExecType <150>` set to I = Order Status
      * `OrdStatus <39>` set to A = Pending New
    * Is the order marketable?
      * Yes, server executes one or more initial fills
        1. server sends client ← an `Execution Report <8>` for each fill or partial fill
        2. Does the order have remaining quantity?.
            * Yes, server puts the remaining quantity on the book
            * No, server closes the order
      * No, server puts the entire quantity of the order on the book
  * No, order is rejected
    * server sends client ← an `Execution Report <8>` indicating the order was rejected with:
      * `ExecType <150>` set to 8 = Rejected
      * `OrdStatus <39>` set to 8 = Rejected


## Execution Report < 8> message

Server uses the `Execution Report <8>` message to:
*confirm the receipt of an order
*confirm the successful cancellation of an order
*relay fill information on orders
*reject orders
  
Tag|Name|Req|Description
---|----|---|-----------
37 | OrderID | Y |Unique identifier of the order as assigned by the server. Possible "NONE" and `ExecType <150>` = 8 Rejected when order not found
11 | ClOrdID | Y * (Required for existing order) | Current unique identifier of the order as assigned by the client. 
41 | OrigClOrdID | N * (Required if client change ClOrdID in Order Cancel/Replace `Request <G>` message or `Order Cancel Request <F>` message) | Previous unique identifier of the order as assigned by the client if current ClOrdID it was changed
17 | ExecID | Y | Unique identifier of execution message as assigned by server.
150 | ExecType | Y |Describes the purpose of the Execution Report <br>Possible values: <br>	I = Order Status<br>	6 = Pending Cancel<br>	E = Pending <br>Replace<br>	F = Trade<br>	8 = Rejected
39 | OrdStatus | Y |Describes the current order status <br>Possible values:<br>   A = Pending New<br>   0 = New<br>   1 = Partially filled<br>   2 = Filled<br>   4 = Canceled<br>   8 = Rejected
58 | Text | N |Error description if contains
54 | Side | Y | Side of the order.<br>Possible values:<br>   1 = Buy<br>   2 = Sell<br>   B = "As Defined" (when `ExecType <150>` = 8 Rejected and/or order not found)
55 | Symbol | Y | Ticker symbol. | Possible "-" when `ExecType <150>` = 8 Rejected and order not found
14 | CumQty | Y | Total quantity of the  order that is filled. Zero (0) when `ExecType <150>` = 8 Rejected and order not found
151 | LeavesQty | Y | Quantity open for further execution. Zero (0) when `ExecType <150>` = 8 Rejected and order not found
31 | LastPx | Y * (Required if `ExecType <150>` = F  Trade) | Price of this fill
32 | LastQty | Y * (Required if `ExecType <150>` = F  Trade) | Quantity of this fill
6 | AvgPx | Y | Calculated average price of all fills on this order
790 | OrdStatusReqID | Y * (Required if responding to and if provided on the `Order Status Request <H>` message. Echo back the value provided by the requester.) | Uniquely identify a specific `Order Status Request <H>` message.
584 | MassStatusReqID | Y * (Required if responding to a Order `Mass Status Request <AF>`. Echo back the value provided by the requester.) | Uniquely identify a specific `Order Mass Status Request <AF>` message.
911 | TotNumReports | Y * (Required if responding to a Order `Mass Status Request <AF>`. Echo back the value provided by the requester.) | Total number of reports returned in response to a request
912 | LastRptRequested | Y * (Required if responding to a Order `Mass Status Request <AF>`. Echo back the value provided by the requester.) |Indicates whether this message is that last report message in response to a `Order Mass Status Request <AF>`.<br>Y = Last message<br>N = Not last message 


## Order Cancel/Replace Request < G> message

The order cancel/replace request is used to change the parameters of an existing order. Do not use this message to cancel the remaining quantity of an outstanding order, use the `Order Cancel Request <F>` message for this purpose.

>For lib Quickfix
>C#

```csharp
OrderCancelReplaceRequest orderCancelReplaceRequest = new OrderCancelReplaceRequest(
                new OrigClOrdID(clOrdId.ToString()),
                clOrdId = new ClOrdID(Guid.NewGuid().ToString()),
                new Symbol("BTC/USD"),
                new Side(Side.BUY),
                new TransactTime(DateTime.Now),
                new OrdType(OrdType.LIMIT))
            {
                Price = new Price(2m),
                OrderQty = new OrderQty(0.2m)
            };
            
testFixApp.Send(orderCancelReplaceRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. (new if needed, otherwise it can be equal OrigClOrdID)
41 | OrigClOrdID | Y | Current unique identifier of the order as assigned by the client. 
40 | OrdType | Y | Order type. Valid values:<br>   2 = Limit <br>Must be equal of an existing order
44 | Price | Y | Price per unit of quantity <br>New value of Price and/or OrderQty
38 | OrderQty | Y | Quantity ordered. Needed > 0 <br>New value of Price and/or OrderQty
54 | Side | Y | Side of the order.<br>Valid values:<br>   1 = Buy<br>   2 = Sell <br>Must be equal of an existing order
55 | Symbol | Y | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC<br>Must be equal of an existing order
59 | TimeInForce | N | Specifies how long the order remains in effect. <br>Valid values:<br>   1 = Good Till Cancel (GTC)<br>   3 = Immediate or Cancel (IOC)<br>   4 = Fill or Kill (FOK)<br>Must be equal of an existing order
60 | TransactTime | Y | Time of request creation


**Submitting an order changes**

3. Client sends server → `Order Cancel/Replace Request <G>` message
4. Does server accept the changes?
	* Yes, changes is accepted for initial processing
		* server sends client ← an `Execution Report <8>` for a change order with:
			* `ExecType <150>` set to E = Pending Replace
			* Current status of order
		* Is the order marketable?
			* Yes, server executes one or more initial fills
				1. server sends client ← an `Execution Report <8>` for each fill or partial fill
				2. Does the order have remaining quantity?
					* Yes, server puts the remaining quantity on the book
					* No, server closes the order
			* No, server puts the entire quantity of the order on the book
	* No, changes is rejected
		* server sends client ← an `Order Cancel Reject <9>` message indicating the changes was rejected with:
			* `CxlRejResponseTo <434>` set to 2 = `Order Cancel/Replace Request <G>`
			* `OrdStatus <39>` set to current order status. If `CxlRejReason <102>` = 'Unknown Order', specify 8 = Rejected.
			* If order not found, `CxlRejReason <102>` set to 1 = Unknown order.


## Order Cancel Request < F> message

The `Order Cancel Request <F>` message requests the cancellation of all of the remaining quantity of an existing order.

>For lib Quickfix
>C#

```csharp
OrderCancelRequest orderCancelRequest = new OrderCancelRequest(
                    new OrigClOrdID(clOrdId.ToString()),
                    new ClOrdID(Guid.NewGuid().ToString()),
                    new Symbol("BTC/USD"),
                    new Side(Side.BUY),
                    new TransactTime(DateTime.Now)
                )
            { OrderQty = new OrderQty(0.1m) };

testFixApp.Send(orderCancelRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. (new if needed, otherwise it can be equal OrigClOrdID)
41 | OrigClOrdID | Y | Current unique identifier of the order as assigned by the client. 
38 | OrderQty | Y | Quantity ordered. Needed > 0, but there is nothing to do. Will be try cancellation of all of the remaining quantity.
54 | Side | Y | Side of the order.<br>Valid values:<br>   1 = Buy<br>   2 = Sell <br>Must be equal of an existing order
55 | Symbol | Y | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC<br>Must be equal of an existing order
60 | TransactTime | Y | Time of request creation


**Submitting an order cancel**

5. Client sends server → `Order Cancel Request <F>` message
6. Does server accept cancel order request?
	* Yes, request is accepted for initial processing
		* server sends client ← an `Execution Report <8>` for a cancellation order with:
			* `ExecType <150>` set to 6 = Pending Cancel
			* Current status of order
		* Successful cancellation of an order? server sends client ← an `Execution Report <8>` for order with:
			* `ExecType <150>` set to 4 = Canceled
	 		* Current status of order
	* No, request is rejected
		* server sends client ← an `Order Cancel Reject <9>` message indicating the cancellation was rejected with:
			* `CxlRejResponseTo <434>` set to 1 = `Order Cancel Request <F>`
			* `OrdStatus <39>` set to current order status. If `CxlRejReason <102>` = 'Unknown Order', specify 8 = Rejected.
			* If order not found, `CxlRejReason <102>` set to 1 = Unknown order.


## Order Status Request < H> message

The `Order Status Request <H>` message is used by the client to generate an order status message (`Execution Report <8>` message) back from the server.

>For lib Quickfix
>C#

```csharp
OrderStatusRequest orderStatusRequest = new OrderStatusRequest(
                clOrdId,
                new Symbol("BTC/USD"),
                new Side(Side.BUY),
                );

testFixApp.Send(orderStatusRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. (new if needed, otherwise it can be equal OrigClOrdID)
790 | OrdStatusReqID | N | Can be used to uniquely identify a specific `Order Status Request <H>` and response message.

Response on `Order Status Request <H>` is an `Execution Report <8>` message with current order status. `ExecType <150>` in a response may have two values: 8 = Rejected (if order not found) and I = Order Status. `Execution Report <8>` response contain `OrdStatusReqID <790>` if request contain it.


## Order Mass Status Request < AF> message

The `Order Mass Status Request <AF>` message requests the status for orders matching criteria specified within the request.

>For lib Quickfix
>C#

```csharp
OrderMassStatusRequest orderMassStatusRequest = new OrderMassStatusRequest(
                new MassStatusReqID(Guid.NewGuid().ToString()),
                new MassStatusReqType(MassStatusReqType.STATUS_FOR_ALL_ORDERS))
            {
                Side = side,
                Symbol = symbol
            };
            
testFixApp.Send(orderMassStatusRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
584 | MassStatusReqID | Y | Value assigned by issuer of Mass Status Request to uniquely identify the request and responses.
585 | MassStatusReqType | Y | Mass Status Request Type <br>Valid values:<br>   1 = Status for orders for a security<br>   7 = Status for all orders
1 | Account | N | Filtering orders by market account id.
660 | AcctIDSource | Y * (required if use `Account <1>`) | Type of account.<br> Valid value:<br>  99 = Other (custom or proprietary)
54 | Side | Y | Side of the order.<br>Valid values:<br>   1 = Buy<br>   2 = Sell 
55 | Symbol | Y | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC

Responses on `Order Mass Status Request <AF>` message is an `Execution Reports <8>` messages with current order status. `ExecType <150>` in a response may have two values: 8 = Rejected (if matching criteria is incorrect) and I = Order Status. <br>If `ExecType <150>` is 8 = Rejected, then <br>	`OrderID <37>` = "-" and `OrdStatus <39>` = 8 (Rejected)<br>	If `Symbol <55>` not set in request then `Symbol <55>` in response = "-"<br>	If `Side <54>` not set in request then `Side <54>` in response = B ("As Defined")<br>Execution `Reports <8>` contains `MassStatusReqID <584>`, `TotNumReports <911>` and `LastRptRequested <912>`.


## Security List Request < x> message

The `Security List Request <x>` message is used to return a list of securities from the server that match criteria provided on the request.

>For lib Quickfix
>C#

```csharp
SecurityListRequest securityListRequest = new SecurityListRequest(
                new SecurityReqID(Guid.NewGuid().ToString()),
                new SecurityListRequestType(SecurityListRequestType.SYMBOL)
            )
            {
                Symbol = new Symbol("BTC/USD")
            };
            
testFixApp.Send(securityListRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
320 | SecurityReqID | Y | Unique ID of a Security Definition Request
559 | SecurityListRequestType | Y | Identifies the type/criteria of Security List Request <br>Valid values:<br>   0 = `Symbol <55>`<br>   4 = All Securities
55 | Symbol | Y * (required if use `SecurityListRequestType <559>` = 0 (`Symbol <55>`)) | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
207 | SecurityExchange | N | Filtering security by  market name. Like “Kraken"
15 | Currency | N | Filtering security by settlement currency. Like “BTC” for “ETH/BTC”

Response on `Security List Request <x>` message is `Security List <y>` message.


## Security List < y> message

The `Security List <y>` message is used to return a list of securities that matches the criteria specified in a `Security List Request <x>`.

Tag|Name|Req|Description
---|----|---|-----------
320 | SecurityReqID | Y | Unique ID of a Security Definition Request
322 | SecurityResponseID | Y | Unique ID of a Security (if `SecurityRequestResult <560>` = 0 (Valid request))
560 | SecurityRequestResult | Y | The results returned to a Security Request message.<br>Possible values:<br>   0 = Valid request<br>   1 = Invalid or unsupported request<br>   2 = No instruments found that match selection criteria
146 | NoRelatedSym | Y | Specifies the number of repeating symbols specified.
=>55 | Symbol | Y * (required if `SecurityRequestResult <560>` = 0 (Valid request)) | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
=>207 | SecurityExchange | Y * (required if `SecurityRequestResult <560>` = 0 (Valid request)) | Market name.
=>15 | Currency | Y * (required if `SecurityRequestResult <560>` = 0 (Valid request)) | Identifies currency used for price.
=>561 | RoundLot | Y * (required if `SecurityRequestResult <560>` = 0 (Valid request)) | The trading lot size of a security
=>562 | MinTradeVol | N * (if `SecurityRequestResult <560>` = 0 (Valid request) and the market provides this value, then it is) | The minimum trading volume for a security
393 | TotNoRelatedSym | Y | Total number of securities returned in response to a request
893 | LastFragment | Y | Indicates whether this message is that last report message in response to a `Security List Request <x>`.<br>Y = Last message<br>N = Not last message


## Market Data Request < V> message

Subscribes the current session to a Market Data - `Snapshot/Full Refresh <W>` followed by zero or more Market Data - `Incremental Refresh <X>` messages.

>For lib Quickfix
>C#

```csharp
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
                Symbol = new Symbol("BTC/USD")
            };

marketDataRequest.AddGroup(bid);
marketDataRequest.AddGroup(ask);
marketDataRequest.AddGroup(symGroup);

testFixApp.Send(marketDataRequest);
```
>[*Full example*](https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample)

Tag|Name|Req|Description
---|----|---|-----------
262 | MDReqID | Y | Unique identifier for `Market Data Request <V>`
263 | SubscriptionRequestType | Y | Subscription Request Type<br>Valid values:<br>   0 = Snapshot<br>   1 = Snapshot + Updates (Subscribe)<br>   2 = Disable previous Snapshot + Update Request (Unsubscribe)
264 | MarketDepth | Y | Depth of market for Book <br>Valid values:<br>   0 = Full Book<br>   1 = Top of Book<br>   N> = Report best N price tiers of data
265 | MDUpdateType | Y * (required if `SubscriptionRequestType <263>` = 1 ( Snapshot + Updates (Subscribe))) | Specifies the type of Market Data update.<br>Valid values:<br>   0 = Full Refresh<br>   1 = Incremental Refresh
267 | NoMDEntryTypes | Y | Number of `MDEntryType <269>`  fields requested.
=>269 | MDEntryType | Y * (required if `NoMDEntryTypes <267>` > 0) | Type Market Data entry.<br>Valid values:<br>   0 = Bid<br>   1 = Offer<br>   2 = Trade
146 | NoRelatedSym | Y | Specifies the number of repeating symbols specified.
=>55 | Symbol| Y * (required if `NoRelatedSym <146>` > 0) | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
=>207 | SecurityExchange | N | Name of the exchange. e.g. “Kraken” Subscribed to all exchanges if not specified.

If a subscription error occurs, server will return `Market Data Request Reject <Y>` message with `MDReqID <262>` refer to the `MDReqID <262>` of the request. 

`MDReqRejReason <281>` (Reason for the rejection of a Market Data request) in `Market Data Request Reject <Y>` message may be not set or:
Possible values:
	0 = Unknown symbol
	1 = Duplicate `MDReqID <262>`
	4 = Unsupported `SubscriptionRequestType <263>`
	5 = Unsupported `MarketDepth <264>`
	6 = Unsupported `MDUpdateType <265>`
	8 = Unsupported `MDEntryType <269>`
There may be an error description in the field `Text <58>`.


## Market Data - Snapshot/Full Refresh < W> message

The Market Data messages are used as the response to a `Market Data Request <V>` message

Tag|Name|Req|Description
---|----|---|-----------
262 | MDReqID | Y | Unique identifier for `Market Data Request <V>`
55 | Symbol | Y | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
207 | SecurityExchange | Y | Market name.
268 | NoMDEntries | Y | Number of entries in Market Data message.
=>269 | MDEntryType | Y * (required if `NoMDEntries <268>` > 0) | Type Market Data entry.<br>Possible values:<br>   0 = Bid<br>   1 = Offer<br>   2 = Trade
=>270 | MDEntryPx | Y * (required if `NoMDEntries <268>` > 0) | `Price <44>` of the Market Data Entry.
=>271 | MDEntrySize | Y * (required if `NoMDEntries <268>` > 0) | `Quantity <53>` or volume represented by the Market Data Entry.


## Market Data - Incremental Refresh < X> message

The Market Data messages are used as the response to a `Market Data Request <V>` message after Market Data - `Snapshot/Full Refresh <W>` message when `SubscriptionRequestType <263>` = 1 ( Snapshot + Updates (Subscribe)) and `MDUpdateType <265>` = 1 (Incremental Refresh)

Tag|Name|Req|Description
---|----|---|-----------
262 | MDReqID | Y | Unique identifier for Market Data `Request <V>`
268 | NoMDEntries | Y | Number of entries in Market Data message.
=>269 | MDEntryType | Y * (required if `NoMDEntries <268>` > 0) | Type Market Data entry.<br>Possible values:<br>   0 = Bid<br>   1 = Offer<br>   2 = Trade
=>270 | MDEntryPx | Y * (required if `NoMDEntries <268>` > 0) | `Price <44>` of the Market Data Entry.
=>271 | MDEntrySize | Y * (required if `NoMDEntries <268>` > 0) | `Quantity <53>` or volume represented by the Market Data Entry.
=>279 | MDUpdateAction | Y * (required if `NoMDEntries <268>` > 0) | Type of Market Data update action.<br>Possible values:<br>   0 = New<br>   1 = Change<br>   2 = Delete
=>55 | Symbol | Y * (required if `NoMDEntries <268>` > 0) | Ticker symbol. Common, "human understood" representation of the security. e.g. ETH/BTC
=>207 | SecurityExchange | Y * (required if `NoMDEntries <268>` > 0) | Market name.


## Balances Request < bQ> message (custom message)

Subscribes the current session to a Balance Data.

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier for `Balances Request <bQ>`
263|SubscriptionRequestType|Y|Subscription Request Type<br><br>Valid values:<br>0 = Snapshot<br>1 = Snapshot + Updates (Subscribe)<br>2 = Disable previous Snapshot + Update Request (Unsubscribe)
1|Account|N|Account identifier. If not specified all active balances from all accounts will send.
15|Currency|N|Currency name. If not specified all active balances from currencies will send.Can contain multiple currencies separated by a comma, like “BTC,USD,EUR”


## Balances Refresh < bU> message (custom message)

The Balances Refresh messages are used as the response to a `Balances Request <bQ>` message.

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier of `Balances Request <bQ>`
20102|Amount|Y|Amount of currency on exchange.
1|Account|Y|Account identifier.
15|Currency|Y|Currency name.


## Balances Reject < bF> message (custom message)

If a subscription error occurs, server will return `Balances Reject <bF>`

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier of `Balances Request <bQ>`
58|Text|N|Message to explain reason for rejection.


## Deposit Address Request < wQ> message (custom message)

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier for `Deposit Address Request <wQ>`
1|Account|Y|Account identifier.
15|Currency|Y|Currency name.


## Deposit Address < wA> message (custom message)
The Deposit Address messages are used as the response to a `Deposit Address Request <wQ>` message.

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier for `Deposit Address Request <wQ>`
20103|Address|Y|Address of cryptocurrency wallet.
20104|PaymentID|N|Payment identifier if applicable for cryptocurrency.


## Withdraw Request < wW> message (custom message)

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier of `Withdraw Request <wW>`
20102|Amount|Y|Amount of currency to withdraw.
1|Account|Y|Account identifier.
15|Currency|Y|Currency name. 
20103|Address|Y|Address of cryptocurrency wallet.
20104|PaymentID|N|Payment identifier if applicable for cryptocurrency.


## Withdraw Ack < wR> message (custom message)

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier of `Withdraw Request <wW>`


## Transactions List Request < wLD> message (custom message)

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier for `Transactions List Request <wLD>`
1|Account|Y|Account identifier.
15|Currency|Y|Currency name.
20109|TransactionsListType|Y|Type of transactions list.<br><br>Valid values:<br>1 = Withdraw<br>2 = Deposit
916|StartDate|N|Begin date and time of transaction in the list.<br><br>Format:<br>01/26/2018 14:27:45
917|EndDate|N|End date and time of transaction in the list.<br><br>Format:<br>01/26/2018 14:27:45


## Transactions List < wld> message (custom message)

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier for `Transactions List Request <wLD>`
20109|TransactionsListType|Y|Type of transactions list.<br><br>Valid values:<br>1 = Withdraw<br>2 = Deposit
893|LastFragment|Y|Indicates if this message in a fragmented response
20103|Address|Y|Address of cryptocurrency wallet.
20104|PaymentID|N|Payment identifier if applicable for cryptocurrency.
20102|Amount|Y|Amount of currency.
20105|TransactionCompleted|Y|Does transaction completed or not.
20106|TransactionID|Y|Transaction identifier in the blockchain.
20107|VenueTransactionID|N|Internal identifier of the transaction provided by exchange.
20108|TransactionFee|N|Fee of transaction in native blockchain currency.
504|PaymentDate|Y|Date when transaction issued.
58|Text|N|Transaction description, if provided by exchange.


## Withdraw Reject < wF> message (custom message)

Possible reject message for `Deposit Address Request <wQ>`, `Withdraw Request <wW>` and `Transactions List Request <wLD>`.

Tag|Name|Req|Description
---|----|---|-----------
20101|QReqID|Y|Unique identifier of rejected request.
58|Text|N|Where possible, message to explain reason for rejection.


## Reject < 3> message

**Description**

The `Reject <3>` message should be issued when a message is received but cannot be properly processed due to a session-level rule violation. An example of when a reject may be appropriate would be the receipt of a message with invalid basic data (e.g. `MsgType <35>`=&) which successfully passes de-encryption, `CheckSum <10>` and `BodyLength <9>` checks. As a rule, messages should be forwarded to the trading application for business level rejections whenever possible.
<br><br>Rejected messages should be logged and the incoming sequence number incremented.
<br><br>Note: The receiving application should disregard any message that is garbled, cannot be parsed or fails a data integrity check. Processing of the next valid FIX message will cause detection of a sequence gap and a `Resend Request <2>` will be generated. Logic should be included in the FIX engine to recognize the possible infinite resend loop, which may be encountered in this situation.
<br><br>Generation and receipt of a `Reject <3>` message indicates a serious error that may be the result of faulty logic in either the sending or receiving application.
<br><br>If the sending application chooses to retransmit the rejected message, it should be assigned a new sequence number and sent with `PossResend <97>`=Y.
<br><br>Whenever possible, it is strongly recommended that the cause of the failure be described in the `Text <58>` field (e.g. INVALID DATA - FIELD 35).
<br><br>If an application-level message received fulfills session-level rules, it should then be processed at a business message-level. If this processing detects a rule violation, a business-level reject should be issued. Many business-level messages have specific "reject" messages, which should be used. All others can be rejected at a business-level via the `Business Message Reject <j>` message. See Volume 1: "`Business Message Reject <j>`" message.
<br><br>Note that in the event a business message is received, fulfills session-level rules, however, the message cannot be communicated to the business-level processing system, a `Business Message Reject <j>` with `BusinessRejectReason <380>` = "Application not available at this time" should be issued.

**Scenarios for session-level `Reject <3>`:**
<br>`SessionRejectReason <373>`
<br>0 Invalid tag number
<br>1 Required tag missing
<br>2 Tag not defined for this message type
<br>3 Undefined Tag
<br>4 Tag specified without a value
<br>5 Value is incorrect (out of range) for this tag
<br>6 Incorrect data format for value
<br>7 Decryption problem
<br>8 `Signature <89>` problem
<br>9 CompID problem
<br>10 `SendingTime <52>` accuracy problem
<br>11 Invalid `MsgType <35>`
<br>12 XML Validation error
<br>13 Tag appears more than once
<br>14 Tag specified out of required order
<br>15 Repeating group fields out of order
<br>16 Incorrect NumInGroup count for repeating group
<br>17 Non "data" value includes field delimiter (SOH character)
<br>99 Other
<br><br>*(Note other session-level rule violations may exist in which case `SessionRejectReason <373>` of Other may be used and further information may be in `Text <58>` field.)*

# RESTFul API Public Endpoints

message which represents 

## General
Requests parameters for POST requests (authenticated) in the "Authenticated Enpoints" section are part of the PAYLOAD, not GET parameters.<br>
Requests parameters for GET requests (non-authenticated) are appended as sub-path of request URL.<br>
For example:<br>
`https://<client host:port>/v1/ticker/<symbol>/<exchange>`<br>
URL depends on customer’s dedicated service.<br>
`/v1` – protocol version<br>
`/ticker` – method name<br>
`/<symbol>` - name of exchange currency pair (ETH_BTC, BTC_USD …)<br>
`/<exchange>` - name of exchange (Bitfinex, HitBTC …)<br>
<br><br>
< symbol> or trading pair name implies that the base currency is written first then the settlement currency using underscore separator. It means when buy order of ETH_BTC executed, trader obtained ETH and paid by BTC. Or – when sell order of ETH_BTC executed, trader paid for ETH to obtain BTC.
Quants framework provides access to multiple exchanges using single API interface. <br>
<br>
< exchange> could be the following:

Exchange|Public trades|Withdrawal API|Comments
--------|-------------|--------------|--------
Bitfinex|Y|Y|
Kraken|Y|Y|
Bittrex|Y|Y|
HitBTC|Y|Y|
Liqui|Y|N|
Cryptopia|N|Y|Only single buy or sell order at time
Poloniex|Y|Y|

<aside class="notice">
Timestamp- Unix Timestamp converted to long value.
</aside>

<!--## Public Endpoints:-->

## Ticker

**Request**
`https://<client host:port>/v1/ticker/<symbol>/<exchange>`<br>
Returns current ticker information for specified symbol and exchange. 

>JSON<br>*http://127.0.0.1:8082/v1/ticker/BTC_USDT/VirtualMarket*

```json
{
  "symbol": "BTC_USDT",
  "bid": 14284.0,
  "ask": 14286.0,
  "last": "NaN",
  "timestamp": 1519893112006906
}
```

**Response details **

Key|Type|Description
---|----|-----------
bid|price|Innermost bid
ask|price|Innermost ask
last|price|The price at which the last order executed
timestamp|long|The timestamp at which this information was valid


## Orderbook

Get the full order book

>JSON<br> *http://127.0.0.1:8082/v1/book/BTC_USDT/VirtualMarket*

```json
{
  "symbol": "BTC_USDT",
  "bids": [
    {
      "price": 14284.0,
      "amount": 0.70000000000000007
    },
	....
    ,
    {
      "price": 14242.0,
      "amount": 0.70000000000000007
    }
  ],
  "asks": [
    {
      "price": 14286.0,
      "amount": 0.70000000000000007
    },
	....
    ,
    {
      "price": 14328.0,
      "amount": 0.70000000000000007
    }
  ],
  "timestamp": 1519893673210679
}
```

**Request**
`https://<client host:port>/v1/book/<symbol>/<exchange>`<br>
Returns current full order book for specified symbol and exchange. 

**Response details**

Key|Type|Description
---|----|-----------
bids|array of { price, amount }|Prices and amounts of bids. First value with higher price
asks|array of { price, amount }|Prices and amounts of asks. First value with smallest price
timestamp|long|The timestamp at which this information was valid


## Public Trades

Get a list of the most recent trades for the given symbol.

>JSON<br> *http://127.0.0.1:8082/v1/public_trades/BTC_USDT/VirtualMarket*

```json
{
  "trades": [
    {
      "trade_id": "905974be-9f18-48cd-a49d-17b93cadbf30",
      "side": "buy",
      "symbol": "BTC_USDT",
      "amount": 0.6,
      "price": 185.0,
      "exchange": "9568b326-6675-46f6-8df3-829b4a0471e6",
      "timestamp": 1519732270706460
    },
    {
      "trade_id": "d3903736-2438-11b2-b250-b747deb3124d",
      "side": "buy",
      "symbol": "BTC_USDT",
      "amount": 0.1,
      "price": 185.0,
      "exchange": "9568b326-6675-46f6-8df3-829b4a0471e6",
      "timestamp": 1519810104416206
    }
  ]
}
```

**Request**
`https://<client host:port>/v1/public_trades/<symbol>/<exchange>`

**Response details**

Key|Type|Description
---|----|-----------
trades|array of trades |(below in table '*' marked)
*trade_id|string|Trade identifier of the trades exchange
*exchange|string|Traded exchange name
*symbol|string|Traded exchange pair name
*side|string|"sell" or "buy" (can be "" if undetermined)
*price|decimal|price of the trade
*amount|decimal|amount of the trade
*timestamp|long|Time when this trade issued


## Symbols

A list of symbol names and exchanges where this symbol is listed.

>JSON<br> *http://127.0.0.1:8082/v1/symbols/VirtualMarket*

```json
{
  "exchange": "VirtualMarket",
  "pairs": [
    "BTC_EUR",
    "BTC_RUR",
	....
	"VIB_ETH",
    "VIB_USDT"
  ]
}
```

**Request**
`https://<client host:port>/v1/symbols/<exchange>`<br>
Returns list of symbols for specified exchange.

**Response details**

Key|Type|Description
---|----|-----------
exchange|string|exchange
pairs|array of string|List of the pairs code. This strings can be used as universal code in order API for any kind of exchange. 

or<br>

`https://<client host:port>/v1/symbols`<br>
Returns all known symbols wherever they are listed.

>JSON<br> *http://127.0.0.1:8082/v1/symbols*

```json
{
  "symbols": [
    {
      "pair": "BTC_EUR",
      "exchanges": [
        "VirtualMarket"
      ]
    },
    {
      "pair": "BTC_RUR",
      "exchanges": [
        "VirtualMarket"
      ]
    },
	....
	{
      "pair": "VIB_ETH",
      "exchanges": [
        "VirtualMarket"
      ]
    },
    {
      "pair": "VIB_USDT",
      "exchanges": [
        "VirtualMarket"
      ]
    }
  ]
}
```

**Response details**

Key|Type|Description
---|----|-----------
symbols|array of symbols| (below in table '*' marked)
*pair|string|The pair code. This string can be used as universal code in order API for any kind of exchange. 
*exchanges|array of string|List of exchanges where this symbol is listed


## Symbol Details

Get a list of valid symbol IDs and the pair details.

>JSON<br> *http://127.0.0.1:8082/v1/symbol/BTC_USDT*

```json
{
  "pair": "BTC_USDT",
  "base_name": "USDT",
  "base_long_name": null,
  "settlement_name": "USDT",
  "settlement_long_name": null,
  "expiration": -9223372036854775808,
  "exchanges": [
    "VirtualMarket"
  ]
}
```

**Request**
`https://<client host:port>/v1/symbol/<symbol>`<br>

**Response details**

Key|Type|Description
---|----|-----------
pair|string|The pair code. This string can be used as universal code in order API for any kind of exchange.
base_name|string|Name of base currency. (e.g. ETH for ETH_BTC pair)
base_long_name|string|Long name of base currency (e.g. Ethereum for ETH_BTC pair)
settlement_name|string|Name of settlement currency (e.g. BTC for ETH_BTC pair)
settlement_long_name|String|Long name of settlement currency (e.g. Bitcoin for ETH_BTC pair)
expiration|long|Time of expiration, for derivatives.
exchanges|array of string|List of exchanges where this symbol is listed


# RESTFul API Authenticated Endpoints

<!--## Authenticated Endpoints-->

>javascript example

```javascript
const request = require('request')
const crypto = require('crypto')

const apiKey = '<Your API key here>'
const apiSecret = '<Your API secret here>'
const baseUrl = 'https://<client host:port>'

const url = '/v1/order/cancel'
const completeURL = baseUrl + url

const fingerprint = url + body  + apiKey

const signature = crypto
  .createHmac('sha512', apiSecret)
  .update(fingerprint)
  .digest('hex')

const options = {
  url: completeURL,
  headers: {
    'APIKEY': apiKey,
    'SIGNATURE': signature
  },
  body: JSON.stringify(body)
}

return request.post(
  options,
  function(error, response, body) {
    console.log('response:', JSON.stringify(body, 0, 2))
  }
)
```

Authentication is done using an API key and a secret. <br>
As an example of how to authenticate, we can look at the "`/order/cancel`" endpoint.<br>
The authentication procedure is as follows:<br>
The fingerprint is the string constructed from URL endpoint, request JSON body and *API key* <br>
<br>
`fingerprint = (endpoint + body  + API key)`<br>
i.e. :<br>
`fingerprint = (/v1/order/cancel + {client_order_id="14bbaa94..."} + your_api_key)`<br>
<br>
The signature is the hex digest of an *HMAC-SHA512* hash where the message is your payload, and the secret key is your *API secret*.<br>
`signature = HMAC-SHA512(fingerprint, api-secret).digest('hex')`<br>
<br>
These are encoded as HTTP headers named:<br>
`APIKEY`<br>
`SIGNATURE`<br>
<br>
<aside class="notice">
Note: all Authenticated Endpoints use POST request
</aside>

<aside class="notice">
 Accounts: For any specific exchange client can operate with multiple registered accounts. Quants framework using account identifier in order to choose which one to use for the given API call. All account identifier are pre-configured for web API installation. 
</aside>


## Wallet Balances

See your balances

>JSON<br> *http://127.0.0.1:8082/v1/balances/*

>request

```json
 {
  "account_id": null,
  "currencies": null
}
```

>responce

```json
{
  "wallets": [
    {
      "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
      "balances": [
        {
          "currency": "BTC",
          "amount": 300000.0,
          "blocked": 0.0
        },
        {
          "currency": "ETH",
          "amount": 100000.0,
          "blocked": 0.0
        },
        {
          "currency": "USDT",
          "amount": 300000.0,
          "blocked": 0.0
        }
      ]
    },
	...
  ]
}
```

**Request**
`https://<client host:port>/v1/balances`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|N|Account identifier. If not specified, returns balances for all registered accounts.
currencies|array of string|N|Currencies filter.

**Response details**

Key|Type|Description
---|----|-----------
wallets|array of wallets| (below in table '*' marked)
*account_id|String|Account identifier of received balances report 	
*balances|array of balance |Array of balances per currency (below in table '* *' marked)
  * *currency|string|Currency name
  * *amount|decimal|Amount available for trading
  * *blocked|decimal|Amount blocked by placed order, pending withdrawals or some specific tasks of exchange


## New Order

Submit a new Order

>JSON<br> *http://127.0.0.1:8082/v1/order/new/*

>request

```json
{
  "client_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
  "symbol": "BTC_USDT",
  "side": "buy",
  "amount": 0.1,
  "price": 185.0,
  "type": "limit"
}
```

>responce

```json
{
  "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
  "order_id": "c171ea8a-243e-11b2-91f0-47deb3124dc8",
  "exchange_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "timestamp": 1519895007075824
}
```

**Request**
`https://<client host:port>/v1/order/new`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
client_order_id|string|Y|Unique order ID generated by client. From 6 to 64 characters.
account_id|string|Y|Account identifier
symbol|string|Y|Symbol of trading pair (e.g. ETH_BTC)
side|string|Y|“buy” or “sell”
amount|decimal|Y|Amount of currency you want to buy or sell
price|decimal|Y|Order price|
type|string|N|‘limit’ for limit order, or ‘IOC’ Immediate Or Cancel

**Response details**

Key|Type|Description
---|----|-----------
account_id|string|Account identifier of placed order 
order_id|string |Global unique identifier of the created order generated by Quants system
exchange_order_id|string|Order identified of the specific exchange. Not for using in the API. Can be used for debug purposes.
timestamp|long|Order creation time


## Move Order

Change order attributes.

>JSON<br> *http://127.0.0.1:8082/v1/order/move/*

>request

```json
{
  "client_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "amount": 0.5,
  "price": 18500.0,
}
```

>responce

```json
{
  "exchange_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "timestamp": 1519895007075824
}
```

<aside class="notice">
Note: some of exchange does not provide functionality to change order. In this case Quants framework doing this work internally by canceling previous order and create new one. From API prospective it works same in all cases by sending one request /order/move.
</aside>

**Request**
`https://<client host:port>/v1/order/move`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
client_order_id|string|Y|Order ID generated by client, the same as in moving order
price|decimal|Y|New price
amount|decimal|Y|New amount

**Response details**

Key|Type|Description
---|----|-----------
exchange_order_id|string|Order identified of the specific exchange. Some exchanges change this identifier after order move. This field can be used for debug purposes.
timestamp|long|Order modification time

<aside class="notice">
Note: order move operation does not change order identifier. Only ‘exchange_order_id’ can be changed
</aside>


## Cancel Order

Cancel an order

>JSON<br> *http://127.0.0.1:8082/v1/order/cancel/*

>request

```json
{
  "client_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
}
```

>responce

```json
{}
```

**Request**
`https://<client host:port>/v1/order/cancel`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
client_order_id|string|Y|Order ID generated by client, the same as in cancelling order

**Response details**
Response is empty or error with description


## Order Status

Get the status of an order. Is it active? Was it cancelled? To what extent has it been executed? etc.

>JSON<br> *http://127.0.0.1:8082/v1/order/status/*

>request

```json
{
  "client_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
}
```

>responce

```json
{
  "client_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
  "order_id": "70796e7e-2442-11b2-91f0-47deb3124dc8",
  "exchange_order_id": "2b848174-243c-11b2-95a0-b747deb3124d",
  "symbol": "BTC_USDT",
  "side": "buy",
  "state": "canceled",
  "amount": 0.0,
  "executed_amount": 0.0,
  "remaining_amount": 0.0,
  "price": 185.0,
  "timestamp_submitted": 1519895604433220,
  "timestamp_last_modified": 1519895606330911
}
```

**Request**
`https://<client host:port>/v1/order/status`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
client_order_id|string|Y|Order ID generated by client

**Response details**

Key|Type|Description
---|----|-----------
order_id|string |Global unique identifier of the created order generated by Quants system
exchange_order_id|string|Order identified of the specific exchange
symbol|string|Symbol of trading pair (e.g. ETH_BTC)
side|string|“buy” or “sell”
state|string|“active” if order still alive<br>“filled” when order fully executed<br>“canceled” when order was canceled<br>“error” if order was failed during /order/new or /order/move request
amount|decimal|Amount was the order originally submitted of last modified
executed_amount|decimal|How much of the order has been executed so far in its history
remaining_amount|decimal|How much is still remaining to be submitted
price|double|The price the order was issued
timestamp_submitted|long|Time when order was submitted on exchange
timestamp_last_modified|long|Order last modification time


## Active Orders

Response all active orders.

>JSON<br> *http://127.0.0.1:8082/v1/order/orders/*

>request

```json
{
  "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
}
```

>responce

```json
{
  "active_orders": [
    {
      "client_order_id": "e36b17b4-2007-11b2-beb0-9049f1f1bbe9",
      "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
      "order_id": "471b215a-2053-11b2-beb0-be84e16cd6ae",
      "exchange_order_id": "e36b17b4-2007-11b2-beb0-9049f1f1bbe9",
      "symbol": "BTC_USDT",
      "side": "buy",
      "state": "active",
      "amount": 0.1,
      "executed_amount": 0.0,
      "remaining_amount": 0.1,
      "price": 185.0,
      "timestamp_submitted": 1519732270706460,
      "timestamp_last_modified": 1519732270706460
    },
    {
      "client_order_id": "fc742d68-2232-11b2-9120-5e0d1c06b747",
      "account_id": "9568b326-6675-46f6-8df3-829b4a0471e6",
      "order_id": "38faa686-2233-11b2-93e0-6cd6ae529049",
      "exchange_order_id": "fc742d68-2232-11b2-9120-5e0d1c06b747",
      "symbol": "BTC_USDT",
      "side": "buy",
      "state": "active",
      "amount": 0.1,
      "executed_amount": 0.0,
      "remaining_amount": 0.1,
      "price": 185.0,
      "timestamp_submitted": 1519810104416206,
      "timestamp_last_modified": 1519810104416206
    }
  ]
}
```

**Request**
`https://<client host:port>/v1/order/orders`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|N|Account identifier. If not specified all active orders from all accounts will send in response.

**Response details**

Key|Type|Description
---|----|-----------
active_orders| array of `active_order`|(below in table '*' marked)
*account_id|string|Order account identifier
*client_order_id|string|Order ID generated by client
*order_id|string|Global unique identifier of the created order generated by Quants system
*exchange_order_id|string|Order identified of the specific exchange
*symbol|string|Symbol of trading pair (e.g. ETH_BTC)
*side|string|“buy” or “sell”
*amount|decimal|Amount was the order originally submitted of last modified
*executed_amount|decimal|How much of the order has been executed so far in its history
*remaining_amount|decimal|How much is still remaining to be submitted
*price|decimal|The price the order was issued
*timestamp_submitted|long|Time when order was submitted on exchange
*timestamp_last_modified|long|Order last modification time


## Trades

Get list of own trades

>JSON<br> **

>request

```json

```

>responce

```json

```

**Request**
`https://<client host:port>/v1/order/trades`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|N|Account identifier. If not specified all trades from all accounts will send in response.
timestamp_from|long|N|Begin of time range (default value is current time minus 2 days)
timestamp_to|long|N|End of time range (default value is maximum time value)

**Response**

Key|Type|Description
---|----|-----------
account_id|string|Identifier of the account where trade issued
trade_id|string|Trade identifier
exchange_trade_id|string|Trade identified of the specific exchange
client_order_id|string|Order ID generated by client
order_id|string|Global unique identifier of the created order generated by Quants system
symbol|string|Symbol of trading pair (e.g. ETH_BTC)
side|string|“buy” or “sell”
amount|decimal|Amount was the order originally submitted of last modified
executed_amount|decimal|Amount of the trade
executed_price|decimal|Price of the executed amount
fee|decimal|Fee charged by exchange. In absolute volume nominated by fee currency.
fee_currency|string|Currency of the fee amount
timestamp|long|Time of the trade

<aside class="notice">
Note: response is limited by 50000 trades. When number of trades in specified range exceed this number error returned. You can reduce time range to avoid error.
</aside>


## Get deposit address

>JSON<br> **

>request

```json

```

>responce

```json

```

**Request**
`https://<client host:port>/v1/get_deposit_address`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|Y|Account identifier.
currency|string|Y|Currency of the deposit.

**Response**

Key|Type|Description
---|----|-----------
address|string|Deposit address.
payment_id|string|Used to identify transactions to merchants and exchanges.


## Withdraw

>JSON<br> **

>request

```json

```

>responce

```json

```

**Rrequest**
`https://<client host:port>/v1/withdraw`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|Y|Account identifier.
currency|string|Y|Currency of the deposit.
amount|decimal|Y| 
destination_address|string|Y| 

**Responce**

Key|Type|Description
---|----|-----------
account_id|string| 
success|boolean| 


## Get withdraw transactions

>JSON<br> **

>request

```json

```

>responce

```json

```

**Rrequest**
`https://<client host:port>/v1/get_withdraw_transactions`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|Y|Account identifier.
timestamp_from|long|Y|Time 
timestamp_to|long|Y| 

**Response**

Key|Type|Description
---|----|-----------
account_id|string| 
withdraw_transactions|array of transactions| (below in table '*' marked)
*market_transaction_id|string| 
*address|string| 
*transaction_id|string| 
*payment_id|string| 
*amount|decimal| 
*transaction_completed|boolean| 
*payment_date_time|long| 
*transaction_fee|decimal| 
*description|string| 


## Get deposit transactions

>JSON<br> **

>request

```json

```

>responce

```json

```

**Rrequest**
`https://<client host:port>/v1/get_deposit_transactions`

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
account_id|string|Y|Account identifier.
timestamp_from|long|Y|Time 
timestamp_to|long|Y| 

**Response**

Key|Type|Description
---|----|-----------
account_id|string| 
deposit_transactions|array of transactions|(below in table '*' marked)
*market_transaction_id|string| 
*address|string| 
*transaction_id|string| 
*payment_id|string| 
*amount|decimal| 
*transaction_completed|boolean| 
*payment_date_time|long| 
*transaction_fee|decimal| 
*description|string| 


# RESTFul API Web Socket market data

<!--# Web Socket market data-->

Web socket channels provides real-time data for order book changes, executed trades, balance changes etc...<br>
Market data service are available by dedicated URL for the customer.<br>
`ws://<client host:ws port>/v1/marketdata`

## Login
First message sent to the Web Socket server should be *login*. Login message includes all required info for client authentication. When login occurs client automatically received orders and trades snapshot and then any order and trade updates. 

**Request details**

Key|Type|Required|Description
---|----|--------|-----------
evt|string|Y|Event name. Should be “login”
api_key|string|Y|API key
auth_sig|string|Y|HMAC SHA512 signature of the fingerprint constructed as:<br>“AUTH” + auth_nonce + API key
auth_nonce|int|Y|Any unique non repeating number between logins.  (i.e. ticks of CPU timer)

>JavaScript example of the ‘auth_sig’ calculation:

```javascript
const request = require('request')
const crypto = require('crypto')

const apiKey = '<Your API key here>'
const apiSecret = '<Your API secret here>'
const baseUrl = 'https://<client host:ws port>'

const authNonce = Date.now().toString()
const fingerprint = ‘AUTH’ + authNonce + apiKey

const  authSig = crypto
  .createHmac('sha512', apiSecret)
  .update(fingerprint)
  .digest('hex')
```

**Response**

Key|Type|Description
---|----|-----------
evt|string|“logon”
auth_nonce|int|Nonce specified in login request

## Orders and Trades Snapshot
Snapshot of the active orders and trades issued last 2 days will send to client automatically after successfully logon.

**Response**

Key|Type|Description
---|----|-----------
evt|string|“snapshot”
orders|array|Array of active orders (below in table '*' marked)
*account_id|string|Order account identifier 
*client_order_id|string|Order ID generated by client
*order_id|string|Global unique identifier of the created order generated by Quants system
*exchange_order_id|string|Order identified of the specific exchange
*symbol|string|Symbol of trading pair (e.g. ETH_BTC)
*side|string|"buy" or "sell"
*state|string|"active" if order still alive<br>"filled" when order fully executed<br>"canceled" when order was canceled<br>"error" if order was failed during /order/new or /order/move request
*amount|decimal|Amount was the order originally submitted of last modified
*executed_amount|decimal|How much of the order has been executed so far in its history
*remaining_amount|decimal|How much is still remaining to be submitted
*price|price|The price the order was issued
*timestamp_submitted|rime|Time when order was submitted on exchange
*timestamp_last_modified|rime|Order last modification time
trades|array|Array of trades issued last 2 days (below in table '*' marked)
*account_id|string|Identifier of the account where trade issued
*trade_id|string |Trade identifier
*exchange_trade_id|string|Trade identified of the specific exchange
*client_order_id|string|Order ID generated by client
*order_id|string|Global unique order identifier 
*symbol|string|Symbol of trading pair (e.g. ETH_BTC)
*side|string|"buy" or "sell"
*Amount|decimal|Amount was the order originally submitted of last modified
*executed_amount|decimal|Amount of the trade
*executed_price|price|Price of the executed amount
*fee|decimal|Fee charged by exchange. In absolute volume nominated by fee currency.
*fee_currency|string|Currency of the fee amount
*timestamp|long|Time of the trade


## Channel subscription and unsubscription
To subscribe to any data provided by Web Socket service client should subscribe to specific channel – 'book', 'balances' …<br>
Let’s looks to request and responses examples for orderbook channel - 'book'<br>

**Subscription request**

Key|Type|Required|Description
---|----|--------|-----------
evt|string|Y|"subscribe"
channel|string|Y|"book"
exchanges|array|Y|Array of names of Exchanges to subscribe
symbols|array|Y|Array of names of symbols for currency pairs to subscribe

**Subscription response**

Key|Type|Description
---|----|-----------
evt|string|"subscribed"
channel|string|"book"
channel_id|string|Identifier of the channel

**Channel online event**

Key|Type|Description
---|----|-----------
evt|string|"online"
channel|string|"book"
channel_id|string|Identifier of the channel

'online' event sends to the clients after Web Socket server sent 'book' events with current orderbook snapshot state.

**Unsubscription request**

Key|Type|Required|Description
---|----|--------|-----------
evt|string|Y|"unsubscribe"
channel_id|string|Y|Identifier of the channel to unsubscribe

**Unsubscription response**

Key|Type|Description
---|----|-----------
evt|string|“unsubscribed”
channel_id|string|Identifier of the channel


## Book event

Order book full update. Event name 'book'

>json

```json
{
	"evt": "book",
	"channel_id": "",
	"account_id": "",
	"data": [
		{
			"symbol": "",
			"bids": [
				{
					"price": 0,
					"amount": 0
				}
			],
			"asks": [
				{
					"price": 0,
					"amount": 0
				}
			],
			"timestamp": 0
		}
	]
}
```

Key|Type|Description
---|----|-----------
evt|string|“book”
channel_id|string|Channel identifier
exchange |string|Exchange name
data|array|Array of order books (below in table '*' marked)
*symbol|string|Symbol of trading pair (e.g. ETH_BTC)
*bids|array of { price, amount }|Prices and amounts of bids. First value with higher price 	
*asks|array of { price, amount }|Prices and amounts of asks. First value with smallest price
*timestamp|long|The timestamp of latest order book change

<aside class="notice">
Note: 'book' request provides full order books data on every update instead of delta change.
</aside>


## Balances event

Subscribe to changes of account balances. Event name 'balances'<br>
Balances channel requires 'account_id' in subscription request

**Subscription request**

Key|Type|Required|Description
---|----|--------|-----------
evt|string|Y|“subscribe”
channel|string|Y|“balances”
account_id|string|Y|Account identifier

Unsubscription request same as other events

>json

```json
{
	"evt": "balances",
	"channel_id": "",
	"account_id": "",
	"data": [
		{
			"currency": 0,
			"amount": 0,
			"blocked": 0
		}
	]
}
```

**Event**

Key|Type|Description
---|----|-----------
Key|Type|Description
evt|string|“balances”
channel_id|string|Channel identifier
account_id|string|Account identifier
data|array|array of balances (below in table '*' marked)
*currency|string|Currency name
*amount|decimal|Amount available for trading
*blocked|decimal|Amount blocked by placed order, pending withdrawals or some specific tasks of exchange


## Error handling
 Any kind of request – POST or GET completed by successful HTTP response with code 200 or error HTTP response. Error will include JSON body with error description:

Key|Type|Description
---|----|-----------
error_code|int|Numerical code of the error
error_name|string|Name of numerical error code
description|string|Error description