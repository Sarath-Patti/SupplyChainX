# Multi-stage production build for Spring Boot process-service
FROM maven:3.9.8-eclipse-temurin-21-alpine AS builder
WORKDIR /build

# Cache dependencies
COPY pom.xml .
RUN mvn dependency:go-offline -B

# Copy source and build executable JAR
COPY src ./src
RUN mvn package -DskipTests

# Runtime stage using eclipse-temurin JRE 21 alpine
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app

# Non-root user execution
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser:appgroup

COPY --from=builder /build/target/process-service-1.0.0-SNAPSHOT.jar app.jar

EXPOSE 8081

ENTRYPOINT ["java", "-jar", "app.jar"]
