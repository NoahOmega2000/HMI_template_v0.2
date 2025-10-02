// Services/OpcUaService.cs
using HMI_template_v0._2.Configuration; // Aggiungi questo using
using Opc.UaFx;
using Opc.UaFx.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HMI_template_v0._2.Services
{
    public class OpcUaService
    {
        private static readonly Lazy<OpcUaService> _instance = new Lazy<OpcUaService>(() => new OpcUaService());
        public static OpcUaService Instance => _instance.Value;

        private OpcClient _client;
        public bool IsConnected => _client?.State == OpcClientState.Connected;
        public event Action<string, OpcValue> ValueChanged;

        private OpcUaService()
        {
            // Leggi le impostazioni dal file di configurazione
            var settings = AppConfig.OpcUa;

            _client = new OpcClient(settings.ServerUrl);

            _client.Security.EndpointPolicy = new OpcSecurityPolicy(OpcSecurityMode.SignAndEncrypt, OpcSecurityAlgorithm.Basic256Sha256);
            _client.Security.UserIdentity = new OpcClientIdentity(settings.Username, settings.Password);
        }

        // Il resto della classe (ConnectAsync, Disconnect, SubscribeToDataChanges, etc.)
        // rimane INVARIATO.

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected) return true;
            try
            {
                await Task.Run(() => _client.Connect());
                Debug.WriteLine("Connesso con successo al server OPC UA.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore di connessione OPC UA: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (IsConnected) _client.Disconnect();
        }

        public void SubscribeToDataChanges(IEnumerable<string> nodeIds)
        {
            if (!IsConnected) return;

            var itemsToSubscribe = new List<OpcSubscribeDataChange>();
            foreach (var nodeId in nodeIds)
            {
                itemsToSubscribe.Add(new OpcSubscribeDataChange(nodeId, HandleDataChange));
            }

            try
            {
                _client.SubscribeNodes(itemsToSubscribe.ToArray());
                Debug.WriteLine($"Sottoscrizione creata per {itemsToSubscribe.Count} nodi.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la sottoscrizione: {ex.Message}");
            }
        }

        private void HandleDataChange(object sender, OpcDataChangeReceivedEventArgs e)
        {

            var monitoredItem = (OpcMonitoredItem)sender;
            ValueChanged?.Invoke(monitoredItem.NodeId.ToString(), e.Item.Value);
        }

        public async Task<bool> WriteValueAsync(string nodeId, object value)
        {
            if (!IsConnected) return false;
            try
            {
                var status = await Task.Run(() => _client.WriteNode(nodeId, value));
                if (status.IsGood)
                {
                    Debug.WriteLine($"Valore '{value}' scritto con successo su {nodeId}.");
                    return true;
                }
                Debug.WriteLine($"Errore durante la scrittura su {nodeId}: {status}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante la scrittura: {ex.Message}");
                return false;
            }
        }
    }
}