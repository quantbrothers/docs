using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Transport;

namespace QuickFIXExchangeExample
{
    class TestFixApp : IApplication
    {
        /// <summary>
        /// Time in miliseconds
        /// </summary>
        private static int MaxResponceTime = 7000;

        private static bool ViewAdminMessages = false;
        private SocketInitiator socketInitiator;
        private Session session;
        private AutoResetEvent WaitResponseResetEvent = new AutoResetEvent(false);
        private bool isRejectMessage;

        #region Colors
        ConsoleColor defColor = ConsoleColor.White;
        ConsoleColor adminReqColor = ConsoleColor.DarkGray;
        ConsoleColor adminRespColor = ConsoleColor.DarkGray;
        ConsoleColor reqColor = ConsoleColor.DarkYellow;
        ConsoleColor respColor = ConsoleColor.DarkCyan;
        ConsoleColor logonColor = ConsoleColor.DarkGreen;
        ConsoleColor logoutColor = ConsoleColor.Yellow;
        #endregion Colors

        public TestFixApp(string ConfigPath)
        {
            if (string.IsNullOrEmpty(ConfigPath))
            {
                ConfigPath = "DefConfig.cfg";
            }
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath,
               @"
                [DEFAULT]
                ConnectionType = initiator
                ReconnectInterval = 2
                FileStorePath = store
                FileLogPath = log
                DebugFileLogPath = debug.log
                StartTime = 00:00:00
                EndTime = 00:00:00
                UseDataDictionary = Y
                DataDictionary = FIX_Spec_44.xml
                SocketConnectHost = 127.0.0.1
                SocketConnectPort = 5007
                LogoutTimeout = 5

                [SESSION]
                # inherit ConnectionType, ReconnectInterval and SenderCompID from default
                BeginString = FIX.4.4
                SenderCompID = TestClient
                TargetCompID = Gateway
                HeartBtInt = 30
                ResetOnLogon=Y
                [Log]
                    ");
            }

            var config = File.OpenText(ConfigPath);
            SessionSettings sessionSettings = new SessionSettings(config);
            IMessageStoreFactory storeFactory = new FileStoreFactory(sessionSettings);
            socketInitiator = new SocketInitiator(this, storeFactory, sessionSettings);
        }

        public void WaitResponces(int waitMessagesCount)
        {
            for (int i = 0; i < waitMessagesCount && !isRejectMessage; i++)
            {
                if (!WaitResponseResetEvent.WaitOne(MaxResponceTime))
                {
                    throw new TimeoutException("Response timeout left.");
                }
            }
            isRejectMessage = false;
        }

        public bool Send(QuickFix.FIX44.Message msg)
        {
            return session.Send(msg);
        }

        public void Start()
        {
            socketInitiator.Start();
            if (!WaitResponseResetEvent.WaitOne(MaxResponceTime))
            {
                throw new TimeoutException("Logon time is out");
            }
        }

        public void Stop()
        {
            socketInitiator.Stop();
        }


        #region IApplication_Implementation
        public void ToAdmin(QuickFix.Message message, SessionID sessionID)
        {
            Console.ForegroundColor = adminReqColor;
            Console.WriteLine($"ToAdmin {message.ToString(ViewAdminMessages)}");
            Console.ForegroundColor = defColor;
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID)
        {
            isRejectMessage = message.IsReject();
            Console.ForegroundColor = isRejectMessage ? ConsoleColor.DarkRed : adminRespColor;
            Console.WriteLine($"FromAdmin {message.ToString(isRejectMessage || ViewAdminMessages)}");
            Console.ForegroundColor = defColor;
            if (isRejectMessage)
            {
                WaitResponseResetEvent.Set();
            }
        }

        public void ToApp(QuickFix.Message message, SessionID sessionId)
        {
            Console.ForegroundColor = reqColor;
            Console.WriteLine(message.ToString(true));
            Console.ForegroundColor = defColor;
        }

        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            Console.ForegroundColor = respColor;
            Console.WriteLine(message.ToString(true));
            Console.ForegroundColor = defColor;
            isRejectMessage = message.IsReject();
            WaitResponseResetEvent.Set();
        }

        public void OnCreate(SessionID sessionID)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Created ID: {sessionID}");
            Console.ForegroundColor = defColor;
            session = Session.LookupSession(sessionID);
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.ForegroundColor = logoutColor;
            Console.WriteLine($"Logout ID: {sessionID}");
            Console.ForegroundColor = defColor;
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.ForegroundColor = logonColor;
            Console.WriteLine($"Logon with ID: {sessionID}");
            Console.ForegroundColor = defColor;
            WaitResponseResetEvent.Set();
        }

        #endregion #region IApplication_Implementation
    }
}
