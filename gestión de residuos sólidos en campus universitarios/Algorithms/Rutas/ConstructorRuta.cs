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

        public static Ruta CrearConCapacidad(
            GrafoCampus grafo,
            IReadOnlyList<PuntoRecoleccion> puntosCampus,
            PuntoRecoleccion centroAcopio,
            IReadOnlyList<PuntoRecoleccion> puntosVisitados,
            double capacidadVehiculoLitros,
            Dijkstra dijkstra)
        {
            if (!double.IsFinite(capacidadVehiculoLitros) || capacidadVehiculoLitros <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacidadVehiculoLitros), "La capacidad del vehículo debe ser mayor que cero.");
            }

            foreach (PuntoRecoleccion punto in puntosVisitados)
            {
                punto.ValidarDatosOperativos();
                double volumen = punto.CapacidadLitros * punto.NivelLlenadoPct / 100;
                if (volumen > capacidadVehiculoLitros)
                {
                    throw new InvalidOperationException(
                        $"El volumen del contenedor «{punto.Nombre}» supera la capacidad del vehículo. Ajusta la configuración o divide la carga.");
                }
            }

            Dictionary<int, PuntoRecoleccion> puntosPorId = puntosCampus.ToDictionary(punto => punto.Id);
            List<PuntoRecoleccion> recorrido = [centroAcopio];
            double distanciaTotal = 0;
            double cargaActualLitros = 0;
            int actual = centroAcopio.Id;

            foreach (PuntoRecoleccion destino in puntosVisitados)
            {
                double volumenDestino = destino.CapacidadLitros * destino.NivelLlenadoPct / 100;
                if (cargaActualLitros > 0 && cargaActualLitros + volumenDestino > capacidadVehiculoLitros)
                {
                    distanciaTotal += AgregarCamino(grafo, puntosPorId, recorrido, actual, centroAcopio.Id, dijkstra);
                    actual = centroAcopio.Id;
                    cargaActualLitros = 0;
                }

                distanciaTotal += AgregarCamino(grafo, puntosPorId, recorrido, actual, destino.Id, dijkstra);
                actual = destino.Id;
                cargaActualLitros += volumenDestino;

                if (cargaActualLitros >= capacidadVehiculoLitros)
                {
                    distanciaTotal += AgregarCamino(grafo, puntosPorId, recorrido, actual, centroAcopio.Id, dijkstra);
                    actual = centroAcopio.Id;
                    cargaActualLitros = 0;
                }
            }

            if (actual != centroAcopio.Id)
            {
                distanciaTotal += AgregarCamino(grafo, puntosPorId, recorrido, actual, centroAcopio.Id, dijkstra);
            }

            return new Ruta(recorrido, puntosVisitados, distanciaTotal);
        }

        private static double AgregarCamino(
            GrafoCampus grafo,
            IReadOnlyDictionary<int, PuntoRecoleccion> puntosPorId,
            ICollection<PuntoRecoleccion> recorrido,
            int origenId,
            int destinoId,
            Dijkstra dijkstra)
        {
            ResultadoDijkstra resultado = dijkstra.Calcular(grafo, origenId);
            double distancia = resultado.DistanciaHasta(destinoId);
            IReadOnlyList<int> camino = resultado.CaminoHasta(destinoId);
            if (!double.IsFinite(distancia) || camino.Count == 0)
            {
                throw new InvalidOperationException($"No existe un camino conectado entre los puntos {origenId} y {destinoId}.");
            }

            foreach (int id in camino.Skip(1))
            {
                if (!puntosPorId.TryGetValue(id, out PuntoRecoleccion? punto))
                {
                    throw new InvalidOperationException($"El nodo {id} no tiene un punto asociado.");
                }

                recorrido.Add(punto);
            }

            return distancia;
        }
    }
}