using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Managers;
using UnityEngine;

namespace ItemManager;

public partial class Item
{
    public void ToggleAllActiveRecipes(bool isEnabled)
    {
        if (activeRecipes.ContainsKey(this)) {
            foreach(var kv in activeRecipes[this])
            {
                foreach(var item in kv.Value)
                {
                    item.m_enabled = isEnabled;
                }
            }
        }
    }

    public enum RecipesEnabled
    {
        False,
        True,
        Mixed
    }

    public RecipesEnabled GetActiveRecipesEnabled()
    {
        bool allTrue = true;
        bool allFalse = false;
        if (activeRecipes.ContainsKey(this))
        {
            foreach (var kv in activeRecipes[this])
            {
                foreach (var item in kv.Value)
                {
                    allTrue &= item.m_enabled;
                    allFalse |= item.m_enabled;
                }
            }
        }
        if (allTrue && allFalse)
            return RecipesEnabled.True;
        if (!allTrue && !allFalse)
            return RecipesEnabled.False;
        return RecipesEnabled.Mixed;
    }

}
