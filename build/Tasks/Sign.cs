using System;
using System.Linq;
using Build.Utilities;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.SignTool;
using Cake.Frosting;

namespace Build.Tasks
{
    [Dependency(typeof(BuildCli))]
    public sealed class SignCli : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            if (Runners.CertificateExists(context, context.ReleaseCertThumbprint))
            {
                context.Sign(context.CliPublishDir.CombineWithFilePath("net45/VGAudioCli.exe"), new SignToolSignSettings
                {
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertThumbprint = context.ReleaseCertThumbprint,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampUri = new Uri("http://sha256timestamp.ws.symantec.com/sha256/timestamp")
                });
            }
        }

        public override bool ShouldRun(Context context) =>
            context.LibBuilds.Values.Any(x => x.CliSuccess == true);

        public override void OnError(Exception exception, Context context) =>
           context.Information("Couldn't sign CLI assemblies");
    }
}
