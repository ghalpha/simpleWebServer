using System;
using System.Diagnostics; // Added to use Process.Start for launching browser
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace simpleWebServer
{
    internal class Program
    {
        private static string logFilePath = "server_log.txt"; // Path to the log file
        private static int startingPort = 8080; // Starting port number
        private static object logLock = new object(); // Lock object for thread-safe logging
        private static bool debugMode = false; // Flag to control logging

        public static void Main(string[] args)
        {
            // Check if debug flag is passed as an argument
            if (args.Length > 0 && args[0] == "--debug")
            {
                debugMode = true;
                Console.WriteLine("Debug mode enabled.");
            }

            // Scan for all index.html files
            string[] indexFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "index.html", SearchOption.AllDirectories);
            string applicationExe = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (indexFiles.Length == 0)
            {
                // No index.html files found, enable directory browsing
                Console.WriteLine("No index.html files found. Enabling directory browsing.");
                ThreadPool.QueueUserWorkItem(state => StartDirectoryBrowsingListener(Directory.GetCurrentDirectory(), startingPort, applicationExe));
                PrintClickableURLAndLaunchBrowser(startingPort, Directory.GetCurrentDirectory());
            }
            else
            {
                // Create a listener for each found index.html file on a unique port
                foreach (string indexFile in indexFiles)
                {
                    int port = startingPort++;
                    // Use the root directory of the index.html file (this will include assets and en-US)
                    string rootDirectory = Path.GetDirectoryName(Path.GetDirectoryName(indexFile)); // Go up two levels to the root
                    ThreadPool.QueueUserWorkItem(state => StartListener(rootDirectory, port));

                    // Always show the clickable URL and launch the browser even if debug mode is off
                    PrintClickableURLAndLaunchBrowser(port, rootDirectory);
                }
            }

            if (debugMode)
            {
                Console.WriteLine("Press any key to stop the servers...");
            }
            Console.ReadKey(); // Keep the main thread alive
        }

        // Method to print a clickable URL in the console and launch it in the default browser
        private static void PrintClickableURLAndLaunchBrowser(int port, string rootDirectory)
        {
            string url = $"http://localhost:{port}/";
            Console.WriteLine($"Listening on {url} (click to open) for files in {rootDirectory}");

            try
            {
                // Launch the default web browser with the URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Required for default browser opening in .NET Core and .NET 5+
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open browser for {url}: {ex.Message}");
            }
        }

        // Method to start an HTTP listener for serving files from the root directory
        private static void StartListener(string rootDirectory, int port)
        {
            string prefix = $"http://localhost:{port}/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
                Log($"Server started for root directory '{rootDirectory}' on {prefix}");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    // Normalize the URL path to use backslashes for Windows file system
                    string requestedFile = request.Url.AbsolutePath.Substring(1).Replace('/', '\\'); // Replace / with \ for Windows
                    string filePath = Path.Combine(rootDirectory, requestedFile);

                    if (string.IsNullOrEmpty(requestedFile) || requestedFile == "\\")
                    {
                        // Serve index.html when no specific file is requested
                        filePath = Path.Combine(rootDirectory, "en-US", "index.html");
                    }

                    if (File.Exists(filePath))
                    {
                        // Read the file content
                        byte[] buffer = File.ReadAllBytes(filePath);

                        // Determine the content type based on file extension
                        string contentType = GetContentType(filePath);
                        response.ContentType = contentType;
                        response.ContentLength64 = buffer.Length;

                        // Write to the response stream safely
                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                        Log($"Served: {filePath} ({contentType}) on port {port}");
                    }
                    else
                    {
                        // Return 404 if the file is not found
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        byte[] buffer = Encoding.UTF8.GetBytes("<html><body><h1>404 - File Not Found</h1></body></html>");
                        response.ContentLength64 = buffer.Length;

                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                        Log($"404 Not Found: {filePath} on port {port}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error on port {port}: {ex.Message}");
            }
        }

        // Method to start directory browsing
        private static void StartDirectoryBrowsingListener(string directory, int port, string applicationExe)
        {
            string prefix = $"http://localhost:{port}/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
                Log($"Directory browsing enabled for '{directory}' on {prefix}");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    // Normalize the URL path to use backslashes for Windows file system
                    string requestedPath = request.Url.AbsolutePath.Substring(1).Replace('/', '\\'); // Replace / with \ for Windows
                    string targetDirectory = Path.Combine(directory, requestedPath);

                    // If target is a directory, list the contents
                    if (Directory.Exists(targetDirectory))
                    {
                        StringBuilder responseString = new StringBuilder("<html><body><h1>Directory Listing</h1><ul>");
                        string[] entries = Directory.GetFileSystemEntries(targetDirectory);

                        foreach (string entry in entries)
                        {
                            // Exclude the application executable from the directory listing
                            if (entry != applicationExe)
                            {
                                string name = Path.GetFileName(entry);
                                responseString.AppendFormat("<li><a href=\"{0}\">{0}</a></li>", name);
                            }
                        }

                        responseString.Append("</ul></body></html>");
                        byte[] buffer = Encoding.UTF8.GetBytes(responseString.ToString());
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/html";

                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                        Log($"Directory listing served for '{targetDirectory}'");
                    }
                    else if (File.Exists(targetDirectory))
                    {
                        // Serve the requested file if it exists
                        byte[] buffer = File.ReadAllBytes(targetDirectory);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = GetContentType(targetDirectory);

                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                        Log($"Served: {targetDirectory}");
                    }
                    else
                    {
                        // Return 404 if not found
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        byte[] buffer = Encoding.UTF8.GetBytes("<html><body><h1>404 - File Not Found</h1></body></html>");
                        response.ContentLength64 = buffer.Length;

                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }

                        Log($"404 Not Found: {targetDirectory}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error on port {port}: {ex.Message}");
            }
        }

        // Thread-safe method to log messages to a text file
        private static void Log(string message)
        {
            if (debugMode)
            {
                lock (logLock)
                {
                    string logMessage = $"{DateTime.Now}: {message}";
                    Console.WriteLine(logMessage);
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
            }
        }

        // Simple method to determine content type based on file extension
        private static string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".html": return "text/html";
                case ".css": return "text/css";
                case ".js": return "application/javascript";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".ico": return "image/x-icon";
                default: return "application/octet-stream"; // Default binary stream
            }
        }
    }
}
