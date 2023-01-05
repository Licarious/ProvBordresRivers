﻿using System.Drawing;
//State class stores provIDlsit, name, arabelResoures, cappedResoures, and discoverableResources
public class State
{
    public List<string> provIDList = new List<string>();
    public string name = "";
    public int stateID = 0;
    public List<string> traits = new List<string>();
    public string subsistanceBuilding = "";
    public int navalID = 0;
    public List<(string type,Color color)> hubs = new List<(string, Color)>();
    public List<Color> impassables = new List<Color>();
    public List<Color> primeLand = new List<Color>();

    public List<Prov> provList = new List<Prov>();
    public List<string> provNameList = new List<string>();

    public int arableLand = 0;
    public List<Color> provColors = new List<Color>();
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public List<(int,int)> coordList = new List<(int, int)>();
    public (int, int) center = (0, 0);
    public (int, int) maxRecSize = (0, 0);
    public bool floodFillMaxRec = false;

    public HashSet<(int, int)> coordSet = new HashSet<(int, int)>();

    //hash set matching color to prov
    //public HashSet<Color, Prov> provSet = new HashSet<Color, Prov>();

    //set of color and prov for each prov in state for quick lookup of prov by color
    public Dictionary<Color, Prov> provSet = new Dictionary<Color, Prov>();


    public State(string name) {
        this.name = name;
    }
    public State() { }

    //convert hexdicimal to color
    public void hexToColor() {
        for (int i = 0; i< provIDList.Count; i++) {
            Color c = ColorTranslator.FromHtml("#"+provIDList[i]);
            provColors.Add(c);
        }
    }

    //set HashSet
    public void setHashSet() {
        foreach (Prov p in provList) {
            foreach ((int, int) coord in p.coords) {
                coordSet.Add(coord);
            }
        }
    }

    public void setProvSet() {
        //clear provSet
        provSet.Clear();

        foreach (Prov p in provList) {
            provSet.Add(p.color, p);
        }
    }

    //tostring
    public override string ToString() {
        return name + ": " + provIDList.Count;
    }
}
