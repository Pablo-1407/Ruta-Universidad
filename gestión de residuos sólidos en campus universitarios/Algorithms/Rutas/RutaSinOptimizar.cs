using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal sealed class RutaSinOptimizar : IEstrategiaRuta
    {
        private readonly Dijkstra _dijkstra = new();

        public Ruta Calcular(
            GrafoCampus grafo,
            IReadOnlyList<PuntoRecoleccion> puntosCampus,
            PuntoRecoleccion centroAcopio,
            IReadOnlyList<PuntoRecoleccion> puntosAVisitar)
        {
            ArgumentNullException.ThrowIfNull(puntosCampus);
            ArgumentNullException.ThrowIfNull(puntosAVisitar);
            return ConstructorRuta.Crear(grafo, puntosCampus, centroAcopio, puntosAVisitar, _dijkstra);
        }
    }
}