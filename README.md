# observability-flow

observability-flow is a local, end-to-end observability demonstration built around a
.NET 8 checkout API and the Grafana observability stack.

The checkout service produces metrics, traces, and structured logs with
OpenTelemetry. An OpenTelemetry Collector receives and processes the telemetry,
then routes it to Prometheus/Mimir, Loki, and Tempo. Grafana provides a single
place to explore and correlate all three signals, while MinIO provides
S3-compatible object storage for the observability backends.

## What the project demonstrates

- ASP.NET Core tracing with ASP.NET Core, `HttpClient`, and SQL Client
  instrumentation.
- Custom request counters and latency histograms.
- Structured JSON logs containing `trace_id` and `span_id`.
- OTLP/gRPC export from the application to the OpenTelemetry Collector.
- Attribute normalization for `service`, `env`, `route`, and `status`.
- Tail-based trace sampling.
- Prometheus exemplars that link metric observations to Tempo traces.
- Links from Loki logs to Tempo traces using `trace_id`.
- Links from Tempo traces back to related Loki logs.
- S3-compatible storage with MinIO.
- Automatically provisioned Grafana datasources and dashboard.

## Architecture

```text
                              ┌────────────────────────────┐
                              │          Grafana           │
                              │ metrics + logs + traces    │
                              └───────┬───────┬───────┬────┘
                                      │       │       │
                                    Mimir    Loki    Tempo
                                      │       │       │
                                      └───────┼───────┘
                                              │
                                            MinIO

checkout-service
      │ OTLP: traces, metrics, logs
      ▼
OpenTelemetry Collector
      ├── traces ─────────────────────────────► Tempo
      ├── logs ───────────────────────────────► Loki
      └── metrics ─► Prometheus scrape ───────► Mimir
```

The detailed metrics path is:

```text
checkout-service
  → OTLP metrics
  → OpenTelemetry Collector
  → Prometheus exporter on port 9464
  → Prometheus scrape
  → Prometheus remote-write
  → Mimir
```

## Components

| Component | Purpose |
| --- | --- |
| `checkout-service` | Sample .NET 8 API that emits logs, metrics, and traces |
| OpenTelemetry Collector | Receives OTLP telemetry, normalizes attributes, samples traces, and routes signals |
| Prometheus | Scrapes the Collector's Prometheus endpoint and remote-writes metrics to Mimir |
| Mimir | Long-term Prometheus-compatible metrics storage |
| Loki | Structured log storage and LogQL query engine |
| Tempo | Distributed trace storage and TraceQL query engine |
| Grafana | Dashboards, exploration, and signal correlation |
| MinIO | S3-compatible object storage for Tempo, Loki, and Mimir |

All services run on the `observability` Docker network.

## Prerequisites

- Docker Engine
- Docker Compose v2 (`docker compose`)
- `curl`
- Bash, if you want to use the traffic-generation script

The application does not require a locally installed .NET SDK when it is run
through Docker.

## Run the project

From the repository root:

```bash
docker compose up -d --build
```

Check container status:

```bash
docker compose ps --all
```

The `minio-init` container is expected to show `Exited (0)`. It is a one-time,
idempotent initialization job that creates the required buckets before Tempo,
Loki, and Mimir start.

Verify the checkout service:

```bash
curl --fail http://localhost:5000/health
```

Expected response:

```json
{"status":"healthy"}
```

Follow all container logs:

```bash
docker compose logs -f
```

Follow only the application and Collector:

```bash
docker compose logs -f checkout-service otel-collector
```

## Generate traffic

Run the continuous traffic generator:

```bash
bash scripts/generate-traffic.sh
```

Press `Ctrl+C` to stop it.

The script sends a request every 0.1 seconds and cycles through:

- Successful `POST /checkout`
- Invalid `POST /checkout` returning `400`
- Missing checkout lookup returning `404`
- Unsupported `PUT /checkout` returning `405`
- Successful `GET /checkout`

The generated errors are `4xx` responses. The provisioned Grafana error-rate
panel counts only `5xx` responses, so that panel can remain at zero while the
traffic generator is running.

## Checkout API

Swagger UI:

[http://localhost:5000/swagger](http://localhost:5000/swagger)

OpenAPI JSON:

[http://localhost:5000/swagger/v1/swagger.json](http://localhost:5000/swagger/v1/swagger.json)

Available endpoints:

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/health` | Service health check |
| `POST` | `/checkout` | Create an in-memory checkout order |
| `GET` | `/checkout` | List checkout orders |
| `GET` | `/checkout/{orderId}` | Get one checkout order |
| `DELETE` | `/checkout/{orderId}` | Mark a checkout order as cancelled |

Create a checkout manually:

```bash
curl --fail-with-body \
  --request POST \
  --header 'Content-Type: application/json' \
  --data '{"cartId":"cart-123","itemCount":2}' \
  http://localhost:5000/checkout
```

The returned `orderId` can be used with the `GET` and `DELETE` endpoints.
Checkout state is held in memory and is reset whenever the application container
is recreated.

## Where to view everything

| Service | URL | Credentials or notes |
| --- | --- | --- |
| Grafana | [http://localhost:3000](http://localhost:3000) | `admin` / `admin` |
| Checkout dashboard | [Checkout Service Observability](http://localhost:3000/d/checkout-observability/checkout-service-observability) | Provisioned automatically |
| Checkout Swagger UI | [http://localhost:5000/swagger](http://localhost:5000/swagger) | No authentication |
| Prometheus | [http://localhost:9090](http://localhost:9090) | PromQL UI |
| Loki API | [http://localhost:3100](http://localhost:3100) | Use Grafana Explore for a UI |
| Tempo API | [http://localhost:3200](http://localhost:3200) | Use Grafana Explore for a UI |
| MinIO Console | [http://localhost:9001](http://localhost:9001) | `observability` / `observability-secret` |
| MinIO S3 API | [http://localhost:9000](http://localhost:9000) | Used internally by the backends |
| Collector OTLP/gRPC | `localhost:4317` | Telemetry ingestion endpoint |
| Collector OTLP/HTTP | `localhost:4318` | Telemetry ingestion endpoint |
| Collector Prometheus exporter | [http://localhost:9464/metrics](http://localhost:9464/metrics) | Scraped by Prometheus |

Mimir is intentionally available only inside the Docker network on port `9009`.
View Mimir data through Grafana's `Prometheus / Mimir` datasource. Grafana,
Loki, and Tempo use the backend APIs; the MinIO console is only for inspecting
stored objects and buckets.

In Grafana, use a relative time range such as **Last 5 minutes** and set refresh
to **5s** when watching live traffic.

## Grafana dashboard

The provisioned `Checkout Service Observability` dashboard includes:

- Checkout p95 latency from Mimir.
- Checkout `5xx` error percentage from Mimir.
- Recent sampled checkout traces from Tempo.
- Structured checkout-service logs from Loki.

The datasources are provisioned with:

- Metrics-to-traces links using Prometheus exemplars.
- Logs-to-traces links using the Loki `trace_id` field.
- Traces-to-logs links from Tempo to Loki.

## Useful PromQL queries

Open Prometheus at [http://localhost:9090](http://localhost:9090), or select the
`Prometheus / Mimir` datasource in Grafana Explore.

Current request rate in requests per second:

```promql
sum(rate(checkout_http_server_requests_total{service="checkout-service"}[1m]))
```

Requests during the last minute:

```promql
sum(increase(checkout_http_server_requests_total{service="checkout-service"}[1m]))
```

Request rate grouped by route and status:

```promql
sum by (route, status) (
  rate(checkout_http_server_requests_total{service="checkout-service"}[1m])
)
```

Percentage of `4xx` and `5xx` responses:

```promql
100 *
sum(rate(checkout_http_server_requests_total{
  service="checkout-service",
  status=~"[45].."
}[1m]))
/
clamp_min(
  sum(rate(checkout_http_server_requests_total{
    service="checkout-service"
  }[1m])),
  0.000000001
)
```

P95 request latency:

```promql
histogram_quantile(
  0.95,
  sum by (le) (
    rate(checkout_http_server_request_duration_milliseconds_bucket{
      service="checkout-service"
    }[5m])
  )
)
```

## Useful LogQL queries

Open Grafana Explore and select the `Loki` datasource.

All checkout-service logs:

```logql
{service="checkout-service"}
```

Log entries per second:

```logql
sum(rate({service="checkout-service"}[1m]))
```

Logs for a specific trace:

```logql
{service="checkout-service"} | trace_id="<TRACE_ID>"
```

Expand a Loki log entry in Grafana and select **View trace** to open the
corresponding trace in Tempo.

## Useful TraceQL query

Open Grafana Explore, select the `Tempo` datasource, and use:

```traceql
{ resource.service.name = "checkout-service" }
```

## Trace sampling

The Collector applies tail-based sampling:

- Keep every trace whose span status is `ERROR`.
- Keep every trace whose duration exceeds two seconds.
- Keep 10% of normal traces.

The sampling decision waits for 10 seconds so the Collector can evaluate the
complete trace.

Logs and traces are exported independently. Loki can therefore contain a log
with a valid `trace_id` while Tempo returns `Trace Not Found` if that normal
trace was part of the 90% discarded by sampling. This is expected behavior.

For local debugging, normal-trace sampling can temporarily be changed to 100% in
`otel/otel-collector-config.yaml`, followed by:

```bash
docker compose up -d --force-recreate otel-collector
```

Only new traces are affected. A trace that was already sampled out cannot be
recovered.

## MinIO storage

The `minio-init` service creates:

```text
tempo-traces
loki-data
mimir-blocks
mimir-ruler
mimir-alertmanager
```

Tempo writes trace blocks such as Parquet data, indexes, bloom filters, and
metadata into `tempo-traces`.

Loki keeps recent logs in its ingester and WAL before flushing compressed chunks
and indexes to `loki-data`. Mimir similarly keeps recent samples in its TSDB head
and WAL before uploading blocks to `mimir-blocks`. Consequently, data can be
queryable in Grafana before corresponding objects become visible in MinIO.

The local backend volumes remain necessary for WAL files, active data, caches,
and temporary processing even though long-term blocks are stored in MinIO.

## Telemetry timing

When traffic is running:

- Metrics are exported and scraped on short intervals, normally appearing within
  several seconds.
- Logs are batched by the Collector and normally appear within several seconds.
- Traces wait at least 10 seconds for the tail-sampling decision and can take
  longer to become searchable while Tempo completes a trace block.
- MinIO objects can appear later than data in Grafana because each backend
  buffers and compacts active data before uploading it.

## Stop or reset the stack

Stop and remove the containers while preserving data volumes:

```bash
docker compose down
```

Remove the containers and all project volumes:

```bash
docker compose down --volumes
```

The second command permanently deletes locally stored Prometheus data, backend
WAL data, and every MinIO bucket and object.

## Rebuild after application changes

After changing the .NET application:

```bash
docker compose up -d --build checkout-service
```

After changing a backend configuration, recreate the corresponding service. For
example:

```bash
docker compose up -d --force-recreate otel-collector tempo loki mimir
```

## Project layout

```text
.
├── observability-flow/             # .NET 8 checkout service
│   ├── Application/                # Checkout application logic
│   ├── Contracts/                  # Request and response models
│   ├── Controllers/                # API controllers
│   ├── Extensions/                 # Logging, OTel, OpenAPI, and DI setup
│   ├── Middleware/                 # Request metrics and structured logging
│   ├── Telemetry/                  # Custom meter and instruments
│   └── Dockerfile
├── grafana/provisioning/           # Datasources and checkout dashboard
├── loki/config.yml
├── mimir/mimir.yaml
├── otel/otel-collector-config.yaml
├── prometheus/prometheus.yml
├── tempo/config.yml
├── scripts/generate-traffic.sh
└── docker-compose.yml
```

## Local-development warning

This stack is designed for local learning and demonstration. It uses:

- Hardcoded development credentials.
- Plain HTTP without TLS.
- Single-node backends and MinIO.
- Disabled authentication for some backend APIs.
- A replication factor of one.
- Mutable `latest` image tags for several observability components.

Do not deploy the Compose file to a public or production environment without
secrets management, authentication, TLS, pinned versions, retention policies,
backups, and a highly available storage design.
