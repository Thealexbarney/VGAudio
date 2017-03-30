using Cake.Frosting;

namespace Build
{
    public class StartBuild : IFrostingStartup
    {
        public static int Main(string[] args)
        {
            // Create the host.
            ICakeHost host = new CakeHostBuilder()
                .WithArguments(args)
                .UseStartup<StartBuild>()
                .Build();

            // Run the host.
            return host.Run();
        }

        public void Configure(ICakeServices services)
        {
            services.UseContext<Context>();
            services.UseLifetime<Lifetime>();
            services.UseWorkingDirectory("..");
        }
    }
}