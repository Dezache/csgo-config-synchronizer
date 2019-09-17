using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CSGO_Config_Synchronizer
{
    class Program
    {
        static ulong GetSteamId64(ulong tradeId)
        {
            // Converting tradeId to steamId64
            return 2 * (tradeId >> 1) + (tradeId & 1) + 0x0110000100000000;
        }
        static string GetDisplayName(ulong steamId64, string apiKey)
        {
            // Getting the display name using Steam API
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load($"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={steamId64}&format=xml");
            return xmlDoc.DocumentElement.SelectSingleNode("/response/players/player/personaname").InnerText;
        }
        static string GetSteamDirectory()
        {
            // Reading the steam directory in the Windows Registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    return key.GetValue("SteamPath").ToString().Replace('/', '\\');
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("Failed to retrieve Steam directory. Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(1);
                return null;
            }
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("CS:GO Config Synchronizer\n");
            Console.ResetColor();

            string apiKey = "INSERT YOUR API KEY HERE";
            string userdataPath = GetSteamDirectory() + @"\userdata";
            string csgoPath = @"\730\local\cfg\";
            DateTime mostRecentCfgTime = DateTime.MinValue;
            string mostRecentCfg = string.Empty;
            string mostRecentVideo = string.Empty;
            ulong mostRecentAccount = 0;

            // Dictionary of display names of users that have a CSGO config <tradeId, displayName>
            Dictionary<ulong, string> displayNames = new Dictionary<ulong, string>();
            
            // If the userdata folder doesn't exist, exit
            if (!Directory.Exists(userdataPath))
            {
                Console.Write("userdata folder was not found. Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            // Getting the most recent config file (+ its date), the associated video.txt and its account's tradeId
            foreach (string userFolder in Directory.EnumerateDirectories(userdataPath))
            {
                var partsOfPath = userFolder.Split('\\');

                string configFilePath = userFolder + csgoPath + "config.cfg";
                string videoFilePath = userFolder + csgoPath + "video.txt";
                ulong accountId = ulong.Parse(partsOfPath[partsOfPath.Length - 1]);

                if (File.Exists(configFilePath))
                {
                    displayNames.Add(accountId, GetDisplayName(GetSteamId64(accountId), apiKey));

                    DateTime configFileDate = File.GetLastWriteTime(configFilePath);
                    // if the current config file is more recent than mostRecentCfg
                    if (DateTime.Compare(configFileDate, mostRecentCfgTime) > 0)
                    {
                        mostRecentCfgTime = configFileDate;
                        mostRecentCfg = configFilePath;
                        mostRecentAccount = accountId;

                        if (File.Exists(videoFilePath))
                        {
                            mostRecentVideo = videoFilePath;
                        }
                    }
                }
            }

            // If there are no configs, exit
            if (displayNames.Count == 0)
            {
                Console.Write("No users with a CSGO config were found. Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            // Display all accounts that will be affected
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Accounts that have a CSGO config:");
            Console.ResetColor();
            foreach (KeyValuePair<ulong, string> pair in displayNames)
            {
                Console.WriteLine($"{pair.Value} ({pair.Key})");
            }
            Console.WriteLine();

            // Display account with the most recent config
            string mostRecentAccountDisplayName;
            if (displayNames.TryGetValue(mostRecentAccount, out mostRecentAccountDisplayName))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Most recent config: ");
                Console.ResetColor();
                Console.WriteLine($"{mostRecentAccountDisplayName} ({mostRecentCfgTime})");
            }
            Console.WriteLine();

            // Prompt user before copying config to other users
            Console.ForegroundColor = ConsoleColor.DarkRed;
            ConsoleKeyInfo keyPressed;
            do
            {
                Console.Write("Replace all configs with ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(mostRecentAccountDisplayName);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("'s config? (y/n)");
                keyPressed = Console.ReadKey(true);
                Console.WriteLine();
            } while (keyPressed.Key != ConsoleKey.Y && keyPressed.Key != ConsoleKey.N);
            Console.ResetColor();

            if (keyPressed.Key == ConsoleKey.Y)
            {
                Console.WriteLine();

                // Copying config to every user from the displayNames dictionary
                foreach (KeyValuePair<ulong, string> pair in displayNames)
                {
                    // skip if current user is the one with the most recent config
                    if (pair.Key == mostRecentAccount)
                        continue;

                    string userConfig = userdataPath + '\\' + pair.Key + csgoPath + "config.cfg";
                    string userVideo = userdataPath + '\\' + pair.Key + csgoPath + "video.txt";
                    Console.WriteLine($"Copying config.cfg to {pair.Value}...");
                    File.Copy(mostRecentCfg, userConfig, true);
                    Console.WriteLine($"Copying video.txt to {pair.Value}...");
                    File.Copy(mostRecentVideo, userVideo, true);
                }
                
            }
            Console.WriteLine();
            Console.Write("Press any key to close the program...");
            Console.ReadKey(true);
        }
    }
}
