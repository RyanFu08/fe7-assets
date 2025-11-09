using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class ActionList {
    public int current_turn = 0;
    public List<GameAction> actions = new List<GameAction>();
    public void add_action(GameAction g) {
    	actions.Add(g);
    }
}

[Serializable]
public class GameAction {

	/** more general commands, procsesed in order **/

	public bool switch_turn; //disregard ALL OTHER THINGS if this is the case
	public string special_command = "none"; //for anything that isnt move/attack/item

	/** IF move **/
	
	public int unit_ind;
	public List<int> move_dir = new List<int>(); //0 = left, 1 = right, 2 = up, 3 = down
	
	/** attack vars **/
	public bool attack;
	
	//load 1 vars
	public int hp_blue, hp_red, dmg_blue, dmg_red, hit_blue, hit_red, crit_blue, crit_red;
	public string doub;
	public int blue_ind, red_ind;
	//load 2 vars
	public int hit_left, dmg_left, crt_left, hp_left, mhp_left, hit_right, dmg_right, crt_right, hp_right, mhp_right;

	/** item vars**/
		//PROCESS THIS FIRST
	public int item_ind;


}
