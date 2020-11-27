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
    public bool waitForPreviousResults = false;
    public WFCManager previousResults;
    public TypeOfResults paramResults;
    private bool resultsProcessed = false;

    public enum TypeOfResults
    {
        TakeAssetsAsTags,//Take tags from assets, will limit the choice to this tag for the case we are on
        //TakeAssetsAsInitial//Take assets and force them on the new grid, except AIR
    }

    //Public parameters
    public bool useCustomRandomSeed = true;
    public int customSeed = 0;
    public bool autoScreenshot = false;
    public bool useCustomRelations = false;

    private string debugText;
    public GameObject prefabBorder = null;

    public InitialGrid initialGrid;//set initial assets on the grid
    public ZoneGrid zoneGrid;//set initial zones on the grid

    //Pour ajouter des poids par exemple 
    [Serializable]
    public class RelationGridForWfc
    {
        [Tooltip("Permet de booster la grille en la comptant plusieurs fois")]
        public int addNbTimes = 1;
        public RelationGrid relationGrid = null;
    }
    public List<RelationGridForWfc> relationGrids = new List<RelationGridForWfc>();

    public WFCConfig wfcConfig = new WFCConfig();

    //Private
    private GAScript ga = null;
    private GAParameters gaParameters = null;
    private bool gaLaunched = false;
    private WFCManager nextWFC = null;

    //Custom test on mouse click
    //private int maxGenerationLoop = 10; 
    private List<SimpleGridWFC> listofWFC = new List<SimpleGridWFC>();
    private SimpleGridWFC currentWFC;

    private List<TimeSpan> listResultTime = new List<TimeSpan>();
    private bool launchOnce = false;
    private float timeBeforeLaunch = 1.0f;

    //---------------------------------------

    private void OnApplicationQuit()
    {
        if(ga != null)
        {
            ga.StopGA();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 1.0f);
        Gizmos.DrawWireCube(transform.position + new Vector3(wfcConfig.gridSize / 2.0f, 0.5f, wfcConfig.gridSize / 2.0f) * wfcConfig.gridUnitSize,
                                                new Vector3(wfcConfig.gridSize, 1, wfcConfig.gridSize) * wfcConfig.gridUnitSize);
    }

    private void Awake()
    {
        if (useCustomRandomSeed)
        {
            //System random
            RandomUtility.setLocalSeed(customSeed);//RandomUtility.setGlobalSeed(customSeed);

            //UnityEngine random => uniquement pour debug determinisme
            UnityEngine.Random.InitState(customSeed);
        }

        //To avoid comma in float to string (r studio recommended)
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        //link next wfc
        if(previousResults != null)
        {
            previousResults.setNextWFC(this);
        }
    }

    public void setNextWFC(WFCManager next)
    {
        nextWFC = next;
    }

    //Pour le script editeur, permet de forcer un scan des listes pour consulter la liste des tiles unique dans l'editeur.
    public void ScanGridsEditor()
    {
        tilesExtraction(ref relationGrids, ref utNormal);

        foreach (UniqueTile u in utNormal.uniqueTilesInGrid)
        {
            if (u.minNb > 0)
                Debug.Log(u.pi.stringId + "(" + u.id + ") has min " + u.minNb);
        }

        wfcConfig.uniqueTilesInGrid = utNormal.uniqueTilesInGrid;
        wfcConfig.hashPrefabToUniqueTile = utNormal.hashPrefabToUniqueTile;
    }

    //Used to extract and stock unique tiles from grids
    private class DataTiles
    {
        public List<UniqueTile> uniqueTilesInGrid = new List<UniqueTile>();
        public Dictionary<string, UniqueTile> hashPrefabToUniqueTile = new Dictionary<string, UniqueTile>();
    }

    private DataTiles utNormal = new DataTiles();

    private void tilesExtraction(ref List<RelationGridForWfc> listRG, ref DataTiles dt)
    {
        //init
        UniqueTile.ResetId();

        dt.uniqueTilesInGrid.Clear();
        dt.hashPrefabToUniqueTile.Clear();

        //generate dummy border tiles (dans tous les cas, on l'a) => a opti
        //if (takeBorderIntoAccount)//si takeBorder => alors il s'agit du premier bloc UT
        bool takeBorderIntoAccount = false;
        foreach (RelationGridForWfc r in listRG)
        {
            if(r.relationGrid.takeBorderIntoAccount)
            {
                takeBorderIntoAccount = true;
                break;
            }
        }
        if(takeBorderIntoAccount)//First unique tile at index 0 is dummy corner
        {
            UniqueTile cornerUT = new UniqueTile(prefabBorder.GetComponent<PrefabInstance>());
            cornerUT.nbInBaseGrid = 0;//a verif si 0 = ok
            dt.uniqueTilesInGrid.Add(cornerUT);
        }
        //il s'agit d'une tile à opti => ne pas prendre en compte pour les cases du milieu du wfc ? (supprimer liste)

        //get tiles from each relation grid
        foreach (RelationGridForWfc r in listRG)
        {
            r.relationGrid.extractTilesFromGrid(ref dt.uniqueTilesInGrid, ref dt.hashPrefabToUniqueTile, r.addNbTimes);
        }

        //Override with custom nbingrid limiter
        foreach(UniqueTile ut in dt.uniqueTilesInGrid)
        {
            ut.maxNb = ut.pi.maxNb;
            ut.minNb = ut.pi.minNb;
        }
    }

    private void initialAssetsExtraction()
    {
        if(initialGrid != null)
            wfcConfig.listInitialAssets = initialGrid.getInitialAssets();
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

    void Start()
    {
        tilesExtraction(ref relationGrids, ref utNormal);

        wfcConfig.uniqueTilesInGrid = utNormal.uniqueTilesInGrid;
        wfcConfig.hashPrefabToUniqueTile = utNormal.hashPrefabToUniqueTile;
        
        initialAssetsExtraction();

        if (useCustomRelations)
        {
            RelationCustom rc = GetComponent<RelationCustom>();
            if (rc != null)
            {
                rc.addRelationsCustom(ref utNormal.hashPrefabToUniqueTile);
            }
        }
        
        Debug.Log("------Tiles Normal------");
        debugTiles(ref utNormal);

        gaParameters = GetComponent<GAParameters>();
        ga = GetComponent<GAScript>();

        TryGenerateNewLevel();
    }

    

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

    public void processPreviousResult(ref Module[,] previousResult)
    {
        if(paramResults == TypeOfResults.TakeAssetsAsTags)
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
                Debug.LogWarning(this + "=> WFC Config do not take into account tags!");
            }
        }
    }

    void Update()
    {
        if (waitForPreviousResults && !resultsProcessed)
            return;

        if (gaLaunched)
        {
            if (ga == null)
                return;

            if (Input.GetButtonDown("GenerateEnd"))
            {
                ga.StopGA();
                gaLaunched = false;
            }

            if (ga.gaEnded)
            {
                gaLaunched = false;
                ga.gaEnded = false;
                ga.ShowResult(this.transform.position);
                
                if(zoneGrid)
                {
                    zoneGrid.setZones(ga.getZonesResult());
                }

                if (autoScreenshot && GetComponent<CameraCapture>())
                {
                    GetComponent<CameraCapture>().TakeScreenshot("AutoScreenshot.png", true);
                }

                debugText = "Generation time for GA: " + Mathf.Round((float)ga.getElapsedTime().TotalMilliseconds/100.0f)/10 + "s";

                StartCoroutine(SendMessageEndOfFrame("LevelGenerated"));

                if (nextWFC)
                {
                    this.gameObject.SetActive(false);
                    nextWFC.NextLaunch(ga.getResult());
                }
            }

            if(ga.isRunning())
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
                Debug.Log("One generation WFC => Time elapsed: " + stopwatch.Elapsed);

                listResultTime.Add(stopwatch.Elapsed);
                double res = listResultTime.Average(item => item.TotalMilliseconds);
                Debug.Log("Multiple generation WFC => Avg time elapsed ms: " + res);
                
                debugText = "Avg generation time: " + Mathf.Round((float)res/100)/10 + "s";

                currentWFC.show(true, this.transform.position, transform);

                StartCoroutine(SendMessageEndOfFrame("LevelGenerated"));
                

                if (nextWFC)
                {
                    this.gameObject.SetActive(false);
                    nextWFC.NextLaunch(currentWFC.getModuleResultFiltered());
                }
            }
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), debugText);
        if (gaLaunched)
            GUI.Label(new Rect(10, 30, 100, 20), "GA running");
    }

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
        Debug.Log("Start Generation in "+timeBeforeLaunch);

    }

    IEnumerator SendMessageEndOfFrame(string message)
    {
        yield return new WaitForEndOfFrame();
        gameObject.SendMessage(message);
    }

    public void TryGenerateNewLevel()
    {
        if (!gaLaunched)
        {
            launchOnce = true;
            timeBeforeLaunch = 0.5f;
            gameObject.SendMessage("PreStartGeneration");
        }
    }
}
