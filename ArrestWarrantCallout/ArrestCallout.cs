using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;

namespace ArrestWarrantCallout
{
    class ArrestCallout : Callout
    {
        //Here we declare our variables, things we need or our callout
        private Vehicle myVehicle; // a rage vehicle
        private Ped myPed; // a rage ped
        private Vector3 SpawnPoint; // a Vector3
        private Blip myBlip; // a rage blip
        private LHandle pursuit; // an API pursuit handle
        private int rand_num = 0;
        private Vector3 airport_pos;
        private Vector3 seaport_pos;
        private bool timeout_is_on = false

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
            int rand_num = random_number.Next(0, 100);
            SpawnPoint = CreateWantedPedLoc(rand_num);
            airport_pos = new Vector3(0, 0, 0);
            seaport_pos = new Vector3(0, 0, 0);
            //Create our ped in the world
            myPed = new Ped("a_m_y_mexthug_01", SpawnPoint, 0f);

            //Create the vehicle for our ped
            if (rand_num > 10 && rand_num < 30)
            {
                myVehicle = new Vehicle("DUKES2", SpawnPoint);
            }
            //Now we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!myPed.Exists()) return false;
            if (!myVehicle.Exists()) return false;

            //If we made it this far both exist so let's warp the ped into the driver seat
            if (rand_num > 10 && rand_num < 30)
            {
                myPed.WarpIntoVehicle(myVehicle, -1);
            }
            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            this.AddMinimumDistanceCheck(5f, myPed.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Arrest Warrant in Progress";
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout (available as of the 0.2a API)
            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT CRIME_RESIST_ARREST IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our ped so we know where he is.
            myBlip = myPed.AttachBlip();
            //this.pursuit = Functions.CreatePursuit();
            //Functions.AddPedToPursuit(this.pursuit, this.myPed);
            Game.DisplayNotification("Control to 1-ADAM-12 : We have wanted criminal arrest warrant, proceed to marked location and arrest person.");
            Game.DisplaySubtitle("Go to marked area and arrest wanted criminal.",5000);
            if (rand_num > 0 && rand_num < 10) // waiting at home
            {
                Game.DisplayNotification("Control to 1-ADAM-12 : We have information that suspect is unaware about Your arrest warrant.");
                myPed.Tasks.StandStill(60000);
            }
            else if (rand_num >= 10 && rand_num < 40) // fleeing to airport
            {
                Game.DisplayNotification("Control to 1-ADAM-12 : We have information that suspect is fleeing to airport.");
                if (rand_num >= 10 && rand_num < 30)
                {
                    myPed.Tasks.DriveToPosition(airport_pos, 35, DriveToPositionFlags.RespectVehicles);
                    myPed.Tasks.StandStill(60000);
                }
                else
                {
                    myPed.Tasks.FollowNavigationMeshToPosition(airport_pos, 0, 10, 12000);
                    myPed.Tasks.StandStill(60000);
                }
            }
            else if (rand_num >= 40  && rand_num < 80) // fleeing to seaport
            {
                Game.DisplayNotification("Control to 1-ADAM-12 : We have information that suspect is fleeing to seaport.");
                if (rand_num >= 40 && rand_num < 70)
                {
                    myPed.Tasks.DriveToPosition(seaport_pos, 35, DriveToPositionFlags.RespectVehicles);
                    myPed.Tasks.StandStill(60000);
                }
                else
                {
                    myPed.Tasks.FollowNavigationMeshToPosition(seaport_pos, 0, 10, 12000);
                    myPed.Tasks.StandStill(60000);
                }
          
            }
            else // hiding in mouintains
            {
                Game.DisplayNotification("Control to 1-ADAM-12 : We have information that suspect is hiding in mountains.");
                myPed.Tasks.Wander();
          
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
            if (myVehicle.Exists()) myVehicle.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();
            
            


            //A simple check, if our pursuit has ended we end the callout
            if (myPed.IsDead || Functions.IsPedArrested(myPed))
            {
                Game.DisplayNotification("1-ADAM-12 : To Control, Suspect is in custody. 10-4.");
                Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
                this.End();
            }
            else if ((myPed.Position == airport_pos) || (myPed.Position == seaport_pos))
            {
                Game.DisplayNotification("Control : Suspect has escaped.");
                Game.DisplayNotification("1-ADAM-12 : Acknowledged. 10-4 on my location.");
                Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
            }
            else if(timeout_is_on)
            {
                Game.DisplayNotification("Control : We have lost track of suspect.");
                Game.DisplayNotification("1-ADAM-12 : Acknowledged. 10-4 on my location.");
                Game.DisplayNotification("Control : Acknowledged. Proceed with patrol.");
            }

        }

        /// <summary>
        /// More cleanup, when we call end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            base.End();
            if (myBlip.Exists()) myBlip.Delete();
            if (myPed.Exists()) myPed.Delete();
            if (myVehicle.Exists()) myVehicle.Delete();
            
        }
        private Vector3 CreateWantedPedLoc(int rand)
        {
            Vector3 s_point = new Vector3(0, 0, 0);
            if (rand > 0 && rand < 40) //city
            {

            }
            else // county
            {

            }


            return s_point;
        }

    }
}
