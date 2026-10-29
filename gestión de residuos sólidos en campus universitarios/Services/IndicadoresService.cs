using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Services
{
    internal sealed class IndicadoresService
    {
        public double CalcularAhorroPorcentual(Ruta rutaBase, Ruta rutaOptimizada)
        {
            ArgumentNullException.ThrowIfNull(rutaBase);
            ArgumentNullException.ThrowIfNull(rutaOptimizada);

            if (rutaBase.DistanciaTotal == 0)
            {
                return 0;
            }

            return (rutaBase.DistanciaTotal - rutaOptimizada.DistanciaTotal)
                / rutaBase.DistanciaTotal * 100;
        }

        public double EstimarKilos(IEnumerable<PuntoRecoleccion> puntos)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            return puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .Sum(p => p.GeneracionKgDia * p.DiasSinRecoleccion);
        }
    }
}
