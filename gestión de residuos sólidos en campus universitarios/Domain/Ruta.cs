namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class Ruta
    {
        public Ruta(
            IEnumerable<PuntoRecoleccion> puntosRecorrido,
            IEnumerable<PuntoRecoleccion> puntosVisitados,
            double distanciaTotal)
        {
            ArgumentNullException.ThrowIfNull(puntosRecorrido);
            ArgumentNullException.ThrowIfNull(puntosVisitados);
            if (!double.IsFinite(distanciaTotal) || distanciaTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distanciaTotal));
            }

            PuntosRecorrido = puntosRecorrido.ToArray();
            PuntosVisitados = puntosVisitados.ToArray();
            DistanciaTotal = distanciaTotal;
        }

        public IReadOnlyList<PuntoRecoleccion> PuntosRecorrido { get; }
        public IReadOnlyList<PuntoRecoleccion> PuntosVisitados { get; }
        public double DistanciaTotal { get; }
    }
}
