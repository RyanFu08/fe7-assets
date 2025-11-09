using System;
using System.Collections.Generic;

[Serializable]
public class DraftAction
{
  public int unit_id;
  public int player_id;
}

[Serializable]
public class DraftActionList
{
  public int turn = 0;
  public List<DraftAction> actions = new List<DraftAction>(); // never null
}
