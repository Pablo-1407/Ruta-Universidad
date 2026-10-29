namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class RegistroHistorialRecoleccion
    {
        public int Id { get; set; }
        public DateTimeOffset FechaCompletada { get; set; }
        public int CentroAcopioId { get; set; }
        public List<int> PuntosIds { get; set; } = [];
        public double VolumenLitros { get; set; }
        public double KilosRecolectados { get; set; }
        public double DistanciaMetros { get; set; }
        public double DuracionMinutos { get; set; }
        public double Costo { get; set; }
        public double KilosAprovechados { get; set; }
        public double CO2eEvitadoKg { get; set; }

        public void Validar()
        {
            if (Id <= 0 || CentroAcopioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Id), "Los identificadores deben ser mayores que cero.");
            }

            if (PuntosIds.Count == 0 || PuntosIds.Any(id => id <= 0) || PuntosIds.Distinct().Count() != PuntosIds.Count)
            {
                throw new ArgumentException("El historial debe incluir puntos válidos y no duplicados.", nameof(PuntosIds));
            }

            ValidarMetrica(VolumenLitros, nameof(VolumenLitros));
            ValidarMetrica(KilosRecolectados, nameof(KilosRecolectados));
            ValidarMetrica(DistanciaMetros, nameof(DistanciaMetros));
            ValidarMetrica(DuracionMinutos, nameof(DuracionMinutos));
            ValidarMetrica(Costo, nameof(Costo));
            ValidarMetrica(KilosAprovechados, nameof(KilosAprovechados));
            ValidarMetrica(CO2eEvitadoKg, nameof(CO2eEvitadoKg));

            if (KilosAprovechados > KilosRecolectados)
            {
                throw new ArgumentException("Los kilos aprovechados no pueden superar los kilos recolectados.");
            }
        }

        private static void ValidarMetrica(double valor, string nombre)
        {
            if (!double.IsFinite(valor) || valor < 0)
            {
                throw new ArgumentOutOfRangeException(nombre, "La métrica no puede ser negativa ni infinita.");
            }
        }
    }
}
