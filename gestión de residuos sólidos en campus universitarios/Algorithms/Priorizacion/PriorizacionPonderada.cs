using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Priorizacion
{
    internal sealed class PriorizacionPonderada : IEstrategiaPriorizacion
    {
        public IReadOnlyList<PuntoPriorizado> Priorizar(IEnumerable<PuntoRecoleccion> puntos)
        {
            ArgumentNullException.ThrowIfNull(puntos);

            PuntoRecoleccion[] puntosRecoleccion = puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .ToArray();
            if (puntosRecoleccion.Length == 0)
            {
                return [];
            }

            double maxGeneracion = puntosRecoleccion.Max(p => p.GeneracionKgDia);
            int maxDias = puntosRecoleccion.Max(p => p.DiasSinRecoleccion);
            double maxPesoZona = puntosRecoleccion.Max(p => p.PesoZona);

            return puntosRecoleccion
                .Select(punto => new PuntoPriorizado(
                    punto,
                    0.4 * punto.NivelLlenadoPct / 100
                    + 0.3 * Normalizar(punto.GeneracionKgDia, maxGeneracion)
                    + 0.2 * Normalizar(punto.DiasSinRecoleccion, maxDias)
                    + 0.1 * Normalizar(punto.PesoZona, maxPesoZona)))
                .OrderByDescending(p => p.Puntaje)
                .ThenBy(p => p.Punto.Id)
                .ToArray();
        }

        private static double Normalizar(double valor, double maximo) => maximo == 0 ? 0 : valor / maximo;
    }
}
