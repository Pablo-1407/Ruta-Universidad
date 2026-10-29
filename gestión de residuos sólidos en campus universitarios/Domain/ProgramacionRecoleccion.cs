namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ProgramacionRecoleccion
    {
        public int Id { get; set; }
        public DateTimeOffset FechaProgramada { get; set; }
        public int CentroAcopioId { get; set; }
        public List<int> PuntosIds { get; set; } = [];
        public double VolumenEstimadoLitros { get; set; }
        public EstadoProgramacion Estado { get; set; } = EstadoProgramacion.Programada;
        public DateTimeOffset? FechaCompletada { get; set; }

        public void Validar()
        {
            if (Id <= 0 || CentroAcopioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Id), "Los identificadores deben ser mayores que cero.");
            }

            if (PuntosIds.Count == 0 || PuntosIds.Any(id => id <= 0) || PuntosIds.Distinct().Count() != PuntosIds.Count)
            {
                throw new ArgumentException("La programación debe incluir puntos válidos y no duplicados.", nameof(PuntosIds));
            }

            if (!double.IsFinite(VolumenEstimadoLitros) || VolumenEstimadoLitros < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(VolumenEstimadoLitros));
            }

            if (!Enum.IsDefined(Estado))
            {
                throw new ArgumentOutOfRangeException(nameof(Estado));
            }

            if (Estado == EstadoProgramacion.Completada && FechaCompletada is null)
            {
                throw new ArgumentException("Una programación completada debe registrar su fecha de finalización.");
            }
        }
    }
}
