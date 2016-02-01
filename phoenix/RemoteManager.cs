﻿using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace phoenix
{
    class RemoteManager : IDisposable
    {
        MqttClient  m_client;
        string      m_channel;

        public Action           OnConnectionClosed;
        public Action           OnConnectionOpened;
        public Action<string>   OnMessage;

        public void Connect(string address, string channel)
        {
            if (address == string.Empty || channel == string.Empty)
                return;

            if (Connected)
                m_client.Disconnect();

            try {
                m_client = new MqttClient(address);
            } catch {
                return;
            }

            m_client.MqttMsgPublishReceived += MqttMessageReceived;
            m_client.ConnectionClosed += (s, e) => {
                Logger.Warn("MQTT connection closed.");
                if (OnConnectionClosed != null)
                    OnConnectionClosed();
            };

            m_client.Connect(RsyncClient.MachineIdentity);

            if (m_client.IsConnected)
            {
                m_client.Subscribe(
                    new string[] { channel },
                    new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                m_channel = channel;

                Logger.Info(string.Format("Established an MQTT connection to {0} and subscribed to {1}.",
                    address, channel));

                if (OnConnectionOpened != null)
                    OnConnectionOpened();
            }
        }

        public void Publish(string message)
        {
            if (m_client == null || m_client.IsConnected || m_channel == string.Empty || message == string.Empty)
                return;

            m_client.Publish(m_channel, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }

        void MqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message);

            Logger.Info(string.Format("MQTT message received: ({0}) from ({1}).",
                msg, e.Topic));
            
            if (OnMessage != null)
                OnMessage(msg);
        }

        public bool Connected
        {
            get { return m_client != null && m_client.IsConnected; }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Connected)
                        m_client.Disconnect();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}