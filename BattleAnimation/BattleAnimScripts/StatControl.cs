using UnityEngine;

public class StatControl : MonoBehaviour
{
    public GameObject hit_left, dmg_left, crt_left, hit_right, dmg_right, crt_right, hp_left, hp_right;
    public GameObject hp_bar_left, hp_bar_right;
    public GameObject name_left, name_right;

    void Start() {
        set_hit_left(AnimLoader.hit_left);
        set_dmg_left(AnimLoader.dmg_left);
        set_crt_left(AnimLoader.crt_left);
        set_hp_left(AnimLoader.hp_blue);
        set_max_hp_left(AnimLoader.mhp_left);

        set_hit_right(AnimLoader.hit_right);
        set_dmg_right(AnimLoader.dmg_right);
        set_crt_right(AnimLoader.crt_right);
        set_hp_right(AnimLoader.hp_red);
        set_max_hp_right(AnimLoader.mhp_right);

        Debug.Log("STAT CONTROL:  " + AnimLoader.hp_blue + " " + AnimLoader.hp_red);

        hp_bar_left.GetComponent<hpbar_controller>().Load();
        hp_bar_right.GetComponent<hpbar_controller>().Load();
        set_names();
    }


    public void set_names() {
        name_left.GetComponent<StringControl>().s = AnimLoader.blue.name;
        name_right.GetComponent<StringControl>().s = AnimLoader.red.name;
    }

    public void set_hit_left(int n) {
        hit_left.GetComponent<NumberControl>().n = n;
    }

    public void set_dmg_left(int n) {
        dmg_left.GetComponent<NumberControl>().n = n;
    }

    public void set_crt_left(int n) {
        crt_left.GetComponent<NumberControl>().n = n;
    }

    public void set_hp_left(int n) {
        hp_left.GetComponent<NumberControl>().n = n;
        hp_bar_left.GetComponent<hpbar_controller>().current_hp = n;
        hp_bar_left.GetComponent<hpbar_controller>().displayed_hp = n;
        
    }

    public void set_hit_right(int n) {
        hit_right.GetComponent<NumberControl>().n = n;
    }

    public void set_dmg_right(int n) {
        dmg_right.GetComponent<NumberControl>().n = n;
    }

    public void set_crt_right(int n) {
        crt_right.GetComponent<NumberControl>().n = n;
    }

    public void set_hp_right(int n) {
        hp_right.GetComponent<NumberControl>().n = n;
        hp_bar_right.GetComponent<hpbar_controller>().current_hp = n;
        hp_bar_right.GetComponent<hpbar_controller>().displayed_hp = n;
        
    }

    public void set_max_hp_left(int n) {
        hp_bar_left.GetComponent<hpbar_controller>().max_hp = n;
    }

    public void set_max_hp_right(int n) {
        hp_bar_right.GetComponent<hpbar_controller>().max_hp = n;
    }

}
