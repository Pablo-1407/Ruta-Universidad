using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Data
{
    internal interface IPuntoRepository
    {
        CampusData Cargar(string rutaArchivo);
        void Guardar(CampusData campus, string rutaArchivo);
    }
}
