---
title: API Reference

language_tabs: # must be one of https://git.io/vQNgJ
  - C#

toc_footers:
  - <a href='http://www.QuantBrothers.com'>QuantBrothers</a>
  - <a href='https://github.com/quantbrothers/docs/tree/master/QuickFIXExchangeExample'>Example</a>
  

includes:

search: true
---

# Framework gateway FIX API
message which represents 


## Workflow

After connecting and logging on, the client can either request a security list or subscribe to market data.


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

*.*FIX 4.4 EP 26 is used to backport MDEntryType (269) = Q Action Clearing Price to denote indicative and final action prices.*


## Logon < A> message

The `Logon <A>` message must be the first message sent by the application requesting to initiate a FIX session. The `Logon <A>` message authenticates an institution establishing a connection to Server.

Tag|Name|Req|Description
---|----|---|-----------
98|EncryptMethod|Y|Server does not support encryption. Valid values: 0 = None.
108|HeartBtInt|Y|`Heartbeat <0>` message interval (seconds).
141|ResetSeqNumFlag|N|Indicates that the both sides of the FIX session should reset sequence numbers. Valid values: Y = Yes, reset sequence numbers N = No.


## Logout < 5> message

The `Logout <5>` message initiates or confirms the termination of a FIX session. Disconnection without the exchange of `Logout <5>` messages should be interpreted as an abnormal condition.


## New Order Single < D> message

To submit a new order to server, send a `New Order Single <D>` message. Server will respond to a `New Order Single <D>` message with an `Execution Report <8>`.

>For lib Quickfix

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
11 | ClOrdID | Y | Unique identifier of the order as assigned by the client. Uniqueness must be guaranteed by the client for the duration of the lifetime the order.
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
17 | ExecID | Y | Unique identifier of execution message as assigned by server (0 (zero) for `ExecType <150>` = 8 Rejected)
150 | ExecType | Y |Describes the purpose of the Execution Report <br>Possible values: <br>	I = Order Status<br>	6 = Pending Cancel<br>	E = Pending <br>Replace<br>	F = Trade<br>	8 = Rejected
39 | OrdStatus | Y |Describes the current order status <br>Possible values:<br>   A = Pending New<br>   0 = New<br>   1 = Partially filled<br>   2 = Filled<br>   4 = Canceled<br>   8 = Rejected
58 | Text | N |Error description if contains
54 | Side | Y | Side of the order.<br>Possible values:<br>   1 = Buy<br>   2 = Sell<br>   B = "As Defined" (when `ExecType <150>` = 8 Rejected and order not found)
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
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. Uniqueness must be guaranteed by the client for the duration of the lifetime the order. (new if needed, otherwise it can be equal OrigClOrdID)
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
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. Uniqueness must be guaranteed by the client for the duration of the lifetime the order. (new if needed, otherwise it can be equal OrigClOrdID)
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
11 | ClOrdID | Y | New unique identifier of the existing order as assigned by the client. Uniqueness must be guaranteed by the client for the duration of the lifetime the order. (new if needed, otherwise it can be equal OrigClOrdID)
790 | OrdStatusReqID | N | Can be used to uniquely identify a specific `Order Status Request <H>` and response message.

Response on `Order Status Request <H>` is an `Execution Report <8>` message with current order status. `ExecType <150>` in a response may have two values: 8 = Rejected (if order not found) and I = Order Status. `Execution Report <8>` response contain `OrdStatusReqID <790>` if request contain it.


## Order Mass Status Request < AF> message

The `Order Mass Status Request <AF>` message requests the status for orders matching criteria specified within the request.

>For lib Quickfix

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