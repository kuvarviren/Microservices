using System.Text;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQCheckoutConsumer : BackgroundService
    {
        private readonly OrderRepository _orderRepository;
        private readonly IMessageBus _msg;
        private IConnection _connection;
        private IModel _channel;
        public RabbitMQCheckoutConsumer(OrderRepository orderRepository, IMessageBus msg)
        {
            _orderRepository = orderRepository;
            _msg = msg;
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
            _channel.QueueDeclare("checkoutqueue", false, false, false, null);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, e) =>
            {
                var content = Encoding.UTF8.GetString(e.Body.ToArray());
                CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(content);
                HandleMessage(checkoutHeaderDto).GetAwaiter().GetResult();
                //send the acknowlegment to delete the message
                _channel.BasicAck(e.DeliveryTag,false);
            };
            //consume the message
            _channel.BasicConsume("checkoutqueue", false, consumer);
            return Task.CompletedTask;

        }

        private async Task HandleMessage(CheckoutHeaderDto checkoutHeaderDto)
        {
            OrderHeader orderHeader = new()
            {
                UserId = checkoutHeaderDto.UserId,
                FirstName = checkoutHeaderDto.FirstName,
                LastName = checkoutHeaderDto.LastName,
                OrderDetails = new List<OrderDetails>(),
                CardNumber = checkoutHeaderDto.CardNumber,
                CouponCode = checkoutHeaderDto.CouponCode,
                CVV = checkoutHeaderDto.CVV,
                DiscountTotal = checkoutHeaderDto.DiscountTotal,
                Email = checkoutHeaderDto.Email,
                ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
                OrderDateTime = DateTime.Now,
                OrderTotal = checkoutHeaderDto.OrderTotal,
                PaymentStatus = false,
                Phone = checkoutHeaderDto.Phone,
                PickupDateTime = checkoutHeaderDto.PickupDateTime
            };
            foreach (var detailList in checkoutHeaderDto.CartDetails)
            {
                OrderDetails orderDetails = new()
                {
                    ProductId = detailList.ProductId,
                    ProductName = detailList.Product.Name,
                    Price = detailList.Product.Price,
                    Count = detailList.Count
                };
                orderHeader.CartTotalItems += detailList.Count;
                orderHeader.OrderDetails.Add(orderDetails);
            }

            await _orderRepository.AddOrder(orderHeader);

            //Once order is processed post a message in service bus for payment processing

            PaymentRequestMessage paymentRequestMessage = new()
            {
                OrderId = orderHeader.OrderHeaderId,
                Name = orderHeader.FirstName + ' ' + orderHeader.LastName,
                ExpiryMonthYear = orderHeader.ExpiryMonthYear,
                CVV = orderHeader.CVV,
                OrderTotal = orderHeader.OrderTotal,
                CardNumber = orderHeader.CardNumber,
                Email = orderHeader.Email
            };
            try
            {
                //post the message to the service bus topic 'orderpaymentprocesstopic'
                //await _msg.PublishMessage(paymentRequestMessage, orderPaymentProcessorTopic);
                //delete the order message from the service
                //await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

       
    }
}
