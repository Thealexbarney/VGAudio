using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
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

        public static bool CertificateExists(string thumbprint, bool validOnly)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly).Count > 0;
            }
        }

        public static void SignFiles(Context context, IEnumerable<FilePath> files, string thumbprint)
        {
            if (CertificateExists(thumbprint, false))
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

        public static string SetupUwpSigningCertificate(Context context)
        {
            XDocument manifest = XDocument.Load(context.UwpCsproj.FullPath);
            XNamespace ns = manifest.Root?.GetDefaultNamespace();
            string storeThumbprint = manifest.Root?.Element(ns + "PropertyGroup")?.Element(ns + "PackageCertificateThumbprint")?.Value;
            string pfxFileName = manifest.Root?.Element(ns + "PropertyGroup")?.Element(ns + "PackageCertificateKeyFile")?.Value;
            FilePath pfxFile = context.UwpDir.CombineWithFilePath(pfxFileName);

            if (CertificateExists(storeThumbprint, false))
            {
                context.Information($"Using store code signing certificate with thumbprint {storeThumbprint} in certificate store");
                return storeThumbprint;
            }

            if (CertificateExists(context.ReleaseCertThumbprint, true))
            {
                context.Information($"Using release code signing certificate with thumbprint {context.ReleaseCertThumbprint} in certificate store");
                return context.ReleaseCertThumbprint;
            }

            if (context.FileExists(pfxFile))
            {
                context.Information($"Using code signing certificate at {pfxFile}");
                return null;
            }

            CreateSelfSignedCertificate(pfxFile, Environment.GetEnvironmentVariable("username"));
            context.Information($"Created self-signed test certificate at {pfxFile}");

            return null;
        }

        public static void CreateSelfSignedCertificate(FilePath outputPath, string subject)
        {
            var command = new StringBuilder();
            command.AppendLine($"$cert = New-SelfSignedCertificate -Subject \"CN={subject}\" -Type CodeSigningCert -TextExtension @(\"2.5.29.19 ={{text}}\") -CertStoreLocation cert:\\currentuser\\my;");
            command.AppendLine("Remove-Item $cert.PSPath;");
            command.AppendLine($"Export-PfxCertificate -Cert $cert -FilePath \"{outputPath}\" -Password (New-Object System.Security.SecureString) | Out-Null;");

            RunPowershell(command.ToString());
        }

        public static void RunPowershell(string command)
        {
            byte[] commandBytes = Encoding.Unicode.GetBytes(command);
            string commandBase64 = Convert.ToBase64String(commandBytes);
            Process.Start("powershell", "-EncodedCommand " + commandBase64);
        }
    }
}
