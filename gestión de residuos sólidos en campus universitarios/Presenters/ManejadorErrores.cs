using System.Diagnostics;
using System.Text;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Presenters
{
    internal sealed class ManejadorErrores
    {
        private readonly Action<string, bool> _mostrarMensaje;

        public ManejadorErrores(Action<string, bool> mostrarMensaje)
        {
            ArgumentNullException.ThrowIfNull(mostrarMensaje);
            _mostrarMensaje = mostrarMensaje;
        }

        public void Manejar(Exception excepcion, string operacion, bool registrarSiempre = false)
        {
            ArgumentNullException.ThrowIfNull(excepcion);

            string nombreOperacion = string.IsNullOrWhiteSpace(operacion) ? "la operación" : operacion;
            string? mensaje = registrarSiempre ? null : ObtenerMensajeConocido(excepcion);
            if (mensaje is not null)
            {
                Mostrar($"No se pudo completar {nombreOperacion}: {mensaje}");
                return;
            }

            string referencia = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            string? rutaRegistro = Registrar(excepcion, nombreOperacion, referencia);
            string detalleRegistro = rutaRegistro is null
                ? "No se pudo guardar el detalle técnico."
                : $"Detalle registrado en {rutaRegistro}.";
            Mostrar($"Ocurrió un error inesperado al completar {nombreOperacion}. Referencia: {referencia}. {detalleRegistro}");
        }

        private static string? ObtenerMensajeConocido(Exception excepcion) => excepcion switch
        {
            ArgumentNullException => null,
            ArgumentOutOfRangeException rango when rango.ParamName is nameof(ParametrosOperacion.CapacidadVehiculoLitros)
                or nameof(ParametrosOperacion.VelocidadMediaKmHora)
                => "Registra correctamente un vehículo en Configuración: la capacidad y la velocidad media deben ser mayores que cero.",
            ArgumentException argumento when argumento.ParamName is nameof(PuntoRecoleccion.TipoResiduo)
                => "Edita el punto de recolección y selecciona un tipo de residuo clasificado.",
            ArgumentOutOfRangeException => excepcion.Message,
            InvalidDataException => excepcion.Message,
            ArgumentException => excepcion.Message,
            InvalidOperationException => excepcion.Message,
            OperationCanceledException => "La operación fue cancelada.",
            IOException => $"No se pudo acceder al archivo. {excepcion.Message}",
            UnauthorizedAccessException => $"No hay permisos suficientes. {excepcion.Message}",
            NotSupportedException => excepcion.Message,
            System.Security.SecurityException => excepcion.Message,
            _ => null
        };

        private void Mostrar(string mensaje)
        {
            try
            {
                _mostrarMensaje(mensaje, true);
            }
            catch (Exception errorPresentacion)
            {
                Trace.TraceError($"No se pudo mostrar el error al usuario: {errorPresentacion}");
            }
        }

        private static string? Registrar(Exception excepcion, string operacion, string referencia)
        {
            try
            {
                string directorio = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Ruta-Universidad");
                Directory.CreateDirectory(directorio);
                string ruta = Path.Combine(directorio, "errores.txt");
                string entrada = $"{DateTimeOffset.Now:O}\t{referencia}\t{operacion}{Environment.NewLine}{excepcion}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(ruta, entrada, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return ruta;
            }
            catch (Exception errorRegistro)
            {
                Trace.TraceError($"No se pudo registrar la excepción {referencia}: {errorRegistro}");
                return null;
            }
        }
    }
}
