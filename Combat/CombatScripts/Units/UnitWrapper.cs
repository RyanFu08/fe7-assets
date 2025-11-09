using UnityEngine;
using System.Collections.Generic;

public class UnitWrapper
{

    public string name, class_type;
    public int mhp, hp, str, skl, spd, lck, def, res, mov, con;
    public int cx, cy;
    public int affilation; //0 if own, 1 if not, 2 if not active

    public bool fly;

    public List<int> range = new List<int>();
    public List<Weapon> weaps = new List<Weapon>();
    public int weap_ind = 0;

    GameObject go;
    public UnitControl unit_controller;

    public UnitWrapper(string name, string class_type, int hp, int cx, int cy, int affilation, bool fl = false)
    {
        this.fly = fl;
        this.name = name;
        this.class_type = class_type;
        this.hp = hp; this.cx = cx; this.cy = cy;
        this.affilation = affilation;

        set_stat();

        if (this.hp <= 0)
        {
            this.cx = -10000;
            this.cy = -10000;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/MapUnit");
        if (prefab == null)
        {
            Debug.Log("pf not found!");
        }
        go = GameObject.Instantiate(prefab, new Vector3(cx, cy, 0), Quaternion.identity);
        unit_controller = go.GetComponent<UnitControl>();
        unit_controller.Load(class_type, "stand");

        set_weaps();

        if (affilation == 1)
        {
            if (name != "Dragon")
                unit_controller.MakeRed();
            else if (name == "Dragon")
            {
                unit_controller.MakeOrange();
            }
        }
        else if (affilation == 2)
        {
            unit_controller.MakeGrayscale();
        }
        else if (affilation == 3)
        {
            unit_controller.MakeOrange();
        }

    }

    public void Load()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/MapUnit");
        if (prefab == null)
        {
            Debug.Log("pf not found!");
        }
        if (this.hp <= 0)
        {
            this.cx = -10000;
            this.cy = -10000;
        }
        go = GameObject.Instantiate(prefab, new Vector3(cx, cy, 0), Quaternion.identity);
        unit_controller = go.GetComponent<UnitControl>();
        unit_controller.Load(class_type, "stand");

        set_weaps();

        if (affilation == 1)
        {
            if (name != "Dragon")
                unit_controller.MakeRed();
            else if (name == "Dragon")
            {
                unit_controller.MakeOrange();
            }
        }
        else if (affilation == 2)
        {
            unit_controller.MakeGrayscale();
        }
    }

    public void go_away()
    {
        go.transform.position = new Vector3(-10000, -10000, 0);
    }

    public void recenter()
    {
        unit_controller.kill_offset();
        go.transform.position = new Vector3(cx, cy, go.transform.position.z);
    }

    public void upd_range()
    {
        range = weaps[weap_ind].range;
    }
    public void set_stat()
    {
        if (name == "Florina")
        {
            mhp = hp = 53; str = 14; skl = 20; spd = 25; lck = 16; def = 10; res = 20; mov = 20; con = 5;
        }
        if (name == "Wil")
        {
            mhp = hp = 35; str = 17; skl = 14; spd = 14; lck = 13; def = 10; res = 8; mov = 6; con = 7;
        }
        if (name == "Oswin")
        {
            mhp = hp = 41; str = 19; skl = 14; spd = 11; lck = 6; def = 20; res = 9; mov = 5; con = 16;
        }
        if (name == "Eliwood")
        {
            mhp = hp = 40; str = 19; skl = 17; spd = 17; lck = 17; def = 28; res = 11; mov = 7; con = 9;
        }
        if (name == "Sain")
        {
            mhp = hp = 36; str = 21; skl = 11; spd = 14; lck = 10; def = 12; res = 4; mov = 40; con = 10;
        }

        if (name == "Heath")
        {
            mhp = hp = 41; str = 17; skl = 16; spd = 14; lck = 10; def = 13; res = 5; mov = 8; con = 10;
        }
        if (name == "Pent")
        {
            mhp = hp = 33; str = 16; skl = 21; spd = 17; lck = 14; def = 11; res = 16; mov = 6; con = 8;
        }
        if (name == "Canas")
        {
            mhp = hp = 32; str = 14; skl = 13; spd = 14; lck = 10; def = 10; res = 15; mov = 6; con = 8;
        }
        if (name == "Raven")
        {
            mhp = hp = 41; str = 15; skl = 19; spd = 21; lck = 7; def = 11; res = 5; mov = 6; con = 9;
        }
        if (name == "Hector")
        {
            mhp = hp = 40; str = 19; skl = 15; spd = 15; lck = 9; def = 19; res = 10; mov = 5; con = 15;
        }
        if (name == "Lyn")
        {
            mhp = hp = 38; str = 18; skl = 25; spd = 25; lck = 20; def = 10; res = 15; mov = 7; con = 5;
        }
        if (name == "Lucius")
        {
            mhp = hp = 34; str = 19; skl = 18; spd = 20; lck = 13; def = 6; res = 20; mov = 6; con = 6;
        }
        if (name == "Priscilla")
        {
            mhp = hp = 31; str = 17; skl = 15; spd = 17; lck = 17; def = 7; res = 18; mov = 7; con = 5;
        }
        if (name == "Rath")
        {
            mhp = hp = 40; str = 17; skl = 18; spd = 20; lck = 10; def = 12; res = 7; mov = 8; con = 9;
        }
        if (name == "Serra")
        {
            mhp = hp = 30; str = 15; skl = 15; spd = 19; lck = 20; def = 8; res = 18; mov = 6; con = 4;
        }
        if (name == "Kent")
        {
            mhp = hp = 39; str = 16; skl = 17; spd = 17; lck = 10; def = 13; res = 6; mov = 7; con = 11;
        }
        if (name == "Lowen")
        {
            mhp = hp = 44; str = 15; skl = 12; spd = 14; lck = 12; def = 16; res = 7; mov = 7; con = 13;
        }
        if (name == "Dragon")
        {
            mhp = hp = 40; str = 8; skl = 10; spd = 2; lck = 4; def = 10; res = 10; mov = 0; con = 30;
        }
    }

    private void set_weaps()
    {
        weaps.Clear();
        if (class_type == "Bishop")
        {
            weaps.Add(new Weapon()); weaps[0].set("Lightning");
        }
        else if (class_type == "Druid")
        {
            weaps.Add(new Weapon()); weaps[0].set("Flux");
        }
        else if (class_type == "Eliwood")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[0].set("Iron lance");

        }
        else if (class_type == "Florina")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[1].set("Iron lance");
            weaps.Add(new Weapon()); weaps[2].set("Javelin");
        }
        else if (class_type == "General")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron lance");
            weaps.Add(new Weapon()); weaps[1].set("Iron axe");
            weaps.Add(new Weapon()); weaps[2].set("Hand axe");
        }
        else if (class_type == "Hector")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron axe");
            weaps.Add(new Weapon()); weaps[1].set("Hand axe");
        }
        else if (class_type == "Hero")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron axe");
            weaps.Add(new Weapon()); weaps[1].set("Iron sword");
            weaps.Add(new Weapon()); weaps[2].set("Light brand");
        }
        else if (class_type == "Lyn")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[1].set("Iron bow");
        }
        else if (class_type == "Nils")
        {

        }
        else if (class_type == "NomadTrooper")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[1].set("Iron bow");
        }
        else if (class_type == "Paladin")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[1].set("Iron lance");
            weaps.Add(new Weapon()); weaps[2].set("Iron axe");
            weaps.Add(new Weapon()); weaps[3].set("Hand axe");
            weaps.Add(new Weapon()); weaps[4].set("Javelin");
        }
        else if (class_type == "Sage")
        {
            weaps.Add(new Weapon()); weaps[0].set("Fire");
        }
        else if (class_type == "Sniper")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron bow");
        }
        else if (class_type == "Valkyrie")
        {
            weaps.Add(new Weapon()); weaps[0].set("Fire");
        }
        else if (class_type == "WyvernLord")
        {
            weaps.Add(new Weapon()); weaps[0].set("Iron sword");
            weaps.Add(new Weapon()); weaps[1].set("Iron lance");
            weaps.Add(new Weapon()); weaps[2].set("Javelin");
        }
        else if (class_type == "Dragon")
        {
            weaps.Add(new Weapon()); weaps[0].set("Dragonstone");
        }
    }

    public void set_blue()
    {
        unit_controller.MakeBlue();
    }
    public void set_red()
    {
        unit_controller.MakeRed();
    }
    public void set_purple()
    {
        unit_controller.MakePurple();
    }

    public void set_state(string s) { unit_controller.LoadState(s); }

    public bool IsMoving()
    {
        if (unit_controller.queued_moves.Count > 0) return true;
        return false;
    }
    public void QueueUp() { unit_controller.QueueMove(0, 16f); cy += 16; }
    public void QueueDown() { unit_controller.QueueMove(1, 16f); cy -= 16; }
    public void QueueLeft() { unit_controller.QueueMove(2, 16f); cx -= 16; }
    public void QueueRight() { unit_controller.QueueMove(3, 16f); cx += 16; }
    public void Teleport(int x, int y)
    {
        unit_controller.Teleport(x, y);
        cx = x; cy = y;
    }

    public void equip_weapon(int ind)
    {
        weap_ind = ind;
    }
    public bool can_attack()
    {
        return true;
    }
    public bool can_item()
    {
        return true;
    }
    public bool can_staff()
    {
        return false;
    }
    public bool can_rescue()
    {
        return false;
    }
    public bool can_drop()
    {
        return false;
    }

    public Weapon weap()
    {
        return weaps[weap_ind];
    }

    public void deactivate()
    {
        affilation = 2;
        unit_controller.MakeGrayscale();
    }

}