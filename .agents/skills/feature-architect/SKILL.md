---
name: feature-architect
description: Used when adding a new feature. Reads existing skills, analyzes components, and proposes the best architecture, business logic, and edge cases.
---

# Feature Architect Guidelines

When you are asked to add a new feature to the platform, you MUST follow these steps before writing any code:

1. **Read Core Skills:**
   First, read the guidelines in `project-business-logic` and `microservices-architecture` to understand the overarching system goals and individual service responsibilities.

2. **Analyze Components:**
   Determine which microservices will be affected by the new feature. Will it require a new service, or modifications to existing ones (e.g., adding an event to RabbitMQ, adding a table to Postgres)?

3. **Propose Architecture & Business Logic:**
   Present a clear architectural design for the feature:
   - Which service owns the data?
   - How will services communicate (Sync HTTP vs Async RabbitMQ)?
   - What are the step-by-step business logic flows?

4. **Identify Edge Cases:**
   Identify and document any potential edge cases you might not have noticed initially:
   - What happens if the network fails during the flow?
   - Do we need Saga compensation?
   - How does this feature behave in a multi-tenant (Egypt vs USA) environment?
   - Are there any race conditions?

5. **Wait for Approval:**
   Present this analysis to the user in a markdown artifact and wait for their approval before implementing the feature.
