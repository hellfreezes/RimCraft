using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path_Edge<T> {

    public float cost; // Цена передвижения через эту грань-направление. Т.е. цена входа в Тайл

    public Path_Node<T> node;

}
