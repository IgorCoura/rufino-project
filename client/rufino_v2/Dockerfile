# ---- Stage 1: Build Flutter Web ----
FROM ghcr.io/cirruslabs/flutter:stable AS build

WORKDIR /app

# Copy dependency files first for better layer caching. É um pub workspace: a
# resolução lê o pubspec de cada membro, então os quatro precisam existir aqui —
# só o da raiz faz `flutter pub get` falhar com "No workspace packages matching".
COPY pubspec.yaml pubspec.lock ./
COPY packages/rufino_core/pubspec.yaml packages/rufino_core/
COPY packages/tenant_management/pubspec.yaml packages/tenant_management/
COPY packages/bill_payment/pubspec.yaml packages/bill_payment/
COPY packages/people_management/pubspec.yaml packages/people_management/
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
# AppConfig.assertConfigured() exige as duas: sem elas o bundle compila e o app
# morre no boot.
ARG TENANT_MANAGEMENT_URL
ARG BILL_PAYMENT_URL
ARG ERROR_MONITORING_ENABLED
ARG ERROR_MONITORING_DSN
ARG ERROR_MONITORING_TRACES_SAMPLE_RATE

RUN mkdir -p secrets && \
    echo "{\"auth_code_authorization_endpoint\":\"$AUTH_CODE_AUTHORIZATION_ENDPOINT\",\"auth_code_token_endpoint\":\"$AUTH_CODE_TOKEN_ENDPOINT\",\"end_session_endpoint\":\"$END_SESSION_ENDPOINT\",\"identifier\":\"$IDENTIFIER\",\"people_management_url\":\"$PEOPLE_MANAGEMENT_URL\",\"tenant_management_url\":\"$TENANT_MANAGEMENT_URL\",\"bill_payment_url\":\"$BILL_PAYMENT_URL\",\"error_monitoring_enabled\":$ERROR_MONITORING_ENABLED,\"error_monitoring_dsn\":\"$ERROR_MONITORING_DSN\",\"error_monitoring_traces_sample_rate\":\"$ERROR_MONITORING_TRACES_SAMPLE_RATE\"}" \
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
