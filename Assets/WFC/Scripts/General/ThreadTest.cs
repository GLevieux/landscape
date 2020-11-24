using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class ThreadTest : MonoBehaviour
{
    public int nbThreads = 6;
    public bool executeTest = false;

    // Start is called before the first frame update
    void Start()
    {
        if(executeTest)
            StartThreads();
    }

    // The ThreadProc method is called when the thread starts.
    // It loops ten times, writing to the console and yielding
    // the rest of its time slice each time, and then ends.
    private static void ThreadProc()
    {
        Thread thread = Thread.CurrentThread;
        int id = thread.ManagedThreadId;

        for (int i = 0; i < 2; i++)
        {
            Debug.Log("Thread n°" + id + " named: " + thread.Name + " -> Do some work: " + RandomUtility.NextInt(0, 20));
            // Yield the rest of the time slice.
            Thread.Sleep(0);
        }
    }

    private void StartThreads()
    {
        List<Thread> listT = new List<Thread>();

        Debug.Log("Main unity thread: Start threads.");
        for(int i = 0; i < nbThreads; i++)
        {
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Name = "Thread " + i;
            listT.Add(t);

            // Start ThreadProc.  Note that on a uniprocessor, the new
            // thread does not get any processor time until the main thread
            // is preempted or yields.  Uncomment the Thread.Sleep that
            // follows t.Start() to see the difference.
            
            t.Start();
        }
        

        
        //Thread.Sleep(0);

        //for (int i = 0; i < 10; i++)
        //{
        //    Debug.Log("Main thread -> Do some work : " + RandomUtility.NextInt(0, 20));

        //    Thread.Sleep(0);
        //}



        Debug.Log("Main thread: Call Join(), to wait until ThreadProc ends.");
        for (int i = 0; i < nbThreads; i++)
        {
            Thread t = listT[i];
            t.Join();
        }
        Debug.Log("Main thread: ThreadProc.Join has returned.  Press Enter to end program.");
        //Console.ReadLine();
    }
}