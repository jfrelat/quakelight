/*
Copyright (C) 1996-1997 Id Software, Inc.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  

See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

*/
// net.h -- quake's interface to the networking layer

namespace quake
{
    public partial class net
    {
        public const int    NET_NAMELEN			= 64;

        public const int    NET_MAXMESSAGE		= 8192;

        public class qsocket_t
        {
	        public qsocket_t    next;
	        public double		connecttime;
            public double       lastMessageTime;
	        double			lastSendTime;

	        public bool		    disconnected;
            public bool         canSend;
            public bool         sendNext;

            public int          driver;
	        int				landriver;
            public int          socket;
	        public object		driverdata;

            public uint         ackSequence;
            public uint         sendSequence;
            public uint         unreliableSendSequence;
            public int          sendMessageLength;
	        byte[]			sendMessage = new byte[NET_MAXMESSAGE];

            public uint         receiveSequence;
            public uint         unreliableReceiveSequence;
            public int          receiveMessageLength;
            public byte[]       receiveMessage = new byte[NET_MAXMESSAGE];

	        //struct qsockaddr	addr;
            public string       address = new string(new char[NET_NAMELEN]);
        };

        public const int	MAX_NET_DRIVERS		= 8;

        public class net_driver_t
        {
	        public string		                name;
            public bool                         initialized;
            public delegate int                 Init();
            public Init                         delegate_Init;
            public delegate void                Listen(bool state);
            public Listen                       delegate_Listen;
            public delegate void                SearchForHosts(bool xmit);
            public SearchForHosts               delegate_SearchForHosts;
            public delegate qsocket_t           Connect(string host);
            public Connect                      delegate_Connect;
            public delegate qsocket_t           CheckNewConnections();
            public CheckNewConnections          delegate_CheckNewConnections;
            public delegate int                 QGetMessage(qsocket_t sock);
            public QGetMessage                  delegate_QGetMessage;
            public delegate int                 QSendMessage(qsocket_t sock, common.sizebuf_t data);
            public QSendMessage                 delegate_QSendMessage;
            public delegate int                 SendUnreliableMessage(qsocket_t sock, common.sizebuf_t data);
            public SendUnreliableMessage        delegate_SendUnreliableMessage;
            public delegate bool                CanSendMessage(qsocket_t sock);
            public CanSendMessage               delegate_CanSendMessage;
            public delegate bool                CanSendUnreliableMessage(qsocket_t sock);
            public CanSendUnreliableMessage     delegate_CanSendUnreliableMessage;
            public delegate void                Close(qsocket_t sock);
            public Close                        delegate_Close;
            public delegate void                Shutdown();
            public Shutdown                     delegate_Shutdown;
            public int                          controlSock;

            public net_driver_t(string name,
                                bool initialized,
                                Init delegate_Init,
                                Listen delegate_Listen,
                                SearchForHosts delegate_SearchForHosts,
                                Connect delegate_Connect,
                                CheckNewConnections delegate_CheckNewConnections,
                                QGetMessage delegate_QGetMessage,
                                QSendMessage delegate_QSendMessage,
                                SendUnreliableMessage delegate_SendUnreliableMessage,
                                CanSendMessage delegate_CanSendMessage,
                                CanSendUnreliableMessage delegate_CanSendUnreliableMessage,
                                Close delegate_Close,
                                Shutdown delegate_Shutdown)
            {
                this.name = name;
                this.initialized = initialized;
                this.delegate_Init = delegate_Init;
                this.delegate_Listen = delegate_Listen;
                this.delegate_SearchForHosts = delegate_SearchForHosts;
                this.delegate_Connect = delegate_Connect;
                this.delegate_CheckNewConnections = delegate_CheckNewConnections;
                this.delegate_QGetMessage = delegate_QGetMessage;
                this.delegate_QSendMessage = delegate_QSendMessage;
                this.delegate_SendUnreliableMessage = delegate_SendUnreliableMessage;
                this.delegate_CanSendMessage = delegate_CanSendMessage;
                this.delegate_CanSendUnreliableMessage = delegate_CanSendUnreliableMessage;
                this.delegate_Close = delegate_Close;
                this.delegate_Shutdown = delegate_Shutdown;
            }
        };

        public const int    HOSTCACHESIZE	    = 8;

        public class hostcache_t
        {
	        public string	name = new string(new char[16]);
	        public string	map = new string(new char[16]);
	        public string   cname = new string(new char[32]);
	        public int		users;
	        public int		maxusers;
	        public int		driver;
	        public int		ldriver;
	        //public qsockaddr addr;
        } ;
    }
}