# ---- Stage 1: Build Flutter Web ----
FROM ghcr.io/cirruslabs/flutter:3.24.5 AS build

WORKDIR /app

# Copy dependency files first for better layer caching
COPY pubspec.yaml pubspec.lock ./
RUN flutter pub get

# Copy the rest of the source code
COPY . .

# Generate secrets/prod_config.json from build args
ARG AUTHORIZATION_ENDPOINT
ARG END_SESSION_ENDPOINT
ARG IDENTIFIER
ARG SECRET
ARG PEOPLE_MANAGEMENT_URL

RUN mkdir -p secrets && \
    echo "{\"authorization_endpoint\":\"$AUTHORIZATION_ENDPOINT\",\"end_session_endpoint\":\"$END_SESSION_ENDPOINT\",\"identifier\":\"$IDENTIFIER\",\"secret\":\"$SECRET\",\"people_management_url\":\"$PEOPLE_MANAGEMENT_URL\"}" \
    > secrets/prod_config.json

RUN flutter build web --release --dart-define-from-file=secrets/prod_config.json

# ---- Stage 2: Serve with Nginx ----
FROM nginx:alpine

COPY --from=build /app/build/web /usr/share/nginx/html

# SPA fallback: redirect all routes to index.html
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
