using HMI_template_v0._2.Configuration;
using HMI_template_v0._2.Services;
using Opc.UaFx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Runtime.CompilerServices;

namespace HMI_template_v0._2.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly OpcUaService _opcUaService;
        private readonly Dictionary<string, string> _nodeIdMap = new Dictionary<string, string>();
        private readonly Dictionary<string, Action<OpcValue>> _updateActions = new Dictionary<string, Action<OpcValue>>();
        private readonly Dictionary<string, object> _tagValues = new Dictionary<string, object>();
        private readonly Dictionary<string, OpcTagConfig> _tagConfigs = new Dictionary<string, OpcTagConfig>();

        // ===== MODIFICA 1: Aggiungi un flag per prevenire i loop di scrittura =====
        private bool _isUpdatingFromPlc = false;

        private string _connectionStatus = "Connecting...";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public object this[string tagName]
        {
            get
            {
                _tagValues.TryGetValue(tagName, out var value);
                return value;
            }
            set
            {
                if (!_tagValues.ContainsKey(tagName) || Equals(_tagValues[tagName], value))
                    return;

                _tagValues[tagName] = value;
                OnPropertyChanged($"Item[{tagName}]");

                // ===== MODIFICA 2: Controlla il flag prima di scrivere sul PLC =====
                // Non scrivere di nuovo sul PLC se il valore è appena arrivato dal PLC stesso.
                if (_isUpdatingFromPlc)
                    return;

                if (_tagConfigs.TryGetValue(tagName, out var config) && config.Access == "ReadWrite")
                {
                    WriteNodeValue(tagName, value);
                }
            }
        }

        public DashboardViewModel()
        {
            _opcUaService = OpcUaService.Instance;
            _opcUaService.ValueChanged += OnOpcValueChanged;
            InitializeFromConfig();
            InitializeOpcConnection();
        }

        private void InitializeFromConfig()
        {
            foreach (var tag in AppConfig.OpcTags)
            {
                string fullNodeId = AppConfig.OpcUa.NodeIdPrefix + tag.Name;
                _nodeIdMap[tag.Name] = fullNodeId;
                _tagConfigs[tag.Name] = tag;
                _tagValues[tag.Name] = GetDefaultValueForType(tag.DataType);

                // ===== MODIFICA 3: Semplifica l'azione di aggiornamento =====
                // Ora l'azione chiama semplicemente il setter pubblico.
                // Tutta la logica è centralizzata nel setter dell'indicizzatore.
                _updateActions[fullNodeId] = (opcValue) =>
                {
                    var convertedValue = ConvertOpcValue(opcValue, tag.DataType);
                    // Usa il Dispatcher per assicurarti che il setter sia chiamato sul thread UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this[tag.Name] = convertedValue;
                    });
                };
            }
        }

        private void OnOpcValueChanged(string nodeId, OpcValue value)
        {
            if (value.Status.IsGood && _updateActions.ContainsKey(nodeId))
            {
                try
                {
                    // Imposta il flag prima di processare il valore
                    _isUpdatingFromPlc = true;
                    _updateActions[nodeId](value);
                }
                finally
                {
                    // Assicurati che il flag venga sempre resettato, anche in caso di errore
                    _isUpdatingFromPlc = false;
                }
            }
        }

        private async void InitializeOpcConnection()
        {
            bool connected = await _opcUaService.ConnectAsync();
            if (connected)
            {
                ConnectionStatus = "Connected";
                _opcUaService.SubscribeToDataChanges(_nodeIdMap.Values);
            }
            else
            {
                ConnectionStatus = "Connection Failed";
            }
        }

        private void WriteNodeValue(string friendlyName, object value)
        {
            if (_nodeIdMap.TryGetValue(friendlyName, out string nodeId))
            {
                _opcUaService.WriteValueAsync(nodeId, value);
            }
        }

        #region Helper Methods (invariati)
        private object GetDefaultValueForType(string type) => type switch
        {
            "Boolean" => default(bool),
            "Byte" => default(byte),
            "Int16" => default(short),
            "UInt16" => default(ushort),
            "Int32" => default(int),
            "UInt32" => default(uint),
            "Int64" => default(long),
            "UInt64" => default(ulong),
            "Double" => default(double),
            "String" => default(string),
            _ => null
        };

        private object ConvertOpcValue(OpcValue value, string type) => type switch
        {
            "Boolean" => value.As<bool>(),
            "Byte" => value.As<byte>(),
            "Int16" => value.As<short>(),
            "UInt16" => value.As<ushort>(),
            "Int32" => value.As<int>(),
            "UInt32" => value.As<uint>(),
            "Int64" => value.As<long>(),
            "UInt64" => value.As<ulong>(),
            "Double" => value.As<double>(),
            "String" => value.As<string>(),
            _ => value.Value
        };
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}