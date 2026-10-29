namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ParametrosOperacion
    {
        public double CapacidadVehiculoLitros { get; set; }
        public double VelocidadMediaKmHora { get; set; }
        public double CostoPorKm { get; set; }
        public double CostoPorHora { get; set; }
        public double MinutosServicioPorContenedor { get; set; }
        public double MinutosDescarga { get; set; }
        public double UmbralProgramacionPct { get; set; } = 80;

        public void Validar()
        {
            ValidarPositivo(CapacidadVehiculoLitros, nameof(CapacidadVehiculoLitros));
            ValidarPositivo(VelocidadMediaKmHora, nameof(VelocidadMediaKmHora));
            ValidarNoNegativo(CostoPorKm, nameof(CostoPorKm));
            ValidarNoNegativo(CostoPorHora, nameof(CostoPorHora));
            ValidarNoNegativo(MinutosServicioPorContenedor, nameof(MinutosServicioPorContenedor));
            ValidarNoNegativo(MinutosDescarga, nameof(MinutosDescarga));

            if (!double.IsFinite(UmbralProgramacionPct) || UmbralProgramacionPct is <= 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(UmbralProgramacionPct), "El umbral debe estar entre 0 y 100.");
            }
        }

        private static void ValidarPositivo(double valor, string nombre)
        {
            if (!double.IsFinite(valor) || valor <= 0)
            {
                throw new ArgumentOutOfRangeException(nombre, "El valor debe ser mayor que cero.");
            }
        }

        private static void ValidarNoNegativo(double valor, string nombre)
        {
            if (!double.IsFinite(valor) || valor < 0)
            {
                throw new ArgumentOutOfRangeException(nombre, "El valor no puede ser negativo.");
            }
        }
    }
}
