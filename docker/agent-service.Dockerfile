# Build context: repo root (contains src/agent-service/)

FROM python:3.14-slim AS runtime
WORKDIR /app

# Copy and install dependencies first for layer caching
COPY src/agent-service/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Copy the agent service package
COPY src/agent-service/ ./

# Install the package itself
RUN pip install --no-cache-dir .

# gRPC port (used for Agent gRPC interface)
EXPOSE 50051

# FastAPI/HTTP port (used for REST interface)
EXPOSE 8000

# Placeholder entrypoint — real entry point (app.main) lands in T040/T041
# Future: CMD ["python", "-m", "app.main"]
CMD ["python", "-c", "print('Agent service placeholder — entry point defined in T040/T041')"]
