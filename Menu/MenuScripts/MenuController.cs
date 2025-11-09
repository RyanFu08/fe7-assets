using System.Collections;
using UnityEngine;

public class MenuController : MonoBehaviour {

	[SerializeField] private string menu_id;  	
	[SerializeField] private GameObject[] buttons;
	public int cs = 0;
	
    void Start() {
    	for (int i=0; i<buttons.Length; i++) {
    		buttons[cs].GetComponent<button>().state = "unselected";
    	}
    	buttons[cs].GetComponent<button>().state = "selected";
    }

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			buttons[cs].GetComponent<button>().state = "unselected";
			cs = (cs + 1) % buttons.Length;
			buttons[cs].GetComponent<button>().state = "selected";
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			buttons[cs].GetComponent<button>().state = "unselected";
			cs = (cs - 1 + buttons.Length) % buttons.Length;
			buttons[cs].GetComponent<button>().state = "selected";
		}
		if (Input.GetKeyDown(KeyCode.X))
		{
			buttons[cs].GetComponent<button>().go();
		}
		if (Input.GetKeyDown(KeyCode.Z))
		{
			if (menu_id == "account2" || menu_id == "account1") {
				TransitionService.LoadScene("StartMenu");
			}
		}
	}
}
