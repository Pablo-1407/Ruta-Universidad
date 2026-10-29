namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ParametrosResiduo
    {
        public TipoResiduo Tipo { get; set; } = TipoResiduo.SinClasificar;
        public double DensidadKgPorLitro { get; set; }
        public double PorcentajeAprovechablePct { get; set; }
        public double FactorCO2eEvitadoKgPorKg { get; set; }
        public string FuenteDensidad { get; set; } = string.Empty;
        public string FuenteAprovechamiento { get; set; } = string.Empty;
        public string FuenteEmisiones { get; set; } = string.Empty;

        public void Validar()
        {
            if (!Enum.IsDefined(Tipo) || Tipo == TipoResiduo.SinClasificar)
            {
                throw new ArgumentException("Selecciona un tipo de residuo clasificado.", nameof(Tipo));
            }

            if (!double.IsFinite(DensidadKgPorLitro) || DensidadKgPorLitro <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(DensidadKgPorLitro), "La densidad debe ser mayor que cero.");
            }

            if (!double.IsFinite(PorcentajeAprovechablePct) || PorcentajeAprovechablePct is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(PorcentajeAprovechablePct), "El porcentaje debe estar entre 0 y 100.");
            }

            if (!double.IsFinite(FactorCO2eEvitadoKgPorKg) || FactorCO2eEvitadoKgPorKg < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(FactorCO2eEvitadoKgPorKg), "El factor de emisiones no puede ser negativo.");
            }

            if (string.IsNullOrWhiteSpace(FuenteDensidad)
                || string.IsNullOrWhiteSpace(FuenteAprovechamiento)
                || string.IsNullOrWhiteSpace(FuenteEmisiones))
            {
                throw new ArgumentException("Cada factor debe incluir su fuente documentada.");
            }
        }
    }
}
