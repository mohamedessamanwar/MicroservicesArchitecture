---
name: docker-compose-troubleshooter
description: Standard operating procedure for running docker compose, checking dependencies, and troubleshooting errors.
---

# Docker Compose Troubleshooter

When you are asked to run Docker Compose, or when you add a new dependency, you MUST follow these standard operating procedures to ensure the system is healthy:

1. **Run Docker Compose:**
   Always run with dependencies and detached mode:
   `docker compose up -d` or `docker compose up -d <service_name>`

2. **Check Container Status:**
   After starting, immediately check if everything is running and healthy:
   `docker compose ps`

3. **Troubleshoot Errors (If Any):**
   If a container is marked as "unhealthy" or "exited", do NOT guess the issue. You MUST:
   - Check the logs of the failing container: `docker compose logs --tail=50 <service_name>`
   - Identify missing configurations (e.g., missing environment variables, wrong ports, missing networks).

4. **Run Health Endpoints:**
   Verify the APIs are actually responding by pinging their health endpoints.
   For example:
   `curl http://localhost:5000/health` (API Gateway)
   `curl http://localhost:8080/health/live` (If exposed locally)

5. **Investigate Container Logs:**
   Even if it says "healthy", if a request fails, investigate the logs of all involved containers to trace the request (Gateway -> Microservice -> DB/RabbitMQ).
