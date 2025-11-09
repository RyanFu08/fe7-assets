using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

public static class save_state
{
    public static ActionList al;
    public static int user_id = 0;
    public static int last_proc_action = 0;
    public static bool seeded = false;
    public static int turn = 0;
    public static bool first_time = true;

}


/* -------------------------------------------------------------
 * CombatControl — gameplay untouched; cloud calls routed
 * through CloudPersistence. Everything else is identical.
 * ------------------------------------------------------------- */
public class CombatControl : MonoBehaviour
{
    /** user info **/
    [SerializeField] private int user_id = 0;
    [SerializeField] private GameObject dragontext;

    /**  PREFABS  **/
    [SerializeField] private GameObject tile_prefab;

    /** Controls **/
    private List<UnitWrapper> units = new List<UnitWrapper>();
    [SerializeField] private GameObject cursor;
    private cursor_animator cursor_anim;
    private cursor_input cursor_inp;
    [SerializeField] private GameObject hp_display;
    private HPDisplay_Movement hpdisplay_movement;
    [SerializeField] private GameObject atk_display;
    private AtkDisplay atkdisplay_control;
    [SerializeField] private GameObject x2_red, x2_blue, up_arrow_blue, down_arrow_blue, up_arrow_red, down_arrow_red;
    [SerializeField] private GameObject menu;
    private menu_controller menu_control;
    [SerializeField] private GameObject player_phase, enemy_phase;

    /** cloud **/
    public int action_index = 0;
    [SerializeField] private ActionList al;

    /** general **/
    public string state = "wander";
    public int cursor_over = -2, prev_cursor_over = -2;
    List<(int, int)> ok_tiles;

    /** first click on own unit variables **/
    private List<GameObject> blue_tile_first_click, red_tile_first_click;
    private List<(int, int)> blue_loc_first_click;
    private Dictionary<(int, int), (int, int)> prev_first_click;
    private UnitWrapper current_selected_first_click;
    private int px_first_click, py_first_click;
    private List<String> opt_first_click;

    /** attacking **/
    private List<UnitWrapper> attackeable_sort_x, attackeable_sort_y;
    private UnitWrapper current_selected_attack_target;

    /** action information **/
    private int unit_ind, item_ind, other_unit_ind;
    private List<int> move_dir = new List<int>();

    /** cloud sync **/
    public float timer_csync = 0f;
    private float interval_csync = 2f;
    private UnitWrapper opponent_unit;


    /** util use **/
    List<(int, int)> path_sq = new List<(int, int)>();
    float timer_click = 0f;

    void Awake()
    {
        if (save_state.seeded == false)
        {
            UnityEngine.Random.InitState(42);
            System.Random rng = new System.Random(42);
            save_state.seeded = true;
        }

        if (save_state.al == null)
        {
            save_state.al = al ?? new ActionList();
        }
        else
        {
            al = save_state.al;
        }
    }

    async Task start_dragon(CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);  // non-blocking 2s wait

        dragontext.GetComponent<StringControl>().s = "A Dragon Has Spawned!";
        dragontext.GetComponent<blinker>().s();

        units = CombatLoader.start_dragon();
    }

    async Task start_dragon1()
    {
        dragontext.GetComponent<blinker>().s();
        units = CombatLoader.start_dragon();
    }

    async Task heal_pulse(CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);  // non-blocking 2s wait

        dragontext.GetComponent<StringControl>().s = "Periodic Regen Tick: HP Up!";
        dragontext.GetComponent<blinker>().s();

        foreach (var uw in units)
            uw.hp = Math.Min(uw.mhp, uw.hp + 3);
    }

    async void Start()
    {
        //Debug.Log(Application.persistentDataPath);              // the folder

        if (RoomData.whoami == 0 && save_state.first_time)
        {
            save_state.user_id = 0;
            save_state.first_time = false;
        }
        else if (RoomData.whoami == 1 && save_state.first_time)
        {
            save_state.user_id = 1;
            save_state.first_time = false;
        }

        al = save_state.al;

        cursor_anim = cursor.GetComponent<cursor_animator>();
        cursor_inp = cursor.GetComponent<cursor_input>();
        hpdisplay_movement = hp_display.GetComponent<HPDisplay_Movement>();
        atkdisplay_control = atk_display.GetComponent<AtkDisplay>();
        menu_control = menu.GetComponent<menu_controller>();

        bool refre = true;
        if (CombatLoader._units.Count == 0)
        {
            CombatLoader.real_init();
            refre = false;
        }
        units = CombatLoader.load(refre);

        await CloudPersistence.EnsureReadyAsync();
        //Debug.Log($"[Diag] env={CloudPersistence.CurrentEnvironment} appId={Application.cloudProjectId} signedIn={CloudPersistence.IsSignedIn} playerId={CloudPersistence.PlayerId}");

        _ = CloudPersistence.SmokeTestAsync();

        if (save_state.user_id == 0)
        {
            await SaveToCloudAsync(al);
        }
        else
        {
            state = "not my turn";
        }

        ok_tiles = IntPairStorage.Load("tsq.txt");
        foreach (var (a, b) in ok_tiles)
        {
            //Debug.Log(a + " " + b);
        }

        cursor_anim.set_breathe();
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.D))
        {
            heal_pulse();
        }

        var cm = cursor.GetComponent<cursor_movement>();
        if (cm.target.x == cursor.transform.position.x && cm.target.y == cursor.transform.position.y)
        {
            Vector3 pos = cursor.transform.position;
            float rx = Mathf.Round(pos.x);
            float ry = Mathf.Round(pos.y);
            if (Mathf.Abs(pos.x - rx) < 1e-3f) pos.x = rx;
            if (Mathf.Abs(pos.y - ry) < 1e-3f) pos.y = ry;
            cursor.transform.position = pos;
            cm.target.x = cursor.transform.position.x;
            cm.target.y = cursor.transform.position.y;
        }

        if (save_state.user_id == 3)
        {
            timer_click += Time.deltaTime;
            if (timer_click > 0.1f && Input.GetKeyDown(KeyCode.X))
            {
                GameObject go = Instantiate(tile_prefab, new Vector3(cursor.transform.position.x, cursor.transform.position.y, 0), Quaternion.identity);
                go.GetComponent<tile_control>().set_state_blue();
                timer_click = 0f;
                path_sq.Add(((int)cursor.transform.position.x, (int)cursor.transform.position.y));
                //Debug.Log(((int)cursor.transform.position.x,(int)cursor.transform.position.y));
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                IntPairStorage.Save("tsq.txt", path_sq);
            }


            return;
        }

        if (state == "wander")
        {
            UpdateCursorOver_Wander();
            AnimateIfNecessary_Wander();
            CheckForEmptyClicks_Wander();
            CheckForMyUnitClicks_Wander();
        }
        else if (state == "wait for end turn after empty click")
        {
            CheckMenuStatus_WaitForEndTurnAfterEmptyClick();
        }
        else if (state == "first click own unit")
        {
            CheckForInputs_FirstClickOwnUnit();
        }
        else if (state == "waiting for unit traversal after first click")
        {
            Wait_WaitingForUnitTraversalAfterFirstClick();
        }
        else if (state == "arrived at first click")
        {
            CheckForInputs_ArrivedAtFirstClick();
        }
        else if (state == "cancelled first unit traversal")
        {
            F_CancelFirstUnitTraversal();
        }
        else if (state == "item select menu")
        {
            Wait_ItemSelectMenu();
        }
        else if (state == "attack enemy selector")
        {
            TakeInputs_Attack();
        }

        if (state == "not my turn")
        {
            SyncActionList();
            CheckForNewAction();
        }
        else if (state == "waiting for opponent unit traversal")
        {
            WaitForOpponentUnitTraversal();
        }
        else if (state == "waiting for opponent unit traversal before attack")
        {
            WaitForOpponentUnitTraversalBeforeAttack();
        }
    }

    // --- Wander state and UI helpers (unchanged logic) ---
    void CheckForEmptyClicks_Wander()
    {
        if (Input.GetKeyDown(KeyCode.X) && cursor_over < 0)
        {
            menu_control.display_menu(new List<string> { "End Turn" });
            state = "wait for end turn after empty click";
        }
    }
    void CheckForMyUnitClicks_Wander()
    {
        if (Input.GetKeyDown(KeyCode.X) && cursor_over >= 0 && units[cursor_over].affilation == 0)
        {
            state = "first click own unit";
            current_selected_first_click = units[cursor_over];
            current_selected_first_click.upd_range();
            px_first_click = (int)cursor.transform.position.x;
            py_first_click = (int)cursor.transform.position.y;
            ShowRange_FirstClickOwnUnit(units[cursor_over]);

        }
    }
    void UpdateCursorOver_Wander()
    {
        prev_cursor_over = cursor_over;
        for (int i = 0; i < units.Count; i++)
        {
            if (cursor.transform.position.x == units[i].cx && cursor.transform.position.y == units[i].cy)
            {
                cursor_over = i; unit_ind = i;
                item_ind = units[i].weap_ind;
                return;
            }
        }
        cursor_over = -1;
    }
    void AnimateIfNecessary_Wander()
    {
        if (cursor_over != prev_cursor_over)
        {
            cursor_anim.show();
            if (cursor_over < 0)
            {
                hpdisplay_movement.hide(); cursor_anim.set_breathe();
                if (prev_cursor_over >= 0) units[prev_cursor_over].set_state("stand");
            }
            else
            {
                hpdisplay_movement.show();
                hpdisplay_movement.set_name_(units[cursor_over].name);
                hpdisplay_movement.set_stat(units[cursor_over].hp, units[cursor_over].mhp);
                cursor_anim.set_expand();
                if (prev_cursor_over >= 0) units[prev_cursor_over].set_state("stand");
                if (units[cursor_over].affilation == 0) units[cursor_over].set_state("ready");
            }
        }
    }
    void CheckMenuStatus_WaitForEndTurnAfterEmptyClick()
    {
        if (menu_control.selection == -1) return;
        if (menu_control.selection == -2)
        {
            state = "wander";
        }
        else
        {
            state = "end turn";
            end_turn();
        }
    }

    void ShowRange_FirstClickOwnUnit(UnitWrapper u)
    {
        cursor_anim.set_breathe();

        var blue_tile = new List<GameObject>();
        var red_tile = new List<GameObject>();
        float cnt = 0f;

        prev_first_click = new Dictionary<(int, int), (int, int)>();

        var bfs = new Queue<(int dist, int x, int y)>();
        var vis = new List<(int, int)>();
        bfs.Enqueue((u.mov, u.cx, u.cy));
        //Debug.Log("CELL STATUS OF -9 -2 :  " + cell_status(-9, -2));
        while (bfs.Count > 0)
        {
            var (dist, x, y) = bfs.Dequeue();
            if (cell_status(x, y) == 2) continue;
            if (vis.Contains((x, y))) continue;
            vis.Add((x, y));

            if (dist > 0)
            {
                bfs.Enqueue((dist - 1, x - 16, y)); if (!vis.Contains((x - 16, y))) prev_first_click[(x - 16, y)] = (x, y);
                bfs.Enqueue((dist - 1, x + 16, y)); if (!vis.Contains((x + 16, y))) prev_first_click[(x + 16, y)] = (x, y);
                bfs.Enqueue((dist - 1, x, y - 16)); if (!vis.Contains((x, y - 16))) prev_first_click[(x, y - 16)] = (x, y);
                bfs.Enqueue((dist - 1, x, y + 16)); if (!vis.Contains((x, y + 16))) prev_first_click[(x, y + 16)] = (x, y);
            }
        }

        foreach (var (x, y) in vis)
        {
            GameObject go = Instantiate(tile_prefab, new Vector3(x, y, 0), Quaternion.identity);
            blue_tile.Add(go);
            go.GetComponent<tile_control>().Invoke("set_state_blue", cnt);
            cnt += 0.002f;
        }

        List<(int, int)> tvis = new List<(int, int)>();
        foreach (int r in u.range)
        {
            foreach ((int xx, int yy) in vis)
            {
                bfs = new Queue<(int dist, int x, int y)>();
                var cvis = new List<(int, int)>();
                bfs.Enqueue((r, xx, yy));
                while (bfs.Count > 0)
                {
                    var (dist, x, y) = bfs.Dequeue();
                    if (cvis.Contains((x, y))) continue;
                    cvis.Add((x, y));
                    tvis.Add((x, y));
                    if (dist > 0)
                    {
                        bfs.Enqueue((dist - 1, x - 16, y));
                        bfs.Enqueue((dist - 1, x + 16, y));
                        bfs.Enqueue((dist - 1, x, y - 16));
                        bfs.Enqueue((dist - 1, x, y + 16));
                    }
                }
            }
        }
        tvis = tvis.Distinct().ToList();
        foreach ((int x, int y) in tvis)
        {
            if (!vis.Contains((x, y)))
            {
                GameObject go = Instantiate(tile_prefab, new Vector3(x, y, 0), Quaternion.identity);
                red_tile.Add(go);
                go.GetComponent<tile_control>().Invoke("set_state_red", cnt);
                cnt += 0.002f;
            }
        }

        blue_tile_first_click = blue_tile;
        red_tile_first_click = red_tile;
        blue_loc_first_click = vis;
    }

    int cell_status(int x, int y)
    {
        if (!ok_tiles.Contains((x, y)) && !current_selected_first_click.fly)
        {
            return 2;
        }
        foreach (UnitWrapper uw in units)
        {
            if (uw.affilation != 1)
            {
                if (uw.cx == x && uw.cy == y)
                {
                    return 1;
                }
            }
            else if (uw.affilation == 1 || uw.affilation == 3)
            {
                if (uw.cx == x && uw.cy == y)
                {
                    return 2;
                }
            }
        }
        return 0;
    }

    void CheckForInputs_FirstClickOwnUnit()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            HideRangeReturnToWander_FirstClickOwnUnit();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            int cursor_x = (int)cursor.transform.position.x;
            int cursor_y = (int)cursor.transform.position.y;
            if (CheckOkayToMoveToCell_FirstClickOwnUnit(cursor_x, cursor_y) || (cursor_x == current_selected_first_click.cx && cursor_y == current_selected_first_click.cy))
            {
                HideRangeReturnToWander_FirstClickOwnUnit();
                NavToCell_FirstClickDownOwnUnit(cursor_x, cursor_y);
            }
        }
    }

    void HideRangeReturnToWander_FirstClickOwnUnit()
    {
        float cnt = 0f;
        red_tile_first_click.Reverse();
        blue_tile_first_click.Reverse();

        foreach (GameObject go in red_tile_first_click) { Destroy(go, cnt); cnt += 0.002f; }
        red_tile_first_click.Clear();

        foreach (GameObject go in blue_tile_first_click) { Destroy(go, cnt); cnt += 0.002f; }
        blue_tile_first_click.Clear();

        blue_loc_first_click.Clear();
        state = "wander";
    }

    bool CheckOkayToMoveToCell_FirstClickOwnUnit(int click_x, int click_y)
    {
        if (cell_status(click_x, click_y) == 0 && blue_loc_first_click.Contains((click_x, click_y))) return true;
        return false;
    }

    void NavToCell_FirstClickDownOwnUnit(int click_x, int click_y)
    {
        List<string> dir = new List<string>();
        int cx = click_x, cy = click_y;

        while (cx != current_selected_first_click.cx || cy != current_selected_first_click.cy)
        {
            (int px, int py) = prev_first_click[(cx, cy)];
            if (px < cx) dir.Add("right");
            if (px > cx) dir.Add("left");
            if (py < cy) dir.Add("up");
            if (py > cy) dir.Add("down");
            cx = px; cy = py;
        }

        dir.Reverse();
        move_dir.Clear();
        foreach (string ss in dir)
        {
            if (ss == "right") { current_selected_first_click.QueueRight(); move_dir.Add(1); }
            if (ss == "left") { current_selected_first_click.QueueLeft(); move_dir.Add(0); }
            if (ss == "up") { current_selected_first_click.QueueUp(); move_dir.Add(2); }
            if (ss == "down") { current_selected_first_click.QueueDown(); move_dir.Add(3); }
        }
        state = "waiting for unit traversal after first click";
    }

    void Wait_WaitingForUnitTraversalAfterFirstClick()
    {
        if (current_selected_first_click.IsMoving()) return;
        cursor.GetComponent<cursor_movement>().Teleport(
            new Vector3(current_selected_first_click.cx, current_selected_first_click.cy, cursor.transform.position.z)
        );
        current_selected_first_click.recenter();
        state = "arrived at first click";
        DisplayMenu_ArrivedAtFirstClick();
    }

    async void CheckForInputs_ArrivedAtFirstClick()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            current_selected_first_click.Teleport(px_first_click, py_first_click);
            state = "cancelled first unit traversal";
            move_dir.Clear();
        }
        else if (menu_control.selection >= 0)
        {
            string select = opt_first_click[menu_control.selection];

            if (select == "Item")
            {
                state = "item select menu";
                DisplayMenu_ItemSelectMenu();
            }
            else if (select == "Attack")
            {
                state = "attack enemy selector";
                SelectOpponentPrep_Attack();
            }
            else if (select == "Wait")
            {
                cursor_anim.set_breathe();
                state = "wander";
                current_selected_first_click.deactivate();
                current_selected_first_click = null;

                var ga = new GameAction
                {
                    unit_ind = unit_ind,
                    move_dir = new List<int>(move_dir),
                    item_ind = item_ind,
                    attack = false
                };
                al.add_action(ga);
                move_dir = new List<int>();
                await SaveToCloudAsync(al);
            }
        }
    }

    void DisplayMenu_ArrivedAtFirstClick()
    {
        cursor_anim.set_expand();
        cursor_inp.deactivate();

        opt_first_click = new List<string>();
        if (can_attack()) opt_first_click.Add("Attack");
        if (current_selected_first_click.can_item()) opt_first_click.Add("Item");
        if (current_selected_first_click.can_staff()) opt_first_click.Add("Staff");
        if (current_selected_first_click.can_rescue()) opt_first_click.Add("Rescue");
        if (current_selected_first_click.can_drop()) opt_first_click.Add("Drop");
        opt_first_click.Add("Wait");

        menu_control.display_menu(opt_first_click);
    }

    void F_CancelFirstUnitTraversal()
    {
        state = "first click own unit";
        ShowRange_FirstClickOwnUnit(current_selected_first_click);
    }

    void DisplayMenu_ItemSelectMenu()
    {
        List<string> item_opt = new List<string>();
        foreach (Weapon w in current_selected_first_click.weaps) item_opt.Add(w.s);
        menu_control.display_menu(item_opt);
    }

    void Wait_ItemSelectMenu()
    {
        if (menu_control.selection == -1) return;
        if (menu_control.selection == -2)
        {
            state = "arrived at first click";
            DisplayMenu_ArrivedAtFirstClick();
        }
        else
        {
            current_selected_first_click.weap_ind = menu_control.selection;
            item_ind = menu_control.selection;
            current_selected_first_click.upd_range();
            state = "arrived at first click";
            DisplayMenu_ArrivedAtFirstClick();
        }
    }

    bool can_attack()
    {
        List<UnitWrapper> attackeable = new List<UnitWrapper>();
        foreach (UnitWrapper uw in units)
        {
            if (uw.affilation != 1) continue;
            int dist = Math.Abs(uw.cx - current_selected_first_click.cx) + Math.Abs(uw.cy - current_selected_first_click.cy);
            dist /= 16;
            if (current_selected_first_click.range.Contains(dist)) attackeable.Add(uw);
        }
        if (attackeable.Count > 0) return true;
        return false;
    }

    void SelectOpponentPrep_Attack()
    {
        cursor_anim.set_breathe();
        cursor_inp.deactivate();
        atkdisplay_control.show();

        List<UnitWrapper> attackeable = new List<UnitWrapper>();
        foreach (UnitWrapper uw in units)
        {
            if (uw.affilation != 1) continue;
            int dist = Math.Abs(uw.cx - current_selected_first_click.cx) + Math.Abs(uw.cy - current_selected_first_click.cy);
            dist /= 16;
            if (current_selected_first_click.range.Contains(dist)) attackeable.Add(uw);
        }

        attackeable_sort_x = attackeable.OrderBy(u => u.cx).ThenBy(u => u.cy).ToList();
        attackeable_sort_y = attackeable.OrderBy(u => u.cy).ThenBy(u => u.cx).ToList();

        current_selected_attack_target = attackeable_sort_x[0];
        SelectTarget_Attack();
    }

    void TakeInputs_Attack()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            atkdisplay_control.hide();
            state = "arrived at first click";
            cursor.GetComponent<cursor_movement>().Teleport(
                new Vector3(current_selected_first_click.cx, current_selected_first_click.cy, cursor.transform.position.z)
            );
            Invoke(nameof(DisplayMenu_ArrivedAtFirstClick), 0.001f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int ind = -1; for (int i = 0; i < attackeable_sort_x.Count; i++) if (attackeable_sort_x[i] == current_selected_attack_target) ind = i;
            current_selected_attack_target = attackeable_sort_x[(ind + attackeable_sort_x.Count - 1) % attackeable_sort_x.Count];
            SelectTarget_Attack();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int ind = -1; for (int i = 0; i < attackeable_sort_x.Count; i++) if (attackeable_sort_x[i] == current_selected_attack_target) ind = i;
            current_selected_attack_target = attackeable_sort_x[(ind + attackeable_sort_x.Count + 1) % attackeable_sort_x.Count];
            SelectTarget_Attack();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int ind = -1; for (int i = 0; i < attackeable_sort_y.Count; i++) if (attackeable_sort_y[i] == current_selected_attack_target) ind = i;
            current_selected_attack_target = attackeable_sort_y[(ind + attackeable_sort_y.Count - 1) % attackeable_sort_y.Count];
            SelectTarget_Attack();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int ind = -1; for (int i = 0; i < attackeable_sort_y.Count; i++) if (attackeable_sort_y[i] == current_selected_attack_target) ind = i;
            current_selected_attack_target = attackeable_sort_y[(ind + attackeable_sort_y.Count + 1) % attackeable_sort_y.Count];
            SelectTarget_Attack();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (current_selected_attack_target == units[i]) { other_unit_ind = i; break; }
            }
            _ = Execute_Attack();
            state = "loading attack";
        }
    }

    void SelectTarget_Attack()
    {
        do_attack_calcs();
        atkdisplay_control.refresh();
        cursor.GetComponent<cursor_movement>().Teleport(
            new Vector3(current_selected_attack_target.cx, current_selected_attack_target.cy, cursor.transform.position.z)
        );
    }

    void do_attack_calcs()
    {
        var blue = current_selected_first_click;
        var red = current_selected_attack_target;

        atkdisplay_control.set_blue_name(blue.name);
        atkdisplay_control.set_red_name(red.name);

        atkdisplay_control.set_blue_hp(blue.hp.ToString());
        atkdisplay_control.set_red_hp(red.hp.ToString());

        int atk_blue = blue.str + 1 * (blue.weap().mt + 2 * weapon_triangle(blue.weap(), red.weap()));
        int atk_red = red.str + 1 * (red.weap().mt + 2 * weapon_triangle(red.weap(), blue.weap()));
        int def_blue = (blue.weap().dmg_type == "phys") ? blue.def : blue.res;
        int def_red = (blue.weap().dmg_type == "phys") ? red.def : red.res;
        int dmg_blue = Math.Max(0, atk_blue - def_red);
        int dmg_red = Math.Max(0, atk_red - def_blue);
        atkdisplay_control.set_blue_mt(dmg_blue.ToString());
        atkdisplay_control.set_red_mt(dmg_red.ToString());

        int blue_atk_speed = Math.Max(0, blue.spd - Math.Max(0, blue.weap().wt - blue.con));
        int red_atk_speed = Math.Max(0, red.spd - Math.Max(0, red.weap().wt - red.con));
        int blue_avoid = 2 * blue_atk_speed + blue.lck;
        int red_avoid = 2 * red_atk_speed + red.lck;
        int blue_hit = blue.weap().hit + 2 * blue.skl + blue.lck / 2 + 15 * weapon_triangle(blue.weap(), red.weap());
        int red_hit = red.weap().hit + 2 * red.skl + red.lck / 2 + 15 * weapon_triangle(red.weap(), blue.weap());
        int blue_disp_hit = Math.Max(0, blue_hit - red_avoid);
        int red_disp_hit = Math.Max(0, red_hit - blue_avoid);
        atkdisplay_control.set_blue_hit(blue_disp_hit.ToString());
        atkdisplay_control.set_red_hit(red_disp_hit.ToString());

        int blue_crit = blue.weap().crt + blue.skl / 2;
        int red_crit = red.weap().crt + red.skl / 2;
        int blue_crit_disp = Math.Max(0, blue_crit - red.lck); blue_crit_disp = Math.Max(5, blue_crit_disp);
        int red_crit_disp = Math.Max(0, red_crit - blue.lck); red_crit_disp = Math.Max(5, red_crit_disp);

        atkdisplay_control.set_blue_crit(blue_crit_disp.ToString());
        atkdisplay_control.set_red_crit(red_crit_disp.ToString());

        x2_blue.GetComponent<x2_control>().hide(); x2_red.GetComponent<x2_control>().hide();
        up_arrow_blue.GetComponent<ArrowControl>().hide(); up_arrow_red.GetComponent<ArrowControl>().hide();
        down_arrow_blue.GetComponent<ArrowControl>().hide(); down_arrow_red.GetComponent<ArrowControl>().hide();

        if (weapon_triangle(blue.weap(), red.weap()) == 1)
        {
            up_arrow_blue.GetComponent<ArrowControl>().show();
            down_arrow_red.GetComponent<ArrowControl>().show();
        }
        else if (weapon_triangle(blue.weap(), red.weap()) == -1)
        {
            down_arrow_blue.GetComponent<ArrowControl>().show();
            up_arrow_red.GetComponent<ArrowControl>().show();
        }

        if (blue_atk_speed - 4 >= red_atk_speed) { x2_blue.GetComponent<x2_control>().show(); x2_red.GetComponent<x2_control>().hide(); }
        if (blue_atk_speed + 4 <= red_atk_speed) { x2_red.GetComponent<x2_control>().show(); x2_blue.GetComponent<x2_control>().hide(); }
    }

    int weapon_triangle(Weapon w1, Weapon w2)
    {
        string t1 = w1.weapon_type.ToLower();
        string t2 = w2.weapon_type.ToLower();

        if (t1 == "sword" && t2 == "axe") return 1;
        if (t1 == "axe" && t2 == "lance") return 1;
        if (t1 == "lance" && t2 == "sword") return 1;
        if (t2 == "sword" && t1 == "axe") return -1;
        if (t2 == "axe" && t1 == "lance") return -1;
        if (t2 == "lance" && t1 == "sword") return -1;

        if (t1 == "anima" && t2 == "light") return 1;
        if (t1 == "light" && t2 == "dark") return 1;
        if (t1 == "dark" && t2 == "anima") return 1;
        if (t2 == "anima" && t1 == "light") return -1;
        if (t2 == "light" && t1 == "dark") return -1;
        if (t2 == "dark" && t1 == "anima") return -1;

        return 0;
    }

    // ======= Cloud Save wrappers =======
    static Task SaveToCloudAsync(ActionList list) => CloudPersistence.SaveActionListAsync(list);
    static Task<ActionList> LoadFromCloudAsync() => CloudPersistence.LoadActionListAsync();

    public async Task ReloadActionsFromCloudAsync()
    {
        var loaded = await LoadFromCloudAsync();
        al = loaded; save_state.al = loaded;
    }

    // ======= Attack flow (unchanged except save) =======
    async Task Execute_Attack()
    {
        var blue = current_selected_first_click;
        var red = current_selected_attack_target;

        int atk_blue = blue.str + 1 * (blue.weap().mt + 2 * weapon_triangle(blue.weap(), red.weap()));
        int atk_red = red.str + 1 * (red.weap().mt + 2 * weapon_triangle(red.weap(), blue.weap()));
        int def_blue = (blue.weap().dmg_type == "phys") ? blue.def : blue.res;
        int def_red = (blue.weap().dmg_type == "phys") ? red.def : red.res;
        int dmg_blue = Math.Max(0, atk_blue - def_red);
        int dmg_red = Math.Max(0, atk_red - def_blue);

        int blue_atk_speed = Math.Max(0, blue.spd - Math.Max(0, blue.weap().wt - blue.con));
        int red_atk_speed = Math.Max(0, red.spd - Math.Max(0, red.weap().wt - red.con));
        int blue_avoid = 2 * blue_atk_speed + blue.lck;
        int red_avoid = 2 * red_atk_speed + red.lck;
        int blue_hit = blue.weap().hit + 2 * blue.skl + blue.lck / 2 + 15 * weapon_triangle(blue.weap(), red.weap());
        int red_hit = red.weap().hit + 2 * red.skl + red.lck / 2 + 15 * weapon_triangle(red.weap(), blue.weap());
        int blue_disp_hit = Math.Max(0, blue_hit - red_avoid);
        int red_disp_hit = Math.Max(0, red_hit - blue_avoid);

        int blue_crit = blue.weap().crt + blue.skl / 2;
        int red_crit = red.weap().crt + red.skl / 2;
        int blue_crit_disp = Math.Max(0, blue_crit - red.lck); blue_crit_disp = Math.Max(5, blue_crit_disp);
        int red_crit_disp = Math.Max(0, red_crit - blue.lck); red_crit_disp = Math.Max(5, red_crit_disp);

        string doub = "none";
        if (blue_atk_speed - 4 >= red_atk_speed) doub = "blue";
        if (blue_atk_speed + 4 <= red_atk_speed) doub = "red";

        current_selected_first_click.affilation = 2;
        CombatLoader.save(units);

        var ga = new GameAction
        {
            unit_ind = unit_ind,
            move_dir = new List<int>(move_dir),
            item_ind = item_ind,
            attack = true,

            hp_blue = blue.hp,
            hp_red = red.hp,
            dmg_blue = dmg_blue,
            dmg_red = dmg_red,
            hit_blue = blue_disp_hit,
            hit_red = red_disp_hit,
            crit_blue = blue_crit_disp,
            crit_red = red_crit_disp,

            doub = doub,
            blue_ind = unit_ind,
            red_ind = other_unit_ind,

            hit_left = blue_disp_hit,
            dmg_left = dmg_blue,
            crt_left = blue_crit_disp,
            hp_left = blue.hp,
            mhp_left = blue.mhp,
            hit_right = red_disp_hit,
            dmg_right = dmg_red,
            crt_right = red_crit_disp,
            hp_right = red.hp,
            mhp_right = red.mhp
        };
        //Debug.Log("BLUE HP: " + blue.hp);
        move_dir = new List<int>();
        al.add_action(ga);

        await SaveToCloudAsync(al);

        //Debug.Log(blue.hp + " ~~");
        //Debug.Log(red.hp + " ~~");


        AnimLoader.Load(blue.hp, red.hp, dmg_blue, dmg_red, blue_disp_hit, red_disp_hit, blue_crit_disp, red_crit_disp, doub, blue, red);
        AnimLoader.Load2(blue_disp_hit, dmg_blue, blue_crit_disp, blue.hp, blue.mhp, red_disp_hit, dmg_red, red_crit_disp, red.hp, red.mhp);
        save_state.al = al;
        AnimLoader.Go();
    }

    async void end_turn()
    {
        foreach (UnitWrapper uw in units)
        {
            if (uw.affilation == 2)
            {
                uw.affilation = 0;
                uw.set_blue();
            }
        }
        enemy_phase.GetComponent<phaes_mover>().go();


        if (RoomData.whoami == 1)
        {
            RoomData.turn += 1;
            if (RoomData.turn % 2 == 0)
            {
                var ga1 = new GameAction
                {
                    special_command = "heal pulse"
                };
                al.add_action(ga1);
                _ = heal_pulse();
                await SaveToCloudAsync(al);
            }
            if (RoomData.turn % 8 == 7)
            {
                var ga2 = new GameAction
                {
                    special_command = "start dragon"
                };
                al.add_action(ga2);
                _ = start_dragon();
                await SaveToCloudAsync(al);
            }
        }

        var ga = new GameAction
        {
            switch_turn = true
        };
        al.add_action(ga);
        await SaveToCloudAsync(al);

        await ReloadActionsFromCloudAsync();

        save_state.last_proc_action = al.actions.Count;

        save_state.user_id = 1; //NOT MY TURN ANYMORE

        state = "not my turn";
    }

    // ======= Syncing when it's not your turn =======


    void SyncActionList()
    {
        timer_csync += Time.deltaTime;
        if (timer_csync < interval_csync) return;
        timer_csync -= interval_csync;

        _ = ReloadLoop();
        async Task ReloadLoop()
        {
            try
            {
                await ReloadActionsFromCloudAsync();
                //Debug.Log("[CloudSave] Reload OK. Count=" + (al?.actions?.Count ?? 0));
            }
            catch (Exception e)
            {
                //Debug.LogError("[CloudSave] Reload failed: " + e);
            }
        }
    }

    void CheckForNewAction()
    {
        if (al?.actions != null && al.actions.Count > save_state.last_proc_action)
        {
            GameAction latest_action = al.actions[save_state.last_proc_action];
            save_state.last_proc_action++;
            if (latest_action.switch_turn)
            {
                player_phase.GetComponent<phaes_mover>().go();
                state = "wander";
                save_state.user_id = 0;
                foreach (UnitWrapper uw in units)
                {
                    if (uw.affilation == 2)
                    {
                        uw.affilation = 0;
                        uw.set_blue();
                    }
                }

                return;
            }

            if (latest_action.special_command == "start dragon")
            {
                _ = start_dragon();

                return;
            }
            else if (latest_action.special_command == "heal pulse")
            {
                _ = heal_pulse();
            }


            if (!latest_action.attack)
            {
                ProcessNonAttackAction(latest_action);
            }
            else
            {
                ProcessAttackAction(latest_action);
            }
        }
    }
    //TODO: DO ITEM SHIT
    void ProcessNonAttackAction(GameAction la)
    {
        state = "waiting for opponent unit traversal";
        opponent_unit = units[la.unit_ind];
        opponent_unit.weap_ind = la.item_ind;
        //Debug.Log(la.unit_ind);
        foreach (int md in la.move_dir)
        {
            if (md == 0) opponent_unit.QueueLeft();
            if (md == 1) opponent_unit.QueueRight();
            if (md == 2) opponent_unit.QueueUp();
            if (md == 3) opponent_unit.QueueDown();
        }
    }

    void WaitForOpponentUnitTraversal()
    {
        if (opponent_unit.IsMoving()) return;
        state = "not my turn";
    }

    void ProcessAttackAction(GameAction la)
    {
        state = "waiting for opponent unit traversal before attack";
        opponent_unit = units[la.unit_ind];
        opponent_unit.weap_ind = la.item_ind;
        //Debug.Log(la.unit_ind);
        foreach (int md in la.move_dir)
        {
            if (md == 0) opponent_unit.QueueLeft();
            if (md == 1) opponent_unit.QueueRight();
            if (md == 2) opponent_unit.QueueUp();
            if (md == 3) opponent_unit.QueueDown();
        }
        AnimLoader.Load(la.hp_blue, la.hp_red, la.dmg_blue, la.dmg_red, la.hit_blue, la.hit_red, la.crit_blue, la.crit_red, la.doub, units[la.blue_ind], units[la.red_ind]);
        AnimLoader.Load2(la.hit_left, la.dmg_left, la.crt_left, la.hp_left, la.mhp_left, la.hit_right, la.dmg_right, la.crt_right, la.hp_right, la.mhp_right);
    }

    void WaitForOpponentUnitTraversalBeforeAttack()
    {
        if (opponent_unit.IsMoving()) return;
        state = "executing attack by opponent";
        ExecuteOpponentAttack();
    }

    void ExecuteOpponentAttack()
    {
        AnimLoader.Go();
    }

    // ======= Quick diag bindings =======
    async Task ForceSaveDiag()
    {
        try
        {
            //Debug.Log("[Diag] F5 pressed -> Save action_list");
            await SaveToCloudAsync(al);
            //Debug.Log("[Diag] Save completed");
        }
        catch (Exception e)
        {
            //Debug.LogError("[Diag] Save failed: " + e);
        }
    }
    async Task ForceReloadDiag()
    {
        try
        {
            //Debug.Log("[Diag] F6 pressed -> Reload action_list");
            await ReloadActionsFromCloudAsync();
            //Debug.Log("[Diag] Reload completed. Count=" + (al?.actions?.Count ?? 0));
        }
        catch (Exception e)
        {
            //Debug.LogError("[Diag] Reload failed: " + e);
        }
    }
}
