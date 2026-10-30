namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ResultadoRecoleccion
    {
        public IReadOnlyList<PuntoPriorizado> PuntosPrioritarios { get; init; } = [];
        public Ruta RutaOptimizada { get; init; } = new([], [], 0);
        public Ruta RutaBase { get; init; } = new([], [], 0);
        public double AhorroPorcentual { get; init; }
        public double KilosEstimados { get; init; }
        public double VolumenEstimadoLitros { get; init; }
        public double VolumenGeneradoEstimadoLitros { get; init; }
        public double? VolumenGeneradoEstimadoLitrosDisponible { get; init; }
        public double UtilizacionVehiculoPct { get; init; }
        public double DuracionDesplazamientoMinutos { get; init; }
        public double DuracionServicioMinutos { get; init; }
        public double DuracionTotalMinutos { get; init; }
        public double CostoEstimado { get; init; }
        public double KilosAprovechables { get; init; }
        public double? KilosAprovechablesDisponibles { get; init; }
        public double CO2eEvitadoKg { get; init; }
        public double? CO2eEvitadoKgDisponible { get; init; }
        public double? DiasHastaUmbralEstimados { get; init; }
    }
}