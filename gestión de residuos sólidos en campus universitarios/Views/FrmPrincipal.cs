using System.Drawing.Drawing2D;
using System.Globalization;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;
using gestión_de_residuos_sólidos_en_campus_universitarios.Views;

namespace gestión_de_residuos_sólidos_en_campus_universitarios
{
    public partial class Main : Form, IRutaView
    {
        private readonly DataGridView _tablaPuntos = CrearTabla();
        private readonly DataGridView _tablaConexiones = CrearTabla();
        private readonly MapCanvas _mapa = new();
        private readonly Label _estado = new();

        public event Action<double, double>? MapaPulsado;
        public event Action? ImagenMapaSolicitada;
        public event Action? CargarCampusSolicitado;
        public event Action? GuardarCampusSolicitado;
        public event Action? EditarPuntoSolicitado;
        public event Action? EliminarPuntoSolicitado;
        public event Action? AgregarConexionSolicitada;
        public event Action? EditarConexionSolicitada;
        public event Action? EliminarConexionSolicitada;

        private int? PuntoSeleccionadoIdActual => (_tablaPuntos.CurrentRow?.Tag as PuntoRecoleccion)?.Id;
        private ConexionCampus? ConexionSeleccionadaActual => _tablaConexiones.CurrentRow?.Tag as ConexionCampus;

        int? IRutaView.PuntoSeleccionadoId => PuntoSeleccionadoIdActual;
        ConexionCampus? IRutaView.ConexionSeleccionada => ConexionSeleccionadaActual;

        public Main()
        {
            InitializeComponent();
            ConstruirInterfaz();
            ConectarEventos();
        }

        private void ConstruirInterfaz()
        {
            TableLayoutPanel contenido = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 4
            };
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            contenido.Controls.Add(CrearCabecera(), 0, 0);
            contenido.Controls.Add(CrearBarraHerramientas(), 0, 1);
            contenido.Controls.Add(CrearPanelTrabajo(), 0, 2);

            _estado.Dock = DockStyle.Fill;
            _estado.TextAlign = ContentAlignment.MiddleLeft;
            _estado.Text = "Carga una imagen y haz clic en el mapa para añadir puntos. Las conexiones se registran con sus distancias.";
            contenido.Controls.Add(_estado, 0, 3);
            Controls.Add(contenido);
        }

        private static Control CrearCabecera()
        {
            TableLayoutPanel cabecera = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            cabecera.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            cabecera.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            cabecera.Controls.Add(new Label
            {
                Text = "Gestión de residuos sólidos en campus",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            cabecera.Controls.Add(new Label
            {
                Text = "Ubica los nodos sobre una imagen y registra las distancias de cada conexión.",
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            return cabecera;
        }

        private Control CrearBarraHerramientas()
        {
            FlowLayoutPanel barra = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 3, 0, 3)
            };
            barra.Controls.Add(CrearBoton("Abrir campus", (_, _) => CargarCampusSolicitado?.Invoke()));
            barra.Controls.Add(CrearBoton("Guardar campus", (_, _) => GuardarCampusSolicitado?.Invoke()));
            barra.Controls.Add(CrearBoton("Cargar imagen", (_, _) => ImagenMapaSolicitada?.Invoke()));
            return barra;
        }

        private Control CrearPanelTrabajo()
        {
            SplitContainer division = new()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1
            };

            const int anchoMinimoPanel1 = 340;
            const int anchoMinimoPanel2 = 450;
            EventHandler configurarDivision = null!;
            configurarDivision = (_, _) =>
            {
                int anchoMinimo = anchoMinimoPanel1 + anchoMinimoPanel2 + division.SplitterWidth;
                if (division.ClientSize.Width < anchoMinimo)
                {
                    return;
                }

                division.SizeChanged -= configurarDivision;
                division.SplitterDistance = Math.Clamp(380, anchoMinimoPanel1, division.ClientSize.Width - anchoMinimoPanel2);
                division.Panel1MinSize = anchoMinimoPanel1;
                division.Panel2MinSize = anchoMinimoPanel2;
            };
            division.SizeChanged += configurarDivision;

            TableLayoutPanel panelListas = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0),
                ColumnCount = 1,
                RowCount = 2
            };
            panelListas.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            panelListas.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            panelListas.Controls.Add(CrearGrupoPuntos(), 0, 0);
            panelListas.Controls.Add(CrearGrupoConexiones(), 0, 1);
            division.Panel1.Controls.Add(panelListas);

            _mapa.Dock = DockStyle.Fill;
            division.Panel2.Controls.Add(_mapa);
            return division;
        }

        private Control CrearGrupoPuntos()
        {
            Button editar = CrearBoton("Editar", (_, _) => EditarPuntoSolicitado?.Invoke());
            Button eliminar = CrearBoton("Eliminar", (_, _) =>
            {
                if (PuntoSeleccionadoIdActual is not null
                    && Confirmar("¿Quieres eliminar el punto y sus conexiones?"))
                {
                    EliminarPuntoSolicitado?.Invoke();
                }
            });
            return CrearGrupo("Puntos de recolección", _tablaPuntos, editar, eliminar);
        }

        private Control CrearGrupoConexiones()
        {
            Button agregar = CrearBoton("Añadir", (_, _) => AgregarConexionSolicitada?.Invoke());
            Button editar = CrearBoton("Editar", (_, _) => EditarConexionSolicitada?.Invoke());
            Button eliminar = CrearBoton("Eliminar", (_, _) =>
            {
                if (ConexionSeleccionadaActual is not null && Confirmar("¿Quieres eliminar la conexión seleccionada?"))
                {
                    EliminarConexionSolicitada?.Invoke();
                }
            });
            return CrearGrupo("Conexiones ingresadas", _tablaConexiones, agregar, editar, eliminar);
        }

        private static GroupBox CrearGrupo(string titulo, DataGridView tabla, params Control[] botones)
        {
            GroupBox grupo = new()
            {
                Text = titulo,
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            TableLayoutPanel contenido = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            contenido.Controls.Add(tabla, 0, 0);

            FlowLayoutPanel barraBotones = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            barraBotones.Controls.AddRange(botones);
            contenido.Controls.Add(barraBotones, 0, 1);
            grupo.Controls.Add(contenido);
            return grupo;
        }

        private static DataGridView CrearTabla()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
        }

        private static Button CrearBoton(string texto, EventHandler accion)
        {
            Button boton = new()
            {
                Text = texto,
                AutoSize = true,
                MinimumSize = new Size(78, 30),
                Margin = new Padding(3)
            };
            boton.Click += accion;
            return boton;
        }

        private void ConectarEventos()
        {
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre", FillWeight = 120 });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", FillWeight = 95 });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Llenado", HeaderText = "Llenado %", FillWeight = 75 });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Generacion", HeaderText = "kg/día", FillWeight = 75 });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dias", HeaderText = "Días", FillWeight = 55 });
            _tablaPuntos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Zona", HeaderText = "Zona", FillWeight = 55 });
            _tablaConexiones.Columns.Add(new DataGridViewTextBoxColumn { Name = "Origen", HeaderText = "Origen", FillWeight = 100 });
            _tablaConexiones.Columns.Add(new DataGridViewTextBoxColumn { Name = "Destino", HeaderText = "Destino", FillWeight = 100 });
            _tablaConexiones.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metros", HeaderText = "Metros", FillWeight = 65 });

            _tablaPuntos.SelectionChanged += (_, _) => _mapa.SeleccionarPunto(PuntoSeleccionadoIdActual);
            _mapa.UbicacionPulsada += (x, y) => MapaPulsado?.Invoke(x, y);
            _mapa.PuntoPulsado += SeleccionarPuntoEnTabla;
        }

        private void SeleccionarPuntoEnTabla(int id)
        {
            foreach (DataGridViewRow fila in _tablaPuntos.Rows)
            {
                if (fila.Tag is PuntoRecoleccion punto && punto.Id == id)
                {
                    _tablaPuntos.ClearSelection();
                    fila.Selected = true;
                    _tablaPuntos.CurrentCell = fila.Cells["Nombre"];
                    return;
                }
            }
        }

        bool IRutaView.SolicitarDatosPunto(
            double x,
            double y,
            PuntoRecoleccion? puntoOriginal,
            out PuntoRecoleccion? punto)
        {
            using PuntoDialog dialogo = new(x, y, puntoOriginal);
            bool aceptado = dialogo.ShowDialog(this) == DialogResult.OK;
            punto = aceptado ? dialogo.Punto : null;
            return aceptado;
        }

        bool IRutaView.SolicitarDatosConexion(
            IReadOnlyList<PuntoRecoleccion> puntos,
            ConexionCampus? conexionOriginal,
            out ConexionCampus? conexion)
        {
            conexion = null;
            if (puntos.Count < 2)
            {
                MostrarMensaje("Añade al menos dos puntos antes de registrar una conexión.", error: true);
                return false;
            }

            using ConexionDialog dialogo = new(puntos, conexionOriginal);
            bool aceptado = dialogo.ShowDialog(this) == DialogResult.OK;
            conexion = aceptado ? dialogo.Conexion : null;
            return aceptado;
        }

        public string? SeleccionarArchivoCampus(bool guardar)
        {
            if (guardar)
            {
                using SaveFileDialog dialogo = new()
                {
                    Title = "Guardar campus",
                    Filter = "Archivo de campus (*.txt)|*.txt",
                    DefaultExt = "txt",
                    AddExtension = true,
                    OverwritePrompt = true
                };
                return dialogo.ShowDialog(this) == DialogResult.OK ? dialogo.FileName : null;
            }

            using OpenFileDialog abrir = new()
            {
                Title = "Abrir campus",
                Filter = "Archivo de campus (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                CheckFileExists = true
            };
            return abrir.ShowDialog(this) == DialogResult.OK ? abrir.FileName : null;
        }

        public string? SeleccionarImagenMapa()
        {
            using OpenFileDialog dialogo = new()
            {
                Title = "Seleccionar imagen del mapa",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|Todos los archivos (*.*)|*.*",
                CheckFileExists = true
            };
            return dialogo.ShowDialog(this) == DialogResult.OK ? dialogo.FileName : null;
        }

        void IRutaView.MostrarCampus(CampusData campus)
        {
            _tablaPuntos.Rows.Clear();
            foreach (PuntoRecoleccion punto in campus.Puntos.OrderBy(p => p.Id))
            {
                int indice = _tablaPuntos.Rows.Add(
                    punto.Id,
                    punto.Nombre,
                    punto.Tipo == TipoPunto.CentroAcopio ? "Centro de acopio" : "Recolección",
                    punto.NivelLlenadoPct.ToString("0.#", CultureInfo.CurrentCulture),
                    punto.GeneracionKgDia.ToString("0.##", CultureInfo.CurrentCulture),
                    punto.DiasSinRecoleccion,
                    punto.PesoZona.ToString("0.##", CultureInfo.CurrentCulture));
                _tablaPuntos.Rows[indice].Tag = punto;
            }

            _tablaConexiones.Rows.Clear();
            Dictionary<int, string> nombres = campus.Puntos.ToDictionary(p => p.Id, p => p.Nombre);
            foreach (ConexionCampus conexion in campus.Conexiones)
            {
                int indice = _tablaConexiones.Rows.Add(
                    nombres.GetValueOrDefault(conexion.OrigenId, $"#{conexion.OrigenId}"),
                    nombres.GetValueOrDefault(conexion.DestinoId, $"#{conexion.DestinoId}"),
                    conexion.Metros.ToString("0.##", CultureInfo.CurrentCulture));
                _tablaConexiones.Rows[indice].Tag = conexion;
            }

            try
            {
                _mapa.CargarImagen(campus.RutaImagenMapa);
            }
            catch (Exception ex)
            {
                _mapa.CargarImagen(null);
                MostrarMensaje($"No se pudo abrir la imagen del mapa: {ex.Message}", error: true);
            }

            _mapa.MostrarDatos(campus.Puntos, campus.Conexiones);
            _estado.Text = campus.RutaImagenMapa is null
                ? "Carga una imagen y haz clic en el mapa para añadir puntos. Las conexiones se registran con sus distancias."
                : "Haz clic en una zona vacía del mapa para añadir un punto; selecciona una conexión en la lista para editarla.";
        }

        public void MostrarMensaje(string mensaje, bool error = false)
        {
            MessageBox.Show(
                this,
                mensaje,
                error ? "No se pudo completar la operación" : "Gestión de residuos del campus",
                MessageBoxButtons.OK,
                error ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private static bool Confirmar(string mensaje) =>
            MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        private sealed class MapCanvas : Control
        {
            private Image? _imagen;
            private string? _rutaImagen;
            private IReadOnlyList<PuntoRecoleccion> _puntos = [];
            private IReadOnlyList<ConexionCampus> _conexiones = [];
            private int? _puntoSeleccionadoId;

            public event Action<double, double>? UbicacionPulsada;
            public event Action<int>? PuntoPulsado;

            public MapCanvas()
            {
                BackColor = Color.WhiteSmoke;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public void CargarImagen(string? ruta)
            {
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    _imagen?.Dispose();
                    _imagen = null;
                    _rutaImagen = null;
                    Invalidate();
                    return;
                }

                string rutaCompleta = Path.GetFullPath(ruta);
                if (_imagen is not null && string.Equals(_rutaImagen, rutaCompleta, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                using Image original = Image.FromFile(rutaCompleta);
                Image copia = new Bitmap(original);
                _imagen?.Dispose();
                _imagen = copia;
                _rutaImagen = rutaCompleta;
                Invalidate();
            }

            public void MostrarDatos(IReadOnlyList<PuntoRecoleccion> puntos, IReadOnlyList<ConexionCampus> conexiones)
            {
                _puntos = puntos.ToList();
                _conexiones = conexiones.ToList();
                Invalidate();
            }

            public void SeleccionarPunto(int? id)
            {
                _puntoSeleccionadoId = id;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(BackColor);

                if (_imagen is null)
                {
                    const string texto = "Carga una imagen del campus para ubicar los puntos";
                    using Font fuente = new("Segoe UI", 12, FontStyle.Regular);
                    SizeF medida = graphics.MeasureString(texto, fuente);
                    graphics.DrawString(texto, fuente, Brushes.DimGray,
                        (ClientSize.Width - medida.Width) / 2,
                        (ClientSize.Height - medida.Height) / 2);
                    return;
                }

                RectangleF destino = CalcularRectanguloImagen();
                graphics.DrawImage(_imagen, destino);

                Dictionary<int, PointF> posiciones = _puntos.ToDictionary(p => p.Id, p => ObtenerPosicion(p, destino));
                using Pen lapizConexion = new(Color.FromArgb(190, 30, 65, 110), 2.5f);
                foreach (ConexionCampus conexion in _conexiones)
                {
                    if (posiciones.TryGetValue(conexion.OrigenId, out PointF origen)
                        && posiciones.TryGetValue(conexion.DestinoId, out PointF destinoConexion))
                    {
                        graphics.DrawLine(lapizConexion, origen, destinoConexion);
                    }
                }

                using Font fuentePunto = new("Segoe UI", 9, FontStyle.Bold);
                using Pen borde = new(Color.White, 2);
                foreach (PuntoRecoleccion punto in _puntos)
                {
                    PointF posicion = posiciones[punto.Id];
                    RectangleF marcador = new(posicion.X - 7, posicion.Y - 7, 14, 14);
                    Color color = punto.Id == _puntoSeleccionadoId
                        ? Color.DarkOrange
                        : punto.Tipo == TipoPunto.CentroAcopio ? Color.ForestGreen : Color.RoyalBlue;
                    using SolidBrush relleno = new(color);
                    graphics.FillEllipse(relleno, marcador);
                    graphics.DrawEllipse(borde, marcador);

                    SizeF medida = graphics.MeasureString(punto.Nombre, fuentePunto);
                    RectangleF etiqueta = new(posicion.X + 9, posicion.Y - medida.Height / 2 - 2,
                        medida.Width + 6, medida.Height + 4);
                    graphics.FillRectangle(Brushes.White, etiqueta);
                    graphics.DrawString(punto.Nombre, fuentePunto, Brushes.Black,
                        etiqueta.X + 3, etiqueta.Y + 2);
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left || _imagen is null)
                {
                    return;
                }

                RectangleF rectangulo = CalcularRectanguloImagen();
                if (!rectangulo.Contains(e.Location))
                {
                    return;
                }

                PuntoRecoleccion? cercano = _puntos
                    .Select(p => (Punto: p, Posicion: ObtenerPosicion(p, rectangulo)))
                    .Where(item => Distancia(item.Posicion, e.Location) <= 12)
                    .OrderBy(item => Distancia(item.Posicion, e.Location))
                    .Select(item => item.Punto)
                    .FirstOrDefault();

                if (cercano is not null)
                {
                    PuntoPulsado?.Invoke(cercano.Id);
                    return;
                }

                double x = Math.Clamp((e.X - rectangulo.X) / rectangulo.Width, 0, 1);
                double y = Math.Clamp((e.Y - rectangulo.Y) / rectangulo.Height, 0, 1);
                UbicacionPulsada?.Invoke(x, y);
            }

            private RectangleF CalcularRectanguloImagen()
            {
                if (_imagen is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                {
                    return RectangleF.Empty;
                }

                float escala = Math.Min(ClientSize.Width / (float)_imagen.Width, ClientSize.Height / (float)_imagen.Height);
                float ancho = _imagen.Width * escala;
                float alto = _imagen.Height * escala;
                return new RectangleF((ClientSize.Width - ancho) / 2, (ClientSize.Height - alto) / 2, ancho, alto);
            }

            private static PointF ObtenerPosicion(PuntoRecoleccion punto, RectangleF rectangulo) =>
                new(rectangulo.X + (float)punto.X * rectangulo.Width,
                    rectangulo.Y + (float)punto.Y * rectangulo.Height);

            private static double Distancia(PointF punto, Point ubicacion) =>
                Math.Sqrt(Math.Pow(punto.X - ubicacion.X, 2) + Math.Pow(punto.Y - ubicacion.Y, 2));

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _imagen?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
