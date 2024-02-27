using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;

namespace HTTP_server
{

    
    internal class Program
    {
        static int FileCounter = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Server starting...");

            var ipEndPoint = new IPEndPoint(IPAddress.Any, 80);
            TcpListener listener = new(ipEndPoint);
            Console.WriteLine("Endpoint built");

            while (true)
            {
                try
                {
                    listener.Start();
                    Console.WriteLine("Listener is ready to new requests");

                    // getting request
                    using TcpClient handler = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Got a new request");
                    await using NetworkStream stream = handler.GetStream();

                    // encoding and reading request
                    var buffer = new byte[4_096];
                    int received = await stream.ReadAsync(buffer);
                    string request = Encoding.UTF8.GetString(buffer, 0, received);

                    // sending response
                    Console.WriteLine("Preparing response...");
                    var response = buildResponse(request);
                    Console.WriteLine("Sending response...");
                    await stream.WriteAsync(response);
                    Console.WriteLine("Response sent");
                }
                catch
                {
                    Console.WriteLine("Stopping handling...");
                    listener.Stop();
                }
            }


        static byte[] buildResponse(string request)
        {
            string folderPath = "static/";
            string notFoundPage = "404.html";
            string indexPage = "index.html";
            string status_line = "HTTP/1.1 200 OK";
            byte[] body;
            string bodyLength;
            string requestedFilePath;
            string fileExtension = ".html";

            // trying parse request string and determine requested file
            if (request.StartsWith("GET"))
            {
                try
                {
                    int startIndex = request.IndexOf("GET");
                    int endIndex = request.IndexOf("HTTP/1.1");

                    requestedFilePath = request.Substring(startIndex + 3, endIndex - startIndex - 3).Trim().ToLower();

                    Console.WriteLine(requestedFilePath);
                            
                    if (requestedFilePath == "" || requestedFilePath == "/") { 
                        requestedFilePath = indexPage; 
                    }
                            
                    fileExtension = Path.GetExtension(requestedFilePath).ToString();
                    if (string.IsNullOrEmpty(fileExtension))
                    {
                        fileExtension = ".html"; 
                        requestedFilePath += fileExtension;
                    }

                }
                catch
                {
                    requestedFilePath = notFoundPage;
                }
            }
            else
            {
                requestedFilePath = notFoundPage;
            }

            //trying to find requested file or redirect to 404 page 
            try
            {
                body = File.ReadAllBytes(folderPath + requestedFilePath);
            }
            catch
            {
                status_line = "HTTP/1.1 404 Not Found";
                body = File.ReadAllBytes(folderPath + notFoundPage);
            }

            bodyLength = body.Length.ToString();

            string responseHead = $"{status_line}\r\n" +
                                 $"Content-Type: {setContentType(fileExtension)}\r\n" +
                                 $"Content-Length: { bodyLength}" +
                                 $"\r\n" +
                                 $"\r\n";

            byte[] responseHeadBytes = Encoding.UTF8.GetBytes(responseHead);

            byte[] responseBytes = new byte[responseHeadBytes.Length + body.Length];
            System.Buffer.BlockCopy(responseHeadBytes, 0, responseBytes, 0, responseHeadBytes.Length);
            System.Buffer.BlockCopy(body, 0, responseBytes, 0, body.Length);


            File.WriteAllBytes(folderPath + "data-" + FileCounter++, responseBytes);

            return responseBytes;
        };


        static string setContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".jpeg":
                case ".jpg":
                    return "image/jpeg";
                case ".png":
                    return "image/png"; 
                case ".gif":
                    return "image/gif"; 
                default:
                    return "text/html";
            }
        }
        }
    }
}