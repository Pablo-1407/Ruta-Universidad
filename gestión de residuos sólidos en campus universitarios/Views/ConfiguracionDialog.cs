using System.Globalization;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Views
{
    internal sealed class ConfiguracionDialog : Form
    {
        private readonly TextBox[] _operacion = Enumerable.Range(0, 7).Select(_ => new TextBox()).ToArray();
        private readonly DataGridView _tablaResiduos = new();
        private readonly ParametrosOperacion _parametrosOperacion;
        private readonly List<ParametrosResiduo> _parametrosResiduos;
        private readonly HashSet<TipoResiduo> _tiposDensidadPendiente;

        public ParametrosOperacion ParametrosOperacion { get; private set; } = new();
        public IReadOnlyList<ParametrosResiduo> ParametrosResiduos { get; private set; } = [];

        public ConfiguracionDialog(
            ParametrosOperacion parametrosOperacion,
            IEnumerable<ParametrosResiduo> parametrosResiduos,
            IEnumerable<TipoResiduo>? tiposDensidadPendiente = null)
        {
            _parametrosOperacion = parametrosOperacion;
            _parametrosResiduos = parametrosResiduos.ToList();
            _tiposDensidadPendiente = tiposDensidadPendiente?.ToHashSet() ?? new HashSet<TipoResiduo>();
            Text = "Configuración operativa y de residuos";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 540);
            Size = new Size(940, 640);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            TabControl pestañas = new() { Dock = DockStyle.Fill };
            TabPage operacion = new("Operación");
            TabPage residuos = new("Factores por residuo");
            operacion.Controls.Add(CrearFormularioOperacion());
            residuos.Controls.Add(CrearTablaResiduos());
            pestañas.TabPages.Add(operacion);
            pestañas.TabPages.Add(residuos);
            if (_tiposDensidadPendiente.Count > 0)
            {
                pestañas.SelectedTab = residuos;
            }

            Button guardar = new() { Text = "Guardar configuración", AutoSize = true };
            Button cancelar = new() { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
            guardar.Click += Guardar;
            FlowLayoutPanel botones = new()
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            botones.Controls.Add(guardar);
            botones.Controls.Add(cancelar);
            Controls.Add(pestañas);
            Controls.Add(botones);
            AcceptButton = guardar;
            CancelButton = cancelar;
        }

        private Control CrearFormularioOperacion()
        {
            string[] etiquetas =
            [
                "Capacidad del vehículo (litros)",
                "Velocidad media (km/h)",
                "Coste por km",
                "Coste por hora",
                "Minutos por contenedor",
                "Minutos por descarga",
                "Umbral de programación (%)"
            ];
            double[] valores =
            [
                _parametrosOperacion.CapacidadVehiculoLitros,
                _parametrosOperacion.VelocidadMediaKmHora,
                _parametrosOperacion.CostoPorKm,
                _parametrosOperacion.CostoPorHora,
                _parametrosOperacion.MinutosServicioPorContenedor,
                _parametrosOperacion.MinutosDescarga,
                _parametrosOperacion.UmbralProgramacionPct
            ];

            TableLayoutPanel tabla = new()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = etiquetas.Length
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int indice = 0; indice < etiquetas.Length; indice++)
            {
                tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
                tabla.Controls.Add(new Label
                {
                    Text = etiquetas[indice],
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                }, 0, indice);
                _operacion[indice].Text = valores[indice].ToString("R", CultureInfo.CurrentCulture);
                _operacion[indice].Dock = DockStyle.Fill;
                tabla.Controls.Add(_operacion[indice], 1, indice);
            }

            return tabla;
        }

        private Control CrearTablaResiduos()
        {
            _tablaResiduos.Dock = DockStyle.Fill;
            _tablaResiduos.AllowUserToAddRows = false;
            _tablaResiduos.AllowUserToDeleteRows = false;
            _tablaResiduos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _tablaResiduos.RowHeadersVisible = false;
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", ReadOnly = true });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Densidad", HeaderText = "Densidad kg/l" });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Aprovechamiento", HeaderText = "Aprovechable %" });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Emisiones", HeaderText = "CO₂e evitado kg/kg" });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FuenteDensidad", HeaderText = "Fuente densidad" });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FuenteAprovechamiento", HeaderText = "Fuente aprovechamiento" });
            _tablaResiduos.Columns.Add(new DataGridViewTextBoxColumn { Name = "FuenteEmisiones", HeaderText = "Fuente emisiones" });

            foreach (TipoResiduo tipo in Enum.GetValues<TipoResiduo>().Where(tipo => tipo != TipoResiduo.SinClasificar))
            {
                ParametrosResiduo parametros = _parametrosResiduos.FirstOrDefault(p => p.Tipo == tipo)
                    ?? new ParametrosResiduo { Tipo = tipo };
                int indiceFila = _tablaResiduos.Rows.Add(
                    tipo,
                    Formatear(parametros.DensidadKgPorLitro),
                    Formatear(parametros.PorcentajeAprovechablePct),
                    Formatear(parametros.FactorCO2eEvitadoKgPorKg),
                    parametros.FuenteDensidad,
                    parametros.FuenteAprovechamiento,
                    parametros.FuenteEmisiones);
                if (_tiposDensidadPendiente.Contains(tipo))
                {
                    _tablaResiduos.Rows[indiceFila].Cells["Densidad"].Style.BackColor = Color.LightYellow;
                    _tablaResiduos.Rows[indiceFila].Cells["FuenteDensidad"].Style.BackColor = Color.LightYellow;
                }
            }

            if (_tiposDensidadPendiente.Count > 0)
            {
                TipoResiduo tipoPendiente = _tiposDensidadPendiente.First();
                DataGridViewRow? filaPendiente = _tablaResiduos.Rows
                    .Cast<DataGridViewRow>()
                    .FirstOrDefault(fila => (TipoResiduo)fila.Cells["Tipo"].Value! == tipoPendiente);
                if (filaPendiente is not null)
                {
                    _tablaResiduos.CurrentCell = filaPendiente.Cells["Densidad"];
                }
            }

            return _tablaResiduos;
        }

        private void Guardar(object? sender, EventArgs e)
        {
            try
            {
                ParametrosOperacion parametros = new()
                {
                    CapacidadVehiculoLitros = LeerNumero(_operacion[0].Text),
                    VelocidadMediaKmHora = LeerNumero(_operacion[1].Text),
                    CostoPorKm = LeerNumero(_operacion[2].Text),
                    CostoPorHora = LeerNumero(_operacion[3].Text),
                    MinutosServicioPorContenedor = LeerNumero(_operacion[4].Text),
                    MinutosDescarga = LeerNumero(_operacion[5].Text),
                    UmbralProgramacionPct = LeerNumero(_operacion[6].Text)
                };
                parametros.Validar();

                List<ParametrosResiduo> residuos = [];
                foreach (DataGridViewRow fila in _tablaResiduos.Rows)
                {
                    TipoResiduo tipo = (TipoResiduo)fila.Cells["Tipo"].Value!;
                    double densidad = LeerNumeroOpcional(fila.Cells["Densidad"].Value);
                    double aprovechable = LeerNumeroOpcional(fila.Cells["Aprovechamiento"].Value);
                    double emisiones = LeerNumeroOpcional(fila.Cells["Emisiones"].Value);
                    string fuenteDensidad = Convert.ToString(fila.Cells["FuenteDensidad"].Value) ?? string.Empty;
                    string fuenteAprovechamiento = Convert.ToString(fila.Cells["FuenteAprovechamiento"].Value) ?? string.Empty;
                    string fuenteEmisiones = Convert.ToString(fila.Cells["FuenteEmisiones"].Value) ?? string.Empty;

                    if (densidad == 0 && aprovechable == 0 && emisiones == 0
                        && string.IsNullOrWhiteSpace(fuenteDensidad)
                        && string.IsNullOrWhiteSpace(fuenteAprovechamiento)
                        && string.IsNullOrWhiteSpace(fuenteEmisiones))
                    {
                        continue;
                    }

                    residuos.Add(new ParametrosResiduo
                    {
                        Tipo = tipo,
                        DensidadKgPorLitro = densidad,
                        PorcentajeAprovechablePct = aprovechable,
                        FactorCO2eEvitadoKgPorKg = emisiones,
                        FuenteDensidad = fuenteDensidad,
                        FuenteAprovechamiento = fuenteAprovechamiento,
                        FuenteEmisiones = fuenteEmisiones
                    });
                }

                ValidarDensidadesPendientes(residuos);

                ParametrosOperacion = parametros;
                ParametrosResiduos = residuos;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Configuración no válida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ValidarDensidadesPendientes(IReadOnlyCollection<ParametrosResiduo> residuos)
        {
            foreach (TipoResiduo tipo in _tiposDensidadPendiente)
            {
                ParametrosResiduo? parametros = residuos.FirstOrDefault(p => p.Tipo == tipo);
                DataGridViewRow fila = _tablaResiduos.Rows
                    .Cast<DataGridViewRow>()
                    .First(row => (TipoResiduo)row.Cells["Tipo"].Value! == tipo);

                if (parametros is null || !double.IsFinite(parametros.DensidadKgPorLitro)
                    || parametros.DensidadKgPorLitro <= 0)
                {
                    _tablaResiduos.CurrentCell = fila.Cells["Densidad"];
                    _tablaResiduos.Focus();
                    throw new ArgumentException($"La densidad de {tipo} debe ser positiva y finita, expresada en kg/l.");
                }

                if (string.IsNullOrWhiteSpace(parametros.FuenteDensidad))
                {
                    _tablaResiduos.CurrentCell = fila.Cells["FuenteDensidad"];
                    _tablaResiduos.Focus();
                    throw new ArgumentException($"Añade una fuente documental para la densidad de {tipo}.");
                }
            }
        }

        private static string Formatear(double valor) => valor == 0 ? string.Empty : valor.ToString("R", CultureInfo.CurrentCulture);

        private static double LeerNumeroOpcional(object? valor)
        {
            string texto = Convert.ToString(valor, CultureInfo.CurrentCulture) ?? string.Empty;
            return string.IsNullOrWhiteSpace(texto) ? 0 : LeerNumero(texto);
        }

        private static double LeerNumero(string texto)
        {
            if (double.TryParse(texto, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double resultado)
                && double.IsFinite(resultado))
            {
                return resultado;
            }

            throw new ArgumentException($"El valor «{texto}» no es un número válido.");
        }
    }
}
