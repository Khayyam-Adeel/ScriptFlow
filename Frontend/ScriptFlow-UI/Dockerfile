# Build context is this folder, e.g. (run from Frontend/ScriptFlow-UI):
#   docker build -t scriptflow-ui .

FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/script-flow-ui/browser /usr/share/nginx/html
EXPOSE 80
