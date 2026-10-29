using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Services
{
    internal sealed class ProgramacionService
    {
        public IReadOnlyList<ProgramacionRecoleccion> GenerarProgramacion(CampusData campus, DateTimeOffset ahora)
        {
            ArgumentNullException.ThrowIfNull(campus);
            campus.ParametrosOperacion.Validar();

            PuntoRecoleccion[] centros = campus.Puntos
                .Where(p => p.Tipo == TipoPunto.CentroAcopio)
                .ToArray();
            if (centros.Length != 1)
            {
                throw new InvalidOperationException("Registra exactamente un centro de acopio antes de programar recolecciones.");
            }

            PriorityQueue<PuntoRecoleccion, (double DiasHastaUmbral, int Id)> cola = new();
            int nextId = campus.Programaciones.Count == 0 ? 1 : campus.Programaciones.Max(p => p.Id) + 1;
            foreach (PuntoRecoleccion punto in campus.Puntos.Where(p => p.Tipo == TipoPunto.Recoleccion))
            {
                punto.ValidarDatosOperativos();
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
            parametros.Validar();
            return parametros.DensidadKgPorLitro;
        }
    }
}
