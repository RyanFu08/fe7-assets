using UnityEngine;

/// <summary>
/// Controls the attack‑results UI panel with a fast refresh cycle.
/// Robust against rapid command spam or lag spikes.
/// </summary>
public class AtkDisplay : MonoBehaviour
{
    // ── Animation targets ───────────────────────────────────────────────────────
    private const float SHOW_X =  85f;   // on‑screen position
    private const float HIDE_X = 155f;   // off‑screen position

    // ── Spring parameters ───────────────────────────────────────────────────────
    [SerializeField] private float springFrequency    = 4f;   // Hz
    [SerializeField] private float springDampingRatio = 0.6f;
    [SerializeField] private float refreshBoost       = 1.8f; // ≥1 boosts hide & show

    // Watch‑dog: maximum seconds we’ll stay in a single move
    [SerializeField] private float maxPhaseTime       = 0.6f;

    private float velocity   = 0f;   // current x‑velocity
    private float phaseTimer = 0f;   // time spent in current ToHidden / ToShown

    // ── UI references (must have StringControl.s) ───────────────────────────────
    [SerializeField] private GameObject blue_name, red_name, red_weap,
                                        blue_hp,  blue_mt,  blue_hit,  blue_crit,
                                        red_hp,   red_mt,   red_hit,   red_crit,
                                        stat;

    // ── Finite‑state machine ────────────────────────────────────────────────────
    private enum State {Hidden, Shown, ToHidden, ToShown}
    private State state = State.Hidden;

    // Refresh bookkeeping
    private bool pendingRefresh = false;   // reopen after hidden

    // ── Unity loop ──────────────────────────────────────────────────────────────
    private void Update() => Animate();

    // ── Public API (lower‑case names) ───────────────────────────────────────────
    public void show   () => BeginTransition(State.ToShown , false);
    public void hide   () => BeginTransition(State.ToHidden, false);
    public void refresh() => BeginTransition(State.ToHidden, true );

    // ── Stat setters (unchanged) ────────────────────────────────────────────────
    public void set_blue_name(string s) => blue_name.GetComponent<StringControl>().s = s;
    public void set_red_name (string s) => red_name .GetComponent<StringControl>().s = s;
    public void set_red_weap (string s) => red_weap .GetComponent<StringControl>().s = s;
    public void set_blue_hp  (string s) => blue_hp  .GetComponent<StringControl>().s = s;
    public void set_blue_mt  (string s) => blue_mt  .GetComponent<StringControl>().s = s;
    public void set_blue_hit (string s) => blue_hit .GetComponent<StringControl>().s = s;
    public void set_blue_crit(string s) => blue_crit.GetComponent<StringControl>().s = s;
    public void set_red_hp   (string s) => red_hp   .GetComponent<StringControl>().s = s;
    public void set_red_mt   (string s) => red_mt   .GetComponent<StringControl>().s = s;
    public void set_red_hit  (string s) => red_hit  .GetComponent<StringControl>().s = s;
    public void set_red_crit (string s) => red_crit .GetComponent<StringControl>().s = s;

    public void set_stats(
        string blueName, string blueHp,  string blueMt,  string blueHit,  string blueCrit,
        string redName,  string redWeap, string redHp,   string redMt,    string redHit,  string redCrit)
    {
        set_blue_name(blueName); set_blue_hp (blueHp);
        set_blue_mt  (blueMt);   set_blue_hit(blueHit); set_blue_crit(blueCrit);

        set_red_name (redName);  set_red_weap(redWeap); set_red_hp   (redHp);
        set_red_mt   (redMt);    set_red_hit (redHit);  set_red_crit (redCrit);
    }

    // ── Transition helpers ─────────────────────────────────────────────────────
    private void BeginTransition(State targetState, bool wantRefreshAfterHide)
    {
        // Already in desired steady state? do nothing
        if ((state == State.Shown  && targetState == State.ToShown ) ||
            (state == State.Hidden && targetState == State.ToHidden))
            return;

        pendingRefresh = wantRefreshAfterHide;
        state          = targetState;
        phaseTimer     = 0f; // reset watchdog
        // keep current velocity; new spring will steer it
    }

    private void SnapTo(float x, State newState)
    {
        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        velocity   = 0f;
        state      = newState;
        phaseTimer = 0f;
    }

    // ── Core animation routine ─────────────────────────────────────────────────
    private void Animate()
    {
        var rt = GetComponent<RectTransform>();
        float dt = Time.deltaTime;
        if (dt <= 0f) return;            // safety

        // Determine spring frequency (boost for refresh hide+show)
        bool moving   = state == State.ToHidden || state == State.ToShown;
        bool boosting = pendingRefresh && moving;
        float freq    = springFrequency * (boosting ? refreshBoost : 1f);

        float ω = freq * 2f * Mathf.PI;
        float k = ω * ω;
        float c = 2f * springDampingRatio * ω;

        // Target position
        float targetX = state switch
        {
            State.ToHidden => HIDE_X,
            State.ToShown  => SHOW_X,
            State.Hidden   => HIDE_X,
            _              => SHOW_X   // Shown
        };

        // Semi‑implicit Euler integration
        float x = rt.anchoredPosition.x - targetX;
        velocity += (-k * x - c * velocity) * dt;
        if (float.IsNaN(velocity) || float.IsInfinity(velocity)) velocity = 0f;

        rt.anchoredPosition += new Vector2(velocity * dt, 0f);

        // Settling / watchdog
        bool settled  = Mathf.Abs(velocity) < 0.01f &&
                        Mathf.Abs(rt.anchoredPosition.x - targetX) < 0.1f;
        if (moving) phaseTimer += dt;
        bool timedOut = moving && phaseTimer > maxPhaseTime;

        // State transitions
        if (state == State.ToHidden && (settled || timedOut))
        {
            SnapTo(HIDE_X, State.Hidden);
            if (pendingRefresh)
            {
                pendingRefresh = false;
                BeginTransition(State.ToShown, false); // reopen
            }
        }
        else if (state == State.ToShown && (settled || timedOut))
        {
            SnapTo(SHOW_X, State.Shown);
        }
    }
}
