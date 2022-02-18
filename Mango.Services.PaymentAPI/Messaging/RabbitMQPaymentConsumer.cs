using System.Text;
using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private readonly IProcessPayment _processPayment;
        private readonly IRabbitMQPaymentMessageSender _rabbitMQPaymentMessageSender;
        private IConnection _connection;
        private IModel _channel;
        public RabbitMQPaymentConsumer(IProcessPayment processPayment, IRabbitMQPaymentMessageSender rabbitMQPaymentMessageSender)
        {
            var factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
            };
            //Create a Connection (This will establish a connection with the RabbitMQ)
             _connection = factory.CreateConnection();
            //Create a channel
            _channel = _connection.CreateModel();
            //define the Queue
            _channel.QueueDeclare("orderpaymentqueue", false, false, false, null);
            _processPayment = processPayment;
            _rabbitMQPaymentMessageSender = rabbitMQPaymentMessageSender;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, e) =>
            {
                var content = Encoding.UTF8.GetString(e.Body.ToArray());
                PaymentRequestMessage msg = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);
                HandleMessage(msg).GetAwaiter().GetResult();
                //send the acknowlegment to delete the message
                _channel.BasicAck(e.DeliveryTag,false);
            };
            //consume the message
            _channel.BasicConsume("checkoutqueue", false, consumer);
            return Task.CompletedTask;

        }
        private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
        {
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
                //await _msg.PublishMessage(updatePaymentResultMessage, orderPaymentUpdateStatusTopic);
                //delete the order message from the service
                //await args.CompleteMessageAsync(args.Message);
                _rabbitMQPaymentMessageSender.SendMessage(updatePaymentResultMessage);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
