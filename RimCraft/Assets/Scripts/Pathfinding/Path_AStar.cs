using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_AStar {

    Queue<Tile> path;

    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }

        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Стартовый тайл не содержится в списке вершин карты поиска пути.");
            return;
        }
        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Конечный тайл не содержится в списке вершин карты поиска пути.");
            return;
        }

        List<Path_Node<Tile>> ClosetSet = new List<Path_Node<Tile>>();
        List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();

        OpenSet.Add(nodes[tileStart]);

    }

    public Tile GetNextTile()
    {
        return path.Dequeue();
    }
}
