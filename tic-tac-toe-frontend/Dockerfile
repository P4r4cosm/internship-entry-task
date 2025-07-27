# ./tic-tac-toe-frontend/Dockerfile

# --- Этап 1: Сборка (Builder) ---
# Используем Node.js для сборки статических файлов
FROM node:18-alpine AS builder

# Устанавливаем рабочую директорию в контейнере
WORKDIR /app

# Копируем package.json и package-lock.json для установки зависимостей
COPY package*.json ./
RUN npm install

# Копируем остальные файлы проекта
COPY . .

# ---- ВОТ КЛЮЧЕВЫЕ ИЗМЕНЕНИЯ ----
# 1. Объявляем аргумент сборки, который мы будем передавать из docker-compose
ARG REACT_APP_API_URL

# 2. Устанавливаем переменную окружения ДЛЯ СЛЕДУЮЩЕЙ КОМАНДЫ (npm run build)
ENV REACT_APP_API_URL=${REACT_APP_API_URL}
# ---------------------------------

# Запускаем сборку. React автоматически подхватит ENV переменную
RUN npm run build

# --- Этап 2: Запуск (Production) ---
# Используем легковесный веб-сервер Nginx для раздачи статики
FROM nginx:stable-alpine

# Копируем только собранные файлы из этапа "builder"
COPY --from=builder /app/build /usr/share/nginx/html

# (Опционально, но рекомендуется) Копируем свой конфиг Nginx для правильной работы React Router
# COPY nginx.conf /etc/nginx/conf.d/default.conf

# Открываем порт 80
EXPOSE 80

# Команда для запуска Nginx
CMD ["nginx", "-g", "daemon off;"]