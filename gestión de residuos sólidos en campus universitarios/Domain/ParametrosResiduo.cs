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

        public void ValidarDensidad()
        {
            ValidarTipo();
            if (!double.IsFinite(DensidadKgPorLitro) || DensidadKgPorLitro <= 0
                || string.IsNullOrWhiteSpace(FuenteDensidad))
            {
                throw new ArgumentException("La densidad debe ser positiva e incluir una fuente documentada.");
            }
        }

        public void ValidarAprovechamiento()
        {
            ValidarTipo();
            if (!double.IsFinite(PorcentajeAprovechablePct) || PorcentajeAprovechablePct is < 0 or > 100
                || string.IsNullOrWhiteSpace(FuenteAprovechamiento))
            {
                throw new ArgumentException("El porcentaje de aprovechamiento debe estar entre 0 y 100 e incluir una fuente.");
            }
        }

        public void ValidarEmisiones()
        {
            ValidarAprovechamiento();
            if (!double.IsFinite(FactorCO2eEvitadoKgPorKg) || FactorCO2eEvitadoKgPorKg < 0
                || string.IsNullOrWhiteSpace(FuenteEmisiones))
            {
                throw new ArgumentException("El factor de emisiones no puede ser negativo y debe incluir una fuente.");
            }
        }

        public void Validar()
        {
            ValidarDensidad();
            ValidarAprovechamiento();
            ValidarEmisiones();
        }

        private void ValidarTipo()
        {
            if (!Enum.IsDefined(Tipo) || Tipo == TipoResiduo.SinClasificar)
            {
                throw new ArgumentException("Selecciona un tipo de residuo clasificado.", nameof(Tipo));
            }
        }
    }
}
