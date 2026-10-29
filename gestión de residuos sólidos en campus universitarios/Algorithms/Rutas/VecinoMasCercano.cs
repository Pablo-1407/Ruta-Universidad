using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal sealed class VecinoMasCercano : IEstrategiaRuta
    {
        private readonly Dijkstra _dijkstra = new();

        public Ruta Calcular(
            GrafoCampus grafo,
            IReadOnlyList<PuntoRecoleccion> puntosCampus,
            PuntoRecoleccion centroAcopio,
            IReadOnlyList<PuntoRecoleccion> puntosAVisitar)
        {
            ArgumentNullException.ThrowIfNull(grafo);
            ArgumentNullException.ThrowIfNull(puntosCampus);
            ArgumentNullException.ThrowIfNull(centroAcopio);
            ArgumentNullException.ThrowIfNull(puntosAVisitar);

            List<PuntoRecoleccion> pendientes = puntosAVisitar.ToList();
            List<PuntoRecoleccion> ordenVisita = [];
            int actual = centroAcopio.Id;

            while (pendientes.Count > 0)
            {
                ResultadoDijkstra distancias = _dijkstra.Calcular(grafo, actual);
                PuntoRecoleccion? siguiente = pendientes
                    .Select(punto => new { Punto = punto, Distancia = distancias.DistanciaHasta(punto.Id) })
                    .Where(candidato => double.IsFinite(candidato.Distancia))
                    .OrderBy(candidato => candidato.Distancia)
                    .ThenBy(candidato => candidato.Punto.Id)
                    .Select(candidato => candidato.Punto)
                    .FirstOrDefault();

                if (siguiente is null)
                {
                    throw new InvalidOperationException("Hay puntos seleccionados que no están conectados al centro de acopio.");
                }

                ordenVisita.Add(siguiente);
                pendientes.Remove(siguiente);
                actual = siguiente.Id;
            }

            return ConstructorRuta.Crear(grafo, puntosCampus, centroAcopio, ordenVisita, _dijkstra);
        }
    }
}
