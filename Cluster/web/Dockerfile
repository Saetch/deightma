FROM rust:1.78-alpine as BUILDER
RUN apk add --no-cache musl-dev musl-utils linux-headers
RUN cargo install --locked trunk
RUN mkdir /web
ADD Cargo.toml index.html Trunk.toml /web/
ADD src /web/src/
ADD static /web/static/
WORKDIR /web
RUN trunk build --release

FROM nginx:1.21-alpine

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /web/dist/ /usr/share/nginx/html/