using System.Text;
using Azure.Messaging.ServiceBus;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dtos;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;

namespace Mango.Services.OrderAPI.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string subscriptionCheckout;
        private readonly string checkoutMessageTopic;
        private readonly string orderPaymentProcessorTopic;
        private readonly string orderPaymentUpdateStatusTopic;

        //To read all the messages automatically from Service bus and create a process out of it
        private ServiceBusProcessor checkOutProcessor, orderPaymentUpdateStatusProcessor;

        private readonly OrderRepository _orderRepository;
        private readonly IConfiguration _config;
        private readonly IMessageBus _msg;

        public AzureServiceBusConsumer(OrderRepository orderRepository, IConfiguration config,
            IMessageBus msg)
        {
            _orderRepository = orderRepository;
            _config = config;
            _msg = msg;
            serviceBusConnectionString = _config.GetValue<string>("ServiceBusConnectionString");
            subscriptionCheckout = _config.GetValue<string>("SubscriptionCheckout");
            checkoutMessageTopic = _config.GetValue<string>("CheckoutMessageTopic");
            orderPaymentProcessorTopic = _config.GetValue<string>("OrderPaymentProcessorTopic");
            orderPaymentUpdateStatusTopic = _config.GetValue<string>("OrderPaymentUpdateStatusTopic");

            var client = new ServiceBusClient(serviceBusConnectionString);
            checkOutProcessor = client.CreateProcessor(checkoutMessageTopic, subscriptionCheckout);
            orderPaymentUpdateStatusProcessor = client.CreateProcessor(orderPaymentUpdateStatusTopic, subscriptionCheckout);

        }
        public async Task Start()
        {
            checkOutProcessor.ProcessMessageAsync += OnCheckoutMessageReceived;
            checkOutProcessor.ProcessErrorAsync += ErrorHandler;
            await checkOutProcessor.StartProcessingAsync();

            orderPaymentUpdateStatusProcessor.ProcessMessageAsync += OnOrderPaymentStatusReceived;
            orderPaymentUpdateStatusProcessor.ProcessErrorAsync += ErrorHandler;
            await orderPaymentUpdateStatusProcessor.StartProcessingAsync();
        }
        public async Task Stop()
        {
            await checkOutProcessor.StopProcessingAsync();
            await checkOutProcessor.DisposeAsync();

            await orderPaymentUpdateStatusProcessor.StopProcessingAsync();
            await orderPaymentUpdateStatusProcessor.DisposeAsync();
        }

        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
        private async Task OnCheckoutMessageReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(body);

            //To DO: Automapper implementation

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
                await _msg.PublishMessage(paymentRequestMessage, orderPaymentProcessorTopic);
                //delete the order message from the service
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task OnOrderPaymentStatusReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            UpdatePaymentResultMessage updatePaymentResult = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(body);
            //update payment status
            await _orderRepository.UpdateOrderPaymentStatus(updatePaymentResult.OrderId,updatePaymentResult.Status);
            await args.CompleteMessageAsync(args.Message);
        }
    }
}
