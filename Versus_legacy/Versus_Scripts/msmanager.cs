using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class msmanager : MonoBehaviour {
    public string ms_state = "stand"; //stand, ready, move, lock, enemy, opt
    public string my_state = "hide"; //hide, path

    public float move = 5;
    private List<int> atk_range = new List<int>{1,2};

    public GameObject ms, cc;
    
    private readonly (int, int)[] ok_pos = new (int, int)[] {(-96, -96),(-96, -112),(-80, -112),(-64, -112),(-64, -128),(-48, -128),(-48, -112),(-16, -112),(-16, -128),(-32, -128),(0, -128),(0, -112),(16, -128),(32, -128),(32, -112),(48, -112),(48, -128),(48, -144),(64, -144),(64, -128),(64, -112),(64, -112),(80, -112),(80, -128),(80, -144),(96, -144),(96, -160),(96, -176),(112, -176),(112, -160),(112, -144),(128, -144),(144, -144),(160, -144),(176, -144),(176, -128),(160, -128),(144, -128),(128, -128),(112, -128),(96, -128),(96, -112),(112, -112),(128, -112),(144, -112),(160, -112),(176, -112),(176, -96),(160, -96),(144, -96),(128, -96),(112, -96),(96, -96),(80, -96),(80, -80),(112, -80),(144, -80),(160, -80),(176, -80),(176, -64),(176, -48),(176, -32),(176, -16),(192, -48),(192, -64),(208, -64),(224, -64),(224, -48),(224, -32),(224, -16),(224, 0),(208, 0),(240, 0),(240, -16),(240, -32),(240, -48),(240, -64),(240, -80),(240, -80),(240, -96),(240, -112),(240, -128),(240, -144),(240, -160),(240, -176),(240, -192),(240, -208),(240, -224),(224, -224),(208, -224),(192, -224),(176, -224),(160, -224),(144, -224),(128, -224),(112, -224),(96, -224),(80, -224),(64, -224),(48, -224),(48, -208),(64, -208),(80, -208),(96, -208),(112, -208),(128, -208),(144, -208),(160, -208),(176, -208),(192, -208),(208, -208),(224, -208),(224, -192),(224, -176),(224, -160),(224, -144),(224, -128),(224, -112),(224, -96),(224, -80),(208, -144),(208, -160),(208, -176),(208, -192),(96, -192),(112, -192),(-224, -192),(-208, -192),(-192, -192),(-176, -192),(-160, -192),(-144, -192),(-128, -192),(-112, -192),(-96, -192),(-80, -192),(-64, -192),(-48, -192),(-32, -192),(-16, -192),(0, -192),(16, -192),(16, -208),(16, -224),(0, -224),(-16, -224),(-32, -224),(-48, -224),(-64, -224),(-80, -224),(-96, -224),(-112, -224),(-128, -224),(-144, -224),(-160, -224),(-176, -224),(-192, -224),(-208, -224),(-224, -224),(-224, -208),(-208, -208),(-192, -208),(-176, -208),(-160, -208),(-144, -208),(-128, -208),(-112, -208),(-96, -208),(-80, -208),(-64, -208),(-48, -208),(-32, -208),(-16, -208),(0, -208),(0, -32),(16, -32),(0, -32),(0, -48),(16, -48),(32, -48),(0, -64),(0, -32),(0, -32),(-16, -32),(-16, -48),(0, -32),(0, -48),(16, -48),(16, -32),(32, -48),(0, -64),(-16, -64),(-16, -80),(0, -80),(-16, -48),(-32, -48),(-48, -48),(-32, -32),(-16, -32),(-32, -16),(-16, -16),(0, -16),(16, -16),(0, 0),(-16, 0),(-48, 0),(-64, 0),(-64, -16),(48, -16),(48, 0),(32, 0),(96, 0),(112, 0),(128, 0),(144, 0),(144, -16),(144, -32),(128, -32),(112, -32),(96, -32),(96, -16),(112, -16),(128, -16),(112, -48),(112, -64),(112, -80),(112, -96),(112, -112),(112, -128),(112, -144),(112, -160),(112, -176),(112, -192),(112, -208),(112, -224),(96, -224),(80, -224),(64, -224),(48, -224),(48, -208),(64, -208),(96, -208),(128, -224),(144, -224),(160, -224),(176, -224),(192, -224),(208, -224),(224, -224),(240, -224),(240, -208),(224, -208),(208, -208),(192, -208),(176, -208),(160, -208),(144, -208),(240, -192),(224, -192),(208, -192),(208, -176),(224, -176),(240, -176),(240, -160),(224, -160),(208, -160),(208, -144),(224, -144),(240, -144),(240, -128),(224, -128),(224, -112),(240, -112),(240, -96),(224, -96),(224, -80),(240, -80),(240, -64),(224, -64),(224, -48),(240, -48),(240, -32),(224, -32),(224, -16),(240, -16),(240, 0),(224, 0),(208, 0),(208, 16),(224, 16),(240, 16),(240, 32),(224, 32),(208, 32),(192, 32),(176, 32),(160, 32),(144, 32),(128, 32),(112, 32),(96, 32),(80, 32),(240, 48),(224, 48),(208, 48),(192, 48),(176, 48),(160, 48),(144, 48),(128, 48),(112, 48),(96, 48),(80, 48),(64, 48),(48, 48),(32, 48),(16, 48),(0, 48),(-16, 48),(-32, 48),(-48, 48),(-64, 48),(-80, 48),(-96, 48),(-112, 48),(-128, 48),(-144, 48),(-160, 48),(-176, 48),(-192, 48),(-208, 48),(-224, 48),(-224, 32),(-208, 32),(-192, 32),(-176, 32),(-160, 32),(-144, 32),(-128, 32),(-112, 32),(-96, 32),(-224, 208),(-208, 208),(-192, 208),(-176, 208),(-160, 208),(-144, 208),(-128, 208),(-112, 208),(-112, 192),(-128, 192),(-128, 176),(-128, 160),(-128, 144),(-160, 128),(-144, 128),(-128, 128),(-112, 128),(-96, 128),(-80, 128),(-64, 128),(-48, 128),(-32, 128),(-16, 128),(0, 128),(16, 128),(32, 128),(48, 128),(64, 128),(80, 128),(80, 112),(64, 112),(48, 112),(32, 112),(16, 112),(0, 112),(-16, 112),(-32, 112),(-48, 112),(-64, 112),(-80, 112),(-96, 112),(-112, 112),(-128, 112),(-144, 112),(-160, 112),(-224, 192),(-208, 192),(-192, 192),(-176, 192),(-160, 192),(-144, 192),(-192, 176),(-192, 160),(-224, 176),(-224, 160),(-224, 144),(-208, 144),(-192, 144),(-192, 128),(-208, 128),(-224, 128),(-224, 112),(-208, 112),(-192, 112),(-192, 96),(-224, 96),(-224, 80),(-192, 80),(-192, 64),(-208, 64),(-224, 64),(-224, 16),(-224, 0),(-224, -16),(-224, -32),(-224, -48),(-224, -64),(-224, -96),(-224, -112),(-224, -128),(-208, -128),(-208, -112),(-208, -96),(-208, -64),(-208, -48),(-208, -32),(-208, -16),(-192, 16),(-192, 0),(-192, -16),(-192, -32),(-192, -48),(-192, -64),(-192, -80),(-192, -96),(-192, -112),(-192, -128),(-160, 0),(-160, -16),(-160, -32),(-144, -32),(-144, -16),(-144, 0),(-128, 0),(-128, -16),(-128, -32),(-112, -32),(-112, -16),(-112, 0),(-144, -48),(-144, -64),(-144, -80),(-144, -96),(-160, -96),(-176, -96),(-176, -80),(-176, -112),(-176, -128),(-160, -128),(-160, -112),(-144, -112),(-144, -128),(-128, -96),(-128, -112),(-128, -128),(-128, -144),(-128, -160),(-128, -176),(-128, -192),(-224, -192),(-224, -208),(-224, -224),(-208, -224),(-208, -208),(-208, -192),(-192, -192),(-192, -208),(-192, -224),(-176, -224),(-176, -208),(-176, -192),(-160, -192),(-160, -208),(-160, -224),(-144, -224),(-144, -208),(-128, -208),(-128, -224),(-112, -224),(-96, -224),(-80, -224),(-64, -224),(-48, -224),(-32, -224),(-16, -224),(0, -224),(16, -224),(16, -208),(16, -192),(0, -192),(-16, -192),(-32, -192),(-48, -192),(-64, -192),(-64, -208),(-48, -208),(-32, -208),(-16, -208),(0, -208),(-80, -208),(-96, -208),(-112, -208),(-112, -192),(-96, -192),(-96, -176),(-112, -176),(-112, -160),(-96, -160),(-96, -144),(-112, -144),(-112, -128),(-96, -128),(-96, -112),(-112, -112),(-112, -96),(-96, -96),(-96, -80),(-112, -80),(-80, -112),(-80, -128),(-64, -128),(-64, -112),(-48, -112),(-48, -128),(-32, -128),(-16, -128),(-16, -112),(0, -112),(0, -96),(-16, -96),(0, -128),(16, -128),(32, -128),(48, -128),(48, -144),(64, -144),(80, -144),(96, -144),(96, -160),(96, -176),(96, -128),(80, -128),(64, -128),(32, -112),(48, -112),(64, -112),(80, -112),(96, -112),(96, -96),(80, -96),(80, -80),(128, -144),(144, -144),(160, -144),(176, -144),(176, -128),(160, -128),(144, -128),(128, -128),(128, -112),(144, -112),(160, -112),(176, -112),(176, -96),(160, -96),(144, -96),(128, -96),(144, -80),(160, -80),(176, -80),(176, -64),(176, -48),(176, -32),(176, -16),(192, -48),(192, -64),(-80, 208),(-80, 192),(-80, 176),(-80, 160),(-80, 144),(-64, 144),(-48, 144),(-32, 144),(-16, 144),(0, 144),(16, 144),(32, 144),(48, 144),(64, 144),(80, 144),(96, 144),(112, 144),(128, 144),(144, 144),(160, 144),(176, 144),(192, 144),(208, 144),(224, 144),(240, 144),(240, 160),(224, 160),(208, 160),(192, 160),(176, 160),(160, 160),(144, 160),(128, 160),(112, 160),(96, 160),(80, 160),(64, 160),(48, 160),(32, 160),(16, 160),(0, 160),(-16, 160),(-32, 160),(-48, 160),(-64, 160),(-64, 160),(-64, 176),(-64, 192),(-64, 208),(-48, 208),(-32, 208),(-16, 208),(0, 208),(16, 208),(32, 208),(48, 208),(64, 208),(80, 208),(80, 192),(80, 176),(64, 176),(48, 176),(32, 176),(16, 176),(0, 176),(-16, 176),(-32, 176),(-48, 176),(-48, 192),(-32, 192),(-16, 192),(0, 192),(16, 192),(32, 192),(48, 192),(64, 192),(112, 176),(128, 176),(144, 176),(160, 176),(176, 176),(192, 176),(208, 176),(224, 176),(240, 176),(16, 64),(32, 64),(48, 64),(64, 64),(80, 64),(96, 64),(112, 64),(128, 64),(144, 64),(160, 64),(176, 64),(192, 64),(208, 64),(208, 80),(224, 80),(240, 80),(240, 96),(224, 96),(192, 128),(192, 112),(192, 96),(192, 80),(176, 80),(160, 80),(144, 80),(128, 80),(112, 80),(112, 96),(128, 96),(144, 96),(160, 96),(96, -192),(208, -64)};

    private List<(int,int)> mov_pos = new List<(int,int)>();
    private List<(int,int)> atk_pos = new List<(int,int)>();    

    public GameObject tile_prefab, arrow_prefab;

    private List<GameObject> tiles, arrows;
    private Dictionary<(int,int),(int,int)> prev = new Dictionary<(int,int),(int,int)>();

    private float arrow_target_x = -1, arrow_target_y = -1;

    void Start() {
        tiles = new List<GameObject>();
        arrows = new List<GameObject>();
        arrow_target_x = -1; arrow_target_y = -1;
    }


    void Update() {
        string mss = ms.GetComponent<MapSprite_Control>().state;
        if (mss == "up" || mss == "down" || mss == "left" || mss == "right") ms_state = "move";
        else if (ms_state == "move") ms_state = "stand";

        if (ms_state != "move") expand_cursor_if_necessary();
        if (ms_state == "ready" && my_state == "hide" && Input.GetKeyDown(KeyCode.X)) show_range();
        if (my_state == "path" && Input.GetKeyDown(KeyCode.Z)) {hide_range(); hide_arrow();}

        if (my_state == "path") {
            ms.GetComponent<MapSprite_Control>().state = "ready";
            ms_state = "ready";

            int ccx = Mathf.RoundToInt(cc.transform.position.x);
            int ccy = Mathf.RoundToInt(cc.transform.position.y);

            if (mov_pos.Contains((ccx,ccy))) {
                if (arrow_target_x != ccx || arrow_target_y != ccy) {
                    hide_arrow();
                    arrow_target_x = ccx; arrow_target_y = ccy;
                    display_arrow_to((ccx,ccy),(-1,-1));
                }
                if (Input.GetKeyDown(KeyCode.Q)) {
                    hide_range(); hide_arrow();
                    List<(int,int)> path = get_cells_to((ccx,ccy));
                    path.Reverse();
                    for (int i=1; i<path.Count; i++) {
                        if (path[i].Item1 > path[i-1].Item1) ms.GetComponent<MapSprite_Control>().GlideRight();
                        if (path[i].Item1 < path[i-1].Item1) ms.GetComponent<MapSprite_Control>().GlideLeft();
                        if (path[i].Item2 > path[i-1].Item2) ms.GetComponent<MapSprite_Control>().GlideUp();
                        if (path[i].Item2 < path[i-1].Item2) ms.GetComponent<MapSprite_Control>().GlideDown();
                    }
                }
            }

        }
    }

    void show_range() {
        my_state = "path";
        show_blue_tiles();
        show_red_tiles();
    }

    List<(int,int)> get_cells_to((int,int) loc) {
        List<(int,int)> path = new List<(int,int)>(); path.Add(loc);
        while (prev[loc] != (-1,-1)) {
            path.Add(prev[loc]);
            loc = prev[loc];
        }
        return path;
    }

    void display_arrow_to((int,int) cur, (int,int) nxt) {
        int px = prev[cur].Item1, py = prev[cur].Item2;
        int cx = cur.Item1, cy = cur.Item2;
        int nx = nxt.Item1, ny = nxt.Item2;

        GameObject go = Instantiate(arrow_prefab, Vector3.zero, Quaternion.identity);
        arrow ar = go.GetComponent<arrow>();
        ar.go_to(cx,cy);

        if (px == -1 && py == -1) {
            if (nx > cx) {ar.state = "start_e";}
            if (nx < cx) {ar.state = "start_w";}
            if (ny > cy) {ar.state = "start_n";}
            if (ny < cy) {ar.state = "start_s";}
        } else if (nxt == (-1,-1)) {
            if (cx > px) {ar.state = "end_w";}
            if (cx < px) {ar.state = "end_e";}
            if (cy > py) {ar.state = "end_s";}
            if (cy < py) {ar.state = "end_n";}
            display_arrow_to(prev[cur], cur);
        } else {
            int val = 0;
            if (nx > cx || px > cx) {val += 1;}
            if (nx < cx || px < cx) {val += 10;}
            if (ny > cy || py > cy) {val += 100;}
            if (ny < cy || py < cy) {val += 1000;}

            if (val == 11) {ar.state = "we";}
            if (val == 1100) {ar.state = "ns";}
            if (val == 101) {ar.state = "ne";}
            if (val == 1010) {ar.state = "ws";}
            if (val == 110) {ar.state = "wn";}
            if (val == 1001) {ar.state = "se";}
            display_arrow_to(prev[cur], cur);
        }
        arrows.Add(go);
    }
    void hide_arrow() {
        arrow_target_x = -1;
        arrow_target_y = -1;
        foreach (GameObject obj in arrows) {
            Destroy(obj);
        }
        arrows.Clear();
    }

    void show_blue_tiles() {
        prev.Clear();
        mov_pos.Clear();
        Queue<(int,(int,int),(int,int))> q = new Queue<(int,(int,int),(int,int))>();
        HashSet<(int, int)> vis = new HashSet<(int, int)>();

        q.Enqueue(
            (0,
                (
                    (int)(16f*ms.GetComponent<MapSprite_Control>().cx),
                    (int)(16f*ms.GetComponent<MapSprite_Control>().cy)
                ),
                (-1,-1)
            )
        );
        while (q.Count > 0) {
            (int,(int,int),(int,int)) a = q.Dequeue();
            int dist = a.Item1; (int,int) pos = a.Item2;
            if (!ok_pos.Contains(pos)) continue;
            if (vis.Contains(pos)) continue;
            vis.Add(pos);
            prev[pos] = a.Item3;
            if (dist <= move) {
                mov_pos.Add(pos);
                q.Enqueue((dist+1,(pos.Item1+16,pos.Item2),pos));
                q.Enqueue((dist+1,(pos.Item1-16,pos.Item2),pos));
                q.Enqueue((dist+1,(pos.Item1,pos.Item2+16),pos));
                q.Enqueue((dist+1,(pos.Item1,pos.Item2-16),pos));
            }
        }

        foreach ((int,int) pos in mov_pos) {
            GameObject go = Instantiate(tile_prefab, Vector3.zero, Quaternion.identity);
            TileHighlight pf = go.GetComponent<TileHighlight>();
            pf.go_to(pos.Item1, pos.Item2);
            pf.set("blue");
            tiles.Add(go);
        }
    }

    void show_red_tiles() {
        atk_pos.Clear();
        int mx_rg = atk_range.Max();

        Queue<(int,(int,int))> q = new Queue<(int,(int,int))>();
        HashSet<(int, int)> vis = new HashSet<(int, int)>();

        foreach ((int,int) pos in mov_pos) {
            q.Enqueue((0,pos));
        }

        while (q.Count > 0) {
            (int,(int,int)) a = q.Dequeue();
            int dist = a.Item1; (int,int) pos = a.Item2;
            if (vis.Contains(pos)) continue;
            vis.Add(pos);
            if (dist <= mx_rg) {
                if (!mov_pos.Contains(pos) && atk_range.Contains(dist)) {atk_pos.Add(pos);}
                q.Enqueue((dist+1,(pos.Item1+16,pos.Item2)));
                q.Enqueue((dist+1,(pos.Item1-16,pos.Item2)));
                q.Enqueue((dist+1,(pos.Item1,pos.Item2+16)));
                q.Enqueue((dist+1,(pos.Item1,pos.Item2-16)));
            }
        }

        foreach ((int,int) pos in atk_pos) {
            GameObject go = Instantiate(tile_prefab, Vector3.zero, Quaternion.identity);
            TileHighlight pf = go.GetComponent<TileHighlight>();
            pf.go_to(pos.Item1, pos.Item2);
            pf.set("red");
            tiles.Add(go);
        }

    }

    void hide_range() {
        my_state = "hide";
        ms_state = "stand";
        ms.GetComponent<MapSprite_Control>().state = "stand";
        foreach (GameObject obj in tiles) {
            Destroy(obj);
        }
        tiles.Clear();
    }

    void expand_cursor_if_necessary() {
        float sx = ms.GetComponent<MapSprite_Control>().cx * 16;
        float sy = ms.GetComponent<MapSprite_Control>().cy * 16;
        Vector3 ccPos = cc.transform.position;
        int ccx = Mathf.RoundToInt(ccPos.x / 16f);
        int ccy = Mathf.RoundToInt(ccPos.y / 16f);
        int tx  = Mathf.RoundToInt(sx  / 16f);
        int ty  = Mathf.RoundToInt(sy  / 16f);
        if (ccx == tx && ccy == ty) {
            if (ms_state == "stand") {
                ms.GetComponent<MapSprite_Control>().state = "ready";
                ms_state = "ready";
                cc.GetComponent<CursorControl2>().freeze_big();
            }
        } else {
            if (ms_state == "ready") {
                ms.GetComponent<MapSprite_Control>().state = "stand";
                ms_state = "stand";
                cc.GetComponent<CursorControl2>().unfreeze_big();
            }
        }
    }
}

//180712
