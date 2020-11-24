using System;

public static class BinaryUtility
{
    public static uint writeRelation(uint relation, int leftSide, int leftRot, bool leftAllRotOk, bool leftSymetricalY,
                                    int rightSide, int rightRot, bool rightAllRotOk, bool rightSymetricalY)
    {
        relation = writeRelation(relation, leftSide, leftRot, rightSide, rightRot);

        if (leftAllRotOk)
        {
            relation = writeRelation(relation, leftSide, (leftRot + 1) % 4, rightSide, rightRot);
            relation = writeRelation(relation, leftSide, (leftRot + 2) % 4, rightSide, rightRot);
            relation = writeRelation(relation, leftSide, (leftRot + 3) % 4, rightSide, rightRot);
        }

        if (rightAllRotOk)
        {
            relation = writeRelation(relation, leftSide, leftRot, rightSide, (rightRot + 1) % 4);
            relation = writeRelation(relation, leftSide, leftRot, rightSide, (rightRot + 2) % 4);
            relation = writeRelation(relation, leftSide, leftRot, rightSide, (rightRot + 3) % 4);
        }

        if (leftSymetricalY)
        {
            relation = writeRelation(relation, leftSide, (leftRot + 2) % 4, rightSide, rightRot);
        }

        if (rightSymetricalY)
        {
            relation = writeRelation(relation, leftSide, leftRot, rightSide, (rightRot + 2) % 4);
        }

        return relation;
    }

    public static uint writeRelation(uint relation, int leftSide, int leftRot, int rightSide, int rightRot)
    {
        uint mask_0 = 0b_0000_0000_0000_0001; // 0b_1000_0000_0000_0000
        int qn = (4 - 1) - (leftSide + leftRot) % 4; //dans l'autre sens se serait (leftSide + 4 - leftRot) % 4
        int bn = (4 - 1) - (rightSide + rightRot) % 4; //dans l'autre sens se serait (rightSide + 4 - rightRot) % 4

        return (relation | (mask_0 << ((qn * 4) + bn))); //dans l'autre sens se serait (relation | (mask_0 >> ((qn * 4) + bn)))
    }
    
    public static bool isRelationOK(uint relation, int leftSide, int leftRot, int rightSide, int rightRot)
    {
        uint mask_0 = 0b_0000_0000_0000_0001;
        int qn = (4 - 1) - (leftSide + leftRot) % 4;
        int bn = (4 - 1) - (rightSide + rightRot) % 4;

        return (relation & (mask_0 << ((qn * 4) + bn))) != 0;
    }

    public static string getIntBinaryString(uint n)
    {
        char[] b = new char[16];
        int pos = 15;
        int i = 0;

        while (i < 16)
        {
            if ((n & (1 << i)) != 0)
            {
                b[pos] = '1';
            }
            else
            {
                b[pos] = '0';
            }
            pos--;
            i++;
        }
        return new string(b);
    }
}
