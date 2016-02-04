using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArrestWarrantCallout
{
    public class CommonFunctions
    {
        
        public static String getVarVehModel()
        {
            String veh_model = "DUKES2";
            Random veh_var = new Random();
            int veh_var_mod = veh_var.Next(1, 100);
            if (veh_var_mod > 0 && veh_var_mod < 20)
            {
                veh_model = "DUKES2";
            }
            else if (veh_var_mod >= 20 && veh_var_mod < 40)
            {
                veh_model = "BLISTA";
            }
            else if (veh_var_mod >= 40 && veh_var_mod < 60)
            {
                veh_model = "BUFFALO";
            }
            else if (veh_var_mod >= 60 && veh_var_mod < 80)
            {
                veh_model = "BURRITO3";
            }
            else if (veh_var_mod >= 80 && veh_var_mod < 101)
            {
                veh_model = "DILETTANTE";
            }
            return veh_model;
        }
        public static String getVarPedModel()
        {
            Random ped_var = new Random();
            int ped_var_mod = ped_var.Next(1, 100);
            String ped_model = "a_m_y_mexthug_01";
            if (ped_var_mod > 0 && ped_var_mod < 10)
            {
                ped_model = "a_m_y_mexthug_01";
            }
            else if (ped_var_mod >= 10 && ped_var_mod < 20)
            {
                ped_model = "a_f_y_hipster_01";
            }
            else if (ped_var_mod >= 20 && ped_var_mod < 30)
            {
                ped_model = "a_f_y_runner_01";
            }
            else if (ped_var_mod >= 30 && ped_var_mod < 40)
            {
                ped_model = "a_f_y_topless_01";
            }
            else if (ped_var_mod >= 40 && ped_var_mod < 50)
            {
                ped_model = "a_m_y_business_03";
            }
            else if (ped_var_mod >= 50 && ped_var_mod < 60)
            {
                ped_model = "a_m_y_cyclist_01";
            }
            else if (ped_var_mod >= 60 && ped_var_mod < 70)
            {
                ped_model = "a_m_y_gay_01";
            }
            else if (ped_var_mod >= 70 && ped_var_mod < 80)
            {
                ped_model = "a_m_y_hippy_01";
            }
            else if (ped_var_mod >= 80 && ped_var_mod < 101)
            {
                ped_model = "a_m_y_skater_01";
            }
            return ped_model;
        }
        



    }
    public class bronie
    {
        public static string get_pistol()
        {
            return "WEAPON_PISTOL";
        }
        public static string get_rifle()
        {
            return "WEAPON_ASSAULTRIFLE";
        }
    }
}
