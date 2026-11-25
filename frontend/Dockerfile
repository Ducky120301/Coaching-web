FROM node:26-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build admin && npx ng build client

FROM nginx:1.29-alpine
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/coaching-common.inc /etc/nginx/conf.d/coaching-common.inc
COPY --from=build /app/dist/admin/browser /usr/share/nginx/admin
COPY --from=build /app/dist/client/browser /usr/share/nginx/client
RUN echo '{"apiBaseUrl":""}' > /usr/share/nginx/admin/config.json \
 && echo '{"apiBaseUrl":""}' > /usr/share/nginx/client/config.json
EXPOSE 4200 4300
