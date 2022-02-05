using Mango.Services.OrderAPI.Messaging;

namespace Mango.Services.OrderAPI
{
    public static class ApplicationBuilderExtensions
    {
        public static IAzureServiceBusConsumer ServiceBusConsumer { get; set; }
        public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
        {
            ServiceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();
            var hostApplicationLifeTime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            hostApplicationLifeTime.ApplicationStarted.Register(OnStart);
            //hostApplicationLifeTime.ApplicationStarted.Register(OnStop);
            hostApplicationLifeTime.ApplicationStopped.Register(OnStop);
            return app;
        }
        private static void OnStart()
        {
            ServiceBusConsumer.Start();
        }
        private static void OnStop()
        {
            ServiceBusConsumer.Stop();
        }

    }
}
