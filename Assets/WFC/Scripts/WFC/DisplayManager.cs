using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimpleGridWFC;

public class DisplayManager : MonoBehaviour
{
    private List<Zone> listZones;
    private int[,] grid;
    private Vector2 coordinates = new Vector2(0, 0);

    private bool changed = false;
    private bool display = true;

    public void setWFC(SimpleGridWFC wfc)
    {
        changed = true;
    }

    private void setZones(List<Zone> zones)
    {
        listZones = zones;
    }

    private void setGrid(int[,] grid)
    {
        changed = true;
        this.grid = grid;
    }

    private void setGrid(Module[,] grid)
    {
        
    }

    private void setGrid(Slot[,] grid)
    {
        
    }

    public void showWFC()
    {
        display = true;
    }

    public void hideWFC()
    {
        display = false;
    }

    private void Update()
    {
        if(display && changed)
        {
            changed = false;
        }
    }
}
