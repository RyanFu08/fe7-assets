using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class readycontrol : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string initial_color; // "blue" or "red"
    [SerializeField] private GameObject username;      // has usernamecontrol
    [SerializeField] private GameObject ready;         // has usernamecontrol
    [SerializeField] private GameObject userstring;    // has StringControl
    [SerializeField] private GameObject readystring;   // has StringControl
    [SerializeField] public float interval = 3f;       // seconds between polls
    [SerializeField] public int id = 0;                // 0 = P1, 1 = P2 (this panel)

    // Local state
    private bool is_ready = false;         // local player's ready flag (for this device)
    private string lastCloudReady = "";    // last fetched cloud value for THIS panel ("YES"/"NONE")
    private string lastSentReady = "";     // last value we wrote for the *local* player
    private float timer = 0f;

    // Prevent overlapping cloud calls
    private int _busy = 0;

    // Fire scene load once
    private bool _combatLaunched = false;

    // Cached components
    private usernamecontrol _usernameCtrl;
    private usernamecontrol _readyCtrl;
    private StringControl _userStringCtrl;
    private StringControl _readyStringCtrl;

    private const string READY_YES = "YES";
    private const string READY_NONE = "NONE";
    private const string UI_READY = "   Ready!";
    private const string UI_NOT_READY = "Not Ready";

    private bool IsP1 => id == 0;
    private bool IsLocalPanel => id == RoomData.whoami;

    private void Awake()
    {
        _usernameCtrl   = username     ? username.GetComponent<usernamecontrol>()   : null;
        _readyCtrl      = ready        ? ready.GetComponent<usernamecontrol>()      : null;
        _userStringCtrl = userstring   ? userstring.GetComponent<StringControl>()   : null;
        _readyStringCtrl= readystring  ? readystring.GetComponent<StringControl>()  : null;
    }

    private async void Start()
    {
        SetPanelColor(initial_color);
        if (_userStringCtrl != null)  _userStringCtrl.s = "Loading…";
        if (_readyStringCtrl != null) _readyStringCtrl.s = UI_NOT_READY;

        await SyncFromCloudAsync();          // initial pull
        await CheckAndStartCombatAsync();    // in case both were already ready
    }

    private void Update()
    {
        if (IsLocalPanel && Input.GetKeyDown(KeyCode.X))
        {
            _ = SetLocalReadyAsync(true);
            timer = 0f;
        }
        if (IsLocalPanel && Input.GetKeyDown(KeyCode.Z))
        {
            _ = SetLocalReadyAsync(false);
            timer = 0f;
        }

        timer += Time.deltaTime;
        if (timer >= Mathf.Max(0.25f, interval))
        {
            timer = 0f;
            _ = PeriodicSyncAndCheckAsync();
        }
    }

    // Combine poll + combat check to avoid overlap races
    private async Task PeriodicSyncAndCheckAsync()
    {
        await SyncFromCloudAsync();
        await CheckAndStartCombatAsync();
    }

    // ----------------------------
    // Cloud I/O
    // ----------------------------

    private async Task SyncFromCloudAsync()
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        try
        {
            // Pull username for this panel
            string uname = IsP1
                ? await CloudPersistence.GetP1NameAsync()
                : await CloudPersistence.GetP2NameAsync();

            // Pull readiness for this panel
            string r = IsP1
                ? await CloudPersistence.GetReadyP1Async()
                : await CloudPersistence.GetReadyP2Async();

            lastCloudReady = r == READY_YES ? READY_YES : READY_NONE;

            // Keep local ready in sync with cloud on the local panel
            if (IsLocalPanel)
                is_ready = (lastCloudReady == READY_YES);

            // Update UI
            ApplyUsername(uname);
            ApplyReadyVisual(lastCloudReady);
        }
        catch (Exception e)
        {
            Debug.LogError("[readycontrol] SyncFromCloudAsync error: " + e);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private async Task SetLocalReadyAsync(bool readyFlag)
    {
        is_ready = readyFlag;
        string desired = is_ready ? READY_YES : READY_NONE;

        if (desired != lastSentReady)
        {
            try
            {
                if (IsLocalPanel)
                {
                    if (IsP1) await CloudPersistence.SetReadyP1Async(desired);
                    else      await CloudPersistence.SetReadyP2Async(desired);
                    lastSentReady = desired;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[readycontrol] SetLocalReadyAsync error: " + e);
            }
        }

        ApplyReadyVisual(desired);
        await CheckAndStartCombatAsync(); // react immediately after local change
    }

    // Check both players; launch once
    private async Task CheckAndStartCombatAsync()
    {
        if (_combatLaunched) return;

        string r1 = await CloudPersistence.GetReadyP1Async();
        string r2 = await CloudPersistence.GetReadyP2Async();

        bool bothReady = (r1 == READY_YES) && (r2 == READY_YES);
        if (bothReady)
        {
            _combatLaunched = true;
            TransitionService.LoadScene("Draft");
        }
    }

    // ----------------------------
    // UI helpers
    // ----------------------------

    private void SetPanelColor(string colorName)
    {
        if (_usernameCtrl == null || _readyCtrl == null) return;

        if (string.Equals(colorName, "red", StringComparison.OrdinalIgnoreCase))
        {
            _usernameCtrl.setcolor("red");
            _readyCtrl.setcolor("red");
        }
        else if (string.Equals(colorName, "blue", StringComparison.OrdinalIgnoreCase))
        {
            _usernameCtrl.setcolor("blue");
            _readyCtrl.setcolor("blue");
        }
        else
        {
            _usernameCtrl.setcolor("blue");
            _readyCtrl.setcolor("blue");
        }
    }

    private void ApplyUsername(string uname)
    {
        if (_userStringCtrl == null || _usernameCtrl == null) return;

        string u = string.IsNullOrEmpty(uname) ? "NONE" : uname;
        _userStringCtrl.s = u;
        _usernameCtrl.dir = (u == "NONE") ? "up" : "down";
    }

    private void ApplyReadyVisual(string cloudReadyValue)
    {
        if (_readyCtrl == null || _readyStringCtrl == null) return;

        bool isR = cloudReadyValue == READY_YES;
        _readyStringCtrl.s = isR ? UI_READY : UI_NOT_READY;
        _readyCtrl.setcolor(isR ? "green" : initial_color);
    }
}
