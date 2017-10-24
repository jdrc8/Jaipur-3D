﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grupo : MonoBehaviour {
    [Header("Ajustes")]
    public TipoAcomodo tipoAcomodo = TipoAcomodo.Vertical;
    public TipoGrupo tipoGrupo = TipoGrupo.Cartas;
    public bool objetosSeleccionables = false;

    [Header("Estetica")]
    public bool objetosCaraAbajo = false;
    public bool rotacionDesordenada = false;
    public float maxRotacionAleatoria = 7f;
    public bool invertirDireccionLateral = false;
    float inversorDireccion = 1f;

    //Arreglo que contiene los hijos de este objeto (osea las cartas)
    public GameObject[] hijos;
    public Carta[] hijosCarta;
    bool autoActualizarHijos = true;

    public bool congelado = false;

    private float separacionVertical = 0.01f;
    private float separacionLateral = 1f;

    void Awake() {
        //Obtener los hijos que tiene el objeto en ese momento
        hijos = ObtenerHijos();

        //Para invertir la direccion del acomodo lateral en caso de ser necesario
        if (invertirDireccionLateral) { inversorDireccion = -1f; }

        CalcularSeparaciones();
    }

    void Update() {
        //Checar si el movimiento de las cartas del mazo no está congelado (propositos de optimización)
        if (!congelado) {
            int indiceEnMazo = 0;

            //Iterar por cada GameObject hijo
            foreach (GameObject hijo in hijos) {
                //Calcular cual debe de ser su posicion en el mazo
                Vector3 posicionEnMazo = ObtenerPosicionEnMazo(indiceEnMazo);

                //Moverlo suavemente hasta su posicion designada
                hijo.transform.position = Vector3.Lerp(hijo.transform.position, posicionEnMazo, 3f * Time.deltaTime);
                //hijo.transform.rotation = Quaternion.Lerp(hijo.transform.rotation, ObtenerRotacionEnMazo(), 6f * Time.deltaTime);
                hijo.transform.rotation = Quaternion.RotateTowards(hijo.transform.rotation, ObtenerRotacionEnMazo(indiceEnMazo), 400f * Time.deltaTime);

                indiceEnMazo++;
            }
        }
    }

    private void OnTransformChildrenChanged() {
        congelado = false;

        if (autoActualizarHijos) {
            //Obtener los GameObject hijos
            hijos = ObtenerHijos();

            //Obtener los componentes Carta de los GameObject hijos
            if (tipoGrupo == TipoGrupo.Cartas) {
                hijosCarta = new Carta[hijos.Length];
                for (int x = 0; x < hijosCarta.Length; x++) {
                    hijosCarta[x] = hijos[x].GetComponent<Carta>();
                    hijosCarta[x].grupo = GetComponent<Grupo>();
                }
            }
        }
    }

    GameObject[] ObtenerHijos() {
        GameObject[] children = new GameObject[transform.childCount];
        int count = 0;
        foreach (Transform child in transform) {
            children[count] = child.gameObject;
            count++;
        }
        return children;
    }

    Vector3 ObtenerPosicionEnMazo(int indiceEnMazo) {
        //Si está seleccionada la Carta, hacerla un poco para arriba
        float offsetSeleccion = 0f;
        if (tipoGrupo == TipoGrupo.Cartas && hijosCarta[indiceEnMazo].seleccionada) {
            offsetSeleccion = 0.2f;
        }

        //En caso de que se necesiten acomodar verticalmente
        if (tipoAcomodo == TipoAcomodo.Vertical) {
            return new Vector3(transform.position.x, transform.position.y + (separacionVertical * indiceEnMazo), transform.position.z);
        }

        //En caso de que se necesiten acomodar lateralmente
        else if (tipoAcomodo == TipoAcomodo.Lateral) {
            return new Vector3(transform.position.x + indiceEnMazo * inversorDireccion, transform.position.y, transform.position.z + offsetSeleccion);
        }

        //En caso de que se necesiten acomodar lateralmente, encimadas
        else if (tipoAcomodo == TipoAcomodo.LateralEncimado) {
            return new Vector3(transform.position.x + (indiceEnMazo * separacionLateral) * inversorDireccion, transform.position.y + (indiceEnMazo * separacionVertical), transform.position.z + offsetSeleccion);
        }

        return transform.position;
    }

    Quaternion ObtenerRotacionEnMazo(int indiceEnMazo) {
        Quaternion rotacion;

        if (!objetosCaraAbajo) {
            rotacion = transform.rotation;
        } else {
            rotacion = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 180f);
        }

        if (rotacionDesordenada) {
            Random.seed = indiceEnMazo;
            rotacion = Quaternion.Euler(rotacion.eulerAngles.x, rotacion.eulerAngles.y + Random.Range(-maxRotacionAleatoria, maxRotacionAleatoria), rotacion.eulerAngles.z);
        }

        return rotacion;
    }

    void CalcularSeparaciones() {
        if (tipoGrupo == TipoGrupo.Cartas) {
            separacionVertical = 0.01f;
            separacionLateral = 1f;

            if (tipoAcomodo == TipoAcomodo.LateralEncimado) {
                separacionLateral = 0.3f;
            }
        } else if (tipoGrupo == TipoGrupo.Fichas) {
            separacionVertical = 0f;
            separacionLateral = 0.2f;
        }
    }

    #region Acciones

    public GameObject[] ObtenerUltimasCartas(int cantidad) {
        if (hijos.Length >= cantidad) {
            //Mandar las cartas
            GameObject[] cartas = new GameObject[cantidad];
            int ultimoIndex = hijos.Length - 1;
            for (int x = 0; x < cantidad; x++) {
                cartas[x] = hijos[ultimoIndex];
                ultimoIndex--;
            }
            return cartas;
        } else {
            //No hay suficientes cartas para satisfacer la peticion
            return null;
        }
    }

    public GameObject ObtenerUltimaCarta() {
        if (hijos.Length >= 1) {
            GameObject x = hijos[hijos.Length - 1];
            return x;
        } else {
            Debug.Log("Solo tengo " + hijos.Length + " hijos.");
            return null;
        }
    }

    public Carta[] RevolverCartas() {
        autoActualizarHijos = false;
        List<Carta> estadoCartas = new List<Carta>();

        //Revolver el arreglo
        for (int x = 0; x < hijos.Length; x++) {
            int nuevoIndex = Random.Range(0, hijos.Length - 1);
            GameObject temporal = hijos[nuevoIndex];
            hijos[nuevoIndex] = hijos[x];
            hijos[x] = temporal;
        }

        //Quitar los hijos actuales
        foreach (Transform x in transform) {
            x.SetParent(null);
        }

        //Reagregar el arreglo
        foreach (GameObject go in hijos) {
            go.transform.SetParent(transform);
        }

        //Checar en que orden quedaron para regresarlo despues
        foreach (Transform hijo in transform) {
            estadoCartas.Add(hijo.GetComponent<Carta>());
        }

        autoActualizarHijos = true;

        return estadoCartas.ToArray();
    }

    public void OrdenarPorTipo() {
        //Ordenarlas en un List
        List<GameObject> ordenadas = new List<GameObject>();
        Carta.TipoMercancia tipoActual = Carta.TipoMercancia.Cuero;
        while (true) {

            foreach (Transform hijo in transform) {
                Carta carta = hijo.GetComponent<Carta>();
                //hijo.SetParent(null);
                if (carta.mercancia == tipoActual) { ordenadas.Add(hijo.gameObject); }
            }

            //Cambiar el tipo actual
            if (tipoActual == Carta.TipoMercancia.Cuero) { tipoActual = Carta.TipoMercancia.Especias; } else if (tipoActual == Carta.TipoMercancia.Especias) { tipoActual = Carta.TipoMercancia.Tela; } else if (tipoActual == Carta.TipoMercancia.Tela) { tipoActual = Carta.TipoMercancia.Plata; } else if (tipoActual == Carta.TipoMercancia.Plata) { tipoActual = Carta.TipoMercancia.Oro; } else if (tipoActual == Carta.TipoMercancia.Oro) { tipoActual = Carta.TipoMercancia.Diamante; } else if (tipoActual == Carta.TipoMercancia.Diamante) { break; }
        }


        //Reasignarlas como hijos
        int index = 0;
        foreach (GameObject x in ordenadas) {
            //x.transform.SetParent(null);
            //x.transform.SetParent(transform);
            x.transform.SetSiblingIndex(index);
            index++;
        }
    }

    public GameObject[] ObtenerCamellos(int cantidad) {
        List<GameObject> camellos = new List<GameObject>();
        foreach (GameObject carta in hijos) {
            if (carta.GetComponent<Carta>().mercancia == Carta.TipoMercancia.Camello) {
                camellos.Add(carta);
            }

            if (camellos.Count >= cantidad) {
                return camellos.ToArray();
            }
        }
        return null;
    }

    #endregion

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if (tipoGrupo == TipoGrupo.Cartas) {
            DrawCube(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), transform.rotation, new Vector3(1f, 1f, 1.5f));
        } else if (tipoGrupo == TipoGrupo.Fichas) {
            DrawCube(new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z), transform.rotation, new Vector3(0.5f, 0.5f, 0.5f));
        }
    }

    /*Este es un procedimiento para dibujar un Cubo que soporta rotación. Fue sacado
    de Unity Forums.*/
    public void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale) {
        Matrix4x4 cubeTransform = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;

        Gizmos.matrix *= cubeTransform;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldGizmosMatrix;
    }

    public enum TipoAcomodo { Vertical, Lateral, LateralEncimado, Mano }
    public enum TipoGrupo { Cartas, Fichas }
}
