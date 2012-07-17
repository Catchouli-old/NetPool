using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;

namespace Pool
{
    public class Networking
    {
        public Game game;
        NetworkMode networkMode;
        public TCPServer server;
        public TCPClient client;
        public ConnectionExitedDelegate ced;
        public bool running = true;

        public Networking(Game game, ConnectionExitedDelegate ced, IPAddress ipAddress, int port, bool server)
        {
            this.game = game;
            this.ced = ced;
            this.ipAddress = ipAddress;
            if (server)
            {
                this.networkMode = NetworkMode.SERVER;
                this.server = new TCPServer(this, ipAddress, port);
            }
            else
            {
                this.networkMode = NetworkMode.CLIENT;
                this.client = new TCPClient(this, ced, ipAddress, port);
            }
        }

        private IPAddress _ipAddress;
        public IPAddress ipAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; }
        }

        public bool Connected
        {
            get
            {
                switch (networkMode)
                {
                    case NetworkMode.SERVER:
                        return this.server.Connected;
                    default:
                        return this.client.Connected;
                }
            }
            set { }
        }

        /*
         * SendHandshake
         * PAcket ID: 0
         */
        public bool SendHandShake()
        {
            byte[] packet = { 0 };
            return SendPacket(packet);
        }

        /*
         * SendVelocity
         * Packet ID: 1
         */
        public void SendVelocity(double speed, double direction)
        {
            // Create byte array for packet data
            byte[] packet = new byte[17];
            // Pack byte array with the packet ID (0), the speed, and the direction
            packet[0] = 1;
            Array.Copy(BitConverter.GetBytes(speed), 0, packet, 1, 8);
            Array.Copy(BitConverter.GetBytes(direction), 0, packet, 9, 8);
            // Send packet
            SendPacket(packet);
        }

        /*
         * RequestSync
         * Packet ID: 2
         */
        public void RequestSync()
        {
            byte[] packet = { 2 };
            SendPacket(packet);
        }

        /*
         * SyncBalls
         * Packet ID: 3
         */
        public void SendSync(List<Ball> balls)
        {
            // Create a byte array to hold a byte for the packet ID (2), and 2 doubles for each ball containing the position
            byte[] packet = new byte[1 + 16 * balls.Count];
            // Set the packet id
            packet[0] = 3;
            
            // Loop through balls
            // the position of the X coordinate for a ball is 1 + i * 8 and for the Y coordinate is 9 + i * 8
            for (int i = 0; i < balls.Count; i++)
            {
                Console.WriteLine("Syncing ball (" + balls[i].Position.X + ", " + balls[i].Position.Y + ")");
                // Pack positions in array
                Array.Copy(BitConverter.GetBytes((double)balls[i].Position.X), 0, packet, 1 + (i * 16), 8);
                Array.Copy(BitConverter.GetBytes((double)balls[i].Position.Y), 0, packet, 9 + (i * 16), 8);
                Console.WriteLine(balls[i].Position.X.ToString() + ", " + balls[i].Position.Y.ToString());

                Console.WriteLine("Synced ball (" + BitConverter.ToDouble(packet, 1 + i * 16) + ", " + BitConverter.ToDouble(packet, 9 + (i * 16)) + ")");
            }
            SendPacket(packet);            
        }

        /*
         * Also SyncBalls, handling code
         */
        public void SyncBalls(double[] positions)
        {
            if (positions.Length / 2 == ((Pool)this.game).balls.Count)
            {
                for (int i = 0; i < ((Pool)this.game).balls.Count; i++)
                {
                    Console.WriteLine("Synced ball (client) (" + positions[2 * i] + ", " + positions[2 * i + 1] + ")");
                    ((Pool)this.game).balls[i].Position = new Vector2((float)positions[2 * i], (float)positions[2 * i + 1]);
                }
            }
            else
            {
                // Really messed up
                // Uh oh
                // What should we do here? TODO
            }
        }

        /*
         * Sends cue rotation to client
         * Packet ID: 5
         */
        public void SendRotation(double rotation)
        {
            byte[] packet = new byte[29];
            packet[0] = 5;
            Array.Copy(BitConverter.GetBytes(rotation), 0, packet, 1, 8);
            Array.Copy(BitConverter.GetBytes((double)((Pool)this.game)._cuePosition.X), 0, packet, 9, 8);
            Array.Copy(BitConverter.GetBytes((double)((Pool)this.game)._cuePosition.Y), 0, packet, 17, 8);
            Array.Copy(BitConverter.GetBytes(((Pool)this.game)._cueDistance), 0, packet, 25, 4);
            SendPacket(packet);
        }

        /*
         * My bad! I potted the cue ball.
         * Packet ID: 6
         */
        public void SendMyBad()
        {
            byte[] packet = { 6 };
            SendPacket(packet);
        }

        /*
         * Syncs player ID from host
         * Packet ID: 8
         */
        public void PlayerSync(PlayerIndex player)
        {
            byte[] packet = { 8, 0, 0, 0, 0 };
            // Pack ID of current player (where host = 1, client = 2)
            Array.Copy(BitConverter.GetBytes(player.GetHashCode()), 0, packet, 1, 4);
        }

        public bool SendPacket(byte[] packet)
        {
            if (this.networkMode == NetworkMode.SERVER)
            {
                if (server != null && server.Connected)
                {
                    server.SendData(packet);
                    return true;
                }
            }
            else
            {
                if (client != null && client.Connected)
                {
                    client.SendData(packet);
                    return true;
                }
            }
            return false;
        }

        public void CommunicationLoop(object client)
        {
            Console.WriteLine("Socket opened");

            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            double cueBallSpeed = 0;
            double cueBallDirection = 0;

            while (true)
            {
                if (!running)
                    break;
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    this.ced(ConnectionError.CONNECTION_EXITED);
                    break;
                }

                ASCIIEncoding encoder = new ASCIIEncoding();

                byte command = message[0];
                switch (command)
                {
                    //case 0:
                    //    /*
                    //     * Handshake packet, must be received before game starts
                    //     */
                    //    if (((Pool)this.game)._gameState != GameState.GAMEPLAY)
                    //    {
                    //        ((Pool)this.game)._gameState = GameState.GAMEPLAY;
                    //    }
                    //    if (this.networkMode == NetworkMode.CLIENT)
                    //    {
                    //        ((Pool)this.game).sentHandShake = SendHandShake();
                    //    }
                    //    ((Pool)this.game).receivedHandShake = true;
                    //    break;
                    case 1:
                        /*
                         * Receive new velocity from client
                         * Packet format 00 XX XX XX XX XX XX XX XX YY YY YY YY YY YY YY YY
                         * XX XX XX XX XX XX XX XX is speed converted from double to bytes
                         * YY YY YY YY YY YY YY YY is direction converted from double to bytes
                         */
                        Console.WriteLine("Receiving cue ball velocity");
                        cueBallSpeed = BitConverter.ToDouble(message, 1);
                        cueBallDirection = BitConverter.ToDouble(message, 9);
                        Console.WriteLine("Speed: " + cueBallSpeed.ToString() + ", direction: " + cueBallDirection.ToString());
                        Console.WriteLine("Calling MakeShot");
                        ((Pool)this.game).MakeShot(cueBallSpeed, cueBallDirection, true);
                        break;
                    case 2:
                        /*
                         * Send ball positions to client to sync
                         */
                        SendSync(((Pool)this.game).balls);
                        break;
                    case 3:
                        double[] positions = new double[(bytesRead - 1) / 8];
                        Console.WriteLine("bytes read:" + bytesRead);
                        for (int i = 0; i < positions.Length; i++)
                        {
                            Console.WriteLine("Receiving " + BitConverter.ToDouble(message, 8 * i + 1) + "");
                            positions[i] = BitConverter.ToDouble(message, 8 * i + 1);
                        }
                        SyncBalls(positions);
                        break;
                    case 5:
                        double rotation = BitConverter.ToDouble(message, 1);
                        double cueX = BitConverter.ToDouble(message, 9);
                        double cueY = BitConverter.ToDouble(message, 17);
                        int cueDistance = BitConverter.ToInt32(message, 25);
                        ((Pool)this.game)._cueRotation = rotation;
                        ((Pool)this.game)._cuePosition.X = (float)cueX;
                        ((Pool)this.game)._cuePosition.Y = (float)cueY;
                        ((Pool)this.game)._cueDistance = cueDistance;
                        break;
                    case 6:
                        /*
                         * If the other client thinks they potted the cue ball, we can reposition it.
                         */
                        break;
                    case 8:
                        int playerIndex = BitConverter.ToInt32(message, 1);
                        switch (playerIndex)
                        {
                            case 1:
                                ((Pool)this.game)._currentPlayer = PlayerIndex.Two;
                                break;
                            default:
                                ((Pool)this.game)._currentPlayer = PlayerIndex.One;
                                break;
                        }
                        Console.WriteLine("Current turn: " + playerIndex.ToString());
                        break;
                    default:
                        Console.WriteLine("Unknown message: " + message[0]);
                        break;
                }
            }
            tcpClient.Close();
            this.ced(ConnectionError.CONNECTION_EXITED);
        }

        public void Close()
        {
            if (server != null)
                server.Close();
            if (client != null)
                client.Close();

            running = false;

            Console.WriteLine("Connection closed");
        }
    }

    public class TCPServer
    {
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private Thread listenThread;
        Networking parent;
        Thread clientThread;

        public TCPServer(Networking parent, IPAddress listenAddress, int port)
        {
            this.parent = parent;
            Console.WriteLine("TCP Server started");
            this.tcpListener = new TcpListener(listenAddress, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClient));
            this.listenThread.Start();
        }

        private void ListenForClient()
        {
            Console.WriteLine("Listening socket opened");
            try
            {
                this.tcpListener.Start();
            }
            catch (SocketException e)
            {
                e.ToString();
                parent.ced(ConnectionError.LISTENING_SOCKET_IN_USE);
                return;
            }

            while (true)
            {
                if (!parent.running)
                    break;

                this.tcpClient = this.tcpListener.AcceptTcpClient();

                clientThread = new Thread(new ParameterizedThreadStart(parent.CommunicationLoop));
                clientThread.Start(this.tcpClient);
                SendData(new ASCIIEncoding().GetBytes("Welcome to extreme pool, user"));
            }
        }

        //private void ClientCommunication(object client)
        //{
        //    Console.WriteLine("Client socket opened");

        //    TcpClient tcpClient = (TcpClient)client;
        //    NetworkStream clientStream = tcpClient.GetStream();

        //    byte[] message = new byte[4096];
        //    int bytesRead;

        //    double cueBallSpeed = 0;
        //    double cueBallDirection = 0;
            
        //    while (true)
        //    {
        //        bytesRead = 0;

        //        try
        //        {
        //            bytesRead = clientStream.Read(message, 0, 4096);
        //        }
        //        catch
        //        {
        //            break;
        //        }

        //        if (bytesRead == 0)
        //        {
        //            parent.ced(ConnectionError.CONNECTION_EXITED);
        //            break;
        //        }

        //        ASCIIEncoding encoder = new ASCIIEncoding();

        //        byte command = message[0];
        //        switch (command)
        //        {
        //            case 0:
        //                /*
        //                 * Receive new velocity from client
        //                 * Packet format 00 XX XX XX XX XX XX XX XX YY YY YY YY YY YY YY YY
        //                 * XX XX XX XX XX XX XX XX is speed converted from double to bytes
        //                 * YY YY YY YY YY YY YY YY is direction converted from double to bytes
        //                 */
        //                Console.WriteLine("Receiving cue ball velocity");
        //                cueBallSpeed = BitConverter.ToDouble(message, 1);
        //                cueBallDirection = BitConverter.ToDouble(message, 9);
        //                ((Pool)parent.game).MakeShot(cueBallSpeed, cueBallDirection);
        //                Console.WriteLine("Speed: " + cueBallSpeed.ToString() + ", direction: " + cueBallDirection.ToString());
        //                break;
        //            case 10:
        //                break;
        //            default:
        //                Console.WriteLine("Unknown message: " + message[0]);
        //                break;
        //        }
        //    }
        //    tcpClient.Close();
        //    parent.ced(ConnectionError.CONNECTION_EXITED);
        //}

        public void SendData(byte[] buffer)
        {
            if (tcpClient.Connected)
            {
                NetworkStream clientStream = tcpClient.GetStream();
                ASCIIEncoding encoder = new ASCIIEncoding();

                Console.WriteLine("Sending " + buffer.Length + " bytes");

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
        }

        public bool Connected
        {
            get { return (tcpClient == null ? false : tcpClient.Connected); }
            set { }
        }

        public void Close()
        {
            if (tcpClient != null)
                tcpClient.Close();
            if (clientThread != null)
                clientThread.Abort();
            if (listenThread != null)
                listenThread.Abort();
        }
    }

    public class TCPClient
    {
        Networking parent;
        TcpClient tcpClient;
        IPEndPoint serverEndpoint;
        ConnectionExitedDelegate ced;
        Thread clientThread;

        public TCPClient(Networking parent, ConnectionExitedDelegate ced, IPAddress serverAddress, int port)
        {
            this.ced = ced;
            Console.WriteLine("Creating TCP client");
            this.parent = parent;
            this.serverEndpoint = new IPEndPoint(serverAddress, 7167);
            tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(serverEndpoint);
            }
            catch (SocketException e)
            {
                e.ToString();
                ced(ConnectionError.REJECTED_BY_HOST);
                this.Close();
            }

            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                {
                    // Send handshake
                    clientThread = new Thread(new ParameterizedThreadStart(parent.CommunicationLoop));
                    clientThread.Start(tcpClient);
                }
            }
            else
            {
                ced(ConnectionError.ERROR_UNKNOWN);
                this.Close();
            }
        }

        public void WaitForHandShake(object client)
        {

        }

        //private void ServerCommunication(object client)
        //{
        //    Console.WriteLine("Server socket opened");

        //    TcpClient tcpClient = (TcpClient)client;
        //    NetworkStream serverStream = tcpClient.GetStream();

        //    byte[] message = new byte[4096];
        //    int bytesRead;

        //    while (true)
        //    {
        //        bytesRead = 0;

        //        try
        //        {
        //            bytesRead = serverStream.Read(message, 0, 4096);
        //        }
        //        catch
        //        {
        //            break;
        //        }

        //        if (bytesRead == 0)
        //        {
        //            break;
        //        }

        //        ASCIIEncoding encoder = new ASCIIEncoding();
        //        Console.WriteLine(encoder.GetString(message, 0, bytesRead));
        //    }
        //    tcpClient.Close();
        //    parent.ced(ConnectionError.CONNECTION_EXITED);
        //}

        public void SendData(byte[] buffer)
        {
            if (tcpClient.Connected)
            {
                NetworkStream clientStream = tcpClient.GetStream();

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
        }

        public bool Connected
        {
            get { return (tcpClient == null ? false : tcpClient.Connected); }
            set { }
        }

        public void Close()
        {
            if (tcpClient != null)
                tcpClient.Close();
            if (clientThread != null)
                clientThread.Abort();
        }
    }

    enum NetworkMode
    {
        SERVER,
        CLIENT
    }

    public enum ConnectionError
    {
        LISTENING_SOCKET_IN_USE,
        CONNECTION_EXITED,
        REJECTED_BY_HOST,
        ERROR_UNKNOWN
    }
}
