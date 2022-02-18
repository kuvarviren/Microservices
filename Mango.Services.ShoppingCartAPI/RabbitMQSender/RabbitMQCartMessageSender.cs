using System.Text;
using Mango.MessageBus;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Mango.Services.ShoppingCartAPI.RabbitMQSender
{
    public class RabbitMQCartMessageSender : IRabbitMQCartMessageSender
    {
        private readonly string _username;
        private readonly string _hostname;
        private readonly string _password;
        private IConnection _connection;
        public RabbitMQCartMessageSender()
        {
            _username = "guest";
            _hostname = "localhost";
            _password = "guest";
        }
        public void SendMessage(BaseMessage message, string queueName)
        {
            if (ConnectionExists())
            {
                //Create a channel
                using var channel = _connection.CreateModel();
                //define the Queue
                channel.QueueDeclare(queueName, false, false, false, null);
                //publish the message to standard queue
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "", queueName, false, null, body);
            }
        }
        private void CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                UserName = _username,
                Password = _password,
                HostName = _hostname,
            };
            //Create a Connection (This will establish a connection with the RabbitMQ)
            var _connection = factory.CreateConnection();
        }
        private bool ConnectionExists()
        {
            if (_connection != null)
            {
                return true;
            }
            CreateConnection();
            return (_connection != null);
        }
    }
}
