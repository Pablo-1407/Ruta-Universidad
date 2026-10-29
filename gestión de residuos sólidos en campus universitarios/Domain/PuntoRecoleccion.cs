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
        public double X { get; set; }
        public double Y { get; set; }
        public double NivelLlenadoPct { get; set; }
        public double GeneracionKgDia { get; set; }
        public int DiasSinRecoleccion { get; set; }
        public double PesoZona { get; set; }

        public override string ToString() => Tipo == TipoPunto.CentroAcopio
            ? $"{Nombre} (Centro de acopio)"
            : Nombre;
    }
}
