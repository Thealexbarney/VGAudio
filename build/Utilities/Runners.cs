using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.SignTool;
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

        public static void DeleteDirectory(Context context, DirectoryPath path, bool verbose)
        {
            if (!context.DirectoryExists(path)) return;

            if (verbose)
            {
                context.Information($"Deleting {path}");
            }
            context.DeleteDirectory(path, true);
        }

        public static void DeleteFile(Context context, FilePath path, bool verbose)
        {
            if (!context.FileExists(path)) return;

            if (verbose)
            {
                context.Information($"Deleting {path}");
            }
            context.DeleteFile(path);
        }

        public static bool CertificateExists(Context context, string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true).Count > 0;
            }
        }

        public static void SignFiles(Context context, IEnumerable<FilePath> files, string thumbprint)
        {
            if (CertificateExists(context, thumbprint))
            {
                context.Sign(files, new SignToolSignSettings
                {
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertThumbprint = thumbprint,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampUri = new Uri("http://timestamp.digicert.com"),
                    //TODO: remove hard coded path once CAKE resolution is fixed
                    ToolPath = context.Environment.GetSpecialPath(SpecialPath.ProgramFilesX86).Combine(@"Windows Kits\10\bin\x64").CombineWithFilePath("signtool.exe")
                });
            }
        }
    }
}
