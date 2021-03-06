﻿namespace phoenix
{
    using System;
    using System.IO;
    using System.Net;
    using System.Xml;
    using System.Reflection;

    /// <summary>
    /// Class responsible for updating Phoenix itself
    /// </summary>
    class UpdateManager
    {
        /// <summary>Compiled version</summary>
        Version m_CurrentVersion;
        /// <summary>Remote version</summary>
        Version m_UpdateVersion;
        /// <summary>Up to date Phoenix URL</summary>
        Uri     m_UpdateAddress;
        /// <summary>Update feed address</summary>
        Uri     m_FeedAddress;
        /// <summary>Update channel</summary>
        string  m_FeedChannel;

        public UpdateManager()
        {
            m_CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            m_UpdateVersion  = Version.Parse("0.0.0.0");
            m_FeedChannel    = Properties.Resources.UpdateChannel;

            Logger.UpdateManager.InfoFormat("Update Manager reports current version is: {0}",
                m_CurrentVersion);
        }

        /// <summary>
        /// Returns the feed address URL
        /// </summary>
        public string FeedAddress
        {
            get { if (m_FeedAddress != null) return m_FeedAddress.OriginalString; else return string.Empty; }
            set { if (!String.IsNullOrEmpty(value)) try { m_FeedAddress = new Uri(value); } catch (Exception ex) { Logger.UpdateManager.ErrorFormat("Feed URL assignment failed: {0}", ex.Message); } }
        }

        /// <summary>
        /// Check for updates of Phoenix
        /// </summary>
        public void Check()
        {
            if (String.IsNullOrEmpty(FeedAddress))
                return;

            Logger.UpdateManager.Info("Checking for updates...");
            XmlDocument feed_xml = new XmlDocument();

            using (WebClient wc = new WebClient())
            {
                wc.DownloadStringCompleted += (object sender, DownloadStringCompletedEventArgs e) => {
                    try
                    {
                        feed_xml.LoadXml(e.Result);
                    }
                    catch (XmlException)
                    {
                        Logger.UpdateManager.Error("Unable to load the feed xml due to malformation.");
                        feed_xml = null;
                    }
                    catch
                    {
                        Logger.UpdateManager.Error("Unable to load the feed xml.");
                        feed_xml = null;
                    }

                    if (feed_xml == null || !feed_xml.HasChildNodes)
                        return;

                    try
                    {
                        if (feed_xml["phoenix"]["channel"].InnerText == m_FeedChannel)
                        {
                            Logger.UpdateManager.InfoFormat("Found a matching update channel: {0}", m_FeedChannel);

                            m_UpdateVersion = Version.Parse(feed_xml["phoenix"]["version"].InnerText);
                            Logger.UpdateManager.InfoFormat("Update version is parsed to be: {0}", m_UpdateVersion);

                            m_UpdateAddress = new Uri(feed_xml["phoenix"]["address"].InnerText);
                            Logger.UpdateManager.InfoFormat("Update address is loaded to be: {0}", m_UpdateAddress);
                        }
                        else
                        {
                            Logger.UpdateManager.WarnFormat("Update channels do not match: {0} vs {1}"
                                , feed_xml["phoenix"]["channel"].InnerText
                                , m_FeedChannel);
                        }
                    }
                    catch
                    {
                        Logger.UpdateManager.Error("Update feed has missing components.");
                        m_UpdateAddress = null;
                    }

                    if (m_UpdateAddress == null)
                        return;

                    if (m_UpdateVersion > m_CurrentVersion)
                    {
                        string temp_loc = Path.Combine(
                            Path.GetTempPath(),
                            Guid.NewGuid().ToString(),
                            Path.GetFileName(m_UpdateAddress.ToString()));

                        Logger.UpdateManager.InfoFormat("Downloading new phoenix to: {0}.", temp_loc);

                        using (WebClient client = new WebClient())
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(temp_loc));
                            client.DownloadFileCompleted += (sender2, e2) =>
                            {
                                string oldapp_location = Assembly.GetExecutingAssembly().Location;
                                string backup_location = Path.Combine(Path.GetDirectoryName(oldapp_location), "backup.dat");

                                Logger.UpdateManager.InfoFormat("Old location is: {0}. Backup location is: {1}"
                                    , oldapp_location
                                    , backup_location);

                                if (File.Exists(backup_location))
                                {
                                    try
                                    {
                                        File.Delete(backup_location);
                                        Logger.UpdateManager.Info("Old backup executable removed.");
                                    }
                                    catch
                                    {
                                        Logger.UpdateManager.Error("Unable to remove old backup.");
                                        return;
                                    }
                                }

                                try
                                {
                                    File.Move(oldapp_location, backup_location);
                                    Logger.UpdateManager.Info("Backup executable created.");
                                }
                                catch
                                {
                                    Logger.UpdateManager.Error("Backup creation failed.");
                                    return;
                                }

                                try
                                {
                                    File.Move(temp_loc, oldapp_location);
                                    Logger.UpdateManager.Info("Updates successfully applied");
                                }
                                catch
                                {
                                    Logger.UpdateManager.Error("Applying updates failed, rolling back.");
                                    File.Move(backup_location, oldapp_location);
                                }
                            };

                            try
                            {
                                client.DownloadFileAsync(m_UpdateAddress, temp_loc);
                            }
                            catch (WebException)
                            {
                                Logger.UpdateManager.Error("Phoenix update download failed due to WebException.");
                                return;
                            }
                            catch
                            {
                                Logger.UpdateManager.Error("Phoenix update download failed due to exception.");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Logger.UpdateManager.Info("Phoenix is already a newer version.");
                    }
                };

                try
                {
                    wc.DownloadStringAsync(m_FeedAddress);
                }
                catch (WebException)
                {
                    Logger.UpdateManager.Error("Phoenix feed download failed due to WebException.");
                    return;
                }
                catch
                {
                    Logger.UpdateManager.Error("Phoenix feed download failed due to exception.");
                    return;
                }
            }
        }
    }
}
