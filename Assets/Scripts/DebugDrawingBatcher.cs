using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawingBatcher : MonoBehaviour
{
    private static DebugDrawingBatcher instance = null;
    private List<Action> batchedCalls = new();

    public static void BatchCall(Action action)
    {
        GetInstance().batchedCalls.Add(action);
    }

    public static void ClearCalls()
    {
        GetInstance().batchedCalls.Clear();
    }

    private static DebugDrawingBatcher GetInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("DebugDrawingBatcher");
            go.hideFlags = HideFlags.HideAndDontSave;
            instance = go.AddComponent<DebugDrawingBatcher>();
        }

        return instance;
    }

    private void Update()
    {
        foreach (var call in batchedCalls)
        {
            call.Invoke();
        }
    }
}