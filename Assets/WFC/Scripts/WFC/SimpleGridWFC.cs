using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using static InitialGrid;
using static PrefabInstance;
using static RelationGrid;

public class SimpleGridWFC
{
    private WFCConfig config = new WFCConfig();
    private List<Zone> listZones = new List<Zone>();//thread safe for config (eviter de copier la config pour les thread, donc on le change pas)
    private float maxNbInGrid = 0; //Pour savoir quel est le nombre max de présence d'un asset dans la grille

    [System.Serializable]
    public class WFCConfig
    {
        public int gridSize = 15;      

        //Spawning only
        public int gridUnitSize = 1;
        public int scaleSize = 1;// ? //5 ? c'est pas pareil que unitsize ?

        public int maxLoops = 10000;
        [ReadOnly] public int nbNeighboors = 4;//not implemented
        [ReadOnly] public bool stepByStep = false;//is still working ?
        //Vector2Int sizeGrid = new Vector2Int(15, 15);//todo rectangle grid
        
        [ReadOnly] public List<UniqueTile> uniqueTilesInGrid = new List<UniqueTile>();
        [ReadOnly] public Dictionary<string, UniqueTile> hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();
        //[ReadOnly] public List<UniqueTile> uniqueGreyBlockInGrid = new List<UniqueTile>();

        public bool takeZonesIntoAccount = true;
        public bool takeInitialZones = false;
        public bool takeInitialAssets = false;
        public bool takeInitialTags = false;

        public bool generateBorders = true;

        [Header("Debug only")]
        [ReadOnly] public List<Zone> listInitialZones = new List<Zone>();//potentiellement sortir ce parametre
        [ReadOnly] public List<InitialAsset> listInitialAssets = new List<InitialAsset>();
        [ReadOnly] public List<ForceTag> listInitialTags = new List<ForceTag>();

        override
        public string ToString()
        {
            string zones = "";
            string uTiles = "";
            //string uGreyBlock = "";
            string initialAssets = "";

            foreach(Zone z in listInitialZones)
            {
                zones += z.ToString() + "\n";
            }

            foreach (InitialAsset ia in listInitialAssets)
            {
                initialAssets += ia.ToString() + "\n";
            }

            foreach (UniqueTile ut in uniqueTilesInGrid)
            {
                uTiles += ut.ToString() + "\n";
            }

            //foreach (UniqueTile ut in uniqueGreyBlockInGrid)
            //{
            //    uGreyBlock += ut.ToString() + "\n";
            //}

            return "GridSize is " + gridSize + "\n"
                    + "MaxLoops is " + maxLoops + "\n"
                    + "Number Neighboors is " + nbNeighboors + "\n"
                    + "Take zone is " + takeZonesIntoAccount + "\n"
                    + "Take initial zones is " + takeInitialZones + "\n"
                    + "Take initial assets is " + takeInitialAssets + "\n"
                    + "Generate borders is " + generateBorders + "\n\n"
                    + "* InitialZones (count: " + listInitialZones.Count + ") are :\n" + zones + "\n"
                    + "* InitialAssets (count: " + listInitialAssets.Count + ") are :\n" + initialAssets + "\n"
                    + "* Unique Tiles (count: " + uniqueTilesInGrid.Count + ") are :\n" + uTiles + "\n";
                    //+ "* Unique Grey Blocks (count: " + uniqueGreyBlockInGrid.Count + ") are :\n" + uGreyBlock + "\n";
        }
    }

    public void setListZones(List<Zone> list)//thread reference safe pour GA
    {
        this.listZones = list;
    }

    [System.Serializable]
    public class ForceTag
    {
        public Vector2Int position = Vector2Int.zero;
        public PrefabTag prefabTag = PrefabTag.None;
    }

    [System.Serializable]
    public class Zone
    {
        public Vector2Int origin = Vector2Int.zero;
        public Vector2Int size = Vector2Int.zero;
        public float probabilityBoost = 0.0f;
        public int assetID = -1; //default -1 => no impact (tile id start at 0)

        public PrefabTag prefabTag = PrefabTag.None;//if none => has no impact

        //add id for each zone ?

        public void ShowDebug()
        {
            Debug.Log(ToString());
        }

        override
        public string ToString()
        {
            return "Zone at " + origin.x + "/" + origin.y + ", size: " + size.x + "/" + size.y + ", proba:" + probabilityBoost + ", assetID:" + assetID + ", prefabTag:" + prefabTag;
        }
    }

    



    private Vector3 instanceCoordinates = new Vector3(30, 0, 0);

    private Slot[,] grid;
    private Slot candidate = null;
    private Queue<Slot> slotsToUpdate = new Queue<Slot>();
    private List<GameObject> tileInstanciated = new List<GameObject>();
    
    private int nbStepsToDo = 0;
    private int counterLoop;
    //Dictionary<UniqueTile, float> hashUTToFrequences = new Dictionary<UniqueTile, float>();//remplacer par array avec id uniquetile
    float[] frequences;//in init()

    private bool restart = false;
    private int nbRestartMax = 10;
    private int nbRestart = 0;

    //Renvoie la grille (en excluant les sous tiles des bigtiles excepté le centre)
    public Module[,] getModuleResult()
    {
        Module[,] gridResult = new Module[config.gridSize, config.gridSize];

        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                gridResult[i, j] = null;

                if (grid[i, j].availables.size != 1)
                {
                    continue;
                }

                Module m = grid[i, j].availables.data[0];
                UniqueTile ut = m.linkedTile;
                PrefabInstance pi = ut.pi;

                if (!pi.doNotShowInCreatedLevel)
                {
                    //if (ut.parent != null && !ut.parent.subpartPos[ut.id].Equals(Vector3Int.zero))
                    //{
                    //    continue;
                    //}

                    gridResult[i, j] = m;
                }
            }
        }

        return gridResult;
    }

    //0 is air (walkable), everything else 1 (blocked)

    public bool[,] getBoolPathGrid(List<int> walkables)
    {
        bool[,] gridResult = new bool[config.gridSize, config.gridSize];

        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                if (grid[i, j].availables.size != 1)
                {
                    gridResult[i, j] = false;
                    continue;
                }

                Module m = grid[i, j].availables.data[0];
                UniqueTile ut = m.linkedTile;

                if (walkables.Contains(ut.id))
                {
                    gridResult[i, j] = true;//walkable
                }
                else
                {
                    gridResult[i, j] = false;
                }
            }
        }

        return gridResult;
    }

    public int[][] getPathGrid()
    {
        int[][] gridResult = new int[config.gridSize][];

        for (int i = 0; i < config.gridSize; i++)
        {
            gridResult[i] = new int[config.gridSize];

            for (int j = 0; j < config.gridSize; j++)
            {
                if (grid[i, j].availables.size != 1)
                {
                    continue;
                }

                Module m = grid[i, j].availables.data[0];
                UniqueTile ut = m.linkedTile;
                PrefabInstance pi = ut.pi;

                if(pi.stringId == "Air")
                {
                    gridResult[i][j] = 0;
                }
                else
                {
                    gridResult[i][j] = 1;
                }
            }
        }

        return gridResult;
    }

    

    public int GetNbAssetInGrid(int assetID)
    {
        int count = 0;
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                if (grid[i, j].availables.data[0].linkedTile.id == assetID)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public Vector2Int GetPositionFirst(int assetID)
    {
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                if (grid[i, j].availables.data[0].linkedTile.id == assetID)
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    public Vector2 GetAveragePositionAssetInGrid(int assetID)
    {
        int count = 0;
        Vector2 totalPos = new Vector2(0, 0);
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                if (grid[i, j].availables.data[0].linkedTile.id == assetID)
                {
                    count++;
                    totalPos.x += i;
                    totalPos.y += j;
                }
            }
        }
        return totalPos / (float) count;
    }

    public SimpleGridWFC(WFCConfig config = null)//Constructor
    {
        if(config != null)
            this.config = config;

        if (config.takeInitialZones)//les zones ne sont pas éditées par le wfc, on peut donc les set ici
        {
            setListZones(config.listInitialZones);
        }
    }
    //public SimpleGridWFC(List<UniqueTile> uniqueTilesInGrid, Vector3 instanceCoordinates, bool generateBorder = false)//Constructor
    //{
    //    this.uniqueTilesInGrid = uniqueTilesInGrid;
    //    this.instanceCoordinates = instanceCoordinates;
    //    this.generateBorder = generateBorder;
    //}

    //public SimpleGridWFC(List<UniqueTile> uniqueTilesInGrid, bool generateBorder = false, List<Zone> listZones = null)
    //{
    //    this.uniqueTilesInGrid = uniqueTilesInGrid;
    //    if(listZones == null)
    //    {
    //        listZones = new List<Zone>();
    //    }
    //    this.listZones = listZones;
    //    this.generateBorder = generateBorder;
    //}

    public class Module//4 modules pour 4 rotations d'un meme UniqueTile
    {
        public float probability = 0f;
        public UniqueTile linkedTile = null;
        public int rotationY = 0;

        public Module(UniqueTile ut, int rotY)
        {
            linkedTile = ut;
            rotationY = rotY;
        }

        override
        public string ToString()
        {
            return "[" + linkedTile.id + "; " + rotationY + "; " + probability.ToString("F2") + "]";
        }
    }

    public class Slot
    {
        //config.nbNeighboors a prendre en compte
        public Slot[] neighboors = new Slot[4]; //if 4 => 0 = right, 1 = top, 2 = left, 3 = down


        public int x = 0;
        public int y = 0;

        public bool isCorner = false;//if selected can't delete more

        public ListOptim<Zone> zonesIn = new ListOptim<Zone>();
        //public List<Module> availables = new List<Module>();
        public ListOptim<Module> availables = new ListOptim<Module>();

        public Slot(int sizeMaxModule = 0, int sizeMaxZones = 0)
        {
            availables.Init(sizeMaxModule);
            zonesIn.Init(sizeMaxZones);
        }
    }

    override
    public string ToString()
    {
        string slots = "";
        for(int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                Slot s = grid[i, j];
                slots += "[" + i + "," + j + "] => " + "NbAvailables: " + s.availables.size + ", InNbZone: " + s.zonesIn.size + "\n";

                string modules = "";

                int lastId = -1;
                if(s.availables.size > 0)
                {
                    lastId = s.availables.data[0].linkedTile.id;
                }

                for (int k = 0; k < s.availables.size; k++)
                {
                    Module m = s.availables.data[k];

                    if (m.linkedTile.id != lastId)
                    {
                        lastId = m.linkedTile.id;
                        modules += "\n\t";
                    }

                    modules += m.ToString() + " ";

                    ////15 modules per line
                    //if((k != 0 && k % 15 == 0) || k == s.availables.size - 1)
                    //{
                    //    modules += "\n\t";
                    //}
                    //All modules for each UniqueTile per line
                    
                    if (k == s.availables.size - 1)
                    {
                        modules += "\n\t";
                    }
                }

                slots += "=====> Modules [id, rot, proba]:\n\t" + modules + "\n";
            }
        }
        return "Grid is composed of\n" + slots;
    }
    
    public void launchWFC()
    {
#if LOGGER
        Logger.Log("Start WFC", Logger.LogType.TITLE);

        Logger.Log(config.ToString());
#endif
        nbRestart = 0;
        do
        {
            restart = false;

            init();

            if(config.takeInitialAssets)
            {
                trySetInitialAssets();
            }

            if (config.takeInitialTags)
            {
                forceTagsOnGrid();
            }

#if LOGGER
            Logger.Log("Init Grid", Logger.LogType.TITLE);
            Logger.Log(ToString());

            Logger.Log("Launch compute", Logger.LogType.TITLE);
#endif
            compute();

            if(config.maxLoops == 0)
            {
                Debug.LogWarning(this + "=> Max Loops reached!");
            }

            if (restart)
            {
                nbRestart++;
                Debug.Log("Error : doing restart " + nbRestart);
            }
            
        } while (restart && nbRestart < nbRestartMax);

        if (restart)
        {
            Debug.Log("Error : no more tries but still " + nbRestart + " fails");
        }
#if LOGGER
        Logger.Log("End WFC", Logger.LogType.TITLE);
#endif
    }

    public bool isWFCFailed()
    {
        return restart;
    }

    public Vector3 offsetGeneration = new Vector3(10.0f,0,0);

    public void unShow()
    {
        foreach (GameObject g in tileInstanciated)
        {
            GameObject.Destroy(g);
        }
    }

    private bool showDebugBigTile = false;
    public void show(bool useCustomCoordinates = false, Vector3 newCoordinates = new Vector3(), Transform parent = null)
    {
        if(useCustomCoordinates)
        {
            instanceCoordinates = newCoordinates;
        }

        foreach (GameObject g in tileInstanciated)
        {
            GameObject.Destroy(g);
        }

        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                for (int z = 0; z < grid[i, j].availables.size; z++)//for (int z = 0; z < grid[i, j].availables.Count; z++)
                {
                    //tileInstanciated.Add((GameObject)GameObject.Instantiate(grid[i, j].availables.data[z].linkedTile.pi.prefab,
                    //                                        new Vector3(instanceCoordinates.x + i * gridUnitSize, z, instanceCoordinates.z + j * gridUnitSize),
                    //                                        Quaternion.Euler(0f, 90f * grid[i, j].availables.data[z].rotationY, 0f)));


                    //if parent != null => afficher que celui qui est en 0,0 (centre)

                    Module m = grid[i, j].availables.data[z];
                    UniqueTile ut = m.linkedTile;
                    PrefabInstance pi = ut.pi;

                    if (!pi.doNotShowInCreatedLevel)  {

                        bool isOrigin = true;

                        if (ut.parent != null && !ut.parent.subpartPos[ut.id].Equals(Vector3Int.zero))//le prob ici c'est que si le bigtile n'est pas en entier, il ne l'affiche pas ici alors qu'il a fail
                        {
                            if(!showDebugBigTile)
                                continue;
                            isOrigin = false;
                        }

                        GameObject go = GameObject.Instantiate(pi.prefab,
                                                            new Vector3(instanceCoordinates.x + i * config.gridUnitSize * config.scaleSize + (float)config.gridUnitSize * config.scaleSize / 2, z, instanceCoordinates.z + j * config.gridUnitSize * config.scaleSize + (float)config.gridUnitSize * config.scaleSize / 2), //+ gridUnitSize * scaleSize / 2 => repositionner au milieu de la case
                                                            Quaternion.Euler(0f, 90f * m.rotationY, 0f));

                        go.transform.parent = parent;

                        go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(config.scaleSize, config.scaleSize, config.scaleSize));
                        tileInstanciated.Add(go);


                        if(showDebugBigTile && !isOrigin && ut.parent != null && go.GetComponent<Renderer>() != null)
                        {
                            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(config.scaleSize, config.scaleSize, config.scaleSize) * 0.9f);
                            var rend = go.GetComponent<Renderer>();
                            rend.material.SetColor("_Color", Color.blue);
                        }
                    }
                    
                }
            }
        }
    }

    //private Corner()
    //{
    //    Slot emptySlot = new Slot();
    //    List<Module> availables
    //}

    private void trySetInitialAssets()
    {
        foreach(InitialAsset ia in config.listInitialAssets)
        {
            if(ia.position.x >= 0 && ia.position.x < config.gridSize
                && ia.position.y >= 0 && ia.position.y < config.gridSize)
            {
                Slot s = grid[ia.position.x, ia.position.y];

                List<Module> wanted = new List<Module>();

                for(int i = 0; i < s.availables.size; i++)
                {
                    Module current = s.availables.data[i];

                    if (((!ia.forceRotation) || (ia.forceRotation && current.rotationY == ia.rot))
                        && current.linkedTile.id == ia.id)
                    {
                        wanted.Add(current);
                    }
                }

                if(wanted.Count > 0)
                {
                    s.availables.Clear();
                    foreach(Module m in wanted)
                    {
                        s.availables.Add(m);

                        slotsToUpdate.Enqueue(grid[ia.position.x, ia.position.y]);
                    }

                    //if we limit to 1 already, then nbingrid must be updated
                    if(wanted.Count == 1)
                    {
                        frequences[wanted[0].linkedTile.id]++;
                    }
                }
                else
                {
                    Debug.LogError("InitialAssets (id:" + ia.id +", rot:" + ia.rot + ") can't be placed at " + ia.position.x + "/"+ ia.position.y);
                }
            }
        }

    }

    private void forceTagsOnGrid()
    {
        foreach (ForceTag ft in config.listInitialTags)
        {
            if (ft.position.x >= 0 && ft.position.x < config.gridSize
                && ft.position.y >= 0 && ft.position.y < config.gridSize)
            {
                Slot s = grid[ft.position.x, ft.position.y];

                List<Module> wanted = new List<Module>();

                for (int i = 0; i < s.availables.size; i++)
                {
                    Module current = s.availables.data[i];

                    if (ft.prefabTag != PrefabTag.None && ft.prefabTag == current.linkedTile.pi.prefabTag)
                    {
                        wanted.Add(current);
                    }
                }

                if (wanted.Count > 0)
                {
                    s.availables.Clear();
                    foreach (Module m in wanted)
                    {
                        s.availables.Add(m);

                        slotsToUpdate.Enqueue(s);
                    }
                }
                else
                {
                    Debug.LogError("ForceTag (tag:" + ft.prefabTag + ") can't be placed at " + ft.position.x + "/" + ft.position.y);
                }
            }
        }

    }

    private void init()
    {
        //Reset
        slotsToUpdate.Clear();
        counterLoop = config.maxLoops;
        //hashUTToFrequences.Clear();
        frequences = new float[config.uniqueTilesInGrid.Count];
        //foreach (UniqueTile ut in uniqueTilesInGrid)
        //    hashUTToFrequences[ut] = 0f;

        //Get max nbInGrid
        
        foreach(UniqueTile u in config.uniqueTilesInGrid)
        {
            if (u.nbInGrid > maxNbInGrid)
                maxNbInGrid = u.nbInGrid;
        }

        
        //Init grid module
        grid = new Slot[config.gridSize, config.gridSize];
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                grid[i, j] = new Slot(config.uniqueTilesInGrid.Count * 4, listZones.Count);

                grid[i, j].x = i;
                grid[i, j].y = j;

                //set border as the only one solution
                if(config.generateBorders && (i == 0 || j == 0 || i == config.gridSize - 1 || j == config.gridSize - 1))
                {
                    grid[i, j].availables.Add(new Module(config.uniqueTilesInGrid[0], 0));
                    grid[i, j].isCorner = true;
                    slotsToUpdate.Enqueue(grid[i, j]);
                    //possible crash en fait grid[i, j].wasSelected = true;//optimisation pour entropy
                    continue;
                }

                //create all module possibilities
                for (int k = 0; k < config.uniqueTilesInGrid.Count; k++)
                {
                    if (config.generateBorders && k == 0)//skip corner
                        continue;

                    UniqueTile ut = config.uniqueTilesInGrid[k];

                    //if (ut.pi.param.allRotationsAllowed)//create 1 rotation
                    //{
                    //    grid[i, j].availables.Add(new Module(ut, 0));
                    //}
                    //else if(ut.pi.param.symetricalAxisY)//create 2 rotations, if allrotations is true => symetric on Y is also true (no need to go here)
                    //{
                    //    grid[i, j].availables.Add(new Module(ut, 0));
                    //    grid[i, j].availables.Add(new Module(ut, 1));
                    //}
                    //else
                    {
                        for (int w = 0; w < 4; w++)//create all variants of rotation (4)
                            grid[i, j].availables.Add(new Module(ut, w));
                    }
                }
            }
        }

        //Fill module neighboors
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                grid[i, j].neighboors[0] = (i + 1 < config.gridSize) ? grid[i + 1, j] : null;
                grid[i, j].neighboors[1] = (j + 1 < config.gridSize) ? grid[i, j + 1] : null;
                grid[i, j].neighboors[2] = (i - 1 >= 0) ? grid[i - 1, j] : null;
                grid[i, j].neighboors[3] = (j - 1 >= 0) ? grid[i, j - 1] : null;


                //rajouter diagonals neighboors ici aussi (dans le bon ordre)
            }
        }


        //Add zones
        if(config.takeZonesIntoAccount == true)
        {
            //foreach (Zone zone in config.listZones)
            //{
            //    Vector3 scale = zone.transform.localScale / gridUnitSize;//Child scale == zone size
            //    Vector3 position = zone.transform.localPosition / gridUnitSize;//center of the zone

            //    for (int i = (int)(position.x) - (int)scale.x / 2; i < (int)(position.x) + (int)scale.x / 2; i++)
            //    {
            //        for (int j = (int)(position.z) - (int)scale.z / 2; j < (int)(position.z) + (int)scale.z / 2; j++)
            //        {
            //            if (i >= 0 && j >= 0 && i < config.gridSize && j < config.gridSize)
            //            {
            //                //Add zones to each gridtile
            //                Slot s = grid[i, j];
            //                s.zonesIn.Add(zone);
            //                //Pas juste ici
            //                Debug.Log("Zone detected at : " + i + " " + j + " for asset " + zone.prefabSelect);
            //            }
            //        }
            //    }
            //}
            foreach (Zone zone in listZones)
            {
                Vector2Int origin = zone.origin;
                Vector2Int size = zone.size;
                
                for (int i = origin.x; i < origin.x + size.x; i++)
                {
                    for (int j = origin.y; j < origin.y + size.y; j++)
                    {
                        if (i >= 0 && j >= 0 && i < config.gridSize && j < config.gridSize)
                        {
                            //Add zones to each gridtile
                            Slot s = grid[i, j];
                            s.zonesIn.Add(zone);
                            //Pas juste ici
                            //Debug.Log("Zone detected at : " + i + " " + j + " for asset " + zone.assetID);
                        }
                    }
                }
            }
        }
    }

    private bool searchMinEntropy()
    {
        //Debug.Log("Search min entropy");
#if LOGGER
        Logger.Log("Search Minimum Entropy", Logger.LogType.TITLE);
#endif
        //Find lowest entropy after propagation update
        //int maxTiles = gridSize * gridSize;
        float minEntropy = 0.0f;
        Slot candidateEntropy = null;

        //on pourrait skip les calculs sur les tempAvailabes == 1 non ? !!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //if (tempAvailables.size <= 1)
        //    continue;

        //normaliser tiles probabilité selon présence dans grille de base (est-ce que ca recalcule inutilement pour ceux à 1 de count, je crois que oui !!!)
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                ListOptim<Module> tempAvailables = grid[i, j].availables;
                float sumWeight = 0;

                //Sumweight
                for (int k = 0; k < tempAvailables.size; k++)
                {
                    Module m = tempAvailables.data[k];
                    if (frequences[m.linkedTile.id] < m.linkedTile.maxNb && frequences[m.linkedTile.id] >= m.linkedTile.minNb)
                        sumWeight += m.linkedTile.nbInGrid / (frequences[m.linkedTile.id] + 1);
                    else
                    {
                        if (frequences[m.linkedTile.id] >= m.linkedTile.maxNb)
                            sumWeight += 0;//0.000001f;
                        if (frequences[m.linkedTile.id] < m.linkedTile.minNb)
                            sumWeight += 10*maxNbInGrid;//0.000001f;
                    }
                        
                }

                //Normalisation by sumweight
                if (sumWeight > 0)
                {
                    for (int k = 0; k < tempAvailables.size; k++)
                    {
                        Module m = tempAvailables.data[k];
                        /*if (frequences[m.linkedTile.id] < m.linkedTile.maxNb)
                            m.probability = (m.linkedTile.nbInGrid / (frequences[m.linkedTile.id] + 1)) / sumWeight;
                        else
                            m.probability = 0;// 0.000001f / sumWeight;*/

                        if (frequences[m.linkedTile.id] < m.linkedTile.maxNb && frequences[m.linkedTile.id] >= m.linkedTile.minNb)
                            m.probability = (m.linkedTile.nbInGrid / (frequences[m.linkedTile.id] + 1)) / sumWeight;
                        else
                        {
                            if (frequences[m.linkedTile.id] >= m.linkedTile.maxNb)
                                m.probability = 0;// 0.000001f / sumWeight;
                            if (frequences[m.linkedTile.id] < m.linkedTile.minNb)
                                m.probability = (10 * maxNbInGrid) / sumWeight;//0.000001f;
                        }
                    }
                }
            }
        }

        //Zones (à déplacer dans la boucle en haut pour éviter 2x loop)

        //V1 Boost proba
        //if (takeZones)
        //{
        //    for (int i = 0; i < gridSize; i++)
        //    {
        //        for (int j = 0; j < gridSize; j++)
        //        {
        //            ListOptim<Zone> tempZones = grid[i, j].zonesIn;
        //            ListOptim<Module> tempAvailables = grid[i, j].availables;

        //            float sumWeight = tempAvailables.size;
        //            float newSumWeight = 0f;

        //            for (int k = 0; k < tempAvailables.size; k++)
        //            {
        //                Module m = tempAvailables.data[k];
        //                m.probability = 1.0f / sumWeight;//par défaut

        //                for (int l = 0; l < tempZones.size; l++)
        //                {
        //                    // /!\ on test le boost de % en premier sans faire la répartition (voir 1 de la feuille)
        //                    Zone z = tempZones.data[l];
        //                    if (z.prefabSelect.Equals(m.linkedTile.pi.stringId))
        //                    {
        //                        m.probability *= 1.0f + z.probability;
        //                        // m.probability = (z.probability / 4.0f) / (float)tempZones.size;
        //                    }
        //                }

        //                newSumWeight += m.probability;
        //            }

        //            //renormalise sur total de 1 par slot
        //            for (int k = 0; k < tempAvailables.size; k++)
        //            {
        //                Module m = tempAvailables.data[k];
        //                m.probability = m.probability / newSumWeight;
        //            }
        //        }
        //    }
        //}

        //V2 Repartition proba
        if (config.takeZonesIntoAccount)
        {
            for (int i = 0; i < config.gridSize; i++)
            {
                for (int j = 0; j < config.gridSize; j++)
                {
                    ListOptim<Zone> tempZones = grid[i, j].zonesIn;
                    ListOptim<Module> tempAvailables = grid[i, j].availables;

                    if(tempZones.size == 0)
                    {
                        continue;
                    }

                    float sumWeight = 0f;

                    for (int k = 0; k < tempAvailables.size; k++)
                    {
                        Module m = tempAvailables.data[k];

                        if (frequences[m.linkedTile.id] >= m.linkedTile.maxNb)
                        {
                            continue;
                        }
                        
                        //keep the one computed just before
                        //m.probability = m.linkedTile.nbInGrid;//voir pour améliorer ici en fonction de la frequence de spawn

                        for (int l = 0; l < tempZones.size; l++)
                        {
                            Zone z = tempZones.data[l];
                            //if (z.prefabSelect.Equals(m.linkedTile.pi.stringId))
                            if (z.assetID == m.linkedTile.id || (z.prefabTag != PrefabTag.None && z.prefabTag == m.linkedTile.pi.prefabTag))
                            {
                                m.probability = z.probabilityBoost;
                                //m.probability *= z.probabilityBoost;

                                //scale la valeur en fonction de la frequence d'apparition, car un boost x2000 pour obtenir 0.9 pour 1 choix / 5 n'a pas de sens
                            }
                        }

                        sumWeight += m.probability;
                    }

                    //renormalise sur total de 1 par slot
                    for (int k = 0; k < tempAvailables.size; k++)
                    {
                        Module m = tempAvailables.data[k];
                        m.probability = m.probability / sumWeight;
                    }
                }
            }
        }






        /* for (int i = 0; i < gridSize; i++)
        {
                for (int j = 0; j < gridSize; j++)
                {
                    //foreach zones
                    //if zones prefab/categorie == grid[].prefab
                    //zones = poids pas proba, entre 0 et 1, default 0.5 ?, on garde une proba de base de 1 on selon presence initiale ?
                    //renormaliser avec la nouvelle somme des proba * poids
                }
        }*/

        //compute entropies and update lowest
        List<Slot> candidatesWithMinEntropy = new List<Slot>();
        for (int i = 0; i < config.gridSize; i++)
        {
            for (int j = 0; j < config.gridSize; j++)
            {
                //List<Module> tempAvailables = grid[i, j].availables;

                ListOptim<Module> tempAvailables = grid[i, j].availables;

                //if (tempAvailables.Count <= 1)
                //    continue;


                if (tempAvailables.size <= 1)//mettre ça aussi en haut non ????? eviter de recalculer proba sur une case avec un seul choix
                    continue;



                //if (tempAvailables.size <= 1)//if(grid[i, j].wasSelected)////on skip ici ceux qui ont déjà ete "choisi", il faut calculer entropie pour ceux a qui il reste un choix
                //    continue;

                float entropy = 0.0f;



                //foreach (Module m in tempAvailables)
                //{
                //    entropy += m.probability * Mathf.Log(m.probability);
                //}

                for (int k = 0; k < tempAvailables.size; k++)
                {
                    Module m = tempAvailables.data[k];
                    entropy += m.probability * Mathf.Log(m.probability);//Mathf.Abs(0.5f - m.probability);//Mathf.Abs(0.5f - m.probability);//m.probability * Mathf.Log(m.probability);// Mathf.Abs(0.5f - m.probability);//;//ou Sum de abs(0.5 - p)
                }

                entropy *= -1;

                if (candidatesWithMinEntropy.Count == 0 || entropy < minEntropy - 0.01)
                {
                    minEntropy = entropy;
                    candidatesWithMinEntropy.Clear();
                    candidatesWithMinEntropy.Add(grid[i, j]);
                }
                else
                {
                    if (entropy <= minEntropy + 0.01 && entropy >= minEntropy - 0.01)
                        candidatesWithMinEntropy.Add(grid[i, j]);
                }
            }
        }

        if (candidatesWithMinEntropy.Count <= 0)//plus de choix possible
            return false;

        //System.Random r = new System.Random();
        candidateEntropy = candidatesWithMinEntropy[RandomUtility.NextInt(0, candidatesWithMinEntropy.Count)];
#if LOGGER
        Logger.Log("Slot selected at " + candidateEntropy.x + "/"+ candidateEntropy.y);
        Logger.Log("======> NbAvailables:" + candidateEntropy.availables.size);
#endif
        //not working thread (ga algorithm)
        //candidateEntropy = candidatesWithMinEntropy[Random.Range(0, candidatesWithMinEntropy.Count)];

        //Choose a module in the slot candidate based on his probability
        Module chosenModule = null;

        //float random = Random.Range(0.0f, 1.0f);
        float random = (float) RandomUtility.NextDouble();



        //foreach (Module m in candidateEntropy.availables)
        //{
        //    random -= m.probability;
        //    if (random <= 0)
        //    {
        //        chosenModule = m;
        //        break;
        //    }
        //}

        for (int k = 0; k < candidateEntropy.availables.size; k++)
        {
            Module m = candidateEntropy.availables.data[k];

            random -= m.probability;
            if (random <= 0)
            {
                chosenModule = m;
                break;
            }
        }

#if LOGGER
        Logger.Log("Chosen module is" + chosenModule.ToString());
#endif

        //is big tile (simplify propagation) => NOT WORKING BECAUSE OF ROTATIONS
        //if(chosenModule.linkedTile.parent != null && false)
        //{
        //    BigTile parent = chosenModule.linkedTile.parent;
        //    //Vector3Int currentOffset = parent.subpartPos[chosenModule.linkedTile.id];
        //    int currentX = candidateEntropy.x;
        //    int currentY = candidateEntropy.y;
        //    Vector3Int currentPosUT = new Vector3Int(currentX, 0, currentY);

        //    int counterNeighboorsOK = 0;

        //    foreach (KeyValuePair<int, Vector3Int> e in parent.subpartPos)
        //    {
        //        //skip current
        //        if (e.Key == chosenModule.linkedTile.id)
        //        {
        //            continue;
        //        }

        //        Vector3Int currentOffset = e.Value;
        //        Vector3Int currentPosition = currentOffset + currentPosUT;

        //        if (currentPosition.x < gridSize || currentPosition.x >= 0
        //            || currentPosition.z < gridSize || currentPosition.z >= 0)
        //        {
        //            Slot currentSlot = grid[currentPosition.x, currentPosition.z];

        //            for (int k = 0; k < currentSlot.availables.size; k++)
        //            {
        //                Module m = candidateEntropy.availables.data[k];
        //                if(m.linkedTile.id == e.Key)
        //                {
        //                    //Auto collapse
        //                    frequences[e.Key]++;

        //                    currentSlot.availables.Clear();
        //                    currentSlot.availables.Add(m);

        //                    //Add to update list
        //                    slotsToUpdate.Enqueue(currentSlot);

        //                    counterNeighboorsOK++;
        //                    break;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Debug.Log("ERROR bigtile off limits");
        //        }

        //    }

        //    if(counterNeighboorsOK != parent.subpartPos.Count - 1)
        //    {
        //        Debug.Log("ERROR some neighboors can't spawn selected bigtile");
        //    }
        //}


        if (frequences[chosenModule.linkedTile.id] >= chosenModule.linkedTile.maxNb)
        {
            Debug.Log("B O U M too much- > " + chosenModule.linkedTile.pi.stringId);

        }

        //on compte (plus on choisit meme tile, moins normalement on la reprend)
        //hashUTToFrequences[chosenModule.linkedTile]++;
        frequences[chosenModule.linkedTile.id]++;
        
        candidateEntropy.availables.Clear();
        candidateEntropy.availables.Add(chosenModule);
        //candidateEntropy.wasSelected = true;

        candidate = candidateEntropy;//useless variable
        slotsToUpdate.Enqueue(candidate);

        return true;
    }

    private void compute()
    {

        bool[] changedNeighboors = new bool[config.nbNeighboors];//by default bool is false

        while (counterLoop-- > 0 && restart == false)
        {
            //Continuer la premiere boucle sans le search min car on doit update tous les voisins des borders

            //Start counter loop at 0
#if LOGGER
            Logger.Log("Loop n°" + (config.maxLoops - 1 - counterLoop), Logger.LogType.TITLE);
#endif

            if (config.generateBorders && counterLoop == config.maxLoops - 1)//force update borders to set neighboors directly before a searchminentropy (first loop)
            {
                // en fait pour que corner marche, il faut que les bords interieur de la map s'update selon les border bloc qui sont la seule possibilité
                // dans l'autre sens c'est border qui tombe a 0
#if LOGGER
                Logger.Log("First loop, borders activated, searchEntropy is skipped", Logger.LogType.TITLE);
#endif
                Debug.Log("number of first border slots to update " + slotsToUpdate.Count);
            }
            else
            {
                if (!searchMinEntropy())//A prendre en compte meme pour le 1er choix (y a aura les frequences avec) 
                {
                    Debug.Log("Entropy ended");
                    break;
                }
            }

            

            nbStepsToDo--;
            
            while (slotsToUpdate.Count > 0 && restart == false)
            {
                Slot currentSlot = slotsToUpdate.Dequeue();

                //List<Module> currentAvailables = currentSlot.availables;
                ListOptim<Module> currentAvailables = currentSlot.availables;

                Slot[] currentNeighboors = currentSlot.neighboors;

                for(int i = 0; i < changedNeighboors.Length; i++)
                    changedNeighboors[i] = false;

                //for(int i = 0; i < changedNeighboors.Length; i++)
                //{
                //    changedNeighboors[i] = false;
                //}

                for (int i = 0; i < config.nbNeighboors; i++)//check the 4 neighboors
                {
                    if (currentNeighboors[i] != null)
                    {
                        for (int j = 0; j < currentNeighboors[i].availables.size; j++)//for (int j = 0; j < currentNeighboors[i].availables.Count; j++)//il se passe quoi si le neighboor n'a plus qu'une possibilite, ca reverifie en trop => dans le cas où il a été collapsé auparavant (à opti pour eviter ça), car si se sont ses choix qui sont suppr, il se peut qu'il reste aucun (erreur de contraintes)
                        {
                            Module moduleN = currentNeighboors[i].availables.data[j];

                            Relation[] currentRelations = moduleN.linkedTile.relations;

                            bool authorizedTile = false;

                            for(int l = 0; l < currentRelations.Length; l++)//foreach (Relation r in currentRelations)
                            {
                                Relation r = currentRelations[l];

                                //int size = currentAvailables.getSize();
                                for (int k = 0; k < currentAvailables.size; k++)//foreach (Module moduleC in currentAvailables)
                                {
                                    Module moduleC = currentAvailables.data[k];
                                    if (moduleC.linkedTile.id != r.to.id)//if (moduleC.linkedTile.pi.prefab != r.to.pi.prefab)//prendre en compte que quand prefab relation == prefab available
                                        continue;

                                    if(BinaryUtility.isRelationOK(r.autorization, (i + config.nbNeighboors / 2) % config.nbNeighboors, moduleN.rotationY, i, moduleC.rotationY))//check relation avec le voisin opposé
                                    {
                                        authorizedTile = true;
                                        break;
                                    }
                                    //si il devient true à un seul moment je peux sortir de la boucle et skip le reste
                                }
                                if (authorizedTile)
                                    break;
                            }

                            if (!authorizedTile)
                            {
                                changedNeighboors[i] = true;
                                currentNeighboors[i].availables.Remove(moduleN);
                                j--;

                                if (currentNeighboors[i].availables.size == 0)
                                {
                                    Debug.Log("Constraints error");
                                    restart = true;
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < config.nbNeighboors; i++)
                {
                    if (changedNeighboors[i] && currentNeighboors[i].availables.size > 0)//if (changedNeighboors[i] && currentNeighboors[i].availables.Count > 0)
                    {
                        slotsToUpdate.Enqueue(currentNeighboors[i]);
                    }
                }
            }
#if LOGGER
            Logger.Log("Propagation Ended", Logger.LogType.TITLE);
            Logger.Log(ToString());
#endif
        }
    }
}
