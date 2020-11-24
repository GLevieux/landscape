using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

//Provide one Random instance per thread
public class RandomUtility
{ 
    private static Random _global = new Random();
    [ThreadStatic]
    private static Random _local;

    public static void setGlobalSeed(int seed)
    {
        _global = new Random(seed);
    }

    public static void setLocalSeed(int seed)
    {
        _local = new Random(seed);
    }

    //le prob du random en thread est que si le thread principal utilise du random, toutes les seed des random dans les threads sont foutues
    //enfin pas sur y a le local static, alors pk ça ne marche pas... l'ordre change trop vite ?

    //l'ordre appel du thread peut changer donc faudrait init le random du thread avant d'effectuer quoi que ce soit !!!!!
    //apres pour tester le determinisme, on peut supprimer les thread dans le GA pour effectuer les vérifications (mais plus lent)

    private static Random getInstance()
    {
        Random inst = _local;
        if (inst == null)
        {
            int seed;
            lock (_global) seed = _global.Next();
            _local = inst = new Random(seed);
        }
        return inst;
    }

    public static int NextInt()
    {
        return getInstance().Next();
    }

    //max is exclusive
    public static int NextInt(int min, int max)
    {
        return getInstance().Next(min, max);
    }

    public static double NextDouble()
    {
        return getInstance().NextDouble();
    }
}
