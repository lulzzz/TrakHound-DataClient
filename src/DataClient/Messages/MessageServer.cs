// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using System.ServiceModel;
using TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient.Messages
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class MessageServer : IMessage
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static IMessageCallback callback;

        public MessageServer()
        {
            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IMessageCallback>();
            }
            catch (Exception ex)
            {
                log.Error("Error during MessageServer Start");
                log.Trace(ex);
            }
        }

        public object SendData(Message data)
        {
            if (data != null && data.Id != null)
            {
                log.Info("Message Received : " + data.Text);

                switch (data.Id.ToLower())
                {
                    case "command":

                        if (!string.IsNullOrEmpty(data.Text))
                        {
                            switch (data.Text.ToLower())
                            {
                                case "start": Program.Start(); break;

                                case "stop": Program.Stop(); break;
                            }
                        }

                        break;
                }
            }

            return "Message Sent Successfully!";
        }

        public static void SendCallback(Message data)
        {
            try
            {
                callback.OnCallback(data);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

    }
}
