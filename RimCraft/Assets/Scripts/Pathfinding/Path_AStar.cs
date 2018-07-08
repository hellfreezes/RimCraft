using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;

public class Path_AStar {

    Queue<Tile> path;

    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {
        // Проверить существование сгенерированной карты пути. И если ее нет, то сгенерировать
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }

        // Создаем перечень вершин
        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];

        // Проверяем входит ли тайл отправления в перечень вершин.
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("Стартовый тайл не содержится в списке вершин карты поиска пути.");
            return;
        }
        // Проверяем входит ли тайл прибытия в перечень вершин
        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("Конечный тайл не содержится в списке вершин карты поиска пути.");
            return;
        }

        // Создаем перечень вершин:
        // Проверенные и непригоные вершины
        List<Path_Node<Tile>> ClosetSet = new List<Path_Node<Tile>>();
        // Подходящие вершины
        //List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();
        // Добавляем в перечень пригодных вершин, тайл-вершину отправления
        //OpenSet.Add(start);

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);
        

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            g_score[n] = Mathf.Infinity;
        }
        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();
        foreach (Path_Node<Tile> n in nodes.Values)
        {
            f_score[n] = Mathf.Infinity;
        }
        f_score[start] = heuristic_cost_estimate(start, goal);

        while (OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();

            //Достигли точки назначения
            if (current == goal)
            {
                //TODO: вернуть путь
                return;
            }

            ClosetSet.Add(current);

            foreach(Path_Edge<Tile> edge in current.edges)
            {
                Path_Node<Tile> neighbour = edge.node;
                if (ClosetSet.Contains(neighbour) == true)
                {
                    continue;
                }

                float tentative_g_score = g_score[current] + dist_between(current, neighbour);

                if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                    continue;

                Came_From[neighbour] = current;
                g_score[neighbour] = tentative_g_score;
                f_score[neighbour] = g_score[neighbour] + heuristic_cost_estimate(neighbour, goal);

                if (OpenSet.Contains(neighbour) == false)
                {
                    OpenSet.Enqueue(neighbour, f_score[neighbour]);
                }
            } //foreach neighbour
        } // while

        /*
         * Если мы добрались до этой точки - это значит что мы прошерстили весь OpenSet, но так
         * и не добрались до тайла назначения. Что значит, что пути от стартовой точки до финишной НЕТ.
         */

        // Тут надо бы вернить неудачу. Но пока метод ничего не возвращает. Хммм...
    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current) 
    {
        // После того как алгоритм нашел путь. Надо перестроить его задо наперед чтобы пройти по нему
        // Метод переворачивает порядок вершин в листе
        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current.data); // точка назначение должна быть первой. Добавляем
        
        while (Came_From.ContainsKey(current))
        {
            current = Came_From[current];
            total_path.Enqueue(current.data);
        }
    }

    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );
    }

    float dist_between(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // Если учитывать, что мы имеем дело с сеткой, то:
        // Hori/Vert дистанция равна 1
        if (Mathf.Abs(a.data.X - b.data.X) + Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1f;
        }
        // Diag равна 1.41421356237 

        if (Mathf.Abs(a.data.X - b.data.X) == 1 && Mathf.Abs(a.data.Y - b.data.Y) == 1)
        {
            return 1.41421356237f;
        }

        return Mathf.Sqrt(
            Mathf.Pow(a.data.X - b.data.X, 2) +
            Mathf.Pow(a.data.Y - b.data.Y, 2)
            );
    }

    public Tile GetNextTile()
    {
        return path.Dequeue();
    }
}
