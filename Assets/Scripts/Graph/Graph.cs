using System.Collections.Generic;
using UnityEngine;

public class Graph<T>
{
    private Dictionary<T, List<T>> adjacencyList; 
    public Graph() 
    {
        adjacencyList = new Dictionary<T, List<T>>(); 
    }
    public void AddNode(T node) 
    { 
        if (!adjacencyList.ContainsKey(node)) 
        {
            adjacencyList[node] = new List<T>(); 
        } 
    }
    public void AddEdge(T fromNode, T toNode) 
    {
        if (!adjacencyList.ContainsKey(fromNode) || !adjacencyList.ContainsKey(toNode)) 
        { 
            Debug.Log("One or both nodes do not exist in the graph."); 
            return; 
        }
        adjacencyList[fromNode].Add(toNode);
        adjacencyList[toNode].Add(fromNode); 
    }

    public void PrintGraph()
    {
        foreach (var item in adjacencyList)
        {
            string connectedNodes = " ";
            foreach (var item1 in item.Value)
            {
                connectedNodes += item1.ToString()+ " ";
            }
            Debug.Log(item.Key + " is connected to "+ connectedNodes);
        }
    }
}
