
namespace SiteExtesionInstalltionKuduConsoleApp
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NuGet.Configuration;
    using NuGet.Protocol.Core.Types;
    using NuGet.Protocol;
    using NuGet.Common;

    public class NuGetPackageFetcher
    {
        public static async Task FetchNuGetPackageAsync(string feedEndpoint, string id, string version = null)
        {
            // Create a package source for v2 API
            var packageSource = new PackageSource(feedEndpoint);

            // Use the source repository to interact with the NuGet v2 API
            var sourceRepository = Repository.Factory.GetCoreV3(packageSource);

            // Get the metadata resource for the NuGet feed
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

            if (metadataResource == null)
            {
                Console.WriteLine("Failed to load the package metadata resource.");
                return;
            }

            if (string.IsNullOrEmpty(version))
            {
                // Fetch all versions of the package by its ID
                var packages = await metadataResource.GetMetadataAsync(id, includePrerelease: true, includeUnlisted: false, new SourceCacheContext(), NullLogger.Instance, System.Threading.CancellationToken.None);

                // Display available versions
                foreach (var package in packages)
                {
                    Console.WriteLine($"Package: {package.Identity.Id}, Version: {package.Identity.Version}");
                }
            }
            else
            {
                // Fetch a specific version of the package
                var packageIdentity = new NuGet.Packaging.Core.PackageIdentity(id, new NuGet.Versioning.NuGetVersion(version));
                var packageMetadata = await metadataResource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), NullLogger.Instance, System.Threading.CancellationToken.None);

                if (packageMetadata != null)
                {
                    Console.WriteLine($"Package found: {packageMetadata.Identity.Id}, Version: {packageMetadata.Identity.Version}");
                }
                else
                {
                    Console.WriteLine("Package not found.");
                }
            }
        }

        // Helper method to get the latest version of the package
        public static async Task<IPackageSearchMetadata> GetLatestPackageMetaDataAsync(string feedEndpoint, string id)
        {
            var packages = await GetAllPackaes(feedEndpoint, id);

            // Get the latest version (this includes pre-release versions)
            var latestPackageMetaData = packages.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
            if (latestPackageMetaData == null)
            {
                Console.WriteLine("No versions found for the specified package.");
                return null;
            }

            return latestPackageMetaData;
        }

        public static async Task<IPackageSearchMetadata> GetPackageMetaDataAsync(string feedEndpoint, string id, string version)
        {
            PackageMetadataResource packageMetadataResource = await CreateMetadataResource(feedEndpoint);
            // Fetch a specific version of the package
            var packageIdentity = new NuGet.Packaging.Core.PackageIdentity(id, new NuGet.Versioning.NuGetVersion(version));
            var packageMetadata = await packageMetadataResource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), NullLogger.Instance, System.Threading.CancellationToken.None);

            if (packageMetadata == null)
            {
                return null;
            }

            return packageMetadata;
        }

        private static async Task<IEnumerable<IPackageSearchMetadata>> GetAllPackaes(string feedEndpoint, string id)
        {
            PackageMetadataResource packageMetadataResource = await CreateMetadataResource(feedEndpoint);
            // Fetch all versions of the package by its ID
            var packages = await packageMetadataResource.GetMetadataAsync(id, includePrerelease: true, includeUnlisted: false, new SourceCacheContext(), NullLogger.Instance, System.Threading.CancellationToken.None);
            return packages;
        }

        private static async Task<PackageMetadataResource> CreateMetadataResource(string feedEndpoint)
        {
            // Create a package source for v2 API
            var packageSource = new PackageSource(feedEndpoint);

            // Use the source repository to interact with the NuGet v2 API
            var sourceRepository = Repository.Factory.GetCoreV3(packageSource);

            // Get the metadata resource for the NuGet feed
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

            if (metadataResource == null)
            {
                Console.WriteLine("Failed to load the package metadata resource.");
                return null;
            }

            return metadataResource;
        }
    }
}