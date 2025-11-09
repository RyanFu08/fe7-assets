using System.Collections.Generic;
using UnityEngine;

public static class CombatLoader
{
    public static int xx;
    public static readonly List<UnitWrapper> _units = new();

    public static void save(IEnumerable<UnitWrapper> units)
    {
        // _units.Clear();
        // _units.AddRange(units);
    }

    public static List<UnitWrapper> load(bool refresh = true)
    {
        // Ensure each unit is ready before handing the list back
        foreach (UnitWrapper uw in _units){
            if (refresh) uw.Load();
        }

        return _units; // callers can modify this list
    }

    public static void add_unit(UnitWrapper uw)
    {
        _units.Add(uw);
    }

    public static void real_init() {
        if (RoomData.whoami == 0) {

//          _units.Clear();

           _units.Add(new UnitWrapper("Florina", "Florina",    32,  -32,   -48, 0, true));
//           _units.Add(new UnitWrapper("Raven",     "Hero",     35,  0,  -32, 0));
//           _units.Add(new UnitWrapper("Oswin",   "General",    41,  0,   -64, 1));
//          _units.Add(new UnitWrapper("Eliwood",     "Eliwood",  40, 16,  -48, 0));
 //          _units.Add(new UnitWrapper("Sain",    "Paladin",    36, -16, -64, 1));

//            _units.Add(new UnitWrapper("Heath",   "WyvernLord", 41, 96, 0, 1, true));
//            _units.Add(new UnitWrapper("Pent",    "Sage",       33,  112, 0, 1));
//            _units.Add(new UnitWrapper("Canas",   "Druid",      32,  128, 0, 1));
//            _units.Add(new UnitWrapper("Raven",   "Hero",       41,  144, 0, 1));
//            _units.Add(new UnitWrapper("Hector",  "Hector",     40,  144, -16, 1));
            
        } else {

//            _units.Clear();
//            _units.Add(new UnitWrapper("Florina", "Florina",    32,  -160,   0, 1, true));
//            _units.Add(new UnitWrapper("Wil",     "Sniper",     35,  -144,   0, 1));
//            _units.Add(new UnitWrapper("Oswin",   "General",    41,  -128,   0, 1));
//            _units.Add(new UnitWrapper("Eliwood",     "Eliwood",        40, -112,   0, 1));
//            _units.Add(new UnitWrapper("Sain",    "Paladin",    36, -112,   -16, 1));
//
 //           _units.Add(new UnitWrapper("Heath",   "WyvernLord", 41, 96, 0, 0, true));
 //           _units.Add(new UnitWrapper("Pent",    "Sage",       33,  112, 0, 0));
 //           _units.Add(new UnitWrapper("Canas",   "Druid",      32,  128, 0, 0));
 //           _units.Add(new UnitWrapper("Raven",   "Hero",       41,  144, 0, 0));
 //           _units.Add(new UnitWrapper("Hector",  "Hector",     40,  144, -16, 0));

        }
    }

    public static List<UnitWrapper> start_dragon()
  {
    Debug.Log("STARTING DRAGOOOON");
    load(false);
    _units.Add(new UnitWrapper("Dragon",  "Dragon",     80,  0, 0, 1));
    return _units;
  }

    public static void init(int x)
    {
        xx = x;
        
    }
}
