using System.Drawing;

public class Prov
{
    public string name = "";
    public int provID = -1;
    public Color color = new Color();
    public List<(int, int)> coords = new List<(int, int)>();
    public List<(int, int)> coordsRiverless = new List<(int, int)>();
    public List<(int, int)> coordsLargestContig = new List<(int, int)>();
    public List<(int, int)> otherCoords = new List<(int, int)>();
    public List<(int, int)> riverCoords = new List<(int, int)>();
    public bool isWater = false;
    public bool isWasteland = false;
    public bool isContiguous = true;

    //hash set of coords
    public HashSet<(int, int)> coordSet = new HashSet<(int, int)>();

    

    public Prov(Color c) {
        color = c;
    }
    public Prov() {
    }

    //Get HexColor
    public string getHexColor() {
        return ColorTranslator.ToHtml(color).Replace("#", "x");
    }

    //set HashSet
    public void setHashSet() {
        foreach ((int, int) coord in coords) {
            coordSet.Add(coord);
        }
    }

    //toString override
    public override string ToString() {
        return getHexColor();
    }

    //Get Contiguous Area
    public void getContiguousArea() {
        //find min and max x,y values in coordsRiverless
        int minX = coordsRiverless[0].Item1;
        int maxX = coordsRiverless[0].Item1;
        int minY = coordsRiverless[0].Item2;
        int maxY = coordsRiverless[0].Item2;
        foreach ((int, int) coord in coordsRiverless) {
            if (coord.Item1 < minX) {
                minX = coord.Item1;
            }
            if (coord.Item1 > maxX) {
                maxX = coord.Item1;
            }
            if (coord.Item2 < minY) {
                minY = coord.Item2;
            }
            if (coord.Item2 > maxY) {
                maxY = coord.Item2;
            }
        }

        //create a 2d array of ints
        int[,] intArray = new int[maxX - minX + 1, maxY - minY + 1];

        //fill intArray with 0
        for (int i = 0; i < maxX - minX + 1; i++) {
            for (int j = 0; j < maxY - minY + 1; j++) {
                intArray[i, j] = 0;
            }
        }

        //fill intArray with 1 where coordsRiverless is
        foreach ((int, int) coord in coordsRiverless) {
            intArray[coord.Item1 - minX, coord.Item2 - minY] = 1;
        }

        //find the largest contiguous area of 1s in intArray and add those coords to coordsLargestContig (coordsLargestContig is a list of tuples)
        int largestContig = 0;

        //find coords for largest contigous area
        for (int i = 0; i < maxX - minX + 1; i++) {
            for (int j = 0; j < maxY - minY + 1; j++) {
                if (intArray[i, j] == 1) {
                    int contig = 0;
                    List<(int, int)> contigCoords = new List<(int, int)>();
                    contig = getContig(intArray, i, j, contig, contigCoords, minX, minY);
                    if (contig > largestContig) {
                        largestContig = contig;
                        coordsLargestContig = contigCoords;
                    }
                }
            }
        }

        //otherCoords is a list of tuples of coords that are in coordsRiverless but not in coordsLargestContig
        foreach ((int, int) coord in coordsRiverless) {
            if (!coordsLargestContig.Contains(coord)) {
                otherCoords.Add(coord);
            }
        }

        //set riverCoords to coords - coordsRiverless
        foreach ((int, int) coord in coords) {
            if (!coordsRiverless.Contains(coord)) {
                riverCoords.Add(coord);
            }
        }

    }

    //getContig
    public int getContig(int[,] intArray, int i, int j, int contig, List<(int, int)> contigCoords, int minX = 0, int minY = 0) {
        if (i < 0 || i >= intArray.GetLength(0) || j < 0 || j >= intArray.GetLength(1)) {
            return contig;
        }
        if (intArray[i, j] == 0) {
            return contig;
        }
        if (intArray[i, j] == 1) {
            contig++;
            contigCoords.Add((i + minX, j + minY));
            intArray[i, j] = 0;
            contig = getContig(intArray, i + 1, j, contig, contigCoords, minX, minY);
            contig = getContig(intArray, i - 1, j, contig, contigCoords, minX, minY);
            contig = getContig(intArray, i, j + 1, contig, contigCoords, minX, minY);
            contig = getContig(intArray, i, j - 1, contig, contigCoords, minX, minY);
            return contig;
        }
        return contig;
    }

}
