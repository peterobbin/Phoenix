﻿namespace phoenix
{
    using Properties;
    using System.Threading.Tasks;

    // All of these callbacks are called on UI thread
    public partial class MainDialog
    {
        private int m_MqttRetryMinutes = 2;
        private void OnProcessStop()
        {
            m_Monitoring = false;
            SendCrashEmail();
            ResetWatchButtonLabel();
            Logger.MainDialog.WarnFormat("Process stopped ({0}).", m_ProcessRunner.ProcessPath);
        }

        private void OnProcessStart()
        {
            m_Monitoring = true;
            ResetWatchButtonLabel();
            m_AppSettings.Store("Internal", "CachedName", m_ProcessRunner.CachedTitle);
            Logger.MainDialog.InfoFormat("Process started ({0}).", m_ProcessRunner.ProcessPath);
        }

        private void OnMqttConnectionOpen()
        {
            ResetMqttConnectionLabel();
            Logger.MainDialog.Info("MQTT connection established.");
        }

        private void OnMqttConnectionClose()
        {
            ResetMqttConnectionLabel();
            Logger.MainDialog.WarnFormat("MQTT connection closed, retrying in {0} minutes."
                , m_MqttRetryMinutes);

            Task.Delay(new System.TimeSpan(0, m_MqttRetryMinutes, 0)).ContinueWith(fn => {
                Logger.MainDialog.Info("MQTT attempting to reconnect.");
                m_RemoteManager.Connect(mqtt_server_address.Text, Resources.MqttTopic);
            });
        }

        private void OnMqttMessage(string message, string topic)
        {
            Logger.MainDialog.InfoFormat("MQTT message received: ({0}) from ({1})."
                , message, Resources.MqttTopic);

            if (topic != Resources.MqttTopic)
                return;

            if (message == "echo") {
                string echo = string.Format("{{ \"name\":\"{0}\", \"public_key\":\"{1}\" }}",
                    RsyncClient.MachineIdentity,
                    RsyncClient.PublicKey.Trim('\n'));

                m_RemoteManager.Publish(echo, string.Format("{0}/machines", Resources.MqttTopic));
            }
        }
    }
}
