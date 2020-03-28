using UnityEngine;

public class Erosion : MonoBehaviour
{
    [Range(2, 8)]
    public int erosionRadius = 3;
    [Range(0, 1)]
    public float inertia = .05f; // At zero, water will instantly change direction to flow downhill. At 1, water will never change direction. 
    public float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    public float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
    [Range(0, 1)]
    public float erodeSpeed = .3f;
    [Range(0, 1)]
    public float depositSpeed = .3f;
    [Range(0, 1)]
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public int maxDropletLifetime = 30;
    [Range(0, 500000)]
    public int Iterations = 50000;

    public float initialWaterVolume = 1;
    public float initialSpeed = 1;

    // Indices and weights of erosion brush precomputed for every node
    (int x, int y)[,][] erosionBrushIndices;
    float[,][] erosionBrushWeights;
    System.Random prng;

    int currentSeed;
    int currentErosionRadius;
    int currentMapSize;

    // Initialization creates a System.Random object and precomputes indices and weights of erosion brush
    void Initialize(int mapSize, int seed)
    {
        if (prng == null || currentSeed != seed)
        {
            prng = new System.Random(seed);
            currentSeed = seed;
        }

        if (erosionBrushIndices == null || currentErosionRadius != erosionRadius || currentMapSize != mapSize)
        {
            InitializeBrushIndices(mapSize, erosionRadius);
            currentErosionRadius = erosionRadius;
            currentMapSize = mapSize;
        }
    }

    // TODO: @Max, multithread this
    public void Erode(ref float[,] map, int mapSize, int seed)
    {
        Initialize(mapSize, seed);

        //Debug.Log("Erroding for iterations: " + Iterations);

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            // Create water droplet at random point on map
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            var speed = initialSpeed;
            var water = initialWaterVolume;
            float sediment = 0;

            for (var lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                var nodeX = (int)posX;
                var nodeY = (int)posY;
                var dropletIndex = (nodeY * mapSize) + nodeX;
                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                var cellOffsetX = posX - nodeX;
                var cellOffsetY = posY - nodeY;

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                var heightAndGradient = CalculateHeightAndGradient(map, mapSize, posX, posY);

                // Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = (dirX * inertia) - (heightAndGradient.gradientX * (1 - inertia));
                dirY = (dirY * inertia) - (heightAndGradient.gradientY * (1 - inertia));
                // Normalize direction
                var len = Mathf.Sqrt((dirX * dirX) + (dirY * dirY));
                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1)
                {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                var newHeight = CalculateHeightAndGradient(map, mapSize, posX, posY).height;
                var deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                var sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    var amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[nodeX, nodeY] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[nodeX + 1, nodeY] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[nodeX, nodeY + 1] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[nodeX + 1, nodeY + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                }
                else
                {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    var amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    for (var brushPointIndex = 0; brushPointIndex < erosionBrushIndices[nodeX, nodeY].Length; brushPointIndex++)
                    {
                        var (nodeIndexX, nodeIndexY) = erosionBrushIndices[nodeX, nodeY][brushPointIndex];
                        var weighedErodeAmount = amountToErode * erosionBrushWeights[nodeX, nodeY][brushPointIndex];
                        var deltaSediment = (map[nodeIndexX, nodeIndexY] < weighedErodeAmount) ? map[nodeIndexX, nodeIndexY] : weighedErodeAmount;
                        map[nodeIndexX, nodeIndexY] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                // Update droplet's speed and water content
                speed = Mathf.Sqrt((speed * speed) + (deltaHeight * gravity));
                water *= 1 - evaporateSpeed;
            }
        }
    }

    HeightAndGradient CalculateHeightAndGradient(float[,] nodes, int mapSize, float posX, float posY)
    {
        var coordX = (int)posX;
        var coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        var x = posX - coordX;
        var y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        var heightNW = nodes[coordX, coordY];
        var heightNE = nodes[coordX + 1, coordY];
        var heightSW = nodes[coordX, coordY + 1];
        var heightSE = nodes[coordX + 1, coordY + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        var gradientX = ((heightNE - heightNW) * (1 - y)) + ((heightSE - heightSW) * y);
        var gradientY = ((heightSW - heightNW) * (1 - x)) + ((heightSE - heightNE) * x);

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        var height = (heightNW * (1 - x) * (1 - y)) + (heightNE * x * (1 - y)) + (heightSW * (1 - x) * y) + (heightSE * x * y);

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    void InitializeBrushIndices(int mapSize, int radius)
    {
        erosionBrushIndices = new (int x, int y)[mapSize, mapSize][];
        erosionBrushWeights = new float[mapSize, mapSize][];

        var xOffsets = new int[radius * radius * 4];
        var yOffsets = new int[radius * radius * 4];
        var weights = new float[radius * radius * 4];
        float weightSum = 0;
        var addIndex = 0;

        for (var centreY = 0; centreY < mapSize; centreY++)
            for (var centreX = 0; centreX < mapSize; centreX++)
            {
                if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius)
                {
                    weightSum = 0;
                    addIndex = 0;
                    for (var y = -radius; y <= radius; y++)
                    {
                        for (var x = -radius; x <= radius; x++)
                        {
                            float sqrDst = (x * x) + (y * y);
                            if (sqrDst < radius * radius)
                            {
                                var coordX = centreX + x;
                                var coordY = centreY + y;

                                if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize)
                                {
                                    var weight = 1 - (Mathf.Sqrt(sqrDst) / radius);
                                    weightSum += weight;
                                    weights[addIndex] = weight;
                                    xOffsets[addIndex] = x;
                                    yOffsets[addIndex] = y;
                                    addIndex++;
                                }
                            }
                        }
                    }
                }

                var numEntries = addIndex;
                erosionBrushIndices[centreX, centreY] = new (int, int)[numEntries];
                erosionBrushWeights[centreX, centreY] = new float[numEntries];

                for (var j = 0; j < numEntries; j++)
                {
                    erosionBrushIndices[centreX, centreY][j] = (xOffsets[j] + centreX, yOffsets[j] + centreY);
                    erosionBrushWeights[centreX, centreY][j] = weights[j] / weightSum;
                }
            }
    }

    struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}