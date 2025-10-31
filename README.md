# SvcAnalysisOrchestrator
Microservice that orchestrates analysis. Consumes `RequestAnalysis` commands from RabbitMQ and publishes `analysis.started` and `analysis.completed` event exchanges.

---

## Codebase Architecture

![Codebase Architecture](docs/images/svc-analysis-orchestrator-codebase-architecture.jpg)

---

## Design Class Diagram

![Design Class Diagram](docs/images/svc-analysis-orchestrator-dcd.jpg)

---

See the [full system overview](https://github.com/team-2-devs/infra-core) in the **infra-core** repository.