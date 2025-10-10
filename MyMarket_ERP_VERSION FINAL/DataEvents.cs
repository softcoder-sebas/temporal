using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public enum DataEventType
    {
        ClientesChanged,
        EmpleadosChanged,
        InventarioChanged,
        ComprasChanged,
        ContabilidadChanged,
        FacturacionChanged
    }

    public sealed class DataEventPayload
    {
        public int? EntityId { get; init; }
        public string? EntityKey { get; init; }
        public object? Metadata { get; init; }
    }

    public static class DataEvents
    {
        private const int DebounceMilliseconds = 200;

        private static readonly Dictionary<DataEventType, List<Subscription>> _subscriptions =
            Enum.GetValues<DataEventType>().ToDictionary(k => k, _ => new List<Subscription>());

        private static readonly Dictionary<DataEventType, CancellationTokenSource> _debounceTokens =
            new();

        private static readonly object _gate = new();

        public static IDisposable Subscribe(DataEventType type, Control control, Action<DataEventPayload> handler)
        {
            if (control is null) throw new ArgumentNullException(nameof(control));
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var sub = new Subscription(type, control, handler);
            lock (_gate)
            {
                _subscriptions[type].Add(sub);
            }

            control.Disposed += (_, __) => sub.Dispose();
            return sub;
        }

        public static void Publish(DataEventType type, DataEventPayload? payload = null)
        {
            payload ??= new DataEventPayload();

            CancellationTokenSource cts;
            lock (_gate)
            {
                if (_debounceTokens.TryGetValue(type, out var existing))
                {
                    existing.Cancel();
                    existing.Dispose();
                }

                cts = new CancellationTokenSource();
                _debounceTokens[type] = cts;
            }

            _ = DebouncedPublishAsync(type, payload, cts);
        }

        public static IDisposable SubscribeClientes(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.ClientesChanged, control, handler);

        public static IDisposable SubscribeEmpleados(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.EmpleadosChanged, control, handler);

        public static IDisposable SubscribeInventario(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.InventarioChanged, control, handler);

        public static IDisposable SubscribeCompras(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.ComprasChanged, control, handler);

        public static IDisposable SubscribeContabilidad(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.ContabilidadChanged, control, handler);

        public static IDisposable SubscribeFacturacion(Control control, Action<DataEventPayload> handler) =>
            Subscribe(DataEventType.FacturacionChanged, control, handler);

        public static void PublishClientesChanged(int? id = null, string? key = null) =>
            Publish(DataEventType.ClientesChanged, new DataEventPayload { EntityId = id, EntityKey = key });

        public static void PublishEmpleadosChanged(int? id = null) =>
            Publish(DataEventType.EmpleadosChanged, new DataEventPayload { EntityId = id });

        public static void PublishInventarioChanged(int? id = null, string? sku = null) =>
            Publish(DataEventType.InventarioChanged, new DataEventPayload { EntityId = id, EntityKey = sku });

        public static void PublishComprasChanged(int? id = null) =>
            Publish(DataEventType.ComprasChanged, new DataEventPayload { EntityId = id });

        public static void PublishContabilidadChanged(int? id = null) =>
            Publish(DataEventType.ContabilidadChanged, new DataEventPayload { EntityId = id });

        public static void PublishFacturacionChanged(int? id = null) =>
            Publish(DataEventType.FacturacionChanged, new DataEventPayload { EntityId = id });

        private static async Task DebouncedPublishAsync(
            DataEventType type,
            DataEventPayload payload,
            CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(DebounceMilliseconds, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                lock (_gate)
                {
                    if (_debounceTokens.TryGetValue(type, out var current) && current == cts)
                    {
                        _debounceTokens.Remove(type);
                    }
                }

                cts.Dispose();
            }

            Dispatch(type, payload);
        }

        private static void Dispatch(DataEventType type, DataEventPayload payload)
        {
            List<Subscription> subscribers;
            lock (_gate)
            {
                subscribers = _subscriptions[type].ToList();
            }

            foreach (var sub in subscribers)
            {
                sub.Invoke(payload);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly WeakReference<Control> _target;
            private readonly Action<DataEventPayload> _handler;
            private bool _disposed;

            public Subscription(DataEventType type, Control control, Action<DataEventPayload> handler)
            {
                EventType = type;
                _target = new WeakReference<Control>(control);
                _handler = handler;
            }

            public DataEventType EventType { get; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                lock (_gate)
                {
                    if (_subscriptions.TryGetValue(EventType, out var list))
                    {
                        list.Remove(this);
                    }
                }
            }

            public void Invoke(DataEventPayload payload)
            {
                if (_disposed) return;
                if (!_target.TryGetTarget(out var control))
                {
                    Dispose();
                    return;
                }

                if (control.IsDisposed)
                {
                    Dispose();
                    return;
                }

                try
                {
                    void Execute()
                    {
                        if (_disposed || control.IsDisposed) return;
                        _handler(payload);
                    }

                    if (control.InvokeRequired)
                    {
                        control.BeginInvoke((Action)Execute);
                    }
                    else
                    {
                        Execute();
                    }
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                }
                catch (InvalidOperationException)
                {
                    Dispose();
                }
            }
        }
    }
}
