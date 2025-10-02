using HMI_template_v0._2.Services; // Aggiungi il using del tuo servizio
using System;
using System.Windows;

namespace HMI_template_v0._2
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Avvia la connessione OPC UA in background
            await OpcUaService.Instance.ConnectAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Disconnetti in modo pulito quando l'app si chiude
            OpcUaService.Instance.Disconnect();
            base.OnExit(e);
        }
    }
}