using System.Text;
using Azure.Messaging.ServiceBus;
using Mango.Services.Email.Messages;
using Mango.Services.Email.Repository;
using Newtonsoft.Json;

namespace Mango.Services.Email.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string subscriptionEmail;
        private readonly string orderPaymentUpdateStatusTopic;

        //To read all the messages automatically from Service bus and create a process out of it
        private ServiceBusProcessor orderPaymentUpdateStatusProcessor;

        private readonly EmailRepository _emailRepository;
        private readonly IConfiguration _config;

        public AzureServiceBusConsumer(EmailRepository emailRepository, IConfiguration config)
        {
            _emailRepository = emailRepository;
            _config = config;
            serviceBusConnectionString = _config.GetValue<string>("ServiceBusConnectionString");
            subscriptionEmail = _config.GetValue<string>("SubscriptionName");
            orderPaymentUpdateStatusTopic = _config.GetValue<string>("OrderPaymentUpdateStatusTopic");

            var client = new ServiceBusClient(serviceBusConnectionString);
            orderPaymentUpdateStatusProcessor = client.CreateProcessor(orderPaymentUpdateStatusTopic, subscriptionEmail);

        }
        public async Task Start()
        {
            orderPaymentUpdateStatusProcessor.ProcessMessageAsync += OnOrderPaymentStatusReceived;
            orderPaymentUpdateStatusProcessor.ProcessErrorAsync += ErrorHandler;
            await orderPaymentUpdateStatusProcessor.StartProcessingAsync();
        }
        public async Task Stop()
        {
            await orderPaymentUpdateStatusProcessor.StopProcessingAsync();
            await orderPaymentUpdateStatusProcessor.DisposeAsync();
        }

        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
        private async Task OnOrderPaymentStatusReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            UpdatePaymentResultMessage objMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(body);
            //update payment status
            await _emailRepository.SendAndLogEmail(objMessage);
            await args.CompleteMessageAsync(args.Message);
        }
    }
}
