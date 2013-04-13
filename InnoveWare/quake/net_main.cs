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
// net_main.c

namespace quake
{
    public partial class net
    {
        static qsocket_t    net_activeSockets = null;
        static qsocket_t	net_freeSockets = null;
        static int			net_numsockets = 0;

        bool	        serialAvailable = false;
        bool	        ipxAvailable = false;
        bool            tcpipAvailable = false;

        static bool         listening = false;

        public static common.sizebuf_t	net_message = new common.sizebuf_t();
        public static int			    net_activeconnections = 0;

        static int messagesSent = 0;
        static int messagesReceived = 0;
        static int unreliableMessagesSent = 0;
        static int unreliableMessagesReceived = 0;

        static cvar_t net_messagetimeout = new cvar_t("net_messagetimeout","300");
        public static cvar_t hostname = new cvar_t("hostname", "UNNAMED");

        static cvar_t config_com_port = new cvar_t("_config_com_port", "0x3f8", true);
        static cvar_t config_com_irq = new cvar_t("_config_com_irq", "4", true);
        static cvar_t config_com_baud = new cvar_t("_config_com_baud", "57600", true);
        static cvar_t config_com_modem = new cvar_t("_config_com_modem", "1", true);
        static cvar_t config_modem_dialtype = new cvar_t("_config_modem_dialtype", "T", true);
        static cvar_t config_modem_clear = new cvar_t("_config_modem_clear", "ATZ", true);
        static cvar_t config_modem_init = new cvar_t("_config_modem_init", "", true);
        static cvar_t config_modem_hangup = new cvar_t("_config_modem_hangup", "AT H", true);

        static int          net_driverlevel;

        static double       net_time;

        static double SetNetTime()
        {
	        net_time = sys_linux.Sys_FloatTime();
	        return net_time;
        }

        /*
        ===================
        NET_NewQSocket

        Called by drivers when a new communications endpoint is required
        The sequence and buffer fields will be filled in properly
        ===================
        */
        static qsocket_t NET_NewQSocket ()
        {
	        qsocket_t	sock;

	        if (net_freeSockets == null)
                return null;

	        if (net_activeconnections >= server.svs.maxclients)
                return null;

	        // get one from free list
	        sock = net_freeSockets;
	        net_freeSockets = sock.next;

	        // add it to active list
	        sock.next = net_activeSockets;
	        net_activeSockets = sock;

	        sock.disconnected = false;
	        sock.connecttime = net_time;
	        sock.address = "UNSET ADDRESS";
	        sock.driver = net_driverlevel;
	        sock.socket = 0;
	        sock.driverdata = null;
	        sock.canSend = true;
	        sock.sendNext = false;
	        sock.lastMessageTime = net_time;
	        sock.ackSequence = 0;
	        sock.sendSequence = 0;
	        sock.unreliableSendSequence = 0;
	        sock.sendMessageLength = 0;
	        sock.receiveSequence = 0;
	        sock.unreliableReceiveSequence = 0;
	        sock.receiveMessageLength = 0;

	        return sock;
        }

        static void NET_FreeQSocket(qsocket_t sock)
        {
	        qsocket_t	s;

	        // remove it from active list
	        if (sock == net_activeSockets)
		        net_activeSockets = net_activeSockets.next;
	        else
	        {
		        for (s = net_activeSockets; s != null; s = s.next)
			        if (s.next == sock)
			        {
				        s.next = sock.next;
				        break;
			        }
		        if (s == null)
			        sys_linux.Sys_Error ("NET_FreeQSocket: not active\n");
	        }

	        // add it to free list
	        sock.next = net_freeSockets;
	        net_freeSockets = sock;
	        sock.disconnected = true;
        }

        static void NET_Listen_f ()
        {
            if (cmd.Cmd_Argc() != 2)
            {
                console.Con_Printf("\"listen\" is \"" + (listening ? 1 : 0) + "\"\n");
                return;
            }

            listening = int.Parse(cmd.Cmd_Argv(1)) != 0 ? true : false;

	        for (net_driverlevel=0 ; net_driverlevel<net_numdrivers; net_driverlevel++)
	        {
		        if (net_drivers[net_driverlevel].initialized == false)
			        continue;
                net_drivers[net_driverlevel].delegate_Listen(listening);
	        }
        }

        static void MaxPlayers_f ()
        {
            int n;

            if (cmd.Cmd_Argc() != 2)
            {
                console.Con_Printf("\"maxplayers\" is \"" + server.svs.maxclients + "\"\n");
                return;
            }

            if (server.sv.active)
            {
                console.Con_Printf("maxplayers can not be changed while a server is running.\n");
                return;
            }

            n = int.Parse(cmd.Cmd_Argv(1));
            if (n < 1)
                n = 1;
            if (n > server.svs.maxclientslimit)
            {
                n = server.svs.maxclientslimit;
                console.Con_Printf("\"maxplayers\" set to \"" + n + "\"\n");
            }

            if ((n == 1) && listening)
                cmd.Cbuf_AddText("listen 0\n");

            if ((n > 1) && (!listening))
                cmd.Cbuf_AddText("listen 1\n");

            server.svs.maxclients = n;
            if (n == 1)
                cvar_t.Cvar_Set("deathmatch", "0");
            else
                cvar_t.Cvar_Set("deathmatch", "1");
        }

        static void NET_Port_f ()
        {
        }

        static void NET_Slist_f ()
        {
        }

        /*
        ===================
        NET_Connect
        ===================
        */

        static int hostCacheCount = 0;
        static hostcache_t[] hostcache = new hostcache_t[HOSTCACHESIZE];
        
        static net()
        {
            for (int kk = 0; kk < HOSTCACHESIZE; kk++) hostcache[kk] = new hostcache_t();
        }

        public static qsocket_t NET_Connect (string host)
        {
        	qsocket_t		ret;
	        int				n;
	        int				numdrivers = 1;

	        SetNetTime();

	        if (host != null && host.Length == 0)
		        host = null;

	        if (host != null)
	        {
		        if (host.CompareTo("local") == 0)
		        {
			        numdrivers = 1;
			        goto JustDoIt;
		        }

		        if (hostCacheCount != 0)
		        {
			        for (n = 0; n < hostCacheCount; n++)
				        if (host.CompareTo(hostcache[n].name) == 0)
				        {
					        host = hostcache[n].cname;
					        break;
				        }
			        if (n < hostCacheCount)
				        goto JustDoIt;
		        }
	        }

        JustDoIt:
	        for (net_driverlevel=0 ; net_driverlevel<numdrivers; net_driverlevel++)
	        {
		        if (net_drivers[net_driverlevel].initialized == false)
			        continue;
                ret = net_drivers[net_driverlevel].delegate_Connect(host);
		        if (ret != null)
			        return ret;
	        }

	        if (host != null)
	        {
		        console.Con_Printf("\n");
	        }

	        return null;
        }

        /*
        ===================
        NET_CheckNewConnections
        ===================
        */

        public static qsocket_t NET_CheckNewConnections ()
        {
	        qsocket_t	ret;

	        SetNetTime();

	        for (net_driverlevel=0 ; net_driverlevel<net_numdrivers; net_driverlevel++)
	        {
		        if (net_drivers[net_driverlevel].initialized == false)
			        continue;
		        if (net_driverlevel != 0 && listening == false)
			        continue;
                ret = net_drivers[net_driverlevel].delegate_CheckNewConnections();
		        if (ret != null)
		        {
			        return ret;
		        }
	        }
        	
	        return null;
        }

        /*
        ===================
        NET_Close
        ===================
        */
        public static void NET_Close(qsocket_t sock)
        {
            if (sock == null)
                return;

            if (sock.disconnected)
                return;

            SetNetTime();

            // call the driver_Close function
            net_drivers[sock.driver].delegate_Close(sock);

            NET_FreeQSocket(sock);
        }

        /*
        =================
        NET_GetMessage

        If there is a complete message, return it in net_message

        returns 0 if no data is waiting
        returns 1 if a message was received
        returns -1 if connection is invalid
        =================
        */

        public static int	NET_GetMessage (qsocket_t sock)
        {
	        int ret;

	        if (sock == null)
		        return -1;

	        if (sock.disconnected)
	        {
		        console.Con_Printf("NET_GetMessage: disconnected socket\n");
		        return -1;
	        }

	        SetNetTime();

            ret = net_drivers[sock.driver].delegate_QGetMessage(sock);

	        // see if this connection has timed out
	        if (ret == 0/* && sock.driver*/)
	        {
		        if (net_time - sock.lastMessageTime > net_messagetimeout.value)
		        {
			        NET_Close(sock);
			        return -1;
		        }
	        }


	        if (ret > 0)
	        {
		        if (sock.driver != 0)
		        {
			        sock.lastMessageTime = net_time;
			        if (ret == 1)
				        messagesReceived++;
			        else if (ret == 2)
				        unreliableMessagesReceived++;
		        }
	        }
	        else
	        {
	        }

	        return ret;
        }

        /*
        ==================
        NET_SendMessage

        Try to send a complete length+message unit over the reliable stream.
        returns 0 if the message cannot be delivered reliably, but the connection
		        is still considered valid
        returns 1 if the message was sent properly
        returns -1 if the connection died
        ==================
        */

        public static int NET_SendMessage (qsocket_t sock, common.sizebuf_t data)
        {
	        int		r;
        	
	        if (sock == null)
		        return -1;

	        if (sock.disconnected)
	        {
		        console.Con_Printf("NET_SendMessage: disconnected socket\n");
		        return -1;
	        }

	        SetNetTime();
            r = net_drivers[sock.driver].delegate_QSendMessage(sock, data);
	        if (r == 1 && sock.driver != 0)
		        messagesSent++;

	        return r;
        }

        public static int NET_SendUnreliableMessage (qsocket_t sock, common.sizebuf_t data)
        {
	        int		r;
        	
	        if (sock == null)
		        return -1;

	        if (sock.disconnected)
	        {
		        console.Con_Printf("NET_SendMessage: disconnected socket\n");
		        return -1;
	        }

	        SetNetTime();
            r = net_drivers[sock.driver].delegate_SendUnreliableMessage(sock, data);
	        if (r == 1 && sock.driver != 0)
		        unreliableMessagesSent++;

	        return r;
        }
        
        /*
        ==================
        NET_CanSendMessage

        Returns true or false if the given qsocket can currently accept a
        message to be transmitted.
        ==================
        */
        public static bool NET_CanSendMessage (qsocket_t sock)
        {
	        bool		r;
        	
	        if (sock == null)
		        return false;

	        if (sock.disconnected)
		        return false;

	        SetNetTime();

            r = net_drivers[sock.driver].delegate_CanSendMessage(sock);
        	
	        return r;
        }
        
        public static int NET_SendToAll(common.sizebuf_t data, int blocktime)
        {
	        double		start;
	        int			i;
	        int			count = 0;
	        bool[]	    state1 = new bool[quakedef.MAX_SCOREBOARD];
            bool[]      state2 = new bool[quakedef.MAX_SCOREBOARD];

	        for (i=0 ; i<server.svs.maxclients ; i++)
	        {
                host.host_client = server.svs.clients[i];
		        if (host.host_client.netconnection == null)
			        continue;
		        if (host.host_client.active)
		        {
			        if (host.host_client.netconnection.driver == 0)
			        {
				        NET_SendMessage(host.host_client.netconnection, data);
				        state1[i] = true;
				        state2[i] = true;
				        continue;
			        }
			        count++;
			        state1[i] = false;
			        state2[i] = false;
		        }
		        else
		        {
			        state1[i] = true;
			        state2[i] = true;
		        }
	        }

	        start = sys_linux.Sys_FloatTime();
	        while (count != 0)
	        {
		        count = 0;
		        for (i=0 ; i<server.svs.maxclients ; i++)
		        {
                    host.host_client = server.svs.clients[i];
			        if (! state1[i])
			        {
				        if (NET_CanSendMessage (host.host_client.netconnection))
				        {
					        state1[i] = true;
					        NET_SendMessage(host.host_client.netconnection, data);
				        }
				        else
				        {
                            NET_GetMessage(host.host_client.netconnection);
				        }
				        count++;
				        continue;
			        }

			        if (! state2[i])
			        {
                        if (NET_CanSendMessage(host.host_client.netconnection))
				        {
					        state2[i] = true;
				        }
				        else
				        {
                            NET_GetMessage(host.host_client.netconnection);
				        }
				        count++;
				        continue;
			        }
		        }
		        if ((sys_linux.Sys_FloatTime() - start) > blocktime)
			        break;
	        }
	        return count;
        }

        //=============================================================================

        /*
        ====================
        NET_Init
        ====================
        */

        public static void NET_Init ()
        {
	        int			i;
	        int			controlSocket;
	        qsocket_t	s;

	        if (common.COM_CheckParm("-listen") != 0 || client.cls.state == client.cactive_t.ca_dedicated)
		        listening = true;
            net_numsockets = server.svs.maxclientslimit;
	        if (client.cls.state != client.cactive_t.ca_dedicated)
		        net_numsockets++;

	        SetNetTime();

	        for (i = 0; i < net_numsockets; i++)
	        {
                s = new qsocket_t();
		        s.next = net_freeSockets;
		        net_freeSockets = s;
		        s.disconnected = true;
	        }

	        // allocate space for network message buffer
	        common.SZ_Alloc (net_message, NET_MAXMESSAGE);

	        cvar_t.Cvar_RegisterVariable (net_messagetimeout);
	        cvar_t.Cvar_RegisterVariable (hostname);
	        cvar_t.Cvar_RegisterVariable (config_com_port);
	        cvar_t.Cvar_RegisterVariable (config_com_irq);
	        cvar_t.Cvar_RegisterVariable (config_com_baud);
	        cvar_t.Cvar_RegisterVariable (config_com_modem);
	        cvar_t.Cvar_RegisterVariable (config_modem_dialtype);
	        cvar_t.Cvar_RegisterVariable (config_modem_clear);
	        cvar_t.Cvar_RegisterVariable (config_modem_init);
	        cvar_t.Cvar_RegisterVariable (config_modem_hangup);

	        cmd.Cmd_AddCommand ("slist", NET_Slist_f);
	        cmd.Cmd_AddCommand ("listen", NET_Listen_f);
	        cmd.Cmd_AddCommand ("maxplayers", MaxPlayers_f);
	        cmd.Cmd_AddCommand ("port", NET_Port_f);

	        // initialize all the drivers
	        for (net_driverlevel=0 ; net_driverlevel<net_numdrivers ; net_driverlevel++)
	        {
		        controlSocket = net_drivers[net_driverlevel].delegate_Init();
		        if (controlSocket == -1)
			        continue;
		        net_drivers[net_driverlevel].initialized = true;
		        net_drivers[net_driverlevel].controlSock = controlSocket;
		        if (listening)
			        net_drivers[net_driverlevel].delegate_Listen (true);
	        }
        }

        /*
        ====================
        NET_Shutdown
        ====================
        */

        void		NET_Shutdown ()
        {
	        qsocket_t	sock;

	        SetNetTime();

	        for (sock = net_activeSockets; sock != null; sock = sock.next)
		        NET_Close(sock);

        //
        // shutdown the drivers
        //
	        for (net_driverlevel = 0; net_driverlevel < net_numdrivers; net_driverlevel++)
	        {
		        if (net_drivers[net_driverlevel].initialized == true)
		        {
			        net_drivers[net_driverlevel].delegate_Shutdown ();
			        net_drivers[net_driverlevel].initialized = false;
		        }
	        }
        }

        public static void NET_Poll()
        {
	        SetNetTime();
        }
    }
}