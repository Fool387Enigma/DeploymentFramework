// Deployment Framework for BizTalk
// Copyright (C) 2008-14 Thomas F. Abraham, 2004-08 Scott Colestock
// This source file is subject to the Microsoft Public License (Ms-PL).
// See http://www.opensource.org/licenses/ms-pl.html.
// All other rights reserved.

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Genghis;
using clp = Genghis.CommandLineParser;

namespace SSOSettingsFileManager
{
    [clp.ParserUsage("Import a settings file (as generated by SettingsFileGenerator.xls/.xml) into the SSO database.")]
    class SSOSettingsFileImportCommandLine : CommandLineParser
    {
        [clp.ValueUsage("Affiliate application name", Optional = false, MatchPosition = true)]
        public string affiliateAppName = null;

        [clp.ValueUsage("Settings filespec", Optional = true, MatchPosition = false)]
        public string settingsFile = null;

        [clp.FlagUsage("Just list name/values for existing application", Optional = true, MatchPosition = false)]
        public bool list = false;

        [clp.FlagUsage("Delete application", Optional = true, MatchPosition = false)]
        public bool deleteApp = false;

        [clp.ValueUsage("User group name", Optional = true, MatchPosition = false)]
        public string userGroupName = "BizTalk Application Users";

        [clp.ValueUsage("Admin group name", Optional = true, MatchPosition = false)]
        public string adminGroupName = "BizTalk Server Administrators";

        [clp.ValueUsage("Property name to modify", Optional = true, MatchPosition = false)]
        public string propToModify = null;

        [clp.ValueUsage("Property value to modify", Optional = true, MatchPosition = false)]
        public string propValue = null;

    }

    class SettingsFileImport
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            SSOSettingsFileImportCommandLine cl = new SSOSettingsFileImportCommandLine();
            if (!cl.ParseAndContinue(args))
                return -1;

            if (cl.list)
            {
                try
                {
                    Console.WriteLine(SSOSettingsManager.GetRawSettings(cl.affiliateAppName, false));
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Unable to list application contents: " + ex.Message);
                    return 1;
                }

                return 0;
            }

            if (cl.propToModify != null && cl.propValue != null)
            {
                try
                {
                    SSOSettingsManager.WriteSetting(cl.affiliateAppName, cl.propToModify, cl.propValue);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Unable to update property name/value: " + ex.Message);
                    return 1;
                }

                return 0;
            }

            if (cl.deleteApp)
            {
                try
                {
                    SSOSettingsManager.DeleteApp(cl.affiliateAppName);
                    Console.WriteLine("Affiliate application '{0}' deleted.", cl.affiliateAppName);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Unable to delete: " + ex.Message);
                }

                return 0;
            }

            string settingsXml = null;
            try
            {
                settings inSettings = null;

                // Make sure we can deserialize the file cleanly.
                XmlSerializer serializer = new XmlSerializer(typeof(settings));
                using (FileStream stream = new FileStream(cl.settingsFile, FileMode.Open, FileAccess.Read))
                {
                    inSettings = (settings)serializer.Deserialize(stream);
                }

                StringBuilder sb = new StringBuilder();
                StringWriter writer = new StringWriter(sb);
                serializer.Serialize(writer, inSettings);

                settingsXml = sb.ToString();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error reading file:");
                Console.WriteLine(ex.ToString());
                return -1;
            }

            try
            {
                SaveSettingsToSSO(cl.affiliateAppName, settingsXml, cl.userGroupName, cl.adminGroupName);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error persisting to SSO:");
                Console.WriteLine(ex.ToString());
                return -1;
            }

            return 0;
        }

        private static void SaveSettingsToSSO(string affiliateAppName, string inSettings, string userGroupName, string adminGroupName)
        {
            // Create affiliate app if it doesn't exist.
            if (!SSOSettingsManager.AppExists(affiliateAppName))
            {
                SSOSettingsManager.CreateApp(affiliateAppName, userGroupName, adminGroupName);
                Console.WriteLine("Affiliate application '{0}' was created.", affiliateAppName);
            }
            else
            {
                Console.WriteLine("Affiliate application '{0}' already exists.", affiliateAppName);
            }

            SSOSettingsManager.WriteRawSettings(affiliateAppName, inSettings);
            Console.WriteLine("Settings file was associated with application '{0}' in SSO.", affiliateAppName);
        }
    }
}
