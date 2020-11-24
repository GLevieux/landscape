using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RelationGrid;

public class RelationCustom : MonoBehaviour
{
    public enum Side
    {
        LeftToRight,
        TopToBottom
    }

    public enum RotC
    {
        none,//no change
        allRot,
        allPrefab1Rot,
        allPrefab2Rot
    }

    [System.Serializable]
    public class RelationC
    {
        public Side side = Side.LeftToRight;
        public string prefab1 = "";
        public int rotPrefab1 = 0;
        public string prefab2 = "";
        public int rotPrefab2 = 0;
        public RotC rotOverride = RotC.none;
    }

    public List<RelationC> relationsCustom = new List<RelationC>();

    public void addRelationsCustom(ref Dictionary<string, UniqueTile> hashPrefabToUniqueTile)
    {
        foreach(RelationC rc in relationsCustom)
        {
            UniqueTile prefab1;
            UniqueTile prefab2;

            if (hashPrefabToUniqueTile.ContainsKey(rc.prefab1))
                prefab1 = hashPrefabToUniqueTile[rc.prefab1];
            else
                continue;

            if (hashPrefabToUniqueTile.ContainsKey(rc.prefab2))
                prefab2 = hashPrefabToUniqueTile[rc.prefab2];
            else
                continue;

            if(rc.rotOverride == RotC.allRot)
            {
                prefab1.relations[prefab2.id].autorization = 0b_1111_1111_1111_1111;
                prefab2.relations[prefab1.id].autorization = 0b_1111_1111_1111_1111;

                continue;
            }

            Relation relation1 = prefab1.relations[prefab2.id];
            Relation relation2 = prefab2.relations[prefab1.id];

            bool allRotPrefab1 = (rc.rotOverride == RotC.allPrefab1Rot) ? true : false;
            bool allRotPrefab2 = (rc.rotOverride == RotC.allPrefab2Rot) ? true : false;

            if (rc.side == Side.LeftToRight)
            {
                //relation1.autorization = BinaryUtility.writeRelation(relation1.autorization, 0, rc.rotPrefab1, 2, rc.rotPrefab2);
                //relation2.autorization = BinaryUtility.writeRelation(relation2.autorization, 2, rc.rotPrefab2, 0, rc.rotPrefab1);


                relation1.autorization = BinaryUtility.writeRelation(relation1.autorization, 0, rc.rotPrefab1, allRotPrefab1, false,
                                                                                            2, rc.rotPrefab2, allRotPrefab2, false);

                relation2.autorization = BinaryUtility.writeRelation(relation2.autorization, 2, rc.rotPrefab2, allRotPrefab2, false,
                                                                                            0, rc.rotPrefab1, allRotPrefab1, false);
            }
            else
            {
                //relation1.autorization = BinaryUtility.writeRelation(relation1.autorization, 3, rc.rotPrefab1, 1, rc.rotPrefab2);
                //relation2.autorization = BinaryUtility.writeRelation(relation2.autorization, 1, rc.rotPrefab2, 3, rc.rotPrefab1);

                relation1.autorization = BinaryUtility.writeRelation(relation1.autorization, 3, rc.rotPrefab1, allRotPrefab1, false,
                                                                                            1, rc.rotPrefab2, allRotPrefab2, false);

                relation2.autorization = BinaryUtility.writeRelation(relation2.autorization, 1, rc.rotPrefab2, allRotPrefab2, false,
                                                                                            3, rc.rotPrefab1, allRotPrefab1, false);
            }
        }
    }
}
