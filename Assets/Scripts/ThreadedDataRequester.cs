using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    static ThreadedDataRequester instance;
    readonly ConcurrentQueue<ThreadInfo> dataQueue = new ConcurrentQueue<ThreadInfo>();

    void Awake()
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<dynamic> generateData, Action<dynamic> callback)
    {
        // TODO: @Max, add cancellation token (ALWAYS)
        Task.Run(() => instance.DataThread(generateData, callback));
    }

    void DataThread(Func<dynamic> generateData, Action<dynamic> callback)
    {
        var data = generateData();

        dataQueue.Enqueue(new ThreadInfo(data, callback));
    }

    void Update()
    {
        while (dataQueue.TryDequeue(out var threadInfo))
        {
            threadInfo.callback(threadInfo.parameter);
        }
    }

    struct ThreadInfo
    {
        public readonly dynamic parameter;
        public readonly Action<dynamic> callback;

        public ThreadInfo(dynamic parameter, Action<dynamic> callback)
        {
            this.parameter = parameter;
            this.callback = callback;
        }
    }
}
