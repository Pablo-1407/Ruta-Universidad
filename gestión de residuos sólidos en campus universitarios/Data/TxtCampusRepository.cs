using System.Diagnostics;
using System.Globalization;
using System.Text;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Data
{
    internal sealed class TxtCampusRepository : IPuntoRepository
    {
        private const string CabeceraV1 = "RUTAS_RESIDUOS_CAMPUS\t1";
        private const string CabeceraV2 = "RUTAS_RESIDUOS_CAMPUS\t2";

        public CampusData Cargar(string rutaArchivo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivo);

            string rutaCompleta = Path.GetFullPath(rutaArchivo);
            string[] lineas = File.ReadAllLines(rutaCompleta);
            if (lineas.Length == 0 || (lineas[0] != CabeceraV1 && lineas[0] != CabeceraV2))
            {
                throw new InvalidDataException("El archivo no tiene el formato de campus esperado.");
            }

            bool versionDos = lineas[0] == CabeceraV2;
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

                if (linea == "PARAMETROS_OPERACION")
                {
                    seccion = Seccion.ParametrosOperacion;
                    continue;
                }

                if (linea == "PARAMETROS_RESIDUOS")
                {
                    seccion = Seccion.ParametrosResiduos;
                    continue;
                }

                if (linea == "PROGRAMACIONES")
                {
                    seccion = Seccion.Programaciones;
                    continue;
                }

                if (linea == "HISTORIAL")
                {
                    seccion = Seccion.Historial;
                    continue;
                }

                string[] campos = linea.Split('\t');
                if (seccion == Seccion.Metadatos && campos.Length == 2 && campos[0] == "IMAGEN")
                {
                    nombreImagen = Desescapar(campos[1], indice + 1);
                    continue;
                }

                if (seccion == Seccion.Puntos && campos[0] == "P")
                {
                    campus.Puntos.Add(LeerPunto(campos, versionDos, indice + 1));
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

                if (seccion == Seccion.ParametrosOperacion && campos.Length == 8 && campos[0] == "O")
                {
                    campus.ParametrosOperacion = new ParametrosOperacion
                    {
                        CapacidadVehiculoLitros = LeerDecimal(campos[1], indice + 1),
                        VelocidadMediaKmHora = LeerDecimal(campos[2], indice + 1),
                        CostoPorKm = LeerDecimal(campos[3], indice + 1),
                        CostoPorHora = LeerDecimal(campos[4], indice + 1),
                        MinutosServicioPorContenedor = LeerDecimal(campos[5], indice + 1),
                        MinutosDescarga = LeerDecimal(campos[6], indice + 1),
                        UmbralProgramacionPct = LeerDecimal(campos[7], indice + 1)
                    };
                    continue;
                }

                if (seccion == Seccion.ParametrosResiduos && campos.Length == 8 && campos[0] == "R")
                {
                    campus.ParametrosResiduos.Add(new ParametrosResiduo
                    {
                        Tipo = LeerTipoResiduo(campos[1], indice + 1),
                        DensidadKgPorLitro = LeerDecimal(campos[2], indice + 1),
                        PorcentajeAprovechablePct = LeerDecimal(campos[3], indice + 1),
                        FactorCO2eEvitadoKgPorKg = LeerDecimal(campos[4], indice + 1),
                        FuenteDensidad = Desescapar(campos[5], indice + 1),
                        FuenteAprovechamiento = Desescapar(campos[6], indice + 1),
                        FuenteEmisiones = Desescapar(campos[7], indice + 1)
                    });
                    continue;
                }

                if (seccion == Seccion.Programaciones && campos.Length == 8 && campos[0] == "S")
                {
                    campus.Programaciones.Add(new ProgramacionRecoleccion
                    {
                        Id = LeerEntero(campos[1], indice + 1),
                        FechaProgramada = LeerFecha(campos[2], indice + 1),
                        CentroAcopioId = LeerEntero(campos[3], indice + 1),
                        PuntosIds = LeerIds(campos[4], indice + 1),
                        VolumenEstimadoLitros = LeerDecimal(campos[5], indice + 1),
                        Estado = LeerEstadoProgramacion(campos[6], indice + 1),
                        FechaCompletada = string.IsNullOrEmpty(campos[7]) ? null : LeerFecha(campos[7], indice + 1)
                    });
                    continue;
                }

                if (seccion == Seccion.Historial && campos.Length == 12 && campos[0] == "H")
                {
                    campus.HistorialRecolecciones.Add(new RegistroHistorialRecoleccion
                    {
                        Id = LeerEntero(campos[1], indice + 1),
                        FechaCompletada = LeerFecha(campos[2], indice + 1),
                        CentroAcopioId = LeerEntero(campos[3], indice + 1),
                        PuntosIds = LeerIds(campos[4], indice + 1),
                        VolumenLitros = LeerDecimal(campos[5], indice + 1),
                        KilosRecolectados = LeerDecimal(campos[6], indice + 1),
                        DistanciaMetros = LeerDecimal(campos[7], indice + 1),
                        DuracionMinutos = LeerDecimal(campos[8], indice + 1),
                        Costo = LeerDecimal(campos[9], indice + 1),
                        KilosAprovechados = LeerDecimal(campos[10], indice + 1),
                        CO2eEvitadoKg = LeerDecimal(campos[11], indice + 1)
                    });
                    continue;
                }

                throw new InvalidDataException($"Formato no válido en la línea {indice + 1}.");
            }

            if (!tieneSeccionPuntos || !tieneSeccionConexiones)
            {
                throw new InvalidDataException("El archivo debe contener las secciones PUNTOS y CONEXIONES.");
            }

            if (!versionDos && campus.Puntos.Any(p => p.Tipo == TipoPunto.Recoleccion))
            {
                foreach (PuntoRecoleccion punto in campus.Puntos.Where(p => p.Tipo == TipoPunto.Recoleccion))
                {
                    punto.TipoResiduo = TipoResiduo.SinClasificar;
                    punto.CapacidadLitros = 0;
                }
            }

            if (!string.IsNullOrWhiteSpace(nombreImagen))
            {
                string nombreSeguro = Path.GetFileName(nombreImagen);
                campus.RutaImagenMapa = Path.Combine(Path.GetDirectoryName(rutaCompleta)!, nombreSeguro);
            }

            return campus;
        }

        public Task<CampusData> CargarAsync(string rutaArchivo, CancellationToken cancellationToken = default) =>
            Task.Run(() => Cargar(rutaArchivo), cancellationToken);

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
                CabeceraV2,
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
                    FormatearTipoResiduo(punto.TipoResiduo),
                    Formatear(punto.CapacidadLitros),
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

            lineas.Add("PARAMETROS_OPERACION");
            ParametrosOperacion operacion = campus.ParametrosOperacion;
            lineas.Add(string.Join('\t', new[]
            {
                "O",
                Formatear(operacion.CapacidadVehiculoLitros),
                Formatear(operacion.VelocidadMediaKmHora),
                Formatear(operacion.CostoPorKm),
                Formatear(operacion.CostoPorHora),
                Formatear(operacion.MinutosServicioPorContenedor),
                Formatear(operacion.MinutosDescarga),
                Formatear(operacion.UmbralProgramacionPct)
            }));

            lineas.Add("PARAMETROS_RESIDUOS");
            foreach (ParametrosResiduo parametros in campus.ParametrosResiduos)
            {
                lineas.Add(string.Join('\t', new[]
                {
                    "R",
                    FormatearTipoResiduo(parametros.Tipo),
                    Formatear(parametros.DensidadKgPorLitro),
                    Formatear(parametros.PorcentajeAprovechablePct),
                    Formatear(parametros.FactorCO2eEvitadoKgPorKg),
                    Escapar(parametros.FuenteDensidad),
                    Escapar(parametros.FuenteAprovechamiento),
                    Escapar(parametros.FuenteEmisiones)
                }));
            }

            lineas.Add("PROGRAMACIONES");
            foreach (ProgramacionRecoleccion programacion in campus.Programaciones)
            {
                lineas.Add(string.Join('\t', new[]
                {
                    "S",
                    Formatear(programacion.Id),
                    Formatear(programacion.FechaProgramada),
                    Formatear(programacion.CentroAcopioId),
                    FormatearIds(programacion.PuntosIds),
                    Formatear(programacion.VolumenEstimadoLitros),
                    FormatearEstado(programacion.Estado),
                    programacion.FechaCompletada is null ? string.Empty : Formatear(programacion.FechaCompletada.Value)
                }));
            }

            lineas.Add("HISTORIAL");
            foreach (RegistroHistorialRecoleccion registro in campus.HistorialRecolecciones)
            {
                lineas.Add(string.Join('\t', new[]
                {
                    "H",
                    Formatear(registro.Id),
                    Formatear(registro.FechaCompletada),
                    Formatear(registro.CentroAcopioId),
                    FormatearIds(registro.PuntosIds),
                    Formatear(registro.VolumenLitros),
                    Formatear(registro.KilosRecolectados),
                    Formatear(registro.DistanciaMetros),
                    Formatear(registro.DuracionMinutos),
                    Formatear(registro.Costo),
                    Formatear(registro.KilosAprovechados),
                    Formatear(registro.CO2eEvitadoKg)
                }));
            }

            string rutaTemporal = Path.Combine(
                directorio,
                $".{Path.GetFileName(rutaCompleta)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllLines(rutaTemporal, lineas, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                File.Move(rutaTemporal, rutaCompleta, overwrite: true);
            }
            finally
            {
                try
                {
                    if (File.Exists(rutaTemporal))
                    {
                        File.Delete(rutaTemporal);
                    }
                }
                catch (Exception excepcion)
                {
                    Trace.TraceError($"No se pudo eliminar el archivo temporal {rutaTemporal}: {excepcion}");
                }
            }
        }

        public Task GuardarAsync(CampusData campus, string rutaArchivo, CancellationToken cancellationToken = default) =>
            Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Guardar(campus, rutaArchivo);
            }, cancellationToken);

        private static PuntoRecoleccion LeerPunto(string[] campos, bool versionDos, int numeroLinea)
        {
            if (versionDos)
            {
                if (campos.Length != 12)
                {
                    throw new InvalidDataException($"Formato de punto no válido en la línea {numeroLinea}.");
                }

                return new PuntoRecoleccion
                {
                    Id = LeerEntero(campos[1], numeroLinea),
                    Tipo = LeerTipo(campos[2], numeroLinea),
                    Nombre = Desescapar(campos[3], numeroLinea),
                    X = LeerDecimal(campos[4], numeroLinea),
                    Y = LeerDecimal(campos[5], numeroLinea),
                    TipoResiduo = LeerTipoResiduo(campos[6], numeroLinea),
                    CapacidadLitros = LeerDecimal(campos[7], numeroLinea),
                    NivelLlenadoPct = LeerDecimal(campos[8], numeroLinea),
                    GeneracionKgDia = LeerDecimal(campos[9], numeroLinea),
                    DiasSinRecoleccion = LeerEntero(campos[10], numeroLinea),
                    PesoZona = LeerDecimal(campos[11], numeroLinea)
                };
            }

            if (campos.Length == 10)
            {
                return new PuntoRecoleccion
                {
                    Id = LeerEntero(campos[1], numeroLinea),
                    Tipo = LeerTipo(campos[2], numeroLinea),
                    Nombre = Desescapar(campos[3], numeroLinea),
                    X = LeerDecimal(campos[4], numeroLinea),
                    Y = LeerDecimal(campos[5], numeroLinea),
                    NivelLlenadoPct = LeerDecimal(campos[6], numeroLinea),
                    GeneracionKgDia = LeerDecimal(campos[7], numeroLinea),
                    DiasSinRecoleccion = LeerEntero(campos[8], numeroLinea),
                    PesoZona = LeerDecimal(campos[9], numeroLinea)
                };
            }

            if (campos.Length == 9)
            {
                return new PuntoRecoleccion
                {
                    Id = LeerEntero(campos[1], numeroLinea),
                    Nombre = Desescapar(campos[2], numeroLinea),
                    X = LeerDecimal(campos[3], numeroLinea),
                    Y = LeerDecimal(campos[4], numeroLinea),
                    NivelLlenadoPct = LeerDecimal(campos[5], numeroLinea),
                    GeneracionKgDia = LeerDecimal(campos[6], numeroLinea),
                    DiasSinRecoleccion = LeerEntero(campos[7], numeroLinea),
                    PesoZona = LeerDecimal(campos[8], numeroLinea)
                };
            }

            throw new InvalidDataException($"Formato de punto no válido en la línea {numeroLinea}.");
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

        private static TipoResiduo LeerTipoResiduo(string valor, int numeroLinea)
        {
            if (Enum.TryParse(valor, ignoreCase: false, out TipoResiduo tipo) && Enum.IsDefined(tipo))
            {
                return tipo;
            }

            throw new InvalidDataException($"Tipo de residuo no válido en la línea {numeroLinea}.");
        }

        private static string FormatearTipoResiduo(TipoResiduo tipo) => Enum.IsDefined(tipo)
            ? tipo.ToString()
            : throw new InvalidDataException("Tipo de residuo no válido.");

        private static EstadoProgramacion LeerEstadoProgramacion(string valor, int numeroLinea)
        {
            if (Enum.TryParse(valor, ignoreCase: false, out EstadoProgramacion estado) && Enum.IsDefined(estado))
            {
                return estado;
            }

            throw new InvalidDataException($"Estado de programación no válido en la línea {numeroLinea}.");
        }

        private static string FormatearEstado(EstadoProgramacion estado) => Enum.IsDefined(estado)
            ? estado.ToString()
            : throw new InvalidDataException("Estado de programación no válido.");

        private static List<int> LeerIds(string valor, int numeroLinea)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return [];
            }

            List<int> ids = [];
            foreach (string id in valor.Split(','))
            {
                ids.Add(LeerEntero(id, numeroLinea));
            }

            return ids;
        }

        private static DateTimeOffset LeerFecha(string valor, int numeroLinea)
        {
            if (DateTimeOffset.TryParseExact(valor, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset fecha))
            {
                return fecha;
            }

            throw new InvalidDataException($"Fecha no válida en la línea {numeroLinea}.");
        }

        private static string FormatearIds(IEnumerable<int> ids) => string.Join(',', ids.Select(Formatear));

        private static string Formatear(DateTimeOffset valor) => valor.ToString("O", CultureInfo.InvariantCulture);

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
            Conexiones,
            ParametrosOperacion,
            ParametrosResiduos,
            Programaciones,
            Historial
        }
    }
}
