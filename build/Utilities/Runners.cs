using System.Security.Cryptography.X509Certificates;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Core.IO;

namespace Build.Utilities
{
    internal static class Runners
    {
        public static void BuildNetCli(Context context, string path, string framework)
        {
            context.DotNetCoreBuild(path, new DotNetCoreBuildSettings
            {
                Framework = framework,
                Configuration = context.Configuration
            });
        }

        public static void TestNetCli(Context context, string csprojPath, string framework)
        {
            context.DotNetCoreTest(csprojPath, new DotNetCoreTestSettings
            {
                Framework = framework,
                Configuration = context.Configuration
            });
        }

        public static void DeleteDirectory(Context context, DirectoryPath path)
        {
            if (context.DirectoryExists(path))
            {
                context.DeleteDirectory(path, true);
            }
        }

        public static bool CertificateExists(Context context, string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true).Count > 0;
            }
        }
    }
}
