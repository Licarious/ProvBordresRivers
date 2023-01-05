using System.Diagnostics;
using System.Drawing;
using System.Linq;


internal class Program{
    private static void Main() {
        //which game are you using this for? 
        string game = "Vic3"; //    "Vic3" for Victoria 3, "CK3" for Crusader Kings 3 and Imperator: Rome,
                              //only set up for Vic3

        string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        //stopwatch
        Stopwatch stopwatch = Stopwatch.StartNew();

        // open up image file rivers
        Bitmap riverBmp = new(localDir + @"\_Input\rivers.png");
        List<Color> NotRiverColors = new() {
            Color.FromArgb(255, 255, 255),  //land
            Color.FromArgb(122, 122, 122),  //water grey
            Color.FromArgb(255, 0, 128)     //water pink
        };

        List<(int x, int y)> riverCoordList = new();
        
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

        
        Dictionary<Color, Prov> provDict = new();
        
        ParseProvMap(provDict, colorArray);
        
        parseDefaultMap(provDict);

        //set riverCoordSet to riverCoordList
        HashSet<(int, int)> riverCoordSet = new();
        foreach ((int, int) c in riverCoordList) {
            riverCoordSet.Add(c);
        }


        //add every prov in provDict whos coordlist contains a riverCoord to list
        List<Prov> riverSplittingProvList = new();
        foreach (Prov p in provDict.Values) {
            p.setHashSet();
            if (!riverSplittingProvList.Contains(p)) {
                if (p.coords.Intersect(riverCoordSet).Any()) {
                    riverSplittingProvList.Add(p);
                }
            }
        }
        Console.WriteLine("River Containing Provs: " + riverSplittingProvList.Count);
        
        debugDrawRiverSplitingProvs(riverSplittingProvList);
        updateMap6(colorArray, riverCoordList, provDict);
        
        compareResult();

        
        

        void ParseProvMap(Dictionary<Color, Prov> provDict, Color[,] colorArray) {
            Bitmap bitmap = new(10, 10);
            //check if localDir + "/_Input/provinces.png" exists, if so set bitmap to it
            if (File.Exists(localDir + @"/_Input/provinces.png")) {
                bitmap = new Bitmap(localDir + @"\_Input\provinces.png");
            }
            //else if provinces.bmp
            else if (File.Exists(localDir + @"\_Input\provinces.bmp")) {
                bitmap = new Bitmap(localDir + @"\_Input\provinces.bmp");
            }
            //print error and exit
            else {
                Console.WriteLine("Error: No provinces.png or provinces.bmp found in _Input folder, please add one and try again");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("Parsing Province Map");
            //for each pixel in bitmap
            for (int i = 0; i < bitmap.Width; i++) {
                for (int j = 0; j < bitmap.Height; j++) {
                    //if pixle color is not a key in provDict add it as a key
                    if (!provDict.ContainsKey(bitmap.GetPixel(i, j))) {
                        provDict.Add(bitmap.GetPixel(i, j), new Prov(bitmap.GetPixel(i, j)));
                    }
                    provDict[bitmap.GetPixel(i, j)].coords.Add((i, j));
                    //add color to colorArray
                    colorArray[i, j] = bitmap.GetPixel(i, j);

                }
                //print progress every 500 iterations as a percentage to 2 decimal places
                if (i % 500 == 0) {
                    Console.WriteLine("\t" + (i * 100 / bitmap.Width) + "%");
                }
            }



        }

        void parseDefaultMap(Dictionary<Color, Prov> provDict) {
            //open _Input/Vic3/default.map
            string[] lines = File.ReadAllLines(localDir + "/_Input/default.map");

            bool waterFound = false;
            foreach (string line in lines) {
                string l1 = line.Replace("{", " { ").Replace("}", " } ").Replace("=", " = ").Replace("#", " # ").Trim();
                if (l1.StartsWith("sea_start") || l1.StartsWith("lakes")) {
                    waterFound = true;
                }
                if (waterFound) {
                    string[] l2 = l1.Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        else if (l2[i].StartsWith("x")) {
                            Color c = ColorTranslator.FromHtml("#" + l2[i].Replace("x", ""));
                            //find key in provDict
                            if (provDict.TryGetValue(c, out var p)) {
                                p.isWater = true;
                            }

                        }
                        else if (l2[i].StartsWith("}")) {
                            waterFound = false;
                            break;
                        }
                    }

                }
            }
        }

        //draw all riverSplittingProvList to a new image and save it
        void debugDrawRiverSplitingProvs(List<Prov> riverSplittingProvList) {
            //check if _Output/ exists, if not, create it
            if (!Directory.Exists(localDir + "/_Output/")) {
                Directory.CreateDirectory(localDir + "/_Output/");
            }

            //get size of province png file and create new bitmap with that size and graphics object 
            Bitmap bmp1 = new(localDir + "/_Input/provinces.png");

            Bitmap bmp = new(bmp1.Width, bmp1.Height);
            foreach (Prov p in riverSplittingProvList) {
                if (p.isWater) continue;

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


            Bitmap bmp2 = new(bmp1.Width, bmp1.Height);
            Bitmap bmp3 = new(bmp1.Width, bmp1.Height);
            int count = 0;
            //finds the largest contiguous area of a prov and set it to coordsLargetstContig
            foreach (Prov p in riverSplittingProvList) {
                if (p.isWater) continue;

                p.getContiguousArea();

                //if coordsLargestContig == coordsRiverless then the prov is not split by a river and can be skiped
                if (p.coordsLargestContig.Count == p.coordsRiverless.Count) {
                    continue;
                }

                //Console.WriteLine(p.getHexColor() + ": " + p.coordsLargestContig.Count);

                foreach ((int, int) c in p.coordsLargestContig) {
                    bmp2.SetPixel(c.Item1, c.Item2, p.color);
                }
                foreach ((int, int) c in p.coordsRiverless)
                    bmp3.SetPixel(c.Item1, c.Item2, p.color);
                p.isContiguous = false;
                count++;

            }
            bmp3.Save(localDir + "/_Output/01_crossing.png");
            bmp2.Save(localDir + "/_Output/02_one_sided.png");

            Console.WriteLine("River Splitting Prov: " + count);
            Console.WriteLine(stopwatch.Elapsed + "s");
        }

        void updateMap6(Color[,] colorArray, List<(int x, int y)> riverCoordList, Dictionary<Color, Prov> provDict) {
            //river hash set
            HashSet<(int, int)> riverCoordSet = riverCoordList.ToHashSet();

            //hash set of all coords that are on the other side of a river
            HashSet<(int, int)> otherSideRiverCoordSet = new();
            foreach (Prov p in provDict.Values) {
                foreach ((int, int) c in p.otherCoords)
                    otherSideRiverCoordSet.Add(c);
            }

            List<(int, int)> otherSideRiverCoordList = otherSideRiverCoordSet.ToList();

            Console.WriteLine("Updating Map");
            int n = 1000;
            List<(int, int)> removeList = new();
            for (int count = 0; count < n; count++) {
                //print progress every 10% as long as count is not 0
                if (count % (n / 20) == 0 && count != 0) {
                    //to 1 decimal places
                    Console.WriteLine("\t" + Math.Round((float)count / n * 100, 0) + "%\t" + stopwatch.Elapsed);
                }


                //randomize otherSideRiverCoordSet
                otherSideRiverCoordList = otherSideRiverCoordList.OrderBy(x => Guid.NewGuid()).ToList();
                int pCount = 0;
                //for each otherSideRiverCoordSet
                foreach ((int x, int y) c in otherSideRiverCoordList) {
                    Dictionary<Color, int> colorCount = new();
                    //find the colors and number of pixels of each color in a 3x3 grid around c
                    //skipping if the color of prov isWater or is in riverCoordSet
                    for (int i = -1; i < 2; i++) {
                        for (int j = -1; j < 2; j++) {
                            if (i == 0 && j == 0) {
                                continue;
                            }
                            else if (c.x + i < 0 || c.x + i >= colorArray.GetLength(0) || c.y + j < 0 || c.y + j >= colorArray.GetLength(1)) {
                                continue;
                            }
                            //check if color at colorArray[c.x + i, c.y + j] in provDict iswater ture
                            else if (provDict.ContainsKey(colorArray[c.x + i, c.y + j]) && provDict[colorArray[c.x + i, c.y + j]].isWater) {
                                continue;
                            }

                            else if (otherSideRiverCoordSet.Contains((c.x + i, c.y + j))) {
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
                    //if colorCount is not empty, set colorArray[c] to the most common color
                    if (colorCount.Count > 0) {
                        //if most common color has less than half of the pixels
                        if (colorCount.First().Value * 2 < pCount) {

                            int maxCount = colorCount.First().Value;
                            //if multiple colors have maxCount number of pixels, choose one at random and set colorArray[c] to that color
                            List<Color> maxColorList = new();
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
                foreach ((int x, int y) in riverCoordList) {

                    //if pixel at c isWater, continue
                    if (provDict.ContainsKey(colorArray[x, y]) && provDict[colorArray[x, y]].isWater) {
                        continue;
                    }


                    //check 8 surounding colors
                    Dictionary<Color, int> colorCount = new();
                    for (int i = -1; i < 2; i++) {
                        for (int j = -1; j < 2; j++) {
                            if (i == 0 && j == 0) {
                                continue;
                            }
                            else if (x + i < 0 || x + i >= colorArray.GetLength(0) || y + j < 0 || y + j >= colorArray.GetLength(1)) {
                                continue;
                            }
                            else if (provDict.ContainsKey(colorArray[x + i, y + j]) && provDict[colorArray[x + i, y + j]].isWater) {
                                continue;
                            }

                            if (colorCount.ContainsKey(colorArray[x + i, y + j])) {
                                colorCount[colorArray[x + i, y + j]]++;
                            }
                            else {
                                colorCount.Add(colorArray[x + i, y + j], 1);
                            }
                        }
                    }


                    //if colorCount is not empty, set colorArray[c] to the most common color
                    if (colorCount.Count > 0) {
                        //if the most common color has at least 3 pixels, set colorArray[c] to the most common color
                        if (colorCount.First().Value > 2) {
                            colorArray[x, y] = colorCount.First().Key;
                        }

                    }
                }
            }

            //save updated map
            Console.WriteLine("Saving Updated Map");
            Bitmap bmp = new(colorArray.GetLength(0), colorArray.GetLength(1));
            for (int i = 0; i < colorArray.GetLength(0); i++) {
                for (int j = 0; j < colorArray.GetLength(1); j++) {
                    bmp.SetPixel(i, j, colorArray[i, j]);
                }
            }
            bmp.Save(localDir + "/_Output/03_updated_map.png");

        }

        void compareResult() {
            Console.WriteLine("Generating Comparison");

            int changedPx = 0;

            //create a new 04_compare.png file and compare the result to the original map
            Bitmap bmp1 = new(localDir + "/_Input/provinces.png");
            Bitmap bmp2 = new(localDir + "/_Output/03_updated_map.png");

            Bitmap bmp = new(bmp1.Width, bmp1.Height);
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
                Console.WriteLine("\n\n" + changedPx + " pixles have been changed this pass\n\n");
            }
            else {
                Console.WriteLine("\n\nOutput map is the same as the input\n\n");
            }

        }

        Console.WriteLine(stopwatch.Elapsed + "s");
    }
}