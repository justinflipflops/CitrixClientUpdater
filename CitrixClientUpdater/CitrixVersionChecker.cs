using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Dom;

namespace CitrixClientUpdater
{
    public class CitrixVersionChecker
    {
        const string internal_CitrixWindowsURL = @"https://www.citrix.com/downloads/workspace-app/windows/workspace-app-for-windows-latest.html";
        const string internal_CitrixMacURL = @"https://www.citrix.com/downloads/workspace-app/mac/workspace-app-for-mac-latest.html";
        private CitrixClientOperatingSystem OperatingSystem { get; }
        public CitrixVersionChecker(CitrixClientOperatingSystem OperatingSystem)
        {
            this.OperatingSystem = OperatingSystem;
        }
        public async Task<CitrixVersionInformation> GetLatestVersionInformation()
        {
            CitrixVersionInformation _CVI = new CitrixVersionInformation();
            const string internal_CtxTextContent = "ctx-text-content";
            const string internal_NameSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>h1";
            const string internal_ReleaseDateSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>h3";
            const string internal_DownloadLinkSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>div:nth-child(4)>div:nth-child(2)>div>div>div>div>a";
            const string internal_VersionSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>div:nth-child(3)>div:nth-child(1)>div:nth-child(1)>div:nth-child(3)>div>p:nth-child(2)";
            const string internal_WindowsChecksumSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>div:nth-child(3)>div:nth-child(1)>div:nth-child(1)>div:nth-child(3)>div>div";
            const string internal_MacOSChecksumSelector = "body>div>div>div:nth-child(2)>div:nth-child(2)>div>div:nth-child(3)>div>div:nth-child(3)>div:nth-child(1)>div:nth-child(1)>div:nth-child(3)>div>p:nth-child(3)";
            string _citrixUpdateURL = this.OperatingSystem == CitrixClientOperatingSystem.WINDOWS ? internal_CitrixWindowsURL : internal_CitrixMacURL;
            IConfiguration angle_config = Configuration.Default.WithDefaultLoader();
            using (IBrowsingContext angle_browsingContext = BrowsingContext.New(angle_config))
            {
                try
                {
                    IDocument angle_document = await angle_browsingContext.OpenAsync(_citrixUpdateURL);
                    _CVI.Name = angle_document.QuerySelector(internal_NameSelector).TextContent.Trim();
                    _CVI.ReleaseDate = angle_document.QuerySelector(internal_ReleaseDateSelector).TextContent.Replace("Release Date:", "").Trim();
                    _CVI.Version = angle_document.QuerySelector(internal_VersionSelector).TextContent.Replace("Version:", "").Trim();
                    _CVI.DownloadURL = "https:" + angle_document.QuerySelector(internal_DownloadLinkSelector).GetAttribute("rel");
                    _CVI.FileName = Path.GetFileName(_CVI.DownloadURL.Split("?")[0]);
                    string _checksum = angle_document.QuerySelector(this.OperatingSystem == CitrixClientOperatingSystem.WINDOWS ? internal_WindowsChecksumSelector : internal_MacOSChecksumSelector).TextContent;
                    _checksum = this.OperatingSystem == CitrixClientOperatingSystem.WINDOWS ? _checksum.Replace("-", "").Replace(" ", "").Replace("\xA0","").ToUpperInvariant().Trim() : _checksum.Replace("Checksums\n", "").Replace("-", "").Replace(" ", "").Replace("\xA0", "").ToUpperInvariant().Trim();
                    if (_checksum.StartsWith("SHA"))
                    {
                        if (_checksum.StartsWith("SHA256"))
                        {
                            _CVI.ChecksumType = CitrixChecksumType.SHA256;
                            _CVI.Checksum = _checksum.Substring(6);
                        }else if (_checksum.StartsWith("SHA1"))
                        {
                            _CVI.ChecksumType = CitrixChecksumType.SHA1;
                            _CVI.Checksum = _checksum.Substring(4);
                        }
                        else
                        {
                            _CVI.ChecksumType = CitrixChecksumType.UNKNOWN;
                            _CVI.Checksum = "";
                        }
                    }
                    return _CVI;
                }
                catch (Exception _ex)
                {
                    _ex = _ex;
                }
            }
            return null;
        }
    }
    public class CitrixVersionInformation
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string ReleaseDate { get; set; }
        public string DownloadURL { get; set; }
        public string Version { get; set; }
        public string Checksum { get; set; }
        public CitrixChecksumType ChecksumType { get; set; }
    }
    public enum CitrixChecksumType
    {
        SHA1,
        SHA256,
        UNKNOWN
    }
    public enum CitrixClientOperatingSystem
    {
        WINDOWS,
        MACOS
    }
}
