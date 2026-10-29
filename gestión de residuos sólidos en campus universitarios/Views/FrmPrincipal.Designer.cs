namespace gestión_de_residuos_sólidos_en_campus_universitarios
{
    partial class Main
    {
        private System.ComponentModel.IContainer? components;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1360, 860);
            MinimumSize = new Size(1120, 720);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Gestión de rutas y residuos del campus";
        }
    }
}
