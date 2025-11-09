using System.Collections;
using System.Threading.Tasks;   // ← required
using UnityEngine;

public class button : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] private Sprite unselected;
    private Sprite[] selected = new Sprite[34];
    public string state = "unselected";
    private string prev_state = "none";
    private int cf = 0;
    [SerializeField] private float timer = 0f;
    private float threshold = 0.035f;
    [SerializeField] private GameObject w1, w2;
    [SerializeField] private string button_id;
    [SerializeField] private GameObject helper1, helper2, helper3;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        for (int i = 1; i <= 34; i++)
        {
            selected[i - 1] = Resources.Load<Sprite>("buttons/1/hh" + i);
        }
        if (state == "unselected")
        {
            w1.GetComponent<WingScript>().set_inactive();
            w2.GetComponent<WingScript>().set_inactive();
        }
        else
        {
            w1.GetComponent<WingScript>().set_active();
            w2.GetComponent<WingScript>().set_active();
        }
    }

    void Update()
    {
        if (prev_state != state)
        {
            if (state == "unselected")
            {
                sr.sprite = unselected;
                w1.GetComponent<WingScript>().set_inactive();
                w2.GetComponent<WingScript>().set_inactive();
            }
            else if (state == "selected")
            {
                w1.GetComponent<WingScript>().set_active();
                w2.GetComponent<WingScript>().set_active();
                cf = 0;
                timer = 0f;
                sr.sprite = selected[cf];
            }
        }
        if (state == "selected")
        {
            timer += Time.deltaTime;
            if (timer >= threshold)
            {
                timer -= threshold;
                cf = (cf + 1) % 34;
                sr.sprite = selected[cf];
            }
        }
        prev_state = state;
    }

    public void go()
    {
        if (button_id == "tutorial")
        {
            TransitionService.LoadScene("Tutorial");
        }
        else if (button_id == "play")
        {
            if (UserData.IsLoggedIn)
            {
                TransitionService.LoadScene("CreateJoinRoom");
            }
            else
            {
                helper1.GetComponent<infopanel>().dir = "down";
            }
        }
        else if (button_id == "account")
        {
            if (UserData.IsLoggedIn)
            {
                TransitionService.LoadScene("Account1");
            }
            else
            {
                TransitionService.LoadScene("Account1");
            }
        }
        else if (button_id == "credits")
        {
            TransitionService.LoadScene("Credits");
        }
        else if (button_id == "signin")
        {
            TransitionService.LoadScene("SignInAccount");
        }
        else if (button_id == "signup")
        {
            TransitionService.LoadScene("CreateAccount");
        }
        else if (button_id == "finishcreateaccount") // SIGN UP
        {
            string username = helper1.GetComponent<InputControl>().s;
            string password = helper2.GetComponent<InputControl>().s;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                helper3.GetComponent<infopanel>().dir = "down";
                return;
            }
            StartCoroutine(SignUpFlow(username, password, overwrite: false));
        }
        else if (button_id == "finishsignin") // SIGN IN (analogous flow)
        {
            string username = helper1.GetComponent<InputControl>().s;
            string password = helper2.GetComponent<InputControl>().s;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                helper3.GetComponent<infopanel>().dir = "down";
                return;
            }
            StartCoroutine(SignInFlow(username, password));
        }
        else if (button_id == "joinroomcode")
        {
            string roomcode = helper1.GetComponent<InputControl>().s;
            _ = join_room(roomcode);
        }
        else if (button_id == "createjoin")
        {
            TransitionService.LoadScene("CreateJoinRoom");
        }
    }

    async Task join_room(string roomcode)
    {
        await CloudPersistence.SetRoomAsync(roomcode);
        string p1name = await CloudPersistence.GetP1NameAsync();
        string p2name = await CloudPersistence.GetP2NameAsync();
        if (p1name == "NONE")
        {
            RoomData.whoami = 0;
            RoomData.roomcode = roomcode;
            p1name = UserData.current_username;
            await CloudPersistence.SetP1NameAsync(p1name);
            TransitionService.LoadScene("Room");
        }
        else if (p2name == "NONE")
        {
            RoomData.whoami = 1;
            RoomData.roomcode = roomcode;
            p2name = UserData.current_username;
            await CloudPersistence.SetP2NameAsync(p2name);
            TransitionService.LoadScene("Room");
        }
    }

    private IEnumerator SignUpFlow(string username, string password, bool overwrite)
    {
        Task<bool> t = UserData.TrySignUpAsync(username, password, overwrite);
        yield return new WaitUntil(() => t.IsCompleted);

        if (t.IsFaulted || !t.Result)
        {
            var panel = helper3.GetComponent<infopanel>();
            if (panel != null) panel.dir = "down";
            yield break;
        }

        // Signup succeeded; user is logged in.
        TransitionService.LoadScene("StartMenu");
    }

    // ===== Analogous sign-in flow =====
    private IEnumerator SignInFlow(string username, string password)
    {
        Task<bool> t = UserData.TryLoginAsync(username, password);
        yield return new WaitUntil(() => t.IsCompleted);

        if (t.IsFaulted || !t.Result)
        {
            var panel = helper3.GetComponent<infopanel>();
            if (panel != null) panel.dir = "down";
            yield break;
        }

        // Sign-in succeeded; user is logged in.
        TransitionService.LoadScene("StartMenu");
    }
}
