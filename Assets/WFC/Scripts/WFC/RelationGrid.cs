//using Boo.Lang;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PrefabInstance;

public class RelationGrid : MonoBehaviour
{
    public class GridTile //chaque case de la grille de départ
    {
        public UniqueTile linkedTile = null;
        public int rotationGridY = 0;

        public TileParameters param = null;


        //public GameObject linkedChild = null;
        //public List<Zone> zoneIn = new List<Zone>();
    }

    [Serializable]
    public class UniqueTile //unique pour chaque asset (sans prendre les rotations en compte)
    {
        static int idCounter = 0;
        public int id = 0;
        public Relation[] relations;
        public float nbInBaseGrid = 0;
        public float maxNb = 0;
        public float minNb = 0;
        public PrefabInstance pi = null;//access parameters (prefab, rotation, symetrical)

        [HideInInspector]
        public float NavHeightZPosRot0 = 0;
        [HideInInspector]
        public float NavHeightZNegRot0 = 0;
        [HideInInspector]
        public float NavHeightXPosRot0 = 0;
        [HideInInspector]
        public float NavHeightXNegRot0 = 0;

        public UniqueTile(PrefabInstance prefInst)
        {
            id = idCounter++;//commence a 0
            pi = prefInst;
        }

        public static void ResetId()
        {
            idCounter = 0;
        }

        public static int getLastId()
        {
            return idCounter - 1;
        }

        //TODO Eviter trop de ref donc copier ici :
        //Prefab
        //TileParameters

        public BigTile parent = null;//only set if is subpart of a bigtile, ou plutot dans gridtile ?

        override
        public string ToString()
        {
            return id + " is " + pi.stringId + ", nbInGrid: " + nbInBaseGrid + ", " + ((parent == null) ? "NoBigtile" : "BigTile => SubTile at " + parent.subpartPos[id].x+"/"+ parent.subpartPos[id].z);
        }
    }

    public class BigTile
    {
        //use vector2int instead (we don't use 3d)
        public Dictionary<int, Vector3Int> subpartPos = new Dictionary<int, Vector3Int>();
    }
    
    public class Relation
    {
        public UniqueTile to; //reference towards relation
        public uint autorization;
    }

    public int gridSize = 10;
    public int gridUnitSize = 1;
    //public bool incrementNbInGrid = true;
    //public bool fillWithAir = false;
    //public bool takeBorderIntoAccount = false;//create relation between "null" (around the grid) and neighboor tile
    public GameObject prefabAir = null;
    
    public bool RangeLesAssets = true;
    public float DistanceMinRangeAsset = 0.1f;
    public Material ErrorMat;

    

    GridTile[,] grid;


    //OLD VERSION
    //todo a mettre dans wfcmanager pour plusieurs sous-grilles
    //List<UniqueTile> uniqueTilesInGrid = new List<UniqueTile>();
    //Dictionary<string, UniqueTile> hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();

    //public List<UniqueTile> extractTilesFromGrid()
    //{
    //    //multi grille a changer ici aussi
    //    UniqueTile.ResetId();

    //    initGrid();

    //    //!!!!!! si multi grille, ne pas rajouter (todo)
    //    if (takeBorderIntoAccount)//si takeBorder => alors il s'agit du premier bloc UT
    //    {
    //        UniqueTile cornerUT = new UniqueTile();
    //        cornerUT.pi = prefabCorner.GetComponent<PrefabInstance>();
    //        cornerUT.nbInGrid = gridSize;
    //        uniqueTilesInGrid.Add(cornerUT);
    //    }

    //    ScanGrid();

    //    //ScanZone();

    //    if (fillWithAir && prefabAir != null)
    //        FillAir();

    //    CreateRelations();

    //    return uniqueTilesInGrid;
    //}

    //MULTIGRILLE VERSION

    [ReadOnly, Tooltip("Debug : pour voir les tiles extraits et leur paramètres. Partagé entre tout les grid utilisées par le meme WFC")] 
      public List<UniqueTile> uniqueTilesInGrid; //Partagée entre toute les grilles
    private Dictionary<string, UniqueTile> hashPrefabToUniqueTile;
    public void extractTilesFromGrid(ref List<UniqueTile> uniqueTilesInGrid, ref Dictionary<string, UniqueTile> hashPrefabToUniqueTile, int nbTimesToAdd = 1)//, bool takeBorderIntoAccount)
    {
        //set parameters
        this.uniqueTilesInGrid = uniqueTilesInGrid;
        this.hashPrefabToUniqueTile = hashPrefabToUniqueTile;

        initGrid();

        ScanGrid(nbTimesToAdd);
        /*if (fillWithAir && prefabAir != null)
            FillAir();*/

        CreateRelations();
    }

    public void ScanAndFillAirEditor()
    {
        if (prefabAir == null)
        {
            Debug.LogWarning("Cannot spawn air, no air prefab !");
            return;
        }

        if (prefabAir.GetComponent<PrefabInstance>().prefabTag != PrefabTag.Air)
        {
            Debug.LogWarning("Cannot spawn air, air prefab is not tagged as air !");
            return;
        }

        //On le fait une première fois pour ajouter l'air
        this.uniqueTilesInGrid = new List<UniqueTile>();
        this.hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();
        UniqueTile.ResetId();

        RemoveAir();
        initGrid();
        ScanGrid(1);
        FillAir();


        //On reset et on rescan tout pour mettre au propre (et avoir les bonnes valeurs dans l'editeur par exemple)
        this.uniqueTilesInGrid = new List<UniqueTile>();
        this.hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();
        UniqueTile.ResetId();

        initGrid();
        ScanGrid(1);
        CreateRelations();

        foreach (UniqueTile u in uniqueTilesInGrid)
        {
            if (u.minNb > 0)
                Debug.Log(u.pi.stringId + "(" + u.id + ") has min " + u.minNb);
        }
    }

    private NavGrid navGridDebug = null;
    public bool showNavGridDebug = true;
    public bool showDistanceField = true;
    private SimpleGridWFC.Module[,] modules = null;

    [Header("Agent flow de test")]
    public AgentFlowCurieux agent = null;
    [Range(-1.0f, 1.0f)]
    public float noveltyBoost = 1.0f;
    [Range(-1.0f, 1.0f)]
    public float heightUpBoost = 0.8f;
    [Range(-1.0f, 1.0f)]
    public float heightDownBoost = -0.2f;
    [Range(-1.0f, 1.0f)]
    public float safetyBoost = 0.5f;
    public float stepHeightDebugNav = 0.05f;
    public float jumpHeightDebugNav = 0.8f;
    public int idPlayerStart = -1;
    public void BuildAndShowNavEditor()
    {
        if (grid == null)
            ScanAndFillAirEditor();

        navGridDebug = new NavGrid();
        modules = new SimpleGridWFC.Module[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                modules[i, j] = new SimpleGridWFC.Module(grid[i, j].linkedTile, grid[i, j].rotationGridY);
            }
        }

        navGridDebug.Build(modules);
        showNavGridDebug = true;
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void TogglePrefabInstanceGizmos()
    {
        PrefabInstance[] allPi = GetComponentsInChildren<PrefabInstance>();
        if (allPi.Length == 0)
            return;

        bool value = !allPi[0].hideGizmos;
        foreach (PrefabInstance pi in allPi)
            pi.hideGizmos = value;

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void StartAgentEditor()
    {

        if (navGridDebug == null)
            BuildAndShowNavEditor();
        //On trouve le point de départ

        int xStart = -1;
        int zStart = -1;
        int dirStart = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (modules[x, z] != null && modules[x, z].linkedTile.id == idPlayerStart)
                {
                    xStart = x;
                    zStart = z;
                    dirStart = modules[x, z].rotationY;
                    break;
                }
            }
        }

        //Pas de start, c'est nul
        if (xStart < 0)
        {
            Debug.LogWarning("Pas de point de start...");
            return;
        }

        //On part du départ et on voit comme ca avance tout droit
        float fitness = 0.0f;

        agent = new AgentFlowCurieux();
        agent.Init(xStart, zStart, 0 ,dirStart, navGridDebug, gridUnitSize);
        agent.heightDownBoost = heightDownBoost;
        agent.heightUpBoost = heightUpBoost;
        agent.safetyBoost = safetyBoost;
        agent.noveltyBoost = noveltyBoost;
        agent.UpdatePerception();

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void StepAgentEditor()
    {
        if (agent == null)
            return;

        agent.heightDownBoost = heightDownBoost;
        agent.heightUpBoost = heightUpBoost;
        agent.safetyBoost = safetyBoost;
        agent.noveltyBoost = noveltyBoost;

        agent.Step();
        agent.UpdatePerception();
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void UpdateAgentEditor()
    {
        if (agent == null)
            return;

        agent.heightDownBoost = heightDownBoost;
        agent.heightUpBoost = heightUpBoost;
        agent.safetyBoost = safetyBoost;
        agent.noveltyBoost = noveltyBoost;
       
        agent.UpdatePerception();
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void KillAgentEditor()
    {
        agent = null;
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [ReadOnly] public float fitnessLevel = 0.0f;
    public void EvaluateLevelEditor()
    {
        agent.heightDownBoost = heightDownBoost;
        agent.heightUpBoost = heightUpBoost;
        agent.safetyBoost = safetyBoost;
        agent.noveltyBoost = noveltyBoost;

        BuildAndShowNavEditor();

        fitnessLevel = 0;
        float nbSteps = 30000.0f;
        for (int i=0;i< nbSteps; i++)
            fitnessLevel += agent.Step();
        fitnessLevel /= nbSteps;

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    

    private void RemoveAir()
    {
        //Array to hold all child obj
        GameObject[] allChildren = new GameObject[transform.childCount];

        //Find all child obj and store to that array
        int i = 0;
        foreach (Transform child in transform)
        {
            allChildren[i] = child.gameObject;
            i += 1;
        }


        foreach (GameObject child in allChildren)
        {
            if (child.GetComponent<PrefabInstance>().prefabTag == PrefabTag.Air)
            {
                DestroyImmediate(child);
            }
        }
    }


    private void FillAir()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (grid[i, j].linkedTile == null)
                {
                    GameObject air = (GameObject)Instantiate(prefabAir, new Vector3(i * gridUnitSize, 0, j * gridUnitSize) + transform.position + new Vector3(gridUnitSize / 2.0f, 0, gridUnitSize / 2.0f), Quaternion.identity);
                    air.transform.parent = this.transform;
                }
            }
        }
    }

    /*private void FillAir()
    {
        string idAir = prefabAir.GetComponent<PrefabInstance>().stringId;
        

        UniqueTile airUT = null;

        //Create or get existing air uniqueTile
        if (!hashPrefabToUniqueTile.ContainsKey(idAir))
        {
            airUT = new UniqueTile();
            airUT.pi = prefabAir.GetComponent<PrefabInstance>();
        }
        else
        {
            airUT = hashPrefabToUniqueTile[idAir];
        }

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if(grid[i, j].linkedTile == null)
                {
                    if(incrementNbInGrid)
                        airUT.nbInGrid++;

                    grid[i, j].linkedTile = airUT;

                    grid[i, j].param = airUT.pi.param;//

                    //Spawn dummy air prefab
                    GameObject air = (GameObject) Instantiate(prefabAir, new Vector3(i * gridUnitSize, 0, j * gridUnitSize) + transform.position + new Vector3(gridUnitSize / 2.0f, 0, gridUnitSize / 2.0f), Quaternion.identity);
                    air.transform.parent = this.transform;
                }
            }
        }

        //If air uniqueTile wasn't created before and is currently in this grid, we add it to uniqueTile list
        if(airUT.nbInGrid > 0 && !hashPrefabToUniqueTile.ContainsKey(idAir))
        {
            uniqueTilesInGrid.Add(airUT);
            hashPrefabToUniqueTile.Add(idAir, airUT);
        }
    }*/

    private void initGrid()
    {
        hashBigTileParent.Clear();

        grid = new GridTile[gridSize, gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                grid[i, j] = new GridTile();
            }
        }
    }

    private Vector3[] coinsRangement =
    {
        new Vector3(0,0,0),
        new Vector3(1,0,0),
        new Vector3(0,0,1),
        new Vector3(1,0,1),
        new Vector3(0.5f,0,0.5f)
    };


    private Dictionary<string, BigTile> hashBigTileParent = new Dictionary<string, BigTile>();

    //Permet de booster la grille : on compte plus les assets en multipliant leur nombre 
    private void ScanGrid(int multiplyNbInGrid = 1)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        PrefabInstance currentPrefabInstance;

        foreach (Transform child in allChildren)
        {
            if (child.gameObject == this.gameObject || !child.gameObject.activeSelf || child.parent != this.transform)
                continue;

            int x = (int)child.localPosition.x / gridUnitSize; //round peut mettre au suivant
            int y = (int)child.localPosition.y;
            int z = (int)child.localPosition.z / gridUnitSize;

            Vector3 casePosition = new Vector3(x * gridUnitSize, 0, z * gridUnitSize);

            //On snap les assets
            if (RangeLesAssets)
            {

                float minDist = float.MaxValue;
                Vector3 coinRangement = new Vector3();
                bool aRanger = false;
                foreach(Vector3 coin in coinsRangement)
                {
                    float dist = Vector3.Distance(coin * gridUnitSize, child.localPosition - casePosition);
                    if(dist < DistanceMinRangeAsset * gridUnitSize && dist < minDist)
                    {
                        minDist = dist;
                        coinRangement = coin;
                        aRanger = true;
                    }
                }

               

                if(aRanger)
                    child.localPosition = coinRangement * gridUnitSize + casePosition;
            }

            //asset out of the grid size
            if ((x >= gridSize || x < 0) || (z >= gridSize || z < 0))
            {
                foreach (Renderer r in child.GetComponentsInChildren<MeshRenderer>())
                {
                    foreach (Material m in r.materials)
                        m.color = new Color(1, 0, 0, 1);
                }
                    
                continue;
            }
                
                
            int rotY = Mathf.RoundToInt(child.rotation.eulerAngles.y / 90);
            if (rotY < 0)
                rotY += 4;

            currentPrefabInstance = child.gameObject.GetComponent<PrefabInstance>();

            if (currentPrefabInstance == null)
                continue;

            //BigTile subdivision, will work fine if pivot is left bottom
            //Si tout se passe bien, pour un asset normal (qui rentre dans une case), une seule boucle et une seule UniqueTile
            {
                int subIdCounter = 0;

                //Debug.Log("For " + currentPrefabInstance.stringId);

                Vector2Int size = currentPrefabInstance.size;

                int xCases = size.x;
                int zCases = size.y;

                //Debug.Log("Size is " + size);

                //Localement
                //child.transform.forward;//regarder x et le y en 1 et -1, round le x et y du right
                //child.transform.right;

                for (int i = 0; i < xCases; i++)
                {
                    for (int j = 0; j < zCases; j++)
                    {
                        //new position based on rotation
                        int offx = 0;
                        int offz = 0;

                        Vector3 wOffDir = child.TransformDirection(new Vector3(i, 0, j));

                        offx = x + Mathf.RoundToInt(wOffDir.x);
                        offz = z + Mathf.RoundToInt(wOffDir.z);

                        if (offx > gridSize || offx < 0)
                            continue;
                        if (offz > gridSize || offz < 0)
                            continue;

                        //Debug.Log("ij is " + i + " " + j);

                        //Nouvel id pour chaque "morceau", id identique si un seul morceau
                        //Debug.Log("subcounter is " + subIdCounter);
                        string id = (subIdCounter == 0) ? currentPrefabInstance.stringId : currentPrefabInstance.stringId + subIdCounter;

                        if (!hashPrefabToUniqueTile.ContainsKey(id))
                        {
                            UniqueTile newUT = new UniqueTile(currentPrefabInstance);
                            
                            //Associer parent
                            if (xCases > 1 || zCases > 1)//bigtile it is
                            {
                                Debug.Log("Bigtile is " + id);

                                if (subIdCounter == 0)
                                {
                                    BigTile parent = new BigTile();
                                    parent.subpartPos.Add(newUT.id, new Vector3Int(offx - x, 0, offz - z));//offset pos from pivot origin
                                    hashBigTileParent.Add(currentPrefabInstance.stringId, parent);
                                    newUT.parent = parent;
                                }
                                else
                                {
                                    BigTile parent = hashBigTileParent[currentPrefabInstance.stringId];
                                    parent.subpartPos.Add(newUT.id, new Vector3Int(offx - x, 0, offz - z));//offset pos from pivot origin
                                    newUT.parent = parent;
                                }
                                
                                //if(i == x && j == z)
                                //{
                                //    newUT.parent = new BigTile();
                                //}
                                //else//Same parent for all uniquetile of the same bigtile
                                //{
                                //    newUT.parent = grid[x, z].linkedTile.parent;
                                //}
                                //newUT.parent.subpartPos.Add(newUT.id, new Vector3(i, 0, j));//create offset positions for bigtile
                            }

                            hashPrefabToUniqueTile.Add(id, newUT);
                            uniqueTilesInGrid.Add(newUT);
                            newUT.maxNb = newUT.pi.maxNb;
                            newUT.minNb = newUT.pi.minNb;
                        }

                        UniqueTile ut = hashPrefabToUniqueTile[id];

                        ut.nbInBaseGrid+= multiplyNbInGrid;

                        grid[offx, offz].linkedTile = ut;

                        grid[offx, offz].rotationGridY = rotY;

                        grid[offx, offz].param = currentPrefabInstance.param;

                        //grid[offx, offz].linkedChild = child.gameObject;//useless ?
                        
                        subIdCounter++;
                    }
                }
            }




        }

        
    }

    public Relation getRelation(Relation[] array, UniqueTile ut)
    {
        foreach (Relation a in array)
        {
            if (a.to == ut)
            {
                return a;
            }
        }
        return null;//error
    }

    private void CreateRelations()
    {
        if (uniqueTilesInGrid.Count == 0)
        {
            Debug.LogError("No relations created because no tiles found");
            return;
        }
            

        Debug.Log("first ut is " + uniqueTilesInGrid[0].pi.prefab);


        //init relations for each unique tiles
        foreach (UniqueTile ut in uniqueTilesInGrid)
        {
            int startIndex = 0;

            //fix multigrille + prevent reinit relation
            if(ut.relations != null)
            {
                startIndex = ut.relations.Length;

                if(ut.relations.Length < uniqueTilesInGrid.Count)
                {
                    Array.Resize<Relation>(ref ut.relations, uniqueTilesInGrid.Count);
                }
            }
            else
            {
                ut.relations = new Relation[uniqueTilesInGrid.Count];
            }

            for (int i = startIndex; i < uniqueTilesInGrid.Count; i++)
            {
                ut.relations[i] = new Relation();
                ut.relations[i].to = uniqueTilesInGrid[i];//raccourci, qui peut etre retrouvé par juste l'index == id de la tile

                /* VIRE AUTO BORDER 
                //corner to corner 
                if(takeBorderIntoAccount && ut == uniqueTilesInGrid[0] && ut.relations[i].to == ut)
                {
                    ut.relations[i].autorization = 0b_1111_1111_1111_1111;
                }*/
            }
        }


        //A verif, pas redondant avec la section en dessous avec le elseif(takeborder) ???

        /* VIRE AUTO BORDER
        if(takeBorderIntoAccount)//Creer relation entre les blocs en bords avec le faux tile border
        {
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (i == 0 || j == 0 || i == gridSize - 1 || j == gridSize - 1)
                    {
                        GridTile current = grid[i, j];
                        UniqueTile ut = current.linkedTile;

                        if (ut == null)//empty case
                            continue;

                        TileParameters tp = current.param;

                        UniqueTile cornerUT = uniqueTilesInGrid[0];

                        if (i + 1 == gridSize)
                        {
                            GridTile left = current;
                            Relation r = getRelation(cornerUT.relations, left.linkedTile);
                            TileParameters leftTP = left.param;
                            r.autorization = BinaryUtility.writeRelation(r.autorization, 2, 0, true, false, 0, left.rotationGridY, leftTP.allRotationsAllowed, leftTP.symetricalAxisY);
                        }

                        if (i == 0)
                        {
                            GridTile right = current;
                            Relation r = getRelation(cornerUT.relations, right.linkedTile);
                            TileParameters rightTP = right.param;
                            r.autorization = BinaryUtility.writeRelation(r.autorization, 0, 0, true, false, 2, right.rotationGridY, rightTP.allRotationsAllowed, rightTP.symetricalAxisY);
                        }

                        if (j + 1 == gridSize)
                        {
                            GridTile down = current;
                            Relation r = getRelation(cornerUT.relations, down.linkedTile);
                            TileParameters downTP = down.param;
                            r.autorization = BinaryUtility.writeRelation(r.autorization, 3, 0, true, false, 1, down.rotationGridY, downTP.allRotationsAllowed, downTP.symetricalAxisY);
                        }

                        if (j == 0)
                        {
                            GridTile top = current;
                            Relation r = getRelation(cornerUT.relations, top.linkedTile);
                            TileParameters topTP = top.param;
                            r.autorization = BinaryUtility.writeRelation(r.autorization, 1, 0, true, false, 3, top.rotationGridY, topTP.allRotationsAllowed, topTP.symetricalAxisY);
                        }
                    }
                }
            }
        }*/
        

        Debug.Log("uniqueTilesInGrid count is " + uniqueTilesInGrid.Count);

        //Applique le allRotationsAlwaysAllowed pour rester cohérent
        /*for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (grid[i, j].param.allRotationsAlwaysAllowed)
                    grid[i, j].param.allRotationsAllowed = true;
            }
        }*/


        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                GridTile current = grid[i, j];
                UniqueTile ut = current.linkedTile;

                if (ut == null)//empty case
                    continue;

                TileParameters tp = current.param;

                //Find a way to put all of this in a loop and then generate 8 for diagonals

                //Debug.Log("ij is " + i + " - " + j);

                //Check neighboors
                if (i - 1 >= 0)
                {
                    GridTile left = grid[i - 1, j];

                    if (left.linkedTile != null)//check for emtpy tile on the grid
                    {
                        Relation r = getRelation(ut.relations, left.linkedTile);
                        TileParameters leftTP = left.param;

                        r.autorization = BinaryUtility.writeRelation(r.autorization, 2, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY,
                                                                    0, left.rotationGridY, leftTP.allRotationsAllowed, leftTP.symetricalAxisY);//2 -> left for current tiles, 0 -> right for left tiles
                    }
                }
                /* VIRE AUTO BORDER
                else if (takeBorderIntoAccount)
                {
                    Relation r = getRelation(ut.relations, uniqueTilesInGrid[0]);//0 => cornerUT
                    r.autorization = BinaryUtility.writeRelation(r.autorization, 2, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY, 0, 0, true, false);
                }
                */

                if (i + 1 < gridSize)
                {
                    GridTile right = grid[i + 1, j];

                    if (right.linkedTile != null)//check for emtpy tile on the grid
                    {
                        Relation r = getRelation(ut.relations, right.linkedTile);
                        TileParameters rightTP = right.param;

                        r.autorization = BinaryUtility.writeRelation(r.autorization, 0, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY,
                                                                    2, right.rotationGridY, rightTP.allRotationsAllowed, rightTP.symetricalAxisY);
                    }
                }
                /* VIRE AUTO BORDER
                else if (takeBorderIntoAccount)
                {
                    Relation r = getRelation(ut.relations, uniqueTilesInGrid[0]);//0 => cornerUT
                    r.autorization = BinaryUtility.writeRelation(r.autorization, 0, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY, 2, 0, true, false);
                }*/

                if (j - 1 >= 0)
                {
                    GridTile down = grid[i, j - 1];

                    if (down.linkedTile != null)//check for emtpy tile on the grid
                    {
                        Relation r = getRelation(ut.relations, down.linkedTile);
                        TileParameters downTP = down.param;

                        r.autorization = BinaryUtility.writeRelation(r.autorization, 3, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY,
                                                                    1, down.rotationGridY, downTP.allRotationsAllowed, downTP.symetricalAxisY);
                    }
                }
                /* VIRE AUTO BORDER
                else if (takeBorderIntoAccount)
                {
                    Relation r = getRelation(ut.relations, uniqueTilesInGrid[0]);//0 => cornerUT
                    r.autorization = BinaryUtility.writeRelation(r.autorization, 3, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY, 1, 0, true, false);
                }*/

                if ((j + 1) < gridSize)
                {

                    GridTile top = grid[i, j + 1];

                    if (top.linkedTile != null)//check for emtpy tile on the grid
                    {
                        Relation r = getRelation(ut.relations, top.linkedTile);
                        TileParameters topTP = top.param;

                        r.autorization = BinaryUtility.writeRelation(r.autorization, 1, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY,
                                                                    3, top.rotationGridY, topTP.allRotationsAllowed, topTP.symetricalAxisY);
                    }
                }
                /* VIRE AUTO BORDER
                else if (takeBorderIntoAccount)
                {
                    Relation r = getRelation(ut.relations, uniqueTilesInGrid[0]);//0 => cornerUT
                    r.autorization = BinaryUtility.writeRelation(r.autorization, 1, current.rotationGridY, tp.allRotationsAllowed, tp.symetricalAxisY, 3, 0, true, false);
                }*/
            }
        }


        //Debug.Log("Tiles availables " + uniqueTilesInGrid.Count);
        //for (int i = 0; i < uniqueTilesInGrid.Count; i++)
        //{
        //    UniqueTile t = uniqueTilesInGrid[i];
        //    Debug.Log("Tile " + i + " - " + t.pi.prefab);

        //    foreach (Relation r in t.relations)
        //    {
        //        Debug.Log("Relation to " + r.to.pi.prefab + " -> " + BinaryUtility.GetIntBinaryString(r.autorization));
        //    }
        //}

    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        /* VIRE AUTO BORDER
        if(takeBorderIntoAccount)
        {
            Gizmos.color = new Color(1, 0, 1, 1.0f);
        }
        else
        {
            Gizmos.color = new Color(0, 0, 1, 1.0f);
        }*/

        Gizmos.color = new Color(0, 0, 1, 1.0f);

        Gizmos.DrawWireCube(transform.position + new Vector3(gridSize / 2.0f, 0.5f, gridSize / 2.0f) * gridUnitSize, new Vector3(gridSize, 1, gridSize) * gridUnitSize);

        for (int i = 0; i < gridSize; i++)
        {
            Vector3 xLineStart = transform.position + new Vector3(i, 0, 0) * gridUnitSize;
            Vector3 zLineStart = transform.position + new Vector3(0, 0, i) * gridUnitSize;

            Vector3 xLineEnd = xLineStart + new Vector3(0, 0, gridSize) * gridUnitSize;
            Vector3 zLineEnd = zLineStart + new Vector3(gridSize, 0, 0) * gridUnitSize;

            Gizmos.DrawLine(xLineStart, xLineEnd);
            Gizmos.DrawLine(zLineStart, zLineEnd);
        }

        if (agent != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position + new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2 + agent.height, agent.zPos * gridUnitSize + gridUnitSize / 2.0f), new Vector3(gridUnitSize * 0.8f, gridUnitSize * 0.8f, gridUnitSize * 0.8f));
            Gizmos.color = Color.green;
            /*switch (agent.direction)
            {
                case 0:
                    Gizmos.DrawCube(transform.position + new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f + gridUnitSize * 0.55f), new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));
                    break;
                case 1:
                    Gizmos.DrawCube(transform.position + new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f + +gridUnitSize * 0.55f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f), new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));
                    break;
                case 2:
                    Gizmos.DrawCube(transform.position + new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f - +gridUnitSize * 0.55f), new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));
                    break;
                case 3:
                    Gizmos.DrawCube(transform.position + new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f - +gridUnitSize * 0.55f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f), new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));
                    break;
            }*/

            Vector3[] offset =
            {
                new Vector3(0,0,1),
                new Vector3(1,0,0),
                new Vector3(0,0,-1),
                new Vector3(-1,0,0)
            };

            Vector3 positionF = transform.position +
                new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f) +
                offset[agent.direction] * gridUnitSize * 0.55f;

            Gizmos.DrawCube(positionF, new Vector3(gridUnitSize * 0.2f, gridUnitSize * 0.2f, gridUnitSize * 0.2f));

            positionF = transform.position +
                new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f) +
                offset[agent.direction] * gridUnitSize;

            Vector3 positionL = transform.position +
                new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f) +
                offset[(agent.direction+3)%4] * gridUnitSize;

            Vector3 positionR = transform.position +
            new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f) +
            offset[(agent.direction+1)%4] * gridUnitSize;

            Vector3 positionB = transform.position +
            new Vector3(agent.xPos * gridUnitSize + gridUnitSize / 2.0f, gridUnitSize / 2, agent.zPos * gridUnitSize + gridUnitSize / 2.0f) +
            offset[(agent.direction + 2) % 4] * gridUnitSize;

            

            UnityEditor.Handles.Label(positionF + Vector3.up * 2, ""+ Mathf.Round(agent.desirabilityF * 100) / 100);
            UnityEditor.Handles.Label(positionL + Vector3.up * 2, "" + Mathf.Round(agent.desirabilityN * 100) / 100);
            UnityEditor.Handles.Label(positionR + Vector3.up * 2, "" + Mathf.Round(agent.desirabilityP * 100) / 100);
            UnityEditor.Handles.Label(positionB + Vector3.up * 2, "" + Mathf.Round(agent.desirabilityB * 100) / 100);

        }

        if (navGridDebug != null && showNavGridDebug)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Color colorGiz = Color.red;
                    if (navGridDebug.Cells[x, z].XNHeightOutDiff < stepHeightDebugNav)
                        colorGiz = Color.green;
                    else if (navGridDebug.Cells[x, z].XNHeightOutDiff < jumpHeightDebugNav)
                        colorGiz = Color.cyan;

                    colorGiz = Color.Lerp(Color.red, colorGiz, navGridDebug.Cells[x, z].CanReachFromInsideXN);

                    Gizmos.color = colorGiz;
                    Gizmos.DrawCube(transform.position + new Vector3(x * gridUnitSize + 0.15f, 0, z * gridUnitSize + gridUnitSize / 2.0f), new Vector3(0.25f, 0.5f, 0.5f));

                    colorGiz = Color.red;
                    if (navGridDebug.Cells[x, z].XPHeightOutDiff < stepHeightDebugNav)
                        colorGiz = Color.green;
                    else if (navGridDebug.Cells[x, z].XPHeightOutDiff < jumpHeightDebugNav)
                        colorGiz = Color.cyan;

                    colorGiz = Color.Lerp(Color.red, colorGiz, navGridDebug.Cells[x, z].CanReachFromInsideXP);

                    Gizmos.color = colorGiz;
                    Gizmos.DrawCube(transform.position + new Vector3((x + 1) * gridUnitSize - 0.15f, 0, z * gridUnitSize + gridUnitSize / 2.0f), new Vector3(0.25f, 0.5f, 0.5f));

                    colorGiz = Color.red;
                    if (navGridDebug.Cells[x, z].ZNHeightOutDiff < stepHeightDebugNav)
                        colorGiz = Color.green;
                    else if (navGridDebug.Cells[x, z].ZNHeightOutDiff < jumpHeightDebugNav)
                        colorGiz = Color.cyan;

                    colorGiz = Color.Lerp(Color.red, colorGiz, navGridDebug.Cells[x, z].CanReachFromInsideZN);

                    Gizmos.color = colorGiz;
                    Gizmos.DrawCube(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, 0, z * gridUnitSize + 0.15f), new Vector3(0.5f, 0.5f, 0.25f));

                    colorGiz = Color.red;
                    if (navGridDebug.Cells[x, z].ZPHeightOutDiff < stepHeightDebugNav)
                        colorGiz = Color.green;
                    else if (navGridDebug.Cells[x, z].ZPHeightOutDiff < jumpHeightDebugNav)
                        colorGiz = Color.cyan;

                    colorGiz = Color.Lerp(Color.red, colorGiz, navGridDebug.Cells[x, z].CanReachFromInsideZP);

                    Gizmos.color = colorGiz;

                    Gizmos.DrawCube(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, 0, (z + 1) * gridUnitSize - 0.15f), new Vector3(0.5f, 0.5f, 0.25f));

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, navGridDebug.Cells[x, z].height, z * gridUnitSize + gridUnitSize / 2.0f), new Vector3(0.25f, 0.25f, 0.25f));
                }
            }
        }

        if (navGridDebug != null && showDistanceField)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    string text = "+";
                    if (navGridDebug.Cells[x, z].DistWallXN < (gridSize * gridSize * gridSize))
                        text =  ""+navGridDebug.Cells[x, z].DistWallXN;
                   
                    UnityEditor.Handles.Label(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 5.0f, navGridDebug.Cells[x, z].height, z * gridUnitSize + gridUnitSize / 2.0f),text);

                    text = "+";
                    if (navGridDebug.Cells[x, z].DistWallXP < (gridSize * gridSize * gridSize))
                        text = "" + navGridDebug.Cells[x, z].DistWallXP;
                    
                    UnityEditor.Handles.Label(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 1.3f, navGridDebug.Cells[x, z].height, z * gridUnitSize + gridUnitSize / 2.0f), text);

                    text = "+";
                    if (navGridDebug.Cells[x, z].DistWallZP < (gridSize * gridSize * gridSize))
                        text = "" + navGridDebug.Cells[x, z].DistWallZP;

                    UnityEditor.Handles.Label(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, navGridDebug.Cells[x, z].height, z * gridUnitSize + gridUnitSize / 1.3f), text);

                    text = "+";
                    if (navGridDebug.Cells[x, z].DistWallZN < (gridSize * gridSize * gridSize))
                        text = "" + navGridDebug.Cells[x, z].DistWallZN;

                    UnityEditor.Handles.Label(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, navGridDebug.Cells[x, z].height, z * gridUnitSize + gridUnitSize / 5.0f), text);

                    UnityEditor.Handles.color = Color.blue;

                    text = "+";
                    if (navGridDebug.Cells[x, z].distWallMean < (gridSize * gridSize * gridSize))
                        text = "" + Mathf.Round(navGridDebug.Cells[x, z].distWallMeanOfSq*100)/100;

                    UnityEditor.Handles.Label(transform.position + new Vector3(x * gridUnitSize + gridUnitSize / 2.0f, navGridDebug.Cells[x, z].height+1, z * gridUnitSize + gridUnitSize / 2.0f), text);
                }
            }
        }
    }
#endif
}
