using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class CurrencyExchangeServer
{
    static int maxConnections = 5;
    static int currentConnections = 0;

    static readonly Dictionary<string, double> exchangeRates = new()
    {
        {"USD_EURO", 0.92},
        {"EURO_USD", 1.09},
        {"USD_GBP", 0.78},
        {"GBP_USD", 1.28},
        {"EURO_GBP", 0.85},
        {"GBP_EURO", 1.18}
    };

    static readonly Dictionary<string, (int requestCount, DateTime lastRequestTime)> clientRequests = new();
    static int maxRequests = 5;
    static TimeSpan blockTime = TimeSpan.FromMinutes(1);

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            
            if (currentConnections >= maxConnections)
            {
                Console.WriteLine("Server is at full capacity. Rejecting new connections.");
                
                Thread.Sleep(5000);
                continue;
            }

            
            TcpClient client = listener.AcceptTcpClient();
            Interlocked.Increment(ref currentConnections); 

            ThreadPool.QueueUserWorkItem(HandleClient, client);
        }
    }

    static void HandleClient(object obj)
    {
        using TcpClient client = (TcpClient)obj;
        using NetworkStream stream = client.GetStream();
        IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
        string clientAddress = endPoint.Address.ToString();
        Console.WriteLine($"Client connected: {clientAddress}:{endPoint.Port}");

        try
        {
            if (clientRequests.ContainsKey(clientAddress) &&
                clientRequests[clientAddress].requestCount >= maxRequests &&
                DateTime.Now - clientRequests[clientAddress].lastRequestTime < blockTime)
            {
                Console.WriteLine($"Client {clientAddress} exceeded request limit. Blocking for {blockTime.TotalMinutes} minutes.");
                byte[] blockMessage = Encoding.UTF8.GetBytes("Request limit exceeded. Try again later.");
                stream.Write(blockMessage, 0, blockMessage.Length);
                return;
            }

            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"Received request: {request}");

                if (!clientRequests.ContainsKey(clientAddress))
                {
                    clientRequests[clientAddress] = (0, DateTime.Now);
                }

                var clientData = clientRequests[clientAddress];
                clientRequests[clientAddress] = (clientData.requestCount + 1, DateTime.Now);

                
                string response = exchangeRates.TryGetValue(request.ToUpper(), out double rate) ? rate.ToString() : "Invalid request";

                byte[] responseData = Encoding.UTF8.GetBytes(response);
                stream.Write(responseData, 0, responseData.Length);
                Console.WriteLine($"Sent response: {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine($"Client {clientAddress} disconnected");
        Interlocked.Decrement(ref currentConnections); 
    }
}
