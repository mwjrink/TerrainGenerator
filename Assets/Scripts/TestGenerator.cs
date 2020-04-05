using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TestGenerator
{
    public struct Biome
    {
        public int baseHeight;
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

        // args;
        [Range(0, 1)]
        public float moisture; // determines hydraulic erosion
        public float averageTemperature; // determines thermal erosion
        public Vector2 temperatureVariance; // x is down, y is up
        public Material material;

        public Color color;
    }

    public struct vPoint
    {
        public Vector2 position;
        public Biome biome;
    }

    public static Cell[,] GenerateVorotoiMap(int width, int height, int numberOfBiomes, int seed, float edgeCuttoffPercent)
    {
        var lesserCutOff = edgeCuttoffPercent;
        var greaterCutOff = 1.0f - lesserCutOff;

        var map = new Cell[width, height];
        var random = new System.Random(seed);

        var voronoiPoints = new vPoint[numberOfBiomes];

        var colors = new Color[] {
            Color.cyan,
            Color.clear,
            Color.grey,
            Color.magenta,
            Color.red,
            Color.yellow,
            Color.black,
            Color.white,
            Color.green,
            Color.blue
        };
        var takenColors = new List<int>(numberOfBiomes);

        for (var v = 0; v < numberOfBiomes; v++)
        {
            var colorIndex = random.Next(0, colors.Length - 1);
            do
            {
                colorIndex = random.Next(0, colors.Length - 1);
            } while (takenColors.Contains(colorIndex));
            takenColors.Add(colorIndex);

            voronoiPoints[v] = new vPoint
            {
                // randomly generate one point on every cell of an evenly spaced grid (makes a much more even layout)
                position = new Vector2(
                    random.Next(Mathf.RoundToInt(width * lesserCutOff), Mathf.RoundToInt(width * greaterCutOff)),
                    random.Next(Mathf.RoundToInt(height * lesserCutOff), Mathf.RoundToInt(height * greaterCutOff))),
                biome = new Biome
                {
                    color = colors[colorIndex],
                    //public int baseHeight;
                    //public Vector2 maximumVariance; // x is down, y is up
                    //public float maximumRateOfChange;
                    //[Range(0, 1)]
                    //public float moisture; // determines hydraulic erosion
                    //public float averageTemperature; // determines thermal erosion
                    //public Vector2 temperatureVariance; // x is down, y is up

                    transitionDst = random.Next(0, Mathf.RoundToInt((width * 0.025f) + (height * 0.025f)))
                }
            };
        }

        var dst = new List<vDst>(numberOfBiomes);

        for (var v = 0; v < numberOfBiomes; v++)
            dst.Add(new vDst { dst = 1.0f, index = -1 });

        for (var j = 0; j < height; j++)
            for (var i = 0; i < width; i++)
            {
                for (var v = 0; v < numberOfBiomes; v++)
                {
                    dst[v] = new vDst
                    {
                        dst = Vector2.Distance(new Vector2(i, j), voronoiPoints[v].position),
                        index = v
                    };
                }

                dst.Sort((a, b) => (int)Mathf.Sign(b.dst - a.dst));

                var waterShrinkFactor = 5.0f;
                var beachSize = 100.0f;
                var beachTransitionFactor = 4.0f;

                var left = dst[0].dst > i * waterShrinkFactor;
                var right = dst[0].dst > (width - i) * waterShrinkFactor;
                var top = dst[0].dst > j * waterShrinkFactor;
                var bot = dst[0].dst > (height - j) * waterShrinkFactor;
                var edge = i < width * lesserCutOff || i > width * greaterCutOff || j < height * lesserCutOff || j > height * greaterCutOff;
                var isWater = top || bot || left || right || edge;
                if (isWater)
                {
                    var biome1 = voronoiPoints[dst[0].index].biome;
                    var isSand = !edge && (
                        left ? Mathf.Abs((i * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        right ? Mathf.Abs(((width - i) * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        top ? Mathf.Abs((j * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        bot ? Mathf.Abs(((height - j) * waterShrinkFactor) - dst[0].dst) < (biome1.transitionDst + beachSize) / 2.0f :
                        throw new Exception("This is impossible."));

                    if (isSand)
                    {
                        var transitionDst =
                        left ? Mathf.Abs((i * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        right ? Mathf.Abs(((width - i) * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        top ? Mathf.Abs((j * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        bot ? Mathf.Abs(((height - j) * waterShrinkFactor) - dst[0].dst) / (biome1.transitionDst * beachTransitionFactor) :
                        throw new Exception("This is also impossible.");
                        var inTransition = transitionDst <= 1.0f;

                        map[i, j] = inTransition ? new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            color = Color.Lerp(new Color(255f / 255f, 224f / 255f, 173f / 255f), biome1.color, 1.0f - transitionDst) // sand
                        } : new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            color = new Color(255f / 255f, 224f / 255f, 173f / 255f) // water
                        };
                    }
                    else
                    {
                        map[i, j] = new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            color = new Color(0f / 255f, 191f / 255f, 255f / 255f) // water
                        };
                    }
                }
                else
                {
                    var biome1 = voronoiPoints[dst[0].index].biome;
                    var biome2 = voronoiPoints[dst[1].index].biome;

                    var transitionDst = (biome1.transitionDst + biome2.transitionDst) / 2.0f;
                    var inTransition = Mathf.Abs(dst[0].dst - dst[1].dst) < transitionDst;

                    map[i, j] = inTransition
                        ? new Cell
                        {
                            dst = new vDst[] { dst[0], dst[1] },
                            color = Color.Lerp(biome1.color, biome2.color, 0.5f - (Mathf.Abs(dst[1].dst - dst[0].dst) * 0.5f / transitionDst))
                        }
                        : new Cell
                        {
                            dst = new vDst[] { dst[0] },
                            color = biome1.color
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

    // TODO: @Max, data etc is super memory and cpu inefficien. Not an issue for smaller maps but it is for huge maps.
    public static T[,] DomainWarpMap<T>(T[,] original, int width, int height, int seed, T fallback, float warpingAmplitude = 80.0f, float adjustmentFactor = 1.2f) // NoiseSettings settings { seed, scale, ... }
    {
        // domain warping in js
        var adjustedWidth = Mathf.RoundToInt(width * adjustmentFactor);// Mathf.RoundToInt(width + warpingAmplitude);
        var adjustedHeight = Mathf.RoundToInt(height * adjustmentFactor);// Mathf.RoundToInt(height + warpingAmplitude);
        var data = new T[adjustedWidth, adjustedHeight]; //imgdata.data,
        var offset = new Vector2(-warpingAmplitude, -warpingAmplitude); // * 0.825f;

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

                data[x, y] = pattern(indexX, indexY, 25f, 3);
            }

        return data;
    }
}
