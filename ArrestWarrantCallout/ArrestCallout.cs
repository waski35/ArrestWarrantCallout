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
    [CalloutInfo("ArrestCallout", CalloutProbability.VeryHigh)]

    class ArrestCallout : Callout
    {
        //Here we declare our variables, things we need or our callout
        private Vehicle myVehicle; // a rage vehicle
        private Ped myPed; // a rage ped
        private Vector3 SpawnPoint; // a Vector3
        private Blip myBlip; // a rage blip
        private Blip myBlipArea;
        private LHandle pursuit; // an API pursuit handle
        private int rand_num = 0;
        private Vector3 airport_pos;
        private Vector3 seaport_pos;
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
            rand_num = random_number.Next(1, 100); // chance what suspect will do
            SpawnPoint = CreateWantedPedLoc(rand_num);

            Random purs = new Random();
            r_chance = purs.Next(1, 100); // fight / surrender / pursuit chance

            Random wep = new Random();
            wep_chance = wep.Next(1, 100);

            Random fel = new Random();
            r_felony = fel.Next(1, 5);
            switch(r_felony)
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
            Random weh = new Random();
            weh_chance = weh.Next(51, 100);

            

            airport_pos = new Vector3(Convert.ToSingle(-1029.346), Convert.ToSingle(-2499.977), Convert.ToSingle(19.704)); // set airport pos
            seaport_pos = new Vector3(Convert.ToSingle(1181.485), Convert.ToSingle(-3099.899), Convert.ToSingle(5.43373)); // set seaport pos

            //Create our ped in the world
            myPed = new Ped(CommonFunctions.getVarPedModel(), SpawnPoint, 0f);
            myPed.KeepTasks = true;
            myPed.MakePersistent();
            DateTime birthday = new DateTime(1971,01,23);
            Persona crim_presona_old = Functions.GetPersonaForPed(myPed);
            
            Persona crim_persona_new = new Persona(myPed, crim_presona_old.Gender, crim_presona_old.BirthDay, 5, crim_presona_old.Forename, crim_presona_old.Surname, crim_presona_old.LicenseState, crim_presona_old.TimesStopped, true, false, false);
            Functions.SetPersonaForPed(myPed, crim_persona_new);
            /*
            if (wep_chance > 70)
            {
                WeaponAsset w_ass = new WeaponAsset("WEAPON_PISTOL");
                myPed.GiveNewWeapon(w_ass,25,false);
            }
            else if (wep_chance >= 95)
            {
                WeaponAsset w_ass = new WeaponAsset("WEAPON_ASSAULTRIFLE");
                myPed.GiveNewWeapon(w_ass, 100, false);
                myPed.Armor = 50;
            }
            */
            //Create the vehicle for our ped
            if (weh_chance > 50 && weh_chance < 101)
            {
                myVehicle = new Vehicle(CommonFunctions.getVarVehModel(), SpawnPoint);
                if (!myVehicle.Exists()) return false;
            }
           

            
            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!myPed.Exists()) return false;
            if (myPed.Position.DistanceTo(airport_pos) < 200f && rand_num >=10 && rand_num < 40)
            {
                return false;
            }
            if (myPed.Position.DistanceTo(seaport_pos) < 200f && rand_num >=40 && rand_num < 80)
            {
                return false;
            }
            

            //If we made it this far both exist so let's warp the ped into the driver seat
            if (weh_chance > 50)
            {
                myPed.WarpIntoVehicle(myVehicle, -1);
            }
            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);
            

            // Set up our callout message and location
            this.CalloutMessage = "Arrest Warrant";
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", SpawnPoint);

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
                from_pos = new Vector3(myPed.Position.X,myPed.Position.Y,myPed.Position.Z);
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
            Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have wanted criminal arrest warrant, criminal is wanted for " + felony_s + ".");
            Game.DisplaySubtitle("Go to marked area and arrest wanted criminal.",9000);
            if (rand_num > 0 && rand_num < 10) // waiting at home
            {
                Game.DisplayNotification("~b~ Control to 1-ADAM-12 ~w~ : We have information that suspect is unaware about Your arrest warrant.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION RESPOND_CODE_2", SpawnPoint);
                //Functions.PlayScannerAudio("RESPOND_CODE_2");
                myPed.Tasks.Wander();
            }
            else if (rand_num >= 10 && rand_num < 40) // fleeing to airport
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspect is fleeing to airport.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_HEADING IN_OR_ON_POSITION RESPOND_CODE_3", airport_pos);
                //Functions.PlayScannerAudio("RESPOND_CODE_3");
                if (weh_chance > 50)
                {
                    myPed.Tasks.DriveToPosition(airport_pos, 50, VehicleDrivingFlags.Emergency);
                   
                }
                else
                {
                    myPed.Tasks.FollowNavigationMeshToPosition(airport_pos, 0f, 10f);// not working - susp standing steel
                    
                }
            }
            else if (rand_num >= 40  && rand_num < 80) // fleeing to seaport
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspect is fleeing to seaport.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_HEADING IN_OR_ON_POSITION RESPOND_CODE_3", seaport_pos);
                //Functions.PlayScannerAudio("RESPOND_CODE_3");
                if (weh_chance > 50)
                {
                    myPed.Tasks.DriveToPosition(seaport_pos, 50, VehicleDrivingFlags.Emergency);
                   
                }
                else
                {
                    myPed.Tasks.FollowNavigationMeshToPosition(seaport_pos, 0f, 10f); // not working - susp standing steel
                    
                }
          
            }
            else // hiding in mouintains
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspect is ~y~ hiding in marked area.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION RESPOND_CODE_2", SpawnPoint);
                //Functions.PlayScannerAudio("RESPOND_CODE_2");
                myPed.Tasks.Wander();
          
            }
            if (wep_chance > 50 && wep_chance < 95) // chance to get intel about weapons is slightly lower than real possibility
            {
                Game.DisplayNotification("~b~ Control : ~w~ Suspect is in posession of ~y~ small firearms ~w~ . Be advised.");
                Functions.PlayScannerAudio("OFFICER_INTRO SUSPECT_IS_ARM SMALL_ARMS");
                //Functions.PlayScannerAudio("SMALL_ARMS");
            }
            else if (wep_chance >= 95)
            {
                Game.DisplayNotification("~b~ Control ~w~ : Suspect is ~r~ heavily armed ~w~ and dangerous. Be advised.");
                Functions.PlayScannerAudio("OFFICER_INTRO SUSPECT_IS_ARM HEAVILY_ARMED_DANGEROUS");
                //Functions.PlayScannerAudio("HEAVILY_ARMED_DANGEROUS");
            }
            else // sometimes, in 10% situations suspect is armed, but player shouldnt know about it - SURPRISE.
            {
                Game.DisplayNotification("~b~ Control ~w~ : We have ~b~ no intel ~w~ about possible firearms posession by suspect.");
            }
            Functions.PlayScannerAudio("OFFICER_INTRO ADAM_4_COPY OUTRO");
           
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (myPed.Exists()) myPed.Delete();
            if (myVehicle.Exists()) myVehicle.Delete();
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
                if (from_pos.DistanceTo(myPed.Position) > 700f)
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

            if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50 && !blip_attached)
            {
                if (myBlipArea.Exists()) myBlipArea.Delete();
                myBlip = myPed.AttachBlip();
                myBlip.Color = System.Drawing.Color.Red;
                myBlip.RouteColor = System.Drawing.Color.Yellow;
                myBlip.Scale = 0.75f;
                myPed.KeepTasks = false; // so they can get out of car when player closes or do anything
                blip_attached = true;
                Functions.PlayScannerAudio("OFFICER_INTRO SUSPECT_LOCATED_ENGAGE");
            }
            if (!pursuit_created && !fight_started && myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50)
            {
                    if (!myPed.IsInAnyVehicle(true))
                    {
                        if (r_chance >= 40 && r_chance < 100) // fight with player
                        {
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

                
            }
            /*if (fight_started && !surrenderred)
            {
                if (myPed.Health < 45)
                {
                    if (r_chance > 45 )
                    {
                        if (pursuit_created)
                        {
                           
                        }
                        myPed.Tasks.Clear();
                        if (myPed.IsInAnyVehicle(true))
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);

                        }

                        myPed.Tasks.PutHandsUp(8000, Game.LocalPlayer.Character);
                        surrenderred = true;
                    }
                }
            }*/
            if (!player_interacting || !blip_attached)
            {
                if (rand_num >= 10 && rand_num < 40)
                {
                    if (myPed.Position.DistanceTo(airport_pos) < 50)
                    {
                        if (myPed.IsInAnyVehicle(true))
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            susp_left_car = true;
                            myPed.Tasks.Wander();
                            if (myBlipArea.Exists())
                            {
                                myBlipArea.Position = myPed.Position;
                                from_pos = myPed.Position;

                                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION", from_pos);
                            }
                            
                        }
                    }
                }
                if (rand_num >= 40 && rand_num < 80)
                {
                    if (myPed.Position.DistanceTo(seaport_pos) < 50)
                    {
                        if (myPed.IsInAnyVehicle(true) && !player_interacting)
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            susp_left_car = true;
                            myPed.Tasks.Wander();
                            if (myBlipArea.Exists())
                            {
                                myBlipArea.Position = myPed.Position;
                                from_pos = myPed.Position;

                                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION", from_pos);
                            }
                            
                        }
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
                        Functions.PlayScannerAudio("10_15_SUSPECT_IN_CUSTODY OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO");
                        Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                        //Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        //Functions.PlayScannerAudio("ADAM_4_COPY");
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
                        Functions.PlayScannerAudio("10_15_SUSPECT_IN_CUSTODY OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO");
                        Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                        //Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        //Functions.PlayScannerAudio("ADAM_4_COPY");
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
                    Functions.PlayScannerAudio("10_15_SUSPECT_IN_CUSTODY OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO");
                    Game.DisplayNotification("~b~ Control ~w~ : Acknowledged. Proceed with patrol.");
                    myPed.Dismiss();
                    this.End();

                }
            }
            if (myPed.IsValid())
            {
                if (got_arrested_notf == true && myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 100f)
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
            if (rand > 0 && rand < 40) //city
            {
                s_point = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(200f));
            }
            else if (rand >= 40 && rand < 80)// county
            {
                Vector3 vect = new Vector3();
                vect = PickMountainLocation();
                s_point = World.GetNextPositionOnStreet(vect.Around(700f));
            }
            else if (rand >=80) // mountains
            {
                s_point = PickMountainLocation();
            }


            return s_point;
        }
        private Vector3 PickMountainLocation()
        {
            Vector3 ret = new Vector3(0, 0, 0);
            Random random_m = new Random();
            int rand_moun = random_m.Next(0, 6);
            switch (rand_moun)
            {
                case 1: ret.X = -1019.682f;
                        ret.Y = 4946.673f;
                        ret.Z = 198.5889f;
                        break;
                case 2: ret.X = -58.27036f;
                        ret.Y = 4978.937f;
                        ret.Z = 398.5285f;
                        break;
                case 3: ret.X = 460.409f;
                        ret.Y = 5561.787f;
                        ret.Z = 781.1706f;
                        break;
                case 4: ret.X = -1019.682f;
                        ret.Y = 4946.673f;
                        ret.Z = 198.5889f;
                        break;
                case 5: ret.X = -58.27036f;
                        ret.Y = 4978.937f;
                        ret.Z = 398.5285f;
                        break;
                default: ret.X = 460.409f;
                        ret.Y = 5561.787f;
                        ret.Z = 781.1706f;
                        break;
            }
            return ret;
        }
        

        

    }
}
