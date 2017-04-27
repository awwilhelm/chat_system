using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace ConsoleApplicationClient
{
    public partial class Program
    {
        static System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        static NetworkStream serverStream = default(NetworkStream);
        string readData = null;
        public static Thread ctThread = null;
        public static bool stillAlive = true;
        public static string thisID = null;
        public static bool connectedToServer = false;
        public static bool madeConnection = false;

        static void Main(string[] args)
        {

            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            Program t = new Program();
            t.button2_Click();

            while (stillAlive)
            {
                t.button1_Click();
            }
        }

        //Sends the information that the user enters to the client
        private void button1_Click()
        {
            String s = Console.ReadLine();
            byte[] outStream = System.Text.Encoding.ASCII.GetBytes(s + "$");
            if (!(outStream.Length == 1))
            {
                try
                {
                    serverStream.Write(outStream, 0, outStream.Length);
                    serverStream.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        //Connects the user to the server.  Does not log in the user
        private void button2_Click()
        {
            readData = "Conected to Chat Server ...";
            msg();
            clientSocket.Connect("127.0.0.1", 19278);
            serverStream = clientSocket.GetStream();

            ctThread = new Thread(getMessage);
            ctThread.Start();
            ctThread.IsBackground = true;
            connectedToServer = true;
        }

        //Recieves messages from the server
        private void getMessage()
        {
            int read = 1;
            while (read > 0 && connectedToServer && clientSocket.Connected == true)
            {
                try
                {
                    serverStream = clientSocket.GetStream();

                    int buffSize = 0;
                    byte[] inStream = new byte[10025];
                    buffSize = clientSocket.ReceiveBufferSize;
                    read = serverStream.Read(inStream, 0, 10025);
                    if (read <= 0)
                    {
                        connectedToServer = false;
                        break;
                    }
                    string returndata = System.Text.Encoding.ASCII.GetString(inStream);
                    returndata = returndata.Substring(0, returndata.IndexOf("\0"));
                    readData = "" + returndata;
                    msg();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        //Prints the current readData message
        private void msg()
        {
            Console.WriteLine(readData);
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            if (ctThread.IsAlive)
            {
                ctThread.Abort();
            }
            clientSocket.Close();
            
            //args.Cancel = true;
            stillAlive = false;
        }

    }
}
