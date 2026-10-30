using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Data
{
    internal interface IPuntoRepository
    {
        CampusData Cargar(string rutaArchivo);
        Task<CampusData> CargarAsync(string rutaArchivo, CancellationToken cancellationToken = default);
        void Guardar(CampusData campus, string rutaArchivo);
        Task GuardarAsync(CampusData campus, string rutaArchivo, CancellationToken cancellationToken = default);
    }
}
