using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;

namespace ArrestWarrantCallout
{
    [CalloutInfo("ArrestAtWorkplaceCallout", CalloutProbability.VeryHigh)]

    class ArrestatWorkplaceCallout : Callout
    {
        //Here we declare our variables, things we need or our callout
        private Ped myPed; // a rage ped
        private Vector3 SpawnPoint; // a Vector3
        private Blip myBlip; // a rage blip
        private Blip myBlipArea;
        private LHandle pursuit; // an API pursuit handle
        private int rand_num = 0;
        private bool timeout_is_on = false;
        private int r_chance = 0;
        private bool fight_started = false;
        private int r_felony = 0;
        private string felony_s = "";
        private int wep_chance = 0;
        private bool got_arrested_notf = false;
        private int weh_chance = 0;
        private bool pursuit_created = false;

        private Vector3 from_pos;
        private bool blip_attached = false;
        private bool susp_left_car = true;
        private bool surrenderred = false;
        private bool player_interacting = false;
        private bool info_displayed = false;
        private int dialog_phase = 0;


        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set our spawn point to be on a street around 300f (distance) away from the player.
            //SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            Random random_number = new Random();
            rand_num = random_number.Next(1, 100); // chance where suspect will be
            SpawnPoint = CreateWantedPedLoc(rand_num);

            Random purs = new Random();
            r_chance = purs.Next(1, 100); // fight / surrender / pursuit chance

            Random wep = new Random();
            wep_chance = wep.Next(1, 100);

            Random fel = new Random();
            r_felony = fel.Next(1, 5);
            switch (r_felony)
            {
                case 1: felony_s = "robbery";
                    break;
                case 2: felony_s = "murder";
                    break;
                case 3: felony_s = "rape";
                    break;
                case 4: felony_s = "assault";
                    break;
                case 5: felony_s = "drug deal";
                    break;
                default: felony_s = "number of crimes";
                    break;
            }
           
            //Create our ped in the world
            myPed = new Ped(CommonFunctions.getVarPedModel(), SpawnPoint, 0f);
            myPed.KeepTasks = true;
            myPed.MakePersistent();
            DateTime birthday = new DateTime(1971, 01, 23);
            Persona crim_presona_old = Functions.GetPersonaForPed(myPed);

            Persona crim_persona_new = new Persona(myPed, crim_presona_old.Gender, crim_presona_old.BirthDay, 5, crim_presona_old.Forename, crim_presona_old.Surname, crim_presona_old.LicenseState, crim_presona_old.TimesStopped, true, false, false);
            Functions.SetPersonaForPed(myPed, crim_persona_new);
            


            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!myPed.Exists()) return false;
            


            
            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);


            // Set up our callout message and location
            this.CalloutMessage = "Arrest Warrant (Suspect at Workplace)";
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", SpawnPoint);
            Functions.PlayScannerAudio("RESPOND_CODE_2");

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            //if (rand_num < 80)
            //{
            //myBlip = myPed.AttachBlip();
            myBlipArea = new Blip(myPed.Position, 60f);
            myBlipArea.Color = System.Drawing.Color.Yellow;
            //myBlipArea.EnableRoute(System.Drawing.Color.Yellow);
            from_pos = new Vector3(myPed.Position.X, myPed.Position.Y, myPed.Position.Z);
            //speed_zone = World.AddSpeedZone(myPed.Position, 40f, 30f);
            //myBlip.Sprite = BlipSprite.Destination;

            myBlipArea.Alpha = 0.45f;
            //myBlip.Scale = 5.0f;



            //}
            //else
            //{

            //}
            //this.pursuit = Functions.CreatePursuit();
            //Functions.AddPedToPursuit(this.pursuit, this.myPed);
            Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information where wanted criminal works. Go there and arrest him.");
            Game.DisplaySubtitle("Go to marked area and arrest wanted criminal.", 9000);
            myPed.Tasks.StandStill(1000);
            Functions.PlayScannerAudio("ADAM_4_COPY");
            
            
            if (wep_chance > 50 && wep_chance < 95) // chance to get intel about weapons is slightly lower than real possibility
            {
                Game.DisplayNotification("~b~ Control : ~w~ Suspect is in posession of ~y~ small firearms ~w~ . Be advised.");
                Functions.PlayScannerAudio("SUSPECT_IS");
                Functions.PlayScannerAudio("SMALL_ARMS");
            }
            else if (wep_chance >= 95)
            {
                Game.DisplayNotification("~b~ Control ~w~ : Suspect is ~r~ heavily armed ~w~ and dangerous. Be advised.");
                Functions.PlayScannerAudio("SUSPECT_IS");
                Functions.PlayScannerAudio("HEAVILY_ARMED_DANGEROUS");
            }
            else // sometimes, in 10% situations suspect is armed, but player shouldnt know about it - SURPRISE.
            {
                Game.DisplayNotification("~b~ Control ~w~ : We have ~b~ no intel ~w~ about possible firearms posession by suspect.");
            }

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (myPed.Exists()) myPed.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();
            /*if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 3000f)
            {
                timeout_is_on = true;
            }*/
            if (ArrestWarrantClass.option_enable_dispatch > 0)
            {
                if (from_pos.DistanceTo(myPed.Position) > 80f)
                {
                    if (myBlipArea.Exists())
                    {
                        myBlipArea.Position = myPed.Position;
                        from_pos = myPed.Position;

                        Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION", from_pos);
                    }
                }
            }
            if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 5 && !player_interacting)
            {
                //myPed.Tasks.Clear();
                //myPed.Dismiss();
                player_interacting = true;
            }

            if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 30 && !blip_attached)
            {
                if (myBlipArea.Exists()) myBlipArea.Delete();
                myBlip = myPed.AttachBlip();
                myBlip.Color = System.Drawing.Color.Red;
                myBlip.RouteColor = System.Drawing.Color.Red;
                //myPed.KeepTasks = false; // so they can get out of car when player closes or do anything
                blip_attached = true;
                myPed.Tasks.Wander();
                Functions.PlayScannerAudio("SUSPECT_LOCATED_ENGAGE");
            }
            if (!pursuit_created && !fight_started && myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 30)
            {
                if (!myPed.IsInAnyVehicle(true))
                {
                    if (r_chance >= 90 && r_chance < 100) // fight with player
                    {
                        dialog_phase = 5;
                        //if (!Game.LocalPlayer.Character.IsInAnyVehicle(true))
                        //{
                        if (wep_chance > 40 && wep_chance < 95)
                        {
                            WeaponAsset w_ass = new WeaponAsset(bronie.get_pistol());
                            myPed.Inventory.GiveNewWeapon(w_ass, 25, true);
                        }
                        else if (wep_chance >= 95)
                        {
                            WeaponAsset w_ass = new WeaponAsset(bronie.get_rifle());
                            myPed.Inventory.GiveNewWeapon(w_ass, 100, true);
                            myPed.Armor = 50;
                        }
                        myPed.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        fight_started = true;
                        //}

                    }



                }
                if (r_chance >= 0 && r_chance < 20) // initiate pursuit.
                {
                    dialog_phase = 5;
                    if (wep_chance > 40 && wep_chance < 95)
                    {
                        WeaponAsset w_ass = new WeaponAsset(bronie.get_pistol());
                        myPed.Inventory.GiveNewWeapon(w_ass, 25, true);
                    }
                    else if (wep_chance >= 95)
                    {
                        WeaponAsset w_ass = new WeaponAsset(bronie.get_rifle());
                        myPed.Inventory.GiveNewWeapon(w_ass, 100, true);
                        myPed.Armor = 50;
                    }
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, myPed);
                    pursuit_created = true;
                }
                if (r_chance >= 20 && r_chance < 90) // talk to suspect
                {
                    if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 10)
                    {
                        if (!info_displayed)
                        {
                            Game.DisplaySubtitle("Talk to suspect ( ~b~ T key ~w~ ), ask for his ID, inform about arrest warrant, and perform arrest.", 9000);
                            info_displayed = true;
                        }
                        if (dialog_phase == 1)
                        {
                            Game.DisplaySubtitle("Get suspect's ID as usual (using built-in LSPDFR ~b~ E key ~w~) and arrest him if he/she is wanted.", 3000);

                        }
                        if (Game.IsKeyDown(System.Windows.Forms.Keys.T))
                        {
                            if (dialog_phase == 0)
                            {
                                Persona crim_presona = Functions.GetPersonaForPed(myPed);
                                //Game.DisplaySubtitle("Hello, My name is " + ArrestWarrantClass.option_player_name + " from Los Santos Police Department. I'm looking for " + crim_presona.Forename + " " + crim_presona.Surname + ".", 3000);
                                myPed.Face(Game.LocalPlayer.Character.Position);
                                //Game.DisplaySubtitle("~y~" + crim_presona.Forename + ": ~w~ Well... that's me.... I think....", 2000);
                                myPed.Tasks.StandStill(2000);
                                dialog_phase = 1;

                            }

                        }
                    }
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.E))
                    {
                        dialog_phase = 2;
                    }
                    if (myPed.IsValid())
                    {
                        if (myPed.IsCuffed)
                        {
                            dialog_phase = 3;
                        }
                        if (myPed.IsDead)
                        {
                            dialog_phase = 4;
                        }
                    }
                    if (dialog_phase == 3)
                    {
                        Game.DisplaySubtitle("YOU : You have the right to remain silent. ... Yeah... You know this formula aren't You ?", 3000);
                        dialog_phase = 5;
                    }
                    if (dialog_phase == 4)
                    {
                        Game.DisplaySubtitle("YOU : Another one... What the hell now I should to write in report?!", 3000);
                        dialog_phase = 5;
                    }

                }


            }
            
            
            //A simple check, if our pursuit has ended we end the callout
            if (myPed.IsValid())
            {
                if (myPed.IsCuffed)
                {
                    if (!got_arrested_notf)
                    {
                        /*if (r_chance >= 0 && r_chance < 10)
                        {
                            if (!Functions.IsPursuitStillRunning(pursuit))
                            {
                                Game.DisplayNotification("1-ADAM-12 : To Control, Suspect is in custody.");
                                Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
                                got_arrested_notf = true;
                            }
                        }
                        else
                        {*/
                        Game.DisplayNotification("~b~ " + ArrestWarrantClass.option_player_name + " ~w~ : To Control, Suspect is in custody.");
                        Functions.PlayScannerAudio("10_15_SUSPECT_IN_CUSTODY");
                        Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                        Functions.PlayScannerAudio("ADAM_4_COPY");
                        Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        got_arrested_notf = true;
                        //}
                    }

                }
                if (myPed.IsDead)
                {
                    if (!got_arrested_notf)
                    {
                        /*if (r_chance >= 0 && r_chance < 10)
                        {
                            if (!Functions.IsPursuitStillRunning(pursuit))
                            {
                                Game.DisplayNotification("1-ADAM-12 : To Control, Suspect is in custody.");
                                Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
                                got_arrested_notf = true;
                            }
                        }
                        else
                        {*/
                        Game.DisplayNotification("~b~ " + ArrestWarrantClass.option_player_name + " ~w~ : To Control, Suspect is in custody.");
                        Functions.PlayScannerAudio("10_15_SUSPECT_IN_CUSTODY");
                        Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                        Functions.PlayScannerAudio("ADAM_4_COPY");
                        Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        got_arrested_notf = true;
                        //}
                    }

                }
                
                /*else if ((myPed.Position.DistanceTo(airport_pos)) < 3f || (myPed.Position.DistanceTo(seaport_pos) < 3f))
                {
                    Game.DisplayNotification("Control : Suspect has escaped.");
                    Game.DisplayNotification("1-ADAM-12 : Acknowledged. 10-4 on my location.");
                    Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
                    myPed.Dismiss();
                    this.End();
                
                }*/
                else if (timeout_is_on)
                {
                    Game.DisplayNotification("~b~ " + ArrestWarrantClass.option_player_name + " ~w~ : We have lost track of suspect.");
                    Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                    myPed.Dismiss();
                    this.End();

                }
            }
            if (myPed.IsValid())
            {
                if (got_arrested_notf == true && myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 80f)
                {
                    this.End();
                }
            }
            if (!myPed.Exists())
            {
                this.End();
            }

        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            base.End();
            if (myBlipArea.Exists()) myBlipArea.Delete();
            if (myBlip.Exists()) myBlip.Delete();
            if (myPed.Exists()) myPed.Delete();


        }
        private Vector3 CreateWantedPedLoc(int rand)
        {
            Vector3 s_point = new Vector3(0, 0, 0);
            if (rand > 0 && rand < 20) // boat shop
            {
                s_point = new Vector3(392.0f,-1162.0f,29.0f);
            }
            else if (rand >= 20 && rand < 40)// // bus depot
            {
                s_point = new Vector3(500.0f,-634.0f,24.0f);
            }
            else if (rand >= 60 && rand < 80) // hairdresser
            {
                s_point = new Vector3(-32.0f, -153.0f, 57.0f);
            }
            else if (rand >= 80 && rand <= 100) // mount chilliad rail bike shop
            {
                s_point = new Vector3(-773.0f, 5596.0f, 33.0f);
            }
            else
            {
                //do nothing
            }


            return s_point;
        }
        
        

        

    }
}
