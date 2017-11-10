using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.SignTool;
using Cake.Core.IO;

namespace Build
{
    internal static class Utilities
    {
        public static void TestNetCli(Context context, string csprojPath, string framework)
        {
            context.DotNetCoreTool(csprojPath, "xunit", $"-c {context.Configuration} -nobuild -f {framework}");
        }

        public static void DeleteDirectory(this Context context, DirectoryPath path, bool verbose)
        {
            if (!context.DirectoryExists(path)) return;

            if (verbose)
            {
                context.Information($"Deleting {path}");
            }
            context.DeleteDirectory(path, new DeleteDirectorySettings
            {
                Recursive = true
            });
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
                    TimeStampUri = new Uri("http://timestamp.digicert.com")
                });
            }
        }

        public static void SetupUwpSigningCertificate(Context context)
        {
            FilePath pfxFile = context.UwpDir.CombineWithFilePath("VGAudio.Uwp_StoreKey.pfx");

            if (!context.FileExists(pfxFile))
            {
                CreateSelfSignedCertificate(pfxFile, context.AppxPublisher);
                context.Information($"Created self-signed test certificate at {pfxFile}");
            }
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
            Process.Start("powershell", "-NoProfile -EncodedCommand " + commandBase64);
        }
    }
}
