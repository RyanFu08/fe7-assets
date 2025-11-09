using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;


public static class AnimLoader
{
    public static int hp_blue, hp_red;
    public static int dmg_blue, dmg_red;
    public static int hit_blue, hit_red;
    public static int crit_blue, crit_red;
    public static string doub;
    public static string blue_weap, red_weap;
    public static string blue_name, red_name;
    public static UnitWrapper blue;
    public static UnitWrapper red;
    public static List<string> actions = new List<string>();
    public static List<string> freezes = new List<string>();
    public static List<string> hit_type = new List<string>();
    public static List<int> damages = new List<int>();
    public static List<int> turns = new List<int>();

    public static int hit_left, dmg_left, crt_left, hp_left, mhp_left, hit_right, dmg_right, crt_right, hp_right, mhp_right;


    public static int DIST;

    public static void Load2(
        int hit_left, int dmg_left, int crt_left, int hp_left, int mhp_left,
        int hit_right, int dmg_right, int crt_right, int hp_right, int mhp_right
    )
    {
        AnimLoader.hit_left = hit_left;
        AnimLoader.dmg_left = dmg_left;
        AnimLoader.crt_left = crt_left;
        AnimLoader.hp_left = hp_left;
        AnimLoader.mhp_left = mhp_left;

        AnimLoader.hit_right = hit_right;
        AnimLoader.dmg_right = dmg_right;
        AnimLoader.crt_right = crt_right;
        AnimLoader.hp_right = hp_right;
        AnimLoader.mhp_right = mhp_right;
    }

    public static void Load(int hp_blue, int hp_red,
                            int dmg_blue, int dmg_red,
                            int hit_blue, int hit_red,
                            int crit_blue, int crit_red,
                            string doub,
                            UnitWrapper blue, UnitWrapper red)
    {
        actions.Clear();
        freezes.Clear();
        hit_type.Clear();
        damages.Clear();
        turns.Clear();

        AnimLoader.hp_blue = hp_blue;
        AnimLoader.hp_red = hp_red;
        AnimLoader.dmg_blue = dmg_blue;
        AnimLoader.dmg_red = dmg_red;
        AnimLoader.hit_blue = hit_blue;
        AnimLoader.hit_red = hit_red;
        AnimLoader.crit_blue = crit_blue;
        AnimLoader.crit_red = crit_red;
        AnimLoader.doub = doub;
        AnimLoader.blue = blue;
        AnimLoader.red = red;

        blue_name = blue.name.ToLower(); Debug.Log(blue_name);
        red_name = red.name.ToLower();

        blue_weap = blue.weap().weapon_type;
        if (blue_weap == "anima" || blue_weap == "light" || blue_weap == "dark") blue_weap = "magic";
        red_weap = red.weap().weapon_type;
        if (red_weap == "anima" || red_weap == "light" || red_weap == "dark") red_weap = "magic";

        Debug.Log("blue: " + blue_weap);
        Debug.Log("red: " + red_weap);


        DIST = Math.Abs(blue.cx - red.cx) / 16 + Math.Abs(blue.cy - red.cy) / 16;
        blue.upd_range();
        red.upd_range();

        AddBlueAction();
        AddRedAction();
        if (doub == "blue") AddBlueAction();
        if (doub == "red") AddRedAction();

        freezes.Add(blue_name + "_" + blue_weap + "_frozen");
        freezes.Add(red_name + "_" + red_weap + "_frozen");

    }

    public static void AddBlueAction()
    {

        if (!blue.range.Contains(DIST)) return;

        if (blue.hp <= 0 || red.hp <= 0) return;

        turns.Add(0);
        freezes.Add(red_name + "_" + red_weap + "_frozen");
        if (rand.roll() <= hit_blue)
        {
            if (rand.roll() <= crit_blue)
            {
                damages.Add(3 * dmg_blue);
                red.hp -= 3 * dmg_blue; red.hp = Math.Max(red.hp, 0);
                hit_type.Add("critical");
                actions.Add(blue_name + "_" + blue_weap + "_crit");
            }
            else
            {
                damages.Add(3*dmg_blue);
                red.hp -= 3*dmg_blue; red.hp = Math.Max(red.hp, 0);
                hit_type.Add("critical");
                actions.Add(blue_name + "_" + blue_weap + "_crit");
            }
        }
        else
        {
            damages.Add(0);
            hit_type.Add("miss");
            actions.Add(blue_name + "_" + blue_weap + "_nocrit");
        }
        Debug.Log(blue.hp);
    }

    public static void AddRedAction()
    {

        if (!red.range.Contains(DIST)) return;

        if (blue.hp <= 0 || red.hp <= 0) return;

        turns.Add(1);
        Debug.Log(red_name);
        freezes.Add(blue_name + "_" + blue_weap + "_frozen");
        if (rand.roll() <= hit_red)
        {
            if (rand.roll() <= crit_red)
            {
                damages.Add(3 * dmg_red);
                blue.hp -= 3 * dmg_red; blue.hp = Math.Max(blue.hp, 0);
                hit_type.Add("critical");
                actions.Add(red_name + "_" + red_weap + "_crit");
            }
            else
            {
                damages.Add(dmg_red);
                blue.hp -= dmg_red; blue.hp = Math.Max(blue.hp, 0);
                if (red_name == "dragon")
                {
                    hit_type.Add("critical");
                }
                else
                    hit_type.Add("nocrit");
                actions.Add(red_name + "_" + red_weap + "_nocrit");
            }
        }
        else
        {
            damages.Add(0);
            hit_type.Add("miss");
            actions.Add(red_name + "_" + red_weap + "_nocrit");
        }
        Debug.Log(red.hp);
    }

    public static void Go()
    {
        SceneManager.LoadScene("BattleAnim");
    }

}
