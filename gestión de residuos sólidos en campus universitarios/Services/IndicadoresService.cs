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

        public double EstimarVolumenActualLitros(IEnumerable<PuntoRecoleccion> puntos)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            PuntoRecoleccion[] puntosRecoleccion = puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .ToArray();

            foreach (PuntoRecoleccion punto in puntosRecoleccion)
            {
                punto.ValidarDatosOperativos();
            }

            return puntosRecoleccion.Sum(p => p.CapacidadLitros * p.NivelLlenadoPct / 100);
        }

        public double EstimarVolumenGeneradoLitros(IEnumerable<PuntoRecoleccion> puntos, IEnumerable<ParametrosResiduo> parametrosResiduos)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            ArgumentNullException.ThrowIfNull(parametrosResiduos);

            Dictionary<TipoResiduo, ParametrosResiduo> parametros = parametrosResiduos
                .Where(p => p.Tipo != TipoResiduo.SinClasificar)
                .GroupBy(p => p.Tipo)
                .ToDictionary(g => g.Key, g =>
                {
                    if (g.Count() > 1)
                    {
                        throw new InvalidOperationException($"Hay parámetros duplicados para el residuo {g.Key}.");
                    }

                    return g.Single();
                });

            double volumen = 0;
            foreach (PuntoRecoleccion punto in puntos.Where(p => p.Tipo == TipoPunto.Recoleccion))
            {
                punto.ValidarDatosOperativos();
                if (!parametros.TryGetValue(punto.TipoResiduo, out ParametrosResiduo? parametrosResiduo))
                {
                    throw new InvalidOperationException($"Faltan parámetros documentados para el residuo {punto.TipoResiduo}.");
                }

                if (!double.IsFinite(parametrosResiduo.DensidadKgPorLitro)
                    || parametrosResiduo.DensidadKgPorLitro <= 0
                    || string.IsNullOrWhiteSpace(parametrosResiduo.FuenteDensidad))
                {
                    throw new InvalidOperationException($"La densidad del residuo {punto.TipoResiduo} debe ser positiva e incluir una fuente.");
                }

                double kilos = punto.GeneracionKgDia * punto.DiasSinRecoleccion;
                volumen += kilos / parametrosResiduo.DensidadKgPorLitro;
            }

            return volumen;
        }

        public double EstimarKilosAprovechables(IEnumerable<PuntoRecoleccion> puntos, IEnumerable<ParametrosResiduo> parametrosResiduos)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            Dictionary<TipoResiduo, ParametrosResiduo> parametros = ObtenerParametros(puntos, parametrosResiduos);

            return puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .Sum(p => p.GeneracionKgDia * p.DiasSinRecoleccion
                    * ValidarParametrosAprovechamiento(parametros, p.TipoResiduo).PorcentajeAprovechablePct / 100);
        }

        public double EstimarCO2eEvitadoKg(IEnumerable<PuntoRecoleccion> puntos, IEnumerable<ParametrosResiduo> parametrosResiduos)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            Dictionary<TipoResiduo, ParametrosResiduo> parametros = ObtenerParametros(puntos, parametrosResiduos);

            return puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .Sum(p => p.GeneracionKgDia * p.DiasSinRecoleccion
                    * ValidarParametrosEmisiones(parametros, p.TipoResiduo).PorcentajeAprovechablePct / 100
                    * ValidarParametrosEmisiones(parametros, p.TipoResiduo).FactorCO2eEvitadoKgPorKg);
        }

        public double? EstimarDiasHastaUmbral(
            IEnumerable<PuntoRecoleccion> puntos,
            IEnumerable<ParametrosResiduo> parametrosResiduos,
            double umbralPct)
        {
            ArgumentNullException.ThrowIfNull(puntos);
            ArgumentNullException.ThrowIfNull(parametrosResiduos);
            if (!double.IsFinite(umbralPct) || umbralPct is <= 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(umbralPct));
            }

            PuntoRecoleccion[] puntosRecoleccion = puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .ToArray();
            if (puntosRecoleccion.Length == 0
                || puntosRecoleccion.Any(p => p.TipoResiduo == TipoResiduo.SinClasificar || p.CapacidadLitros <= 0))
            {
                return null;
            }

            foreach (PuntoRecoleccion punto in puntosRecoleccion)
            {
                punto.ValidarDatosOperativos();
            }

            double[] dias;
            try
            {
                Dictionary<TipoResiduo, ParametrosResiduo> parametros = ObtenerParametros(puntosRecoleccion, parametrosResiduos);
                dias = puntosRecoleccion
                    .Select(p =>
                    {
                        if (p.NivelLlenadoPct >= umbralPct)
                        {
                            return 0;
                        }

                        double densidad = ValidarParametrosDensidad(parametros, p.TipoResiduo).DensidadKgPorLitro;
                        double tasaPctDia = p.GeneracionKgDia / densidad / p.CapacidadLitros * 100;
                        return tasaPctDia > 0 ? (umbralPct - p.NivelLlenadoPct) / tasaPctDia : double.PositiveInfinity;
                    })
                    .ToArray();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return null;
            }

            double minimo = dias.Min();
            return double.IsFinite(minimo) ? minimo : null;
        }

        public double CalcularUtilizacionVehiculoPct(double volumenLitros, double capacidadLitros, int viajes)
        {
            if (!double.IsFinite(volumenLitros) || volumenLitros < 0
                || !double.IsFinite(capacidadLitros) || capacidadLitros <= 0
                || viajes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(volumenLitros));
            }

            return volumenLitros / (capacidadLitros * viajes) * 100;
        }

        public double CalcularDuracionDesplazamientoMinutos(double distanciaMetros, double velocidadKmHora)
        {
            if (!double.IsFinite(distanciaMetros) || distanciaMetros < 0
                || !double.IsFinite(velocidadKmHora) || velocidadKmHora <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distanciaMetros));
            }

            return distanciaMetros / 1000 / velocidadKmHora * 60;
        }

        public double CalcularCosto(double distanciaMetros, double duracionMinutos, ParametrosOperacion parametros)
        {
            ArgumentNullException.ThrowIfNull(parametros);
            parametros.Validar();
            return distanciaMetros / 1000 * parametros.CostoPorKm
                + duracionMinutos / 60 * parametros.CostoPorHora;
        }

        private static Dictionary<TipoResiduo, ParametrosResiduo> ObtenerParametros(
            IEnumerable<PuntoRecoleccion> puntos,
            IEnumerable<ParametrosResiduo> parametrosResiduos)
        {
            ArgumentNullException.ThrowIfNull(parametrosResiduos);
            HashSet<TipoResiduo> tiposNecesarios = puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .Select(p => p.TipoResiduo)
                .ToHashSet();
            return parametrosResiduos
                .Where(p => tiposNecesarios.Contains(p.Tipo))
                .GroupBy(p => p.Tipo)
                .ToDictionary(g => g.Key, g => g.Count() == 1
                    ? g.Single()
                    : throw new InvalidOperationException($"Hay parámetros duplicados para el residuo {g.Key}."));
        }

        private static ParametrosResiduo ValidarParametrosDensidad(
            IReadOnlyDictionary<TipoResiduo, ParametrosResiduo> parametros,
            TipoResiduo tipo)
        {
            ParametrosResiduo parametro = ObtenerParametro(parametros, tipo);
            parametro.ValidarDensidad();
            return parametro;
        }

        private static ParametrosResiduo ValidarParametrosAprovechamiento(
            IReadOnlyDictionary<TipoResiduo, ParametrosResiduo> parametros,
            TipoResiduo tipo)
        {
            ParametrosResiduo parametro = ObtenerParametro(parametros, tipo);
            parametro.ValidarAprovechamiento();
            return parametro;
        }

        private static ParametrosResiduo ValidarParametrosEmisiones(
            IReadOnlyDictionary<TipoResiduo, ParametrosResiduo> parametros,
            TipoResiduo tipo)
        {
            ParametrosResiduo parametro = ObtenerParametro(parametros, tipo);
            parametro.ValidarEmisiones();
            return parametro;
        }

        private static ParametrosResiduo ObtenerParametro(
            IReadOnlyDictionary<TipoResiduo, ParametrosResiduo> parametros,
            TipoResiduo tipo)
        {
            if (!parametros.TryGetValue(tipo, out ParametrosResiduo? parametro))
            {
                throw new InvalidOperationException($"Faltan parámetros documentados para el residuo {tipo}.");
            }

            return parametro;
        }
    }
}
