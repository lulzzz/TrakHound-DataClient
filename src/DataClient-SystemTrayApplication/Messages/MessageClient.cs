// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using NLog;
using System;
using TrakHound.Api.v2.WCF;

namespace TrakHound.DataClient.SystemTray.Messages
{
    public class MessageClient : IMessageCallback
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private IMessage messageProxy;

        public MessageClient(string pipename)
        {
            messageProxy = Client.GetWithCallback(pipename, this);
        }

        public static void Send(string pipename, Message data)
        {
            var client = new MessageClient(pipename);
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
                log.Error(ex);
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
