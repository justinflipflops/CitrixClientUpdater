using System;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Net.Http;

namespace CitrixClientUpdater
{
    class Program
    {

        static async Task Main(string[] args)
        {
            const string interal_RootPath = @"C:\Program Files\Citrix\Receiver StoreFront\Receiver Clients\";
            if (Directory.Exists(interal_RootPath))
            {
                Console.WriteLine("[-] Citrix StoreFront Receiver Clients Root Path Found");
                CitrixVersionInformation win_Version = null;
                CitrixVersionInformation mac_Version = null;
                try
                {
                    Console.WriteLine("[-] Checking Citrix for current Windows version information.");
                    CitrixVersionChecker win_CSC = new CitrixVersionChecker(CitrixClientOperatingSystem.WINDOWS);
                    win_Version = await win_CSC.GetLatestVersionInformation();
                    Console.WriteLine($"[-] Latest Citrix for current Windows version: {win_Version.Version}");
                }
                catch
                {
                    Console.WriteLine("[ERROR] Checking Citrix for current Windows version information.");
                    return;
                }
                try
                {
                    Console.WriteLine("[-] Checking Citrix for current MacOS version information.");
                    CitrixVersionChecker mac_CSC = new CitrixVersionChecker(CitrixClientOperatingSystem.MACOS);
                    mac_Version = await mac_CSC.GetLatestVersionInformation();
                    Console.WriteLine($"[-] Latest Citrix for current MacOS version: {mac_Version.Version}");
                }
                catch
                {
                    Console.WriteLine("[ERROR] Checking Citrix for current MacOS version information.");
                    return;
                }
                bool win_clientForceUpdate = false;
                bool mac_clientForceUpdate = false;
                string win_clientPath = interal_RootPath + win_Version.FileName;
                string mac_clientPath = interal_RootPath + mac_Version.FileName;
                if (File.Exists(win_clientPath))
                {
                    Console.WriteLine("[-] Found existing Citrix client for Windows. Checking checksum.");
                    string win_currentChecksum = await GetChecksumBuffered(win_Version.ChecksumType, win_clientPath);
                    if (win_currentChecksum != win_Version.Checksum)
                    {
                        Console.WriteLine("[-] Citrix client for Windows Checksum mismatch. Removing. Flagging for Update.");
                        File.Delete(win_clientPath);
                        win_clientForceUpdate = true;
                    }
                    else
                        Console.WriteLine("[-] Citrix Client for Windows Checksum match. Up to Date. Skipping.");
                }
                else
                {
                    Console.WriteLine("[-] Existing Citrix client for Windows NOT FOUND. Flagging for Download.");
                    win_clientForceUpdate = true;
                }
                if (File.Exists(mac_clientPath))
                {
                    Console.WriteLine("[-] Found existing Citrix client for MacOS. Checking checksum.");
                    string mac_currentChecksum = await GetChecksumBuffered(mac_Version.ChecksumType, mac_clientPath);
                    if (mac_currentChecksum != mac_Version.Checksum)
                    {
                        Console.WriteLine("[-] Citrix client for MacOS Checksum mismatch. Removing. Flagging for Update.");
                        File.Delete(mac_clientPath);
                        mac_clientForceUpdate = true;
                    }
                    else
                        Console.WriteLine("[-] Citrix Client for MacOS Checksum match. Up to Date. Skipping.");
                }
                else
                {
                    Console.WriteLine("[-] Existing Citrix client for MacOS NOT FOUND. Flagging for Download.");
                    mac_clientForceUpdate = true;
                }
                if (win_clientForceUpdate)
                {
                    Console.WriteLine("[-] Updating Citrix client for Windows.");
                    try
                    {
                        using (HttpClient win_downloadClient = new HttpClient())
                        {
                            using (Stream web_Stream = await win_downloadClient.GetStreamAsync(win_Version.DownloadURL))
                            {
                                using (Stream file_Stream = File.OpenWrite(win_clientPath))
                                {
                                    await web_Stream.CopyToAsync(file_Stream);
                                    await file_Stream.FlushAsync();
                                    file_Stream.Close();
                                    Console.WriteLine("[-] Downloaded Citrix client for Windows. Calculating Checksum.");
                                    string win_downloadChecksum = await GetChecksumBuffered(win_Version.ChecksumType, win_clientPath);
                                    if (win_downloadChecksum != win_Version.Checksum)
                                    {
                                        Console.WriteLine("[ERROR] Citrix client for Windows Checksum mismatch. Removing Download.");
                                        File.Delete(win_clientPath);
                                    }
                                    else
                                        Console.WriteLine("[-] Citrix Client for Windows Checksum match.");
                                }
                                web_Stream.Close();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("[ERROR] Updating Citrix client for Windows.");
                    }
                }
                else
                    Console.WriteLine("[-] Skipping Citrix Client for Windows.");
                if (mac_clientForceUpdate)
                {
                    Console.WriteLine("[-] Updating Citrix client for MacOS.");
                    try
                    {
                        using (HttpClient mac_downloadClient = new HttpClient())
                        {
                            using (Stream web_Stream = await mac_downloadClient.GetStreamAsync(mac_Version.DownloadURL))
                            {
                                using (Stream file_Stream = File.OpenWrite(mac_clientPath))
                                {
                                    await web_Stream.CopyToAsync(file_Stream);
                                    await file_Stream.FlushAsync();
                                    file_Stream.Close();
                                    Console.WriteLine("[-] Downloaded Citrix client for MacOS. Calculating Checksum.");
                                    string mac_downloadChecksum = await GetChecksumBuffered(mac_Version.ChecksumType, mac_clientPath);
                                    if (mac_downloadChecksum != mac_Version.Checksum)
                                    {
                                        Console.WriteLine("[ERROR] Citrix client for MacOS Checksum mismatch. Removing Download.");
                                        File.Delete(mac_clientPath);
                                    }
                                    else
                                        Console.WriteLine("[-] Citrix Client for MacOS Checksum match.");
                                }
                                web_Stream.Close();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("[ERROR] Updating Citrix client for MacOS.");
                    }
                }
                else
                    Console.WriteLine("[-] Skipping Citrix Client for MacOS.");
            }
            else
                Console.WriteLine("[ERROR] Citrix Receiver Clients RootPath does not exist. Is this being run on a Citrix StoreFront server?");
            return;
        }
        public static async Task<string> GetChecksumBuffered(CitrixChecksumType Type, String FilePath)
        {
            if (File.Exists(FilePath))
            {
                using (var InputStream = File.OpenRead(FilePath))
                {
                    using (var bufferedStream = new BufferedStream(InputStream, 1024 * 32))
                    {
                        switch (Type)
                        {
                            case CitrixChecksumType.SHA256:
                                SHA256 sha256 = SHA256.Create();
                                byte[] checksum256 = await sha256.ComputeHashAsync(bufferedStream);
                                return BitConverter.ToString(checksum256).Replace("-", String.Empty).ToUpperInvariant();
                            case CitrixChecksumType.SHA1:
                                SHA1 sha1 = SHA1.Create();
                                byte[] checksum1 = await sha1.ComputeHashAsync(bufferedStream);
                                return BitConverter.ToString(checksum1).Replace("-", String.Empty).ToUpperInvariant();
                            default:
                                break;
                        }
                    }
                }
            }
            return null;
        }
    }
}
