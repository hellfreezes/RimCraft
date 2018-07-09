using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_TileGraph {

    // Класс создает карту путей на основе нашего мира
    // Вершинами будут служить Тайлы, а грани будут являтся
    // связи между соседними проходимыми тайлами.

    public Dictionary<Tile, Path_Node<Tile>> nodes;


    public Path_TileGraph(World world)
    {
        // Пройтись по всем тайлам мира
        // Создать для кажого тайла вершину
        // Нужно ли создавать вершины для пустых тайлов? На данный момент НЕТ
        // Для тайлов которые никогда не будут проходимыми тоже НЕ создаем вершин

        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile t = world.GetTileAt(x, y);

                //if (t.movementCost > 0)
                //{
                    Path_Node<Tile> n = new Path_Node<Tile>();
                    n.data = t;
                    nodes.Add(t, n);
                //}
            }
        }

        Debug.Log("Path_TileGraph: Создано " + nodes.Count + " вершин.");

        // Пройтись по всем вершинам опять
        // и создаем грани - связи с соседними вершинами

        int edgeCount = 0;

        foreach(Tile t in nodes.Keys)
        {
            Path_Node<Tile> n = nodes[t];
            // Получаем список соседей для тайла

            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

            Tile[] neighbours = t.GetNeighbours(true); // некоторые значения массива могут возвращать null

            // Если тайл проходим, то создаем грань к этому соседу
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (neighbours[i] != null && neighbours[i].movementCost > 0)
                {
                    // соседний тайл существует и он проходим
                    // надо создать грань к этому тайлу

                    // но сначала проверим срезаем ли мы углы через непроходимые тайлы или не пытаемся ли мы пройти
                    // подиагонали между двумя непроходимыми тайлами
                    if (IsClippingCorner(t, neighbours[i]))
                    {
                        // Если это так, то не добавляем связь. Пропускам грань
                        continue;
                    }

                    Path_Edge<Tile> e = new Path_Edge<Tile>(); // новая грань
                    e.cost = neighbours[i].movementCost; // стоимость перемещения
                    e.node = nodes[neighbours[i]]; // обратная отсылка к текущей точке
                    edges.Add(e);

                    edgeCount++;
                }
            }

            n.edges = edges.ToArray();

        }

        Debug.Log("Path_TileGraph: Создано " + edgeCount + " граней.");

    }
	
    bool IsClippingCorner(Tile curr, Tile neigh)
    {
        // Если переход от curr до neigh является диагональным
        // Проверить не срезаем ли мы углы
        if (Mathf.Abs(curr.X - neigh.X) + Mathf.Abs(curr.Y - neigh.Y) == 2)
        {
            //Тайлы смежные подиагонали
            int dX = curr.X - neigh.X;
            int dY = curr.Y - neigh.Y;

            if (curr.world.GetTileAt(curr.X - dX, curr.Y).movementCost == 0)
            {
                //E или W непроходим, значит это будет срезание угла
                return true;
            }
            if (curr.world.GetTileAt(curr.X, curr.Y - dY).movementCost == 0)
            {
                //S или N непроходим, значит это будет срезание угла
                return true;
            }
        }

        return false;
    }
}
