using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using LemonUI;
using LemonUI.TimerBars;

public class WeaponsCleaning : Script
{
    int player_reload_now = 0;
    static int all_guns = 61;
    WeaponHash[] weapon_hashes = new WeaponHash[all_guns];
    int[] count_reloads = new int[all_guns];
    float[] weapon_wear = new float[all_guns];
    Vector3[] ammo_pos = new Vector3[11];
    int kits_inventory;
    static string path = @".\scripts\WeaponsCleaning.ini";
    ScriptSettings config = ScriptSettings.Load(path);
    private ObjectPool pool_main = new ObjectPool();
    private ObjectPool pool_kits = new ObjectPool();
    private TimerBarCollection bar_pool_main = new TimerBarCollection();
    private TimerBarCollection bar_pool_kits = new TimerBarCollection();
    private static readonly TimerBarProgress condition = new TimerBarProgress("WEAPON CONDITION") { Progress = 0.0f };
    private static readonly TimerBar kits = new TimerBar("WEAPON CLEANING KITS", "0");

    public WeaponsCleaning()
    {

        for (int i = 0; i <= 60; i++)
        {
            string hashcode = config.GetValue<string>($"GUN_{i}", "weapon_hashes", "empty_slot");
            weapon_wear[i] = config.GetValue<float>($"GUN_{i}", "weapon_wear", 1.0f);
            weapon_hashes[i] = Function.Call<WeaponHash>(Hash.GET_HASH_KEY, hashcode);
        }

        kits_inventory = config.GetValue<int>($"GUN_KITS", "inventory", 0);
        condition.BackgroundColor = Color.Gray;
        condition.ForegroundColor = Color.White;
        string kits_update = kits_inventory.ToString();
        kits.Info = kits_update;
        bar_pool_main.Add(condition);
        bar_pool_main.Add(kits);
        bar_pool_kits.Add(kits);
        pool_main.Add(bar_pool_main);
        pool_kits.Add(bar_pool_kits);

        ammo_pos[0] = new Vector3(1696.247f, 3757.441f, 34.70534f);
        ammo_pos[1] = new Vector3(248.7674f, -48.84159f, 69.94105f);
        ammo_pos[2] = new Vector3(842.2771f, -1029.98f, 28.19486f);
        ammo_pos[3] = new Vector3(-327.8542f, 6081.387f, 31.45479f);
        ammo_pos[4] = new Vector3(-662.149f, -938.6592f, 21.82922f);
        ammo_pos[5] = new Vector3(-1309.105f, -393.5137f, 36.6958f);
        ammo_pos[6] = new Vector3(-1115.285f, 2695.835f, 18.55414f);
        ammo_pos[7] = new Vector3(-3168.695f, 1086.431f, 20.83873f);
        ammo_pos[8] = new Vector3(2567.811f, 297.624f, 108.7349f);
        ammo_pos[9] = new Vector3(20.87156f, -1110.625f, 29.79703f);
        ammo_pos[10] = new Vector3(810.0795f, -2153.798f, 29.61901f);

        Tick += OnTick;
        KeyUp += onkeyup;
        KeyDown += onkeydown;
    }

    private void OnTick(object sender, EventArgs e)
    {
        int index_weapon = 0;
        foreach (WeaponHash weapon in weapon_hashes)
        {
            WeaponHash current_weapon = Function.Call<WeaponHash>(Hash.GET_SELECTED_PED_WEAPON, Game.Player.Character);
            if (current_weapon == weapon && Function.Call<bool>(Hash.IS_PED_RELOADING, Game.Player.Character) && player_reload_now == 0)
            {
                count_reloads[index_weapon]++;
                player_reload_now = 1;
            }
            if (count_reloads[index_weapon] >= 10)
            {
                count_reloads[index_weapon] = 0;
                if (weapon_wear[index_weapon] > 0.1)
                {
                    weapon_wear[index_weapon] -= 0.1f;
                }
                config.SetValue($"GUN_{index_weapon}", "weapon_wear", weapon_wear[index_weapon]);
                config.Save();
            }
            Function.Call(Hash.SET_WEAPON_DAMAGE_MODIFIER, weapon, weapon_wear[index_weapon]);
            index_weapon++;
        }

        if (!Function.Call<bool>(Hash.IS_PED_RELOADING, Game.Player.Character) && player_reload_now == 1)
        {
            player_reload_now = 0;
        }

        if (Function.Call<bool>(Hash.IS_HUD_COMPONENT_ACTIVE, 19))
        {
            WeaponHash current_weapon = Function.Call<WeaponHash>(Hash.HUD_GET_WEAPON_WHEEL_CURRENTLY_HIGHLIGHTED);
            float progress_bar = 0.0f;
            for (int i = 0; i <= 60; i++)
            {

                if (weapon_hashes[i] != current_weapon) continue;
                pool_main.Process();
                progress_bar = weapon_wear[i] * 100.0f;
                condition.Progress = progress_bar;

                if (Function.Call<bool>(GTA.Native.Hash.IS_CONTROL_PRESSED, 0, 105) && weapon_wear[i] < 1.0)
                {
                    if (kits_inventory >= 1)
                    {
                        weapon_wear[i] += 0.01f;
                        if (weapon_wear[i] > 1.0)
                        {
                            weapon_wear[i] = 1.0f;
                            kits_inventory -= 1;
                            config.SetValue($"GUN_KITS", "inventory", kits_inventory);
                            string kits_update = kits_inventory.ToString();
                            kits.Info = kits_update;
                        }
                        config.SetValue($"GUN_{i}", "weapon_wear", weapon_wear[i]);
                        config.Save();
                    }
                    else
                    {
                        GTA.UI.Screen.ShowHelpText("You don't have enough Gun Cleaning Kits!", -1, false, false);
                    }
                }
            }
        }

        for (int i = 0; i <= 10; i++)
        {
            if (World.GetDistance(Game.Player.Character.Position, ammo_pos[i]) < 1.5)
            {
                string kits_update = kits_inventory.ToString();
                kits.Info = kits_update;
                pool_kits.Process();
                GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_PICKUP~ to purchase gun oil ($500).");
            }
        }

    }

    private void onkeyup(object sender, KeyEventArgs e)
    {
        for (int i = 0; i <= 10; i++)
        {
            if (World.GetDistance(Game.Player.Character.Position, ammo_pos[i]) < 1.5 && e.KeyCode == Keys.E)
            {
                GTA.UI.Screen.ShowSubtitle("~r~sus");
                if (kits_inventory < 10)
                {
                    kits_inventory++;
                    Game.Player.Money -= 500;
                }
                else
                {
                    GTA.UI.Screen.ShowSubtitle("~r~You have a full set of gun oil!");
                }
            }
        }
    }

    private void onkeydown(object sender, KeyEventArgs e)
    {

    }
}
