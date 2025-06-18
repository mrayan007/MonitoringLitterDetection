# FastAPIMonitoring/Dockerfile

# Stap 1: Gebruik een officiële Python runtime als basisimage
# Update deze lijn!
FROM python:3.11-slim-buster

# Rest van je Dockerfile blijft hetzelfde
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY . .
EXPOSE 8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]