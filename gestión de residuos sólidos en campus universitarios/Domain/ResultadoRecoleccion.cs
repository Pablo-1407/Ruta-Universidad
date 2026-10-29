namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ResultadoRecoleccion
    {
        public IReadOnlyList<PuntoPriorizado> PuntosPrioritarios { get; init; } = [];
        public Ruta RutaOptimizada { get; init; } = new([], [], 0);
        public Ruta RutaBase { get; init; } = new([], [], 0);
        public double AhorroPorcentual { get; init; }
        public double KilosEstimados { get; init; }
    }
}