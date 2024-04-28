FROM rust:1.77-alpine3.19 as BUILDER
RUN apk add --no-cache musl-dev musl-utils linux-headers

COPY ./src/ ./app/src/
COPY ./Cargo.toml ./Cargo.lock /app/
WORKDIR /app/

RUN cargo build --release


FROM alpine:3.19.1

COPY --from=BUILDER /app/target/release/node_rs /app/
WORKDIR /app/
CMD ["./node_rs"]