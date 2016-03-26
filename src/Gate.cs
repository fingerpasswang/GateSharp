using System;
using System.Collections.Generic;
using System.IO;
using Network;

namespace Gate
{
    class Gate
    {
        private ServerNetwork frontendNetwork;
        private ServerNetwork backendNetwork;

        private readonly Broker broker = new Broker();
        private readonly ForwardManager forwardManager = new ForwardManager();

        private readonly Dictionary<Guid, IRemote> uuidToClients = new Dictionary<Guid, IRemote>();

        public static byte[] receivedBuffer = new[] { (byte)GateMessage.Received };

        public Gate(int frontPort, int backPort)
        {
            frontendNetwork = new ServerNetwork(frontPort)
            {
                OnClientConnected = OnFrontendConnected,
                OnClientDisconnected = OnFrontendDisconnected,
                OnClientMessageReceived = OnFrontendMessageReceived,
            };
            backendNetwork = new ServerNetwork(backPort)
            {
                OnClientConnected = OnBackendConnected,
                OnClientDisconnected = OnBackendDisconnected,
                OnClientMessageReceived = OnBackendMessageReceived,
            };

            frontendNetwork.BeginAccept();
            backendNetwork.BeginAccept();
        }

        public void MainLoop()
        {
            frontendNetwork.Poll();
            backendNetwork.Poll();
        }

        private void OnFrontendConnected(IRemote conn)
        {
            Console.WriteLine("OnFrontendConnected id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);


        }

        private void OnFrontendDisconnected(IRemote conn)
        {
            Console.WriteLine("OnFrontendDisconnected id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);

        }
        private void OnBackendConnected(IRemote conn)
        {
            Console.WriteLine("OnBackendConnected id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);

        }

        private void OnBackendDisconnected(IRemote conn)
        {
            Console.WriteLine("OnBackendDisconnected id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);


        }

        private const int UuidLen = 16;

        private void OnFrontendMessageReceived(IRemote conn, Message msg)
        {
            Console.WriteLine("OnFrontendMessageReceived id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);
            Console.WriteLine("OnFrontendMessageReceived msgLen:{0}", msg.Buffer.Length);

            // forward frontend msg to backend, 
            // which is based on uuid associated with the msg

            var stream = new MemoryStream(msg.Buffer);
            var br = new BinaryReader(stream);
            var msgType = (GateMessage)br.ReadByte();

            Console.WriteLine("OnFrontendMessageReceived msgType:{0}", msgType);

            switch (msgType)
            {
                case GateMessage.Send:
                    {
                        var uuid = new Guid(br.ReadBytes(UuidLen));
                        var key = br.ReadInt32();

                        Console.WriteLine("OnFrontendMessageReceived key:{0} uuid:{1}", key, uuid);
                        broker.RouteData(key, uuid, msg.Buffer, msg.Buffer.Length - 17, 17);
                    }
                    break;
                case GateMessage.Handshake:
                    {
                        var uuid = new Guid(br.ReadBytes(UuidLen));

                        Console.WriteLine("OnFrontendMessageReceived uuid:{0}", uuid);

                        uuidToClients[uuid] = conn;

                        // send back a ack
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void OnBackendMessageReceived(IRemote conn, Message msg)
        {
            Console.WriteLine("OnBackendMessageReceived id:{0} ip:{1} port:{2}", conn.Id, conn.RemoteIp, conn.RemotePort);
            Console.WriteLine("OnBackendMessageReceived msgLen:{0}", msg.Buffer.Length);

            // forward backend msg to frontend, 
            // which is based on forwardId associated with the msg

            var stream = new MemoryStream(msg.Buffer);
            var br = new BinaryReader(stream);
            var msgType = (GateMessage)br.ReadByte();

            Console.WriteLine("OnBackendMessageReceived msgType:{0}", msgType);

            switch (msgType)
            {
                #region Broker Component
                case GateMessage.Subscribe:
                    {
                        var key = br.ReadInt32();
                        var uuid = new Guid(br.ReadBytes(UuidLen));

                        Console.WriteLine("OnBackendMessageReceived key:{0} uuid:{1}", key, uuid);
                        broker.AddRouting(key, uuid, conn);
                    }
                    break;
                case GateMessage.Unicast:
                    {
                        var uuid = new Guid(br.ReadBytes(UuidLen));

                        Console.WriteLine("OnBackendMessageReceived uuid:{0}", uuid);
                        // uuid to client IRemote
                        IRemote remote = null;
                        if (!uuidToClients.TryGetValue(uuid, out remote))
                        {
                            return;
                        }

                        PushReceivedMessage(remote, msg.Buffer, msg.Buffer.Length - UuidLen - 1, UuidLen + 1);
                    }
                    break;
                #endregion

                #region Forward Component
                case GateMessage.AddForward:
                case GateMessage.RemoveForward:
                    {
                        var uuid = new Guid(br.ReadBytes(UuidLen));
                        var forwardId = br.ReadInt32();

                        Console.WriteLine("OnBackendMessageReceived uuid:{0} forwardId:{1}", uuid, forwardId);
                        // todo 
                        // what if backend control forward first,
                        // then client handshake?
                        switch (msgType)
                        {
                            case GateMessage.AddForward:
                                forwardManager.AddForward(uuid, forwardId);
                                break;
                            case GateMessage.RemoveForward:
                                forwardManager.RemoveForward(uuid, forwardId);
                                break;
                        }
                    }
                    break;
                case GateMessage.Multicast:
                    {
                        var forwardId = br.ReadInt32();

                        Console.WriteLine("OnBackendMessageReceived forwardId:{0}", forwardId);
                        // forwardId to client IRemotes
                        foreach (var uuid in forwardManager.GetForwardGroup(forwardId))
                        {
                            IRemote remote = null;
                            if (uuidToClients.TryGetValue(uuid, out remote))
                            {
                                PushReceivedMessage(remote, msg.Buffer, msg.Buffer.Length - 4 - 1, 4 + 1);
                            }
                        }
                    }
                    break;
                #endregion
            }
        }

        public static void PushReceivedMessage(IRemote remote, byte[] data, int len, int offset)
        {
            if (!remote.Connected)
            {
                return;
            }
            remote.PushBegin(receivedBuffer.Length + len);
            remote.PushMore(receivedBuffer, receivedBuffer.Length, 0);
            remote.PushMore(data, len, offset);
        }

        internal enum GateMessage
        {
            Send = 0,
            Handshake = 1,
            Received = 2,
            Subscribe = 3,
            AddForward = 7,
            RemoveForward = 22,
            Unicast = 33,
            Multicast = 36,
        }
    }
}
