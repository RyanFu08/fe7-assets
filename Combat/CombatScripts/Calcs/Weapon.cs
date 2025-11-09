using System.Collections.Generic;


public class Weapon
{
	public int mt, hit, crt, wt;
	public string weapon_type; //sword, lance, axe, anima, light, dark
	public string dmg_type;
	public List<int> range = new List<int>();
	public string s;

	public void set(string s)
	{
		this.s = s;
		if (s == "Iron sword")
		{
			weapon_type = "sword";
			dmg_type = "phys";
			mt = 5; hit = 90; crt = 0; wt = 5;
			range.Add(1);
		}
		if (s == "Iron lance")
		{
			weapon_type = "lance";
			dmg_type = "phys";
			mt = 7; hit = 80; crt = 0; wt = 8;
			range.Add(1);
		}
		if (s == "Iron axe")
		{
			weapon_type = "axe";
			dmg_type = "phys";
			mt = 8; hit = 75; crt = 0; wt = 10;
			range.Add(1);
		}
		if (s == "Iron bow")
		{
			weapon_type = "bow";
			dmg_type = "phys";
			mt = 6; hit = 85; crt = 0; wt = 5;
			range.Add(2); range.Add(3); range.Add(4);
		}
		if (s == "Fire")
		{
			weapon_type = "anima";
			dmg_type = "mag";
			mt = 5; hit = 90; crt = 0; wt = 5;
			range.Add(1); range.Add(2);
		}
		if (s == "Lightning")
		{
			weapon_type = "light";
			dmg_type = "mag";
			mt = 4; hit = 95; crt = 5; wt = 6;
			range.Add(1); range.Add(2);
		}
		if (s == "Flux")
		{
			weapon_type = "dark";
			dmg_type = "mag";
			mt = 7; hit = 80; crt = 0; wt = 8;
			range.Add(1); range.Add(2);
		}
		if (s == "Javelin")
		{
			weapon_type = "lance";
			dmg_type = "phys";
			mt = 5; hit = 65; crt = 0; wt = 11;
			range.Add(1); range.Add(2);
		}
		if (s == "Hand axe")
		{
			weapon_type = "axe";
			dmg_type = "phys";
			mt = 6; hit = 60; crt = 0; wt = 12;
			range.Add(1); range.Add(2);
		}
		if (s == "Light brand")
		{
			weapon_type = "sword";
			dmg_type = "mag";
			mt = 6; hit = 70; crt = 0; wt = 9;
			range.Add(1); range.Add(2);
		}
		if (s == "Dragonstone")
		{
			weapon_type = "attack";
			dmg_type = "mag";
			mt = 8; hit = 75; crt = 0; wt = 20;
			range.Add(1); range.Add(2);
		}
	}
}
