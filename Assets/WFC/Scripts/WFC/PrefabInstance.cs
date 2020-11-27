using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PrefabInstance : MonoBehaviour
{
    public enum PrefabTag
    {
        None = 0,
        Border = 1,
        Air = 2
    }

    [HideInInspector]
    public PrefabTag prefabTag = PrefabTag.None;

    public GameObject prefab;
    public string stringId = "UnknownStringId please fill";
    public bool doNotShowInCreatedLevel = false;
        
    [Tooltip("Nombre min dans le niveau généré")]
    public int minNb = 0;
    [Tooltip("Nombre max dans le niveau généré")]
    public int maxNb = 1000;
    
    //public bool isBigTile = false;

    public Vector2Int size = new Vector2Int(1, 1); //si > 1 == bigtile
    
    [Tooltip("Debug : pour avoir le bon gizmo de taille. Mettre le meme que le WFC")]
    public float gridUnitSize = 1; //Debug

    //Pour la navigation
    [Tooltip("Height of this side of the tile")]
    public float NavHeightZPosRot0 = -1;
    [Tooltip("How easy to reach this height from inside : 0 impossible 1 very quick")]
    public float CanReachFromInsideZPos = 1;
    [Tooltip("Height of this side of the tile")]
    public float NavHeightZNegRot0 = -1;
    [Tooltip("How easy to reach this height from inside : 0 impossible 1 very quick")]
    public float CanReachFromInsideZNeg = 1;
    [Tooltip("Height of this side of the tile")]
    public float NavHeightXPosRot0 = -1;
    [Tooltip("How easy to reach this height from inside : 0 impossible 1 very quick")]
    public float CanReachFromInsideXPos = 1;
    [Tooltip("Height of this side of the tile")]
    public float NavHeightXNegRot0 = -1;
    [Tooltip("How easy to reach this height from inside : 0 impossible 1 very quick")]
    public float CanReachFromInsideXNeg = 1;

    public bool hideGizmos = false;


    //Depend de l'instance
    [System.Serializable]
    public class TileParameters
    {
        //Permet de n'ajouter qu'une seule rotation
        /*[Tooltip("Ne prend jamais en compte les rotations pour ce prefab")]
        public bool allRotationsAlwaysAllowed = false;*/

        [Tooltip("Valide les liens pour toutes les rotations, juste ici pour ce module sur cette grille")]
        public bool allRotationsAllowed = false; 

        //public Axis symetricAxis = Axis.None; //opti ici, peut servir a supprimer des rotations inutiles
        //si both équivalent à => allrotallowed

        public bool symetricalAxisY = false; //permet de supprimer deux rotations

        //public Vector3 offsetOrigin = new Vector3(0, 0, 0);//used for bigtile
        
        //public Vector3 size = new Vector3(1,1,1);//in unity unit
    }

    public void OnDrawGizmosSelected()
    {
        if (hideGizmos)
            return;

        Vector3 wOffDir = transform.TransformDirection(new Vector3(1, 0, 1));

        Gizmos.DrawWireCube(transform.position - new Vector3(wOffDir.x*gridUnitSize / 2, 0, wOffDir.z * gridUnitSize / 2) + new Vector3((size.x * gridUnitSize* wOffDir.x) / 2.0f, gridUnitSize/2, (size.y*gridUnitSize* wOffDir.z) / 2.0f), new Vector3((size.x * gridUnitSize* wOffDir.x) , gridUnitSize, (size.y*gridUnitSize* wOffDir.z) ));

        float unitSize = gridUnitSize;
        
        Vector3 coinXZZero = transform.position - new Vector3(wOffDir.x * gridUnitSize / 2, 0, wOffDir.z * gridUnitSize / 2);
        Vector3 tailleModule = new Vector3(unitSize * size.x, 0, unitSize * size.y);
        Vector3 tailleModuleDemi = new Vector3(unitSize * size.x / 2.0f, 0, unitSize * size.y / 2.0f);

        
        Gizmos.color = NavHeightXPosRot0 < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModule.x, NavHeightXPosRot0, tailleModuleDemi.z)), 0.2f);
        Gizmos.color = NavHeightXNegRot0 < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(0, NavHeightXNegRot0,  tailleModuleDemi.z)), 0.2f);
        Gizmos.color = NavHeightZPosRot0 < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x, NavHeightZPosRot0, tailleModule.z)), 0.2f);
        Gizmos.color = NavHeightZNegRot0 < 0 ? Color.red : Color.blue;
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x, NavHeightZNegRot0, 0)), 0.2f);

        float heightMoy = (NavHeightXPosRot0 + NavHeightXNegRot0 + NavHeightZPosRot0 + NavHeightZNegRot0) / 4;

        Gizmos.color = Color.Lerp(Color.red, Color.green, CanReachFromInsideXPos);
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x + 0.2f, heightMoy, tailleModuleDemi.z)), 0.1f);
        Gizmos.color = Color.Lerp(Color.red, Color.green, CanReachFromInsideXNeg);
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x - 0.2f, heightMoy, tailleModuleDemi.z)), 0.1f);
        Gizmos.color = Color.Lerp(Color.red, Color.green, CanReachFromInsideZPos);
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x, heightMoy, tailleModuleDemi.z + 0.2f)), 0.1f);
        Gizmos.color = Color.Lerp(Color.red, Color.green, CanReachFromInsideZNeg);
        Gizmos.DrawSphere(coinXZZero + transform.TransformDirection(new Vector3(tailleModuleDemi.x, heightMoy, tailleModuleDemi.z - 0.2f)), 0.1f);
    }

    public TileParameters param = new TileParameters();
}
