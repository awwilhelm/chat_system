using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading;

namespace ConsoleApplication1
{
    public class Program
    {
        public static Hashtable currentlyLoggedInHash = new Hashtable();
        public static Dictionary<string, string> userCredentials = new Dictionary<string, string>();
        public static Dictionary<string, handleClinet> currentLoggedInUsers = new Dictionary<string, handleClinet>();
        static TcpListener serverSocket;
        static void Main(string[] args)
        {
            initializeUserCrediential();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 8888);
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            serverSocket.Start();
            Console.WriteLine("Chat Server Started ....");
            counter = 0;
            while ((true))
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();

                //byte[] bytesFrom = new byte[10025];
                //string dataFromClient = null;
                NetworkStream networkStream = clientSocket.GetStream();
                
                //networkStream.Read(bytesFrom, 0, 10025);
                //dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                //dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

                currentlyLoggedInHash.Add(""+counter, clientSocket);
                
                handleClinet client = new handleClinet();
                client.startClient(clientSocket, ""+counter, currentlyLoggedInHash);
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine("exit");
            Console.ReadLine();
        }

        static void initializeUserCrediential()
        {
            userCredentials.Add("1", "1");
            userCredentials.Add("2", "2");
            userCredentials.Add("3", "3");
            userCredentials.Add("admin", "admin");
            userCredentials.Add("lastTest", "jk");
        }

        public static void broadcast(string msg, string uName, bool flag, string clNo)
        {
            if(msg.Length == 0)
            {
                return;
            }
            //foreach (DictionaryEntry Item in currentlyLoggedInHash)
            //{
            //    broadcastToUser(msg, uName, flag, (TcpClient) Item.Value);
            //}
            foreach (KeyValuePair<string, handleClinet> Item in currentLoggedInUsers)
            {
                if (clNo == null)
                {
                    broadcastToUser(msg, uName, flag, Item.Value.clientSocket, false);
                }
                else
                {
                    if (clNo != Item.Key)
                    {
                        broadcastToUser(msg, uName, flag, Item.Value.clientSocket, false);
                    }
                }
            }

        }  //end broadcast function

        public static void broadcastToUser(string msg, string uName, bool flag, TcpClient tcpClient, bool priv)
        {
            TcpClient broadcastSocket;
            broadcastSocket = tcpClient;
            NetworkStream broadcastStream = broadcastSocket.GetStream();
            Byte[] broadcastBytes = null;

            if (flag == true)
            {
                if (priv == true)
                {
                    broadcastBytes = Encoding.ASCII.GetBytes(uName + " says (private) : " + msg);
                }
                else
                {
                    broadcastBytes = Encoding.ASCII.GetBytes(uName + " says : " + msg);
                }
            }
            else
            {
                broadcastBytes = Encoding.ASCII.GetBytes(msg);
            }

            broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
            broadcastStream.Flush();
        }

        static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            
            serverSocket.Stop();
            // Set the Cancel property to true to prevent the process from terminating.
            //Console.WriteLine("Setting the Cancel property to true...");

            //args.Cancel = true;
            //stillAlive = false;

            // Announce the new value of the Cancel property.
            //Console.WriteLine("  Cancel property: {0}", args.Cancel);
            //Console.WriteLine("The read operation will resume...\n");
        }
    }//end Main class


    public class handleClinet
    {
        public TcpClient clientSocket;
        public string clNo;
        public string id;
        Hashtable clientsList;
        public Thread ctThread;
        public bool killThisThread = false;

        public void startClient(TcpClient inClientSocket, string id, Hashtable cList)
        {
            this.clientSocket = inClientSocket;
            this.id = id;
            this.clientsList = cList;
            ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private void doChat()
        {
            int requestCount = 0;
            byte[] bytesFrom = new byte[10025];
            string dataFromClient = null;
            Byte[] sendBytes = null;
            string serverResponse = null;
            string rCount = null;
            requestCount = 0;
            int read = 1;
            while (read > 0)
            {
                try
                {
                    NetworkStream networkStream;
                    requestCount = requestCount + 1;
                    if (clientSocket.Connected == true)
                    {
                        networkStream = clientSocket.GetStream();
                        read = networkStream.Read(bytesFrom, 0, 10025);
                        if(read <= 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        //ctThread.Abort();
                        break;
                    }
                    dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                    dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                    
                    userCommands(dataFromClient.Split(' '));

                    //rCount = Convert.ToString(requestCount);

                    //Program.broadcast(dataFromClient, clNo, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }//end while
            Program.broadcast(clNo + " Left chat room ", clNo, false, null);
            Program.currentlyLoggedInHash.Remove(id);
        }//end doChat

        bool validLogin(string user, string pass)
        {
            if(Program.userCredentials.ContainsKey(user))
            {
                if(Program.userCredentials[user].Equals(pass))
                {
                    return true;
                }
            }
            return false;
        }

        void userCommands(string[] input)
        {
            if (input.Length > 1)
            {
                if (input.Length >= 3)
                {
                    if (input[0].Equals("login"))
                    {
                        if (validLogin(input[1], input[2]))
                        {
                            //TODO add to list of valid users that can push
                            Program.currentLoggedInUsers.Add(input[1], this);
                            clNo = input[1];
                            Program.broadcastToUser("login confirmed", clNo, false, clientSocket, true);
                            Console.WriteLine(clNo + " login.");
                        } else
                        {
                            Program.broadcastToUser("You entered the incorrect credentials", clNo, false, clientSocket, false);
                        }
                    }
                    else if (input[0].Equals("send") && input[1].Equals("all"))
                    {
                        //TODO checks to see if logged in and if so broadcast
                        if (clNo == null)
                        {
                            Program.broadcastToUser("Denied. Please login first.", clNo, false, clientSocket, false);
                        }
                        else
                        {
                            string[] dataArray = input.Where((x, index) => index > 1).ToArray();
                            string data = string.Join(" ", dataArray);
                            foreach (string name in Program.currentLoggedInUsers.Keys)
                            {
                                if (clNo.Equals(name))
                                {
                                    Program.broadcast(data, clNo, true, clNo);
                                    Console.WriteLine(clNo + ": " + data);
                                }
                            }
                        }
                    }
                    else if (input[0].Equals("send"))
                    {
                        if (clNo == null)
                        {
                            Program.broadcastToUser("Denied. Please login first.", clNo, false, clientSocket, false);
                        }
                        else
                        {
                            if (Program.currentLoggedInUsers.ContainsKey(input[1]))
                            {
                                //TODO make a broadcast that sends to 1 person
                                string[] dataArray = input.Where((x, index) => index > 1).ToArray();
                                string data = string.Join(" ", dataArray);
                                Program.broadcastToUser(data, clNo, true, Program.currentLoggedInUsers[input[1]].clientSocket, true);
                                Console.WriteLine(clNo + " (" + input[1] + "): " + data);
                            }
                            else
                            {
                                Program.broadcastToUser("This user doesn't exist or isn't online", clNo, false, clientSocket, false);
                            }
                        }
                    }
                }
            }
            else if (input.Length == 1)
            {
                if (clNo == null)
                {
                    Program.broadcastToUser("Denied. Please login first.", clNo, false, clientSocket, false);
                }
                else
                {
                    if (Program.currentLoggedInUsers.ContainsKey(clNo))
                    {
                        if (input[0].Equals("who"))
                        {
                            //TODO print out all users that are logged in
                            Program.broadcastToUser(string.Join(", ", Program.currentLoggedInUsers.Keys), clNo, false, clientSocket, false);
                        }
                        else if (input[0].Equals("logout"))
                        {
                            //TODO stops thread and disconnects connections
                            if (Program.currentLoggedInUsers.ContainsKey(clNo))
                            {
                                Program.currentLoggedInUsers.Remove(clNo);
                                clNo = null;
                            }
                        }
                    }
                    else
                    {
                        Program.broadcastToUser("You need to be logged in", clNo, false, clientSocket, true);
                    }
                }
            }
        }

    } //end class handleClinet

}
