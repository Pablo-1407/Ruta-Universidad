using System.Globalization;
using System.Text;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Data
{
    internal sealed class TxtCampusRepository : IPuntoRepository
    {
        private const string Cabecera = "RUTAS_RESIDUOS_CAMPUS\t1";

        public CampusData Cargar(string rutaArchivo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivo);

            string rutaCompleta = Path.GetFullPath(rutaArchivo);
            string[] lineas = File.ReadAllLines(rutaCompleta);
            if (lineas.Length == 0 || lineas[0] != Cabecera)
            {
                throw new InvalidDataException("El archivo no tiene el formato de campus esperado.");
            }

            CampusData campus = new();
            string? nombreImagen = null;
            Seccion seccion = Seccion.Metadatos;
            bool tieneSeccionPuntos = false;
            bool tieneSeccionConexiones = false;

            for (int indice = 1; indice < lineas.Length; indice++)
            {
                string linea = lineas[indice];
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith('#'))
                {
                    continue;
                }

                if (linea == "PUNTOS")
                {
                    seccion = Seccion.Puntos;
                    tieneSeccionPuntos = true;
                    continue;
                }

                if (linea == "CONEXIONES")
                {
                    seccion = Seccion.Conexiones;
                    tieneSeccionConexiones = true;
                    continue;
                }

                string[] campos = linea.Split('\t');
                if (seccion == Seccion.Metadatos && campos.Length == 2 && campos[0] == "IMAGEN")
                {
                    nombreImagen = Desescapar(campos[1], indice + 1);
                    continue;
                }

                if (seccion == Seccion.Puntos && campos[0] == "P" && campos.Length is 9 or 10)
                {
                    int desplazamiento = campos.Length == 10 ? 1 : 0;
                    campus.Puntos.Add(new PuntoRecoleccion
                    {
                        Id = LeerEntero(campos[1], indice + 1),
                        Tipo = desplazamiento == 0 ? TipoPunto.Recoleccion : LeerTipo(campos[2], indice + 1),
                        Nombre = Desescapar(campos[2 + desplazamiento], indice + 1),
                        X = LeerDecimal(campos[3 + desplazamiento], indice + 1),
                        Y = LeerDecimal(campos[4 + desplazamiento], indice + 1),
                        NivelLlenadoPct = LeerDecimal(campos[5 + desplazamiento], indice + 1),
                        GeneracionKgDia = LeerDecimal(campos[6 + desplazamiento], indice + 1),
                        DiasSinRecoleccion = LeerEntero(campos[7 + desplazamiento], indice + 1),
                        PesoZona = LeerDecimal(campos[8 + desplazamiento], indice + 1)
                    });
                    continue;
                }

                if (seccion == Seccion.Conexiones && campos.Length == 4 && campos[0] == "C")
                {
                    campus.Conexiones.Add(new ConexionCampus
                    {
                        OrigenId = LeerEntero(campos[1], indice + 1),
                        DestinoId = LeerEntero(campos[2], indice + 1),
                        Metros = LeerDecimal(campos[3], indice + 1)
                    });
                    continue;
                }

                throw new InvalidDataException($"Formato no válido en la línea {indice + 1}.");
            }

            if (!tieneSeccionPuntos || !tieneSeccionConexiones)
            {
                throw new InvalidDataException("El archivo debe contener las secciones PUNTOS y CONEXIONES.");
            }

            if (!string.IsNullOrWhiteSpace(nombreImagen))
            {
                string nombreSeguro = Path.GetFileName(nombreImagen);
                campus.RutaImagenMapa = Path.Combine(Path.GetDirectoryName(rutaCompleta)!, nombreSeguro);
            }

            return campus;
        }

        public void Guardar(CampusData campus, string rutaArchivo)
        {
            ArgumentNullException.ThrowIfNull(campus);
            ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivo);

            string rutaCompleta = Path.GetFullPath(rutaArchivo);
            string directorio = Path.GetDirectoryName(rutaCompleta)!;
            Directory.CreateDirectory(directorio);

            string? nombreImagen = null;
            if (!string.IsNullOrWhiteSpace(campus.RutaImagenMapa))
            {
                string rutaImagenOrigen = Path.GetFullPath(campus.RutaImagenMapa);
                if (!File.Exists(rutaImagenOrigen))
                {
                    throw new FileNotFoundException("No se encontró la imagen del mapa.", rutaImagenOrigen);
                }

                string extension = Path.GetExtension(rutaImagenOrigen);
                string rutaImagenDestino = Path.Combine(
                    directorio,
                    $"{Path.GetFileNameWithoutExtension(rutaCompleta)}_mapa{extension}");

                if (!string.Equals(rutaImagenOrigen, rutaImagenDestino, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(rutaImagenOrigen, rutaImagenDestino, overwrite: true);
                }

                nombreImagen = Path.GetFileName(rutaImagenDestino);
                campus.RutaImagenMapa = rutaImagenDestino;
            }

            List<string> lineas =
            [
                Cabecera,
                $"IMAGEN\t{(nombreImagen is null ? string.Empty : Escapar(nombreImagen))}",
                "PUNTOS"
            ];

            foreach (PuntoRecoleccion punto in campus.Puntos)
            {
                lineas.Add(string.Join('\t', new[]
                {
                    "P",
                    Formatear(punto.Id),
                    FormatearTipo(punto.Tipo),
                    Escapar(punto.Nombre),
                    Formatear(punto.X),
                    Formatear(punto.Y),
                    Formatear(punto.NivelLlenadoPct),
                    Formatear(punto.GeneracionKgDia),
                    Formatear(punto.DiasSinRecoleccion),
                    Formatear(punto.PesoZona)
                }));
            }

            lineas.Add("CONEXIONES");
            foreach (ConexionCampus conexion in campus.Conexiones)
            {
                lineas.Add(string.Join('\t', new[]
                {
                    "C",
                    Formatear(conexion.OrigenId),
                    Formatear(conexion.DestinoId),
                    Formatear(conexion.Metros)
                }));
            }

            File.WriteAllLines(rutaCompleta, lineas, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static TipoPunto LeerTipo(string valor, int numeroLinea) => valor switch
        {
            "RECOLECCION" => TipoPunto.Recoleccion,
            "ACOPIO" => TipoPunto.CentroAcopio,
            _ => throw new InvalidDataException($"Tipo de punto no válido en la línea {numeroLinea}.")
        };

        private static string FormatearTipo(TipoPunto tipo) => tipo switch
        {
            TipoPunto.Recoleccion => "RECOLECCION",
            TipoPunto.CentroAcopio => "ACOPIO",
            _ => throw new InvalidDataException("Tipo de punto no válido.")
        };

        private static string Escapar(string valor) => valor
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        private static string Desescapar(string valor, int numeroLinea)
        {
            StringBuilder resultado = new(valor.Length);
            for (int indice = 0; indice < valor.Length; indice++)
            {
                char caracter = valor[indice];
                if (caracter != '\\')
                {
                    resultado.Append(caracter);
                    continue;
                }

                if (++indice >= valor.Length)
                {
                    throw new InvalidDataException($"Secuencia de escape incompleta en la línea {numeroLinea}.");
                }

                resultado.Append(valor[indice] switch
                {
                    '\\' => '\\',
                    't' => '\t',
                    'r' => '\r',
                    'n' => '\n',
                    _ => throw new InvalidDataException($"Secuencia de escape no válida en la línea {numeroLinea}.")
                });
            }

            return resultado.ToString();
        }

        private static int LeerEntero(string valor, int numeroLinea)
        {
            if (int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int resultado))
            {
                return resultado;
            }

            throw new InvalidDataException($"Número entero no válido en la línea {numeroLinea}.");
        }

        private static double LeerDecimal(string valor, int numeroLinea)
        {
            if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double resultado)
                && double.IsFinite(resultado))
            {
                return resultado;
            }

            throw new InvalidDataException($"Número decimal no válido en la línea {numeroLinea}.");
        }

        private static string Formatear(int valor) => valor.ToString(CultureInfo.InvariantCulture);

        private static string Formatear(double valor) => valor.ToString("R", CultureInfo.InvariantCulture);

        private enum Seccion
        {
            Metadatos,
            Puntos,
            Conexiones
        }
    }
}
