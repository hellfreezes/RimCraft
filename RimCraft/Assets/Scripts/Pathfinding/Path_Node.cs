using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Node<T> {

    public T data; // Привязка вершины к какмоу-то объекту. Например к Тайлу. 
                   // Класс спецом сделан дженериком, чтобы не привязываться 
                   // только к одному типу объектов

    public Path_Edge<T>[] edges; // Это пути лежащие из текущей вершины в соседние вершины
	
}
