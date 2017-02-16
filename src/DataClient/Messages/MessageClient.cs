// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient.Messages
{
    public class MessageClient : IMessageCallback
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private IMessage messageProxy;

        public MessageClient()
        {
            messageProxy = Client.GetWithCallback(MessageServer.PIPE_NAME, this);
        }

        public static void Send(Message data)
        {
            var client = new MessageClient();
            client.SendMessage(data);
        }

        public void SendMessage(Message data)
        {
            try
            {
                if (messageProxy != null) messageProxy.SendData(data);
            }
            catch (Exception ex)
            {
                log.Warn("MessageClient : Send Failed");
                log.Trace(ex);
            }
        }

        public delegate void MessageRecieved_Handler(Message data);
        public event MessageRecieved_Handler MessageRecieved;

        public void OnCallback(Message data)
        {
            MessageRecieved?.Invoke(data);
        }
    }
}
