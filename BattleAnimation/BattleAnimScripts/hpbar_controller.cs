using UnityEngine;
using System.Collections.Generic;


public class hpbar_controller : MonoBehaviour {
    public int max_hp = 60; // >= 31
    public int current_hp = 60;
    public int displayed_hp = 60;
    private float anim_time = 0.025f;
    public float timer;

    public GameObject stickPrefab;

    public List<GameObject> l1 = new List<GameObject>(); // 1-30
    public List<GameObject> l2 = new List<GameObject>(); // 31-60

    public GameObject hp_stat;

    void Awake() {
        foreach (GameObject go in l1) {
            Destroy(go);
        }
        foreach (GameObject go in l2) {
            Destroy(go);
        }
        l1 = new List<GameObject>(); // 1-30
        l2 = new List<GameObject>(); // 31-60
    }

    public void Load() {
        displayed_hp = current_hp;
        for (int i=0; i<30; i++) {
            GameObject go = Instantiate(stickPrefab, transform);
            go.GetComponent<hpstick_controller>().MoveLocalY(4);
            go.GetComponent<hpstick_controller>().MoveLocalX(2*i);
            if (i+1 <= current_hp) go.GetComponent<hpstick_controller>().set_full();
            else go.GetComponent<hpstick_controller>().set_empty();
            l1.Add(go);
        }
        for (int i=30; i<max_hp; i++) {
            GameObject go = Instantiate(stickPrefab, transform);
            go.GetComponent<hpstick_controller>().MoveLocalY(-4);
            go.GetComponent<hpstick_controller>().MoveLocalX(2*(i-30));
            
            if (i+1 <= current_hp) go.GetComponent<hpstick_controller>().set_full();
            else go.GetComponent<hpstick_controller>().set_empty();
            
            l2.Add(go);
        }
    }

    void Update() {
        hp_stat.GetComponent<NumberControl>().n = displayed_hp;
        if (displayed_hp == current_hp) return;

        timer += Time.deltaTime;
        if (timer > anim_time) {
            timer -= anim_time;
            if (displayed_hp > current_hp) {
                if (displayed_hp == 0) return;
                if (displayed_hp > 30) {
                    l2[displayed_hp-31].GetComponent<hpstick_controller>().set_empty();
                } else {
                    l1[displayed_hp-1].GetComponent<hpstick_controller>().set_empty();
                }
                displayed_hp--;
            } else {
                if (displayed_hp >= 30) {
                    l2[displayed_hp-30].GetComponent<hpstick_controller>().set_full();
                } else {
                    l1[displayed_hp].GetComponent<hpstick_controller>().set_full();
                }
                displayed_hp++;
            }
        }
    }
}
