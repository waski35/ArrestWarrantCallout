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
    [CalloutInfo("PrisonEscape", CalloutProbability.High)]

    class PrisonEscape : Callout
    {
        //Here we declare our variables, things we need or our callout
        private Vehicle myVehicle; // a rage vehicle
        private Ped myPed; // a rage ped
        private Ped myPed2;
        private Vector3 SpawnPoint; // a Vector3
        private Blip myBlip; // a rage blip
        private Blip myBlip2;
        private Blip myBlipArea;
        private LHandle pursuit; // an API pursuit handle
        private int rand_num = 0;
        private Vector3 airport_pos;
        private Vector3 seaport_pos;
        private Vector3 random_escape_pos;
        private bool timeout_is_on = false;
        private int r_chance = 0;
        private bool fight_started = false;
        private int wep_chance = 0;
        private bool got_arrested_notf = false;
        private bool pursuit_created = false;

        private Group myGroup;


        private Vector3 from_pos;
        private bool blip_attached = false;
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



            random_escape_pos = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(900f));
            airport_pos = new Vector3(Convert.ToSingle(-1029.346), Convert.ToSingle(-2499.977), Convert.ToSingle(19.704)); // set airport pos
            seaport_pos = new Vector3(Convert.ToSingle(1181.485), Convert.ToSingle(-3099.899), Convert.ToSingle(5.43373)); // set seaport pos

            //Create our ped in the world
            myPed = new Ped(CommonFunctions.getVarPedModel(), SpawnPoint, 0f);
            myPed.KeepTasks = true;
            myPed.MakePersistent();
            DateTime birthday = new DateTime(1971, 01, 23);
            Persona crim_presona_old = Functions.GetPersonaForPed(myPed);

            Persona crim_persona_new = new Persona(myPed, crim_presona_old.Gender, crim_presona_old.BirthDay, 5, crim_presona_old.Forename, crim_presona_old.Surname, crim_presona_old.LicenseState, crim_presona_old.TimesStopped, true, false, false);
            Functions.SetPersonaForPed(myPed, crim_persona_new);

            //Create our ped in the world
            myPed2 = new Ped(CommonFunctions.getVarPedModel(), SpawnPoint, 0f);
            myPed2.KeepTasks = true;
            myPed2.MakePersistent();
            DateTime birthday2 = new DateTime(1971, 01, 23);
            Persona crim_presona_old2 = Functions.GetPersonaForPed(myPed2);

            Persona crim_persona_new2 = new Persona(myPed2, crim_presona_old2.Gender, crim_presona_old2.BirthDay, 5, crim_presona_old2.Forename, crim_presona_old2.Surname, crim_presona_old2.LicenseState, crim_presona_old2.TimesStopped, true, false, false);
            Functions.SetPersonaForPed(myPed2, crim_persona_new2);

            //Create the vehicle for our ped
            myVehicle = new Vehicle(CommonFunctions.getVarVehModel(), SpawnPoint);
                if (!myVehicle.Exists()) return false;
           


            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!myPed.Exists()) return false;
            if (!myPed2.Exists()) return false;

            myGroup = new Group(myPed);
            myGroup.AddMember(myPed2);


            if (myPed.Position.DistanceTo(airport_pos) < 200f && rand_num >= 10 && rand_num < 40)
            {
                return false;
            }
            if (myPed.Position.DistanceTo(seaport_pos) < 200f && rand_num >= 40 && rand_num < 80)
            {
                return false;
            }


            //If we made it this far both exist so let's warp the ped into the driver seat
            if (myVehicle.Exists())
            {
                myPed.WarpIntoVehicle(myVehicle, -1);
                myPed2.WarpIntoVehicle(myVehicle, 0);
            }
            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);


            // Set up our callout message and location
            this.CalloutMessage = "Prison Escape";
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
            myBlipArea = new Blip(myPed.Position, 90f);
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
            Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + "  ~w~ : We have wanted criminal escaping from county prison");
            Game.DisplaySubtitle("Go to marked area and arrest wanted criminals.", 9000);
            if (rand_num >= 0 && rand_num < 20) // fleeing to airport
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspects are fleeing to airport.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_HEADING IN_OR_ON_POSITION RESPOND_CODE_3", airport_pos);
                //Functions.PlayScannerAudio("RESPOND_CODE_3");
                myPed.Tasks.DriveToPosition(airport_pos, 30, VehicleDrivingFlags.FollowTraffic);

                
            }
            else if (rand_num >= 20 && rand_num < 40) // fleeing to seaport
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspects are fleeing to seaport.");
                Functions.PlayScannerAudioUsingPosition("SUSPECT_HEADING IN_OR_ON_POSITION RESPOND_CODE_3", seaport_pos);
                //Functions.PlayScannerAudio("RESPOND_CODE_3");
                myPed.Tasks.DriveToPosition(seaport_pos, 30, VehicleDrivingFlags.FollowTraffic);
                

            }
            else // fleeing to random position
            {
                Game.DisplayNotification("~b~ Control to " + ArrestWarrantClass.option_player_name + " ~w~ : We have information that suspects are fleeing to unknown location.");
                Functions.PlayScannerAudio("RESPOND_CODE_3");
                myPed.Tasks.DriveToPosition(random_escape_pos, 30, VehicleDrivingFlags.FollowTraffic);
            }
            
            if (wep_chance > 20 && wep_chance < 75) // chance to get intel about weapons is slightly lower than real possibility
            {
                Game.DisplayNotification("~b~ Control ~w~ : Suspects are in posession of small firearms. Be advised.");
                Functions.PlayScannerAudio("SUSPECT_IS SMALL_ARMS");
                //Functions.PlayScannerAudio("SMALL_ARMS");
            }
            else if (wep_chance >= 75)
            {
                Game.DisplayNotification("~b~ Control ~w~ : Suspects are heavily armed and dangerous. Be advised.");
                Functions.PlayScannerAudio("SUSPECT_IS HEAVILY_ARMED_DANGEROUS");
                //Functions.PlayScannerAudio("HEAVILY_ARMED_DANGEROUS");
            }
            else // sometimes, in 10% situations suspect is armed, but player shouldnt know about it - SURPRISE.
            {
                Game.DisplayNotification("~b~ Control ~w~ : We have no intel about possible firearms posession by suspects.");
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
            if (myPed2.Exists()) myPed2.Delete();
            if (myVehicle.Exists()) myVehicle.Delete();
            if (myBlip.Exists()) myBlip.Delete();
            if (myBlip2.Exists()) myBlip2.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();
            /*if (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 3000f)
            {
                timeout_is_on = true;
            }*/
            /*if (!pursuit_created)
            {
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, myPed);
                Functions.AddPedToPursuit(pursuit, myPed2);
                Functions.RequestBackup(myPed.Position.Around(90f), LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
                pursuit_created = true;
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

            if ((myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50 || myPed2.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50) && !blip_attached)
            {
                if (myBlipArea.Exists()) myBlipArea.Delete();
                /*if (myPed.Exists())
                {
                    myBlip = myPed.AttachBlip();
                    myBlip.Color = System.Drawing.Color.Red;
                    //myBlip.RouteColor = System.Drawing.Color.Red;
                    myPed.KeepTasks = false;
                }
                if (myPed2.Exists())
                {
                    myBlip2 = myPed2.AttachBlip();
                    myBlip2.Color = System.Drawing.Color.Red;
                    //myBlip2.RouteColor = System.Drawing.Color.Red;
                    myPed2.KeepTasks = false;
                }*/
                Functions.PlayScannerAudio("OFFICER_INTRO SUSPECT_LOCATED_ENGAGE");
                if (!pursuit_created)
                {
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, myPed);
                    Functions.AddPedToPursuit(pursuit, myPed2);
                    Functions.RequestBackup(World.GetNextPositionOnStreet(myPed.Position).Around(15f), LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
                    pursuit_created = true;
                }
                blip_attached = true;
            }

            if (!fight_started && (myPed.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50 || myPed2.Position.DistanceTo(Game.LocalPlayer.Character.Position) < 50))
            {
                    if (!myPed.IsInAnyVehicle(true) || !myPed2.IsInAnyVehicle(true))
                    {
                        if (r_chance >= 5 && r_chance < 100)
                        {
                            //if (!Game.LocalPlayer.Character.IsInAnyVehicle(true))
                            //{
                            if (wep_chance > 10 && wep_chance < 75)
                            {
                                WeaponAsset w_ass = new WeaponAsset(bronie.get_pistol());
                                myPed.Inventory.GiveNewWeapon(w_ass, 25, true);
                                myPed2.Inventory.GiveNewWeapon(w_ass, 25, true);
                            }
                            else if (wep_chance >= 75)
                            {
                                WeaponAsset w_ass = new WeaponAsset(bronie.get_rifle());
                                myPed.Inventory.GiveNewWeapon(w_ass, 100, true);
                                myPed.Armor = 50;
                                myPed2.Inventory.GiveNewWeapon(w_ass, 100, true);
                                myPed2.Armor = 50;
                            }
                            myPed.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            myPed2.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            fight_started = true;
                            //}

                        }


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
                if (rand_num >= 0 && rand_num < 20)
                {
                    if (myPed.Position.DistanceTo(airport_pos) < 50)
                    {
                        if (myPed.IsInAnyVehicle(true))
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            myPed2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            
                            myPed.Tasks.Wander();
                            myPed2.Tasks.Wander();
                            if (myBlipArea.Exists())
                            {
                                myBlipArea.Position = myPed.Position;
                                from_pos = myPed.Position;

                                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION", from_pos);
                            }

                        }
                    }
                }
                if (rand_num >= 20 && rand_num < 40)
                {
                    if (myPed.Position.DistanceTo(seaport_pos) < 50)
                    {
                        if (myPed.IsInAnyVehicle(true) && !player_interacting)
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            myPed2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            
                            myPed.Tasks.Wander();
                            myPed2.Tasks.Wander();
                            if (myBlipArea.Exists())
                            {
                                myBlipArea.Position = myPed.Position;
                                from_pos = myPed.Position;

                                Functions.PlayScannerAudioUsingPosition("SUSPECT_LAST_SEEN IN_OR_ON_POSITION", from_pos);
                            }

                        }
                    }
                }
                if (rand_num >= 40 && rand_num < 101)
                {
                    if (myPed.Position.DistanceTo(random_escape_pos) < 50)
                    {
                        if (myPed.IsInAnyVehicle(true) && !player_interacting)
                        {
                            myPed.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            myPed2.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);

                            myPed.Tasks.Wander();
                            myPed2.Tasks.Wander();
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
            if (myPed.IsValid() && myPed2.IsValid())
            {
                if (myPed.IsCuffed || myPed2.IsCuffed)
                {
                    if (!got_arrested_notf)
                    {

                        Game.DisplayNotification(ArrestWarrantClass.option_player_name + " : To Control, Suspect is in custody.");
                        Functions.PlayScannerAudio("ALL_CRIMS_IN_CUSTODY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO");
                        Game.DisplayNotification("Control : Acknowledged. ");
                        //Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        //Functions.PlayScannerAudio("ADAM_4_COPY");
                        got_arrested_notf = true;
                    }

                }
                if (myPed.IsDead || myPed2.IsDead)
                {
                    if (!got_arrested_notf)
                    {

                        Game.DisplayNotification(ArrestWarrantClass.option_player_name + " : To Control, Suspect is in custody.");
                        Functions.PlayScannerAudio("ALL_CRIMS_IN_CUSTODY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO");
                        Game.DisplayNotification("Control : Acknowledged. ");
                        //Functions.PlayScannerAudio("CODE_4_ADAM_NO_ADDITIONAL");
                        //Functions.PlayScannerAudio("ADAM_4_COPY");
                        got_arrested_notf = true;
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
                    Game.DisplayNotification(ArrestWarrantClass.option_player_name + " : We have lost track of suspect.");

                    Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
                    Functions.PlayScannerAudio("ALL_CRIMS_IN_CUSTODY OUTRO OFFICER_INTRO CODE_4_ADAM_NO_ADDITIONAL OUTRO OFFICER_INTRO ADAM_4_COPY OUTRO");
                    //Functions.PlayScannerAudio("ADAM_4_COPY");
                    myPed.Dismiss();
                    myPed2.Dismiss();
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
            if (myPed2.IsValid())
            {
                if (got_arrested_notf == true && myPed2.Position.DistanceTo(Game.LocalPlayer.Character.Position) > 100f)
                {
                    this.End();
                }
            }
            if (!myPed.Exists() && !myPed2.Exists())
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
            if (myBlip2.Exists()) myBlip2.Delete();
            if (myPed.Exists()) myPed.Delete();
            if (myPed2.Exists()) myPed2.Delete();
            if (myGroup.Exists()) myGroup.Delete();


        }
        private Vector3 CreateWantedPedLoc(int rand)
        {
            Vector3 s_point = new Vector3(1994.899f, 2651.14f, 46.34293f); // prison
            //Vector3 vect = new Vector3();
            //vect = PickPrisonLocation();
            //s_point = World.GetNextPositionOnStreet(vect.Around(30f));
            
            return s_point;
        }
        private Vector3 PickPrisonLocation()
        {
            Vector3 ret = new Vector3(0, 0, 0);
            Random random_m = new Random();
            int rand_moun = 1;// random_m.Next(1, 5);
            switch (rand_moun)
            {
                case 1:
                    ret.X = 1994.899f;
                    ret.Y = 2651.14f;
                    ret.Z = 46.34293f;
                    break;
                
                default:
                    ret.X = 1994.899f;
                    ret.Y = 2651.14f;
                    ret.Z = 46.34293f;
                    break;
            }
            return ret;
        }
        

    }
}