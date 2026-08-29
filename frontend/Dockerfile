# Multi-stage production build for Angular 19 Client
FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/package*.json ./
RUN npm ci --legacy-peer-deps

COPY frontend/ ./
RUN npm run build

# Nginx web server stage
FROM nginx:alpine AS final
COPY --from=build /app/dist/supplychainx/browser /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
