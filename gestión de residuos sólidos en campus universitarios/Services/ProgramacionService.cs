using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Services
{
    internal sealed class ProgramacionService
    {
        public IReadOnlyList<ProgramacionRecoleccion> GenerarProgramacion(CampusData campus, DateTimeOffset ahora)
        {
            ArgumentNullException.ThrowIfNull(campus);
            campus.ParametrosOperacion.ValidarParaProgramacion();

            PuntoRecoleccion[] centros = campus.Puntos
                .Where(p => p.Tipo == TipoPunto.CentroAcopio)
                .ToArray();
            if (centros.Length != 1)
            {
                throw new InvalidOperationException("Registra exactamente un centro de acopio antes de programar recolecciones.");
            }

            PuntoRecoleccion[] puntosRecoleccion = campus.Puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .ToArray();
            foreach (PuntoRecoleccion punto in puntosRecoleccion)
            {
                punto.ValidarDatosOperativos();
            }

            ValidarDensidadesNecesarias(campus, puntosRecoleccion);

            PriorityQueue<PuntoRecoleccion, (double DiasHastaUmbral, int Id)> cola = new();
            int nextId = campus.Programaciones.Count == 0 ? 1 : campus.Programaciones.Max(p => p.Id) + 1;
            foreach (PuntoRecoleccion punto in puntosRecoleccion)
            {
                if (punto.NivelLlenadoPct >= campus.ParametrosOperacion.UmbralProgramacionPct)
                {
                    cola.Enqueue(punto, (0, punto.Id));
                    continue;
                }
                if (punto.GeneracionKgDia == 0)
                {
                    continue;
                }
                double tasaLlenadoPctDia = punto.CapacidadLitros == 0
                    ? 0
                    : punto.GeneracionKgDia / ObtenerDensidad(campus, punto.TipoResiduo) / punto.CapacidadLitros * 100;
                double margenPct = Math.Max(0, campus.ParametrosOperacion.UmbralProgramacionPct - punto.NivelLlenadoPct);
                double diasHastaUmbral = tasaLlenadoPctDia > 0 ? margenPct / tasaLlenadoPctDia : double.PositiveInfinity;
                cola.Enqueue(punto, (diasHastaUmbral, punto.Id));
            }

            List<ProgramacionRecoleccion> resultado = [];
            while (cola.TryDequeue(out PuntoRecoleccion? punto, out (double DiasHastaUmbral, int Id) prioridad))
            {
                if (double.IsPositiveInfinity(prioridad.DiasHastaUmbral))
                {
                    continue;
                }

                double dias = Math.Max(0, prioridad.DiasHastaUmbral);
                double volumenEstimado = Math.Min(
                    punto.CapacidadLitros,
                    punto.CapacidadLitros * punto.NivelLlenadoPct / 100
                    + punto.GeneracionKgDia / ObtenerDensidad(campus, punto.TipoResiduo) * dias);
                resultado.Add(new ProgramacionRecoleccion
                {
                    Id = nextId,
                    FechaProgramada = ahora.AddDays(dias),
                    CentroAcopioId = centros[0].Id,
                    PuntosIds = [punto.Id],
                    VolumenEstimadoLitros = volumenEstimado
                });
                nextId++;
            }

            return resultado;
        }

        public void GuardarProgramacion(CampusData campus, IReadOnlyList<ProgramacionRecoleccion> programaciones)
        {
            ArgumentNullException.ThrowIfNull(campus);
            ArgumentNullException.ThrowIfNull(programaciones);

            campus.Programaciones.RemoveAll(p => p.Estado == EstadoProgramacion.Programada);
            foreach (ProgramacionRecoleccion programacion in programaciones)
            {
                programacion.Validar();
                campus.Programaciones.Add(programacion);
            }
        }

        private static void ValidarDensidadesNecesarias(CampusData campus, IEnumerable<PuntoRecoleccion> puntos)
        {
            TipoResiduo[] tiposNecesarios = puntos
                .Where(p => p.NivelLlenadoPct >= campus.ParametrosOperacion.UmbralProgramacionPct
                    || p.GeneracionKgDia > 0)
                .Select(p => p.TipoResiduo)
                .Distinct()
                .ToArray();
            List<ProblemaDensidadResiduo> problemas = [];

            foreach (TipoResiduo tipo in tiposNecesarios)
            {
                ParametrosResiduo[] coincidencias = campus.ParametrosResiduos
                    .Where(p => p.Tipo == tipo)
                    .ToArray();
                if (coincidencias.Length == 0)
                {
                    problemas.Add(new ProblemaDensidadResiduo(tipo,
                    [
                        "crear una única configuración para este residuo",
                        "indicar una densidad positiva en kg/l y su fuente documental"
                    ]));
                    continue;
                }

                if (coincidencias.Length > 1)
                {
                    problemas.Add(new ProblemaDensidadResiduo(tipo,
                    [
                        $"hay {coincidencias.Length} configuraciones; debe quedar una sola"
                    ]));
                    continue;
                }

                ParametrosResiduo parametros = coincidencias[0];
                List<string> informacionPendiente = [];
                if (!double.IsFinite(parametros.DensidadKgPorLitro) || parametros.DensidadKgPorLitro <= 0)
                {
                    informacionPendiente.Add("indicar una densidad positiva y finita en kg/l");
                }

                if (string.IsNullOrWhiteSpace(parametros.FuenteDensidad))
                {
                    informacionPendiente.Add("añadir la fuente documental de la densidad");
                }

                if (informacionPendiente.Count > 0)
                {
                    problemas.Add(new ProblemaDensidadResiduo(tipo, informacionPendiente));
                }
            }

            if (problemas.Count > 0)
            {
                throw new DensidadesResiduosIncompletasException(problemas);
            }
        }

        private static double ObtenerDensidad(CampusData campus, TipoResiduo tipo)
        {
            ParametrosResiduo[] coincidencias = campus.ParametrosResiduos
                .Where(p => p.Tipo == tipo)
                .ToArray();
            if (tipo == TipoResiduo.SinClasificar || coincidencias.Length != 1)
            {
                throw new InvalidOperationException($"Debe configurarse una densidad documentada única para el residuo {tipo}.");
            }

            ParametrosResiduo parametros = coincidencias[0];
            parametros.ValidarDensidad();
            return parametros.DensidadKgPorLitro;
        }
    }
}
