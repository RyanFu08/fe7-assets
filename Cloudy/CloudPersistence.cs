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

public static class CloudAutostart
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async void Prewarm()
    {
        Debug.Log("[CloudAutostart] Prewarming CloudPersistence…");
        try
        {
            await CloudPersistence.EnsureReadyAsync();
            Debug.Log($"[CloudAutostart] Ready. IsSignedIn={CloudPersistence.IsSignedIn}, PlayerId={CloudPersistence.PlayerId}, Env='{CloudPersistence.CurrentEnvironment}'");
        }
        catch (Exception e) { Debug.LogError("[CloudAutostart] Init failed: " + e); }
    }
}

public static class UserData
{
    public static string current_username, current_password;
    public static bool IsLoggedIn => !string.IsNullOrEmpty(current_username);
    public static void Logout() { current_username = null; current_password = null; }

    public static async Task<bool> TryLoginAsync(string u, string p)
    {
        try
        {
            await CloudPersistence.EnsureReadyAsync();
            bool ok = await CloudPersistence.VerifyUserPlainAsync(u, p);
            current_username = ok ? u : null;
            current_password = ok ? p ?? "" : null;
            Debug.Log($"[UserData] Login {(ok ? "OK" : "FAIL")} for '{u}'.");
            return ok;
        }
        catch (Exception e) { Debug.LogError("[UserData] TryLoginAsync error: " + e); return false; }
    }

    public static async Task<bool> TrySignUpAsync(string u, string p, bool overwrite = false)
    {
        try
        {
            await CloudPersistence.EnsureReadyAsync();
            bool created = await CloudPersistence.CreateUserPlainAsync(u, p, overwrite);
            current_username = created ? u : null;
            current_password = created ? p ?? "" : null;
            Debug.Log($"[UserData] Sign-up {(created ? "OK" : "FAIL")} for '{u}' (overwrite={overwrite}).");
            return created;
        }
        catch (Exception e) { Debug.LogError("[UserData] TrySignUpAsync error: " + e); return false; }
    }
}

static class CloudPersistence
{
    const string ENVIRONMENT = "production";
    const string FIXED_USERNAME = "fe7-fixed-user";
    const string FIXED_PASSWORD = "ChangeMe!12345";
    const int MAX_RETRIES = 3;

    static Task _initTask;
    static readonly object _gate = new();

    static string _currentRoomId;
    public static string CurrentEnvironment => ENVIRONMENT;
    public static bool IsSignedIn => AuthenticationService.Instance?.IsSignedIn ?? false;
    public static string PlayerId => AuthenticationService.Instance?.PlayerId;
    public static bool HasRoom => !string.IsNullOrEmpty(_currentRoomId);
    public static string CurrentRoomId => _currentRoomId;

    public static async Task EnsureReadyAsync()
    {
        if (_initTask != null) { await _initTask; return; }
        lock (_gate) { _initTask ??= InitializeAsyncInternal(); }
        await _initTask;
    }

    static async Task InitializeAsyncInternal()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var opt = new InitializationOptions().SetEnvironmentName(ENVIRONMENT).SetProfile("fixed");
                await UnityServices.InitializeAsync(opt);
                Debug.Log($"[UGS] Initialized. Env='{ENVIRONMENT}', profile='fixed', appId={Application.cloudProjectId}");
            }
            await SignInFixedUserAsync();
            Debug.Log($"[Auth] Fixed user signed in. PlayerId={AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e) { Debug.LogError("[UGS] Initialize/Auth failed: " + e); throw; }
    }

    static async Task SignInFixedUserAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn) return;
        try { await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(FIXED_USERNAME, FIXED_PASSWORD); return; }
        catch (RequestFailedException rfe) { Debug.LogWarning($"[Auth] Sign-in failed ({rfe.ErrorCode}): {rfe.Message}. Will try sign-up."); }
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(FIXED_USERNAME, FIXED_PASSWORD);
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(FIXED_USERNAME, FIXED_PASSWORD);
        }
        catch (RequestFailedException rfe2)
        {
            var m = rfe2.Message ?? "";
            if (m.IndexOf("USERNAME_PASSWORD", StringComparison.OrdinalIgnoreCase) >= 0 && m.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) >= 0)
                Debug.LogError("[Auth] Username & Password provider is disabled. Enable it in UGS Dashboard > Authentication > Settings.");
            else if (m.IndexOf("USERNAME_TAKEN", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0)
                Debug.LogError("[Auth] Fixed username exists with a different password. Update FIXED_PASSWORD, reset that user, or choose a new FIXED_USERNAME.");
            else Debug.LogError($"[Auth] Sign-up/sign-in failed ({rfe2.ErrorCode}): {rfe2.Message}");
            throw;
        }
    }

    // -------- Rooms --------
    public static Task<bool> SetRoom(string roomId) => SetRoomAsync(roomId);
    public static async Task<bool> SetRoomAsync(string roomId)
    {
        await EnsureReadyAsync();
        var canon = CanonRoom(roomId);
        if (string.IsNullOrEmpty(canon)) { Debug.LogWarning("[Rooms] SetRoom: invalid id."); return false; }
        _currentRoomId = canon;

        string actionKey = RoomActionKey(canon);
        string json = await LoadPlayerJsonAsync(actionKey, allowReauthRetry: true, logOnMissing: false);
        if (string.IsNullOrEmpty(json))
        {
            await SaveManyAsync(new()
            {
                [RoomActionKey(canon)] = JsonUtility.ToJson(new ActionList()),
                [RoomDraftActionKey(canon)] = JsonUtility.ToJson(new DraftActionList()),
                [RoomLeafKey(canon, P1_NAME_LEAF)] = "NONE",
                [RoomLeafKey(canon, P2_NAME_LEAF)] = "NONE",
                [RoomLeafKey(canon, READY_P1_LEAF)] = "NONE",
                [RoomLeafKey(canon, READY_P2_LEAF)] = "NONE",
            });
            Debug.Log($"[Rooms] Created room '{canon}'.");
        }
        Debug.Log($"[Rooms] Current room set to '{_currentRoomId}'.");
        return true;
    }

    public static async Task<bool> ClearRoom(string roomId)
    {
        await EnsureReadyAsync();
        var canon = CanonRoom(roomId);
        if (string.IsNullOrEmpty(canon)) { Debug.LogWarning("[Rooms] ClearRoom: invalid id."); return false; }
        await SaveManyAsync(new()
        {
            [RoomActionKey(canon)] = JsonUtility.ToJson(new ActionList()),
            [RoomDraftActionKey(canon)] = JsonUtility.ToJson(new DraftActionList()),
            [RoomLeafKey(canon, P1_NAME_LEAF)] = "NONE",
            [RoomLeafKey(canon, P2_NAME_LEAF)] = "NONE",
            [RoomLeafKey(canon, READY_P1_LEAF)] = "NONE",
            [RoomLeafKey(canon, READY_P2_LEAF)] = "NONE",
        });
        Debug.Log($"[Rooms] Cleared room '{canon}'.");
        return true;
    }

    const string ACTION_KEY_LEAF = "action_list";
    const string P1_NAME_LEAF = "p1_name";
    const string P2_NAME_LEAF = "p2_name";
    const string READY_P1_LEAF = "ready_p1";
    const string READY_P2_LEAF = "ready_p2";
    const string DRAFT_ACTION_KEY_LEAF = "draft_action_list";


    public static async Task<ActionList> LoadActionListAsync()
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning("[ActionList] Load: no room set."); return new ActionList(); }
        var json = await LoadPlayerJsonAsync(RoomActionKey(_currentRoomId), allowReauthRetry: true);
        return string.IsNullOrEmpty(json) ? new ActionList() : (JsonUtility.FromJson<ActionList>(json) ?? new ActionList());
    }

    public static async Task<bool> SaveActionListAsync(ActionList al)
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning("[ActionList] Save: no room set."); return false; }
        await SaveManyAsync(new() { [RoomActionKey(_currentRoomId)] = JsonUtility.ToJson(al ?? new ActionList()) });
        return true;
    }

    public static async Task<DraftActionList> LoadDraftActionListAsync()
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning("[DraftActionList] Load: no room set."); return new DraftActionList(); }
        var json = await LoadPlayerJsonAsync(RoomDraftActionKey(_currentRoomId), allowReauthRetry: true);
        return string.IsNullOrEmpty(json) ? new DraftActionList() : (JsonUtility.FromJson<DraftActionList>(json) ?? new DraftActionList());
    }

    public static async Task<bool> SaveDraftActionListAsync(DraftActionList dal)
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning("[DraftActionList] Save: no room set."); return false; }
        await SaveManyAsync(new() { [RoomDraftActionKey(_currentRoomId)] = JsonUtility.ToJson(dal ?? new DraftActionList()) });
        return true;
    }


    public static async Task<(string p1, string p2, string r1, string r2)> LoadRoomSnapshotAsync()
    {
        await EnsureReadyAsync(); if (!HasRoom) return ("", "", "NONE", "NONE");
        var keys = new HashSet<string>{
            RoomLeafKey(_currentRoomId,P1_NAME_LEAF),
            RoomLeafKey(_currentRoomId,P2_NAME_LEAF),
            RoomLeafKey(_currentRoomId,READY_P1_LEAF),
            RoomLeafKey(_currentRoomId,READY_P2_LEAF),
        };
        var d = await LoadManyAsync(keys);
        string Get(string k) => d.TryGetValue(k, out var v) ? v : null;
        return (
            Get(RoomLeafKey(_currentRoomId, P1_NAME_LEAF)) ?? "",
            Get(RoomLeafKey(_currentRoomId, P2_NAME_LEAF)) ?? "",
            Get(RoomLeafKey(_currentRoomId, READY_P1_LEAF)) ?? "NONE",
            Get(RoomLeafKey(_currentRoomId, READY_P2_LEAF)) ?? "NONE"
        );
    }

    // names
    public static Task<string> GetP1NameAsync() => GetLeafAsync(P1_NAME_LEAF);
    public static Task<string> GetP2NameAsync() => GetLeafAsync(P2_NAME_LEAF);
    public static Task<bool> SetP1NameAsync(string v) => SetLeafAsync(P1_NAME_LEAF, v);
    public static Task<bool> SetP2NameAsync(string v) => SetLeafAsync(P2_NAME_LEAF, v);

    // readiness
    public static Task<string> GetReadyP1Async() => GetLeafAsync(READY_P1_LEAF);
    public static Task<string> GetReadyP2Async() => GetLeafAsync(READY_P2_LEAF);
    public static Task<bool> SetReadyP1Async(string v) => SetLeafAsync(READY_P1_LEAF, v);
    public static Task<bool> SetReadyP2Async(string v) => SetLeafAsync(READY_P2_LEAF, v);

    static async Task<string> GetLeafAsync(string leaf)
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning($"[RoomLeaf] Get '{leaf}': no room set."); return ""; }
        return await LoadPlayerJsonAsync(RoomLeafKey(_currentRoomId, leaf), allowReauthRetry: true) ?? "";
    }

    static async Task<bool> SetLeafAsync(string leaf, string value)
    {
        await EnsureReadyAsync();
        if (!HasRoom) { Debug.LogWarning($"[RoomLeaf] Set '{leaf}': no room set."); return false; }
        await SaveManyAsync(new() { [RoomLeafKey(_currentRoomId, leaf)] = value ?? "" });
        return true;
    }

    // -------- Cloud Save primitives --------
    static string RoomActionKey(string canonRoomId) => RoomLeafKey(canonRoomId, ACTION_KEY_LEAF);
    static string RoomDraftActionKey(string canonRoomId) => RoomLeafKey(canonRoomId, DRAFT_ACTION_KEY_LEAF);

    static string RoomLeafKey(string canonRoomId, string leaf) => $"rooms__{canonRoomId}__{leaf}";

    static string CanonRoom(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        id = id.Trim().ToLowerInvariant();

        var buf = new char[id.Length];
        int n = 0;
        for (int i = 0; i < id.Length; i++)
        {
            char c = id[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
                buf[n++] = c;
        }
        return n == 0 ? null : new string(buf, 0, n);
    }


    static void ValidateKeyOrThrow(string k)
    {
        if (string.IsNullOrEmpty(k) || k.Length > 255) throw new Exception($"Bad key length: '{k}'");
        foreach (var c in k) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) throw new Exception($"Bad key char '{c}' in '{k}'");
    }

    static async Task<Dictionary<string, string>> LoadManyAsync(HashSet<string> keys)
    {
        foreach (var k in keys) ValidateKeyOrThrow(k);
        try
        {
            var res = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            var d = new Dictionary<string, string>(res.Count);
            foreach (var kv in res) d[kv.Key] = kv.Value?.Value?.GetAs<string>();
            Debug.Log($"[CloudSave] Loaded {d.Count}/{keys.Count} keys.");
            return d;
        }
        catch (CloudSaveException ex)
        {
            if (IsUnauthorized(ex)) { await Reauthenticate(); return await LoadManyAsync(keys); }
            Debug.LogError($"[CloudSave] LoadMany failed: {ex}"); throw;
        }
    }

    static async Task SaveManyAsync(Dictionary<string, string> fields)
    {
        if (fields == null || fields.Count == 0) return;
        foreach (var k in fields.Keys) ValidateKeyOrThrow(k);
        await Retry(async () =>
        {
            try
            {
                var payload = fields.ToDictionary(kv => kv.Key, kv => (object)(kv.Value ?? ""));
                await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
                Debug.Log($"[CloudSave] Saved {fields.Count} keys.");
            }
            catch (CloudSaveException ex) { if (IsUnauthorized(ex)) { await Reauthenticate(); throw; } throw; }
        });
    }

    static async Task<string> LoadPlayerJsonAsync(string key, bool allowReauthRetry = true, bool logOnMissing = true)
    {
        ValidateKeyOrThrow(key);
        try
        {
            var res = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            if (!res.TryGetValue(key, out var item)) { if (logOnMissing) Debug.Log($"[CloudSave] Load: '{key}' missing."); return null; }
            var json = item.Value.GetAs<string>();
            Debug.Log($"[CloudSave] Loaded '{key}' ({json?.Length ?? 0} bytes).");
            return json;
        }
        catch (CloudSaveException ex)
        {
            if (allowReauthRetry && IsUnauthorized(ex)) { await Reauthenticate(); return await LoadPlayerJsonAsync(key, false, logOnMissing); }
            Debug.LogError($"[CloudSave] Load '{key}' failed: {ex}"); throw;
        }
    }

    static async Task Retry(Func<Task> f, int maxAttempts = MAX_RETRIES)
    {
        int a = 0; Exception last = null;
        while (a < maxAttempts)
        {
            try { await f(); return; }
            catch (Exception e)
            {
                last = e; a++; int ms = Math.Min(2000, 200 * a * a);
                Debug.LogWarning($"[CloudSave] Attempt {a} failed: {e.Message}. Retry in {ms}ms"); await Task.Delay(ms);
            }
        }
        Debug.LogError("[CloudSave] Exhausted retries."); throw last ?? new Exception("Unknown Cloud Save failure");
    }

    static bool IsUnauthorized(CloudSaveException ex)
        => ex?.Message?.IndexOf("Access token", StringComparison.OrdinalIgnoreCase) >= 0
        || ex?.Message?.IndexOf("Unauthorized", StringComparison.OrdinalIgnoreCase) >= 0;

    static async Task Reauthenticate()
    {
        Debug.LogWarning("[Auth] Token invalid. Reauthenticating fixed user…");
        try { AuthenticationService.Instance.SignOut(true); } catch { }
        await SignInFixedUserAsync();
        Debug.Log($"[Auth] Re-signed in. PlayerId={AuthenticationService.Instance.PlayerId}");
    }

    // -------- Plain-text user registry (dev/alpha) --------
    const string USER_PLAIN_KEY = "__user_registry_plain_v1__";

    [Serializable] class PlainUserRegistry { public List<PlainUserRec> users = new(); }
    [Serializable] class PlainUserRec { public string username; public string password; public string updatedUtc; }

    static string CanonUser(string u) => (u ?? "").Trim().ToLowerInvariant();

    public static async Task<bool> CreateUserPlainAsync(string u, string p, bool overwrite = false)
    {
        u = CanonUser(u); if (string.IsNullOrWhiteSpace(u)) { Debug.LogWarning("[UsersPlain] Empty username"); return false; }
        p ??= "";
        var reg = await LoadUserRegistryPlainAsync();
        int idx = reg.users.FindIndex(x => x.username == u);
        var now = DateTime.UtcNow.ToString("O");
        if (idx >= 0 && !overwrite) { Debug.LogWarning($"[UsersPlain] Create: '{u}' exists, overwrite=false."); return false; }
        if (idx >= 0) { reg.users[idx].password = p; reg.users[idx].updatedUtc = now; Debug.Log($"[UsersPlain] Updated '{u}'."); }
        else { reg.users.Add(new() { username = u, password = p, updatedUtc = now }); Debug.Log($"[UsersPlain] Created '{u}'."); }
        return await SaveUserRegistryPlainAsync(reg);
    }

    public static async Task<bool> VerifyUserPlainAsync(string u, string p)
    {
        u = CanonUser(u);
        var reg = await LoadUserRegistryPlainAsync();
        var rec = reg.users.Find(x => x.username == u);
        bool ok = rec != null && rec.password == (p ?? "");
        Debug.Log($"[UsersPlain] Verify '{u}': {(ok ? "OK" : "FAIL")}"); return ok;
    }

    public static async Task<bool> ChangePasswordPlainAsync(string u, string np)
    {
        u = CanonUser(u);
        var reg = await LoadUserRegistryPlainAsync();
        var rec = reg.users.Find(x => x.username == u);
        if (rec == null) { Debug.LogWarning($"[UsersPlain] ChangePassword: '{u}' not found."); return false; }
        rec.password = np ?? ""; rec.updatedUtc = DateTime.UtcNow.ToString("O");
        Debug.Log($"[UsersPlain] Changed password for '{u}'.");
        return await SaveUserRegistryPlainAsync(reg);
    }

    public static async Task<bool> DeleteUserPlainAsync(string u)
    {
        u = CanonUser(u);
        var reg = await LoadUserRegistryPlainAsync();
        int removed = reg.users.RemoveAll(x => x.username == u);
        Debug.Log($"[UsersPlain] Delete '{u}': removed={removed}");
        return removed > 0 && await SaveUserRegistryPlainAsync(reg);
    }

    public static async Task<string[]> ListUsersPlainAsync()
    {
        var reg = await LoadUserRegistryPlainAsync();
        var names = reg.users.Select(x => x.username).OrderBy(x => x).ToArray();
        Debug.Log($"[UsersPlain] List: {names.Length} users");
        return names;
    }

    public static Task<bool> ClearAllUsersPlainAsync()
        => SaveUserRegistryPlainAsync(new PlainUserRegistry());

    static async Task<PlainUserRegistry> LoadUserRegistryPlainAsync()
    {
        var json = await LoadPlayerJsonAsync(USER_PLAIN_KEY);
        if (string.IsNullOrEmpty(json)) { Debug.Log("[UsersPlain] Load: empty registry"); return new(); }
        try { var reg = JsonUtility.FromJson<PlainUserRegistry>(json) ?? new(); Debug.Log($"[UsersPlain] Load: {reg.users.Count} users"); return reg; }
        catch (Exception e) { Debug.LogError("[UsersPlain] Load: corrupt JSON, resetting. " + e); return new(); }
    }

    static async Task<bool> SaveUserRegistryPlainAsync(PlainUserRegistry reg)
    {
        try
        {
            string json = JsonUtility.ToJson(reg ?? new());
            await SaveManyAsync(new() { [USER_PLAIN_KEY] = json });
            Debug.Log($"[UsersPlain] Save: {reg?.users?.Count ?? 0} users, {json.Length} bytes");
            return true;
        }
        catch (Exception e) { Debug.LogError("[UsersPlain] Save failed: " + e); return false; }
    }

    // -------- Diagnostics --------
    public static async Task<bool> SmokeTestAsync()
    {
        try
        {
            await EnsureReadyAsync();
            string key = "__ping__", stamp = DateTime.UtcNow.ToString("O");
            await SaveManyAsync(new() { [key] = stamp });
            var back = await LoadPlayerJsonAsync(key);
            Debug.Log($"[CloudSave][Ping] wrote={stamp} read={back}");
            return back == stamp;
        }
        catch (Exception e) { Debug.LogError("[CloudSave][Ping] failed: " + e); return false; }
    }
}
