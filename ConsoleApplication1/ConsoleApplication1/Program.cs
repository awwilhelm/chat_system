using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading;
using System.IO;

namespace ConsoleApplication1
{
    public class Program
    {
        public static Hashtable currentlyLoggedInHash = new Hashtable();
        public static Dictionary<string, string> userCredentials = new Dictionary<string, string>();
        public static Dictionary<string, handleClinet> currentLoggedInUsers = new Dictionary<string, handleClinet>();
        static TcpListener serverSocket;
        public static int maxClients = 3;

        public static string filename = "passwordStuff.txt";
        static void Main(string[] args)
        {
            initializeUserCrediential();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 19278);
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            serverSocket.Start();
            Console.WriteLine("Chat Server Started ....");
            counter = 0;
            while ((true))
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                
                NetworkStream networkStream = clientSocket.GetStream();

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
            if (!File.Exists(filename))
            {
                string[] names = new string[] { "(Tom, Tom11)", "(David, David22)", "(Beth, Beth33)", "(John, John44)" };
                using (StreamWriter sw = new StreamWriter(filename))
                {

                    foreach (string s in names)
                    {
                        sw.WriteLine(s);
                    }
                }
            }

            // Read and show each line from the file.
            string line = "";
            using (StreamReader sr = new StreamReader(filename))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    string username, pass;
                    username = line.Substring(1, line.IndexOf(',') - 1);
                    pass = line.Substring(line.IndexOf(',') + 2, line.Length - (line.IndexOf(',')+2) - 1);
                    userCredentials.Add(username, pass);
                }
            }
        }

        public static void broadcast(string msg, string uName, bool flag, string clNo)
        {
            if(msg.Length == 0)
            {
                return;
            }
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
        }//end doChat

        bool validLogin(string user, string pass)
        {
            if (Program.currentLoggedInUsers.Count < Program.maxClients)
            {
                if (Program.userCredentials.ContainsKey(user))
                {
                    if (Program.currentLoggedInUsers.ContainsKey(user) == false)
                    {
                        if (Program.userCredentials[user].Equals(pass))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        Program.broadcastToUser("User is alread logged in.", clNo, false, clientSocket, false);
                    }
                }
            }
            else
            {
                Program.broadcastToUser("Login max of " + Program.maxClients + " has been met. Try again later.", clNo, false, clientSocket, false);
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
                            if (input[1].Equals(clNo))
                            {
                                Program.broadcastToUser("You can't send a pm to yourself.", clNo, false, clientSocket, false);
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
                    else if(input[0].Equals("newuser"))
                    {
                        if (clNo != null)
                        {
                            Program.broadcastToUser("Can't be logged in.", clNo, false, clientSocket, false);
                        }
                        else
                        {
                            if (input[1].Length < 32)
                            {
                                if (input[2].Length > 4 && input[2].Length < 8)
                                {
                                    bool taken = false;
                                    foreach(KeyValuePair<string, string> user in Program.userCredentials)
                                    {
                                        if(user.Key.Equals(input[1]))
                                        {
                                            taken = true;
                                        }
                                    }
                                    if (taken == false)
                                    {
                                        using (StreamWriter sw = new StreamWriter(Program.filename, true))
                                        {
                                            sw.WriteLine("(" + input[1] + ", " + input[2] + " )");
                                        }

                                        Program.userCredentials.Add(input[1], input[2]);
                                    }
                                    else
                                    {
                                        Program.broadcastToUser("That username was already taken.", clNo, false, clientSocket, false);
                                    }
                                }
                                else
                                {
                                    Program.broadcastToUser("Pass needs to be > 4 and < 8", clNo, false, clientSocket, false);
                                }
                            }
                            else
                            {
                                Program.broadcastToUser("Name needs to be less than 32 characters.", clNo, false, clientSocket, false);
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
                                Program.broadcast(clNo + " Left chat room.", clNo, false, null);
                                Console.WriteLine(clNo + " Left chat room.");
                                Program.currentlyLoggedInHash.Remove(id);
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
