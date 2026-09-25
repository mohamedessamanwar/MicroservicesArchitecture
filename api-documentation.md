# Microservices API Documentation

All API calls below are routed through the API Gateway via HTTP on port 8080. 
*(Note: I updated these from HTTPS to HTTP on port 8080 to fix the SSL/TLS handshake connection abortion issues happening locally through Docker).*

## 🛍️ Product Service Endpoints

**1. Create a new Product**
```bash
curl -X POST "http://localhost:8080/api/v1/Products" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Product Name",
       "description": "Product Description",
       "price": 100.00
     }'
```

**2. Decrease Product Stock**
```bash
curl -X POST "http://localhost:8080/api/v1/Products/{id}/decrease-count?amount=1"
```

**3. Increase Product Stock**
```bash
curl -X POST "http://localhost:8080/api/v1/Products/{id}/increase-count?amount=1"
```

**4. Bulk Decrease Stock**
```bash
curl -X POST "http://localhost:8080/api/v1/Products/decrease-bulk" \
     -H "Content-Type: application/json" \
     -d '[
       {
         "productId": "00000000-0000-0000-0000-000000000000",
         "quantity": 1
       }
     ]'
```

**5. Bulk Increase Stock**
```bash
curl -X POST "http://localhost:8080/api/v1/Products/increase-bulk" \
     -H "Content-Type: application/json" \
     -d '[
       {
         "productId": "00000000-0000-0000-0000-000000000000",
         "quantity": 1
       }
     ]'
```

---

## 💳 Payment Service Endpoints

**1. Create a Payment**
```bash
curl -X POST "http://localhost:8080/api/v1/Payments" \
     -H "Content-Type: application/json" \
     -d '{
       "orderId": "00000000-0000-0000-0000-000000000000",
       "amount": 100.00,
       "currency": "USD"
     }'
```

**2. Update Payment Status**
```bash
curl -X PUT "http://localhost:8080/api/v1/Payments/{id}/status?status=1"
```

---

## 📦 Order Service Endpoints

**1. Get All Orders**
```bash
curl -X GET "http://localhost:8080/api/v1/Orders"
```

**2. Create an Order**
```bash
curl -X POST "http://localhost:8080/api/v1/Orders" \
     -H "Content-Type: application/json" \
     -H "X-Idempotency-Key: my-unique-key-12345" \
     -d '{
       "customerId": "00000000-0000-0000-0000-000000000000",
       "items": [
         {
           "productId": "00000000-0000-0000-0000-000000000000",
           "quantity": 1,
           "price": 100.00
         }
       ]
     }'
```

**3. Process Payment for an Order**
```bash
curl -X POST "http://localhost:8080/api/v1/Orders/{id}/process-payment"
```

---

## 🏗 Infrastructure Services

You can access these services directly from your browser:

* **Grafana:** [http://localhost:3000](http://localhost:3000) (Credentials: `admin`/`admin`)
* **Prometheus:** [http://localhost:9090](http://localhost:9090)
* **Tempo:** [http://localhost:3200](http://localhost:3200)
* **Loki:** [http://localhost:3100](http://localhost:3100)
* **OTEL Collector:** `http://localhost:4318` / `http://localhost:8889`
