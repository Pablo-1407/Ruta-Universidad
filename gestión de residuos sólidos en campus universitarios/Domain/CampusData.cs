namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class CampusData
    {
        public CampusData()
        {
        }

        public List<PuntoRecoleccion> Puntos { get; set; } = [];
        public List<ConexionCampus> Conexiones { get; set; } = [];
        public string? RutaImagenMapa { get; set; }
    }
}