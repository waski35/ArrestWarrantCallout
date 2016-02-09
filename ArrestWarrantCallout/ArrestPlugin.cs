using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using Rage;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ArrestWarrantCallout
{
    
    public class ArrestWarrantClass
    {
        public static string plug_ver = "Arrest Warrant Callout " + typeof(ArrestWarrantClass).Assembly.GetName().Version;
        public static int option_enable_dispatch = 0;
        public static string option_player_name = "01-ADAM-12";
        public static int option_dev_mode = 0;
        public static GameFiber dthread;
        
    /// <summary>
    /// Do not rename! Attributes or inheritance based plugins will follow when the API is more in depth.
    /// </summary>
    public class Main : Plugin
    {
       


        /// <summary>
        /// Constructor for the main class, same as the class, do not rename.
        /// </summary>
        public Main()
        {
            Game.LogTrivial(plug_ver + " : Plugin loaded !");
            if (!CheckVersionsofAssemblies()) return;
            if (option_dev_mode == 35)
            {
                Game.LogTrivial(plug_ver + " : Developer mode activated !");
            }
            ThreadStart dev_thread = new ThreadStart(ArrestWarrantClass.DevThread);
            dthread = new GameFiber(ArrestWarrantClass.DevThread, "awc_dev_checks_thread");
            dthread.Start();
          
        }

        /// <summary>
        /// Called when the plugin ends or is terminated to cleanup
        /// </summary>
        public override void Finally()
        {
            
        }

        /// <summary>
        /// Called when the plugin is first loaded by LSPDFR
        /// </summary>
        public override void Initialize()
        {
            //Event handler for detecting if the player goes on duty
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            
            Game.LogTrivial("Arrest Warrant Callout " + typeof(ArrestWarrantClass).Assembly.GetName().Version.ToString() + " loaded!");
            
            ReadSettings();
        }

        /// <summary>
        /// The event handler mentioned above,
        /// </summary>
        static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                //If the player goes on duty we need to register our custom callouts
                //Here we register our ExampleCallout class which is inside our Callouts folder (APIExample.Callouts namespace)
                Functions.RegisterCallout(typeof(ArrestCallout));
                Functions.RegisterCallout(typeof(PrisonEscape));
                Functions.RegisterCallout(typeof(ArrestatWorkplaceCallout));
                
                Game.DisplayNotification("~b~Arrest Warrant Callout~w~ " + typeof(ArrestWarrantClass).Assembly.GetName().Version.ToString() + "~g~ loaded !");
            }
       }
        static void ReadSettings()
        {
            string line = "";
            string path = Directory.GetCurrentDirectory();
            path = path + "\\Plugins\\LSPDFR\\ArrestWarrantCallout.ini";
            if (File.Exists(path))
            {
                Game.LogTrivial(plug_ver + " : found settings file, adjusting settings.");
                Game.LogTrivial(plug_ver + " : Settings File path : " + path);
                System.IO.StreamReader file = new System.IO.StreamReader(path);
                int index_start = 0;
                int index_stop = 0;
                char[] usun_zn = { ';', ',', '.', '#', '/', '\\', ' ' };
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    line = line.Trim(usun_zn);
                    if (line.Contains("enable_dispatch="))
                    {
                        index_start = line.IndexOf('=');
                        index_stop = line.Length - line.IndexOf('=');
                        option_enable_dispatch = Convert.ToInt32(line.Substring(index_start + 1));
                    }
                    if (line.Contains("player_name="))
                    {
                        index_start = line.IndexOf('=');
                        index_stop = line.Length - line.IndexOf('=');
                        option_player_name = Convert.ToString(line.Substring(index_start + 1));
                        if (option_player_name == "" || option_player_name.Length < 2)
                        {
                            option_player_name = "01-ADAM-12";
                        }
                    }
                    if (line.Contains("do_not_touch_this="))
                    {
                        index_start = line.IndexOf('=');
                        index_stop = line.Length - line.IndexOf('=');
                        option_dev_mode = Convert.ToInt32(line.Substring(index_start + 1));
                        if (option_dev_mode != 35)
                        {
                            option_dev_mode = 0;
                        }
                    }
                    

                }

                file.Close();
            }

        }
    }
    public static void DevThread()
    {
        while (true)
           {
               if (option_dev_mode == 35)
               {
                   if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad0))
                   {
                       Functions.StopCurrentCallout();
                       Functions.StartCallout("ArrestCallout");
                   }
                   else if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad1))
                   {
                       Functions.StopCurrentCallout();
                       Functions.StartCallout("PrisonEscape");
                   }
                   else if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad2))
                   {
                       Functions.StopCurrentCallout();
                       Functions.StartCallout("ArrestAtWorkplaceCallout");
                   }
                   else
                   {
                       //do nothing
                   }
               }
               GameFiber.Yield();
              
           }
           
    }
    private static bool CheckVersionsofAssemblies()
    {
        bool ret = false;
        // Get the file version for the notepad.
        FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo("RagePluginHook.exe");
        if (myFileVersionInfo.FileMajorPart >= 0)
        {
            if (myFileVersionInfo.FileMinorPart >= 35 && myFileVersionInfo.FileMinorPart < 37)
            {
                Game.LogTrivial("Found RPH version 0.35 or 0.36.");
                ret = true;
            }
            else if (myFileVersionInfo.FileMinorPart >= 33 && myFileVersionInfo.FileMinorPart < 35)
            {
                Game.LogTrivial("Found RPH version 0.33 or 0.34.");
                Game.LogTrivial("exiting.");
                Game.DisplayNotification("AWC : Incompatible RPH version detected, exiting!");
                ret = false;
            }
            else if (myFileVersionInfo.FileMinorPart < 33)
            {
                Game.LogTrivial("Found incompatible version of RPH.");
                Game.LogTrivial("exiting.");
                Game.DisplayNotification("AWC : Incompatible RPH version detected, exiting!");
                ret = false;
            }
            else if (myFileVersionInfo.FileMinorPart >= 37)
            {
                Game.LogTrivial("Found non-tested version of RPH.");
                Game.LogTrivial("allowing to run.");
                Game.DisplayNotification("AWC : Non-tested version of RPH found. Allowing to run.");
                ret = true;
            }
            else
            {
                Game.LogTrivial("Found incompatible version of RPH.");
                Game.LogTrivial("exiting.");
                Game.DisplayNotification("AWC : Incompatible RPH version detected, exiting!");
                ret = false;
            }
        }
        if (ret == false)
        {
            return false;
        }
        FileVersionInfo myFileVersionInfo2 = FileVersionInfo.GetVersionInfo("\\Plugins\\LSPD First Response.dll");
        if (myFileVersionInfo2.FileMajorPart >= 0)
        {
            if (myFileVersionInfo2.FileMinorPart >= 3)
            {
                Game.LogTrivial("Found LSPDFR version 0.3 or better.");
                ret = true;
            }
            else if (myFileVersionInfo2.FileMinorPart >= 2 && myFileVersionInfo2.FileMinorPart < 3)
            {
                Game.LogTrivial("Found LSPDFR version 0.2.");
                Game.LogTrivial("exiting.");
                Game.DisplayNotification("AWC : Incompatible LSPDFR version detected, exiting!");
                ret = false;
            }
            else
            {
                Game.LogTrivial("Found incompatible LSPDFR version, there might be compatibility issues.");
                Game.LogTrivial("exiting.");
                Game.DisplayNotification("AWC : Incompatible LSPDFR version detected, exiting!");
                ret = false;
            }
        }
        return ret;
    }


    } // class
} // namespace
