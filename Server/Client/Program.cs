using System.Net.Sockets;
using System.Net;
using System.Text;

class CurrencyExchangeClient
{
    static void Main()
    {
        using Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 5000));

            while (true)
            {
                Console.Write("Enter currency pair (e.g., USD EURO) or type EXIT to quit: ");
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message) || message.ToUpper() == "EXIT")
                {
                    Console.WriteLine("Closing connection.");
                    break;
                }

                byte[] messageData = Encoding.UTF8.GetBytes(message);
                clientSocket.Send(messageData);

                byte[] buffer = new byte[1024];
                int receivedBytes = clientSocket.Receive(buffer);
                string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                Console.WriteLine($"Server response: {response}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
