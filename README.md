# svc-analysis-orchestrator
Microservice that orchestrates analysis. Consumes `tu.image.uploaded` and `tu.recognition.completed` events from RabbitMQ and publishes `analysis.started` and `analysis.completed` events to RabbitMQ.
>**Note:** Although **svc-analysis-orchestrator** should be the central entry point for all analysis workflows, the current implementation temporarily allows **svc-ai-vision-adapter** to consume the `tu.image.uploaded` event directly from **tu-ingestion-service**. This was a decision by the team to simplify early development. In a future iteration, the architecture would be expected to align with the intended responsibility boundaries, with **svc-analysis-orchestrator** serving as the sole consumer of the `tu.image.uploaded` event and orchestrating the full analysis pipeline.

<!-- ---

## Codebase Architecture

![Codebase Architecture](docs/images/svc-analysis-orchestrator-codebase-architecture.jpg)

---

## Design Class Diagram

![Design Class Diagram](docs/images/svc-analysis-orchestrator-dcd.jpg) -->

---

See the [full system overview](https://github.com/team-2-devs/infra-core) in the **infra-core** repository.