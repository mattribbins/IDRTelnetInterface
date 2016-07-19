using System;
using System.Configuration;
using System.Net.Sockets;

namespace IDRBasicSwitcher
{
    class Program
    {
        static int Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            bool stayConnected = true;
            Int32 port = 23;
            String server = "";
            TcpClient client;
            NetworkStream stream;
            Byte[] dataIn = new Byte[256];
            Byte[] dataOut = new Byte[256];
            String textIn = String.Empty;
            Int32 bytesIn = 0;

            if(args.Length < 2)
            {
                Console.WriteLine("At least 2 args required. Not happening. Goodbye cruel world.");
                return 1;
            }

          
            try
            {
                // Read settings
                server = appSettings["server"];
                port = Int32.Parse(appSettings["port"]);

                // Connect to server
                client = new TcpClient(server, port);
                stream = client.GetStream();

                // Loop de loop
                while (stayConnected)
                {
                    bytesIn = stream.Read(dataIn, 0, dataIn.Length);
                    textIn = System.Text.Encoding.ASCII.GetString(dataIn, 0, bytesIn);
                    textIn = textIn.Replace("\r", string.Empty);
                    textIn = textIn.Replace("\n", string.Empty);
                    textIn = textIn.Replace("\0", string.Empty);
                    Console.WriteLine("{0}", textIn);

                    // Respond based on what we receive from the IDR
                    switch (textIn)
                    {
                        case "Connected to iDR":
                            Console.WriteLine("We're seeing an IDR");
                            break;
                        case "Password: ":
                            Console.WriteLine("Authenticating...");
                            dataOut = System.Text.Encoding.ASCII.GetBytes(appSettings["password"] + "\r");
                            stream.Write(dataOut, 0, dataOut.Length);
                            break;
                        case "iDR4>":
                            // Ready to do some real stuff now!
                            Console.WriteLine("Let's go!");

                            switch (args[0].ToUpper())
                            {
                                case "GET":
                                    switch (args[1].ToUpper())
                                    {
                                        case "PRESET":
                                            Console.WriteLine("Getting current preset");
                                            dataOut = System.Text.Encoding.ASCII.GetBytes("GET PRESET\r");
                                            stream.Write(dataOut, 0, dataOut.Length);

                                            do
                                            {
                                                bytesIn = stream.Read(dataIn, 0, dataIn.Length);
                                                textIn = System.Text.Encoding.ASCII.GetString(dataIn, 0, bytesIn);
                                                textIn = textIn.Replace("\0", string.Empty);
                                            } while (String.Compare(textIn, 0, "", 0, 2) == 0);
                                            Console.WriteLine("Currently on preset {0}", textIn);

                                            stayConnected = false;
                                            break;
                                    }

                                    break;
                                case "SET":
                                    switch (args[1].ToUpper())
                                    {
                                        case "PRESET":
                                            string preset = (args.Length >= 3) ? args[2] : "1";

                                            Console.WriteLine("Setting current preset to {0}", preset);
                                            dataOut = System.Text.Encoding.ASCII.GetBytes("SET PRESET " + preset + "\r");
                                            stream.Write(dataOut, 0, dataOut.Length);

                                            // Verify our actions
                                            dataOut = System.Text.Encoding.ASCII.GetBytes("GET PRESET\r");
                                            stream.Write(dataOut, 0, dataOut.Length);

                                            do
                                            {
                                                bytesIn = stream.Read(dataIn, 0, dataIn.Length);
                                                textIn = System.Text.Encoding.ASCII.GetString(dataIn, 0, bytesIn);
                                                textIn = textIn.Replace("\0", string.Empty);
                                                textIn = textIn.Replace("iDR4>", string.Empty);
                                            } while (String.Compare(textIn, 0, "", 0, 2) == 0);


                                            // Did it work?

                                            if (string.Compare(preset, textIn) == 0)
                                            {
                                                Console.WriteLine("Successfully set");
                                            }
                                            else
                                            {
                                                Console.WriteLine("ERROR: Not set successfully.");
                                            }
                                            stayConnected = false;
                                            break;
                                    }
                                    break;
                            }

                            break;
                    }

                }

                stream.Close();
                client.Close();

            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Unable to read settings. Expect things to fail.");
                return 2;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
                return 3;
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                return 4;
            }

            return 0;

        }

    }

}
