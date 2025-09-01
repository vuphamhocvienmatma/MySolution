# Docker Configuration for Development Environment

This document provides a collection of Docker commands and a Docker Compose file to set up the necessary infrastructure for the project.

---
## 1. Individual Docker Commands 🐳

This section guides you on how to pull and run each service independently.

### **SQL Server** 🗄️
* **Pull Image:**
    ```bash
    docker pull [mcr.microsoft.com/mssql/server:2022-latest](https://mcr.microsoft.com/mssql/server:2022-latest)
    ```
* **Run Container:**
    ```bash
    docker run -d --name mssql-server -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Password123" -p 1433:1433 [mcr.microsoft.com/mssql/server:2022-latest](https://mcr.microsoft.com/mssql/server:2022-latest)
    ```
    > **Note:** `-e "ACCEPT_EULA=Y"` is required. Please replace `YourStrong!Password123` with a strong password.

### **Redis** caching
* **Pull Image:**
    ```bash
    docker pull redis:alpine
    ```
* **Run Container:**
    ```bash
    docker run -d --name redis-stack -p 6379:6379 redis:alpine
    ```

### **Elasticsearch** 🔍
* **Pull Image:**
    ```bash
    docker pull elasticsearch:8.14.1
    ```
* **Run Container:**
    ```bash
    docker run -d --name elasticsearch -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" elasticsearch:8.14.1
    ```
    > **Note:** `-e "discovery.type=single-node"` is used to run as a single node for a development environment.

### **RabbitMQ** 🐇
* **Pull Image (with management UI):**
    ```bash
    docker pull rabbitmq:3-management
    ```
* **Run Container:**
    ```bash
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
    ```
    > **Web UI:** Access at `http://localhost:15672` (user: `guest`, pass: `guest`).

### **MongoDB** 🍃
* **Pull Image:**
    ```bash
    docker pull mongo
    ```
* **Run Container:**
    ```bash
    docker run -d --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=password mongo
    ```

---
## 2. Best Practice: Using Docker Compose ✨

The best way to manage all these services is by using a `docker-compose.yml` file.

### **Contents of `docker-compose.yml` file**
```yaml
version: '3.8'

services:
  # SQL Server Service
  sql-server:
    image: [mcr.microsoft.com/mssql/server:2022-latest](https://mcr.microsoft.com/mssql/server:2022-latest)
    container_name: mssql-server
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong!Password123
    ports:
      - "1433:1433"
    volumes:
      - sql-server-data:/var/opt/mssql

  # Redis Service
  redis:
    image: redis:alpine
    container_name: redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  # Elasticsearch Service
  elasticsearch:
    image: elasticsearch:8.14.1
    container_name: elasticsearch
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false # Disable security for dev environment
    ports:
      - "9200:9200"
      - "9300:9300"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data

  # RabbitMQ Service
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

  # MongoDB Service
  mongodb:
    image: mongo
    container_name: mongodb
    ports:
      - "27017:27017"
    environment:
      - MONGO_INITDB_ROOT_USERNAME=admin
      - MONGO_INITDB_ROOT_PASSWORD=password
    volumes:
      - mongodb-data:/data/db

# Docker Volumes for data persistence
volumes:
  sql-server-data:
  redis-data:
  elasticsearch-data:
  rabbitmq-data:
  mongodb-data:
