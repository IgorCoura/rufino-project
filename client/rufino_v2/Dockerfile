# ---- Stage 1: Build Flutter Web ----
FROM ghcr.io/cirruslabs/flutter:stable AS build

WORKDIR /app

# Copy dependency files first for better layer caching
COPY pubspec.yaml pubspec.lock ./
RUN flutter pub get

# Copy the rest of the source code
COPY . .

# Generate secrets/prod_config.json from build args. The app defaults to the
# Authorization Code Flow + PKCE, so only the auth-code endpoints are required;
# Direct Access Grants secrets (authorization_endpoint, secret) are no longer
# part of the production build.
ARG AUTH_CODE_AUTHORIZATION_ENDPOINT
ARG AUTH_CODE_TOKEN_ENDPOINT
ARG END_SESSION_ENDPOINT
ARG IDENTIFIER
ARG PEOPLE_MANAGEMENT_URL
ARG ERROR_MONITORING_ENABLED
ARG ERROR_MONITORING_DSN
ARG ERROR_MONITORING_TRACES_SAMPLE_RATE

RUN mkdir -p secrets && \
    echo "{\"auth_code_authorization_endpoint\":\"$AUTH_CODE_AUTHORIZATION_ENDPOINT\",\"auth_code_token_endpoint\":\"$AUTH_CODE_TOKEN_ENDPOINT\",\"end_session_endpoint\":\"$END_SESSION_ENDPOINT\",\"identifier\":\"$IDENTIFIER\",\"people_management_url\":\"$PEOPLE_MANAGEMENT_URL\",\"error_monitoring_enabled\":$ERROR_MONITORING_ENABLED,\"error_monitoring_dsn\":\"$ERROR_MONITORING_DSN\",\"error_monitoring_traces_sample_rate\":\"$ERROR_MONITORING_TRACES_SAMPLE_RATE\"}" \
    > secrets/prod_config.json

RUN flutter build web --release --dart-define-from-file=secrets/prod_config.json

# ---- Stage 2: Serve with Nginx ----
FROM nginx:alpine

COPY --from=build /app/build/web /usr/share/nginx/html

# Cache-Control headers + SPA fallback. Defined in nginx/default.conf so it
# stays reviewable and matches the cache-busting plan for production deploys.
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
