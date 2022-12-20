using System.Diagnostics;

using Prometheus; // for telemetry
namespace promMetrics
{

    //All kinds of metrics
    public class PrometheusMetrics
    {
        private readonly Counter requestCounter = Metrics.CreateCounter("requests", "Number of requests made");
        public void IncrementRequestCounter()
        {
            requestCounter.Inc();
        }
        private readonly Process _process = Process.GetCurrentProcess();
        private readonly Gauge _cpuUsage = Metrics.CreateGauge("app_cpu_usage", "The current CPU usage of the process.");
        private readonly Gauge _memoryUsage = Metrics.CreateGauge("app_memory_usage", "The current memory usage of the process.");
        public void UpdateCpuMemMetrics()
        {
            _cpuUsage.Set(_process.TotalProcessorTime.TotalSeconds);
            _memoryUsage.Set(_process.WorkingSet64);
        }
    }


}