using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawingBatcher : MonoBehaviour
{
    private static Dictionary<string, DebugDrawingBatcher> instances = new();
    private static DebugDrawingBatcher root = null;
    private static List<DebugDrawingBatcher> debugDrawingBatchers = new List<DebugDrawingBatcher>();

    public static DebugDrawingBatcher GetInstance(string pName = "default") {
        if (!instances.TryGetValue(pName, out var value))
        {
            instances[pName] = value = CreateInstance(pName);
            if (root == null) root = value;
            debugDrawingBatchers.Add(value);
        }

        return value;
    }

    private static DebugDrawingBatcher CreateInstance(string pName)
    {
        GameObject go = new GameObject("DebugDrawingBatcher_"+pName);
        DebugDrawingBatcher instance = go.AddComponent<DebugDrawingBatcher>();
        return instance;
    }

    private List<Action> batchedCalls = new();

    public void BatchCall(Action action)
    {
        batchedCalls.Add(action);
    }

    public void ClearAllBatches()
    {
        foreach (var batcher in instances.Values)
        {
            batcher.batchedCalls.Clear();
        }
    }

    public void ClearCalls()
    {
        batchedCalls.Clear();
    }

    private void OnDrawGizmos()
    {
        if (this != root) return;

        foreach (var batcher in debugDrawingBatchers) {
            foreach (var call in batcher.batchedCalls)
            {
                call.Invoke();
            }
        }
    }

	private void OnApplicationQuit()
	{
        Debug.Log("Quitting");
        instances.Clear();
        debugDrawingBatchers.Clear();
		root = null;
	}
}