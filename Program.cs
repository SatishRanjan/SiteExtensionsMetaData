using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Diagnostics;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SiteExtesionInstalltionKuduConsoleApp
{
    internal class Program
    {
        private static string feedEndpoint = "https://www.nuget.org/api/v2/";

        static async Task Main(string[] args)
        {
            var id = "Dynatrace";
            // This is an example function to get nuget package metadata for nuget v2 or v3 apis
            // This works for both nuget v2 and v3 endpoints
            await NuGetPackageFetcher.FetchNuGetPackageAsync(feedEndpoint, id);
            bool isInstalled = await IsSiteExtensionInstalled(id);
        }

        private static async Task<bool> IsSiteExtensionInstalled(string id, string? version = null)
        {
            bool isInstalled = false;

            JsonSettings siteExtensionSettings = GetSettingManager(id);

            string localPackageVersion = siteExtensionSettings.GetValue("version");
            string localPackageInstallationArgs = siteExtensionSettings.GetValue("installer_command_line_params");

            SemanticVersion localPkgVer = null;
            SemanticVersion lastFoundVer = null;

            SiteExtensionInfo latestRemotePackage = await GetSiteExtensionInfoFromRemote(id);

            if (!string.IsNullOrEmpty(localPackageVersion))
            {
                SemanticVersion.TryParse(localPackageVersion, out localPkgVer);
            }

            if (latestRemotePackage != null)
            {
                SemanticVersion.TryParse(latestRemotePackage.Version, out lastFoundVer);
            }

            if (lastFoundVer != null && localPkgVer != null && lastFoundVer <= localPkgVer)
            {
                isInstalled = true;
            }

            return isInstalled;
        }

        private static JsonSettings GetSettingManager(string id)
        {
            // The siteExtensions for a site is installed on the app service worker at the location root directory for a site at the location /SiteExtensions
            string filePath = Path.Combine("C:\\myfiles\\code\\SiteExtesionInstalltionKuduConsoleApp\\SiteExtensions", id, "SiteExtensionSettings.json");
            return new JsonSettings(filePath);
        }

        public static async Task<SiteExtensionInfo> GetSiteExtensionInfoFromRemote(string id, string version = null)
        {
            SiteExtensionInfo info = await GetPackageByIdentity(id, version);

            IPackageSearchMetadata data;

            if (info == null) 
            {
                if (string.IsNullOrEmpty(version))
                {
                    data = await NuGetPackageFetcher.GetLatestPackageMetaDataAsync(feedEndpoint, id);
                }
                else
                {
                    data = await NuGetPackageFetcher.GetPackageMetaDataAsync(feedEndpoint, id, version);
                }

                if (data != null)
                {
                    info = new SiteExtensionInfo(data);
                }
            }

            return info;
        }

        private static async Task<SiteExtensionInfo> GetPackageByIdentity(string packageId, string version = null)
        {
            string address = null;
            try
            {
                JObject json = null;
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    address = $"https://azuresearch-usnc.nuget.org/query?q=tags:AzureSiteExtension%20packageid:{packageId}&prerelease=true&semVerLevel=2.0.0";
                    using (var response = await client.GetAsync(address))
                    {
                        response.EnsureSuccessStatusCode();
                        json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    }

                    json = (JObject)json.Value<JArray>("data").FirstOrDefault();
                    if (json == null)
                    {
                        return null;
                    }

                    json = (JObject)json.Value<JArray>("versions").FirstOrDefault(j => j.Value<string>("version") == version);
                    if (json == null)
                    {
                        return null;
                    }

                    address = json.Value<string>("@id");
                    using (var response = await client.GetAsync(address))
                    {
                        response.EnsureSuccessStatusCode();
                        json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    }

                    address = json.Value<string>("catalogEntry");
                    using (var response = await client.GetAsync(address))
                    {
                        response.EnsureSuccessStatusCode();
                        json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    }

                    return new SiteExtensionInfo(json);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    throw;
                }

                throw new InvalidOperationException($"Http request to {address} failed with {ex.Message}", ex);
            }

        }
    }
}
