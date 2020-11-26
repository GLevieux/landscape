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
        Vector3 wOffDir = transform.TransformDirection(new Vector3(1, 0, 1));

        Gizmos.DrawWireCube(transform.position - new Vector3(wOffDir.x*gridUnitSize / 2, 0, wOffDir.z * gridUnitSize / 2) + new Vector3((size.x * gridUnitSize* wOffDir.x) / 2.0f, gridUnitSize/2, (size.y*gridUnitSize* wOffDir.z) / 2.0f), new Vector3((size.x * gridUnitSize* wOffDir.x) , gridUnitSize, (size.y*gridUnitSize* wOffDir.z) ));
    }

    public TileParameters param = new TileParameters();
}
