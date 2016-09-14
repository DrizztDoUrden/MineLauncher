using System;
using System.Configuration;
using Server.Core;
using Topshelf;

namespace Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            var serviceUri = ConfigurationManager.AppSettings["Server:ServiceUri"];
            var serviceName = ConfigurationManager.AppSettings["Server:ServiceName"];

            var host = HostFactory.New(c =>
            {
                c.Service<WcfServiceWrapper<UpdateServer, IUpdateServer>>(s =>
                {
                    s.ConstructUsing(x => new WcfServiceWrapper<UpdateServer, IUpdateServer>(serviceName, serviceUri));
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                });
                c.RunAsLocalSystem();

                c.SetDescription($"Runs {serviceName}.");
                c.SetDisplayName(serviceName);
                c.SetServiceName(serviceName);
            });

            Console.WriteLine($"Hosting {serviceName}...");
            host.Run();
            Console.WriteLine($"Done hosting {serviceName}...");
        }
    }
}
