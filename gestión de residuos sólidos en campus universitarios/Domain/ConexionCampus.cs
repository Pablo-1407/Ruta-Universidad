using System.Text.Json.Serialization;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class ConexionCampus
    {
        public ConexionCampus()
        {
        }

        [JsonPropertyName("origen")]
        public int OrigenId { get; set; }

        [JsonPropertyName("destino")]
        public int DestinoId { get; set; }

        public double Metros { get; set; }
    }
}