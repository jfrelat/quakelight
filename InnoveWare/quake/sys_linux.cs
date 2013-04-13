using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using System.Threading;

namespace quake
{
    public sealed class sys_linux
    {
        bool	isDedicated;

        static int     nostdout = 0;

        static string  basedir = ".";
        string  cachedir = "/tmp";

        static cvar_t  sys_linerefresh = new cvar_t("sys_linerefresh","0");// set for entity display

        // =======================================================================
        // General routines
        // =======================================================================

        void Sys_DebugNumber(int y, int val)
        {
        }

        static string printbuffer = "";
        public static void Sys_Printf(string text)
        {
            printbuffer += text;
            if (printbuffer[printbuffer.Length - 1] == '\n')
            {
                Debug.WriteLine(printbuffer.Substring(0, printbuffer.Length - 1));
                printbuffer = "";
            }
        }

        public static void Sys_Quit ()
        {
        }

        static void Sys_Init()
        {
        }

        public static void Sys_Error (string error)
        {
            MessageBox.Show("Error: " + error);
//            Debug.WriteLine("Error: " + error);
//            Host_Shutdown ();
            throw new Exception();
        } 

        void Sys_Warn (string warning)
        { 
            Debug.WriteLine("Warning: " + warning);
        } 

        /*
        ===============================================================================

        FILE IO

        ===============================================================================
        */

        const int	MAX_HANDLES = 10;
        public static StreamResourceInfo[]	sys_handles = new StreamResourceInfo[MAX_HANDLES];

        static int		findhandle ()
        {
	        int		i;
        	
	        for (i=1 ; i<MAX_HANDLES ; i++)
		        if (sys_handles[i] == null)
			        return i;
	        Sys_Error ("out of handles");
	        return -1;
        }

        /*
        ============
        Sys_FileTime

        returns -1 if not present
        ============
        */
        int	Sys_FileTime (string path)
        {
            return -1;
        }

        void Sys_mkdir (string path)
        {
        }

        public static int Sys_FileOpenRead (string path, ref int handle)
        {
            if (path.StartsWith("./"))
                path = path.Substring(2);

            StreamResourceInfo si = Application.GetResourceStream(new Uri("InnoveWare;component/" + path, UriKind.Relative));
            if (si == null)
            {
                handle = -1;
                return -1;
            }

            handle = findhandle();
		    sys_handles[handle] = si;

            return (int) si.Stream.Length;
        }

        public static int Sys_FileOpenWrite(string path)
        {
            return -1;
        }

        public static int Sys_FileWrite (int handle, byte[] src, int count)
        {
            return -1;
        }

        public static void Sys_FileClose (int handle)
        {
            sys_handles[handle].Stream.Close();
            sys_handles[handle] = null;
        }

        public static void Sys_FileSeek (int handle, int position)
        {

            sys_handles[handle].Stream.Seek(position, SeekOrigin.Begin);
        }

        public static int Sys_FileRead (int handle, byte[] dest, int count)
        {
            return sys_handles[handle].Stream.Read(dest, 0, count);
        }

        void Sys_DebugLog(string file, string fmt, params string[] strings)
        {
        }

        void Sys_EditFile(string filename)
        {
        }

        public static double Sys_FloatTime ()
        {
            DateTime now = DateTime.Now;
            return (now.Ticks / 10000000.0);
        }

        // =======================================================================
        // Sleeps for microseconds
        // =======================================================================

        static volatile int oktogo;

        void alarm_handler(int x)
        {
	        oktogo=1;
        }

        static void Sys_LineRefresh()
        {
        }

        void floating_point_exception_handler(int whatever)
        {
        }

        string Sys_ConsoleInput()
        {
	        return null;
        }

        void Sys_HighFPPrecision ()
        {
        }

        void Sys_LowFPPrecision ()
        {
        }

        public static int main (int c, string[] v)
        {
	        double		            time, oldtime, newtime;
	        quakedef.quakeparms_t   parms = new quakedef.quakeparms_t();
	        int j;

	        //COM_InitArgv(c, v);
	        //parms.argc = com_argc;
	        //parms.argv = com_argv;

	        parms.memsize = 8*1024*1024;

	        parms.basedir = basedir;

            host.Host_Init(parms);

	        Sys_Init();

	        if (common.COM_CheckParm("-nostdout") != 0)
		        nostdout = 1;
	        else {
                Debug.WriteLine("Linux Quake -- Version " + quakedef.LINUX_VERSION);
	        }

            //cmd.Cbuf_InsertText("menu_main\n");
            return 0;

            oldtime = Sys_FloatTime () - 0.1;
            while (true)
            {
        // find time spent rendering last frame
                newtime = Sys_FloatTime ();
                time = newtime - oldtime;

                /*if (cls.state == ca_dedicated)
                {   // play vcrfiles at max speed
                    if (time < sys_ticrate.value && (vcrFile == -1 || recording) )
                    {
				        usleep(1);
                        continue;       // not time to run a server only tic yet
                    }
                    time = sys_ticrate.value;
                }*/

                /*if (time > sys_ticrate.value*2)
                    oldtime = newtime;
                else
                    oldtime += time;*/

                host.Host_Frame (time);

        // graphic debugging aids
                if (sys_linerefresh.value != 0)
                    Sys_LineRefresh ();

                Thread.Sleep(200);
            }

        }


        /*
        ================
        Sys_MakeCodeWriteable
        ================
        */
        void Sys_MakeCodeWriteable (ulong startaddr, ulong length)
        {
        }
    }
}

