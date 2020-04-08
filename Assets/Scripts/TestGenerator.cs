using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TestGenerator
{
    public enum BiomeKey
    {
        water,
        sand,
        other
    }

    public static Color[] colors = new Color[] {
        Color.cyan,
        Color.grey,
        Color.magenta,
        Color.red,
        Color.yellow,
        Color.black,
        Color.white,
        Color.green
    };

    public struct Biome
    {
        public BiomeKey key;

        public float baseHeight;
        public Vector2 maximumHeightVariance; // x is down, y is up
        public float maximumRateOfChange;
        [Range(0, 1)]
        public float moisture; // determines hydraulic erosion
        public float averageTemperature; // determines thermal erosion
        public Vector2 temperatureVariance; // x is down, y is up
        public float transitionDst;
        // transitionMaterial; // used for stuff like Dolomites, Italy (sand)

        // Testing
        public Color color;

        public static Biome Lerp(Biome a, Biome b, float t)
        {
            return new Biome
            {
                baseHeight = Mathf.Lerp(a.baseHeight, b.baseHeight, t),
                maximumHeightVariance = Vector2.Lerp(a.maximumHeightVariance, b.maximumHeightVariance, t), // x is down, y is up
                maximumRateOfChange = Mathf.Lerp(a.maximumRateOfChange, b.maximumRateOfChange, t),
                moisture = Mathf.Lerp(a.moisture, b.moisture, t), // determines hydraulic erosion
                averageTemperature = Mathf.Lerp(a.averageTemperature, b.averageTemperature, t), // determines thermal erosion
                temperatureVariance = Vector2.Lerp(a.temperatureVariance, b.temperatureVariance, t), // x is down, y is up
                transitionDst = Mathf.Lerp(a.transitionDst, b.transitionDst, t),
                // transitionMaterial; // used for stuff like Dolomites, Italy (sand)

                // Testing
                color = Color.Lerp(a.color, b.color, t),
            };
        }
    }

    public struct vDst
    {
        public int index;
        public float dst;
    }

    public struct Cell
    {
        public float height;

        public vDst[] dst;

        public Biome biome;

        public Material material;
    }

    public struct vPoint
    {
        public Vector2 position;
        public Biome biome;
    }

    public static Biome sand = new Biome
    {
        color = new Color(255f / 255f, 224f / 255f, 173f / 255f),

        key = BiomeKey.sand,
        baseHeight = 0f,
        maximumHeightVariance = new Vector2(0, 10), // x is down, y is up
        maximumRateOfChange = 0.5f,
        moisture = 1.0f, // super high here but we dont want any hydraulic erosion
        averageTemperature = 20f, // determines thermal erosion
        temperatureVariance = new Vector2(10f, 10f) // x is down, y is up
    };

    public static Biome water = new Biome
    {
        color = new Color(0f / 255f, 191f / 255f, 255f / 255f),

        key = BiomeKey.water,
        baseHeight = 0f,
        maximumHeightVariance = new Vector2(100f, 0f), // x is down, y is up
        maximumRateOfChange = 100.0f,
        moisture = 100.0f, // super high here but we dont want any hydraulic erosion
        averageTemperature = 15f, // determines thermal erosion
        temperatureVariance = new Vector2(10f, 10f) // x is down, y is up
    };

    private static vPoint[] GeneratePoints(System.Random random, int width, int height, int numberOfBiomes, float edgeCuttoffPercent, bool useGrid = true)
    {
        if (useGrid)
        {
            var maxSupportedGridWidth = 10;
            var gridWidth = 0;
            for (var i = 1; i <= maxSupportedGridWidth; i++)
            {
                if (numberOfBiomes <= i * i)
                {
                    gridWidth = i;
                    break;
                }
            }

            if (gridWidth == 0)
            {
                throw new ArgumentOutOfRangeException("Unsupported number of biomes.");
            }

            var voronoiPoints = new vPoint[gridWidth * gridWidth];

            var takenColors = new List<int>(numberOfBiomes);

            var cellWidth = Mathf.RoundToInt(width / gridWidth);
            var cellHeight = Mathf.RoundToInt(height / gridWidth);
            var cellsToFill = Enumerable.Repeat(false, gridWidth * gridWidth).ToList();

            var filled = 0;
            while (filled < numberOfBiomes)
            {
                int index;
                do
                {
                    index = random.Next(0, gridWidth * gridWidth);
                } while (cellsToFill[index]);

                cellsToFill[index] = true;
                filled++;
            }

            var normoColors = 0;
            for (var v = 0; v < gridWidth * gridWidth; v++)
            {
                var x = v % gridWidth;
                var y = v / gridWidth;

                voronoiPoints[v] = new vPoint
                {
                    // randomly generate one point on every cell of an evenly spaced grid (makes a much more even layout)
                    position = new Vector2(
                        (width * edgeCuttoffPercent) + (cellWidth * x) + random.Next(0, cellWidth),
                        (height * edgeCuttoffPercent) + (cellHeight * y) + random.Next(0, cellHeight)),
                    biome = cellsToFill[v] ? new Biome
                    {
                        color = colors[normoColors],
                        baseHeight = random.Next(10, 1000),
                        maximumHeightVariance = new Vector2(random.Next(0, 1000), random.Next(0, 1000)), // x is down, y is up
                        maximumRateOfChange = random.Next(0, 100000) * 0.01f, // TODO: @Max, use an array or something of possible values, as it stand we could get 1000, 999, 998 which are essentially the same in terms of the terrain itself
                        moisture = (float)random.NextDouble(), // super high here but we dont want any hydraulic erosion
                        averageTemperature = random.Next(-50, 50), // determines thermal erosion // TODO: @Max, have this average around 10-20 or so
                        temperatureVariance = new Vector2(random.Next(0, 50), random.Next(0, 50)), // x is down, y is up

                        transitionDst = random.Next(0, Mathf.RoundToInt((width * 0.25f) + (height * 0.25f)))
                    } : water
                };

                if (cellsToFill[v])
                {
                    normoColors++;
                }
            }

            return voronoiPoints;
        }
        else
        {
            var voronoiPoints = new vPoint[numberOfBiomes];
            for (var v = 0; v < numberOfBiomes; v++)
            {
                voronoiPoints[v] = new vPoint
                {
                    // randomly generate one point on every cell of an evenly spaced grid (makes a much more even layout)
                    position = new Vector2(
                        random.Next(Mathf.RoundToInt(width * edgeCuttoffPercent), Mathf.RoundToInt(width * (1.0f - edgeCuttoffPercent))),
                        random.Next(Mathf.RoundToInt(height * edgeCuttoffPercent), Mathf.RoundToInt(height * (1.0f - edgeCuttoffPercent)))),
                    biome = new Biome
                    {
                        color = colors[v],
                        baseHeight = random.Next(10, 1000),
                        maximumHeightVariance = new Vector2(random.Next(0, 1000), random.Next(0, 1000)), // x is down, y is up
                        maximumRateOfChange = random.Next(0, 100000) * 0.01f, // TODO: @Max, use an array or something of possible values, as it stand we could get 1000, 999, 998 which are essentially the same in terms of the terrain itself
                        moisture = (float)random.NextDouble(), // super high here but we dont want any hydraulic erosion
                        averageTemperature = random.Next(-50, 50), // determines thermal erosion // TODO: @Max, have this average around 10-20 or so
                        temperatureVariance = new Vector2(random.Next(0, 50), random.Next(0, 50)), // x is down, y is up

                        transitionDst = random.Next(0, Mathf.RoundToInt((width * 0.25f) + (height * 0.25f)))
                    }
                };
            }
            return voronoiPoints;
        }
    }

    public static Cell[,] GenerateVoronoiMap(int width, int height, int numberOfBiomes, int seed, float edgeCuttoffPercent, bool useGrid)
    {
        var lesserCutOff = edgeCuttoffPercent;
        var greaterCutOff = 1.0f - lesserCutOff;

        var map = new Cell[width, height];
        var random = new System.Random(seed);

        var voronoiPoints = GeneratePoints(random, width, height, numberOfBiomes, edgeCuttoffPercent, useGrid);

        var dst = Enumerable.Repeat(new vDst { dst = 1.0f, index = -1 }, numberOfBiomes).ToList();

        for (var j = 0; j < height; j++)
            for (var i = 0; i < width; i++)
            {
                // could optimize this using the grid if we wanted to
                for (var v = 0; v < numberOfBiomes; v++)
                {
                    dst[v] = new vDst
                    {
                        dst = Vector2.Distance(new Vector2(i, j), voronoiPoints[v].position),
                        index = v
                    };
                }

                dst.Sort((a, b) => (int)Mathf.Sign(b.dst - a.dst));

                var waterShrinkFactor = 1.0f;
                var beachSize = 75.0f;
                var beachTransitionFactor = 2.0f;

                var biome1 = voronoiPoints[dst[0].index].biome;
                var biome2 = voronoiPoints[dst[1].index].biome;

                var left = dst[0].dst > i * waterShrinkFactor;
                var right = dst[0].dst > (width - i) * waterShrinkFactor;
                var top = dst[0].dst > j * waterShrinkFactor;
                var bot = dst[0].dst > (height - j) * waterShrinkFactor;
                var isEdge = i < width * lesserCutOff || i > width * greaterCutOff || j < height * lesserCutOff || j > height * greaterCutOff;

                var isWater = top || bot || left || right || isEdge || biome1.key == BiomeKey.water;
                if (isWater)
                {
                    var isSand = !isEdge && (
                        biome1.key == BiomeKey.water ? (dst[0].dst - dst[1].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        left ? Mathf.Abs((i * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        right ? Mathf.Abs(((width - i) * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        top ? Mathf.Abs((j * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        bot ? Mathf.Abs(((height - j) * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        throw new Exception("This is impossible."));

                    if (isSand)
                    {
                        var transitionDst =
                        biome1.key == BiomeKey.water ? (dst[0].dst - dst[1].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        left ? Mathf.Abs((i * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        right ? Mathf.Abs(((width - i) * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        top ? Mathf.Abs((j * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        bot ? Mathf.Abs(((height - j) * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        throw new Exception("This is also impossible.");

                        var inTransition = transitionDst <= 1.0f;

                        map[i, j] = inTransition ? new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            biome = Biome.Lerp(sand, biome1, 1.0f - transitionDst)
                        } : new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            biome = water
                        };
                    }
                    else
                    {
                        map[i, j] = new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            biome = water
                        };
                    }
                }
                else
                {
                    var transitionDst = (biome1.transitionDst + biome2.transitionDst) / 2.0f;
                    var inTransition = Mathf.Abs(dst[0].dst - dst[1].dst) < transitionDst;

                    map[i, j] = inTransition
                        ? new Cell
                        {
                            dst = new vDst[] { dst[0], dst[1] },
                            biome = Biome.Lerp(biome1, biome2, 0.5f - (Mathf.Abs(dst[1].dst - dst[0].dst) * 0.5f / transitionDst))
                        }
                        : new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            biome = biome1
                        };
                }
            }

        return map;
    }

    // alter domain warping amplitude depending on biome
    public static Color[] DomainWarpMap(int width, int height, int seed) // NoiseSettings settings { seed, scale, ... }
    {
        // domain warping in js
        var data = new Color[width * height]; //imgdata.data,

        float fbm(float x, float y, float scale = 1f, int octaves = 1, float lacunarity = 2f, float gain = 0.5f)
        {
            var total = 0f;
            var amplitude = 1f;
            var frequency = 1f;

            for (var i = 0; i < octaves; i++)
            {
                var v = Mathf.PerlinNoise(x / scale * frequency, y / scale * frequency) * amplitude;
                total += v;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            return total;
        }

        float pattern(float x, float y, float scale = 1f, int octaves = 1, float lacunarity = 2f, float gain = 0.5f)
        {
            var q = new Vector2(
                    fbm(x, y, scale, octaves, lacunarity, gain),
                    fbm(x + 5.2f, y + 1.3f, scale, octaves, lacunarity, gain));

            // alter domain warping amplitude depending on biome
            var amplitude = 80.0f;
            return fbm(x + (amplitude * q[0]), y + (amplitude * q[1]), scale, octaves, lacunarity, gain);
        }

        var damping = 0.8f;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var v = (pattern(x, y, 25f, 3) + 1f) * 128f * damping;
                if (v > 255f)
                {
                    Debug.Log(v);
                }
                data[x + (y * width)] = new Color(v / 255f, v / 255f, v / 255f);
            }

        return data;
    }

    private static float fbm(float x, float y, float scale = 1f, int octaves = 1, float lacunarity = 2f, float gain = 0.5f)
    {
        var total = 0f;
        var amplitude = 1f;
        var frequency = 1f;

        for (var i = 0; i < octaves; i++)
        {
            var v = Mathf.PerlinNoise(x / scale * frequency, y / scale * frequency) * amplitude;
            total += v;
            frequency *= lacunarity;
            amplitude *= gain;
        }

        return total;
    }

    // TODO: @Max, data etc is super memory and cpu inefficien. Not an issue for smaller maps but it is for huge maps.
    public static T[,] DomainWarpMap<T>(T[,] original, int width, int height, int seed, T fallback, float warpingAmplitude = 80.0f, float adjustmentFactor = 1.2f, float noiseScale = 25f) // NoiseSettings settings { seed, scale, ... }
    {
        // domain warping in js
        var adjustedWidth = Mathf.RoundToInt(width * adjustmentFactor);// Mathf.RoundToInt(width + warpingAmplitude);
        var adjustedHeight = Mathf.RoundToInt(height * adjustmentFactor);// Mathf.RoundToInt(height + warpingAmplitude);
        var data = new T[adjustedWidth, adjustedHeight]; //imgdata.data,
        var offset = new Vector2(-warpingAmplitude, -warpingAmplitude); // * 0.825f;

        T pattern(float x, float y, float scale = 1f, int octaves = 1, float lacunarity = 2f, float gain = 0.5f)
        {
            var q = new Vector2(
                    fbm(x + seed, y + seed, scale, octaves, lacunarity, gain),
                    fbm(x + 5.2f + seed, y + 1.3f + seed, scale, octaves, lacunarity, gain));

            // alter domain warping amplitude depending on biome
            var indexX = Mathf.RoundToInt(x + (warpingAmplitude * q[0]));
            var indexY = Mathf.RoundToInt(y + (warpingAmplitude * q[1]));
            return indexX >= width || indexX < 0 || indexY >= height || indexY < 0 ? fallback : original[indexX, indexY];
        }

        for (var y = 0; y < adjustedHeight; y++)
            for (var x = 0; x < adjustedWidth; x++)
            {
                var indexX = Mathf.RoundToInt(x + offset.x);
                var indexY = Mathf.RoundToInt(y + offset.y);

                data[x, y] = pattern(indexX, indexY, noiseScale, 3);
            }

        return data;
    }

    public static Cell[,] FillHeightMap(Cell[,] original, int width, int height, int seed, float noiseScale)
    {
        var copy = new Cell[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var octaves = 1;
                var lacunarity = 2f;
                var gain = 0.5f;

                var value = fbm(x + seed, y + seed, noiseScale, octaves, lacunarity, gain);
                copy[x, y].height = value;
                Debug.Log(value);
            }

        return copy;
    }

    public static float[,] TestFillHeightMap(Cell[,] original, int width, int height, int seed, float noiseScale, float heightScale)
    {
        var copy = new float[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var octaves = 5;
                var lacunarity = 4f;
                var gain = 0.2f;

                var cell = original[x, y];

                // math

                var heightValue = fbm(x + seed, y + seed, noiseScale, octaves, lacunarity, gain);

                var value = cell.biome.baseHeight + (heightValue - 0.5f) < 0 ?
                    (heightValue - 0.5f) * cell.biome.maximumHeightVariance.x :
                    (heightValue - 0.5f) * cell.biome.maximumHeightVariance.y;

                copy[x, y] = value * heightScale;
            }

        return copy;
    }
}
