using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal sealed class Dijkstra
    {
        public ResultadoDijkstra Calcular(GrafoCampus grafo, int origen)
        {
            ArgumentNullException.ThrowIfNull(grafo);
            if (!grafo.Nodos.Contains(origen))
            {
                throw new ArgumentException($"El nodo de origen {origen} no existe en el grafo.", nameof(origen));
            }

            Dictionary<int, double> distancias = grafo.Nodos.ToDictionary(id => id, _ => double.PositiveInfinity);
            Dictionary<int, int> predecesores = [];
            PriorityQueue<int, double> pendientes = new();
            distancias[origen] = 0;
            pendientes.Enqueue(origen, 0);

            while (pendientes.TryDequeue(out int actual, out double distanciaActual))
            {
                if (distanciaActual > distancias[actual])
                {
                    continue;
                }

                foreach ((int vecino, double metros) in grafo.Vecinos(actual))
                {
                    double nuevaDistancia = distanciaActual + metros;
                    if (nuevaDistancia >= distancias[vecino])
                    {
                        continue;
                    }

                    distancias[vecino] = nuevaDistancia;
                    predecesores[vecino] = actual;
                    pendientes.Enqueue(vecino, nuevaDistancia);
                }
            }

            return new ResultadoDijkstra(origen, distancias, predecesores);
        }
    }
}
