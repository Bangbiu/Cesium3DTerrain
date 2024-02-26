using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenScript : MonoBehaviour
{

    public int TERR_WIDTH = 200;
    public int TERR_HEIGHT = 200;
    public float TERR_SCALE = 0.1f;
    public float ORIGIN_OFFSET = 0.5f;

    public float heightScale {
        get => _heightScale;
        set
        {
            _heightScale = value;
            Build();
        }
    }

    public int curMapIndex {
        get => _curMapIndex;
        set
        {
            _curMapIndex = value;
            LoadMap(value);
        }
    }

    public GameObject PinpointTipPrefab;
    public GameObject TracePointPrefab;
    public GameObject uiObject;

    public static int MAP_SIZE = 512;

    [SerializeField]
    public TextAsset[] HEIGHT_MAPS;

    private int[,] currentMap;

    private float OFFSET_X;
    private float OFFSET_Z; 
    private int _curMapIndex = 0;
    private float _heightScale = 0.01f;

    private GameObject[] pinpoints;
    private List<GameObject> tracePoints;
    private UIScript ui;

    // Start is called before the first frame update
    void Start()
    {
        InitHints();
        InitUI();
        LoadMap(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void LoadMap(int mapIndex) {
        
        _curMapIndex = mapIndex;

        TextAsset cur_asset = HEIGHT_MAPS[mapIndex];
        Stream stream = new MemoryStream(cur_asset.bytes);
        BinaryReader br = new BinaryReader(stream);

        currentMap = new int[MAP_SIZE, MAP_SIZE];
        for (int z = 0; z < MAP_SIZE; z++) {
            for (int x = 0; x < MAP_SIZE; x++) {
                currentMap[x, z] = (int)br.ReadByte();
            }
        }

        Build();
    }

    public void Build() {
        OFFSET_X = TERR_WIDTH * TERR_SCALE * ORIGIN_OFFSET;
        OFFSET_Z = TERR_HEIGHT * TERR_SCALE * ORIGIN_OFFSET; 
        
        Mesh terrain = GenerateTerrain();
        GetComponent<MeshFilter>().mesh = terrain;
        GetComponent<MeshCollider>().sharedMesh = terrain;
        
        // Refresh Pinpoint Position
        for (int i = 0; i < pinpoints.Length; i++) {
            if (pinpoints[i].activeSelf) {
                TipScript ts = pinpoints[i].GetComponent<TipScript>();
                Pinpoint(ts.gridPos, i);
            }
        }

        if (pinpoints[0].activeSelf && pinpoints[1].activeSelf) {
            ShowSurfacePath();
        }
    }

    public void InitHints() {
        pinpoints = new GameObject[2];
        pinpoints[0] = Instantiate(PinpointTipPrefab) as GameObject;
        pinpoints[1] = Instantiate(PinpointTipPrefab) as GameObject;

        pinpoints[0].SetActive(false);
        pinpoints[1].SetActive(false);

        tracePoints = new List<GameObject>();
    }

    public void InitUI() {
        ui = uiObject.GetComponent<UIScript>();
    }

    public GameObject Pinpoint(Vector3 point) {
        
        GameObject pinpoint = pinpoints[ui.pickingIndex];
        pinpoint.SetActive(true);
        
        TipScript ts = pinpoint.GetComponent<TipScript>();

        Vector2Int gridPos = GetGridPos(point);
        Vector3 actPos = GetActualPos(gridPos.x, gridPos.y);

        ts.Tip(point, gridPos, actPos.y + " m");

        // Change User Input Coordinate
        ui.setInputPosition(gridPos.x, gridPos.y);

        //Calc Path
        if (pinpoints[0].activeSelf && pinpoints[1].activeSelf) {
            ShowSurfacePath();
        }

        return pinpoint;

    } 

    public GameObject Pinpoint(Vector2Int gridPos, int targetIndex) {

        GameObject pinpoint = pinpoints[targetIndex];
        if (gridPos.x >= MAP_SIZE || gridPos.y >= MAP_SIZE || gridPos.x < 0 || gridPos.y < 0) {
            return pinpoint;
        }

        pinpoint.SetActive(true);

        TipScript ts = pinpoint.GetComponent<TipScript>();
        Vector3 actPos = GetActualPos(gridPos.x, gridPos.y);
        Vector3 worldPos = GetMeshVertPos(gridPos);
        ts.Tip(worldPos, gridPos, actPos.y + " m");

        //Calc Path
        if (pinpoints[0].activeSelf && pinpoints[1].activeSelf) {
            ShowSurfacePath();
        }

        return pinpoint;

    }

    private Vector2 GridToMeshGrid(float gridX, float gridY) {
        return new Vector2(gridX / MAP_SIZE * TERR_WIDTH  * TERR_SCALE - OFFSET_X,
                            gridY / MAP_SIZE * TERR_HEIGHT  * TERR_SCALE - OFFSET_Z);
    }

    private Vector3 GetMeshVertPos(Vector2Int gridPos) {
        return GetMeshVertPos((float)gridPos.x / MAP_SIZE * TERR_WIDTH, (float)gridPos.y / MAP_SIZE * TERR_HEIGHT);
    }

    private Vector3 GetMeshVertPos(float wx, float wz) {
        float height = currentMap[(int) (wx / TERR_WIDTH * MAP_SIZE), 
                                    (int) (wz / TERR_HEIGHT * MAP_SIZE)] * _heightScale; 
                                    //Random.Range(0, 2.0f);
        return new Vector3(wx * TERR_SCALE - OFFSET_X, height, wz * TERR_SCALE - OFFSET_Z);
    }

    private Vector2Int GetGridPos(Vector3 worldPos) {
        return new Vector2Int((int) Mathf.Round((worldPos.x + OFFSET_X) / TERR_SCALE / TERR_WIDTH * MAP_SIZE ), 
                                (int) Mathf.Round((worldPos.z + OFFSET_Z) / TERR_SCALE / TERR_HEIGHT * MAP_SIZE));
    }

    private Vector3 GetActualPos(int x, int z) {
        return GetActualPos(x, currentMap[x, z], z);
    }

    private Vector3 GetActualPos(float x, float y, float z) {
        // The heightmap has a spatial resolution of 30 meters per pixel and 11 meters per height value.
        return new Vector3(x * 30f,  y* 11f, z * 30f);
    }

    private Mesh GenerateTerrain() {

        Mesh terrain = new Mesh();
        
        Vector3[] vertices = new Vector3[TERR_WIDTH * TERR_HEIGHT];
        Vector2[] uv = new Vector2[TERR_WIDTH * TERR_HEIGHT];

        int TRIANGLE_VERT_COUNT = TERR_WIDTH * TERR_HEIGHT * 6;
        int[] triangles = new int[TRIANGLE_VERT_COUNT];

        for (int z = 0; z < TERR_HEIGHT; z++) {
            for (int x = 0; x < TERR_WIDTH; x++) {
                vertices[TERR_WIDTH * z + x] = GetMeshVertPos((float)x, (float)z);
            }
        }


        int curr_tri_index = 0;
        for (int z = 0; z < TERR_HEIGHT - 1; z++) {
            for (int x = 0; x < TERR_WIDTH - 1; x++) {
                int curr_vert_index = TERR_WIDTH * z + x;

                triangles[curr_tri_index + 0] = curr_vert_index;
                triangles[curr_tri_index + 1] = curr_vert_index + TERR_WIDTH;
                triangles[curr_tri_index + 2] = curr_vert_index + 1;
                triangles[curr_tri_index + 3] = curr_vert_index + 1;
                triangles[curr_tri_index + 4] = curr_vert_index + TERR_WIDTH;
                triangles[curr_tri_index + 5] = curr_vert_index + TERR_WIDTH + 1;

                curr_tri_index += 6;
            }
        }

        terrain.vertices = vertices;
        terrain.uv = uv;
        terrain.triangles = triangles;

        terrain.RecalculateNormals();

        return terrain;
    }

    /**

     * Deprecated - Connecting interpolated points inside triangles can only estimate distance
     * while we want to calculate exact distance across the surface of triangles
     * 
     * Helper Function that Interpolate 3D Coordinate Between Grid Vertices

     * Since we treated the terrain surface as a triangular grid derived from the height map.
     * -- The Quad is split from TOP_RIGHT to BOTTOM_LEFT
     * We can linearly interpolate height between grid vertices using point coordinate inside triangle.

     * @param pos position on the Grid
     * @returns interpolated Height of given position

    */

    private float InterpolateHeight(Vector2 pos) {
        Vector2Int topLeftVertPos = new Vector2Int((int) pos.x, (int) pos.y);
        Vector2 posInCell = pos - topLeftVertPos;

        // Four Vertices in the Quad
        
        // Top Left
        Vector3 v1 = new Vector3(0, currentMap[topLeftVertPos.x, topLeftVertPos.y], 0);
        
        // Top Right
        Vector3 v2 = new Vector3(1, currentMap[topLeftVertPos.x + 1, topLeftVertPos.y],0);

        // Bottom Left
        Vector3 v3 = new Vector3(0, currentMap[topLeftVertPos.x, topLeftVertPos.y + 1], 1);

        // Bottom Right
        Vector3 v4 = new Vector3(1, currentMap[topLeftVertPos.x + 1, topLeftVertPos.y + 1], 1);

        // Inside Top Left Triangle
        Vector3[] vs = {v1, v2, v3};

        if (posInCell.y < posInCell.x) {
            // Inside Bottom Right Triangle
            vs[0] = v2;
            vs[1] = v3;
            vs[2] = v4;
        }

        // Calculate barycentric coordinates
        float denom = (vs[1].z - vs[2].z) * (vs[0].x - vs[2].x) + (vs[2].x - vs[1].x) * (vs[0].z - vs[2].z);
        float u = ((vs[1].z - vs[2].z) * (posInCell.x - vs[2].x) + (vs[2].x - vs[1].x) * (posInCell.y - vs[2].z)) / denom;
        float v = ((vs[2].z - vs[0].z) * (posInCell.x - vs[2].x) + (vs[0].x - vs[2].x) * (posInCell.y - vs[2].z)) / denom;
        float w = 1 - u - v;

        // Interpolation
        return u * vs[0].y + v * vs[1].y + w * vs[2].y;
    }


    /** 
     * Helper Function that Interpolate Height on The Grid Line

     * There are three types of grid lines on the surface:
     * 1. Horizontal Grid Line
     * 2. Vertical Grid Line
     * 3. Diagonal Grid Line

     * For each grid line, the function uses different vertices to interpolate: (x,y) refer to top left vertex
     * 1. (x, y) - (x + 1, y)
     * 2. (x, y) - (x, y + 1)
     * 3. (x + 1, y) - (x, y + 1)

     * @param gridX x coordinate on Grid
     * @param gridY y coordinate on Grid
     *      - mode of interpolation: 0-Horizontal, 1-Veritcal, 2-Diagonal
     
     * @returns interpolated Height of given position
        
    */
    private float InterpHeightOnGrid(float gridX, float gridY) {
        Vector2Int topLeftVertPos = new Vector2Int((int) gridX, (int) gridY);
        Vector2 posInCell = new Vector2(gridX - topLeftVertPos.x, gridY - topLeftVertPos.y);

        int mode = 0;
        if (posInCell.y == 0f && posInCell.x == 0f) {
            return currentMap[topLeftVertPos.x, topLeftVertPos.y];
        }
        
        if (posInCell.y == 0f)  {
            mode = 0;
        } else if (posInCell.x == 0f) {
            mode = 1;
        } else {
            mode = 2;
        }

        float heightStart, heightEnd;
        if (mode == 0) {
            heightStart = currentMap[topLeftVertPos.x, topLeftVertPos.y];
            heightEnd = currentMap[topLeftVertPos.x + 1, topLeftVertPos.y];
            return heightStart + posInCell.x * (heightEnd - heightStart);
        } else if (mode == 1) {
            heightStart = currentMap[topLeftVertPos.x, topLeftVertPos.y];
            heightEnd = currentMap[topLeftVertPos.x, topLeftVertPos.y + 1];
            return heightStart + posInCell.y * (heightEnd - heightStart);
        } else if (mode == 2) {
            //Debug.Log(topLeftVertPos.x + "," + (topLeftVertPos.y + 1));
            heightStart = currentMap[topLeftVertPos.x, topLeftVertPos.y + 1];
            heightEnd = currentMap[topLeftVertPos.x + 1, topLeftVertPos.y];
            return heightStart + posInCell.x * (heightEnd - heightStart);
        } else {
            return -1f;
        }
        
    }

    /**
        Render The Path between Two Tracked Points
    */
    public void ShowSurfacePath() {

        foreach(GameObject tp in tracePoints) {
            Destroy(tp);
        }
        tracePoints.Clear();

        TipScript ts1 = pinpoints[0].GetComponent<TipScript>();
        TipScript ts2 = pinpoints[1].GetComponent<TipScript>();

        Debug.Log("ts1:" + ts1.gridPos + ", ts2:" + ts2.gridPos);


        List<Vector2> keypoints = GenerateSurfacePath(ts1.gridPos, ts2.gridPos);
        List<Vector3> worldPoses = new List<Vector3>();
        List<Vector3> actualPoses = new List<Vector3>();

        foreach(Vector2 kp in keypoints) {
            Vector2 worldGridPos = GridToMeshGrid(kp.x, kp.y);
            float height = InterpHeightOnGrid(kp.x, kp.y);
            worldPoses.Add(new Vector3(worldGridPos.x, height * heightScale, worldGridPos.y));
            actualPoses.Add(GetActualPos(kp.x, height, kp.y));
        }


        foreach(Vector3 pos in worldPoses) {
            GameObject tp = Instantiate(TracePointPrefab) as GameObject;
            tp.transform.position = pos;
            tracePoints.Add(tp);
        }

        
        float totalLength = 0f;

        for (int i = 0; i < actualPoses.Count - 1; i++) {
            totalLength += (actualPoses[i] - actualPoses[i + 1]).magnitude;
        }

        ui.setCalcResult(totalLength);
    }

    /**
     * Generate Surface Path Keypoints Between Two Points
     * @param pointA target 1 Coordinate on Grid 
     * @param pointB target 2 Coordinate on Grid 
     * @returns list of coordinates of Keypoints on Grid
     */
    public List<Vector2> GenerateSurfacePath(Vector2Int pointA, Vector2Int pointB) {
        List<Vector2> keypoints = new List<Vector2>();

        if (pointA == pointB) {
            keypoints.Add(new Vector2(0, 0));
            keypoints.Add(new Vector2(0, 0));
            return keypoints;
        }

        // y = slope * x + pb;
        float dy = pointB.y - pointA.y;
        float dx = pointB.x - pointA.x; // Maybe Small Amount to avoid Infinity
        float slope = dy / dx;
        float pb = - slope * pointB.x + pointB.y;
        Debug.Log("slope: " + slope);

        if (dx == 0f) {
            int minY = Mathf.Min(pointA.y, pointB.y);
            int maxY = Mathf.Max(pointA.y, pointB.y);
            for (int gy = minY; gy <= maxY; gy++) {
                keypoints.Add(new Vector2(pointA.x, gy));
            }
            return keypoints;
        }

        // Diagnal Intersections: y = -x + gb (point.x + point.y)
        int gb1 = pointA.x + pointA.y;
        int gb2 = pointB.x + pointB.y;

        int gbStart = Mathf.Min(gb1, gb2) + 1;
        int gbEnd = Mathf.Max(gb1, gb2);

        
        for (int gb = gbStart; gb < gbEnd; gb ++) {
            float isectX = (gb - pb) / (slope + 1f);
            float isectY = -isectX + gb;
            
            if (float.IsNaN(isectX) || float.IsNaN(isectY)) {
                continue;
            }
            keypoints.Add(new Vector2(isectX, isectY));
        }

        // Orthogonal Intersections
        int startX = Mathf.Min(pointA.x, pointB.x) + 1;
        int EndX = Mathf.Max(pointA.x, pointB.x);

        // Vertical Grid Line: x = b
        for (int lx = startX; lx < EndX; lx++) {
            float isectY = slope * lx + pb;
            float isectX = (float) lx;

            if (float.IsNaN(isectX) || float.IsNaN(isectY)) {
                continue;
            }

            keypoints.Add(new Vector2(isectX, isectY));
        }

        // Vertical Grid Line: y = b
        int startY = Mathf.Min(pointA.y, pointB.y) + 1;
        int EndY = Mathf.Max(pointA.y, pointB.y);
        
        for (int ly = startY; ly < EndY; ly++) {
            float isectY = (float) ly;
            float isectX = (ly - pb) / slope;

            if (float.IsNaN(isectX) || float.IsNaN(isectY)) {
                continue;
            }

            keypoints.Add(new Vector2(isectX, isectY));
            
        }

        // Add Two Starting Points To the Path
        keypoints.Add(new Vector2(pointA.x, pointA.y));
        keypoints.Add(new Vector2(pointB.x, pointB.y));

        // Sort keypoints using one of the component
        keypoints.Sort((a, b) => a.x < b.x ? -1 : 1);

        return keypoints;
    }
}
