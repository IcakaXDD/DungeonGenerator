using System.Collections.Generic;
using UnityEngine;

public class Graph<T>
{
    private Dictionary<T, List<T>> adjacencyList;

    public Graph()
    {
        adjacencyList = new Dictionary<T, List<T>>();
    }
    
    public void Clear() 
    { 
        adjacencyList.Clear(); 
    }
    
    public void RemoveNode(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            adjacencyList.Remove(node);
        }
        
        foreach (var key in adjacencyList.Keys)
        {
            adjacencyList[key].Remove(node);
        }
    }
    
    public List<T> GetNodes()
    {
        return new List<T>(adjacencyList.Keys);
    }
    
    public void AddNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<T>();
        }
    }

    public void RemoveEdge(T fromNode, T toNode)
    {
        if (adjacencyList.ContainsKey(fromNode))
        {
            adjacencyList[fromNode].Remove(toNode);
        }
        if (adjacencyList.ContainsKey(toNode))
        {
            adjacencyList[toNode].Remove(fromNode);
        }
    }

    public void AddEdge(T fromNode, T toNode) { 
        if (!adjacencyList.ContainsKey(fromNode))
        {
            AddNode(fromNode);
        }
        if (!adjacencyList.ContainsKey(toNode)) { 
            AddNode(toNode);
        } 
        
        adjacencyList[fromNode].Add(toNode); 
        adjacencyList[toNode].Add(fromNode); 
    } 
    
    public List<T> GetNeighbors(T node) 
    { 
        return new List<T>(adjacencyList[node]); 
    }

    public int GetNodeCount()
    {
        return adjacencyList.Count;
    }
    
    public void PrintGraph()
    {
        foreach (var node in adjacencyList)
        {
            Debug.Log($"{node.Key}: {string.Join(", ", node.Value)}");
        }
    }

    public void BFS(T startNode)
    {
        HashSet<T> visited = new HashSet<T>();
        Queue<T> queue = new Queue<T>();
        queue.Enqueue(startNode);
        visited.Add(startNode);
        Debug.Log(startNode + " is discovered");
        while (queue.Count > 0)
        {
            startNode = queue.Dequeue();
            foreach (var edge in adjacencyList)
            {
                if (!visited.Contains(edge.Key))
                {
                    queue.Enqueue(edge.Key);
                    visited.Add(edge.Key);
                    Debug.Log(edge.Key + " is discovered");
                }

            }
        }
    }

    // Depth-First Search (DFS)
    public void DFS(T startNode)
    {
        Stack<T> stack = new Stack<T>();
        HashSet<T> visited = new HashSet<T>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            T currentNode = stack.Pop();
            if (!visited.Contains(currentNode))
            {
                Debug.Log(currentNode);
                visited.Add(currentNode);
                foreach (var edge in adjacencyList[currentNode])
                {
                    stack.Push(edge);
                }
            }
        }

    }
}