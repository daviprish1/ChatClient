using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UserClassLibrary;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        ManualResetEvent con = new ManualResetEvent(false);
        ManualResetEvent sen = new ManualResetEvent(false);
        ManualResetEvent rec = new ManualResetEvent(false);

        string answer { get; set; }

        void StartClient(string command)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 1024);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.BeginConnect(ep, ConnectCallback, socket);
            con.WaitOne();

            byte[] request = Encoding.ASCII.GetBytes(command + "|");
            socket.BeginSend(request, 0, request.Length, 0, SendCallback, socket);
            sen.WaitOne();


            StateObject obj = new StateObject();
            obj.socket = socket;

            socket.BeginReceive(obj.buffer, 0, obj.Size, 0, ReceiveCallback, obj);
            rec.WaitOne();

            //Console.WriteLine("Data received {0}", answer);
            string cmd = answer.Substring(0, answer.IndexOf("$") + 1);
            answer = answer.Substring(answer.IndexOf("$") + 1);
            switch (cmd)
            {
                case "LoginSuccesCmd$":
                    {
                        this.UnlockChat();
                        //this.StartClient("GetUserListCmd$");
                        request = Encoding.ASCII.GetBytes("GetUserListCmd$" + "|");
                        socket.BeginSend(request, 0, request.Length, 0, SendCallback, socket);
                        sen.WaitOne();

                        StateObject objUsrList = new StateObject();
                        objUsrList.socket = socket;

                        socket.BeginReceive(objUsrList.buffer, 0, objUsrList.Size, 0, ReceiveCallback, objUsrList);
                        rec.WaitOne();
                    }
                    break;

                case "LoginFailCmd$":
                    MessageBox.Show("Wrong Pass or User!");
                    this.tbUserName.Text = "";
                    this.tbPass.Text = "";
                    break;

                case "RetUserListCmd$":
                    string[] users = answer.Split(' ');
                    this.lbUsers.DataSource = users;
                    break;
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        void ConnectCallback(IAsyncResult res)
        {
            try
            {
                Socket socket = (Socket)res.AsyncState;

                socket.EndConnect(res);
                con.Set();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        void SendCallback(IAsyncResult res)
        {
            Socket socket = (Socket)res.AsyncState;

            int count = socket.EndSend(res);
            sen.Set();
        }

        void ReceiveCallback(IAsyncResult res)
        {
            StateObject obj = (StateObject)res.AsyncState;
            Socket clientSocket = obj.socket;

            int count = clientSocket.EndReceive(res);
            if (count > 0)
            {
                obj.result.Append(Encoding.ASCII.GetString(obj.buffer, 0, count));
                clientSocket.BeginReceive(obj.buffer, 0, obj.Size, 0, ReceiveCallback, obj);

            }
            else
            {
                if (obj.result.Length > 1)
                {
                    answer = obj.result.ToString();
                }
                rec.Set();
            }
        }



        public Form1()
        {
            InitializeComponent();
        }

        private void UnlockChat()
        {
            this.lbUsers.Enabled = true;
            this.tbChat.Enabled = true;
            this.tbMessage.Enabled = true;
            this.btnSendMsg.Enabled = true;

            this.tbUserName.Enabled = false;
            this.tbPass.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.lbUsers.Enabled = false;
            this.tbChat.Enabled = false;
            this.tbMessage.Enabled = false;
            this.btnSendMsg.Enabled = false;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string command = "LogToChat$" + this.tbUserName.Text + " " + this.tbPass.Text;
            this.StartClient(command);
        }

    }

    class StateObject
    {
        public Socket socket = null;
        public const int size = 1024;
        public int Size
        {
            get { return size; }
        }
        public byte[] buffer = new byte[size];
        public StringBuilder result = new StringBuilder();
    }
}
