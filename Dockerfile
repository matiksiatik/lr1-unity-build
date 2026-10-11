# ЛР №3: Docker-образ веб-сервера Nginx (Alpine) с сжатой WebGL-игрой Unity 6.
FROM nginx:alpine

# Шаг 1: удаляем стандартную конфигурацию Nginx из базового образа
RUN rm -f /etc/nginx/conf.d/default.conf

# Шаг 2: подменяем её кастомной конфигурацией с заголовками Content-Encoding: gzip
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Шаг 3: копируем сжатую WebGL-сборку игры в корневую директорию Nginx
COPY Builds/WebGL/ /usr/share/nginx/html/

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
