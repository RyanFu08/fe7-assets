using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public class DraftControl : MonoBehaviour
{
  // ---------- Inspector Refs ----------
  [Header("Refs")]
  [SerializeField] private GameObject cursor;
  [SerializeField] private GameObject col;
  [SerializeField] private GameObject name;
  [SerializeField] private GameObject hpstat, mtstat, sklstat, spdstat, lckstat, defstat, resstat, movstat;

  // ---------- Shared State ----------
  // External code likely relies on these being public/static
  public static readonly List<UnitWrapper> units = new();
  public static readonly List<int>          unitstatus = new(); // -1 = undrafted, 0/1 = team, etc.
  private static readonly List<string>      unitNames = new();   // index-aligned with units

  // ---------- Components / Cached ----------
  private col             colCtrl;
  private cursor_animator curAnim;

  // ---------- Draft State ----------
  // 0 = not my turn, 1 = my turn
  public int isitmyturn = 0;

  [SerializeField] private float refreshThreshold = 1.5f;
  private float timer = 0f;

  private DraftActionList draftActions = new();
  private int draftActionIndex = 0;

  // Loader guards
  private bool _loading = false;
  private int _lastSeenCount = 0;

  // Hover cache to avoid redundant UI writes
  private int _lastHoverIndex = -2; // -2 = uninitialized; -1 = no hover

  // Click gating
  private bool  clicked = false;
  private float timeSinceLastClick = 0f;

  // Spawn locations for teams (post-swapped in Start)
  private static readonly Vector2Int[] V1Default =
  {
    new( 96,   0), new(112,   0), new(128,   0), new(144,   0), new(144, -16)
  };
  private static readonly Vector2Int[] V2Default =
  {
    new(-160,  0), new(-144,  0), new(-128,  0), new(-112,  0), new(-112, -16)
  };

  private Vector2Int[] v1 = (Vector2Int[])V1Default.Clone();
  private Vector2Int[] v2 = (Vector2Int[])V2Default.Clone();

  // ---------- Constants ----------
  private const double PosEps = 1e-3;

  // a: [name,hp,mt,skl,spd,lck,def,res,mov]
  private static readonly Dictionary<string, string[]> STATS = new()
  {
    ["Lyn"]       = new[] { "Lyn", "38", "18", "25", "25", "20", "10", "15", "7" },
    ["Eliwood"]   = new[] { "Eliwood", "40", "19", "18", "19", "17", "14", "12", "7" },
    ["Hector"]    = new[] { "Hector", "50", "24", "18", "16", "12", "22", "13", "5" },
    ["Florina"]   = new[] { "Florina", "40", "17", "20", "24", "20", "12", "18", "8" },
    ["Heath"]     = new[] { "Heath", "48", "22", "19", "20", "10", "20", "7", "8" },
    ["Lucius"]    = new[] { "Lucius", "36", "24", "18", "20", "12", "5",  "25", "6" },
    ["Pent"]      = new[] { "Pent", "38", "22", "21", "18", "14", "12", "19", "6" },
    ["Canas"]     = new[] { "Canas", "40", "22", "20", "16", "12", "12", "17", "6" },
    ["Priscilla"] = new[] { "Priscilla", "34", "20", "18", "20", "18", "9",  "23", "7" },
    ["Rath"]      = new[] { "Rath", "40", "20", "22", "22", "12", "12", "8",  "8" },
    ["Oswin"]     = new[] { "Oswin", "50", "24", "18", "13", "10", "26", "12", "5" },
    ["Wil"]       = new[] { "Wil", "40", "20", "22", "20", "15", "10", "8",  "6" },
    ["Raven"]     = new[] { "Raven", "45", "24", "23", "25", "8",  "12", "7",  "6" },
    ["Nils"]      = new[] { "Nils", "28", "0",  "14", "18", "25", "6",  "20", "5" },
    ["Serra"]     = new[] { "Serra", "34", "20", "18", "20", "22", "7",  "25", "6" },
    ["Sain"]      = new[] { "Sain", "46", "24", "18", "20", "12", "14", "8",  "8" },
    ["Kent"]      = new[] { "Kent", "44", "22", "22", "21", "10", "13", "9",  "8" },
    ["Lowen"]     = new[] { "Lowen", "50", "20", "16", "18", "12", "18", "8",  "8" },
  };

  // ---------- Unity Lifecycle ----------
  private void Awake()
  {
    // Safe initial cache
    if (col)    colCtrl = col.GetComponent<col>();
    if (cursor) curAnim = cursor.GetComponent<cursor_animator>();
  }

  private void OnDestroy()
  {
    // Clear cached references so Unity "destroyed" objects aren't used later
    colCtrl = null;
    curAnim = null;
  }

  private void Start()
  {
    RealInit();

    // Swap spawn arrays based on perspective
    if (RoomData.whoami != 0)
    {
      var tmp = v1; v1 = v2; v2 = tmp;
    }
  }

  private void Update()
  {
    // Click cool-down
    if (clicked)
    {
      timeSinceLastClick += Time.deltaTime;
      if (timeSinceLastClick >= 1f)
      {
        clicked = false;
        timeSinceLastClick = 0f;
      }
    }

    UpdateCursor();

    if (!clicked)
      HandleInput();

    // Periodic refresh
    timer += Time.deltaTime;
    if (timer >= refreshThreshold)
    {
      timer = 0f;
      _ = LoadDraftActionsAsync(); // throttled via _loading
    }
  }

  // ---------- Draft Loading / Applying ----------
  private async Task LoadDraftActionsAsync()
  {
    if (_loading) return;
    _loading = true;
    try { await LoadDraftActionList(); }
    catch (Exception e) { Debug.LogError($"[Draft] Refresh failed: {e}"); }
    finally { _loading = false; }
  }

  private async Task LoadDraftActionList()
  {
    var latest = await CloudPersistence.LoadDraftActionListAsync();
    if (latest == null || latest.actions == null) return;

    if (latest.actions.Count < draftActionIndex)
      draftActionIndex = 0;

    _lastSeenCount = latest.actions.Count;

    for (int i = draftActionIndex; i < latest.actions.Count; i++)
    {
      var da = latest.actions[i];
      if (da == null) continue;

      if (da.unit_id < 0 || da.unit_id >= unitstatus.Count || da.unit_id >= units.Count)
        continue;

      unitstatus[da.unit_id] = da.player_id;
      var uu = units[da.unit_id];

      var c = TryGetCol(); // safe component fetch at application time

      if (da.player_id == RoomData.whoami)
      {
        uu.set_blue();
        var slot = v1[i / 2];
        var uuw  = new UnitWrapper(uu.name, uu.class_type, uu.hp, slot.x, slot.y, 1);
        CombatLoader.add_unit(uuw);
        uuw.go_away();
      }
      else
      {
        c?.trigger_on(uu.cx, uu.cy, "red");
        uu.set_red();
        var slot = v2[i / 2];
        var uuw  = new UnitWrapper(uu.name, uu.class_type, uu.hp, slot.x, slot.y, 0);
        CombatLoader.add_unit(uuw);
        uuw.go_away();
      }

      if (i == 9)
        TransitionService.LoadScene("Combat");
    }

    draftActionIndex = latest.actions.Count;
    draftActions = latest;

    // Authoritative turn after applying all actions
    isitmyturn = draftActions.turn == RoomData.whoami ? 1 : 0;
  }

  // ---------- Input / Cursor / UI ----------
  private void HandleInput()
  {
    // No drafting here per your note; keeping the scaffold for future use
    if (isitmyturn == 0) return;

    if (Input.GetKeyDown(KeyCode.X))
    {
      int idx = FindUnitAtCursor();
      if (idx == -1) return;
      if (IsDrafted(idx)) return;

      clicked = true;

      // Optimistic local apply; server will validate on save
      unitstatus[idx] = RoomData.whoami;

      draftActions.actions.Add(new DraftAction { unit_id = idx, player_id = RoomData.whoami });
      draftActions.turn = 1 - draftActions.turn;

      _ = CloudPersistence.SaveDraftActionListAsync(draftActions);

      var uu = units[idx];
      var c  = TryGetCol();
      c?.trigger_on(uu.cx, uu.cy, "blue");
      uu.set_blue();

      isitmyturn = 0;

      // Nudge refresh soon after save
      timer = refreshThreshold + 0.1f;
    }
  }

  private void UpdateCursor()
  {
    int idx = FindUnitAtCursor();

    if (idx == _lastHoverIndex) return;
    _lastHoverIndex = idx;

    if (idx == -1) { ClearStats(); return; }

    if (idx >= 0 && idx < unitNames.Count)
    {
      string uname = unitNames[idx];
      if (STATS.TryGetValue(uname, out var a)) ApplyStats(a);
      else ClearStats();
    }
    else ClearStats();
  }

  private int FindUnitAtCursor()
  {
    if (!cursor) return -1;

    var p = cursor.transform.position;

    for (int i = 0; i < units.Count; i++)
    {
      var u = units[i];
      if (Math.Abs(u.cx - p.x) < PosEps && Math.Abs(u.cy - p.y) < PosEps)
        return i;
    }
    return -1;
  }

  private bool IsDrafted(int unitId)
  {
    if (unitId < 0 || unitId >= unitstatus.Count) return true;
    return unitstatus[unitId] != -1;
  }

  private void ApplyStats(string[] a)
  {
    // a: [name,hp,mt,skl,spd,lck,def,res,mov]
    Set(name,   a[0]);
    Set(hpstat, a[1]); Set(mtstat, a[2]); Set(sklstat, a[3]);
    Set(spdstat, a[4]); Set(lckstat, a[5]); Set(defstat, a[6]); Set(resstat, a[7]); Set(movstat, a[8]);
  }

  private void ClearStats()
  {
    Set(name, "");
    Set(hpstat, ""); Set(mtstat, ""); Set(sklstat, "");
    Set(spdstat, ""); Set(lckstat, ""); Set(defstat, ""); Set(resstat, ""); Set(movstat, "");
  }

  private static void Set(GameObject go, string v)
  {
    if (!go) return;
    var s = go.GetComponent<StringControl>();
    if (s != null) s.s = v;
  }

  // ---------- Helpers ----------
  // Unity "destroyed" objects are not C# null. This returns a live component or null.
  private col TryGetCol()
  {
    if (!colCtrl)
    {
      colCtrl = null;
      if (col) colCtrl = col.GetComponent<col>();
    }
    return colCtrl ? colCtrl : null;
  }

  // ---------- Bootstrap ----------
  public static void RealInit()
  {
    units.Clear();
    unitNames.Clear();
    unitstatus.Clear();

    // Helper local to keep unitNames aligned with units.Add
    void Add(string display, string cls, int cx, int cy, int z, int team, bool alive)
    {
      units.Add(new(display, cls, cx, cy, z, team, alive));
      unitNames.Add(display);
    }

    Add("Eliwood",  "Eliwood",      32, -96,  48, 2, true);
    Add("Hector",   "Hector",       32, -64,  48, 2, true);
    Add("Lyn",      "Lyn",          32, -32,  48, 2, true);
    Add("Florina",  "Florina",      32, -80,  32, 2, true);
    Add("Heath",    "WyvernLord",   32, -48,  32, 2, true);
    Add("Lucius",   "Bishop",       32, -96,  16, 2, true);
    Add("Pent",     "Sage",         32, -64,  16, 2, true);
    Add("Canas",    "Druid",        32, -32,  16, 2, true);
    Add("Priscilla","Valkyrie",     32, -80,   0, 2, true);
    Add("Rath",     "NomadTrooper", 32, -48,   0, 2, true);
    Add("Oswin",    "General",      32, -96, -16, 2, true);
    Add("Wil",      "Sniper",       32, -64, -16, 2, true);
    Add("Raven",    "Hero",         32, -32, -16, 2, true);
    Add("Serra",    "Bishop",       32, -48, -32, 2, true);
    Add("Sain",     "Paladin",      32, -96, -48, 2, true);
    Add("Kent",     "Paladin",      32, -64, -48, 2, true);
    Add("Lowen",    "Paladin",      32, -32, -48, 2, true);

    // Initialize status to undrafted
    for (int i = 0; i < units.Count; i++) unitstatus.Add(-1);
  }
}