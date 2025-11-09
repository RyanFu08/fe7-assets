using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Choreo : MonoBehaviour {
    [Header("Assign your two fighters here")]
    public GameObject u0, u1;             // 0 = left slot, 1 = right slot
    [Tooltip("Tick if Hector is in the LEFT slot (u0); untick if he’s in the RIGHT slot (u1).")]
    public bool hectorIsLeft = true;

    public GameObject hp0, hp1;
    public GameObject fx;

    //––– Internal state –––
    public List<int> turns;        // who acts: 0 or 1
    public List<string> actions;   // animation keys
    public List<string> freezes;   // explicit freeze keys
    public List<int> damages;      // HP to subtract
    public List<string> hit_type;  // “nocrit”/“critical”/“miss”

    public int cstate, pstate;    // current & previous step
    
    void Start() {
        turns = AnimLoader.turns;
        actions = AnimLoader.actions;
        freezes = AnimLoader.freezes;
        damages = AnimLoader.damages;
        hit_type = AnimLoader.hit_type;

        cstate = 0;
        pstate = -1;
        // immediately show the “final” pose on both
        u0.GetComponent<BattleAnimator>().state = freezes[actions.Count];
        u1.GetComponent<BattleAnimator>().state = freezes[actions.Count + 1];
    }

    void Update() {
        if (cstate >= turns.Count) {
            SceneManager.LoadScene("Combat");
            // reel finished → hold final freeze
            u0.GetComponent<BattleAnimator>().state  = freezes[actions.Count];
            u1.GetComponent<BattleAnimator>().state  = freezes[actions.Count + 1];
            u0.GetComponent<BattleAnimator>().myturn = false;
            u1.GetComponent<BattleAnimator>().myturn = false;
            return;
        }

        if (cstate != pstate) {
            pstate = cstate;
            int actor = turns[cstate];

            // actor plays the action, target shows freeze
            if (actor == 0) {
                u0.GetComponent<BattleAnimator>().state    = actions[cstate];
                u0.GetComponent<BattleAnimator>().myturn   = true;
                u0.GetComponent<BattleAnimator>().hit_type = hit_type[cstate];

                u1.GetComponent<BattleAnimator>().state    = freezes[cstate];
                u1.GetComponent<BattleAnimator>().myturn   = false;
            } else {
                u1.GetComponent<BattleAnimator>().state    = actions[cstate];
                u1.GetComponent<BattleAnimator>().myturn   = true;
                u1.GetComponent<BattleAnimator>().hit_type = hit_type[cstate];

                u0.GetComponent<BattleAnimator>().state    = freezes[cstate];
                u0.GetComponent<BattleAnimator>().myturn   = false;
            }
        }
    }

    public void add_damage() {
        // all damages are zero, but FX & whiten still fire
        int actor = turns[cstate];
        if (actor == 0) {
            hp1.GetComponent<hpbar_controller>().current_hp -= damages[cstate];
            var fxCtrl = fx.GetComponent<HitFXController>();
            fxCtrl.hit_type = hit_type[cstate];
            if (hit_type[cstate] != "miss") {
                fxCtrl.set_orientation(-1);
                u1.GetComponent<BattleAnimator>().whiten();
            } else {
                fxCtrl.set_orientation(1);
            }
            fxCtrl.cframe = 0;
        } else {
            hp0.GetComponent<hpbar_controller>().current_hp -= damages[cstate];
            var fxCtrl = fx.GetComponent<HitFXController>();
            fxCtrl.hit_type = hit_type[cstate];
            fxCtrl.set_orientation(1);
            fxCtrl.cframe = 0;
            if (hit_type[cstate] != "miss")
                u0.GetComponent<BattleAnimator>().whiten();
        }
    }
}
