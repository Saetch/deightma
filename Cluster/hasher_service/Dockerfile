FROM rust:1.78-alpine as BUILDER
RUN apk add --no-cache musl-dev musl-utils linux-headers

COPY ./src/ ./app/src/
COPY ./Cargo.toml ./Cargo.lock /app/
WORKDIR /app/

RUN cargo build --release


FROM alpine:3.19.1

COPY --from=BUILDER /app/target/release/coordinator /app/
WORKDIR /app/
CMD ["./hasher_service"]