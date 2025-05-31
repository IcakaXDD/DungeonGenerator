using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
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
    /// <summary>
    /// Make a clone of the graph
    /// </summary>
    public Graph<T> Clone()
    {
        var newGraph = new Graph<T>();
        foreach (var node in adjacencyList.Keys)
        {
            newGraph.AddNode(node);
            foreach (var neighbor in GetNeighbors(node))
            {
                newGraph.AddEdge(node, neighbor);
            }
        }
        return newGraph;
    }
    /// <summary>
    /// Checks if the graph is fully connected.
    /// </summary>
    public bool IsFullyConnected()
    { 
        HashSet<T> visited = new HashSet<T>();
        Queue<T> queue = new Queue<T>();
        T startNode = default;

        foreach (var node in adjacencyList.Keys)
        {
            if (adjacencyList.ContainsKey(node))
            {
                startNode = node;
                break;
            }
        }

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacencyList[current])
            {
                //Skip if the neighbor of this node was removed
                if (!adjacencyList.ContainsKey(neighbor)) continue;

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return visited.Count == adjacencyList.Count;
    }

    public void BFS(T startNode)
    {
        HashSet<T> visited = new HashSet<T>();
        Queue<T> queue = new Queue<T>();
        queue.Enqueue(startNode);
        visited.Add(startNode);
        //Debug.Log(startNode + " is discovered");
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
    ///<summary>
    ///A method that generates a graph with no cycles 
    ///</summary>
    public Graph<T> BFSTree()
    {
        Graph<T> tree = new Graph<T>();
        HashSet<T> visited = new HashSet<T>();
        Queue<T> queue = new Queue<T>();
        Dictionary<T, T> parentMap = new Dictionary<T, T>(); // Track parent-child relationships

        T startNode = adjacencyList.Keys.FirstOrDefault();
        if (startNode == null) return tree;

        queue.Enqueue(startNode);
        visited.Add(startNode);
        //tree.AddNode(startNode);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();

            foreach (T neighbor in adjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);

                    tree.AddNode(current);
                    tree.AddNode(neighbor);
                    tree.AddEdge(current, neighbor);

                    parentMap[neighbor] = current;
                }
            }
        }
        return tree;
    }

    public void BFSTree2()
    {
        if (adjacencyList.Count == 0) return;

        var visited = new HashSet<T>();
        var parent = new Dictionary<T, T>();
        var queue = new Queue<T>();
        var edgesToKeep = new HashSet<(T, T)>();
        T startNode = adjacencyList.Keys.First();
        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            foreach (T neighbor in adjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    edgesToKeep.Add((current, neighbor)); // Mark as tree edge
                    edgesToKeep.Add((neighbor, current)); 
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Collect all edges to remove (those not in edgesToKeep)
        var edgesToRemove = new List<(T, T)>();
        foreach (var node in adjacencyList.Keys)
        {
            foreach (var neighbor in adjacencyList[node])
            {
                if (!edgesToKeep.Contains((node, neighbor)))
                {
                    edgesToRemove.Add((node, neighbor));

                }
            }
        }
        Debug.Log(edgesToRemove.Count);

        foreach (var (a, b) in edgesToRemove)
        {
            RemoveEdge(a, b);
        }
    }

}


