using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        var skipIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        var numVertsPerLine = meshSettings.numberOfVerticesPerLine;

        var topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2.0f;

        var meshData = new MeshData(numVertsPerLine, skipIncrement);

        var vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        var meshVertexIndex = 0;
        var outOfMeshVertexIndex = -1;

        for (var y = 0; y < numVertsPerLine; y++)
            for (var x = 0; x < numVertsPerLine; x++)
            {
                var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }

        for (var y = 0; y < numVertsPerLine; y++)
            for (var x = 0; x < numVertsPerLine; x++)
            {
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex)
                {
                    var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    var isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    var isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    var isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    var vertexIndex = vertexIndicesMap[x, y];
                    var percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    var vertexPosition2D = topLeft + (new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize);
                    var height = heightMap[x, y];

                    if (isEdgeConnectionVertex)
                    {
                        var isVertical = x == 2 || x == numVertsPerLine - 3;
                        var dstToMainVertexA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                        var dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        var dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        var ax = isVertical ? x : x - dstToMainVertexA;
                        var ay = isVertical ? y - dstToMainVertexA : y;
                        var bx = isVertical ? x : x + dstToMainVertexB;
                        var by = isVertical ? y + dstToMainVertexB : y;

                        var heightMainVertexA = heightMap[ax, ay];
                        var heightMainVertexB = heightMap[bx, by];

                        height = (heightMainVertexA * (1 - dstPercentFromAToB)) + (heightMainVertexB * dstPercentFromAToB);

                        var indexA = vertexIndicesMap[ax, ay];
                        var indexB = vertexIndicesMap[bx, by];
                        meshData.AddAveragedNormal(vertexIndex, indexA, indexB, dstPercentFromAToB);
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    var createTrianlge = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTrianlge)
                    {
                        var currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        var a = vertexIndicesMap[x, y];
                        var b = vertexIndicesMap[x + currentIncrement, y];
                        var c = vertexIndicesMap[x, y + currentIncrement];
                        var d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }

        meshData.BakeNormals();

        return meshData;
    }
}

public class MeshData
{
    readonly Vector3[] vertices;
    readonly int[] indices;
    readonly Vector2[] uvs;
    readonly Vector3[] outOfMeshVertices;
    readonly int[] outOfMeshIndices;

    Vector3[] bakedNormals;

    int indicesIndex;
    int outOfMeshTriangleIndex;
    readonly List<(int vertexIndex, int a, int b, float weightOfB)> averagedNormals;

    public MeshData(int numVertsPerLine, int skipIncrement)
    {
        var numMeshEdgeVertices = ((numVertsPerLine - 2) * 4) - 4;
        var numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        var numMainVerticesPerLine = ((numVertsPerLine - 5) / skipIncrement) + 1;
        var numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices]; // same
        uvs = new Vector2[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices]; // same
        averagedNormals = new List<(int vertexIndex, int a, int b, float weightOfB)>(numEdgeConnectionVertices);

        var numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        var numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        indices = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVertices = new Vector3[(numVertsPerLine * 4) - 4];
        outOfMeshIndices = new int[24 * (numVertsPerLine - 2)];
    }

    public void AddAveragedNormal(int vertexIndex, int a, int b, float weightOfB)
    {
        averagedNormals.Add((vertexIndex, a, b, weightOfB));
    }

    public void AddVertex(Vector3 vertex, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            outOfMeshVertices[-vertexIndex - 1] = vertex;
        }
        else
        {
            vertices[vertexIndex] = vertex;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            outOfMeshIndices[outOfMeshTriangleIndex] = a;
            outOfMeshIndices[outOfMeshTriangleIndex + 1] = b;
            outOfMeshIndices[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
        }
        else
        {
            indices[indicesIndex] = a;
            indices[indicesIndex + 1] = b;
            indices[indicesIndex + 2] = c;
            indicesIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        var vertexNormals = new Vector3[vertices.Length];

        {
            var triangleCount = indices.Length / 3;
            for (var i = 0; i < triangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = indices[normalTriangleIndex];
                var vertexIndexB = indices[normalTriangleIndex + 1];
                var vertexIndexC = indices[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        {
            var borderTriangleCount = outOfMeshIndices.Length / 3;
            for (var i = 0; i < borderTriangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = outOfMeshIndices[normalTriangleIndex];
                var vertexIndexB = outOfMeshIndices[normalTriangleIndex + 1];
                var vertexIndexC = outOfMeshIndices[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

                if (vertexIndexA >= 0)
                    vertexNormals[vertexIndexA] += triangleNormal;

                if (vertexIndexB >= 0)
                    vertexNormals[vertexIndexB] += triangleNormal;

                if (vertexIndexC >= 0)
                    vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (var i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        {
            averagedNormals.Sort((a, b) => a.vertexIndex - b.vertexIndex);
            foreach (var (vertexIndex, a, b, weightOfB) in averagedNormals)
            {
                vertexNormals[vertexIndex] = (vertexNormals[a] * (1 - weightOfB)) + (vertexNormals[b] * weightOfB);
            }
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        var pointA = indexA < 0 ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
        var pointB = indexB < 0 ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
        var pointC = indexC < 0 ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

        var sideAB = pointB - pointA;
        var sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh();
        //mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(bakedNormals);
        return mesh;
    }
}