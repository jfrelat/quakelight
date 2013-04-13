using System;

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
// net_loop.c

namespace quake
{
    struct vrect_s
    {
        const int x = 0;
        const int y = 4;
        const int width = 8;
        const int height = 12;
    }

    public partial class net
    {
        static bool	        localconnectpending = false;
        static qsocket_t	loop_client = null;
        static qsocket_t    loop_server = null;

        static int Loop_Init ()
        {
	        if (client.cls.state == client.cactive_t.ca_dedicated)
		        return -1;
	        return 0;
        }

        static void Loop_Shutdown()
        {
        }

        static void Loop_Listen(bool state)
        {
        }

        static void Loop_SearchForHosts(bool xmit)
        {
	        if (!server.sv.active)
		        return;

	        hostCacheCount = 1;
	        if (hostname._string.CompareTo("UNNAMED") == 0)
		        hostcache[0].name = "local";
	        else
		        hostcache[0].name = hostname._string;
	        hostcache[0].map = server.sv.name;
	        hostcache[0].users = net_activeconnections;
	        hostcache[0].maxusers = server.svs.maxclients;
	        hostcache[0].driver = net_driverlevel;
	        hostcache[0].cname = "local";
        }

        static qsocket_t Loop_Connect(string host)
        {
	        if (host.CompareTo("local") != 0)
		        return null;
        	
	        localconnectpending = true;

	        if (loop_client == null)
	        {
		        if ((loop_client = NET_NewQSocket ()) == null)
		        {
			        console.Con_Printf("Loop_Connect: no qsocket available\n");
			        return null;
		        }
		        loop_client.address = "localhost";
	        }
	        loop_client.receiveMessageLength = 0;
	        loop_client.sendMessageLength = 0;
	        loop_client.canSend = true;

	        if (loop_server == null)
	        {
		        if ((loop_server = NET_NewQSocket ()) == null)
		        {
			        console.Con_Printf("Loop_Connect: no qsocket available\n");
			        return null;
		        }
		        loop_server.address = "LOCAL";
	        }
	        loop_server.receiveMessageLength = 0;
	        loop_server.sendMessageLength = 0;
	        loop_server.canSend = true;

	        loop_client.driverdata = loop_server;
	        loop_server.driverdata = loop_client;
        	
	        return loop_client;	
        }

        static qsocket_t Loop_CheckNewConnections()
        {
	        if (!localconnectpending)
		        return null;

	        localconnectpending = false;
	        loop_server.sendMessageLength = 0;
	        loop_server.receiveMessageLength = 0;
	        loop_server.canSend = true;
	        loop_client.sendMessageLength = 0;
	        loop_client.receiveMessageLength = 0;
	        loop_client.canSend = true;
	        return loop_server;
        }

        static int IntAlign(int value)
        {
	        return (value + (sizeof(int) - 1)) & (~(sizeof(int) - 1));
        }

        static int Loop_GetMessage(qsocket_t sock)
        {
	        int		ret;
	        int		length;

	        if (sock.receiveMessageLength == 0)
		        return 0;

	        ret = sock.receiveMessage[0];
	        length = sock.receiveMessage[1] + (sock.receiveMessage[2] << 8);
	        // alignment byte skipped here
	        common.SZ_Clear (net_message);
            common.SZ_Write (net_message, sock.receiveMessage, 4, length);

	        length = IntAlign(length + 4);
	        sock.receiveMessageLength -= length;

            if (sock.receiveMessageLength != 0)
                Buffer.BlockCopy(sock.receiveMessage, length, sock.receiveMessage, 0, sock.receiveMessageLength);

	        if (sock.driverdata != null && ret == 1)
		        ((qsocket_t)sock.driverdata).canSend = true;

	        return ret;
        }

        static int Loop_SendMessage(qsocket_t sock, common.sizebuf_t data)
        {
            byte[]  buffer;
            int     ofs;
	        int     bufferLength;

	        if (sock.driverdata == null)
		        return -1;

	        bufferLength = ((qsocket_t)sock.driverdata).receiveMessageLength;

	        if ((bufferLength + data.cursize + 4) > NET_MAXMESSAGE)
		        sys_linux.Sys_Error("Loop_SendMessage: overflow\n");

	        buffer = ((qsocket_t)sock.driverdata).receiveMessage;
            ofs = bufferLength;

	        // message type
	        buffer[ofs++] = 1;

	        // length
	        buffer[ofs++] = (byte)(data.cursize & 0xff);
	        buffer[ofs++] = (byte)(data.cursize >> 8);

	        // align
	        ofs++;

	        // message
            Buffer.BlockCopy(data.data, 0, buffer, ofs, data.cursize);
	        ((qsocket_t)sock.driverdata).receiveMessageLength = IntAlign(bufferLength + data.cursize + 4);

	        sock.canSend = false;
	        return 1;
        }

        static int Loop_SendUnreliableMessage(qsocket_t sock, common.sizebuf_t data)
        {
	        byte[]  buffer;
            int     ofs;
	        int     bufferLength;

	        if (sock.driverdata == null)
		        return -1;

	        bufferLength = ((qsocket_t)sock.driverdata).receiveMessageLength;

	        if ((bufferLength + data.cursize + sizeof(byte) + sizeof(short)) > NET_MAXMESSAGE)
		        return 0;

	        buffer = ((qsocket_t)sock.driverdata).receiveMessage;
            ofs = bufferLength;

	        // message type
	        buffer[ofs++] = 2;

	        // length
	        buffer[ofs++] = (byte)(data.cursize & 0xff);
	        buffer[ofs++] = (byte)(data.cursize >> 8);

	        // align
	        ofs++;

	        // message
            Buffer.BlockCopy(data.data, 0, buffer, ofs, data.cursize);
            ((qsocket_t)sock.driverdata).receiveMessageLength = IntAlign(bufferLength + data.cursize + 4);
	        return 1;
        }

        static bool Loop_CanSendMessage(qsocket_t sock)
        {
	        if (sock.driverdata == null)
		        return false;
	        return sock.canSend;
        }

        static bool Loop_CanSendUnreliableMessage(qsocket_t sock)
        {
	        return true;
        }

        static void Loop_Close(qsocket_t sock)
        {
	        if (sock.driverdata != null)
		        ((qsocket_t)sock.driverdata).driverdata = null;
	        sock.receiveMessageLength = 0;
	        sock.sendMessageLength = 0;
	        sock.canSend = true;
	        if (sock == loop_client)
		        loop_client = null;
	        else
                loop_server = null;
        }
    }
}