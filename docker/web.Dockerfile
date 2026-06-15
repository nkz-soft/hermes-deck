# Build context: repo root (contains src/HermesDeck.Web/)

# Stage 1: Build the React/Vite app
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files first for layer caching
COPY src/HermesDeck.Web/package.json src/HermesDeck.Web/package-lock.json ./

# Install dependencies
RUN npm ci

# Copy remaining source
COPY src/HermesDeck.Web/ ./

# Build the static output to dist/
RUN npm run build

# Stage 2: Serve with nginx
FROM nginx:alpine AS runtime

EXPOSE 80

# Copy built static assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html
