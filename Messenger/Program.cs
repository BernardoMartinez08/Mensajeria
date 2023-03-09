
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

int option = 0;
while (option != 3)
{
    Console.WriteLine("\n\nMESSENGER.\n");

    Messenger messenger = new Messenger();

    Console.Write("\n1. Enviar un Mensaje.\n2. Ver Mensajes Recibidos.\n3. Salir.\nQue quieres hacer: ");
    var text = Console.ReadLine();

    if (text != null)
    {
        option = int.Parse(text);
    }

    switch (option)
    {
        case 1:
            Console.WriteLine("\nEscribe tu mensaje a continuacion:");
            Console.WriteLine("Nota: al finalizar el mensaje escribe send en la ultima linea\n\n");
            var message = "";
            var line = "";

            while (line != "send")
            {
                line = Console.ReadLine();

                if (line != "send")
                    message += "\n" + line;
            }

            messenger.send_mesage(message);

            break;

        case 2:
            await messenger.read_message();
            break;

        case 3:
            Console.WriteLine("CERRANDO EL CHAT");
            break;

        default:
            Console.WriteLine("OPCION NO VALIDA");
            break;
    }
}


public class MessageDataTransferObjet
{
    public Guid id { get; set; }
    public string message { get; set; }
    public DateTime date { get; set; }
}


public class Messenger
{
    private IConnection connection;
    private IModel channel_messenger;
    private IModel channel_whatsapp;
    private EventingBasicConsumer consumer;

    public Messenger()
    {
        bool connected = conect_chatservice();
        var mensaje_conection = connected == true ? "\nCHAT CONNECTED\n" : "\nCONNECTION ERROR\n";
        Console.WriteLine(mensaje_conection);
    }

    public bool conect_chatservice()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672
            };

            connection = factory.CreateConnection();
            channel_messenger = connection.CreateModel();
            channel_whatsapp = connection.CreateModel();
            channel_whatsapp.QueueDeclare("messages_queue_whatsapp", false, false, false, null);
            channel_messenger.QueueDeclare("messages_queue_messenger", false, false, false, null);
            consumer = new EventingBasicConsumer(channel_messenger);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR ERROR WHILE TRYING TO CONNECT TO SERVER:\n\n" + ex + "\n\n");
            return false;
        }
    }

    public bool send_mesage(string message)
    {
        try
        {
            var _message = new MessageDataTransferObjet
            {
                id = Guid.NewGuid(),
                message = message,
                date = DateTime.Now,
            };

            var message_jason = JsonConvert.SerializeObject(_message);
            var encoded_message = Encoding.UTF8.GetBytes(message_jason);

            channel_whatsapp.BasicPublish(string.Empty, "messages_queue_whatsapp", null, encoded_message);
            Console.WriteLine("\nMESSAGE SENDED.\n\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR WHILE TRYING TO SEND A MESSAGE\n\n: " + ex + "\n\n");
            return false;
        }
    }

    public Task read_message()
    {
        try
        {
            bool recibed = false;
            consumer.Received += async (model, content) =>
            {
                var encoded_message = content.Body.ToArray();
                var message_jason = Encoding.UTF8.GetString(encoded_message);
                var message = JsonConvert.DeserializeObject<MessageDataTransferObjet>(message_jason);
                Console.WriteLine($"\n\nMESSAGE RESIVED at: {message.date}.\n {message.message}");
                recibed = true;
            };

            if(!recibed)
                Console.WriteLine($"\n\nNO MESSEGES TO READ.\n");

            channel_messenger.BasicConsume("messages_queue_messenger", true, consumer);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR WHILE TRYING TO READ A MESSAGE\n\n" + ex + "\n\n");
            return Task.CompletedTask;
        }
    }
}
