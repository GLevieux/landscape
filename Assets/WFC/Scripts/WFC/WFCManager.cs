using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static RelationGrid;
using static SimpleGridWFC;
using Debug = UnityEngine.Debug;
using static GAScript;
using static InitialGrid;

public class WFCManager : MonoBehaviour
{    
    private const int yGuiSize = 40;
    private static int currentYGui = 10;
    private int yGui;

    private WFCManager[] WFCManagers = null;
    private bool LevelGeneratedMessageToSend = false;

    //Pour ajouter des poids par exemple 
    [Serializable]
    public class RelationGridForWfc
    {
        [Tooltip("Permet de booster la grille en la comptant plusieurs fois")]
        public int addNbTimes = 1;
        public RelationGrid relationGrid = null;
    }
    [Tooltip("Permet entre autres de set automatiquement l'id du border dans le wfcConfig")]
    public GameObject prefabBorder = null;
    public List<RelationGridForWfc> relationGrids = new List<RelationGridForWfc>();

    [Header("Randomess")]
    public bool useCustomRandomSeed = true;
    public int customSeed = 0;

    [Header("Border WFC")]
    [Tooltip("Permet d'avoir un WFC précédent a coté du notre, qui va propager des contraintes")]
    public WFCManager prevBorderWFCManager = null;
    private bool prevBorderWFCProcessed = false;

    public enum TypeOfResults
    {
        TakeAssetsAsTags,//Take tags from assets, will limit the choice to this tag for the case we are on
        //TakeAssetsAsInitial//Take assets and force them on the new grid, except AIR
    }

    [Header("Divers")]
    public bool autoScreenshot = false;

    [Header("Configuration WFC")]
    public WFCConfig wfcConfig = new WFCConfig();

    [Header("Init Oldies")]
    public InitialGrid initialGrid;//set initial assets on the grid
    public ZoneGrid zoneGrid;//set initial zones on the grid
    [Tooltip("Ajouter un composant RelationCustom")]
    public bool useCustomRelations = false;
    public bool waitForPreviousResults = false;
    public WFCManager previousResults;
    public TypeOfResults paramResults;
    private bool resultsProcessed = false;

    private string debugText;
    private GAScript ga = null;
    private GAParameters gaParameters = null;
    private bool gaLaunched = false;

    private WFCManager nextWFC = null;
    private SimpleGridWFC currentWFC;
    [HideInInspector]
    public Module[,] currentModules = null;

    private bool launchOnce = false;
    private float timeBeforeLaunch = 1.0f;
    [HideInInspector]
    public bool generationIsDone = false;

    private void Awake()
    {
        yGui = currentYGui;
        currentYGui += yGuiSize;

        WFCManagers = GameObject.FindObjectsOfType<WFCManager>();

        if (useCustomRandomSeed)
        {
            //System random
            RandomUtility.setLocalSeed(customSeed);//RandomUtility.setGlobalSeed(customSeed);

            //UnityEngine random => uniquement pour debug determinisme
            UnityEngine.Random.InitState(customSeed);
        }

        //To avoid comma in float to string (better for r studio import)
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        //link next wfc
        if (previousResults != null)
        {
            previousResults.setNextWFC(this);
        }
    }

    void Start()
    {
        extractTilesAndSetToWfcConfig();

        initialAssetsExtraction();

        if (useCustomRelations)
        {
            RelationCustom rc = GetComponent<RelationCustom>();
            if (rc != null)
            {
                rc.addRelationsCustom(ref dataTiles.hashPrefabToUniqueTile);
            }
        }

        Debug.Log("------Tiles Normal------");
        debugTiles(ref dataTiles);

        gaParameters = GetComponent<GAParameters>();
        ga = GetComponent<GAScript>();

        TryGenerateNewLevel();
    }

    void Update()
    {
        if (waitForPreviousResults && !resultsProcessed)
            return;

        if (prevBorderWFCManager != null)
        {
            if (prevBorderWFCManager.generationIsDone == false)
            {
                prevBorderWFCProcessed = false;
                return;
            }
                
            if(prevBorderWFCProcessed == false)
                ProcessPrevBorderWFC();
        }

        bool allWFCFinished = true;
        foreach (WFCManager w in WFCManagers)
        {
            if (!w.generationIsDone)
            {
                allWFCFinished = false;
                break;
            }
        }
        if (allWFCFinished && LevelGeneratedMessageToSend)
        {
            gameObject.SendMessage("LevelGenerated");
            LevelGeneratedMessageToSend = false;
        }

        //Si le GA tourne
        if (gaLaunched)
        {
            if (Input.GetButtonDown("GenerateEnd"))
            {
                ga.StopGA();
                gaLaunched = false;
            }

            if (Input.GetButtonDown("ShowCurrentGeneration") || ga.getGenerationNumber() % 200 == 0)
            {
                ga.PauseGA(true);
                ga.ShowResult(this.transform.position);
                StartCoroutine(SendMessageEndOfFrame("LevelGenerated"));
                saveCurrentGeneration();
                ga.PauseGA(false);               
            }

            if (ga.gaEnded)
            {
                gaLaunched = false;
                ga.gaEnded = false;
                ga.ShowResult(this.transform.position);

                Logger.FlushToDisk();

                if (zoneGrid)
                {
                    zoneGrid.setZones(ga.getZonesResult());
                }

                if (autoScreenshot && GetComponent<CameraCapture>())
                {
                    GetComponent<CameraCapture>().TakeScreenshots(true);
                }

                debugText = ga.debug +  " Time GA: " + Mathf.Round((float)ga.getElapsedTime().TotalMilliseconds / 100.0f) / 10 + "s";

                StartCoroutine(SendMessageEndOfFrame("LevelGenerated"));
                //LevelGeneratedMessageToSend = true;

                currentModules = ga.getResult();

                generationIsDone = true;

                if (nextWFC)
                {
                    this.gameObject.SetActive(false);
                    nextWFC.NextLaunch(currentModules);
                }
            }

            if (ga.isRunning())
            {
                debugText = ga.debug;
            }
        }

        if (Input.GetButtonDown("Generate"))
        {
            TryGenerateNewLevel();
        }

        timeBeforeLaunch -= Time.deltaTime;
        if (launchOnce && timeBeforeLaunch <= 0)
        {
            launchOnce = false;
            gameObject.SendMessage("StartGeneration");

            //Si on lance le GA
            if (gaParameters && gaParameters.launchGA)
            {
                if (!ga)
                {
                    Debug.LogError(this + "You want to launch GA but have no GAScript");
                    return;
                }

                ga.init(gaParameters.gaConfig, wfcConfig, useCustomRandomSeed, customSeed);

                if (!wfcConfig.takeZonesIntoAccount)
                {
                    Debug.LogWarning(this + "=> WFC Config do not take into account zones, GA is pointless!");
                }

                Debug.Log("Launching GA");

                ga.launchGA();
                gaLaunched = true;
            }
            //Si on lance plutot simplement le WFC seul
            else
            {
                if (currentWFC == null)
                {
                    currentWFC = new SimpleGridWFC(wfcConfig);
                }

                // Create new stopwatch.
                Stopwatch stopwatch = new Stopwatch();
                // Begin timing.
                stopwatch.Start();

                currentWFC.launchWFC();

                // Stop timing.
                stopwatch.Stop();
                // Write result.
                Debug.Log("WFC => Time elapsed: " + stopwatch.Elapsed);

                currentWFC.show(true, this.transform.position, transform);
                
                StartCoroutine(SendMessageEndOfFrame("LevelGenerated"));
                //LevelGeneratedMessageToSend = true;

                currentModules = currentWFC.getModuleResult(true);

                generationIsDone = true;

                if (nextWFC)
                {
                    this.gameObject.SetActive(false);
                    nextWFC.NextLaunch(currentModules);
                }
            }
        }

        
    }

    private void OnApplicationQuit()
    {
        if (ga != null)
        {
            ga.StopGA();
        }
    }

    public void TryGenerateNewLevel()
    {
        if (!gaLaunched && !launchOnce)
        {
            generationIsDone = false;
            prevBorderWFCProcessed = false;
            launchOnce = true;
            timeBeforeLaunch = 0.5f;
            gameObject.SendMessage("PreStartGeneration");
        }
    }

    //Used to extract and stock unique tiles from grids
    private class DataTiles
    {
        public List<UniqueTile> uniqueTilesInGrid = new List<UniqueTile>();
        public Dictionary<string, UniqueTile> hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();
    }
    private DataTiles dataTiles = new DataTiles();

    private void extractTiles(ref List<RelationGridForWfc> listRG, ref DataTiles dt)
    {
        //init
        UniqueTile.ResetId();

        dt.uniqueTilesInGrid.Clear();
        dt.hashPrefabToUniqueTile.Clear();

        //get tiles from each relation grid
        foreach (RelationGridForWfc r in listRG)
        {
            r.relationGrid.extractTilesFromGrid(ref dt.uniqueTilesInGrid, ref dt.hashPrefabToUniqueTile, r.addNbTimes);
        }

        //Override with custom nbingrid limiter
        foreach (UniqueTile ut in dt.uniqueTilesInGrid)
        {
            ut.maxNb = ut.pi.maxNb;
            ut.minNb = ut.pi.minNb;
        }
    }

    private void extractTilesAndSetToWfcConfig()
    {
        extractTiles(ref relationGrids, ref dataTiles);

        foreach (UniqueTile u in dataTiles.uniqueTilesInGrid)
        {
            if (u.minNb > 0)
                Debug.Log(u.pi.stringId + "(" + u.id + ") has min " + u.minNb);

            if (u.pi.stringId == prefabBorder.GetComponent<PrefabInstance>().stringId)
                wfcConfig.idBorderTile = u.id;
        }

        wfcConfig.uniqueTilesInGrid = dataTiles.uniqueTilesInGrid;
        wfcConfig.hashPrefabToUniqueTile = dataTiles.hashPrefabToUniqueTile;
    }

    private void initialAssetsExtraction()
    {
        if (initialGrid != null)
            wfcConfig.listInitialAssets = initialGrid.getInitialAssets();
    }

    /**
     * BORDER WFC
     */

    void ProcessPrevBorderWFC()
    {
        float unitSize = prevBorderWFCManager.wfcConfig.gridUnitSize;
        if(unitSize != wfcConfig.gridUnitSize)
        {
            Debug.LogWarning("Not same grid size with previous border ! Unable to process.");
            return;
        }

        //On récupère les résultats de l'autre WFC
        Module[,] prevModules = prevBorderWFCManager.currentModules;
        int prevSize = prevModules.GetUpperBound(0) + 1;       
        Vector3 prevOrigin = prevBorderWFCManager.transform.position;
        int prevOriginX = Mathf.RoundToInt(prevOrigin.x / unitSize);
        int prevOriginZ = Mathf.RoundToInt(prevOrigin.z / unitSize);

        int originX = Mathf.RoundToInt(transform.position.x / unitSize);
        int originZ = Mathf.RoundToInt(transform.position.z / unitSize);
        int size = wfcConfig.gridSize;

        wfcConfig.borderModulesToPropagate.Clear();

        //Pour tous les modules
        for (int x = 0; x < prevSize; x++)
        {
            for (int z = 0; z < prevSize; z++)
            {
                if(prevModules[x,z] != null)
                {
                    int xWorld = x + prevOriginX;
                    int zWorld = z + prevOriginZ;

                    int xMySpace = xWorld - originX;
                    int zMySpace = zWorld - originZ;

                    if (xMySpace == -1 || zMySpace == -1 || xMySpace == size || zMySpace == size)
                    {
                        WFCConfig.ModuleToPropagate modTP = new WFCConfig.ModuleToPropagate();
                        modTP.module = prevModules[x, z];
                        modTP.xPos = xMySpace;
                        modTP.zPos = zMySpace;
                        wfcConfig.borderModulesToPropagate.Add(modTP);
                    }
                }
            }
        }

        prevBorderWFCProcessed = true;
    }

    /**
     * SEQUENTIAL WFC
     */
    public void NextLaunch(Module[,] previousResult)
    {
        processPreviousResult(ref previousResult);
        resultsProcessed = true;

        if (gaParameters && gaParameters.launchGA)
        {
            if (!wfcConfig.takeZonesIntoAccount)
            {
                Debug.LogWarning(this + "=> WFC Config do not take into account zones, GA is pointless!");
            }

            ga.launchGA();
        }
    }

    public void setNextWFC(WFCManager next)
    {
        nextWFC = next;
    }

    public void processPreviousResult(ref Module[,] previousResult)
    {
        if (paramResults == TypeOfResults.TakeAssetsAsTags)
        {
            List<ForceTag> res = new List<ForceTag>();

            for (int i = 0; i < wfcConfig.gridSize; i++)
            {
                for (int j = 0; j < wfcConfig.gridSize; j++)
                {
                    ForceTag tag = new ForceTag();
                    tag.position = new Vector2Int(i, j);
                    tag.prefabTag = previousResult[i, j].linkedTile.pi.prefabTag;
                    res.Add(tag);
                }
            }

            wfcConfig.listInitialTags = res;

            if (!wfcConfig.takeInitialTags)
            {
                Debug.LogWarning(this + "=> WFC Config does not take into account tags!");
            }
        }
    }

    //Pour le script editeur, permet de forcer un scan des listes pour consulter la liste des tiles unique dans l'editeur.
    public void ScanGridsEditor()
    {
        extractTilesAndSetToWfcConfig();
    }

    IEnumerator SendMessageEndOfFrame(string message)
    {
        yield return new WaitForEndOfFrame();
        gameObject.SendMessage(message);
    }

    /***
     * GIZMOS / GUI / Debug
     **/

    private void OnDrawGizmos()
    {
        gaParameters = GetComponent<GAParameters>();
        if(gaParameters && gaParameters.launchGA)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;

        Gizmos.DrawWireCube(transform.position + new Vector3(wfcConfig.gridSize / 2.0f, 0.5f, wfcConfig.gridSize / 2.0f) * wfcConfig.gridUnitSize,
                                                new Vector3(wfcConfig.gridSize, 1, wfcConfig.gridSize) * wfcConfig.gridUnitSize);

        Gizmos.color = Color.red;
        if (wfcConfig.initWithBordersXP)
            Gizmos.DrawLine(transform.position + new Vector3(wfcConfig.gridSize, 1.1f,                  0) * wfcConfig.gridUnitSize,
                            transform.position + new Vector3(wfcConfig.gridSize, 1.1f, wfcConfig.gridSize) * wfcConfig.gridUnitSize);
        if (wfcConfig.initWithBordersXN)
            Gizmos.DrawLine(transform.position + new Vector3(0, 1.1f, 0) * wfcConfig.gridUnitSize,
                            transform.position + new Vector3(0, 1.1f, wfcConfig.gridSize) * wfcConfig.gridUnitSize);
        if (wfcConfig.initWithBordersZP)
            Gizmos.DrawLine(transform.position + new Vector3(0, 1.1f, wfcConfig.gridSize) * wfcConfig.gridUnitSize,
                            transform.position + new Vector3(wfcConfig.gridSize, 1.1f, wfcConfig.gridSize) * wfcConfig.gridUnitSize);
        if (wfcConfig.initWithBordersZN)
            Gizmos.DrawLine(transform.position + new Vector3(0, 1.1f, 0) * wfcConfig.gridUnitSize,
                            transform.position + new Vector3(wfcConfig.gridSize, 1.1f, 0) * wfcConfig.gridUnitSize);

    }
    void OnGUI()
    {

        GUI.Label(new Rect(10, yGui, 600, 20), debugText);
        if (gaLaunched)
            GUI.Label(new Rect(10, yGui+20, 100, 20), "GA running");
    }
    private void debugTiles(ref DataTiles dt)
    {
        Debug.Log("Tiles availables " + dt.uniqueTilesInGrid.Count);
        for (int i = 0; i < dt.uniqueTilesInGrid.Count; i++)
        {
            UniqueTile t = dt.uniqueTilesInGrid[i];
            Debug.Log("Tile " + i + " - " + t.pi.prefab);

            foreach (Relation r in t.relations)
            {
                Debug.Log("Relation to " + r.to.pi.prefab + " -> " + BinaryUtility.getIntBinaryString(r.autorization));
            }
        }
    }


    /***
     * Dummy event handlers
     **/
    public void LevelGenerated()
    {
        Debug.Log("Level Generated");
    }

    public void StartGeneration()
    {
        Debug.Log("Start Generation");
    }

    public void PreStartGeneration()
    {
        Debug.Log("Start Generation in " + timeBeforeLaunch);
    }

    /***
     * LOG
     */
    
    public void saveCurrentGeneration()
    {
        CameraCapture c = GetComponent<CameraCapture>();
        if (c != null)
            c.TakeScreenshots(true);
        Logger.FlushToDisk();
    }
}


