
# build stage
FROM node:lts-alpine as build-stage
WORKDIR /app
COPY package*.json ./
ARG REQUESTS_PER_SECOND
ENV REQUESTS_PER_SECOND=$REQUESTS_PER_SECOND
RUN npm install
COPY . .
RUN npm run build

# production stage
FROM nginx:stable-alpine as production-stage
ENV CLUSTER="TRUE"
COPY --from=build-stage /app/dist /usr/share/nginx/html
CMD ["nginx", "-g", "daemon off;"]