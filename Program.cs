using System.Diagnostics;
using System.Drawing;
using System.Linq;


//which game are you using this for? 
string game = "Vic3"; //    "Vic3" for Victoria 3, "CK3" for Crusader Kings 3 and Imperator: Rome,
//only set up for Vic3

string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

//stopwatch
Stopwatch stopwatch = Stopwatch.StartNew();

// open up image file rivers
Bitmap riverBmp = new Bitmap(localDir + @"\_Input\rivers.png");
List<Color> NotRiverColors = new() {
    Color.FromArgb(255, 255, 255),  //land
    Color.FromArgb(122, 122, 122),  //water grey
    Color.FromArgb(255, 0, 128)     //water pink
};

List<(int x, int y)> riverCoordList = new List<(int x, int y)>();

List<Color> seaColorList = new List<Color>();

//2d array of colors
Color[,] colorArray = new Color[riverBmp.Width, riverBmp.Height];

//get all river coordinates and store in list riverCoordList 
for (int y = 0; y < riverBmp.Height; y++) {
    for (int x = 0; x < riverBmp.Width; x++) {
        //if riverBmp.GetPixel(x, y) is in NotRiverColors, then it is not a river and we can skip it
        if (NotRiverColors.Contains(item: riverBmp.GetPixel(x, y))) {
            continue;
        }
        else {
            riverCoordList.Add((x, y));
        }
    }
}

List<State> stateList = new List<State>();

//method to parse state files
void parseStateFilesVic3(List<State> stateList) {
    //read all files in localDir/_Input/state_regions
    string[] files = Directory.GetFiles(localDir + "/_Input/state_regions");
    //for each file
    int count = 0;

    foreach (string file in files) {
        if (file.EndsWith(".txt")) {
            //read file
            string[] lines = File.ReadAllLines(file);
            //for each line
            //Console.WriteLine(file);
            State s = new State();
            bool traitsfound = false;
            foreach (string l1 in lines) {
                string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                //get STATE_NAME
                if (line.StartsWith("STATE_")) {
                    //Console.WriteLine("\t"+line.Split()[0]);
                    s = new State(line.Split()[0]);

                    //incase people are orverriding states in latter files
                    //check if state with same name already exists in stateList and if so, delete it
                    foreach (State state in stateList) {
                        if (state.name == s.name) {
                            stateList.Remove(state);
                            break;
                        }
                    }

                    stateList.Add(s);
                }
                //get stateID
                if (line.StartsWith("id")) {
                    s.stateID = int.Parse(line.Split()[2]);
                }
                if (line.StartsWith("subsistence_building")) {
                    s.subsistanceBuilding = line.Split("=")[1].Replace("\"", "").Trim();
                }

                //get provinces
                if (line.TrimStart().StartsWith("provinces")) {
                    string[] l2 = line.Split("=")[1].Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                            string n = l2[i].Replace("\"", "").Replace("x", "");
                            s.provIDList.Add(n);
                            s.provList.Add(new Prov(ColorTranslator.FromHtml("#" + n)));
                        }
                    }
                }
                //get impassable colors
                if (line.TrimStart().StartsWith("impassable")) {
                    string[] l2 = line.Split("=")[1].Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                            string n = l2[i].Replace("\"", "").Replace("x", "");
                            Color c = ColorTranslator.FromHtml("#" + n);
                            s.impassables.Add(c);

                            //set isWastland for that prov color to ture
                            foreach (Prov p in s.provList) {
                                if (p.color == c) {
                                    p.isWasteland = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                //get prime_land colors
                if (line.TrimStart().StartsWith("prime_land")) {
                    string[] l2 = line.Split("=")[1].Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                            string n = l2[i].Replace("\"", "").Replace("x", "");
                            Color c = ColorTranslator.FromHtml("#" + n);
                            s.primeLand.Add(c);
                        }
                    }
                }

                //get traits
                if (line.Trim().StartsWith("traits")) {
                    traitsfound = true;
                }
                if (traitsfound) {
                    string[] l2 = line.Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("\"")) {
                            s.traits.Add(l2[i].Replace("\"", ""));
                        }
                    }
                }

                //get arable_land
                if (line.TrimStart().StartsWith("arable_land")) {
                    s.arableLand = int.Parse(line.Split("=")[1].Trim());
                    count++;
                }
                //get naval id
                if (line.TrimStart().StartsWith("naval_exit_id")) {
                    string[] l2 = line.Split("=");
                    s.navalID = int.Parse(l2[1].Trim());
                }

                //get city color
                if (line.TrimStart().StartsWith("city") || line.TrimStart().StartsWith("port") || line.TrimStart().StartsWith("farm") || line.TrimStart().StartsWith("mine") || line.TrimStart().StartsWith("wood")) {
                    string[] l2 = line.Split("=");
                    s.hubs.Add((l2[0].Trim(), ColorTranslator.FromHtml("#" + l2[1].Replace("\"", "").Replace("x", "").Trim())));
                    s.color = s.hubs[0].color;
                }
                //reset cappedResourseFound and discoverableResourseFound
                if (line.Trim().StartsWith("}")) {
                    traitsfound = false;
                }

            }
        }
    }

    foreach (State s in stateList) {
        //set provSet
        s.setProvSet();
    }

}

void parseProvincesVic3(List<State> stateList, Color[,] colorArray) {
    //open _Input/Vic3/provinces.png
    Bitmap bmp = new Bitmap(localDir + "/_Input/provinces.png");

    Console.WriteLine("Compressing Vic3 Prov Map");
    //compress image into 2D colorList and 2D lengthList
    List<List<Color>> colorList = new List<List<Color>>();
    List<List<int>> lengthList = new List<List<int>>();
    for (int i = 0; i < bmp.Width; i++) {
        if ((i+1) % 2000 == 0) {
            Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
        }

        colorList.Add(new List<Color>());
        lengthList.Add(new List<int>());

        colorList[i].Add(bmp.GetPixel(i, 0));
        int tmpLength = 0;
        int tx = 0;

        for (int j = 0; j < bmp.Height; j++) {
            //check if pixel is the same as current last one in colorList
            if (bmp.GetPixel(i, j) == colorList[i][colorList[i].Count - 1]) {
                tmpLength++;
            }
            else {
                colorList[i].Add(bmp.GetPixel(i, j));
                lengthList[i].Add(tmpLength);
                tx += tmpLength;
                tmpLength = 1;
            }

            //add color to colorArray
            colorArray[i, j] = bmp.GetPixel(i, j);
        }
        lengthList[i].Add(tmpLength);
    }

    //match provList color to colorList color and add the coords to prov coords
    Console.WriteLine("Matching Vic3 Prov Map");
    for (int i = 0; i < colorList.Count; i++) {
        if ((i+1) % 2000 == 0) {
            Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
        }

        int tx = 0;
        for (int j = 0; j < colorList[i].Count; j++) {
            tx += lengthList[i][j];
            if (tx >= bmp.Height) {
                break;
            }
            //if alpha is 0, skip
            if (colorList[i][j].A == 0) {
                continue;
            }

            foreach (State s in stateList) {
                //find if color is in s.provSet and add coords to prov.coords
                if (s.provSet.TryGetValue(colorList[i][j], out Prov p)) {
                    for (int k = 0; k < lengthList[i][j]; k++) {
                        p.coords.Add((i, tx - k - 1));
                    }
                    break;
                }
            }

            stateCheckExit:
            int ignoreMe = 0;
        }
    }
}

void parseDefaultMap(List<Color> seaColorList) {
    //open _Input/Vic3/default.map
    string[] lines = File.ReadAllLines(localDir + "/_Input/default.map");

    bool seaFound = false;
    foreach (string line in lines) {
        string l1 = line.Replace("{", " { ").Replace("}", " } ").Replace("=", " = ").Replace("#", " # ").Trim();
        if (l1.StartsWith("sea_start") || l1.StartsWith("lakes")) {
            seaFound = true;
        }
        if (seaFound) {
            string[] l2 = l1.Split(' ');
            for (int i = 0; i < l2.Length; i++) {
                if (l2[i].StartsWith("#")) {
                    break;
                }
                else if (l2[i].StartsWith("x")) {
                    seaColorList.Add(ColorTranslator.FromHtml("#" + l2[i].Replace("x", "")));
                }
                else if (l2[i].StartsWith("}")) {
                    seaFound = false;
                    break;
                }
            }

        }
    }
}

parseStateFilesVic3(stateList);
parseProvincesVic3(stateList, colorArray);
parseDefaultMap(seaColorList);

//set riverCoordSet to riverCoordList
HashSet<(int, int)> riverCoordSet = new HashSet<(int, int)>();
foreach ((int, int) c in riverCoordList) {
    riverCoordSet.Add(c);
}


//print every prov in stateList whos coordlist contains a riverCoord
List<Prov> riverSplittingProvList = new List<Prov>(); 
foreach(State s in stateList) {
    foreach (Prov p in s.provList) {
        p.setHashSet();
        //if p is not in riverSplittingProvList
        if (!riverSplittingProvList.Contains(p)) {
            //if p contains a riverCoord
            if (p.coordSet.Intersect(riverCoordSet).Count() > 0) {
                //add p to riverSplittingProvList
                riverSplittingProvList.Add(p);
            }
        }

    }

}

Console.WriteLine("River Containing Provs: " + riverSplittingProvList.Count);

//draw all riverSplittingProvList to a new image and save it
void debugDrawRiverSplitingProvs(List<Prov> riverSplittingProvList) {
    //check if _Output/ exists, if not, create it
    if (!Directory.Exists(localDir + "/_Output/")) {
        Directory.CreateDirectory(localDir + "/_Output/");
    }

    //get size of province png file and create new bitmap with that size and graphics object 
    Bitmap bmp1 = new Bitmap(localDir + "/_Input/provinces.png");

    Bitmap bmp = new Bitmap(bmp1.Width, bmp1.Height);
    foreach (Prov p in riverSplittingProvList) {
        foreach ((int, int) c in p.coords) {
            //skip if c is in riverCoordSet
            if (riverCoordSet.Contains(c)) {
                continue;
            }
            bmp.SetPixel(c.Item1, c.Item2, p.color);
            p.coordsRiverless.Add(c);
        }

    }
    bmp.Save(localDir + "/_Output/00_along_river.png");


    Bitmap bmp2 = new Bitmap(bmp1.Width, bmp1.Height);
    Bitmap bmp3 = new Bitmap(bmp1.Width, bmp1.Height);
    int count = 0;
    //finds the largest contiguous area of a prov and set it to coordsLargetstContig
    foreach (Prov p in riverSplittingProvList) {
        p.getContiguousArea();

        //if coordsLargestContig == coordsRiverless then the prov is not split by a river and can be skiped
        if (p.coordsLargestContig.Count == p.coordsRiverless.Count) {
            continue;
        }

        //Console.WriteLine(p.getHexColor() + ": " + p.coordsLargestContig.Count);

        foreach ((int,int) c in p.coordsLargestContig) {
            bmp2.SetPixel(c.Item1, c.Item2, p.color);
        }
        foreach ((int, int) c in p.coordsRiverless)
            bmp3.SetPixel(c.Item1, c.Item2, p.color);
        p.isContiguous = false;
        count++;
        
    }
    bmp3.Save(localDir + "/_Output/01_crossing.png");
    bmp2.Save(localDir + "/_Output/02_one_sided.png");

    Console.WriteLine("River Splitting Prov: "+ count);
    Console.WriteLine(stopwatch.Elapsed + "s");
}

void remvoeUnusedSates(List<State> stateList) {
    List<State> stateRemoveList = new List<State>();
    foreach (State s in stateList) {
        List<Prov> provRemoveList = new List<Prov>();
        foreach (Prov p in s.provList) {
            if (p.isContiguous) {
                provRemoveList.Add(p);
            }
        }
        //remove all elements from provRemoveList from s.provList
        foreach (Prov p in provRemoveList) {
            s.provList.Remove(p);
        }

        //if s has no provs remove it from stateList
        if (s.provList.Count == 0) {
            stateRemoveList.Add(s);
            
        }

    }
    //remove all elements from stateRemoveList from stateList
    foreach (State s in stateRemoveList) {
        stateList.Remove(s);
    }

    Console.WriteLine(stateList.Count);
}

debugDrawRiverSplitingProvs(riverSplittingProvList);
remvoeUnusedSates(stateList);
updateMap5(colorArray, stateList, seaColorList, riverCoordList);


void updateMap2(Color[,] colorArray, List<State> stateList, List<Color> seaColorList, List<(int x, int y)> riverCoordList) {
    //update map

    HashSet<(int, int)> riverCoordSet = riverCoordList.ToHashSet();

    Console.WriteLine("Updating Map");
    int n = 1000;
    for (int c2 = 0; c2 < n; c2++) {
        //print progress every 10% of n as a presentage
        
        if ((c2+1) % (n / 20) == 0) {
            Console.WriteLine("\t" + ((float)c2 / (n / 100)) + "%\t\t" + stopwatch.Elapsed+"s");
        }


        foreach (State s in stateList) {
            //Console.WriteLine(s.name);
            foreach (Prov p in s.provList) {
                if (p.otherCoords.Count>0) {
                    //randomize order of p.otherCoords
                    p.otherCoords = p.otherCoords.OrderBy(x => Guid.NewGuid()).ToList();

                    List<(int, int)> removeList = new List<(int, int)>();

                    //parralle for each (int, int) c in p.otherCoords
                    //replace colorArray[c] with a nearby color that is not p.color
                    //if colorArray[c] is not p.color, skip

                    Parallel.ForEach(p.otherCoords, (c) => {
                        //replace colorArray[c] with a nearby color that is not p.color
                        //if colorArray[c] is not p.color, skip
                        if (colorArray[c.Item1, c.Item2] == p.color) {


                            //find the colors and number of pixels of each color in a 3x3 grid around c
                            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
                            for (int i = -1; i < 2; i++) {
                                for (int j = -1; j < 2; j++) {
                                    if (i == 0 && j == 0) {
                                        continue;
                                    }
                                    if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                                        continue;
                                    }

                                    //if coord is in riverCoordList, skip
                                    if (riverCoordSet.Contains((c.Item1 + i, c.Item2 + j))) {
                                        //Console.WriteLine("\t" +p.color + ":\t" + c);
                                        continue;
                                    }

                                    //if color in seaColorList continue
                                    if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j])) {
                                        continue;
                                    }
                                    if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                                        colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                                    }
                                    else {
                                        // if color is not in colorCount try to add it
                                        try {
                                            colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                                        }
                                        catch (Exception e) {
                                            colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                                            Console.WriteLine(e);
                                        }
                                        
                                    }
                                }
                            }

                            //if there is atleast 3 pixles of p.color in colorCount, continue
                            if (colorCount.ContainsKey(p.color) && colorCount[p.color] > 3) {
                                
                            }

                            //set colorArray[c] to the most common color
                            //if colorArray has any elements
                            else if (colorCount.Count > 0) {
                                colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                                removeList.Add(c);
                            }
                        }
                    });

                    /*
                    //using p.otherCoords to update colorArray
                    foreach ((int, int) c in p.otherCoords) {
                        //replace colorArray[c] with a nearby color that is not p.color
                        //if colorArray[c] is not p.color, skip
                        if (colorArray[c.Item1, c.Item2] != p.color) {
                            continue;
                        }

                        //find the colors and number of pixels of each color in a 3x3 grid around c
                        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
                        for (int i = -1; i < 2; i++) {
                            for (int j = -1; j < 2; j++) {
                                if (i == 0 && j == 0) {
                                    continue;
                                }
                                if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                                    continue;
                                }
                                
                                //if coord is in riverCoordList, skip
                                if (riverCoordList.Contains((c.Item1 + i, c.Item2 + j))) {
                                    //Console.WriteLine("\t" +p.color + ":\t" + c);
                                    continue;
                                }
                                
                                //if color in seaColorList continue
                                if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j])) {
                                    continue;
                                }
                                if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                                    colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                                }
                                else {
                                    colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                                }
                            }
                        }
                         
                        

                        //if there is atleast 3 pixles of p.color in colorCount, continue
                        if (colorCount.ContainsKey(p.color) && colorCount[p.color] > 3) {
                            continue;
                        }

                        //set colorArray[c] to the most common color
                        //if colorArray has any elements
                        if (colorCount.Count > 0) {
                            colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                            removeList.Add(c);
                        }
                        

                    }
                    */

                    //remove in removeList from p.otherCoords
                    foreach ((int, int) c in removeList) {
                        p.otherCoords.Remove(c);
                    }


                }

            }
        }
    }

    
    foreach (State s in stateList) {
        foreach(Prov p in s.provList) {
            //for each coord in riverCoords get the color
            //ckech the color of the 8 surrounding pixels
            //if less than 3 are the same color, change the color of the coord to the most common color

            foreach ((int, int) c in p.riverCoords) {
                //find the colors and number of pixels of each color in a 3x3 grid around c
                Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
                for (int i = -1; i < 2; i++) {
                    for (int j = -1; j < 2; j++) {
                        if (i == 0 && j == 0) {
                            continue;
                        }
                        if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                            continue;
                        }
                        //if color in seaColorList continue
                        if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j])) {
                            continue;
                        }

                        if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                            colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                        }
                        else {
                            colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                        }
                    }
                }

                //if there is atleast 3 pixles of p.color in colorCount, continue
                if (colorCount.ContainsKey(p.color) && colorCount[p.color] > 2) {
                    continue;
                }

                //set colorArray[c] to the most common color
                colorArray[c.Item1, c.Item2] = colorCount.First().Key;
            }


        }

    }

    //remove any fully surround pixles
    foreach((int x,int y) c in riverCoordSet) {
        //check the 8 surronding colors
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
        Color currentColor = colorArray[c.Item1, c.Item2];
        for (int i = -1; i < 2; i++) {
            for (int j = -1; j < 2; j++) {
                if (i == 0 && j == 0) {
                    continue;
                }
                if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                    continue;
                }
                if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                    colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                }
                else {
                    colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                }
            }
        }

        //if current color is not in colorCount or has less than 3, replace with most common color that is not in seaColorList
        if (!colorCount.ContainsKey(currentColor) || colorCount[currentColor] < 3) {
            //randomize colorCount order
            colorCount = colorCount.OrderBy(x => Guid.NewGuid()).ToDictionary(x => x.Key, x => x.Value);

            //if the number of pixels of a color is greater than 3 and the color is not p.color or seacolorlist, replace colorArray[c] with that color
            foreach (KeyValuePair<Color, int> kvp in colorCount) {
                if (kvp.Value > 3 && !seaColorList.Contains(kvp.Key)) {
                    colorArray[c.Item1, c.Item2] = kvp.Key;
                    break;
                }
            }
        }


    }



    //save updated map
    Console.WriteLine("Saving Updated Map");
    Bitmap bmp = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
    for (int i = 0; i < colorArray.GetLength(0); i++) {
        for (int j = 0; j < colorArray.GetLength(1); j++) {
            bmp.SetPixel(i, j, colorArray[i, j]);
        }
    }
    bmp.Save(localDir + "/_Output/03_updated_map.png");
}

void updateMap3(Color[,] colorArray, List<State> stateList, List<Color> seaColorList, List<(int x, int y)> riverCoordList) {
    HashSet<(int, int)> riverCoordSet = riverCoordList.ToHashSet();

    Console.WriteLine("Updating Map");
    int n = 10000;
    for (int c2 = 0; c2 < n; c2++) {
        //print progress every 10%
        if ((c2+1) % (n / 20) == 0) {
            Console.WriteLine("\t" + ((float)c2 / n) * 100 + "%\t"+stopwatch.Elapsed);
        }

        //for each state
        foreach(State s in stateList) {
            //Console.WriteLine(s.name);
            List<Prov> removeProvList = new List<Prov>();
            foreach(Prov p in s.provList) {
                //randomize p.otherCoords order
                p.otherCoords = p.otherCoords.OrderBy(x => Guid.NewGuid()).ToList();
                List<(int, int)> removeList = new List<(int, int)>();
                //for each coord in p.otherCoords
                foreach ((int, int) c in p.otherCoords) { 
                    Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
                    //find the colors and number of pixels of each color in a 3x3 grid around c
                    //skipping if the color is in seaColorList or is in riverCoordSet
                    for (int i = -1; i < 2; i++) {
                        for (int j = -1; j < 2; j++) {
                            if (i == 0 && j == 0) {
                                continue;
                            }
                            else if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                                continue;
                            }
                            else if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j]) || riverCoordSet.Contains((c.Item1 + i, c.Item2 + j))) {
                                continue;
                            }

                            if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                                colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                            }
                            else {
                                colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                            }
                        }
                    }

                    //if there is atleast 3 pixles of p.color in colorCount, continue
                    if (colorCount.ContainsKey(p.color) && colorCount[p.color] > 4) {
                        continue;
                    }

                    //if colorCount is not empty, set colorArray[c] to the most common color
                    if (colorCount.Count > 0) {
                        colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                        removeList.Add(c);
                    }
                }

                //remove any coords that were changed
                foreach ((int x, int y) c in removeList) {
                    p.otherCoords.Remove(c);
                }

                //if p.otherCoords is empty remove p
                if(p.otherCoords.Count == 0) {
                    removeProvList.Add(p);
                }
            }
            foreach(Prov p in removeProvList) {
                s.provList.Remove(p);
            }
        }
        
    }

    //save updated map
    Console.WriteLine("Saving Updated Map");
    Bitmap bmp = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
    for (int i = 0; i < colorArray.GetLength(0); i++) {
        for (int j = 0; j < colorArray.GetLength(1); j++) {
            bmp.SetPixel(i, j, colorArray[i, j]);
        }
    }
    bmp.Save(localDir + "/_Output/03_updated_map.png");

}

void updateMap4(Color[,] colorArray, List<State> stateList, List<Color> seaColorList, List<(int x, int y)> riverCoordList) {
    //river hash set
    HashSet<(int, int)> riverCoordSet = riverCoordList.ToHashSet();

    //hash set of all coords that are on the other side of a river
    HashSet<(int, int)> otherSideRiverCoordSet = new HashSet<(int, int)>();    
    foreach (State s in stateList) {
        foreach (Prov p in s.provList) {
            foreach ((int x, int y) c in p.otherCoords) {
                otherSideRiverCoordSet.Add(c);
            }
        }
        
    }
    List<(int, int)> otherSideRiverCoordList = otherSideRiverCoordSet.ToList();

    Console.WriteLine("Updating Map");
    int n = 1000;
    for (int count = 0; count < n; count++) {
        //print progress every 10% as long as count is not 0
        if (count % (n / 10) == 0 && count != 0) {
            //to 1 decimal places
            Console.WriteLine("\t" + Math.Round(((float)count / n) * 100, 0) + "%\t" + stopwatch.Elapsed);
        }


        //for each state
        foreach (State s in stateList) {

            foreach (Prov p in s.provList) {
                //randomize p.otherCoords order
                p.otherCoords = p.otherCoords.OrderBy(x => Guid.NewGuid()).ToList();

                //for each coord in p.otherCoords
                foreach ((int, int) c in p.otherCoords) {
                    Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
                    //find the colors and number of pixels of each color in a 3x3 grid around c
                    //skipping if the color is in seaColorList or is in riverCoordSet
                    for (int i = -1; i < 2; i++) {
                        for (int j = -1; j < 2; j++) {
                            if (i == 0 && j == 0) {
                                continue;
                            }
                            else if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                                continue;
                            }
                            else if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j]) || riverCoordSet.Contains((c.Item1 + i, c.Item2 + j))) {
                                continue;
                            }
                            else if (otherSideRiverCoordSet.Contains((c.Item1 + i, c.Item2 + j))) {
                                continue;
                            }

                            if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                                colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                            }
                            else {
                                colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                            }
                        }
                    }

                    //if there is atleast 2 pixles of p.color in colorCount, continue
                    if (colorCount.ContainsKey(p.color) && colorCount[p.color] > 2) {
                        continue;
                    }

                    //if colorCount is not empty, set colorArray[c] to the most common color
                    if (colorCount.Count > 0) {
                        //if the most common color has at least 3 pixels, set colorArray[c] to the most common color
                        if (colorCount.First().Value > 2) {
                            colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                            otherSideRiverCoordSet.Remove(c);
                        }

                    }
                }


            }
        }

    }


    for (int count = 0; count < 10; count++) {
        //update colors along river coords
        foreach ((int, int) c in riverCoordList) {

            //if pixel at c is in seaColorList, continue
            if (seaColorList.Contains(colorArray[c.Item1, c.Item2])) {
                continue;
            }


            //check 8 surounding colors
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            for (int i = -1; i < 2; i++) {
                for (int j = -1; j < 2; j++) {
                    if (i == 0 && j == 0) {
                        continue;
                    }
                    else if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                        continue;
                    }
                    else if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j])) {
                        continue;
                    }

                    if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                        colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                    }
                    else {
                        colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                    }
                }
            }


            //if colorCount is not empty, set colorArray[c] to the most common color
            if (colorCount.Count > 0) {
                //if the most common color has at least 3 pixels, set colorArray[c] to the most common color
                if (colorCount.First().Value > 2) {
                    colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                }

            }
        }
    }

    //save updated map
    Console.WriteLine("Saving Updated Map");
    Bitmap bmp = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
    for (int i = 0; i < colorArray.GetLength(0); i++) {
        for (int j = 0; j < colorArray.GetLength(1); j++) {
            bmp.SetPixel(i, j, colorArray[i, j]);
        }
    }
    bmp.Save(localDir + "/_Output/03_updated_map.png");

}

void updateMap5(Color[,] colorArray, List<State> stateList, List<Color> seaColorList, List<(int x, int y)> riverCoordList) {
    //river hash set
    HashSet<(int, int)> riverCoordSet = riverCoordList.ToHashSet();

    //hash set of all coords that are on the other side of a river
    HashSet<(int, int)> otherSideRiverCoordSet = new HashSet<(int, int)>();
    foreach (State s in stateList) {
        foreach (Prov p in s.provList) {
            foreach ((int x, int y) c in p.otherCoords) {
                otherSideRiverCoordSet.Add(c);
            }
        }

    }
    List<(int, int)> otherSideRiverCoordList = otherSideRiverCoordSet.ToList();

    Console.WriteLine("Updating Map");
    int n = 1000;
    List<(int, int)> removeList = new List<(int, int)>();
    for (int count = 0; count < n; count++) {
        //print progress every 10% as long as count is not 0
        if (count % (n / 20) == 0 && count != 0) {
            //to 1 decimal places
            Console.WriteLine("\t" + Math.Round(((float)count / n) * 100, 0) + "%\t" + stopwatch.Elapsed);
        }

        
        //randomize otherSideRiverCoordSet
        otherSideRiverCoordList = otherSideRiverCoordList.OrderBy(x => Guid.NewGuid()).ToList();
        int pCount = 0;
        //for each otherSideRiverCoordSet
        foreach ((int x, int y) c in otherSideRiverCoordList) {
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            //find the colors and number of pixels of each color in a 3x3 grid around c
            //skipping if the color is in seaColorList or is in riverCoordSet
            for (int i = -1; i < 2; i++) {
                for (int j = -1; j < 2; j++) {
                    if (i == 0 && j == 0) {
                        continue;
                    }
                    else if (c.x + i < 0 || c.x + i >= colorArray.GetLength(0) || c.y + j < 0 || c.y + j >= colorArray.GetLength(1)) {
                        continue;
                    }
                    else if (seaColorList.Contains(colorArray[c.x + i, c.y + j]) || riverCoordSet.Contains((c.x + i, c.y + j))) {
                        continue;
                    }
                    else if (otherSideRiverCoordSet.Contains((c.Item1 + i, c.Item2 + j))) {
                        continue;
                    }

                    if (colorCount.ContainsKey(colorArray[c.x + i, c.y + j])) {
                        colorCount[colorArray[c.x + i, c.y + j]]++;
                    }
                    else {
                        colorCount.Add(colorArray[c.x + i, c.y + j], 1);
                    }
                    pCount++;
                }
            }
            /*
            //if there is atleast 4 pixles of p.color in colorCount, continue
            if (colorCount.ContainsKey(colorArray[c.x, c.y]) && colorCount[colorArray[c.x, c.y]] > 3) {
                continue;
            }
            */
            //if colorCount is not empty, set colorArray[c] to the most common color
            if (colorCount.Count > 0) {
                //if most common color has less than half of the pixels
                if (colorCount.First().Value * 2 < pCount) {

                    int maxCount = colorCount.First().Value;
                    //if multiple colors have maxCount number of pixels, choose one at random and set colorArray[c] to that color
                    List<Color> maxColorList = new List<Color>();
                    foreach (KeyValuePair<Color, int> kvp in colorCount) {
                        if (kvp.Value == maxCount) {
                            maxColorList.Add(kvp.Key);
                        }
                    }
                    colorArray[c.x, c.y] = maxColorList.OrderBy(x => Guid.NewGuid()).First();
                    
                }
                else {
                    //use most common color 
                    colorArray[c.x, c.y] = colorCount.First().Key;
                }

                otherSideRiverCoordSet.Remove(c);
                removeList.Add(c);

            }
        }
        //remove all removeList from otherSideRiverCoordList
        foreach ((int x, int y) c in removeList) {
            otherSideRiverCoordList.Remove(c);
        }


    }


    for (int count = 0; count < 10; count++) {
        //update colors along river coords
        foreach ((int, int) c in riverCoordList) {

            //if pixel at c is in seaColorList, continue
            if (seaColorList.Contains(colorArray[c.Item1, c.Item2])) {
                continue;
            }


            //check 8 surounding colors
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            for (int i = -1; i < 2; i++) {
                for (int j = -1; j < 2; j++) {
                    if (i == 0 && j == 0) {
                        continue;
                    }
                    else if (c.Item1 + i < 0 || c.Item1 + i >= colorArray.GetLength(0) || c.Item2 + j < 0 || c.Item2 + j >= colorArray.GetLength(1)) {
                        continue;
                    }
                    else if (seaColorList.Contains(colorArray[c.Item1 + i, c.Item2 + j])) {
                        continue;
                    }

                    if (colorCount.ContainsKey(colorArray[c.Item1 + i, c.Item2 + j])) {
                        colorCount[colorArray[c.Item1 + i, c.Item2 + j]]++;
                    }
                    else {
                        colorCount.Add(colorArray[c.Item1 + i, c.Item2 + j], 1);
                    }
                }
            }


            //if colorCount is not empty, set colorArray[c] to the most common color
            if (colorCount.Count > 0) {
                //if the most common color has at least 3 pixels, set colorArray[c] to the most common color
                if (colorCount.First().Value > 2) {
                    colorArray[c.Item1, c.Item2] = colorCount.First().Key;
                }

            }
        }
    }

    //save updated map
    Console.WriteLine("Saving Updated Map");
    Bitmap bmp = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
    for (int i = 0; i < colorArray.GetLength(0); i++) {
        for (int j = 0; j < colorArray.GetLength(1); j++) {
            bmp.SetPixel(i, j, colorArray[i, j]);
        }
    }
    bmp.Save(localDir + "/_Output/03_updated_map.png");

}

compareResult();

void compareResult() {
    Console.WriteLine("Generating Comparison");

    int changedPx = 0;

    //create a new 04_compare.png file and compare the result to the original map
    Bitmap bmp1 = new Bitmap(localDir + "/_Input/provinces.png");
    Bitmap bmp2 = new Bitmap(localDir + "/_Output/03_updated_map.png");

    Bitmap bmp = new Bitmap(bmp1.Width, bmp1.Height);
    for (int i = 0; i < bmp1.Width; i++) {
        for (int j = 0; j < bmp1.Height; j++) {
            if (bmp1.GetPixel(i, j) == bmp2.GetPixel(i, j)) {
                //set pixel to alpha if the same color is found in both bmp1 and bmp2 
                bmp.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
            }
            else {
                //set pixelk to bmp2 color if different color is found in bmp1 and bmp2
                bmp.SetPixel(i, j, bmp2.GetPixel(i, j));
                changedPx++;
            }
        }
    }

    bmp.Save(localDir + "/_Output/04_compare.png");

    if (changedPx > 0) {
        Console.WriteLine("\n\n"+ changedPx+ " pixles have been changed this pass\n\n");
    }
    else { 
        Console.WriteLine("\n\nOutput map is the same as the input\n\n");
    }

}

Console.WriteLine(stopwatch.Elapsed + "s");
