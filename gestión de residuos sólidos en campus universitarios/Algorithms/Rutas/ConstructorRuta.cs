using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal static class ConstructorRuta
    {
        public static Ruta Crear(
            GrafoCampus grafo,
            IReadOnlyList<PuntoRecoleccion> puntosCampus,
            PuntoRecoleccion centroAcopio,
            IReadOnlyList<PuntoRecoleccion> puntosVisitados,
            Dijkstra dijkstra)
        {
            ArgumentNullException.ThrowIfNull(grafo);
            ArgumentNullException.ThrowIfNull(puntosCampus);
            ArgumentNullException.ThrowIfNull(centroAcopio);
            ArgumentNullException.ThrowIfNull(puntosVisitados);
            ArgumentNullException.ThrowIfNull(dijkstra);

            Dictionary<int, PuntoRecoleccion> puntosPorId = puntosCampus.ToDictionary(punto => punto.Id);

            List<PuntoRecoleccion> recorrido = [centroAcopio];
            double distanciaTotal = 0;
            int actual = centroAcopio.Id;

            foreach (PuntoRecoleccion destino in puntosVisitados.Append(centroAcopio))
            {
                ResultadoDijkstra resultado = dijkstra.Calcular(grafo, actual);
                double distancia = resultado.DistanciaHasta(destino.Id);
                IReadOnlyList<int> camino = resultado.CaminoHasta(destino.Id);
                if (!double.IsFinite(distancia) || camino.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No existe un camino conectado entre los puntos {actual} y {destino.Id}.");
                }

                foreach (int id in camino.Skip(1))
                {
                    if (!puntosPorId.TryGetValue(id, out PuntoRecoleccion? punto))
                    {
                        throw new InvalidOperationException($"El nodo {id} no tiene un punto asociado.");
                    }

                    recorrido.Add(punto);
                }

                distanciaTotal += distancia;
                actual = destino.Id;
            }

            return new Ruta(recorrido, puntosVisitados, distanciaTotal);
        }
    }
}