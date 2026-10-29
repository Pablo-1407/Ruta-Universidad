namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class PuntoRecoleccion
    {
        public PuntoRecoleccion()
        {
        }

        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public TipoPunto Tipo { get; set; } = TipoPunto.Recoleccion;
        public TipoResiduo TipoResiduo { get; set; } = TipoResiduo.SinClasificar;
        public double X { get; set; }
        public double Y { get; set; }
        public double CapacidadLitros { get; set; }
        public double NivelLlenadoPct { get; set; }
        public double GeneracionKgDia { get; set; }
        public int DiasSinRecoleccion { get; set; }
        public double PesoZona { get; set; }

        public void ValidarDatosOperativos()
        {
            if (Tipo == TipoPunto.CentroAcopio)
            {
                return;
            }

            if (!Enum.IsDefined(TipoResiduo) || TipoResiduo == TipoResiduo.SinClasificar)
            {
                throw new ArgumentException("El punto debe tener un tipo de residuo clasificado.", nameof(TipoResiduo));
            }

            if (!double.IsFinite(CapacidadLitros) || CapacidadLitros <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(CapacidadLitros), "La capacidad debe ser mayor que cero.");
            }

            if (!double.IsFinite(NivelLlenadoPct) || NivelLlenadoPct is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(NivelLlenadoPct), "El nivel de llenado debe estar entre 0 y 100.");
            }

            if (!double.IsFinite(GeneracionKgDia) || GeneracionKgDia < 0 || DiasSinRecoleccion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(GeneracionKgDia), "La generación y los días sin recolección no pueden ser negativos.");
            }
        }

        public override string ToString() => Tipo == TipoPunto.CentroAcopio
            ? $"{Nombre} (Centro de acopio)"
            : Nombre;
    }
}
