using System.Text;
using Azure.Messaging.ServiceBus;
using Mango.MessageBus;
using Mango.Services.PaymentAPI.Messages;
using Newtonsoft.Json;
using PaymentProcessor;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string subscriptionPayment;
        private readonly string orderPaymentProcessorTopic;
        private readonly string orderPaymentUpdateStatusTopic;

        //To read all the messages automatically from Service bus and create a process out of it
        private ServiceBusProcessor orderPaymentProcessor;
        private readonly IProcessPayment _processPayment;
        private readonly IConfiguration _config;
        private readonly IMessageBus _msg;

        public AzureServiceBusConsumer(IProcessPayment processPayment,IConfiguration config,IMessageBus msg)
        {
            _processPayment = processPayment;
            _config = config;
            _msg = msg;
            serviceBusConnectionString = _config.GetValue<string>("ServiceBusConnectionString");
            subscriptionPayment = _config.GetValue<string>("OrderPaymentProcessSubscription");
            orderPaymentProcessorTopic = _config.GetValue<string>("OrderPaymentProcessorTopic");
            orderPaymentUpdateStatusTopic = _config.GetValue<string>("OrderPaymentUpdateStatusTopic");

            var client = new ServiceBusClient(serviceBusConnectionString);
            orderPaymentProcessor = client.CreateProcessor(orderPaymentProcessorTopic, subscriptionPayment);

        }
        public async Task Start()
        {
            orderPaymentProcessor.ProcessMessageAsync += OnProcessPayment;
            orderPaymentProcessor.ProcessErrorAsync += ErrorHandler;
            await orderPaymentProcessor.StartProcessingAsync();
        }
        public async Task Stop()
        {
            await orderPaymentProcessor.StopProcessingAsync();
            await orderPaymentProcessor.DisposeAsync();
        }

        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
        private async Task OnProcessPayment(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            PaymentRequestMessage paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(body);

            //To DO: Automapper implementation

            //process the payment
            var result = _processPayment.PaymentProcessor();

            //Once payment is processed post a message in service bus for upadting the order status

            UpdatePaymentResultMessage updatePaymentResultMessage = new()
            {
                OrderId = paymentRequestMessage.OrderId,
                Status = result,
                Email = paymentRequestMessage.Email
            };
            try 
            {
                //post the message to the service bus topic 'orderpaymentprocesstopic'
                await _msg.PublishMessage(updatePaymentResultMessage, orderPaymentUpdateStatusTopic);
                //delete the order message from the service
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
