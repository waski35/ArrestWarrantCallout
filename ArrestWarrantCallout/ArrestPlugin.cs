using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using Rage;
using System.IO;

namespace ArrestWarrantCallout
{
    
    public class ArrestWarrantClass
    {
        public static string plug_ver = "Arrest Warrant Callout " + typeof(ArrestWarrantClass).Assembly.GetName().Version;
        public static int option_enable_dispatch = 0;
        
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
            Game.LogTrivial("Arrest Warrant Callout " + typeof(ArrestWarrantClass).Assembly.GetName().Version.ToString() + "loaded!");
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
                ReadSettings();
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
                    

                }

                file.Close();
            }

        }
    }

    }
}
